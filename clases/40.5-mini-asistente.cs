#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package OpenAI@2.9.1
#pragma warning disable CS8321

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;

DotNetEnv.Env.Load();

string proveedor = (args.Length > 0 ? args[0] : "openai").ToUpper();

var inicio = DateTime.Now;
var salida = "";

// Console.InputEncoding  = Encoding.UTF16;
// Console.OutputEncoding = Encoding.UTF16;
Console.Clear();

var historia = """
SYSTEM: Eres un asistente de programacion. Responde sumamente breve y prefieres c#.
""";

while( true ){
    Console.Write("👤 > "); 
    var texto = Console.ReadLine() ?? "";

    historia += $"""
    USER: {texto}
    """;
    var respuesta = await Completar(historia);
    historia += $"""
    ASSISTANT: respuesta
    """;
    Console.WriteLine($"🤖 > {respuesta}");
}

// Función genérica para completar cualquier indicación usando el modelo de lenguaje.
async Task<string> Completar(string indicacion) {
    string? URL      = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
    string? API_KEY  = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
    string  MODELO   = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.5";
    
    var opciones = new OpenAIClientOptions { Endpoint = new Uri(URL) };
    IChatClient chat = new OpenAIClient(new ApiKeyCredential(API_KEY), opciones)
        .GetChatClient(MODELO).AsIChatClient();
    
    var respuesta = await chat.GetResponseAsync(indicacion);
    return respuesta.Text ?? "";
}
