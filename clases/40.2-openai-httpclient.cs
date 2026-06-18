#:package DotNetEnv@*

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

DotNetEnv.Env.TraversePath().Load();

string proveedor = args.Length > 0 ? args[0] : "openai";

var inicio = DateTime.Now;
var salida = "";

// Mostrar( await Traducir("Todo lo que necesitas es atencion", "ingles"));
// Mostrar( await ExtraerNombre("Mi nombre es Ada Lovelace"));
// Mostrar( await ExtraerFecha("La reunion es el proximo lunes"));
// Mostrar( await Resumir(agenda));
// Mostrar( await Programar("calcula el área de un círculo con radio 5"));
// Mostrar( await Consultar("¿Cuántos alumnos varones y mujeres hay en total?"));
Mostrar( await PaginaWeb("Muestre un reloj analogico en tiempo real"));

Console.WriteLine($"\n✧ {(DateTime.Now - inicio).TotalSeconds:0.0}s");

async Task<string> Traducir(string texto, string idioma) {
    return await Completar($"traduce el siguiente texto al {idioma}: {texto}");
} 

async Task<string> ExtraerNombre(string texto) {
    return await Completar($"extrae el nombre del siguiente texto en formato <apellido>, <nombre>: {texto}");
} 

async Task<string> ExtraerFecha(string texto) {
    return await Completar($"Hoy es {DateTime.Now:yyyy-MM-dd}. Extrae la fecha relativa del siguiente texto: {texto}");
} 

async Task<string> Resumir(string texto) {
    return await Completar($"resume el siguiente texto en una frase: {texto}");
}

async Task<string> Consultar(string texto) {
    var alumnos = File.ReadAllText("../alumnos/alumnos.md");

    return await Completar($"Actual como un asistente de programacion y responde a la siguiente pregunta: {texto}\n\nTen en cuenta esta informacion de los alumnos:\n{alumnos}");
}

async Task<string> Programar(string texto) {
    return await Completar($"Escribe un programa en c# que {texto}. Solo dame el codigo sin explicaciones.");
}

async Task<string> PaginaWeb(string texto) {
    return await Completar($"Escribe una pagina web autocontenida en html que {texto}. Solo dame el codigo sin explicaciones.");
}


void Mostrar(string texto) {
    Console.WriteLine("\n---\n");
    Console.WriteLine(texto);

    salida += $"{texto}\n";
    File.WriteAllText("salida.md", salida);
}

async Task<string> Completar(string prompt) {
    var config = Proveedor.Crear(proveedor);

    const double temperatura = 1;
    const int maxTokens = 8192;

    var json = $$"""
    {
      "model": "{{Json(config.Modelo)}}",
      "messages": [
        {
          "role": "user",
          "content": "{{Json(prompt)}}"
        }
      ],
      "temperature": {{temperatura}},
      "max_completion_tokens": {{maxTokens}}
    }
    """;

    using var http = new HttpClient();
    if (config.ApiKey is not null) {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
    }

    using var contenido = new StringContent(json, Encoding.UTF8, "application/json");
    using var respuesta = await http.PostAsync(config.Url, contenido);
    var cuerpo = await respuesta.Content.ReadAsStringAsync();

    if (!respuesta.IsSuccessStatusCode) { return $"Error HTTP {(int)respuesta.StatusCode}\n{cuerpo}"; }

    return JsonNode.Parse(cuerpo)?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
}

static string Json(string texto) =>
    JsonEncodedText.Encode(texto).ToString();

record Proveedor(string Url, string Modelo, string? ApiKeyVariable = null) {
    public string? ApiKey => Environment.GetEnvironmentVariable(ApiKeyVariable ?? "") ?? "";

    public static Proveedor Crear(string proveedor) => proveedor.ToLower() switch {
     // "openai"     => new("https://api.openai.com/v1/chat/completions",                               "gpt-5.4-mini",     "OPENAI_API_KEY"),
        "openai"     => new("https://api.openai.com/v1/chat/completions",                               "gpt-5.5",          "OPENAI_API_KEY"),
        "gemini"     => new("https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", "gemini-2.5-flash", "GEMINI_API_KEY"),
        "groq"       => new("https://api.groq.com/openai/v1/chat/completions",                          "qwen/qwen3.6-27b", "GROQ_API_KEY"),
        "ollama"     => new("http://localhost:11434/v1/chat/completions",                               "qwen2.5-coder:7b"),
        "openrouter" => new("https://openrouter.ai/api/v1/chat/completions",                            "openrouter/auto",  "OPENROUTER_API_KEY"),
        _ => throw new InvalidOperationException($"Proveedor desconocido '{proveedor}'.")
    };
}
