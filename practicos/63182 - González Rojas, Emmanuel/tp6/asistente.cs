#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.7.0
#:package Microsoft.Extensions.AI.OpenAI@10.7.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


// =================== CONFIGURACION ====================
DotNetEnv.Env.Load();

var urlApi   = Environment.GetEnvironmentVariable("GEMINI_API_URL") ?? "";
var clave    = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
var modeloIA = Environment.GetEnvironmentVariable("GEMINI_MODEL")   ?? "gemini-2.5-flash";

if (string.IsNullOrEmpty(clave)) {
    Console.Error.WriteLine("Falta GEMINI_API_KEY en .env ");
    Environment.Exit(1);
}


// ================== CLIENTE DE IA ====================

IChatClient clienteBase = new OpenAIClient(
        new ApiKeyCredential(clave),
        new OpenAIClientOptions { Endpoint = new Uri(urlApi) })
    .GetChatClient(modeloIA)
    .AsIChatClient();

IChatClient cliente = new ChatClientBuilder(clienteBase)
    .UseFunctionInvocation()
    .Build();

// ================== HERRAMIENTAS PARA ARCHIVOS ====================
[Description("Lee el contenido de un archivo")]
static string LeerArchivo([Description("Ruta del archivo")] string ruta) =>
    File.Exists(ruta) ? File.ReadAllText(ruta) : $"No existe: {ruta}";

[Description("Crea o sobreescribe un archivo con el texto dado")]
static string EscribirArchivo(
    [Description("Ruta")] string ruta,
    [Description("Contenido")] string contenido) {
    File.WriteAllText(ruta, contenido);
    return $"Guardado: {ruta}";
}

[Description("Lista archivos y carpetas de un directorio")]
static string ListarArchivos([Description("Ruta (vacío = actual)")] string ruta = "") {
    var carpeta = string.IsNullOrEmpty(ruta) ? "." : ruta;
    var items   = Directory.GetFileSystemEntries(carpeta);
    return items.Length == 0 ? "Vacío." : string.Join("\n", items);
}

var opciones = new ChatOptions {
    Tools = [
        AIFunctionFactory.Create(LeerArchivo,      "leer-archivo"),
        AIFunctionFactory.Create(EscribirArchivo,  "escribir-archivo"),
        AIFunctionFactory.Create(ListarArchivos,   "listar-archivos")
    ]
};

// ================== HISTORIAL Y GUARDADO DE CONVERSACION ====================
var historial = new List<ChatMessage> {
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};

var archivoSalida = "salida.md";
File.WriteAllText(archivoSalida,
    $"# AsistenteIA\nModelo: {modeloIA}\nFecha: {DateTime.Now:dd/MM/yyyy HH:mm}\n---\n\n");