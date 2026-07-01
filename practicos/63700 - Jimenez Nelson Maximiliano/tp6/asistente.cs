#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-4o-mini"; // Cambiado a un modelo existente estándar

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = string.IsNullOrEmpty(url) ? null : new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

// Intentar leer el sistema de agentes, si no existe usamos un fallback para que no rompa
string systemPrompt = File.Exists("AGENTS.md") ? File.ReadAllText("AGENTS.md") : "Sos un asistente de IA útil.";

List<ChatMessage> mensajes = [
    new(ChatRole.System, systemPrompt)
];

// 1. Inicialización de la App de Terminal.Gui
using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

// 2. Panel de conversación (TextView histórico, de solo lectura)
var panelChat = new TextView {
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill() - 3, // Deja espacio abajo para el input
    ReadOnly = true,
    WordWrap = true,
    Text = "=== Sistema iniciado. Escribí tu mensaje abajo y presioná Enter ===\n\n"
};

// 3. Panel de entrada de texto
var inputChat = new TextField {
    X = 0,
    Y = Pos.Bottom(panelChat),
    Width = Dim.Fill(),
    Height = 1,
    Text = ""
};

ventana.Add(panelChat, inputChat);

// 4. Lógica para enviar mensajes y procesar el streaming
inputChat.KeyDown += async (s, e) => {
    // Escuchamos la tecla Enter
    if (e.KeyCode == Key.Enter) {
        var textoUsuario = inputChat.Text.ToString().Trim();
        if (string.IsNullOrEmpty(textoUsuario)) return;

        // Limpiar el input inmediatamente para feedback visual
        inputChat.Text = "";

        // Actualizar UI con el mensaje del usuario
        panelChat.Text += $"[Vos]: {textoUsuario}\n\n[Asistente]: ";
        panelChat.ScrollTo(panelChat.Lines - 1, 0);

        // Guardar en el historial
        mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));

        try {
            var respuestaBuilder = new StringBuilder();
            
            // Llamada asincrónica con streaming
            await foreach (var chunk in chat.GetStreamingResponseAsync(mensajes)) {
                if (chunk.Text != null) {
                    respuestaBuilder.Append(chunk.Text);
                    
                    // Ir agregando el texto a la pantalla en tiempo real
                    panelChat.Text += chunk.Text;
                    panelChat.ScrollTo(panelChat.Lines - 1, 0);
                    
                    // Forzar el redibujado de la interfaz para ver el efecto "tipeo"
                    Application.Refresh();
                }
            }

            panelChat.Text += "\n\n"; // Espaciado para el próximo mensaje
            
            // Guardar la respuesta completa del asistente en el historial
            mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaBuilder.ToString()));
        }
        catch (Exception ex) {
            panelChat.Text += $"\n[Error]: {ex.Message}\n\n";
        }
        
        panelChat.ScrollTo(panelChat.Lines - 1, 0);
    }
};

app.Run(ventana);