#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package OpenAI@2.9.1

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;

DotNetEnv.Env.TraversePath().Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL");

if (string.IsNullOrWhiteSpace(url)) {
    throw new InvalidOperationException($"Configura {proveedor}_API_URL antes de ejecutar este ejemplo.");
}

if (string.IsNullOrWhiteSpace(modelo)) {
    throw new InvalidOperationException($"Configura {proveedor}_MODEL antes de ejecutar este ejemplo.");
}

if (string.IsNullOrWhiteSpace(apiKey) && proveedor != "OLLAMA") {
    throw new InvalidOperationException($"Configura {proveedor}_API_KEY antes de ejecutar este ejemplo.");
}

Console.OutputEncoding = Encoding.UTF8;
Console.Clear();

var baseUrl = url.TrimEnd('/');
const string chatCompletions = "/chat/completions";

if (baseUrl.EndsWith(chatCompletions, StringComparison.OrdinalIgnoreCase)) {
    baseUrl = baseUrl[..^chatCompletions.Length];
}

var opciones = new OpenAIClientOptions {
    Endpoint = new Uri(baseUrl)
};

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"), opciones)
    .GetChatClient(modelo)
    .AsIChatClient();

var mensajes = new List<ChatMessage> {
    new(ChatRole.System, """
    Sos un asistente de programación.
    Respondé en español, directo y técnico.
    Priorizá ejemplos en C# cuando el usuario no indique lenguaje.
    Si falta contexto, pedí el dato mínimo necesario.
    """)
};

var transcript = new StringBuilder();

Console.WriteLine($"Asistente MEAI listo ({proveedor} / {modelo}).");
Console.WriteLine("Escribí /salir para terminar.\n");

while (true) {
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("vos> ");
    Console.ResetColor();

    var entrada = Console.ReadLine();

    if (entrada is null || entrada.Trim().Equals("/salir", StringComparison.OrdinalIgnoreCase)) {
        break;
    }

    if (string.IsNullOrWhiteSpace(entrada)) {
        continue;
    }

    mensajes.Add(new(ChatRole.User, entrada));
    transcript.AppendLine($"## Vos\n\n{entrada}\n");

    var respuesta = await chat.GetResponseAsync(mensajes);
    var texto = respuesta.Text ?? "";

    mensajes.Add(new(ChatRole.Assistant, texto));
    transcript.AppendLine($"## Asistente\n\n{texto}\n");
    File.WriteAllText("salida.md", transcript.ToString());

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\nasistente>");
    Console.ResetColor();
    Console.WriteLine($"{texto}\n");
}
