#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Microsoft.Extensions.AI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

ConfiguracionIA configuracion;
string promptSistema;
IChatClient chat;
var opcionesChat = CrearOpcionesChat();
bool respondiendo = false;

try
{
    configuracion = CargarConfiguracion(args);
    promptSistema = CargarPromptSistema();
    chat = CrearCliente(configuracion);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error de configuracion: {ex.Message}");
    Console.Error.WriteLine("Verifica tu archivo .env y las variables de entorno.");
    return;
}

List<ChatMessage> mensajes = [
    new(ChatRole.System, promptSistema)
];

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA - {configuracion.Modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var panelConversacion = new FrameView {
    Title = " Conversacion ",
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill(6)
};

var conversacion = new Markdown {
    Text = RenderizarConversacion(mensajes),
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill()
};

var panelEntrada = new FrameView {
    Title = " Nuevo mensaje ",
    X = 0, Y = Pos.AnchorEnd(6),
    Width = Dim.Fill(), Height = 6
};

var entrada = new TextField {
    X = 1, Y = 0,
    Width = Dim.Fill(15)
};

var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.AnchorEnd(12), Y = 0,
    Width = 11,
    IsDefault = true
};

var ayuda = new Label {
    Text = "Enter: enviar | Esc: salir | La respuesta aparecera en el panel superior",
    X = 1, Y = 2,
    Width = Dim.Fill(2), Height = 1
};

botonEnviar.Accepted += async (_, _) => await EnviarMensajeAsync();

entrada.KeyDown += async (_, args) =>
{
    if (args.KeyCode == KeyCode.Enter && !respondiendo)
    {
        await EnviarMensajeAsync();
        args.Handled = true;
    }
};

ventana.KeyDown += (_, args) =>
{
    if (args.KeyCode == KeyCode.Esc)
    {
        app.RequestStop();
        args.Handled = true;
    }
};

panelConversacion.Add(conversacion);
panelEntrada.Add(entrada, botonEnviar, ayuda);
ventana.Add(panelConversacion, panelEntrada);

app.Run(ventana);

async Task EnviarMensajeAsync()
{
    if (respondiendo)
    {
        return;
    }

    var textoUsuario = entrada.Text?.ToString()?.Trim();

    if (string.IsNullOrWhiteSpace(textoUsuario))
    {
        return;
    }

    respondiendo = true;
    entrada.Text = string.Empty;
    entrada.Enabled = false;
    botonEnviar.Enabled = false;
    ayuda.Text = "El asistente esta respondiendo...";

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
    conversacion.Text = RenderizarConversacion(mensajes, "Pensando...");

    var respuesta = new StringBuilder();

    try
    {
        await foreach (var actualizacion in chat.GetStreamingResponseAsync(mensajes, opcionesChat))
        {
            respuesta.Append(actualizacion.Text);
            conversacion.Text = RenderizarConversacion(mensajes, "Asistente\n\n" + respuesta);
        }

        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuesta.ToString()));
        conversacion.Text = RenderizarConversacion(mensajes);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[DEBUG] Excepcion completa: {ex}");

        var errorDetalle = ex switch
        {
            InvalidOperationException => $"Error de configuracion: {ex.Message}",
            HttpRequestException => "No se pudo conectar con la API. Verifica tu conexion y la URL.",
            TaskCanceledException => "La consulta tardo demasiado. Intenta de nuevo.",
            _ => $"Error inesperado: {ex.Message}"
        };

        mensajes.RemoveAt(mensajes.Count - 1);
        conversacion.Text = RenderizarConversacion(mensajes, $"[ERROR] {errorDetalle}");
        ayuda.Text = "Ocurrio un error. Intenta de nuevo.";
    }
    finally
    {
        entrada.Enabled = true;
        botonEnviar.Enabled = true;
        respondiendo = false;
    }
}

static string RenderizarConversacion(IEnumerable<ChatMessage> mensajes, string? estado = null)
{
    var markdown = new StringBuilder();
    var hayMensajesVisibles = false;

    foreach (var mensaje in mensajes)
    {
        if (mensaje.Role == ChatRole.System)
        {
            continue;
        }

        hayMensajesVisibles = true;
        var titulo = mensaje.Role == ChatRole.User ? "Vos" : "Asistente";
        markdown.AppendLine($"# {titulo}");
        markdown.AppendLine();
        markdown.AppendLine(mensaje.Text);
        markdown.AppendLine();
    }

    if (!hayMensajesVisibles)
    {
        markdown.AppendLine("# Asistente IA");
        markdown.AppendLine();
        markdown.AppendLine("Bienvenido. Escribi una consulta en el panel inferior para iniciar la conversacion.");
        markdown.AppendLine();
        markdown.AppendLine("## Estado");
        markdown.AppendLine();
        markdown.AppendLine("Esperando tu primer mensaje...");
    }

    if (!string.IsNullOrWhiteSpace(estado))
    {
        markdown.AppendLine();
        markdown.AppendLine($"# {estado}");
    }

    return markdown.ToString();
}

