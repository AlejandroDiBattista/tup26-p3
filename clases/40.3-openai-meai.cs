#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package OpenAI@2.9.1

using Microsoft.Extensions.AI;
using OpenAI.Chat;

DotNetEnv.Env.Load();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var modelo = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-5.5";

IChatClient chat = new ChatClient(modelo, apiKey).AsIChatClient();

Console.Clear();
var inicio = DateTime.Now;


var salida = await Traducir("La mañana esta soleada en Alabama");
Console.WriteLine(salida);
File.WriteAllText("./40.0-salida.md", salida);

Console.WriteLine($"\n✦ {(DateTime.Now - inicio).TotalSeconds:N2}s");


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
    var resultado = await Completar($"Escribe un programa en c# que {texto}. Solo dame el codigo sin explicaciones.");
    File.WriteAllText("./40.0-programa.cs", resultado, Encoding.UTF8);
    return resultado;
}

async Task<string> PaginaWeb(string texto) {
    var resultado = await Completar($"Escribe una pagina web autocontenida en html que {texto}. Solo dame el codigo sin explicaciones.");
    File.WriteAllText("./40.0-pagina.html", resultado, Encoding.UTF8);
    return resultado;
}

async Task<string> Completar(string indicacion) {
    var respuesta = await chat.GetResponseAsync(indicacion);
    return respuesta.Text ?? "";
}
