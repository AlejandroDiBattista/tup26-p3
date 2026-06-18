#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package OpenAI@2.9.1

using Microsoft.Extensions.AI;
using System.Text;

using OpenAIChatClient = OpenAI.Chat.ChatClient;

DotNetEnv.Env.TraversePath().Load();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var modelo = "gpt-5.5";

if (string.IsNullOrWhiteSpace(apiKey)) {
    throw new InvalidOperationException("Configura OPENAI_API_KEY antes de ejecutar este ejemplo.");
}

Console.OutputEncoding = Encoding.UTF8;
Console.Clear();

IChatClient chat = new OpenAIChatClient(modelo, apiKey).AsIChatClient();

var mensajes = new List<ChatMessage> {
    new(ChatRole.System, """
    Sos un asistente de programación.
    Respondé en español, directo y técnico.
    Priorizá ejemplos en C# cuando el usuario no indique lenguaje.
    Si falta contexto, pedí el dato mínimo necesario.
    """)
};

var transcript = new StringBuilder();

Console.WriteLine($"Asistente MEAI listo ({modelo}).");
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

    var inicio = DateTime.Now;
    var respuesta = await chat.GetResponseAsync(mensajes);
    var texto = respuesta.Text ?? "";
    var segundos = (DateTime.Now - inicio).TotalSeconds;

    mensajes.Add(new(ChatRole.Assistant, texto));
    transcript.AppendLine($"## Asistente\n\n{texto}\n");
    File.WriteAllText("salida.md", transcript.ToString());

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\nasistente>");
    Console.ResetColor();
    Console.WriteLine($"{texto}\n");
    Console.WriteLine($"✦ {segundos:N2}s\n");
}
