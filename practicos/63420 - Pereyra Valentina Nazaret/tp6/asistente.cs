#!/usr/bin/env -S dotnet run
#:package DotNetEnv@3.2.0
#:package Microsoft.Extensions.AI@10.7.0
#:package Microsoft.Extensions.AI.OpenAI@10.7.0
#:package Terminal.Gui@2.4.9-develop.1
#:property Nullable=enable
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

#region Configuración

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-4o-mini";

string systemPrompt = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "Sos un asistente útil. Respondé en español.";

List<ChatMessage> mensajes = [
    new(ChatRole.System, systemPrompt)
];

#endregion

#region Herramientas

AIFunction leerArchivo = AIFunctionFactory.Create(
    (string ruta) =>
    {
        try { return File.ReadAllText(ruta); }
        catch (Exception ex) { return $"Error al leer '{ruta}': {ex.Message}"; }
    },
    "leer-archivo",
    "Devuelve el contenido de un archivo de texto dado su ruta."
);

AIFunction escribirArchivo = AIFunctionFactory.Create(
    (string ruta, string contenido) =>
    {
        try
        {
            var dir = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ruta, contenido);
            return $"Archivo '{ruta}' guardado correctamente.";
        }
        catch (Exception ex) { return $"Error al escribir '{ruta}': {ex.Message}"; }
    },
    "escribir-archivo",
    "Crea o sobrescribe un archivo con el contenido indicado."
);

AIFunction listarArchivos = AIFunctionFactory.Create(
    (string ruta) =>
    {
        try
        {
            var directorio = string.IsNullOrWhiteSpace(ruta) ? "." : ruta;
            if (!Directory.Exists(directorio))
                return $"El directorio '{directorio}' no existe.";

            var entradas = Directory.GetFileSystemEntries(directorio)
                .Select(e => Directory.Exists(e)
                    ? $"[DIR]  {Path.GetFileName(e)}"
                    : $"[FILE] {Path.GetFileName(e)}")
                .OrderBy(e => e);

            return string.Join("\n", entradas);
        }
        catch (Exception ex) { return $"Error al listar '{ruta}': {ex.Message}"; }
    },
    "listar-archivos",
    "Lista los archivos y carpetas de un directorio dado su ruta."
);

var chatOptions = new ChatOptions
{
    Tools = [leerArchivo, escribirArchivo, listarArchivos],
    ToolMode = ChatToolMode.Auto
};

#endregion

#region Cliente IA

IChatClient chatBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url ?? "https://api.openai.com/v1") })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient chat = chatBase
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

#endregion

#region Interfaz de usuario

using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" ◆ Asistente AI · {modelo} · [Esc] para salir ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var marcoConversacion = new FrameView
{
    Title = " 💬 Conversacion ",
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill() - 3
};

var panelConversacion = new Markdown
{
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Text = "_Escribi un mensaje para comenzar._\n"
};

marcoConversacion.Add(panelConversacion);

var marcoEntrada = new FrameView
{
    Title = " ✎ Mensaje  [Enter] enviar  [Esc] salir ",
    X = 0,
    Y = Pos.Bottom(marcoConversacion),
    Width = Dim.Fill(),
    Height = 3
};

var panelEntrada = new TextField
{
    X = 0, Y = 0,
    Width = Dim.Fill() - 12
};

var btnEnviar = new Button
{
    Title = " ➤ Enviar",
    X = Pos.Right(panelEntrada) + 1,
    Y = 0
};

marcoEntrada.Add(panelEntrada, btnEnviar);
ventana.Add(marcoConversacion, marcoEntrada);

#endregion

#region Lógica de envío

string ConstruirHistorial(string respuestaActual = "")
{
    var sb = new System.Text.StringBuilder();
    foreach (var msg in mensajes.Where(m => m.Role != ChatRole.System))
    {
        var nombre = msg.Role == ChatRole.User ? "## ▶ Vos" : "## ◆ Asistente";
        sb.AppendLine(nombre);
        sb.AppendLine();
        sb.AppendLine(msg.Text);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }
    if (!string.IsNullOrEmpty(respuestaActual))
    {
        sb.AppendLine("## ◆ Asistente");
        sb.AppendLine();
        sb.AppendLine(respuestaActual);
    }
    return sb.ToString();
}

void ActualizarPanel(string texto)
{
    panelConversacion.Text = texto;
    panelConversacion.SetNeedsDraw();
}


async Task EnviarMensajeAsync()
{
    var textoUsuario = panelEntrada.Text?.Trim();
    if (string.IsNullOrWhiteSpace(textoUsuario)) return;

    panelEntrada.Enabled = false;
    btnEnviar.Enabled    = false;
    panelEntrada.Text    = "";

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));

    app.Invoke(() => ActualizarPanel(ConstruirHistorial("_Pensando..._")));

    string textoAsistente = "";

    try
    {
      

await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, chatOptions))
{
    if (fragmento.Text is null)
        continue;

    textoAsistente += fragmento.Text;

    var snapshot = textoAsistente;

    app.Invoke(() =>
    {
        ActualizarPanel(ConstruirHistorial(snapshot));
    });
}

    }
    catch (Exception ex)
    {
        textoAsistente = $"**Error al conectar:** {ex.Message}\n\n_Podés intentar de nuevo._";
        app.Invoke(() => ActualizarPanel(ConstruirHistorial(textoAsistente)));
    }
    finally
    {
        if (!string.IsNullOrWhiteSpace(textoAsistente))
            mensajes.Add(new ChatMessage(ChatRole.Assistant, textoAsistente));

        app.Invoke(() =>
        {
            ActualizarPanel(ConstruirHistorial());
            panelEntrada.Enabled = true;
            btnEnviar.Enabled    = true;
            panelEntrada.SetFocus();
        });
    }
}

#endregion

#region Eventos

btnEnviar.Accepting += (s, e) =>
{
    _ = EnviarMensajeAsync();
};

panelEntrada.KeyDown += (s, e) =>
{
    // Usamos ToString() como tenías al principio, que es 100% seguro
    if (e.KeyCode.ToString() == "Enter" && panelEntrada.Enabled)
    {
        e.Handled = true;
        _ = EnviarMensajeAsync();
    }
    
    if (e.KeyCode.ToString() == "Esc")
    {
        app.RequestStop();
    }
};

ventana.KeyDown += (s, e) =>
{
    if (e.KeyCode.ToString() == "Esc")
    {
        app.RequestStop();
    }
};

#endregion

app.Run(ventana);
