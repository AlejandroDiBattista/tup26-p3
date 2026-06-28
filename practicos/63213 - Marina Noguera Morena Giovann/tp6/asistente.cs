using System;
using System.ClientModel;
using System.Collections.Generic;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;
using Terminal.Gui;

DotNetEnv.Env.Load();

var proveedorSolicitado = args.Length > 0 ? args[0] : "GEMINI";
var proveedor = proveedorSolicitado.ToUpperInvariant();
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "llama-3.3-70b-versatile";

if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(apiKey))
{
    var fallbackProveedor = "GEMINI";
    url = Environment.GetEnvironmentVariable($"{fallbackProveedor}_API_URL");
    apiKey = Environment.GetEnvironmentVariable($"{fallbackProveedor}_API_KEY");
    modelo = Environment.GetEnvironmentVariable($"{fallbackProveedor}_MODEL") ?? modelo;
}

if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Faltan variables de entorno. Revisa el archivo .env.");
    return;
}

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.Exists("AGENTS.md") ? File.ReadAllText("AGENTS.md") : "Sos un asistente de programación útil y claro.")
};

Application.Init();

var win = new Window("Asistente IA")
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var tv = new TextView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(2),
    ReadOnly = true,
    Text = "Escribí tu mensaje y presioná Enviar.\n"
};

var tf = new TextField
{
    X = 0,
    Y = Pos.Bottom(tv),
    Width = Dim.Fill(10),
    Height = 1
};

var btn = new Button("Enviar")
{
    X = Pos.Right(tf),
    Y = Pos.Top(tf)
};

win.Add(tv, tf, btn);

async Task EnviarAsync()
{
    var texto = tf.Text?.ToString();
    if (string.IsNullOrWhiteSpace(texto))
    {
        return;
    }

    tv.Text += $"\n> Vos: {texto}\n> IA: ";
    tf.Text = "";
    mensajes.Add(new ChatMessage(ChatRole.User, texto));

    var respuesta = string.Empty;

    await foreach (var update in chat.GetStreamingResponseAsync(mensajes))
    {
        if (!string.IsNullOrEmpty(update.Text))
        {
            respuesta += update.Text;
            tv.Text += update.Text;
        }
    }

    mensajes.Add(new ChatMessage(ChatRole.Assistant, respuesta));
}

btn.Clicked += () => _ = EnviarAsync();
tf.KeyPress += (e) =>
{
    if (e.KeyEvent.Key == Key.Enter)
    {
        e.Handled = true;
        _ = EnviarAsync();
    }
};

Application.Run(win);
Application.Shutdown();