#!/usr/bin/env dotnet

#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property PublishAot=false
#:package OpenAI@2.9.1
#:package Microsoft.Agents.AI.OpenAI@1.1.0
#:package Microsoft.Extensions.AI@10.4.0

#pragma warning disable OPENAI001

using System.ComponentModel;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

Console.OutputEncoding = Encoding.UTF8;

string workspaceRoot = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine(Environment.CurrentDirectory, "ejemplos"));
Directory.CreateDirectory(workspaceRoot);

string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey)) {
    Say(ConsoleColor.Red, "Falta OPENAI_API_KEY.\n");
    Console.WriteLine("""
    Configuralo así:

        export OPENAI_API_KEY="tu_api_key"

    Y después ejecutá de nuevo.
    """);

    return;
}

var agent = new ResponsesClient(apiKey).AsAIAgent(
    "gpt-5.5",
    $"""
    Sos un asistente de programación por consola.
    Respondé siempre en español, directo, técnico y práctico.
    Podés listar, leer y escribir archivos solo dentro de este workspace:
    {workspaceRoot}
    Usá la herramienta Archivo con acción listar, leer o escribir.
    Antes de modificar un archivo existente, leelo con Archivo/leer.
    Cuando escribas un archivo, escribí el contenido completo.
    No digas que leíste un archivo si no usaste Archivo/leer.
    Si el usuario pide algo riesgoso, explicá el riesgo antes.
    """,
    "CodeAgent",
    "Agente de consola para programación asistida con herramientas locales.",
    [AIFunctionFactory.Create(Archivo)]
);

AgentSession session = await agent.CreateSessionAsync();

Say(ConsoleColor.Green, "CodeAgent listo.\n");
Console.WriteLine($"Workspace: {workspaceRoot}\nComandos:\n  /salir\n");

while (true) {
    Say(ConsoleColor.Cyan, "vos> ");

    string? input = Console.ReadLine();

    if (input is null) {
        Console.WriteLine();
        break;
    }

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("/salir", StringComparison.OrdinalIgnoreCase))
        break;

    try {
        AgentResponse response = await agent.RunAsync(new ChatMessage(ChatRole.User, input), session);
        Say(ConsoleColor.Yellow, "asistente>\n");
        Console.WriteLine($"{response.Text}\n");
    }
    catch (Exception ex) {
        Say(ConsoleColor.Red, $"Error: {ex.Message}\n");
    }
}

[Description("Opera archivos dentro del workspace.")]
string Archivo(
    [Description("Acción a ejecutar: listar, leer o escribir.")]
    string accion,

    [Description("Ruta relativa dentro del workspace. Usar punto para la raíz.")]
    string path = ".",

    [Description("Contenido completo para la acción escribir.")]
    string? contenido = null) {
    string fullPath = ResolveSafePath(workspaceRoot, path);

    return accion.Trim().ToLowerInvariant() switch {
        "listar"   => Listar(fullPath, path),
        "leer"     => Leer(fullPath, path),
        "escribir" when contenido is not null => Escribir(fullPath, path, contenido),
        "escribir" => "Falta el contenido para escribir el archivo.",
        _ => "Acción inválida. Usá listar, leer o escribir."
    };
}

string Listar(string fullPath, string path) {
    if (!Directory.Exists(fullPath))
        return $"La carpeta no existe: {path}";

    var files = Directory
        .EnumerateFileSystemEntries(fullPath, "*", SearchOption.TopDirectoryOnly)
        .Select(p => $"{(Directory.Exists(p) ? "[dir] " : "[file]")} {Path.GetRelativePath(workspaceRoot, p)}")
        .Order()
        .ToArray();

    return files.Length == 0 ? $"La carpeta está vacía: {path}" : string.Join(Environment.NewLine, files);
}

string Leer(string fullPath, string path) =>
    File.Exists(fullPath)
        ? $"Archivo leído: {path}\n\n```text\n{File.ReadAllText(fullPath, Encoding.UTF8)}\n```"
        : $"El archivo no existe: {path}";

string Escribir(string fullPath, string path, string contenido) {
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    File.WriteAllText(fullPath, contenido, Encoding.UTF8);
    return $"Archivo escrito correctamente: {path}";
}

static void Say(ConsoleColor color, string text) {
    Console.ForegroundColor = color;
    Console.Write(text);
    Console.ResetColor();
}

static string ResolveSafePath(string workspaceRoot, string relativePath) {
    if (string.IsNullOrWhiteSpace(relativePath))
        throw new ArgumentException("La ruta no puede estar vacía.");

    if (Path.IsPathRooted(relativePath))
        throw new ArgumentException("La ruta debe ser relativa, no absoluta.");

    string root = Path.GetFullPath(workspaceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    string fullPath = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));

    if (fullPath != root && !fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        throw new UnauthorizedAccessException("Intento de acceder fuera del workspace.");

    return fullPath;
}
