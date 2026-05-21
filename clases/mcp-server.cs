using System.Text.Json.Nodes;

while (Console.ReadLine() is string line) {
    var request = JsonNode.Parse(line)!;

    // Las notificaciones no tienen id; no se responden.
    if (request["id"] is null) { continue; }

    var id = request["id"]!.ToJsonString();
    var method = request["method"]!.GetValue<string>();

    var response = method switch {
        "initialize" => Ok(id, """
            { "protocolVersion": "2025-06-18", "capabilities": { "tools": {} }, "serverInfo": { "name": "mini-mcp", "version": "1.0" } }
            """),

        "tools/list" => Ok(id, """
            { "tools": [ { "name": "saludar", "description": "Saluda a una persona.", "inputSchema": { "type": "object", "properties": { "nombre": { "type": "string" } }, "required": ["nombre"] } } ] }
            """),

        "tools/call" => Ok(id, ToolResult(CallTool(request))),

        _ => Ok(id, "{}")
    };

    Console.WriteLine(response);
    Console.Out.Flush();
}

static string CallTool(JsonNode request) {
    var nombre = request["params"]!["arguments"]!["nombre"]!.GetValue<string>();
    return $"Hola, {nombre}!";
}

static string ToolResult(string text) {
    return $$"""{ "content": [ { "type": "text", "text": "{{text}}" } ] }""";
}

static string Ok(string id, string result) {
    return $$"""{ "jsonrpc": "2.0", "id": {{id}}, "result": {{result}} }""";
}
