using AsistenteIA.Services;
using AsistenteIA.Tools;
using AsistenteIA.UI;
using Microsoft.Extensions.AI;
using Terminal.Gui.App;

DotNetEnv.Env.Load();

try
{
    var provider = args.Length > 0 ? args[0] : "GROQ";
    var config = ChatConfiguration.FromEnvironment(provider);

    var baseClient = ChatClientFactory.Create(config);
    var chatClient = new FunctionInvokingChatClient(baseClient);
    var chatOptions = new ChatOptions
    {
        Tools = FileSystemTools.Create()
    };

    var systemPrompt = SystemPromptLoader.Load("AGENTS.md");
    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, systemPrompt)
    };

    Application.Init();

    var window = new MainWindow(
        title: $" AsistenteIA - {config.Model} ",
        chatClient,
        chatOptions,
        messages);

    Application.Run(window);
    Application.Shutdown();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error de inicio: {ex.Message}");
    Console.Error.WriteLine("Revisa el archivo .env y el archivo AGENTS.md.");
}
