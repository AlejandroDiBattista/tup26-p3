using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using HttpClient client = new();
string url = "http://localhost:5002/mcp";

Console.WriteLine("=== INICIANDO CLIENTE MCP HTTP ===");

// 1. Enviar initialize
await SendRequest("initialize", """{ "protocolVersion": "2025-06-18", "capabilities": {}, "clientInfo": { "name": "mini-client", "version": "1.0" } }""");

// 2. Enviar tools/list
await SendRequest("tools/list", "null");

// 3. Enviar tools/call
await SendRequest("tools/call", """{ "name": "saludar", "arguments": { "nombre": "Ada Lovelace" } }""");

async Task SendRequest(string method, string @params) {
    string jsonPayload = $$"""
    {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "{{method}}",
        "params": {{@params}}
    }
    """;

    Console.WriteLine($"\nENVIANDO POST a: {url} para método '{method}'...");
    using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
    
    try {
        var response = await client.PostAsync(url, content);
        string responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("RESPUESTA RECIBIDA:");
        Console.WriteLine(responseBody);
    } catch (Exception ex) {
        Console.WriteLine($"Error de comunicación: {ex.Message}");
    }
}
