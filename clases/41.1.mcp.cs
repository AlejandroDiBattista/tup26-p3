#:package ModelContextProtocol@1.4.0
#:package Microsoft.Extensions.Hosting@10.0.0

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// En transporte stdio, stdout queda reservado para JSON-RPC.
// Cualquier log debe salir por stderr para no romper el protocolo MCP.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => {
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<Herramientas>();

await builder.Build().RunAsync();

[McpServerToolType]
public class Herramientas {
    [McpServerTool(Name = "saludar")]
    [Description("Saluda a una persona por su nombre.")]
    public string Saludar(
        [Description("Nombre de la persona a saludar.")] string nombre) {
        return $"Hola, {nombre}!";
    }
}
