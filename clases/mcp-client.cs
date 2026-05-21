using System.Diagnostics;
using System.Text.Json.Nodes;

using var server = Process.Start(new ProcessStartInfo("/usr/local/bin/dotnet", "mcp-server.cs") {
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    UseShellExecute = false
})!;

Send("""{ "jsonrpc": "2.0", "id": 1, "method": "initialize", "params": { "protocolVersion": "2025-06-18", "capabilities": {}, "clientInfo": { "name": "mini-client", "version": "1.0" } } }""");

Send("""{ "jsonrpc": "2.0", "id": 2, "method": "tools/list" }""");

Send("""{ "jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": { "name": "saludar", "arguments": { "nombre": "Ada" } } }""");

Console.WriteLine($"Resultado para 'Alejandro': {Saludar("Alejandro")}");
Console.WriteLine($"Resultado para 'Josefa'   : {Saludar("Josefa")}");

server.StandardInput.Close();
server.WaitForExit();

string Saludar(string nombre) {
    var request = $$"""{ "jsonrpc": "2.0", "id": 4, "method": "tools/call", "params": { "name": "saludar", "arguments": { "nombre": "{{nombre}}" } } }""";

    server.StandardInput.WriteLine(request);
    server.StandardInput.Flush();

    var response = server.StandardOutput.ReadLine();

    var json = JsonNode.Parse(response!)!.AsObject();
    return json["result"]!["content"]![0]!["text"]!.GetValue<string>();
}


void Send(string request) {
    Console.WriteLine("CLIENTE -> SERVIDOR");
    Console.WriteLine(request);

    server.StandardInput.WriteLine(request);
    server.StandardInput.Flush();

    Console.WriteLine("SERVIDOR -> CLIENTE");
    Console.WriteLine(server.StandardOutput.ReadLine());
    Console.WriteLine();
}
