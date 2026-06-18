#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package OpenAI@2.9.1

using Microsoft.Extensions.AI;
using OpenAI.Chat;

DotNetEnv.Env.TraversePath().Load();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var modelo = "gpt-5.5";

if (string.IsNullOrWhiteSpace(apiKey)) {
    throw new InvalidOperationException("Configura OPENAI_API_KEY antes de ejecutar este ejemplo.");
}

Console.Clear();
var inicio = DateTime.Now;

IChatClient chat = new ChatClient(modelo, apiKey).AsIChatClient();

var salida = await Traducir("La mañana esta soleada en Alabama");
Console.WriteLine(salida);
File.WriteAllText("salida.md", salida);

Console.WriteLine($"\n✦ {(DateTime.Now - inicio).TotalSeconds:N2}s");

async Task<string> Traducir(string texto, string idiomaDestino = "ingles") {
    return await Completar($"Traduce el siguiente texto al {idiomaDestino}: '{texto}'. Dame la frase traducida sin ningún comentario adicional.");
}

async Task<string> Completar(string indicacion) {
    var respuesta = await chat.GetResponseAsync(indicacion);
    return respuesta.Text ?? "";
}
