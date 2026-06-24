#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ComponentModel;
using System.ClientModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var directorioProyecto = Directory.GetCurrentDirectory();
var rutaAgentes = Path.Combine(directorioProyecto, "AGENTS.md");
if (!File.Exists(rutaAgentes))
{
    throw new FileNotFoundException(
        "No se encontró AGENTS.md. Ejecutá la aplicación desde la carpeta del TP6.",
        rutaAgentes);
}

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

if (string.IsNullOrWhiteSpace(url))
{
    throw new InvalidOperationException($"Falta configurar {proveedor}_API_URL en el entorno o en .env.");
}

IChatClient chatBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = NormalizeOpenAIEndpoint(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient chat = new ChatClientBuilder(chatBase)
    .UseFunctionInvocation()
    .Build();

var herramientas = new FileSystemTools(directorioProyecto);
ChatOptions opciones = new()
{
    Tools =
    [
        AIFunctionFactory.Create(
            herramientas.LeerArchivo,
            new AIFunctionFactoryOptions { Name = "leer-archivo" }),
        AIFunctionFactory.Create(
            herramientas.EscribirArchivo,
            new AIFunctionFactoryOptions { Name = "escribir-archivo" }),
        AIFunctionFactory.Create(
            herramientas.ListarArchivos,
            new AIFunctionFactoryOptions { Name = "listar-archivos" })
    ]
};

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText(rutaAgentes))
];

List<VisibleMessage> conversacion = [];
var respuestaEnCurso = false;

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var panelConversacion = new FrameView
{
    Title = " Conversación ",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(4)
};

var markdown = new Markdown
{
    Text = RenderConversation(conversacion),
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ShowCopyButtons = true
};

var panelEntrada = new FrameView
{
    Title = " Mensaje ",
    X = 0,
    Y = Pos.AnchorEnd(4),
    Width = Dim.Fill(),
    Height = 4
};

var entrada = new TextField
{
    X = 1,
    Y = 1,
    Width = Dim.Fill(14),
    Height = 1
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.AnchorEnd(12),
    Y = 1,
    Width = 10,
    IsDefault = true
};

panelConversacion.Add(markdown);
panelEntrada.Add(entrada, enviar);
ventana.Add(panelConversacion, panelEntrada);

enviar.Accepting += (_, _) => _ = EnviarMensajeAsync();
ventana.KeyDown += (_, key) =>
{
    if (key == Key.Esc)
    {
        app.RequestStop();
    }
};

entrada.SetFocus();

app.Run(ventana);

async Task EnviarMensajeAsync()
{
    var textoUsuario = entrada.Text?.ToString()?.Trim();
    if (string.IsNullOrWhiteSpace(textoUsuario) || respuestaEnCurso)
    {
        return;
    }

    SetInputEnabled(false);
    entrada.Text = string.Empty;

    conversacion.Add(new VisibleMessage("Vos", textoUsuario));
    conversacion.Add(new VisibleMessage("Asistente", string.Empty));
    mensajes.Add(new(ChatRole.User, textoUsuario));
    UpdateConversation(autoScroll: true);

    try
    {
        await StreamAssistantResponseAsync(conversacion[^1]);
    }
    catch (Exception ex)
    {
        app.Invoke(() =>
        {
            conversacion[^1] = conversacion[^1] with
            {
                Markdown = $"No pude completar la respuesta.\n\n`{ex.Message}`"
            };
            UpdateConversation(autoScroll: true);
        });
    }
    finally
    {
        app.Invoke(() => SetInputEnabled(true));
    }
}

async Task StreamAssistantResponseAsync(VisibleMessage respuestaVisible)
{
    var builder = new StringBuilder();
    await foreach (var update in chat.GetStreamingResponseAsync(mensajes, opciones))
    {
        if (string.IsNullOrEmpty(update.Text))
        {
            continue;
        }

        builder.Append(update.Text);
        var parcial = builder.ToString();
        app.Invoke(() =>
        {
            var mantenerAbajo = IsScrolledNearBottom();
            var index = conversacion.IndexOf(respuestaVisible);
            if (index >= 0)
            {
                respuestaVisible = respuestaVisible with { Markdown = parcial };
                conversacion[index] = respuestaVisible;
                UpdateConversation(autoScroll: mantenerAbajo);
            }
        });
    }

    var textoFinal = builder.ToString();
    if (string.IsNullOrWhiteSpace(textoFinal))
    {
        textoFinal = "_El modelo no devolvió contenido visible._";
    }

    app.Invoke(() =>
    {
        var mantenerAbajo = IsScrolledNearBottom();
        mensajes.Add(new(ChatRole.Assistant, textoFinal));
        var finalIndex = conversacion.IndexOf(respuestaVisible);
        if (finalIndex >= 0)
        {
            conversacion[finalIndex] = respuestaVisible with { Markdown = textoFinal };
            UpdateConversation(autoScroll: mantenerAbajo);
        }
    });
}

