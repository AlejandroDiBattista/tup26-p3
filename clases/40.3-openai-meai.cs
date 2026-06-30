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
string? URL      = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
string? API_KEY  = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
string  MODELO   = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.5";

if (string.IsNullOrWhiteSpace(URL)) {
    throw new InvalidOperationException($"Configura {proveedor}_API_URL en el archivo .env.");
}


var inicio = DateTime.Now;
var salida = "";

Console.InputEncoding  = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
// Console.Clear();
Console.WriteLine($"\n- | Proveedor: {proveedor} | Modelo: {MODELO} |---------------------\n\n");

Mostrar( await Completar("No por mucho madrugar..."));
// Mostrar( await Traducir("Todo lo que necesitas es atencion", "ingles"));
// Mostrar( await ExtraerNombre("Mi nombre es Ada Lovelace y soy una pionera de la computación"));
// Mostrar( await ExtraerFecha("La reunion es el proximo lunes"));
// Mostrar( await Resumir(agenda));
// Mostrar( await Programar("calcule los 10 primeros números primos que sean mayores a 40. "));
// Mostrar( await Consultar("Cuántos alumnos varones y mujeres hay en total?"));
// Mostrar( await Consultar("que alumnos solo le solo le falta el tp5?"));
// Mostrar( await Sentimiento("La verdad que el curso me re copo, aprendi un monton"));
// Mostrar( await Corregir("ola ke ase, vos save programar en C#?"));
// Mostrar( await ExtraerContacto("Llamame al 381-555-1234 o escribime a ada@utn.edu.ar, soy Ada Lovelace"));
// Mostrar( await Examen("inyeccion de dependencias en ASP.NET Core"));
// Mostrar( await Explicar("var r = Enumerable.Range(1, 10).Where(x => x % 2 == 0).Sum();"));
// Mostrar( await Clasificar("No me llego la factura del mes pasado y me cobraron de mas"));
// Mostrar( await PaginaWeb("Calculadora muy elegante con operaciones basicas y un diseño moderno"));
Mostrar( await PaginaWeb("Muestre un reloj analogico en tiempo real"));

// Traduce un texto a otro idioma.
async Task<string> Traducir(string texto, string idioma) {
    return await Completar($"traduce el siguiente texto al {idioma}: {texto}");
} 

// Extrae el nombre de una persona a partir de un texto.
async Task<string> ExtraerNombre(string texto) {
    return await Completar($"extrae el nombre del siguiente texto en formato <apellido>, <nombre>: {texto}");
} 

// Extrae fechas relativas como "proximo lunes" o "en 3 dias" y las convierte a formato ISO (yyyy-MM-dd).
async Task<string> ExtraerFecha(string texto) {
    return await Completar($"Hoy es {DateTime.Now:yyyy-MM-dd}. Extrae la fecha relativa del siguiente texto: {texto}");
} 

// Resume un texto largo en una frase corta, ideal para generar titulares o resúmenes rápidos.
async Task<string> Resumir(string texto) {
    return await Completar($"resume el siguiente texto en una frase: {texto}");
}

// Responde a preguntas sobre los alumnos, usando la informacion del archivo alumnos.md como contexto.
async Task<string> Consultar(string texto) {
    var alumnos = File.ReadAllText("../alumnos/alumnos.md");

    return await Completar($"Actua como un asistente de programacion y responde a la siguiente pregunta: {texto}\n\nTen en cuenta esta informacion de los alumnos:\n{alumnos}");
}

// Escribe un programa en c#. Lo guarda en 40.0-programa.cs para poder probarlo y ejecutarlo. 
async Task<string> Programar(string texto) {
    var resultado = await Completar($"Escribe un programa en c# que {texto}. Solo dame el codigo sin explicaciones.");
    File.WriteAllText("./40.0-programa.cs", resultado, Encoding.UTF8);
    return resultado;
}

// Genera una pagina web autocontenida en html, con css y js embebidos. Lo guarda en 40.0-pagina.html para poder abrirlo en el navegador y probarlo.
async Task<string> PaginaWeb(string texto) {
    var resultado = await Completar($"Escribe una pagina web autocontenida en html que {texto}. Solo dame el codigo sin explicaciones.");
    File.WriteAllText("./40.0-pagina.html", resultado, Encoding.UTF8);
    return resultado;
}

// Clasifica el sentimiento de un comentario.
async Task<string> Sentimiento(string texto) {
    return await Completar($"Clasifica el sentimiento como POSITIVO, NEGATIVO o NEUTRO. Responde solo la palabra: {texto}");
}

// Corrige ortografia y gramatica sin cambiar el contenido.
async Task<string> Corregir(string texto) {
    return await Completar($"Corrige la ortografia y gramatica del siguiente texto, sin cambiar el sentido ni agregar nada: {texto}");
}

// Extrae datos estructurados como JSON, listo para deserializar.
async Task<string> ExtraerContacto(string texto) {
    return await Completar($"Extrae nombre, telefono y email del texto y responde SOLO un JSON con esas claves, sin markdown: {texto}");
}

// Genera preguntas de opcion multiple sobre un tema.
async Task<string> Examen(string tema) {
    return await Completar($"Genera 5 preguntas de opcion multiple sobre {tema}, con 4 opciones cada una y la respuesta correcta marcada.");
}

// Explica codigo paso a paso para un principiante.
async Task<string> Explicar(string codigo) {
    return await Completar($"Explica en castellano, paso a paso y para un principiante, que hace este codigo:\n{codigo}");
}

// Clasifica un ticket de soporte en categorias fijas.
async Task<string> Clasificar(string ticket) {
    return await Completar($"Clasifica este ticket en una de estas categorias: FACTURACION, TECNICO, VENTAS, OTRO. Responde solo la categoria:\n{ticket}");
}

// Muestra por consola y en `salida.md` para visualizar mejor el resultado. 
void Mostrar(string texto) {
    Console.WriteLine(texto);
    Console.WriteLine($"\n-- {(DateTime.Now - inicio).TotalSeconds:0.0}s ---------------\n");
    inicio = DateTime.Now;

    salida += $"---\n{texto}\n";
    File.WriteAllText("./40.0-salida.md", salida, Encoding.UTF8);
}

// Función genérica para completar cualquier indicación usando el modelo de lenguaje.
async Task<string> Completar(string indicacion) {
    var opciones = new OpenAIClientOptions { Endpoint = new Uri(URL) };
    IChatClient chat = new OpenAIClient(new ApiKeyCredential(API_KEY), opciones)
        .GetChatClient(MODELO).AsIChatClient();
    var respuesta = await chat.GetResponseAsync(indicacion);
    return respuesta.Text ?? "";
}
