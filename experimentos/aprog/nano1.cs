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

string workspaceRoot = Path.GetFullPath(
    args.Length > 0
        ? args[0]
        : Path.Combine(Environment.CurrentDirectory, "ejemplos")
);

Directory.CreateDirectory(workspaceRoot);

string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Falta OPENAI_API_KEY.");
    Console.ResetColor();

    Console.WriteLine("""
    Configuralo así:

        export OPENAI_API_KEY="tu_api_key"

    Y después ejecutá de nuevo.
    """);

    return;
}

ResponsesClient responsesClient = new(apiKey);

AIAgent agent = responsesClient.AsAIAgent(
    "gpt-5.5",
    $$"""
    Sos un asistente de programación que trabaja desde consola.

    Reglas:
    - Respondé siempre en español.
    - Sé directo, técnico y práctico.
    - Tenés acceso a herramientas locales para leer y escribir archivos.
    - El workspace permitido es:
      {{workspaceRoot}}
    - Solo podés operar dentro de ese workspace.
    - Antes de modificar un archivo existente, primero leelo.
    - Cuando escribas un archivo, escribí el contenido completo.
    - No digas que leíste un archivo si no usaste la herramienta leer_archivo.
    - Si el usuario pide algo riesgoso, explicá el riesgo antes.
    """,
    "CodeAgent",
    "Agente de consola para programación asistida con herramientas locales.",
    [
        AIFunctionFactory.Create(LeerArchivo),
        AIFunctionFactory.Create(EscribirArchivo),
        AIFunctionFactory.Create(ListarArchivos)
    ]
);

AgentSession session = await agent.CreateSessionAsync();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("CodeAgent listo.");
Console.ResetColor();

Console.WriteLine($"Workspace: {workspaceRoot}");
Console.WriteLine("Comandos:");
Console.WriteLine("  /salir");
Console.WriteLine();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("vos> ");
    Console.ResetColor();

    string? input = Console.ReadLine();

    if (input is null)
    {
        Console.WriteLine();
        break;
    }

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("/salir", StringComparison.OrdinalIgnoreCase))
        break;

    try
    {
        AgentResponse response = await agent.RunAsync(new ChatMessage(ChatRole.User, input), session);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("asistente>");
        Console.ResetColor();

        Console.WriteLine(response.Text);
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
    }
}

[Description("Lista archivos dentro del workspace.")]
string ListarArchivos(
    [Description("Ruta relativa de la carpeta dentro del workspace. Usar punto para la raíz.")]
    string path = ".")
{
    string fullPath = ResolveSafePath(workspaceRoot, path);

    if (!Directory.Exists(fullPath))
        return $"La carpeta no existe: {path}";

    var files = Directory
        .EnumerateFileSystemEntries(fullPath, "*", SearchOption.TopDirectoryOnly)
        .Select(p =>
        {
            string relative = Path.GetRelativePath(workspaceRoot, p);
            return Directory.Exists(p)
                ? $"[dir]  {relative}"
                : $"[file] {relative}";
        })
        .OrderBy(x => x)
        .ToArray();

    if (files.Length == 0)
        return $"La carpeta está vacía: {path}";

    return string.Join(Environment.NewLine, files);
}

[Description("Lee un archivo de texto dentro del workspace.")]
string LeerArchivo(
    [Description("Ruta relativa del archivo dentro del workspace. Ejemplo: src/Program.cs")]
    string path)
{
    string fullPath = ResolveSafePath(workspaceRoot, path);

    if (!File.Exists(fullPath))
        return $"El archivo no existe: {path}";

    string content = File.ReadAllText(fullPath, Encoding.UTF8);

    return $"""
    Archivo leído: {path}

    ```text
    {content}
    ```
    """;
}

[Description("Escribe o reemplaza un archivo de texto dentro del workspace.")]
string EscribirArchivo(
    [Description("Ruta relativa del archivo dentro del workspace. Ejemplo: src/Program.cs")]
    string path,

    [Description("Contenido completo que debe quedar guardado en el archivo.")]
    string content)
{
    string fullPath = ResolveSafePath(workspaceRoot, path);

    string? directory = Path.GetDirectoryName(fullPath);

    if (!string.IsNullOrWhiteSpace(directory))
        Directory.CreateDirectory(directory);

    File.WriteAllText(fullPath, content, Encoding.UTF8);

    return $"Archivo escrito correctamente: {path}";
}

static string ResolveSafePath(string workspaceRoot, string relativePath)
{
    if (string.IsNullOrWhiteSpace(relativePath))
        throw new ArgumentException("La ruta no puede estar vacía.");

    if (Path.IsPathRooted(relativePath))
        throw new ArgumentException("La ruta debe ser relativa, no absoluta.");

    string fullPath = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));

    string normalizedRoot = Path.GetFullPath(workspaceRoot)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        + Path.DirectorySeparatorChar;

    if (!fullPath.StartsWith(normalizedRoot, StringComparison.Ordinal))
        throw new UnauthorizedAccessException("Intento de acceder fuera del workspace.");

    return fullPath;
}
