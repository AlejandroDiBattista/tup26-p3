#:sdk Microsoft.NET.Sdk.Web
#:package ModelContextProtocol.AspNetCore@1.3.0

using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5003");

// Registrar el servidor MCP oficial
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MisHerramientas>();

var app = builder.Build();

// Mapear los endpoints del servidor MCP oficial en /mcp
app.MapMcp("/mcp");

await app.RunAsync();

[McpServerToolType]
public class MisHerramientas {
    [McpServerTool]
    [Description("Saluda a una persona.")]
    public string Saludar(string nombre) {
        return $"Hola, {nombre}! (desde el SDK oficial de Microsoft)";
    }
}
