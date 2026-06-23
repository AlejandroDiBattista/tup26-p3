#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using System.Text;
using System.Drawing;
using System.ComponentModel;

var raiz = EncontrarRaizAplicacion(Directory.GetCurrentDirectory());
DotNetEnv.Env.Load(Path.Combine(raiz, ".env"));

ConfiguracionApi configuracion;
try
{
    configuracion = CargarConfiguracion(args);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return;
}

var proveedor = configuracion.Proveedor;
var url = configuracion.Url;
var apiKey = configuracion.ApiKey;
var modelo = configuracion.Modelo;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = NormalizarEndpoint(url) })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var herramientas = new List<AITool>
{
    AIFunctionFactory.Create(
        (Func<string, string>)LeerArchivo,
        "leer-archivo",
        "Devuelve el contenido de un archivo de texto del proyecto."),
    AIFunctionFactory.Create(
        (Func<string, string, string>)EscribirArchivo,
        "escribir-archivo",
        "Crea o sobrescribe un archivo de texto del proyecto."),
    AIFunctionFactory.Create(
        (Func<string, string>)ListarArchivos,
        "listar-archivos",
        "Lista los archivos y carpetas de un directorio del proyecto.")
};

var opciones = new ChatOptions
{
    Tools = herramientas,
    ToolMode = ChatToolMode.Auto
};

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText(Path.Combine(raiz, "AGENTS.md")))
};

var turnos = new List<TurnoPantalla>();

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var conversacion = new Markdown
{
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    CanFocus = true,
    Text = "# Asistente IA\n\nEscribi un mensaje y presiona Enter."
};
conversacion.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

var entrada = new TextField
{
    X = 0,
    Y = Pos.Bottom(conversacion),
    Width = Dim.Fill(12),
    Height = 1
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.Top(entrada),
    Width = 10,
    IsDefault = true
}; 

ventana.Add(conversacion, entrada, enviar);
bool ocupado = false;
entrada.Accepted += (_, _) => _ = EnviarMensajeAsync();
enviar.Accepted += (_, _) => _ = EnviarMensajeAsync();
entrada.SetFocus();

async Task EnviarMensajeAsync()
{
    if (ocupado) return;

    var texto = entrada.Text?.ToString()?.Trim();
    if (string.IsNullOrWhiteSpace(texto)) return;

    ocupado = true;
    entrada.Text = "";
    entrada.Enabled = false;
    enviar.Enabled = false;

    mensajes.Add(new ChatMessage(ChatRole.User, texto));
    turnos.Add(new TurnoPantalla("Vos", texto));
    turnos.Add(new TurnoPantalla("Asistente", ""));
    RefrescarConversacion();

    try
    {
        var respuesta = new StringBuilder();
        var actualizaciones = new List<ChatResponseUpdate>();

        await foreach (var parte in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
            actualizaciones.Add(parte);
            if (!string.IsNullOrEmpty(parte.Text))
            {
                respuesta.Append(parte.Text);
                turnos[^1] = turnos[^1] with { Texto = respuesta.ToString() };
                app.Invoke(RefrescarConversacion);
            }
        }
        var respuestaCompleta = actualizaciones.ToChatResponse();
        mensajes.AddMessages(respuestaCompleta);
        turnos[^1] = turnos[^1] with { Texto = respuestaCompleta.Text };
    }
    catch (Exception ex)
    {
        var error = $"No pude obtener respuesta: {ex.Message}";
        mensajes.Add(new ChatMessage(ChatRole.Assistant, error));
        turnos[^1] = turnos[^1] with { Texto = error };
    }
    finally
    {
        app.Invoke(() =>
        {
            ocupado = false;
            entrada.Enabled = true;
            enviar.Enabled = true;
            RefrescarConversacion();
            entrada.SetFocus();
        });
    }
}

string RenderizarTurnos()
{
    if (turnos.Count == 0)
        return "# Asistente IA\n\nEscribi un mensaje y presiona Enter.";

    var md = new StringBuilder();
    foreach (var turno in turnos)
    {
        md.AppendLine($"# {turno.Autor}");
        md.AppendLine();
        md.AppendLine(string.IsNullOrWhiteSpace(turno.Texto) ? "_Pensando..._" : turno.Texto);
        md.AppendLine();
    }
    return md.ToString();
}   

void RefrescarConversacion()
{
    conversacion.Text = RenderizarTurnos();
    conversacion.SetContentSize(new Size(conversacion.Viewport.Width, conversacion.LineCount));
    conversacion.ScrollVertical(conversacion.LineCount);
    conversacion.SetNeedsDraw();
}

