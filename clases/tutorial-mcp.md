# MCP minimo con C# y consola

La idea central: un servidor MCP puede ser un programa de consola.

No hace falta HTTP, sockets ni framework. El cliente le escribe JSON por `stdin`; el servidor responde JSON por `stdout`.

## 0. El protocolo en una frase

MCP es un protocolo de mensajes.

En este ejemplo usamos:

- Formato de mensaje: `JSON-RPC 2.0`.
- Transporte: `stdio`, es decir, entrada y salida estandar.
- Servidor: `mcp-server.cs`.
- Cliente: `mcp-client.cs`.

La comunicacion se ve asi:

```text
cliente escribe una linea JSON  -> stdin  -> servidor
cliente lee una linea JSON      <- stdout <- servidor
```

Cada mensaje viaja como una linea de texto. Por eso el servidor hace esto:

```csharp
while (Console.ReadLine() is string line) {
    ...
}
```

Y el cliente hace esto:

```csharp
server.StandardInput.WriteLine(request);
var response = server.StandardOutput.ReadLine();
```

No hay magia: el protocolo es el formato del JSON; la consola es el canal.

## 1. Requests, responses y notifications

MCP usa mensajes JSON-RPC.

Un request tiene `id`, `method` y opcionalmente `params`:

```json
{ "jsonrpc": "2.0", "id": 1, "method": "tools/list" }
```

El servidor responde usando el mismo `id`:

```json
{ "jsonrpc": "2.0", "id": 1, "result": { "tools": [] } }
```

Ese `id` es lo que permite saber que respuesta corresponde a que pregunta.

Una notification es parecida, pero no tiene `id`:

```json
{ "jsonrpc": "2.0", "method": "notifications/initialized" }
```

Como no tiene `id`, no espera respuesta. Por eso el servidor minimo ignora mensajes sin `id`:

```csharp
if (request["id"] is null) { continue; }
```

## 2. Ciclo minimo de una conexion MCP

El camino feliz de una conexion MCP es:

1. El cliente arranca el servidor.
2. El cliente manda `initialize`.
3. El servidor responde version, capacidades e informacion del servidor.
4. En un cliente completo, el cliente manda `notifications/initialized`.
5. El cliente pide `tools/list`.
6. El servidor responde que herramientas tiene.
7. El cliente manda `tools/call`.
8. El servidor ejecuta la herramienta y devuelve el resultado.
9. El cliente cierra `stdin`; el servidor termina.

En nuestro ejemplo, los tres requests que mostramos son:

```text
initialize
tools/list
tools/call
```

La notification `notifications/initialized` no espera respuesta. Por eso no aparece en el cliente minimo, pero el servidor ya esta preparado para ignorarla porque no tiene `id`.

## 3. Que hace el servidor

El servidor MCP es el programa que expone capacidades.

En este tutorial expone una sola herramienta:

```text
saludar(nombre) -> "Hola, nombre!"
```

El servidor no decide cuando ejecutar la herramienta. Espera que el cliente se lo pida con:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "saludar",
    "arguments": {
      "nombre": "Ada"
    }
  }
}
```

Y responde:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      { "type": "text", "text": "Hola, Ada!" }
    ]
  }
}
```

Regla importante: si el transporte es `stdio`, el servidor no debe imprimir logs ni texto suelto por `stdout`.

`stdout` queda reservado para mensajes MCP validos. Si hace falta loguear, usar `stderr`.

## 4. Que hace el cliente

El cliente MCP es el programa que usa el servidor.

En este tutorial el cliente:

- Arranca el servidor como proceso hijo.
- Le escribe mensajes JSON por `StandardInput`.
- Lee respuestas JSON por `StandardOutput`.
- Muestra el intercambio en pantalla.

Esta linea crea el proceso:

```csharp
using var server = Process.Start(new ProcessStartInfo("/usr/local/bin/dotnet", "mcp-server.cs") {
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    UseShellExecute = false
})!;
```

Lo importante son estas dos opciones:

```csharp
RedirectStandardInput = true,
RedirectStandardOutput = true
```

Sin eso, el cliente no podria hablar con el servidor por consola.

## 5. Uso como servidor real

