using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;

DotEnv.Load(Path.Combine(AppContext.BaseDirectory, ".env"));
DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var baseUrl = Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL");
if (string.IsNullOrWhiteSpace(baseUrl))
{
    baseUrl = "https://openrouter.ai/api/v1";
}

var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Falta la variable de entorno OPENROUTER_API_KEY. Podes crear un archivo .env basado en .env.example.");
    return 1;
}

var model = Environment.GetEnvironmentVariable("OPENROUTER_MODEL");
if (string.IsNullOrWhiteSpace(model))
{
    model = "openai/gpt-4o-mini";
}

var appDirectory = AppContext.BaseDirectory;
var systemPromptPath = Path.Combine(appDirectory, "AGENTS.md");
var enableToolsSetting = Environment.GetEnvironmentVariable("AI_ENABLE_TOOLS");
var enableTools = !bool.TryParse(enableToolsSetting, out var parsedEnableTools) || parsedEnableTools;

if (args.Contains("--check", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("Configuracion OK.");
    Console.WriteLine("Proveedor: OpenRouter");
    Console.WriteLine($"Modelo: {model}");
    Console.WriteLine($"AGENTS.md: {systemPromptPath}");
    Console.WriteLine($"OPENROUTER_API_KEY: configurada ({apiKey.Length} caracteres)");
    Console.WriteLine($"OPENROUTER_BASE_URL: {baseUrl}");
    Console.WriteLine($"Herramientas/function calling: {(enableTools ? "activadas" : "desactivadas")}");
    return 0;
}

if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var endpoint))
{
    Console.Error.WriteLine($"OPENROUTER_BASE_URL no es una URL valida: {baseUrl}");
    return 1;
}

var options = new OpenAIClientOptions
{
    Endpoint = endpoint
};

var rawClient = new OpenAIClient(new ApiKeyCredential(apiKey), options)
    .GetChatClient(model)
    .AsIChatClient();

var chatClient = rawClient.AsBuilder()
    .UseFunctionInvocation()
    .Build();

var systemPrompt = await SystemPromptLoader.LoadAsync(systemPromptPath);
var fileTools = new FileTools(Directory.GetCurrentDirectory());
var chatService = new ChatService(
    chatClient,
    systemPrompt,
    enableTools ? fileTools.CreateTools() : []);

var terminalDriver = Environment.GetEnvironmentVariable("TERMINAL_GUI_DRIVER");

Console.WriteLine($"Iniciando AsistenteIA con Terminal.Gui driver='{(string.IsNullOrWhiteSpace(terminalDriver) ? "default" : terminalDriver)}'...");
Console.WriteLine("Si la pantalla queda en blanco, cerrala con Esc o Ctrl+C y ejecuta: dotnet run -- --check");

try
{
    using IApplication app = Application.Create();
    if (string.IsNullOrWhiteSpace(terminalDriver))
    {
        app.Init();
    }
    else
    {
        app.Init(terminalDriver);
    }

    var mainWindow = new MainWindow(app, chatService, model);
    app.Run(mainWindow);
    mainWindow.Dispose();
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("No se pudo iniciar la interfaz TUI.");
    Console.Error.WriteLine(ex);
    return 1;
}
finally
{
}

return 0;
