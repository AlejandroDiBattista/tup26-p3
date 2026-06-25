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

var (chat, modelo) = CrearChatClient(args);

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

const int altoEntrada = 3;

var panelConversacion = new Markdown {
    Text = "# Asistente\n\nEscribí un mensaje para comenzar.",
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(altoEntrada)
};

var campoEntrada = new TextField {
    X = 0,
    Y = Pos.AnchorEnd(altoEntrada),
    Width = Dim.Fill(12),
    Height = 1
};

var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.AnchorEnd(11),
    Y = Pos.AnchorEnd(altoEntrada),
    Width = 11
};

ventana.Add(panelConversacion, campoEntrada, botonEnviar);

app.Run(ventana);

static (IChatClient chat, string modelo) CrearChatClient(string[] args)
{
    var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
    var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL")?? "https://api.openai.com/v1";
    var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
    var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

    IChatClient chat = new OpenAIClient(
            new ApiKeyCredential(apiKey ?? "no-requiere-key"),
            new OpenAIClientOptions { Endpoint = new Uri(url) })
        .GetChatClient(modelo)
        .AsIChatClient();

    return (chat, modelo);
}