void SetInputEnabled(bool enabled)
{
    respuestaEnCurso = !enabled;
    entrada.Enabled = enabled;
    enviar.Enabled = enabled;
    if (enabled)
    {
        entrada.SetFocus();
    }
}

void UpdateConversation(bool autoScroll)
{
    markdown.Text = RenderConversation(conversacion);
    if (autoScroll)
    {
        markdown.Viewport = markdown.Viewport with { Y = Math.Max(0, markdown.LineCount - 1) };
    }
    markdown.SetNeedsDraw();
}

bool IsScrolledNearBottom()
{
    var ultimaLineaVisible = markdown.Viewport.Y + markdown.Viewport.Height;
    return ultimaLineaVisible >= Math.Max(0, markdown.LineCount - 2);
}

static Uri NormalizeOpenAIEndpoint(string configuredUrl)
{
    var endpoint = new Uri(configuredUrl, UriKind.Absolute);
    const string chatCompletionsSuffix = "/chat/completions";
    if (!endpoint.AbsolutePath.EndsWith(chatCompletionsSuffix, StringComparison.OrdinalIgnoreCase))
    {
        return endpoint;
    }

    // OpenAIClientOptions.Endpoint espera el endpoint base del servicio. El
    // .env de la cátedra usa rutas compatibles con Chat Completions, por eso se
    // recorta solo ese sufijo conocido y se conserva el resto del host/base path.
    var builder = new UriBuilder(endpoint);
    builder.Path = endpoint.AbsolutePath[..^chatCompletionsSuffix.Length].TrimEnd('/');
    builder.Query = string.Empty;
    builder.Fragment = string.Empty;
    return builder.Uri;
}

static string RenderConversation(IEnumerable<VisibleMessage> mensajes)
{
    var builder = new StringBuilder();
    foreach (var mensaje in mensajes)
    {
        builder.Append("## ").AppendLine(mensaje.Author);
        builder.AppendLine();
        builder.AppendLine(string.IsNullOrWhiteSpace(mensaje.Markdown) ? "_Escribiendo..._" : mensaje.Markdown);
        builder.AppendLine();
    }

    return builder.Length == 0
        ? "# Asistente IA\n\nEscribí un mensaje y presioná Enter para conversar."
        : builder.ToString();
}

/// <summary>
/// Representa un turno visible del diálogo. Se mantiene separado de
/// <see cref="ChatMessage"/> porque el mensaje de sistema no debe renderizarse.
/// </summary>
sealed record VisibleMessage(string Author, string Markdown);

/// <summary>
/// Herramientas expuestas al modelo para operar únicamente dentro de la carpeta
/// del trabajo práctico. Todas las rutas relativas se resuelven contra ese
/// directorio y se rechazan los intentos de escapar mediante rutas absolutas o
/// segmentos "..".
/// </summary>
sealed class FileSystemTools
{
    private readonly string root;

    public FileSystemTools(string root)
    {
        this.root = Path.GetFullPath(root);
    }

    [Description("Devuelve el contenido de un archivo de texto dentro del proyecto.")]
    public async Task<string> LeerArchivo(
        [Description("Ruta relativa del archivo a leer.")] string ruta)
    {
        var path = ResolveInsideRoot(ruta);
        if (!File.Exists(path))
        {
            return $"No existe el archivo: {ruta}";
        }

        return await File.ReadAllTextAsync(path);
    }

    [Description("Crea o sobrescribe un archivo de texto dentro del proyecto.")]
    public async Task<string> EscribirArchivo(
        [Description("Ruta relativa del archivo a crear o sobrescribir.")] string ruta,
        [Description("Contenido textual que debe guardarse.")] string contenido)
    {
        var path = ResolveInsideRoot(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, contenido);
        return $"Archivo escrito: {ruta}";
    }

    [Description("Lista archivos y carpetas de un directorio dentro del proyecto.")]
    public string ListarArchivos(
        [Description("Ruta relativa del directorio a listar. Usar punto para la carpeta raíz.")] string ruta)
    {
        var path = ResolveInsideRoot(ruta);
        if (!Directory.Exists(path))
        {
            return $"No existe el directorio: {ruta}";
        }

        var builder = new StringBuilder();
        foreach (var item in Directory.EnumerateFileSystemEntries(path).OrderBy(Path.GetFileName))
        {
            var name = Path.GetFileName(item);
            builder.AppendLine(Directory.Exists(item) ? $"{name}/" : name);
        }

        return builder.Length == 0 ? "El directorio está vacío." : builder.ToString();
    }

    private string ResolveInsideRoot(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            throw new ArgumentException("La ruta no puede estar vacía.", nameof(ruta));
        }

        var fullPath = Path.GetFullPath(Path.Combine(root, ruta));
        var relative = Path.GetRelativePath(root, fullPath);
        if (Path.IsPathRooted(ruta)
            || relative == ".."
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("La ruta solicitada está fuera del proyecto.");
        }

        return fullPath;
    }
}
