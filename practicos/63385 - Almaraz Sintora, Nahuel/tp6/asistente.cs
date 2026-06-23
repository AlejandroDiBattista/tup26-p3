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
using Terminal.Gui.Input;

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

var conversacion = new Markdown
{
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    CanFocus = true,
    Text = "# Asistente IA\n\nEscribi un mensaje y presiona Enter."
};
conversacion.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

var entrada = new TextField
{
    X = 0,
    Y = Pos.Bottom(conversacion),
    Width = Dim.Fill(12),
    Height = 1
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.Top(entrada),
    Width = 10,
    IsDefault = true
};

ventana.Add(conversacion, entrada, enviar);
entrada.SetFocus();
app.Run(ventana);
