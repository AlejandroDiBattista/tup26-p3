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


var urlBase = url!.EndsWith("/chat/completions") ? url[..^"/chat/completions".Length] : url;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(urlBase) })
    .GetChatClient(modelo)
    .AsIChatClient();


List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

string textoConversacion = "# Asistente IA\n\nEscribí un mensaje para comenzar.";

using IApplication app = Application.Create().Init();

using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown {
    Text = textoConversacion,
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3)
};

var entrada = new TextField {
    X = 0,
    Y = Pos.AnchorEnd(3),
    Width = Dim.Fill(12),
    Height = 1
};

var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.AnchorEnd(3),
    Width = 10
};

void EnviarMensaje()
{
    string texto = entrada.Text?.ToString() ?? "";

    if (string.IsNullOrWhiteSpace(texto)) {
        return;
    }

    textoConversacion += $"\n\n# Vos\n\n{texto}";
    conversacion.Text = textoConversacion;

    entrada.Text = "";
    entrada.SetFocus();
}

botonEnviar.Accepting += (sender, e) => {
    EnviarMensaje();
};

entrada.Accepting += (sender, e) => {
    EnviarMensaje();
};

ventana.Add(conversacion, entrada, botonEnviar);

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
