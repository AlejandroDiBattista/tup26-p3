#:package Google.GenAI@*
#:property JsonSerializerIsReflectionEnabledByDefault=true

using Google.GenAI;
var api_key = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var modelo = "gemini-2.5-flash";

if (string.IsNullOrWhiteSpace(api_key)) {
    throw new InvalidOperationException("Configura GEMINI_API_KEY antes de ejecutar este ejemplo.");
}

Console.WriteLine(await Completar("dame exclusivamente el codigo fuente de la funcion factorial mas simple que puedas imaginar en c#, js, python, ruby, go, dart, swift"));

async Task<string> Completar(string indicacion) {
    var client   = new Client(apiKey: api_key );
    var response = await client.Models.GenerateContentAsync(modelo, indicacion);
    return response.Text ?? "";
}
