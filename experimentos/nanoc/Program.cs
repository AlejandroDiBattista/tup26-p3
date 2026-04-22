using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

var configuration = NanoConfiguration.Load();
var promptLoader = new PromptLoader(configuration);
var tools = new WorkspaceTools(configuration.Workspace);

if (string.IsNullOrWhiteSpace(configuration.OpenAIApiKey)) {
    Console.Error.WriteLine("Falta OPENAI_API_KEY.");
    Console.Error.WriteLine("Ejemplo:");
    Console.Error.WriteLine("  export OPENAI_API_KEY=\"sk-...\"");
    Console.Error.WriteLine("  export OPENAI_MODEL=\"gpt-4.1-mini\"");
    Console.Error.WriteLine("  dotnet run");
    return 1;
}

var instructions = promptLoader.LoadInstructions();
var responsesClient = new ResponsesClient(configuration.OpenAIApiKey);

var agent = responsesClient.AsAIAgent(
    configuration.Model,
    instructions,
    "NanoC",
    "Agente de consola para programacion asistida con acceso a archivos y shell.",
    [
        AIFunctionFactory.Create(tools.ReadFile),
        AIFunctionFactory.Create(tools.WriteFile),
        AIFunctionFactory.Create(tools.RunShell)
    ]);

var session = await agent.CreateSessionAsync();

PrintBanner(configuration);

while (true) {
    Console.Write("Tu> ");
    var rawInput = Console.ReadLine();

    if (rawInput is null) {
        Console.WriteLine();
        break;
    }

    var input = rawInput.Trim();

    if (string.IsNullOrWhiteSpace(input)) {
        continue;
    }

    if (IsExitCommand(input)) {
        break;
    }

    if (await TryHandleLocalCommandAsync(input, agent, configuration, newSession => session = newSession)) {
        continue;
    }

    try {
        var response = await agent.RunAsync(
            new ChatMessage(ChatRole.User, input),
            session);

        Console.WriteLine();
        Console.WriteLine($"NanoC> {response.Text}");
        Console.WriteLine();
    }
    catch (Exception ex) {
        Console.WriteLine();
        Console.WriteLine("NanoC> Ocurrio un error al ejecutar el agente.");
        Console.WriteLine(ex.Message);
        Console.WriteLine();
    }
}

return 0;

static bool IsExitCommand(string input) =>
    input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
    input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
    input.Equals("salir", StringComparison.OrdinalIgnoreCase);

static async Task<bool> TryHandleLocalCommandAsync(
    string input,
    AIAgent agent,
    NanoConfiguration configuration,
    Action<AgentSession> setSession)
{
    if (!input.StartsWith('/')) {
        return false;
    }

    switch (input) {
        case "/help":
            Console.WriteLine();
            Console.WriteLine("Comandos locales:");
            Console.WriteLine("  /help       Muestra esta ayuda.");
            Console.WriteLine("  /reset      Reinicia la conversacion.");
            Console.WriteLine("  /workspace  Muestra el workspace efectivo.");
            Console.WriteLine("  /model      Muestra el modelo actual.");
            Console.WriteLine("  exit        Sale de la app.");
            Console.WriteLine();
            return true;

        case "/reset":
            setSession(await agent.CreateSessionAsync());
            Console.WriteLine();
            Console.WriteLine("Sesion reiniciada.");
            Console.WriteLine();
            return true;

        case "/workspace":
            Console.WriteLine();
            Console.WriteLine(configuration.Workspace);
            Console.WriteLine();
            return true;

        case "/model":
            Console.WriteLine();
            Console.WriteLine(configuration.Model);
            Console.WriteLine();
            return true;

        default:
            Console.WriteLine();
            Console.WriteLine($"Comando local desconocido: {input}");
            Console.WriteLine("Usa /help para ver la lista.");
            Console.WriteLine();
            return true;
    }
}

    static void PrintBanner(NanoConfiguration configuration) {
    Console.WriteLine("NanoC");
    Console.WriteLine($"Modelo: {configuration.Model}");
    Console.WriteLine($"Workspace: {configuration.Workspace}");
    Console.WriteLine("Escribe /help para ver comandos locales.");
    Console.WriteLine();
}

sealed class NanoConfiguration {
    public required string OpenAIApiKey { get; init; }
    public required string Model { get; init; }
    public required string Workspace { get; init; }
    public required string PromptPath { get; init; }

    public static NanoConfiguration Load() {
        var workspace = ResolveWorkspace();
        var promptPath = ResolvePromptPath();

        return new NanoConfiguration {
            OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty,
            Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4.1-mini",
            Workspace = workspace,
            PromptPath = promptPath
        };
    }

    private static string ResolveWorkspace() {
        var configured = Environment.GetEnvironmentVariable("NANOC_WORKSPACE");
        var raw = string.IsNullOrWhiteSpace(configured)
            ? Environment.CurrentDirectory
            : configured;

        return Path.GetFullPath(raw);
    }

