#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

[Description ("Devuelve el contenido de un archivo de texto.")]
string LeerArchivo([Description("Ruta del archivo a leer.")] string ruta)
{
    if (!File.Exists(ruta))
        return $"Error: el archivo '{ruta}' no existe.";
        return File.ReadAllText(ruta);
}

[Description("Crea o sobrescribe un archivo con el contenido indicado.")]
string EscribirArchivo(
    [Description("Ruta del archivo a escribir.")] string ruta,
    [Description("Contenido a escribir en el archivo.")] string contenido)
    {
        try
        {
            var dir = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ruta, contenido);
            return $"Archivo '{ruta}' guardado correctamente.";
        }
        catch (Exception ex)
        {
             return $"Error al escribir '{ruta}': {ex.Message}";
        }
    }

    [Description("Lista los archivos y carpetas de un directorio.")]
    string ListarArchivos([Description("Ruta del directorio a listar.")] string ruta)
    {
        if(!Directory.Exists(ruta))
            return $"Error: el directorio '{ruta}' no existe.";

        var entradas = Directory.GetFileSystemEntries(ruta)
            .Select(e => Directory.Exists(e)
                ? $"[DIR]  {Path.GetFileName(e)}"
                : $"[FILE] {Path.GetFileName(e)}")
                .OrderBy(e => e);

         return string.Join("\n", entradas);
    }

IChatClient chat = new ChatClientBuilder(
        new OpenAIClient(
            new ApiKeyCredential(apiKey ?? "no-requiere-key"),
            new OpenAIClientOptions { Endpoint = new Uri(url) })
                .GetChatClient(modelo)
                .AsIChatClient())
        .UseFunctionInvocation()
        .Build();
   
var herramientas = new List<AITool> {
    AIFunctionFactory.Create(LeerArchivo,   "leer-archivo"),
    AIFunctionFactory.Create(EscribirArchivo,   "escribir-archivo"),
    AIFunctionFactory.Create(ListarArchivos,   "listar-archivo"),
};

var opciones = new ChatOptions{Tools = herramientas };


List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md")+
        "\n\nRespondé siempre usando Markdown cuando sea posible." +
        "\nUsá títulos, listas y bloques de código con triple acento grave cuando la respuesta lo justifique." +
        "\nSi mostrás código, encerralo en bloques Markdown.")
];

using IApplication app = Application.Create().Init();

var ventana= new Window
{
    Title = $"AsistenteAI · {modelo} ",
    Width  = Dim.Fill(),
    Height = Dim.Fill(),
};

var panelConversacion = new TextView
{
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    ReadOnly = true,
    WordWrap = true 
};

var campoTexto = new TextField
{
    X      = 0,
    Y      = Pos.AnchorEnd(2),
    Width  = Dim.Fill(10),
    Height = 1,
};

var botonEnviar = new Button 
{
    X    = Pos.AnchorEnd(9),
    Y    = Pos.AnchorEnd(2),
    Text = "Enviar",
};

ventana.Add(panelConversacion, campoTexto, botonEnviar);

void ActualizarConversacion()
{
    app.Invoke(() => {
        panelConversacion.Text = textoConversacion.ToString();
        panelConversacion.MoveEnd();
        panelConversacion.SetNeedsDraw();
    });
}

void AgregarTexto(string texto)
{
    textoConversacion.Append(texto);
    ActualizarConversacion();
}

void ReemplazarTextoDesde(int inicio, string texto)
{
     textoConversacion.Remove(inicio, textoConversacion.Length - inicio);
     textoConversacion.Append(texto);
    ActualizarConversacion();
}

string RenderizarMarkdown(string markdown)
{
    var lineas = markdown.Replace("\r\n", "\n").Split('\n');
    var salida = new StringBuilder();
    var enCodigo = false;
    var marcaCodigo = new string('`', 3);

    foreach (var linea in lineas)
    {
        var limpia = linea.TrimStart();

        if (limpia.StartsWith(marcaCodigo))
        {
            enCodigo = !enCodigo;
            salida.AppendLine(enCodigo ? "----- codigo -----" : "------------------");
            continue;
        }

        if(enCodigo)
            salida.AppendLine("  " + linea);
         else if (linea.StartsWith("### "))
          salida.AppendLine("  " + linea.Substring(4).ToUpperInvariant());   
        else if (linea.StartsWith("## "))
           salida.AppendLine("== " + linea.Substring(3).ToUpperInvariant() + " ==");
        else if (linea.StartsWith("# "))
           salida.AppendLine("=== " + linea.Substring(2).ToUpperInvariant() + " ==="); 
         else
           salida.AppendLine(linea.Replace("**", "")); 
    }
    return salida.ToString();
}

void SetEntradaHabilitada(bool habilitada)
{
    app.Invoke(() => 
    {
      campoTexto.Enabled = habilitada;
      botonEnviar.Enabled = habilitada;
      if (habilitada) campoTexto.SetFocus();  
    });
}

async Task EnviarMensajeAsync()
{
    var texto = campoTexto.Text?.Trim();
    if (string.IsNullOrEmpty(texto)) return;
    app.Invoke(() => campoTexto.Text = "");
    SetEntradaHabilitada(false);

    AgregarTexto($"\n─── Vos ────────────────────────────────\n{texto}\n");
    mensajes.Add(new ChatMessage(ChatRole.User, texto));

    AgregarTexto("\n─── Asistente ──────────────────────────\n");
    var respuestaCompleta = new StringBuilder();
    var inicioRespuestaAsistente = textoConversacion.Length;

    try
    {
        await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
           if (!string.IsNullOrEmpty(fragmento.Text))
           {
                AgregarTexto(fragmento.Text);
                respuestaCompleta.Append(fragmento.Text);
           } 
        }
    }
    catch (Exception ex)
    {
        AgregarTexto($"\n[Error]: {ex.Message}\n");
    }

    if(respuestaCompleta.Length > 0)
    {
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta.ToString()));

        var respuestaRenderizada = RenderizarMarkdown(respuestaCompleta.ToString());
        ReemplazarTextoDesde(inicioRespuestaAsistente, respuestaRenderizada);
    }
     AgregarTexto("\n");
     SetEntradaHabilitada(true);
}


ventana.Add(new Markdown {
    Text = $"# Vos\n\n{pregunta}\n\n# Asistente\n\n{respuesta.Text}",
    Width = Dim.Fill(), Height = Dim.Fill()
});

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
