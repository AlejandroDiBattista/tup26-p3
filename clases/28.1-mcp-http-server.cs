#:sdk Microsoft.NET.Sdk.Web

using System.IO;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5002");
var app = builder.Build();

app.MapPost("/mcp", async (HttpContext context) => {
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = JsonNode.Parse(body)!;

    if (request["id"] is null) {
        return Results.BadRequest("JSON-RPC requiere un campo 'id'.");
    }

    var id = request["id"]!.ToJsonString();
    var method = request["method"]!.GetValue<string>();

    var response = method switch {
        "initialize" => OkResponse(id, """
            { "protocolVersion": "2025-06-18", "capabilities": { "tools": {} }, "serverInfo": { "name": "mini-http-mcp", "version": "1.0" } }
            """),

        "tools/list" => OkResponse(id, """
            { "tools": [ { "name": "saludar", "description": "Saluda a una persona.", "inputSchema": { "type": "object", "properties": { "nombre": { "type": "string" } }, "required": ["nombre"] } } ] }
            """),

        "tools/call" => OkResponse(id, ToolResult(CallTool(request))),

        _ => OkResponse(id, "{}")
    };

    return Results.Content(response, "application/json");
});

app.Run();

static string CallTool(JsonNode request) {
    var nombre = request["params"]!["arguments"]!["nombre"]!.GetValue<string>();
    return $"Hola, {nombre}! (desde HTTP)";
}

static string ToolResult(string text) {
    return $$"""{ "content": [ { "type": "text", "text": "{{text}}" } ] }""";
}

static string OkResponse(string id, string result) {
    return $$"""{ "jsonrpc": "2.0", "id": {{id}}, "result": {{result}} }""";
}
