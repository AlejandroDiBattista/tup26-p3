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
Console.WriteLine($"Proveedor: {proveedor}");
Console.WriteLine($"URL: {url}");
Console.WriteLine($"Modelo: {modelo}");
Console.WriteLine($"API Key cargada: {!string.IsNullOrWhiteSpace(apiKey)}");

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url!) })
    .GetChatClient(modelo)
    .AsIChatClient();

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};

using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var historial = new Markdown
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3)
};

var entrada = new TextField
{
    X = 0,
    Y = Pos.AnchorEnd(1),
    Width = Dim.Fill(12)
};

var botonEnviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada),
    Y = Pos.AnchorEnd(1)
};

ventana.Add(historial);
ventana.Add(entrada);
ventana.Add(botonEnviar);

async Task EnviarMensaje()
{
    var texto = entrada.Text?.ToString()?.Trim();

    if (string.IsNullOrWhiteSpace(texto))
        return;

    entrada.Text = "";

    mensajes.Add(new ChatMessage(ChatRole.User, texto));

    historial.Text += $"\n# Vos\n\n{texto}\n";

    botonEnviar.Enabled = false;
    entrada.Enabled = false;

  try
{
    historial.Text += "\n# Asistente\n\n";

    string respuestaCompleta = "";

    await foreach (var update in chat.GetStreamingResponseAsync(mensajes))
    {
        respuestaCompleta += update.Text;

        historial.Text =
            historial.Text.ToString() + update.Text;
    }

    historial.Text += "\n";

    mensajes.Add(
        new ChatMessage(
            ChatRole.Assistant,
            respuestaCompleta));
}
    catch(Exception ex)
    {
        historial.Text +=
            $"\n# Error\n\n{ex.Message}\n";
    }

    botonEnviar.Enabled = true;
    entrada.Enabled = true;
}
entrada.Accepting += async (_, _) =>
{
    await EnviarMensaje();
};

botonEnviar.Accepting += async (_, _) =>
{
    await EnviarMensaje();
};

app.Run(ventana);

