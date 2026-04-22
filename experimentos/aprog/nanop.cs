using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using OpenAI.Agents;
using OpenAI.Agents.Extensions;

public static class NanoProg {
    private static readonly string Workspace = Path.GetDirectoryName(Path.GetFullPath(Environment.ProcessPath ?? AppContext.BaseDirectory))
        ?? Directory.GetCurrentDirectory();

    [FunctionTool]
    public static string ReadFile(string path) {
        return File.ReadAllText(path);
    }

    [FunctionTool]
    public static string WriteFile(string path, string content) {
        string? parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);

        File.WriteAllText(path, content);
        return $"OK: {path}";
    }

    public static string RunShell(dynamic request) {
        object? commandsObj = null;

        try { commandsObj = request?.data?.action?.commands; } catch { }
        if (commandsObj == null) {
            try { commandsObj = request?.data?.commands; } catch { }
        }

        var commands = new List<string>();
        if (commandsObj is IEnumerable<object> items) {
            foreach (var item in items)
                commands.Add(item?.ToString() ?? string.Empty);
        }

        var outputs = new List<string>();

        foreach (string cmd in commands) {
            var psi = new ProcessStartInfo {
                FileName = "/bin/sh",
                Arguments = $"-c \"{cmd.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
                WorkingDirectory = Workspace,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new Exception("No se pudo iniciar el proceso");
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            outputs.Add(
                $"$ {cmd}\nexit_code: {process.ExitCode}\n--- STDOUT ---\n{(string.IsNullOrWhiteSpace(stdout) ? "(vacío)" : stdout.Trim())}\n--- STDERR ---\n{(string.IsNullOrWhiteSpace(stderr) ? "(vacío)" : stderr.Trim())}"
            );
        }

        return string.Join("\n\n", outputs);
    }

    public static void Main() {
        var agent = new Agent(
            name: "NanoProg",
            model: "gpt-5.4-mini",
            instructions: $@"
        Eres un asistente de programación experto en Python.
        Trabajás en {Workspace}.
        Antes de modificar, leé archivos relevantes.
        Usá read_file/write_file para archivos y shell para comandos.
        No inventes resultados. Respondé corto.",
            tools: new object[] {
                new ShellTool(RunShell),
                Tool.FromFunction(ReadFile),
                Tool.FromFunction(WriteFile)
            }
        );

        var history = new List<object>();

        while (true) {
            Console.Write("Tú> ");
            string? userInput = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(userInput))
                continue;

            string lower = userInput.ToLowerInvariant();
            if (lower == "salir" || lower == "exit" || lower == "quit")
                break;

            var input = new List<object>(history) {
                new Dictionary<string, object> {
                    ["role"] = "user",
                    ["content"] = userInput
                }
            };

            dynamic result = Runner.RunSync(agent, input);
            Console.WriteLine($"\nAgente> {result.final_output}\n");
            history = result.to_input_list();
        }
    }
}
