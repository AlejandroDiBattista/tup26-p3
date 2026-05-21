using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// 1. Instanciamos un cliente HTTP para interactuar con APIs externas
using HttpClient client = new();

Console.WriteLine("=== 1. Inicio de ejecución sincrónica/secuencial ===");
var cronometro = Stopwatch.StartNew();

// Realizamos tres llamadas secuenciales (bloqueamos conceptualmente el flujo hasta obtener cada una)
Console.WriteLine("Descargando Post 1...");
string post1 = await client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");

Console.WriteLine("Descargando Post 2...");
string post2 = await client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/2");

Console.WriteLine("Descargando Post 3...");
string post3 = await client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/3");

cronometro.Stop();
Console.WriteLine($"Llamadas secuenciales completadas en: {cronometro.ElapsedMilliseconds} ms\n");

// =========================================================================

Console.WriteLine("=== 2. Inicio de ejecución paralela (Task.WhenAll) ===");
cronometro.Restart();

// Disparamos las tres tareas al mismo tiempo (sin 'await' inmediato)
Task<string> tarea1 = client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
Task<string> tarea2 = client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/2");
Task<string> tarea3 = client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/3");

// Esperamos que todas las tareas en curso finalicen juntas
string[] resultados = await Task.WhenAll(tarea1, tarea2, tarea3);

cronometro.Stop();
Console.WriteLine($"Llamadas paralelas completadas en: {cronometro.ElapsedMilliseconds} ms (mucho más rápido!)");
Console.WriteLine($"Longitud de caracteres recibidos: {resultados[0].Length + resultados[1].Length + resultados[2].Length}\n");

// =========================================================================

Console.WriteLine("=== 3. Manejo de excepciones asincrónicas ===");
try {
    Console.WriteLine("Intentando acceder a una URL inexistente...");
    string errorContent = await client.GetStringAsync("https://sitio-que-no-existe-para-prueba.com");
} catch (HttpRequestException ex) {
    Console.WriteLine($"✓ Excepción capturada correctamente: {ex.Message}\n");
}

// =========================================================================

Console.WriteLine("=== 4. Cancelación de tareas por Timeout ===");
// Creamos una fuente de cancelación que aborta la operación si pasa más de 1 milisegundo (para forzar la cancelación)
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

try {
    Console.WriteLine("Disparando petición HTTP con tiempo límite extremadamente corto...");
    string rapido = await client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1", cts.Token);
} catch (OperationCanceledException) {
    Console.WriteLine("✓ Operación cancelada exitosamente por Timeout!");
}

Console.WriteLine("\n=== Fin de demostración ===");
