#:package DotNetEnv@*

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

DotNetEnv.Env.TraversePath().Load();

var api_key = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
var modelo = "openrouter/auto";
var url = "https://openrouter.ai/api/v1/chat/completions";

if (string.IsNullOrWhiteSpace(api_key)) {
    throw new InvalidOperationException("Configura OPENROUTER_API_KEY antes de ejecutar este ejemplo.");
}

using var http = new HttpClient();
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_key);

var json = $$"""
{
  "model": "{{modelo}}",
  "messages": [
    {
      "role": "user",
      "content": "dame la version mas simple de quicksort en c#"
    }
  ],
  "temperature": 0.6,
  "max_completion_tokens": 4096,
  "top_p": 0.95
}
""";

using var contenido = new StringContent(json, Encoding.UTF8, "application/json");
using var respuesta = await http.PostAsync(url, contenido);
var cuerpo = await respuesta.Content.ReadAsStringAsync();

if (!respuesta.IsSuccessStatusCode) {
    Console.WriteLine($"Error HTTP {(int)respuesta.StatusCode}");
    Console.WriteLine(cuerpo);
    return;
}

using var doc = JsonDocument.Parse(cuerpo);
var salida = doc.RootElement
    .GetProperty("choices")[0]
    .GetProperty("message")
    .GetProperty("content")
    .GetString() ?? "";

Console.WriteLine(salida);
File.WriteAllText("salida.md", salida);
