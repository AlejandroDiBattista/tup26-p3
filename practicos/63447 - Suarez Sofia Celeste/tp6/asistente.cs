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

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md")),
];

string historialMarkdown = "";
bool enviando = false;

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var panelMensajes = new FrameView()
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3)
};

var panelEntrada = new FrameView()
{
    X = 0,
    Y = Pos.Bottom(panelMensajes),
    Width = Dim.Fill(),
    Height = 3
};

var conversacion = new Markdown
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Text = "Asistente IA\n\nEsperando mensaje..."
};

var input = new TextField
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(12)
};

var botonEnviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(input) + 1,
    Y = 0
};

panelMensajes.Add(conversacion);
panelEntrada.Add(input);
panelEntrada.Add(botonEnviar);
ventana.Add(panelMensajes);
ventana.Add(panelEntrada);

botonEnviar.Accepted += async (sender, e) =>
{
    await EnviarMensaje();
};


input.KeyDown += async (sender, key) =>
{
    if (key == Key.Enter)
    {
        await EnviarMensaje();
    }
};


ventana.KeyDown += (sender, key) =>
{
    if (key == Key.Esc)
    {
        app.RequestStop();
    }
};

async Task EnviarMensaje()
{
    if (enviando)
      return;
      enviando = true;

    var texto = input.Text?.ToString()?.Trim();
    if (string.IsNullOrWhiteSpace(texto))
    {
        enviando = false;
        return;
    }
    
    input.Text = "";
    mensajes.Add(new ChatMessage(ChatRole.User, texto));


    historialMarkdown += $"\n# Usuario\n\n{texto}\n";
    conversacion.Text = historialMarkdown;

    input.Enabled = false;
    botonEnviar.Enabled = false;

    string respuestaCompleta = "";

    await foreach (var update in chat.GetStreamingResponseAsync(mensajes))
    {
        respuestaCompleta += update.Text ?? "";
        app.Invoke(() =>
        {
            conversacion.Text = historialMarkdown + "\n# Asistente\n\n" + respuestaCompleta;
        });
    }

    historialMarkdown += $"\n# Asistente\n\n{respuestaCompleta}\n";
    conversacion.Text = historialMarkdown;

    mensajes.Add( new ChatMessage(ChatRole.Assistant, respuestaCompleta));

    input.Enabled = true;
    botonEnviar.Enabled = true;

    input.SetFocus();
    enviando = false;
}

app.Run(ventana);