    private static string ResolvePromptPath() {
        var local = Path.Combine(AppContext.BaseDirectory, "AGENTS.MD");
        if (File.Exists(local)) {
            return local;
        }

        var cwd = Path.Combine(Environment.CurrentDirectory, "AGENTS.MD");
        if (File.Exists(cwd)) {
            return cwd;
        }

        return local;
    }
}

sealed class PromptLoader {
    private readonly NanoConfiguration _configuration;

    public PromptLoader(NanoConfiguration configuration) {
        _configuration = configuration;
    }

    public string LoadInstructions() {
        if (!File.Exists(_configuration.PromptPath)) {
            throw new FileNotFoundException($"No existe el prompt base: {_configuration.PromptPath}");
        }

        var template = File.ReadAllText(_configuration.PromptPath, Encoding.UTF8);
        var promptFromFile = template.Replace("{workspace}", _configuration.Workspace, StringComparison.Ordinal);

        var baseRules = $"""
        Reglas operativas:
        - Trabajas en {_configuration.Workspace}.
        - Antes de modificar, lee archivos relevantes.
        - Usa read_file y write_file para archivos.
        - Usa run_shell para comandos del sistema.
        - No inventes resultados.
        - Responde corto.
        - Si un comando falla, explica el error y propone el siguiente paso.
        - No salgas del workspace.
        """;

        return $"{promptFromFile.Trim()}\n\n{baseRules.Trim()}";
    }
}

sealed class WorkspaceTools {
    private static readonly string[] DangerousCommandMarkers =
    [
        "rm -rf",
        "sudo ",
        "shutdown",
        "reboot",
        "mkfs",
        "dd if=",
        "git reset --hard",
        "git clean -fd",
        ":(){:|:&};:"
    ];

    private readonly string _workspace;

    public WorkspaceTools(string workspace) {
        _workspace = Path.GetFullPath(workspace);
    }

    [Description("Lee un archivo de texto dentro del workspace y devuelve su contenido completo.")]
    public string ReadFile([Description("Ruta relativa o absoluta del archivo a leer.")] string path) {
        var fullPath = ResolvePath(path);
        return File.ReadAllText(fullPath, Encoding.UTF8);
    }

    [Description("Escribe contenido UTF-8 en un archivo dentro del workspace. Crea carpetas faltantes si es necesario.")]
    public string WriteFile(
        [Description("Ruta relativa o absoluta del archivo a escribir.")] string path,
        [Description("Contenido completo del archivo.")] string content) {
        var fullPath = ResolvePath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content, Encoding.UTF8);
        return $"OK: {fullPath}";
    }

    [Description("Ejecuta un comando de shell dentro del workspace y devuelve stdout, stderr y exit code.")]
    public string RunShell([Description("Comando a ejecutar en zsh.")] string command) {
        if (string.IsNullOrWhiteSpace(command)) {
            return "Comando vacio.";
        }

        if (DangerousCommandMarkers.Any(marker => command.Contains(marker, StringComparison.OrdinalIgnoreCase))) {
            return $"Comando bloqueado por seguridad: {command}";
        }

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo {
            FileName = "/bin/zsh",
            WorkingDirectory = _workspace,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("-lc");
        process.StartInfo.ArgumentList.Add(command);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try {
            process.Start();
            process.WaitForExitAsync(cts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) {
            TryKill(process);
            return $"$ {command}\nexit_code: timeout\n--- STDOUT ---\n(timeout)\n--- STDERR ---\nEl comando excedio 30 segundos.";
        }

        var stdout = process.StandardOutput.ReadToEnd().Trim();
        var stderr = process.StandardError.ReadToEnd().Trim();

        if (string.IsNullOrWhiteSpace(stdout)) {
            stdout = "(vacio)";
        }

        if (string.IsNullOrWhiteSpace(stderr)) {
            stderr = "(vacio)";
        }

        return $"$ {command}\nexit_code: {process.ExitCode}\n--- STDOUT ---\n{stdout}\n--- STDERR ---\n{stderr}";
    }

    private string ResolvePath(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("La ruta no puede estar vacia.", nameof(path));
        }

        var candidate = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(_workspace, path));

        if (!IsInsideWorkspace(candidate)) {
            throw new InvalidOperationException($"La ruta sale del workspace: {candidate}");
        }

        return candidate;
    }

    private bool IsInsideWorkspace(string fullPath) {
        if (string.Equals(fullPath, _workspace, StringComparison.Ordinal)) {
            return true;
        }

        var normalizedWorkspace = _workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(normalizedWorkspace, StringComparison.Ordinal);
    }

    private static void TryKill(Process process) {
        try {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
            }
        }
        catch {
        }
    }
}
