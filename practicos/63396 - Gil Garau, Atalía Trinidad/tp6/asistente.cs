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
    new(ChatRole.System, File.ReadAllText("AGENTS.md")),
    
];

using IApplication app = Application.Create().Init();
using var ventana = new Window
{
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};
var historial = new Markdown
{
    Text = "# Conversación\n\nEsperando tu mensaje...",
    Width = Dim.Fill(), Height = Dim.Fill(3)
};

var entrada = new TextField
{
    Width = Dim.Fill(12),
    X = 1, Y = Pos.Bottom(historial) + 1
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.Bottom(historial) + 1
};

async Task EnviarMensaje()
{
    var texto = entrada.Text?.ToString()?.Trim() ?? "";
    if (texto == "")
        return;

    entrada.Text = "";
    mensajes.Add(new(ChatRole.User, texto));

    historial.Text =
        "# Vos\n\n" + texto + "\n\n" +
        historial.Text;

    var respuesta = await chat.GetResponseAsync(mensajes);

    mensajes.Add(new(ChatRole.Assistant, respuesta.Text));

    historial.Text =
        historial.Text +
        "\n\n# Asistente\n\n" + respuesta.Text;
}

enviar.Accepting += async (_, e) =>
{
    e.Handled = true;
    await EnviarMensaje();
};

entrada.KeyDown += async (_, key) =>
{
    if (key == Key.Enter)
    {
        await EnviarMensaje();
    }
};

var respuesta = await chat.GetResponseAsync(mensajes);




app.Run(ventana);

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

