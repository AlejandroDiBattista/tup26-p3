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
        Text = $"# Asistente IA\n\nProveedor: `{config.Provider}`\n\nModelo: `{config.Model}`\n\nLa interfaz de chat se implementa en los siguientes pasos.",
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
