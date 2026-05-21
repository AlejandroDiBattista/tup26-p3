using HttpClient client = new();
// Crea un objeto HttpClient para poder enviar solicitudes HTTP desde este programa hacia una API web.

const string host = "http://localhost:5001";

Console.Clear();
Console.WriteLine("=== Cliente de Hola ===\n");

string respuesta = await client.GetStringAsync($"{host}/saludo");
Console.WriteLine("GET /saludo");
Console.WriteLine($"Respuesta: {respuesta}\n");   

respuesta = await client.GetStringAsync($"{host}/despedida");
Console.WriteLine("GET /despedida");
Console.WriteLine($"Respuesta: {respuesta}\n");