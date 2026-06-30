#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.7.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false
#:package Google.GenAI

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

// ------------------config -------------------------------------
var url    = Environment.GetEnvironmentVariable("GEMINI_API_URL");
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var modelo = Environment.GetEnvironmentVariable("GEMINI_MODEL");

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey!),
        new OpenAIClientOptions { Endpoint = new Uri(url!) })
    .GetChatClient(modelo)
    .AsIChatClient();


// ---------------------------------------------------------------

const string pregunta = "Definí recursividad";

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md")),
    new(ChatRole.User, pregunta)
];

var respuesta = await chat.GetResponseAsync(mensajes);

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

ventana.Add(new Markdown {
    Text = $"# Vos\n\n{pregunta}\n\n# Asistente\n\n{respuesta.Text}",
    Width = Dim.Fill(), Height = Dim.Fill()
});

// Panel de conversación y el panel de entrada.







// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).




app.Run(ventana);
