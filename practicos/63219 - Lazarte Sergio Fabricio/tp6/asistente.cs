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

var directorioBase = BuscarDirectorioRaiz(Directory.GetCurrentDirectory());
DotNetEnv.Env.Load(Path.Combine(directorioBase, ".env"));

ConfiguracionServicio config;
try
{
    config = InicializarConfiguracion(args);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return;
}

var nombreProveedor = config.Proveedor;
var endpointUrl = config.Url;
var claveApi = config.ApiKey;
var nombreModelo = config.Modelo;

IChatClient clienteChat = new OpenAIClient(
        new ApiKeyCredential(claveApi ?? "sin-clave"),
        new OpenAIClientOptions { Endpoint = PrepararEndpoint(endpointUrl) })
    .GetChatClient(nombreModelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var capacidades = new List<AITool>
{
    AIFunctionFactory.Create(
        (Func<string, string>)ObtenerContenidoArchivo,
        "leer-archivo",
        "Devuelve el contenido de un archivo de texto del proyecto."),
    AIFunctionFactory.Create(
        (Func<string, string, string>)GuardarContenidoArchivo,
        "escribir-archivo",
        "Crea o sobrescribe un archivo de texto del proyecto."),
    AIFunctionFactory.Create(
        (Func<string, string>)ObtenerListadoDirectorio,
        "listar-archivos",
        "Lista los archivos y carpetas de un directorio del proyecto.")
};

var parametrosChat = new ChatOptions
{
    Tools = capacidades,
    ToolMode = ChatToolMode.Auto
};

var historial = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText(Path.Combine(directorioBase, "AGENTS.md")))
};

var intercambios = new List<InteraccionUI>();

string BuscarDirectorioRaiz(string inicio)
{
    if (File.Exists(Path.Combine(inicio, "AGENTS.md"))) return inicio;
    var subcarpeta = Path.Combine(inicio, "tp6");
    if (File.Exists(Path.Combine(subcarpeta, "AGENTS.md"))) return subcarpeta;
    return inicio;
}

string ValidarYResolverRuta(string rutaRelativa)
{
    if (string.IsNullOrWhiteSpace(rutaRelativa)) rutaRelativa = ".";
    var rutaAbsoluta = Path.GetFullPath(Path.Combine(directorioBase, rutaRelativa));
    if (!rutaAbsoluta.StartsWith(directorioBase, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("La ruta debe permanecer dentro de la carpeta del proyecto.");
    return rutaAbsoluta;
}

string ObtenerContenidoArchivo([Description("Ruta relativa del archivo a leer.")] string ruta)
{
    var ubicacion = ValidarYResolverRuta(ruta);
    return File.Exists(ubicacion) ? File.ReadAllText(ubicacion) : $"Archivo no encontrado: {ruta}";
}

string GuardarContenidoArchivo(
    [Description("Ruta relativa del archivo a crear o sobrescribir.")] string ruta,
    [Description("Contenido que se guardara en el archivo.")] string contenido)
{
    var ubicacion = ValidarYResolverRuta(ruta);
    Directory.CreateDirectory(Path.GetDirectoryName(ubicacion)!);
    File.WriteAllText(ubicacion, contenido);
    return $"Guardado correctamente: {ruta}";
}

string ObtenerListadoDirectorio([Description("Ruta relativa del directorio a listar.")] string ruta)
{
    var ubicacion = ValidarYResolverRuta(ruta);
    if (!Directory.Exists(ubicacion)) return $"Directorio no encontrado: {ruta}";
    return string.Join(Environment.NewLine, Directory
        .EnumerateFileSystemEntries(ubicacion)
        .Select(Path.GetFileName)
        .OrderBy(n => n));
}

bool EsValorValido(string? valor)
{
    if (string.IsNullOrWhiteSpace(valor)) return false;
    var v = valor.Trim();
    return !(v.StartsWith('<') && v.EndsWith('>'))
        && !v.Contains("tu_clave_api_aqui", StringComparison.OrdinalIgnoreCase);
}

string LeerVariableObligatoria(string clave)
{
    var valor = Environment.GetEnvironmentVariable(clave);
    if (!EsValorValido(valor))
        throw new InvalidOperationException($"Falta configurar {clave} en .env.");
    return valor!;
}

string ResolverProveedorPredeterminado()
{
    var lista = new[] { "OPENAI", "GROQ", "GEMINI", "OPENROUTER", "FIREWORK", "GROK", "HHGG", "OLLAMA" };
    return lista.FirstOrDefault(p => EsValorValido(Environment.GetEnvironmentVariable($"{p}_API_KEY")))
        ?? "OPENAI";
}

ConfiguracionServicio InicializarConfiguracion(string[] argumentos)
{
    var proveedor = (argumentos.Length > 0
            ? argumentos[0]
            : Environment.GetEnvironmentVariable("ASISTENTE_PROVIDER") ?? ResolverProveedorPredeterminado())
        .Trim().ToUpperInvariant();

    var urlBase = LeerVariableObligatoria($"{proveedor}_API_URL");
    var clave = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
    var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-4o-mini";

    if (proveedor == "GROQ")
        clave = string.IsNullOrWhiteSpace(clave) ? "groq" : clave;
    else if (!EsValorValido(clave))
        throw new InvalidOperationException(
            $"Falta configurar {proveedor}_API_KEY en .env. " +
            "Pegá la clave sin los signos < >, por ejemplo: GROQ_API_KEY=gsk_...");

    return new ConfiguracionServicio(proveedor, urlBase, clave!, modelo);
}

Uri PrepararEndpoint(string endpoint)
{
    var url = endpoint.TrimEnd('/');
    if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        url = url[..^"/chat/completions".Length];
    return new Uri(url);
}

record InteraccionUI(string Autor, string Texto);
record ConfiguracionServicio(string Proveedor, string Url, string ApiKey, string Modelo);