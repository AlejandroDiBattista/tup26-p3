#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
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

    if (args.Contains("--check", StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine("Configuracion OK.");
        Console.WriteLine($"Proveedor: {config.Provider}");
        Console.WriteLine($"Endpoint: {config.Endpoint}");
        Console.WriteLine($"Modelo: {config.Model}");
        Console.WriteLine($"Prompt de sistema: {SystemPromptFileName} ({systemPrompt.Length} caracteres)");
        Console.WriteLine($"Herramientas: {tools.Count}");
        return;
    }

    IChatClient chat = new OpenAIClient(
            new ApiKeyCredential(config.ApiKey),
            new OpenAIClientOptions { Endpoint = config.Endpoint })
        .GetChatClient(config.Model)
        .AsIChatClient();

    var chatSession = new ChatSession(
        chat.AsBuilder().UseFunctionInvocation().Build(),
        systemPrompt,
        tools);

    using IApplication app = Application.Create().Init();
    using var ventana = new AssistantWindow(app, chatSession, config);
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
        var providerArgument = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
        var provider = (providerArgument ?? "OPENAI").Trim().ToUpperInvariant();
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

/// <summary>
/// Mantiene la conversacion completa y envia cada consulta con streaming.
/// Si la llamada falla, retira del historial el mensaje de usuario que no tuvo respuesta.
/// </summary>
internal sealed class ChatSession
{
    private readonly IChatClient _chat;
    private readonly List<ChatMessage> _messages;
    private readonly ChatOptions _options;

    public ChatSession(IChatClient chat, string systemPrompt, IReadOnlyList<AITool> tools)
    {
        _chat = chat;
        _messages = [new ChatMessage(ChatRole.System, systemPrompt)];
        _options = new ChatOptions
        {
            Tools = [.. tools]
        };
    }

    public int MessageCount => _messages.Count;

    public async Task<string> SendAsync(
        string userMessage,
        Action<string> onDelta,
        CancellationToken cancellationToken = default)
    {
        var cleanMessage = userMessage.Trim();
        if (cleanMessage.Length == 0)
            return string.Empty;

        _messages.Add(new ChatMessage(ChatRole.User, cleanMessage));
        var response = new StringBuilder();

        try
        {
            await foreach (var update in _chat.GetStreamingResponseAsync(_messages, _options, cancellationToken))
            {
                if (string.IsNullOrEmpty(update.Text))
                    continue;

                response.Append(update.Text);
                onDelta(update.Text);
            }

            var assistantMessage = response.ToString().TrimEnd();
            if (assistantMessage.Length == 0)
                assistantMessage = "El modelo no devolvio contenido de texto.";

            _messages.Add(new ChatMessage(ChatRole.Assistant, assistantMessage));
            return assistantMessage;
        }
        catch
        {
            if (_messages.Count > 0 && _messages[^1].Role == ChatRole.User)
                _messages.RemoveAt(_messages.Count - 1);

            throw;
        }
    }
}

/// <summary>
/// Ventana principal de la TUI: historial Markdown arriba y controles de entrada abajo.
/// La clase solo coordina eventos de interfaz; el historial real queda en ChatSession.
/// </summary>
internal sealed class AssistantWindow : Window
{
    private readonly IApplication _app;
    private readonly ChatSession _chatSession;
    private readonly Markdown _conversation;
    private readonly TextField _input;
    private readonly Button _sendButton;
    private readonly Label _status;
    private readonly StringBuilder _markdown = new();
    private int _currentAssistantStart = -1;
    private bool _isSending;
    private bool _autoScroll = true;

    public AssistantWindow(IApplication app, ChatSession chatSession, AssistantConfig config)
    {
        _app = app;
        _chatSession = chatSession;

        Title = $" Asistente IA · {config.Provider} · {config.Model} ";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _conversation = new Markdown
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
            CanFocus = true
        };

        var inputPanel = new FrameView
        {
            Title = "Mensaje",
            X = 0,
            Y = Pos.Bottom(_conversation),
            Width = Dim.Fill(),
            Height = 4
        };

        _input = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(14),
            Height = 1,
            CanFocus = true
        };

        _sendButton = new Button
        {
            Text = "Enviar",
            X = Pos.Right(_input) + 1,
            Y = 1,
            Width = 10,
            IsDefault = true
        };

        _status = new Label
        {
            Text = "Listo",
            X = 1,
            Y = 2,
            Width = Dim.Fill(),
            Height = 1
        };

        inputPanel.Add(_input, _sendButton, _status);
        Add(_conversation, inputPanel);
        AppendTurn("Asistente", "Hola. Soy tu asistente de programacion. Escribi un mensaje y presiona Enter.");
        WireEvents();
    }

    private void WireEvents()
    {
        _sendButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            _ = SendCurrentMessageAsync();
        };

        _input.KeyDown += (_, args) =>
        {
            if (args.KeyCode == KeyCode.Enter)
            {
                args.Handled = true;
                _ = SendCurrentMessageAsync();
            }
        };

        _conversation.KeyDown += (_, _) => _autoScroll = false;
        _conversation.MouseEvent += (_, _) => _autoScroll = false;

        KeyDown += (_, args) =>
        {
            if (args.KeyCode == KeyCode.Esc)
            {
                args.Handled = true;
                _app.RequestStop();
            }
        };
    }

    private async Task SendCurrentMessageAsync()
    {
        if (_isSending)
            return;

        var userText = _input.Text?.ToString()?.Trim() ?? string.Empty;
        if (userText.Length == 0)
        {
            _status.Text = "Escribi un mensaje antes de enviar.";
            return;
        }

        SetSending(true);
        _autoScroll = true;
        _input.Text = string.Empty;
        AppendTurn("Vos", userText);
        BeginAssistantTurn();

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var finalResponse = await _chatSession.SendAsync(userText, delta =>
            {
                _app.Invoke(() => AppendAssistantDelta(delta));
            }, timeout.Token);

            _app.Invoke(() =>
            {
                ReplaceCurrentAssistantText(finalResponse);
                _status.Text = "Listo";
            });
        }
        catch (OperationCanceledException)
        {
            _app.Invoke(() =>
            {
                ReplaceCurrentAssistantText("La respuesta supero el tiempo de espera. Revisa la conexion o el proveedor configurado.");
                _status.Text = "Tiempo agotado";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                ReplaceCurrentAssistantText($"No pude completar la respuesta: {ex.Message}");
                _status.Text = "Error";
            });
        }
        finally
        {
            _app.Invoke(() => SetSending(false));
        }
    }

    private void SetSending(bool sending)
    {
        _isSending = sending;
        _input.Enabled = !sending;
        _sendButton.Enabled = !sending;
        _status.Text = sending ? "El asistente esta respondiendo..." : _status.Text;

        if (!sending)
            _input.SetFocus();

        SetNeedsDraw();
    }

    private void AppendTurn(string role, string content)
    {
        _markdown.Append("## ").Append(role).AppendLine();
        _markdown.AppendLine();
        _markdown.AppendLine(content.TrimEnd());
        _markdown.AppendLine();
        RenderConversation();
    }

    private void BeginAssistantTurn()
    {
        _markdown.Append("## Asistente").AppendLine();
        _markdown.AppendLine();
        _currentAssistantStart = _markdown.Length;
        _markdown.Append("_Pensando..._").AppendLine();
        _markdown.AppendLine();
        RenderConversation();
    }

    private void AppendAssistantDelta(string delta)
    {
        if (_currentAssistantStart < 0)
            return;

        var current = _markdown.ToString(_currentAssistantStart, _markdown.Length - _currentAssistantStart);
        if (current.StartsWith("_Pensando..._", StringComparison.Ordinal))
        {
            _markdown.Remove(_currentAssistantStart, current.Length);
        }

        _markdown.Append(delta);
        RenderConversation();
    }

    private void ReplaceCurrentAssistantText(string text)
    {
        if (_currentAssistantStart < 0)
            return;

        _markdown.Remove(_currentAssistantStart, _markdown.Length - _currentAssistantStart);
        _markdown.AppendLine(text.TrimEnd());
        _markdown.AppendLine();
        _currentAssistantStart = -1;
        RenderConversation();
    }

    private void RenderConversation()
    {
        _conversation.Text = _markdown.ToString();

        if (_autoScroll)
        {
            _conversation.ScrollVertical(int.MaxValue);
            _conversation.SetNeedsDraw();
        }

        SetNeedsDraw();
    }
}
