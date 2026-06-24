#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "groq").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen/qwen3-32b";

if(url == null){
    Console.WriteLine("falta la API url");
    return; }

IChatClient chat = new OpenAIClient(
new ApiKeyCredential(apiKey ?? "no-key"),
  new OpenAIClientOptions {
    Endpoint = new Uri(url)
  })
.GetChatClient(modelo)
.AsIChatClient();

var mensajes = new List<ChatMessage> {
  new(ChatRole.System , "responde en español.")
};

var logFile = File.AppendText("chat.log");
void Log(string texto) { logFile.WriteLine(texto); logFile.Flush(); }

using IApplication app = Application.Create().Init();

var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var chatBox = new TextView {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    ReadOnly = true,
    WordWrap = true,
    Text = "Asistente IA\n\n"
};

var entrada = new TextField {
    X = 0,
    Y = Pos.Bottom(chatBox),
    Width = Dim.Fill(10)
};

var boton = new Button {
    X = Pos.Right(entrada),
    Y = Pos.Bottom(chatBox),
    Text = "Enviar"
};

ventana.Add(chatBox, entrada, boton);

void Agregar(string texto)
{
    chatBox.Text += texto;
    chatBox.MoveEnd();
    chatBox.SetNeedsDraw();
}

bool ocupado = false;

async Task Enviar()
{
    if(ocupado)
        return;

    var texto = entrada.Text?.Trim();

    if(string.IsNullOrWhiteSpace(texto))
        return;

    
    ocupado = true;
    
    entrada.Text = "";

    Agregar($"Vos:\n{texto}\n\n");
    Log($"Vos: {texto}");

    
    mensajes.Add(new ChatMessage(ChatRole.User, texto));

   
    Agregar("IA:\n");

    try {
        var respuesta = await chat.GetResponseAsync(mensajes);
        Agregar(respuesta.Text + "\n\n");
        Log($"IA: {respuesta.Text}");
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuesta.Text));
    }
    catch(Exception e) {
        Agregar($"error: {e.Message}");
        Log($"error: {e.Message}");
    }

    ocupado = false;
}
