#:package Google.GenAI@*
#:package DotNetEnv@*
#:property JsonSerializerIsReflectionEnabledByDefault=true

using Google.GenAI;
using Google.GenAI.Types;

DotNetEnv.Env.TraversePath().Load();

var api_key = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var modelo = "gemini-3-flash-preview";

if (string.IsNullOrWhiteSpace(api_key)) {
    throw new InvalidOperationException("Configura GEMINI_API_KEY antes de ejecutar este ejemplo.");
}

Console.Clear();
var inicio = DateTime.Now;

var salida = "";
// salida = await Traducir("mañana va a llover temprano.", "ingles");
// Console.WriteLine(salida);
// salida = await Traducir("¿Cuál es la capital de Francia?", "frances");
// Console.WriteLine(salida);

salida = await Traducir("La mañana esta soleada en Alabama");
Console.WriteLine(salida);
System.IO.File.WriteAllText("salida.md", salida);


Console.WriteLine($"\n✦ {(DateTime.Now - inicio).TotalSeconds:N2}s");

async Task<string> Traducir(string texto, string idiomaDestino="ingles") {
    return await Completar($"Traduce el siguiente texto al {idiomaDestino}: '{texto}'. Dame la frase traducida sin ningún comentario adicional.");
}

async Task<string> Completar(string indicacion) {
    var client   = new Client(apiKey: api_key );
    var config   = new GenerateContentConfig {
        ServiceTier = ServiceTier.Standard
    };
    var response = await client.Models.GenerateContentAsync(modelo, indicacion, config);
    return response.Text ?? "";
}
