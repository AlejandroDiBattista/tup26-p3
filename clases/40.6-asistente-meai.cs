#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;

// Carga las variables de entorno desde el archivo .env.
DotNetEnv.Env.TraversePath().Load();

// Usa OpenAI como proveedor predeterminado.
var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();

Asistente chat = new Asistente(proveedor);

// Define el comportamiento inicial del asistente.
var alumnos = File.ReadAllText("../alumnos/alumnos.md");
// Console.WriteLine(alumnos);

chat.Registrar(ChatRole.System, $"""
    Eres el ayudante de programacion III.
    Responde pregunta solo los alumnos. 
    En base a esta informacion:
    {alumnos}
""");

// Console.Clear();
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Asistente de programación");

while (true) {
    Console.Write("vos > ");
    var entrada = Console.ReadLine()?.Trim() ?? "";

    // Finaliza cuando la entrada está vacía.
    if (entrada.Length == 0) { break; }

    // Registra la entrada del usuario.
    chat.Registrar(ChatRole.User, entrada);

    // Genera una respuesta a partir del historial de la conversación.
    var texto = await chat.Respuesta();

    // Muestra la respuesta.
    Console.WriteLine($"\nasistente > {texto}\n");

    // Registra la respuesta del asistente.
    chat.Registrar(ChatRole.Assistant, texto);
}


class Asistente {
    private readonly IChatClient cliente;
    private readonly List<ChatMessage> historia = [];

    public Asistente(string proveedor) {
        proveedor = proveedor.ToUpperInvariant();
        var apiUrl = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
        var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
        var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL")   ?? "";

        var opciones = new OpenAIClientOptions { Endpoint = new Uri(apiUrl) };
        cliente = new OpenAIClient(new ApiKeyCredential(apiKey), opciones)
            .GetChatClient(modelo)
            .AsIChatClient();
    }

    public void Registrar(ChatRole rol, string texto) {
        historia.Add(new(rol, texto));
    }

    public async Task<string> Respuesta() {
        var respuesta = await cliente.GetResponseAsync(historia);
        return respuesta.Text ?? "";
    }
}
