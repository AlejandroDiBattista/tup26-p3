#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load(".env.mio");

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "no-requiere-key";
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient clienteBase = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();
 
IChatClient chat = new ChatClientBuilder(clienteBase)
    .UseFunctionInvocation()
    .Build();

string sistemaPrompt = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "Sos un asistente de programación. Respondé en español.";
 
List<ChatMessage> historial = [
    new(ChatRole.System, sistemaPrompt)
];
 
ChatOptions opcionesChat = new() {
    Tools = [
        AIFunctionFactory.Create(Herramientas.ListarArchivos, new() {
            Name        = "listar-archivos",
            Description = "Lista los archivos y carpetas de un directorio."
        }),
        AIFunctionFactory.Create(Herramientas.LeerArchivo, new() {
            Name        = "leer-archivo",
            Description = "Devuelve el contenido de un archivo de texto."
        }),
        AIFunctionFactory.Create(Herramientas.EscribirArchivo, new() {
            Name        = "escribir-archivo",
            Description = "Crea o sobrescribe un archivo con el contenido indicado."
        }),
    ]
};

Console.OutputEncoding = Encoding.UTF8;
 
using IApplication app = Application.Create().Init();
app.Run(new VentanaAsistente(chat, historial, opcionesChat, modelo));
