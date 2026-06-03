#:sdk Microsoft.NET.Sdk
#:package ModelContextProtocol@1.3.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

Console.WriteLine("=== INICIANDO CLIENTE MCP OFICIAL DE .NET ===");

// 1. Configuramos el transporte HTTP apuntando al endpoint de la API
var opciones = new HttpClientTransportOptions {
    Endpoint = new Uri("http://localhost:5003/mcp")
};
var transporte = new HttpClientTransport(opciones);

try {
    // 2. Creamos y conectamos el cliente
    await using var cliente = await McpClient.CreateAsync(transporte);
    Console.WriteLine("✓ Conexión establecida de forma exitosa.");

    // 3. Listamos las herramientas del servidor (retorna IList<McpClientTool> directamente)
    var respuestaTools = await cliente.ListToolsAsync();
    Console.WriteLine("\nHerramientas descubiertas:");
    foreach (var tool in respuestaTools) {
        Console.WriteLine($"- {tool.Name}: {tool.Description}");
    }

    // 4. Invocamos la herramienta 'saludar' (en minúsculas como se registró)
    Console.WriteLine("\nLlamando a la herramienta 'saludar'...");
    var argumentos = new Dictionary<string, object?> {
        ["nombre"] = "Ada Lovelace"
    };
    var resultado = await cliente.CallToolAsync("saludar", argumentos);
    
    Console.WriteLine("\nResultado del servidor:");
    var textBlock = resultado.Content.OfType<TextContentBlock>().FirstOrDefault();
    if (textBlock != null) {
        Console.WriteLine(textBlock.Text);
    } else {
        Console.WriteLine("No se recibió contenido de texto.");
    }

} catch (Exception ex) {
    Console.WriteLine($"Ocurrió un error: {ex.Message}");
}
