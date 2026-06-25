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
// Define proveedor a usar en este caso Gemini
var proveedor = (args.Length > 0 ? args[0] : "gemini").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gemini-2.5-flash";
// Lee el mensaje del sistema que marca el comportamiento inicial 

var promptSistema = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "Sos un asistente de programacion. Responde en espanol.";

// Inicializa Terminal.Gui y crea la ventana principal a pantalla completa.
using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" Asistente IA - {proveedor} - {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

// Panel superior: muestra toda la conversacion con formato Markdown.
var panelConversacion = new FrameView
{
    Title = " Conversacion ",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(4)
};

// Vista Markdown: permite mostrar titulos, listas y bloques de codigo en la terminal.
var vistaConversacion = new Markdown
{
    Text = "# Asistente IA\n\nConfigura `.env` y escribi tu consulta abajo.\n\n",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ShowHeadingPrefix = false,
    ShowCopyButtons = true
};

panelConversacion.Add(vistaConversacion);

// Panel inferior: contiene el campo donde escribe el usuario y el boton de envio.
var panelEntrada = new FrameView
{
    Title = " Mensaje ",
    X = 0,
    Y = Pos.Bottom(panelConversacion),
    Width = Dim.Fill(),
    Height = 4
};

// Campo de texto donde el usuario escribe su pregunta.
var entrada = new TextField
{
    X = 1,
    Y = 0,
    Width = Dim.Fill(13),
    Height = 1,
    CanFocus = true
};

// Boton que dispara el mismo envio que la tecla Enter.
var botonEnviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = 0,
    Width = 10,
    Height = 1
};

panelEntrada.Add(entrada, botonEnviar);
ventana.Add(panelConversacion, panelEntrada);

app.Run(ventana);
