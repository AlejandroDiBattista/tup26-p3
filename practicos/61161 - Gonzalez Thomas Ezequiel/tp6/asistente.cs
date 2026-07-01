#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

//layaout

var textoConversacion = new System.Text.StringBuilder();
textoConversacion.AppendLine("¡Hola! Soy tu asistente. Escribí un mensaje para comenzar.\n");

//panel superior

var panelConversacion = new Markdown {
    Width = Dim.Fill(), 
    Height = Dim.Fill() - 3,
    Text = textoConversacion.ToString()

};

//panel inferior

var campoTexto = new TextField {
    X = 0,
    Y = 0,
    Width = Dim.Fill() - 10,

 };
 
 var botonEnviar = new Button {
    X = Pos.Right(campoTexto),
    Y = 0,
    Title = "Enviar",
    
};

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