Este mismo `mcp-server.cs` podria ser ejecutado por un host MCP real.

Un host MCP es una aplicacion que coordina uno o mas servidores MCP. Por ejemplo, un editor, una aplicacion de escritorio o una herramienta de IA.

La configuracion exacta depende del host, pero conceptualmente seria algo asi:

```json
{
  "mcpServers": {
    "mini-mcp": {
      "command": "dotnet",
      "args": ["mcp-server.cs"]
    }
  }
}
```

El host hace lo mismo que nuestro cliente minimo:

1. Ejecuta el comando.
2. Abre `stdin` y `stdout`.
3. Manda `initialize`.
4. Descubre herramientas con `tools/list`.
5. Llama herramientas con `tools/call`.

La diferencia es que un host real conecta esas herramientas con una interfaz de usuario o con un modelo de IA.

## 6. Uso como cliente de prueba

Nuestro `mcp-client.cs` no es un cliente MCP completo.

Es una herramienta didactica para ver el protocolo funcionando.

Sirve para mostrar que MCP por consola no requiere nada especial:

- Un proceso.
- Dos pipes.
- Mensajes JSON.

Cuando se entienda eso, se puede reemplazar el cliente de prueba por cualquier host MCP real.

## 7. Crear el servidor

Archivo: `mcp-server.cs`

```csharp
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
```

Este servidor hace tres cosas:

- Lee una linea desde la consola.
- Interpreta esa linea como JSON.
- Responde otro JSON por la consola.

Eso es suficiente para mostrar el camino feliz de MCP por `stdio`.

## 8. Probar el servidor a mano

Ejecutar:

```bash
dotnet mcp-server.cs
```

Pegar este JSON y presionar Enter:

```json
{ "jsonrpc": "2.0", "id": 1, "method": "initialize", "params": { "protocolVersion": "2025-06-18", "capabilities": {}, "clientInfo": { "name": "mini-client", "version": "1.0" } } }
```

El servidor responde otro JSON con informacion basica del servidor.

Ahora pedir la lista de herramientas:

```json
{ "jsonrpc": "2.0", "id": 2, "method": "tools/list" }
```

El servidor responde que tiene una herramienta llamada `saludar`.

Finalmente llamar a la herramienta:

```json
{ "jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": { "name": "saludar", "arguments": { "nombre": "Ada" } } }
```

Respuesta esperada:

```json
{ "jsonrpc": "2.0", "id": 3, "result": { "content": [ { "type": "text", "text": "Hola, Ada!" } ] } }
```

## 9. Crear el cliente

Archivo: `mcp-client.cs`

```csharp
using System.Diagnostics;

using var server = Process.Start(new ProcessStartInfo("/usr/local/bin/dotnet", "mcp-server.cs") {
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    UseShellExecute = false
})!;

Send("""{ "jsonrpc": "2.0", "id": 1, "method": "initialize", "params": { "protocolVersion": "2025-06-18", "capabilities": {}, "clientInfo": { "name": "mini-client", "version": "1.0" } } }""");

Send("""{ "jsonrpc": "2.0", "id": 2, "method": "tools/list" }""");

Send("""{ "jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": { "name": "saludar", "arguments": { "nombre": "Ada" } } }""");

server.StandardInput.Close();
server.WaitForExit();

void Send(string request) {
    Console.WriteLine("CLIENTE -> SERVIDOR");
    Console.WriteLine(request);

    server.StandardInput.WriteLine(request);
    server.StandardInput.Flush();

    Console.WriteLine("SERVIDOR -> CLIENTE");
    Console.WriteLine(server.StandardOutput.ReadLine());
    Console.WriteLine();
}
```

## 10. Ejecutar todo

```bash
dotnet mcp-client.cs
```

El cliente:

- Arranca el servidor como un proceso hijo.
- Escribe requests JSON en el `stdin` del servidor.
- Lee responses JSON desde el `stdout` del servidor.
- Imprime el intercambio completo en pantalla.

## Idea clave

MCP por `stdio` es simplemente esto:

```text
cliente escribe JSON  -> stdin  -> servidor
cliente lee JSON      <- stdout <- servidor
```

El protocolo define que JSON mandar.

La consola nos da el transporte.