static ConfiguracionIA CargarConfiguracion(string[] args)
{
    var proveedor = (args.Length > 0 ? args[0] : "gemini").ToUpperInvariant();
    var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
    var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "no-requiere-key";
    var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL");

    if (string.IsNullOrWhiteSpace(url))
    {
        throw new InvalidOperationException($"Falta configurar la variable {proveedor}_API_URL.");
    }

    if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint))
    {
        throw new InvalidOperationException($"La variable {proveedor}_API_URL no contiene una URL valida.");
    }

    if (string.IsNullOrWhiteSpace(modelo))
    {
        throw new InvalidOperationException($"Falta configurar la variable {proveedor}_MODEL.");
    }

    return new ConfiguracionIA(proveedor, endpoint, apiKey, modelo);
}

static string CargarPromptSistema()
{
    const string rutaPrompt = "AGENTS.md";

    if (!File.Exists(rutaPrompt))
    {
        throw new FileNotFoundException("No se encontro el archivo AGENTS.md junto a la aplicacion.", rutaPrompt);
    }

    return File.ReadAllText(rutaPrompt);
}

static IChatClient CrearCliente(ConfiguracionIA configuracion)
{
    var clienteBase = new OpenAIClient(
            new ApiKeyCredential(configuracion.ApiKey),
            new OpenAIClientOptions { Endpoint = configuracion.Endpoint })
        .GetChatClient(configuracion.Modelo)
        .AsIChatClient();

    return clienteBase
        .AsBuilder()
        .UseFunctionInvocation()
        .Build();
}

static ChatOptions CrearOpcionesChat()
{
    return new ChatOptions {
        ToolMode = ChatToolMode.Auto,
        Tools = [
            AIFunctionFactory.Create((string ruta) => LeerArchivo(ruta), "leer-archivo", "Devuelve el contenido de un archivo de texto."),
            AIFunctionFactory.Create((string ruta, string contenido) => EscribirArchivo(ruta, contenido), "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado."),
            AIFunctionFactory.Create((string ruta) => ListarArchivos(ruta), "listar-archivos", "Lista los archivos y carpetas de un directorio.")
        ]
    };
}

static string LeerArchivo(string ruta)
{
    try
    {
        var rutaCompleta = ResolverRutaProyecto(ruta);

        if (!File.Exists(rutaCompleta))
        {
            return $"No se encontro el archivo: {ruta}";
        }

        return File.ReadAllText(rutaCompleta);
    }
    catch (Exception ex)
    {
        return $"Error al leer el archivo '{ruta}': {ex.Message}";
    }
}

static string EscribirArchivo(string ruta, string contenido)
{
    try
    {
        var rutaCompleta = ResolverRutaProyecto(ruta);
        var directorio = Path.GetDirectoryName(rutaCompleta);

        if (!string.IsNullOrWhiteSpace(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        File.WriteAllText(rutaCompleta, contenido);
        return $"Archivo escrito correctamente: {ruta}";
    }
    catch (Exception ex)
    {
        return $"Error al escribir el archivo '{ruta}': {ex.Message}";
    }
}

static string ListarArchivos(string ruta)
{
    try
    {
        var rutaCompleta = ResolverRutaProyecto(ruta);

        if (!Directory.Exists(rutaCompleta))
        {
            return $"No se encontro el directorio: {ruta}";
        }

        var entradas = Directory.EnumerateFileSystemEntries(rutaCompleta)
            .Select(entrada => Directory.Exists(entrada)
                ? $"[DIR] {Path.GetFileName(entrada)}"
                : $"[ARCHIVO] {Path.GetFileName(entrada)}");

        return string.Join(Environment.NewLine, entradas);
    }
    catch (Exception ex)
    {
        return $"Error al listar el directorio '{ruta}': {ex.Message}";
    }
}

static string ResolverRutaProyecto(string ruta)
{
    var baseProyecto = Directory.GetCurrentDirectory();
    var rutaCombinada = Path.IsPathRooted(ruta)
        ? ruta
        : Path.Combine(baseProyecto, ruta);

    var rutaCompleta = Path.GetFullPath(rutaCombinada);

    if (!rutaCompleta.StartsWith(baseProyecto, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La ruta debe estar dentro del directorio del proyecto.");
    }

    return rutaCompleta;
}

record ConfiguracionIA(string Proveedor, Uri Endpoint, string ApiKey, string Modelo);
