#!/usr/bin/env -S dotnet run
#:package DotNetEnv@3.1.1
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@1.17.1
#:property Nullable=enable
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui;

// Cargamos variables desde .env
DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

List<ChatMessage> mensajes = File.Exists("historial.json") 
    ? JsonSerializer.Deserialize<List<ChatMessage>>(File.ReadAllText("historial.json")) 
    : [new(ChatRole.System, File.Exists("AGENTS.md") ? File.ReadAllText("AGENTS.md") : "Sos un asistente útil.")];

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? throw new Exception("Falta GROQ_API_KEY")),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

Application.Init();
var ventana = new Window($" AsistenteIA · Groq ({modelo}) ") { Width = Dim.Fill(), Height = Dim.Fill() };

var vistaMarkdown = new TextView { Width = Dim.Fill(), Height = Dim.Fill() - 2, Text = "Escribí y presioná Enviar." };
var campoTexto    = new TextField { X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill() - 10, Height = 1 };
var botonEnviar   = new Button { X = Pos.Right(campoTexto), Y = Pos.AnchorEnd(1), Text = "Enviar" };

botonEnviar.Clicked += async () => {
    var texto = campoTexto.Text.ToString();
    if (string.IsNullOrWhiteSpace(texto)) return;
    
    mensajes.Add(new ChatMessage(ChatRole.User, texto));
    campoTexto.Text = "";
    
    try {
        string respuesta = "";
        await foreach (var frag in chat.GetStreamingResponseAsync(mensajes)) {
            respuesta += frag.Text;
            vistaMarkdown.Text = respuesta;
        }
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuesta));
        File.WriteAllText("historial.json", JsonSerializer.Serialize(mensajes));
    } catch (Exception ex) {
        vistaMarkdown.Text = $"Error: {ex.Message}";
    }
};

ventana.Add(vistaMarkdown, campoTexto, botonEnviar);
Application.Run(ventana);
Application.Shutdown();
