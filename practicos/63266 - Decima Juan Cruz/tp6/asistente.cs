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
var url = NormalizarEndpoint(Environment.GetEnvironmentVariable($"{proveedor}_API_URL")
    ?? "https://api.groq.com/openai/v1/chat/completions");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "sin-api-key";
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen/qwen3.6-27b";

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};

var opciones = new ChatOptions
{
    Tools = CrearHerramientasDeArchivos()
};

var turnos = new List<TurnoMostrado>();

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown
{
    Text = "# Asistente IA\n\nListo para conversar.",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    CanFocus = true
};

var entrada = new TextField
{
    X = 0,
    Y = Pos.AnchorEnd(3),
    Width = Dim.Fill(12),
    Height = 1
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.AnchorEnd(10),
    Y = Pos.AnchorEnd(3),
    Width = 10,
    Height = 1,
    IsDefault = true
};

var estado = new Label
{
    Text = "Enter: enviar | Esc: salir",
    X = 0,
    Y = Pos.AnchorEnd(2),
    Width = Dim.Fill(),
    Height = 1
};

ventana.Add(conversacion, entrada, enviar, estado);
entrada.SetFocus();



app.Run(ventana);
