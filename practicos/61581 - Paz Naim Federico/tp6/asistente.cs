#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Drawing;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

if (string.IsNullOrWhiteSpace(url))
{
    Console.Error.WriteLine($"Falta configurar {proveedor}_API_URL en el archivo .env.");
    return;
}

var promptSistema = File.ReadAllText("AGENTS.md");

IChatClient chatBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = NormalizarEndpoint(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient chat = new ChatClientBuilder(chatBase)
    .UseFunctionInvocation()
    .Build();

List<ChatMessage> mensajes = [
    new(ChatRole.System, promptSistema)
];

ChatOptions opciones = new()
{
    Tools =
    [
        AIFunctionFactory.Create((string ruta) => LeerArchivo(ruta), "leer-archivo", "Devuelve el contenido de un archivo de texto."),
        AIFunctionFactory.Create((string ruta, string contenido) => EscribirArchivo(ruta, contenido), "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado."),
        AIFunctionFactory.Create((string ruta) => ListarArchivos(ruta), "listar-archivos", "Lista los archivos y carpetas de un directorio.")
    ]
};

StringBuilder conversacion = new();

using IApplication app = Application.Create().Init();
using var ventana = new Window
{
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var historial = new Markdown
{
    Text = "Escribí un mensaje para comenzar la conversación.",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(5)
};

var entrada = new TextView
{
    X = 0,
    Y = Pos.AnchorEnd(4),
    Width = Dim.Fill(14),
    Height = 3
};

entrada.SetScheme(new Scheme
{
    Normal = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.White),
    Focus = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.White)
});

var enviar = new Button
{
    Text = " Enviar ",
    X = Pos.AnchorEnd(12),
    Y = Pos.AnchorEnd(3),
    IsDefault = true
};

ventana.Add(historial, entrada, enviar);
entrada.SetFocus();

ventana.KeyDown += (_, args) =>
{
    if (args == Key.Esc)
    {
        args.Handled = true;
        app.RequestStop();
    }
};

entrada.KeyDown += async (_, args) =>
{
    if (args == Key.Enter)
    {
        args.Handled = true;
        await EnviarMensajeAsync();
    }
};

enviar.Accepted += async (_, _) => await EnviarMensajeAsync();

app.Run(ventana);

async Task EnviarMensajeAsync()
{
    var textoUsuario = entrada.Text?.ToString()?.Trim();

    if (string.IsNullOrWhiteSpace(textoUsuario) || !entrada.Enabled)
    {
        return;
    }

    entrada.Text = string.Empty;
    entrada.Enabled = false;
    enviar.Enabled = false;

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
    conversacion.AppendLine("## Vos");
    conversacion.AppendLine();
    conversacion.AppendLine(textoUsuario);
    conversacion.AppendLine();
    conversacion.AppendLine("## Asistente");
    conversacion.AppendLine();

    var inicioRespuesta = conversacion.Length;
    ActualizarHistorial();

    try
    {
        await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
            conversacion.Append(fragmento.Text);
            ActualizarHistorial();
        }

        var textoAsistente = conversacion.ToString(inicioRespuesta, conversacion.Length - inicioRespuesta).Trim();
        mensajes.Add(new ChatMessage(ChatRole.Assistant, textoAsistente));
        conversacion.AppendLine();
        conversacion.AppendLine();
        ActualizarHistorial();
    }
    catch (Exception ex)
    {
        var error = $"No pude completar la respuesta: {ex.Message}";
        conversacion.AppendLine();
        conversacion.AppendLine($"> {error}");
        conversacion.AppendLine();
        mensajes.Add(new ChatMessage(ChatRole.Assistant, error));
        ActualizarHistorial();
    }
    finally
    {
        entrada.Enabled = true;
        enviar.Enabled = true;
        entrada.SetFocus();
    }
}

void ActualizarHistorial()
{
    historial.Text = conversacion.ToString();
    historial.SetNeedsDraw();
}

static Uri NormalizarEndpoint(string url)
{
    var endpoint = new Uri(url);
    const string sufijoChatCompletions = "/chat/completions";

    if (endpoint.AbsolutePath.EndsWith(sufijoChatCompletions, StringComparison.OrdinalIgnoreCase))
    {
        var baseUrl = url[..^sufijoChatCompletions.Length].TrimEnd('/');
        return new Uri(baseUrl);
    }

    return endpoint;
}

static string LeerArchivo(string ruta)
{
    var rutaSegura = ResolverRuta(ruta);

    if (!File.Exists(rutaSegura))
    {
        return $"No existe el archivo: {ruta}";
    }

    return File.ReadAllText(rutaSegura);
}

static string EscribirArchivo(string ruta, string contenido)
{
    var rutaSegura = ResolverRuta(ruta);
    var directorio = Path.GetDirectoryName(rutaSegura);

    if (!string.IsNullOrWhiteSpace(directorio))
    {
        Directory.CreateDirectory(directorio);
    }

    File.WriteAllText(rutaSegura, contenido);
    return $"Archivo escrito correctamente: {ruta}";
}

static string ListarArchivos(string ruta)
{
    var rutaSegura = ResolverRuta(ruta);

    if (!Directory.Exists(rutaSegura))
    {
        return $"No existe el directorio: {ruta}";
    }

    var entradas = Directory.EnumerateFileSystemEntries(rutaSegura)
        .Select(entrada => Directory.Exists(entrada)
            ? $"[dir] {Path.GetFileName(entrada)}"
            : $"[archivo] {Path.GetFileName(entrada)}")
        .OrderBy(entrada => entrada)
        .ToArray();

    return entradas.Length == 0
        ? "El directorio está vacío."
        : string.Join(Environment.NewLine, entradas);
}

static string ResolverRuta(string ruta)
{
    if (string.IsNullOrWhiteSpace(ruta))
    {
        return Directory.GetCurrentDirectory();
    }

    var rutaCompleta = Path.GetFullPath(Path.IsPathRooted(ruta)
        ? ruta
        : Path.Combine(Directory.GetCurrentDirectory(), ruta));

    var raizProyecto = Path.GetFullPath(Directory.GetCurrentDirectory());
    if (!rutaCompleta.StartsWith(raizProyecto, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La ruta debe estar dentro del directorio del proyecto.");
    }

    return rutaCompleta;
}
