#:package DotNetEnv@*


using System.Text;
using System.Text.Json;

DotNetEnv.Env.TraversePath().Load();

var api_key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var modelo = "gemini-3-flash-preview";
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent";

if (string.IsNullOrWhiteSpace(api_key)) {
    throw new InvalidOperationException("Configura GEMINI_API_KEY antes de ejecutar este ejemplo.");
}

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("x-goog-api-key", api_key);

var json = """
{
  "contents": [
    {
      "role": "user",
      "parts": [
        {
          "text": "dame la version mas simple de quicksort en c#"
        }
      ]
    }
  ],
  "generationConfig": {
    "temperature": 0.6,
    "topP": 0.95,
    "maxOutputTokens": 4096
  }
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
    .GetProperty("candidates")[0]
    .GetProperty("content")
    .GetProperty("parts")[0]
    .GetProperty("text")
    .GetString() ?? "";

Console.WriteLine(salida);
File.WriteAllText("salida.md", salida);
