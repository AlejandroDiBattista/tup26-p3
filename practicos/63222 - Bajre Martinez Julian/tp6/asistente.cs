#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var (chat, modelo) = CrearChatClient(args);

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

var opcionesChat = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(LeerArchivo),
        AIFunctionFactory.Create(EscribirArchivo),
        AIFunctionFactory.Create(ListarArchivos)
    ]
};


using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

const int altoEntrada = 3;

var panelConversacion = new Markdown {
    Text = "# Asistente\n\nEscribí un mensaje para comenzar.",
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(altoEntrada)
};

var campoEntrada = new TextField {
    X = 0,
    Y = Pos.AnchorEnd(altoEntrada),
    Width = Dim.Fill(12),
    Height = 1
};

var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.AnchorEnd(11),
    Y = Pos.AnchorEnd(altoEntrada),
    Width = 11
};

string TextoDeHistorial(IEnumerable<ChatMessage> historial)
{
    var sb = new StringBuilder();
    foreach (var m in historial)
    {
        if (m.Role == ChatRole.System) continue;
        var encabezado = m.Role == ChatRole.User ? "Vos" : "Asistente";
        sb.AppendLine($"# {encabezado}\n");
        sb.AppendLine(m.Text);
        sb.AppendLine();
    }
    return sb.ToString();
}

bool UsuarioEstaAlFinal()
{
    var maxScrollY = Math.Max(0, panelConversacion.GetContentSize().Height - panelConversacion.Viewport.Height);
    return panelConversacion.Viewport.Y >= maxScrollY - 1;
}

void IrAlFinal()
{
    var maxScrollY = Math.Max(0, panelConversacion.GetContentSize().Height - panelConversacion.Viewport.Height);
    var vp = panelConversacion.Viewport;
    panelConversacion.Viewport = vp with { Y = maxScrollY };
}

async void EnviarMensaje()
{
    var texto = campoEntrada.Text?.ToString()?.Trim();
    if (string.IsNullOrEmpty(texto)) return;

    campoEntrada.Enabled = false;
    botonEnviar.Enabled = false;
    campoEntrada.Text = "";

    mensajes.Add(new ChatMessage(ChatRole.User, texto));
    panelConversacion.Text = TextoDeHistorial(mensajes);
    IrAlFinal();

    var textoAsistente = new StringBuilder();
    mensajes.Add(new ChatMessage(ChatRole.Assistant, ""));

    await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes.Take(mensajes.Count - 1)))
    {
        if (string.IsNullOrEmpty(fragmento.Text)) continue;
        textoAsistente.Append(fragmento.Text);

        app.Invoke(() =>
        {
            mensajes[^1] = new ChatMessage(ChatRole.Assistant, textoAsistente.ToString());

            var pegadoAlFinal = UsuarioEstaAlFinal();
            panelConversacion.Text = TextoDeHistorial(mensajes);
            if (pegadoAlFinal) IrAlFinal();
        });
    }

    campoEntrada.Enabled = true;
    botonEnviar.Enabled = true;
    campoEntrada.SetFocus();
}

botonEnviar.Accepting += (_, _) => EnviarMensaje();
campoEntrada.KeyDown += (_, key) =>
{
    if (key.KeyCode == Terminal.Gui.Input.Key.Enter)
    {
        EnviarMensaje();
        key.Handled = true;
    }
};


ventana.Add(panelConversacion, campoEntrada, botonEnviar);

app.Run(ventana);

static (IChatClient chat, string modelo) CrearChatClient(string[] args)
{
    var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
    var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL")?? "https://api.openai.com/v1";
    var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
    var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

    IChatClient chat = new OpenAIClient(
            new ApiKeyCredential(apiKey ?? "no-requiere-key"),
            new OpenAIClientOptions { Endpoint = new Uri(url) })
        .GetChatClient(modelo)
        .AsIChatClient();

    return (chat, modelo);
}

[Description("Devuelve el contenido de un archivo de texto.")]
static string LeerArchivo(
    [Description("Ruta del archivo a leer, relativa o absoluta.")] string ruta)
{
    try
    {
        return File.ReadAllText(ruta);
    }
    catch (Exception ex)
    {
        return $"Error al leer el archivo '{ruta}': {ex.Message}";
    }
}

[Description("Crea o sobrescribe un archivo con el contenido indicado.")]
static string EscribirArchivo(
    [Description("Ruta del archivo a crear o sobrescribir.")] string ruta,
    [Description("Contenido a escribir en el archivo.")] string contenido)
{
    try
    {
        File.WriteAllText(ruta, contenido);
        return $"Archivo '{ruta}' guardado correctamente.";
    }
    catch (Exception ex)
    {
        return $"Error al escribir el archivo '{ruta}': {ex.Message}";
    }
}

[Description("Lista los archivos y carpetas de un directorio.")]
static string ListarArchivos(
    [Description("Ruta del directorio a listar.")] string ruta)
{
    try
    {
        var entradas = Directory.GetFileSystemEntries(ruta)
            .Select(e => Directory.Exists(e) ? $"[dir]  {Path.GetFileName(e)}" : $"[file] {Path.GetFileName(e)}");
        var resultado = string.Join("\n", entradas);
        return string.IsNullOrEmpty(resultado) ? "El directorio está vacío." : resultado;
    }
    catch (Exception ex)
    {
        return $"Error al listar '{ruta}': {ex.Message}";
    }
}
