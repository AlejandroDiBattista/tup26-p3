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
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string SystemPromptFileName = "AGENTS.md";

try
{
    DotNetEnv.Env.Load();

    var config = AssistantConfig.FromEnvironment(args);
    var systemPrompt = File.ReadAllText(SystemPromptFileName);
    var tools = ProjectFileTools.Create(Directory.GetCurrentDirectory());

    IChatClient chat = new OpenAIClient(
            new ApiKeyCredential(config.ApiKey),
            new OpenAIClientOptions { Endpoint = config.Endpoint })
        .GetChatClient(config.Model)
        .AsIChatClient();

    var mensajes = new List<ChatMessage>
    {
        new(ChatRole.System, systemPrompt)
    };

    using IApplication app = Application.Create().Init();
    using var ventana = new Window {
        Title = $" Asistente IA · {config.Model} ",
        Width = Dim.Fill(), Height = Dim.Fill()
    };

    ventana.Add(new Markdown {
        Text = $"# Asistente IA\n\nProveedor: `{config.Provider}`\n\nModelo: `{config.Model}`\n\nHerramientas disponibles: `{tools.Count}`\n\nLa interfaz de chat se implementa en los siguientes pasos.",
        Width = Dim.Fill(), Height = Dim.Fill()
    });

    app.Run(ventana);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"No se pudo iniciar el asistente: {ex.Message}");
    Console.Error.WriteLine("Revisa el archivo .env, el proveedor elegido y el archivo AGENTS.md.");
}

/// <summary>
/// Configuracion necesaria para crear un cliente compatible con OpenAI.
/// El proveedor se toma del primer argumento, por ejemplo OPENAI, GROQ u OLLAMA.
/// </summary>
internal sealed record AssistantConfig(string Provider, Uri Endpoint, string ApiKey, string Model)
{
    public static AssistantConfig FromEnvironment(string[] args)
    {
        var provider = (args.Length > 0 ? args[0] : "OPENAI").Trim().ToUpperInvariant();
        var rawUrl = Environment.GetEnvironmentVariable($"{provider}_API_URL");
        var apiKey = Environment.GetEnvironmentVariable($"{provider}_API_KEY");
        var model = Environment.GetEnvironmentVariable($"{provider}_MODEL");

        if (string.IsNullOrWhiteSpace(rawUrl))
            throw new InvalidOperationException($"Falta la variable {provider}_API_URL.");

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException($"Falta la variable {provider}_MODEL.");

        var endpoint = NormalizeEndpoint(rawUrl);
        var resolvedKey = string.IsNullOrWhiteSpace(apiKey) ? "no-requiere-key" : apiKey;
        return new AssistantConfig(provider, endpoint, resolvedKey, model);
    }

    private static Uri NormalizeEndpoint(string rawUrl)
    {
        var trimmed = rawUrl.Trim().TrimEnd('/');

        foreach (var suffix in new[] { "/chat/completions", "/completions" })
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^suffix.Length];
                break;
            }
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var endpoint))
            throw new InvalidOperationException($"La URL configurada no es valida: {rawUrl}");

        return endpoint;
    }
}

/// <summary>
/// Herramientas expuestas al modelo para operar sobre archivos del proyecto.
/// Cada funcion resuelve rutas dentro del directorio actual para evitar escrituras accidentales fuera del TP.
/// </summary>
internal sealed class ProjectFileTools
{
    private readonly string _rootDirectory;

    private ProjectFileTools(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public static IReadOnlyList<AITool> Create(string rootDirectory)
    {
        var tools = new ProjectFileTools(rootDirectory);
        return
        [
            AIFunctionFactory.Create(tools.ReadTextFile, "leer-archivo", "Devuelve el contenido de un archivo de texto del proyecto."),
            AIFunctionFactory.Create(tools.WriteTextFile, "escribir-archivo", "Crea o sobrescribe un archivo de texto del proyecto."),
            AIFunctionFactory.Create(tools.ListDirectory, "listar-archivos", "Lista archivos y carpetas de un directorio del proyecto.")
        ];
    }

    private string ReadTextFile([Description("Ruta relativa del archivo a leer.")] string ruta)
    {
        try
        {
            var fullPath = ResolveProjectPath(ruta);
            if (!File.Exists(fullPath))
                return $"No existe el archivo '{ruta}'.";

            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            return $"No pude leer '{ruta}': {ex.Message}";
        }
    }

    private string WriteTextFile(
        [Description("Ruta relativa del archivo a crear o sobrescribir.")] string ruta,
        [Description("Contenido completo que se escribira en el archivo.")] string contenido)
    {
        try
        {
            var fullPath = ResolveProjectPath(ruta);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, contenido);
            return $"Archivo '{ruta}' guardado correctamente.";
        }
        catch (Exception ex)
        {
            return $"No pude escribir '{ruta}': {ex.Message}";
        }
    }

    private string ListDirectory([Description("Ruta relativa del directorio a listar. Usar '.' para el directorio actual.")] string ruta)
    {
        try
        {
            var fullPath = ResolveProjectPath(string.IsNullOrWhiteSpace(ruta) ? "." : ruta);
            if (!Directory.Exists(fullPath))
                return $"No existe el directorio '{ruta}'.";

            var directories = Directory.GetDirectories(fullPath)
                .OrderBy(Path.GetFileName)
                .Select(path => $"[dir]  {Path.GetRelativePath(_rootDirectory, path)}/");

            var files = Directory.GetFiles(fullPath)
                .OrderBy(Path.GetFileName)
                .Select(path => $"[file] {Path.GetRelativePath(_rootDirectory, path)}");

            var entries = directories.Concat(files).ToArray();
            return entries.Length == 0
                ? $"El directorio '{ruta}' esta vacio."
                : string.Join(Environment.NewLine, entries);
        }
        catch (Exception ex)
        {
            return $"No pude listar '{ruta}': {ex.Message}";
        }
    }

    private string ResolveProjectPath(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            throw new ArgumentException("La ruta no puede estar vacia.");

        var candidate = Path.GetFullPath(Path.Combine(_rootDirectory, ruta));
        var rootWithSeparator = _rootDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? _rootDirectory
            : _rootDirectory + Path.DirectorySeparatorChar;

        if (candidate != _rootDirectory &&
            !candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La ruta debe quedar dentro del directorio del proyecto.");
        }

        return candidate;
    }
}
