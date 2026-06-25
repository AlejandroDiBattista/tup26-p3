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

var proveedor = (args.Length > 0 ? args[0] : "ollama").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen2.5-coder:7b";

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

var vistaMarkdown = new Markdown {
    Width = Dim.Fill(),
    Height = Dim.Fill(2)
};

var campoEntrada = new TextField {
    X = 0,
    Y = Pos.AnchorEnd(1),
    Width = Dim.Fill(12)
};

var botonEnviar = new Button {
    Title = "Enviar",
    X = Pos.AnchorEnd(10),
    Y = Pos.AnchorEnd(1),
    IsDefault = true
};

ventana.Add(vistaMarkdown, campoEntrada, botonEnviar);

vistaMarkdown.Text = "Escribí un mensaje para comenzar.\n\n---\n\n";

app.Run(ventana);