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

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
