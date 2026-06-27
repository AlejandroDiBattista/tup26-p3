#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.4
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load(".env.ejemplo");
DotNetEnv.Env.Load(".env");

string proveedor = (args.Length > 0 ? args[0] : "grok").ToUpperInvariant();
string? url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
string? apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
string? modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL");

if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(modelo))
{
    Console.Error.WriteLine($"Faltan variables para {proveedor}. Revisá {proveedor}_API_URL y {proveedor}_MODEL.");
    return;
}

if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("tu_clave_api", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Falta {proveedor}_API_KEY en el archivo .env.");
    return;
}

Uri endpoint = NormalizarEndpoint(url);

IChatClient clienteBase = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = endpoint })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient chat = new ChatClientBuilder(clienteBase)
    .UseFunctionInvocation()
    .Build();

List<AITool> herramientas =
[
    AIFunctionFactory.Create(LeerArchivo, new AIFunctionFactoryOptions
    {
        Name = "leer_archivo",
        Description = "Lee el contenido de un archivo de texto del proyecto."
    }),
    AIFunctionFactory.Create(EscribirArchivo, new AIFunctionFactoryOptions
    {
        Name = "escribir_archivo",
        Description = "Crea o sobrescribe un archivo de texto dentro del proyecto."
    }),
    AIFunctionFactory.Create(ListarArchivos, new AIFunctionFactoryOptions
    {
        Name = "listar_archivos",
        Description = "Lista archivos y carpetas de un directorio del proyecto."
    })
];

ChatOptions opciones = new()
{
    Tools = herramientas,
    AllowMultipleToolCalls = true
};

string promptSistema = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "Sos un asistente de chat por terminal. Respondé en español, con claridad y de forma útil.";

List<ChatMessage> mensajes =
[
    new(ChatRole.System, promptSistema)
];

StringBuilder markdown = new();
markdown.AppendLine("# Asistente MEAI");
markdown.AppendLine();
markdown.AppendLine("Escribí un mensaje para comenzar.");

using IApplication app = Application.Create().Init();
using var ventana = new Window
{
    Title = $" AsistenteIA · {proveedor} · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var panelConversacion = new Markdown
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    CanFocus = true,
    Text = markdown.ToString()
};

var campoEntrada = new TextField
{
    X = 0,
    Y = Pos.AnchorEnd(1),
    Width = Dim.Fill(14),
    CanFocus = true
};

var botonEnviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(campoEntrada),
    Y = Pos.AnchorEnd(1),
    Width = 14
};

ventana.Add(panelConversacion, campoEntrada, botonEnviar);

campoEntrada.Accepting += async (_, e) =>
{
    e.Handled = true;
    await EnviarAsync();
};

botonEnviar.Accepting += async (_, e) =>
{
    e.Handled = true;
    await EnviarAsync();
};

ventana.KeyDown += (_, e) =>
{
    if (e == Key.Esc)
    {
        app.RequestStop();
        e.Handled = true;
    }
};

campoEntrada.SetFocus();
app.Run(ventana);

async Task EnviarAsync()
{
    string textoUsuario = campoEntrada.Text?.ToString()?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(textoUsuario) || !campoEntrada.Enabled)
    {
        return;
    }

    campoEntrada.Text = "";
    campoEntrada.Enabled = false;
    botonEnviar.Enabled = false;

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
    markdown.AppendLine();
    markdown.AppendLine("## Vos");
    markdown.AppendLine();
    markdown.AppendLine(textoUsuario);
    markdown.AppendLine();
    markdown.AppendLine("## Asistente");
    markdown.AppendLine();

    panelConversacion.Text = markdown.ToString();

    StringBuilder respuestaCompleta = new();

    try
    {
        await foreach (ChatResponseUpdate parte in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
            if (string.IsNullOrEmpty(parte.Text))
            {
                continue;
            }

            respuestaCompleta.Append(parte.Text);
            app.Invoke(() =>
            {
                panelConversacion.Text = markdown.ToString() + respuestaCompleta;
            });
        }

        string respuesta = respuestaCompleta.ToString();

        if (string.IsNullOrWhiteSpace(respuesta))
        {
            respuesta = "_El modelo no devolvió texto._";
        }

        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuesta));
        markdown.AppendLine(respuesta);
    }
    catch (Exception ex)
    {
        string error = $"**Error:** {ex.Message}";
        mensajes.Add(new ChatMessage(ChatRole.Assistant, error));
        markdown.AppendLine(error);
    }
    finally
    {
        app.Invoke(() =>
        {
            panelConversacion.Text = markdown.ToString();
            campoEntrada.Enabled = true;
            botonEnviar.Enabled = true;
            campoEntrada.SetFocus();
        });
    }
}

static Uri NormalizarEndpoint(string apiUrl)
{
    string limpia = apiUrl.Trim().TrimEnd('/');

    if (limpia.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
    {
        limpia = limpia[..^"/chat/completions".Length];
    }

    return new Uri(limpia);
}

static string LeerArchivo(string ruta)
{
    try
    {
        string path = ResolverRutaProyecto(ruta);

        if (!File.Exists(path))
        {
            return $"No existe el archivo: {ruta}";
        }

        return File.ReadAllText(path);
    }
    catch (Exception ex)
    {
        return $"No se pudo leer el archivo: {ex.Message}";
    }
}

static string EscribirArchivo(string ruta, string contenido)
{
    try
    {
        string path = ResolverRutaProyecto(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contenido);
        return $"Archivo escrito correctamente: {ruta}";
    }
    catch (Exception ex)
    {
        return $"No se pudo escribir el archivo: {ex.Message}";
    }
}

static string ListarArchivos(string ruta)
{
    try
    {
        string path = ResolverRutaProyecto(string.IsNullOrWhiteSpace(ruta) ? "." : ruta);

        if (!Directory.Exists(path))
        {
            return $"No existe el directorio: {ruta}";
        }

        var lineas = Directory.EnumerateFileSystemEntries(path)
            .OrderBy(p => p)
            .Select(p => Directory.Exists(p)
                ? $"[carpeta] {Path.GetFileName(p)}"
                : $"[archivo] {Path.GetFileName(p)}");

        return string.Join(Environment.NewLine, lineas);
    }
    catch (Exception ex)
    {
        return $"No se pudo listar el directorio: {ex.Message}";
    }
}

static string ResolverRutaProyecto(string ruta)
{
    string raiz = Path.GetFullPath(Directory.GetCurrentDirectory());
    string combinada = Path.GetFullPath(Path.Combine(raiz, ruta));

    if (!combinada.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La ruta debe estar dentro de la carpeta del proyecto.");
    }

    return combinada;
}