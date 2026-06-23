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

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    Text = "# Asistente IA\n\nListo para conversar."
};

var entrada = new TextField
{
    X = 0,
    Y = Pos.Bottom(conversacion),
    Width = Dim.Fill(12)
};

var botonEnviar = new Button
{
    X = Pos.Right(entrada) + 1,
    Y = Pos.Bottom(conversacion),
    Text = "Enviar"
};

string historialMarkdown = "# Asistente IA\n\n";

async Task EnviarMensaje()
{
    var texto = entrada.Text?.ToString()?.Trim();

    Console.WriteLine($"Enviando: {texto}");

    if (string.IsNullOrWhiteSpace(texto))
        return;

    mensajes.Add(new ChatMessage(ChatRole.User, texto));

    historialMarkdown += $"# Vos\n\n{texto}\n\n";

    conversacion.Text = historialMarkdown;

    entrada.Text = "";

    try
    {
        string respuestaCompleta = "";

        historialMarkdown += "# Asistente\n\n";

        await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes))
        {
            respuestaCompleta += fragmento.Text;

            conversacion.Text = historialMarkdown + respuestaCompleta;
        }

        historialMarkdown += respuestaCompleta + "\n\n";

        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta));

        conversacion.Text = historialMarkdown;
    }
    catch (Exception ex)
    {
        File.WriteAllText("error.txt", ex.ToString());
        Console.WriteLine(ex);

        // IMPORTANTE: no dejar que cierre la app silenciosamente
        conversacion.Text = "ERROR:\n\n" + ex.Message;
    }
}

botonEnviar.Accepting += async (sender, e) =>
{
    try
    {
        await EnviarMensaje();
    }
    catch (Exception ex)
    {
        Console.Clear();
        Console.WriteLine(ex);
        Console.WriteLine("\nPresione ENTER...");
        Console.ReadLine();
    }
};

ventana.Add(conversacion);
ventana.Add(entrada);
ventana.Add(botonEnviar);

app.Run(ventana);
