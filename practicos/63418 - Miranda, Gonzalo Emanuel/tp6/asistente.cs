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
using Terminal.Gui;

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
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), 
    Height = Dim.Fill()
};
var historialView = new Markdown {
    Text = "### Asistente MEAI\n\nEscribí un mensaje para comenzar.",
    Width = Dim.Fill(),
    Height = Dim.Fill() - 3
};

var panelInferior = new View {
    Y = Pos.AnchorEnd(3),
    Width = Dim.Fill(),
    Height = 3,
    BorderStyle = Terminal.Gui.Drawing.LineStyle.Single 
};
var inputTexto = new TextField {
    Width = Dim.Fill() - 14, 
    Height = 1
};

var btnEnviar = new Button {
    Title = "Enviar",
    X = Pos.AnchorEnd(12),
    Y = 0,
    IsDefault = true
};

btnEnviar.Accepting += async (s, e) => {
    var texto = inputTexto.Text;
    if (string.IsNullOrWhiteSpace(texto)) {
        e.Handled = true;
        return;
    }

    mensajes.Add(new ChatMessage(ChatRole.User, texto));
    var historialParaEnviar = mensajes.ToList();

    inputTexto.Text = "";
    inputTexto.Enabled = false;
    btnEnviar.Enabled = false;
    e.Handled = true;

    string textoAcumulado = "";
    mensajes.Add(new ChatMessage(ChatRole.Assistant, textoAcumulado));
    ActualizarPantalla();
    
    try {
        var stream = chat.GetStreamingResponseAsync(historialParaEnviar);

        // 4. Consumimos el stream fragmento a fragmento
        await foreach (var chunk in stream) {
            if (chunk.Text != null) {
                textoAcumulado += chunk.Text; 
                
                // Reemplazamos el último mensaje de la lista con la versión actualizada
                mensajes[mensajes.Count - 1] = new ChatMessage(ChatRole.Assistant, textoAcumulado);
                
                // Actualizamos la pantalla (Terminal.Gui v2 se encarga del redibujado solo)
                Application.Invoke(() => {
                    ActualizarPantalla();
                });
            }
        }
    }
    catch (Exception ex) {
        textoAcumulado += $"\n\n**Error:** {ex.Message}";
        mensajes[mensajes.Count - 1] = new ChatMessage(ChatRole.Assistant, textoAcumulado);
        Application.Invoke(() => ActualizarPantalla());
    }

    // 5. Desbloqueamos la UI cuando termina
    Application.Invoke(() => {
        inputTexto.Enabled = true;
        btnEnviar.Enabled = true;
        inputTexto.SetFocus(); 
    });
};

panelInferior.Add(inputTexto, btnEnviar);
ventana.Add(historialView, panelInferior);


// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

void ActualizarPantalla()
{
    var textoPantalla = "";
    
    foreach (var msg in mensajes)
    {
        if (msg.Role == ChatRole.System) continue;

        var nombre = msg.Role == ChatRole.User ? "Vos" : "Asistente";
        textoPantalla += $"### {nombre}\n{msg.Text}\n\n";
    }

    if (string.IsNullOrWhiteSpace(textoPantalla)) {
        textoPantalla = "### Asistente MEAI\n\nEscribí un mensaje para comenzar.";
    }
    
    historialView.Text = textoPantalla;
}

app.Run(ventana);
