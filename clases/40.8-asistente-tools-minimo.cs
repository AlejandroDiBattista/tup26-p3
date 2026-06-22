#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0

using System.ComponentModel;
using Microsoft.Extensions.AI;
using OpenAIChatClient = OpenAI.Chat.ChatClient;

DotNetEnv.Env.Load();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey)) {
    throw new InvalidOperationException("Falta OPENAI_API_KEY en el archivo .env.");
}

var workspace = Path.GetFullPath("workspace");
Directory.CreateDirectory(workspace);

IChatClient chat = new OpenAIChatClient("gpt-5.4-mini", apiKey)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var opciones = new ChatOptions {
    Tools = [
        AIFunctionFactory.Create(ListarArchivos, new() {
            Name = "listar-archivos",
            Description = "Lista los archivos y directorios de una ruta del workspace."
        }),
        AIFunctionFactory.Create(LeerArchivo, new() {
            Name = "leer-archivo",
            Description = "Lee el contenido completo de un archivo del workspace."
        }),
        AIFunctionFactory.Create(EscribirArchivo, new() {
            Name = "escribir-archivo",
            Description = "Crea o reemplaza un archivo de texto dentro del workspace."
        })
    ]
};

var mensajes = new List<ChatMessage> {
    new(ChatRole.System, """
    Sos un asistente de programación. Por defecto responde en c# con un archivo (file based) con sintaxis k&r. 
    Respondé en español en forma clara y concisa. Si no estás seguro de algo, preguntá.
    Usá las herramientas para listar, leer y escribir archivos cuando sea necesario.
    Antes de modificar un archivo existente, leelo.
    """)
};

Console.WriteLine($"Asistente listo. Workspace: {workspace}");
Console.WriteLine("Escribí /salir para terminar.\n");

while (true) {
    Console.Write("vos> ");
    var texto = Console.ReadLine();

    if (texto is null || texto.Equals("/salir", StringComparison.OrdinalIgnoreCase)) break;
    if (string.IsNullOrWhiteSpace(texto)) continue;

    mensajes.Add(new(ChatRole.User, texto));

    try {
        var respuesta = await chat.GetResponseAsync(mensajes, opciones);
        mensajes.Add(new(ChatRole.Assistant, respuesta.Text ?? ""));
        Console.WriteLine($"asistente> {respuesta.Text}\n");
    }
    catch (Exception ex) {
        Console.WriteLine($"Error: {ex.Message}\n");
    }
}

string ListarArchivos(
    [Description("Ruta relativa del directorio. Usar punto para la raíz.")] string ruta = ".") {
    var rutaCompleta = ResolverRuta(ruta);

    if (!Directory.Exists(rutaCompleta)) return $"No existe el directorio: {ruta}";

    var elementos = Directory.EnumerateFileSystemEntries(rutaCompleta)
        .Select(Path.GetFileName);

    return string.Join(Environment.NewLine, elementos);
}

string LeerArchivo(
    [Description("Ruta relativa del archivo que se quiere leer.")] string ruta) {
    var rutaCompleta = ResolverRuta(ruta);

    return File.Exists(rutaCompleta)
        ? File.ReadAllText(rutaCompleta)
        : $"No existe el archivo: {ruta}";
}

string EscribirArchivo(
    [Description("Ruta relativa del archivo que se quiere escribir.")] string ruta,
    [Description("Contenido completo que se guardará en el archivo.")] string contenido) {
    var rutaCompleta = ResolverRuta(ruta);
    Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);
    File.WriteAllText(rutaCompleta, contenido);
    return $"Archivo escrito: {ruta}";
}

string ResolverRuta(string ruta) {
    var rutaCompleta = Path.GetFullPath(Path.Combine(workspace, ruta));
    var prefijoValido = workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

    if (rutaCompleta != workspace && !rutaCompleta.StartsWith(prefijoValido, StringComparison.Ordinal)) {
        throw new UnauthorizedAccessException("La ruta está fuera del workspace.");
    }

    return rutaCompleta;
}