string ResolverRuta(string ruta)
{
    if (string.IsNullOrWhiteSpace(ruta)) ruta = ".";
    var completa = Path.GetFullPath(Path.Combine(raiz, ruta));
    if (!completa.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("La ruta debe estar dentro de la carpeta del proyecto.");
    return completa;
}

string LeerArchivo([Description("Ruta relativa del archivo a leer.")] string ruta)
{
    var archivo = ResolverRuta(ruta);
    return File.Exists(archivo) ? File.ReadAllText(archivo) : $"No existe el archivo: {ruta}";
}

string EscribirArchivo(
    [Description("Ruta relativa del archivo a crear o sobrescribir.")] string ruta,
    [Description("Contenido que se guardara en el archivo.")] string contenido)
{
    var archivo = ResolverRuta(ruta);
    Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
    File.WriteAllText(archivo, contenido);
    return $"Archivo guardado: {ruta}";
}

string ListarArchivos([Description("Ruta relativa del directorio a listar.")] string ruta)
{
    var directorio = ResolverRuta(ruta);
    if (!Directory.Exists(directorio)) return $"No existe el directorio: {ruta}";
    return string.Join(Environment.NewLine, Directory
        .EnumerateFileSystemEntries(directorio)
        .Select(Path.GetFileName)
        .OrderBy(nombre => nombre));
}

string EncontrarRaizAplicacion(string directorioActual)
{
    if (File.Exists(Path.Combine(directorioActual, "AGENTS.md"))) return directorioActual;
    var subcarpetaTp6 = Path.Combine(directorioActual, "tp6");
    if (File.Exists(Path.Combine(subcarpetaTp6, "AGENTS.md"))) return subcarpetaTp6;
    return directorioActual;
}

ConfiguracionApi CargarConfiguracion(string[] argumentos)
{
    var proveedorElegido = (argumentos.Length > 0
            ? argumentos[0]
            : Environment.GetEnvironmentVariable("ASISTENTE_PROVIDER") ?? DetectarProveedorDisponible())
        .Trim().ToUpperInvariant();

    var apiUrl = ObtenerVariableRequerida($"{proveedorElegido}_API_URL");
    var apiKeyConfigurada = Environment.GetEnvironmentVariable($"{proveedorElegido}_API_KEY");
    var modeloElegido = Environment.GetEnvironmentVariable($"{proveedorElegido}_MODEL") ?? "gpt-4o-mini";

    if (proveedorElegido == "OLLAMA")
        apiKeyConfigurada = string.IsNullOrWhiteSpace(apiKeyConfigurada) ? "ollama" : apiKeyConfigurada;
    else if (!TieneValorReal(apiKeyConfigurada))
        throw new InvalidOperationException(
            $"Falta configurar {proveedorElegido}_API_KEY en .env. " +
            "Pegala sin signos < >, por ejemplo: GROQ_API_KEY=gsk_...");

    return new ConfiguracionApi(proveedorElegido, apiUrl, apiKeyConfigurada!, modeloElegido);
}

string DetectarProveedorDisponible()
{
    var proveedores = new[] { "OPENAI", "GROQ", "GEMINI", "OPENROUTER", "FIREWORK", "GROK", "HHGG", "OLLAMA" };
    return proveedores.FirstOrDefault(p => TieneValorReal(Environment.GetEnvironmentVariable($"{p}_API_KEY")))
        ?? "OPENAI";
}

string ObtenerVariableRequerida(string nombre)
{
    var valor = Environment.GetEnvironmentVariable(nombre);
    if (!TieneValorReal(valor))
        throw new InvalidOperationException($"Falta configurar {nombre} en .env.");
    return valor!;
}

bool TieneValorReal(string? valor)
{
    if (string.IsNullOrWhiteSpace(valor)) return false;
    var limpio = valor.Trim();
    return !(limpio.StartsWith('<') && limpio.EndsWith('>'))
        && !limpio.Contains("tu_clave_api_aqui", StringComparison.OrdinalIgnoreCase);
}

Uri NormalizarEndpoint(string endpoint)
{
    var limpio = endpoint.TrimEnd('/');
    if (limpio.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        limpio = limpio[..^"/chat/completions".Length];
    return new Uri(limpio);
} 


app.Run(ventana);
record TurnoPantalla(string Autor, string Texto);
record ConfiguracionApi(string Proveedor, string Url, string ApiKey, string Modelo);