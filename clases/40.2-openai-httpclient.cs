#:package DotNetEnv@*

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

DotNetEnv.Env.Load();

string proveedor = (args.Length > 0 ? args[0] : "openai").ToUpper();

string  URL     = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
string? API_KEY = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
string  MODELO  = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.5";

var inicio = DateTime.Now;
var salida = "";
Console.Clear();
Console.WriteLine($"\n- | Proveedor: {proveedor} | Modelo: {MODELO} |---------------------\n\n");

// Mostrar(await Completar("No por mucho madrugar..."));
// Mostrar(await Traducir("Todo lo que necesitas es atencion", "ingles"));
// Mostrar( await ExtraerNombre("Mi nombre es Ada Lovelace y soy una pionera de la computación"));
// Mostrar( await ExtraerFecha("La reunion es el proximo lunes"));
// Mostrar( await PaginaWeb("Calculadora muy elegante con operaciones basicas y un diseño moderno"));
// Mostrar( await Resumir(agenda));
// Mostrar( await Programar("calcule los 10 primeros números primos que sean mayores a 40. "));
// Mostrar( await Consultar("Cuántos alumnos varones y mujeres hay en total?"));
// Mostrar( await Consultar("que alumnos solo le solo le falta el tp5?"));
Mostrar( await PaginaWeb("Muestre un reloj analogico en tiempo real que tenga una bola que rebote dentro de la esfera y cambie de color cada vez que rebote."));


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

    return await Completar($"Actua como un asistente de programacion y responde a la siguiente pregunta: {texto}\n\nTen en cuenta esta informacion de los alumnos:\n{alumnos}");
}

async Task<string> Programar(string texto) {
    var resultado = await Completar($"Escribe un programa en c# que {texto}. Solo dame el codigo sin explicaciones.");
    File.WriteAllText("./40.0-programa.cs", resultado, Encoding.UTF8);
    return resultado;
}

async Task<string> PaginaWeb(string texto) {
    var resultado = await Completar($"Escribe una pagina web autocontenida en html que {texto}. Solo dame el codigo sin explicaciones.");
    File.WriteAllText("./40.0-pagina.html", resultado, Encoding.UTF8);
    return resultado;
}

// Muestra por consola y en `salida.md` para visualizar mejor el resultado. 
void Mostrar(string texto) {
    Console.WriteLine(texto);
    Console.WriteLine($"\n-- {(DateTime.Now - inicio).TotalSeconds:0.0}s ---------------\n");
    inicio = DateTime.Now;

    salida += $"---\n{texto}\n";
    File.WriteAllText("./40.0-salida.md", salida, Encoding.UTF8);
}

// Completa el prompt usando la API HTTP del proveedor configurado. (Compatible con OpenAI).
async Task<string> Completar(string prompt) {
    var temperatura = 1;
    var maxTokens = 4096;
    var tokenLimitName = proveedor == "OPENAI" ? "max_completion_tokens" : "max_tokens";

    var json = $$"""
    {
        "model": "{{Json(MODELO)}}",
        "messages": [
            {
                "role": "user",
                "content": "{{Json(prompt)}}"
            }
        ],
        "temperature": {{temperatura}},
        "{{tokenLimitName}}": {{maxTokens}}
    }
    """;

    using var http = new HttpClient();
    if (API_KEY is not null) {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", API_KEY);
    }

    using var contenido = new StringContent(json, Encoding.UTF8, "application/json");
    using var respuesta = await http.PostAsync(URL, contenido);

    var cuerpo = await respuesta.Content.ReadAsStringAsync();
    if (!respuesta.IsSuccessStatusCode) { return $"Error HTTP {respuesta.StatusCode}:{respuesta.ReasonPhrase}\n{cuerpo}"; }

    return JsonNode.Parse(cuerpo)?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
}

static string Json(string texto) => JsonEncodedText.Encode(texto).ToString();
