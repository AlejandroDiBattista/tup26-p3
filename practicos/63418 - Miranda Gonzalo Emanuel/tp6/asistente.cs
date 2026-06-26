#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Microsoft.Extensions.AI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui;

DotNetEnv.Env.Load(".env.ejemplo");

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient clienteBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

    IChatClient chat = new ChatClientBuilder(clienteBase)
    .UseFunctionInvocation()
    .Build();

ChatOptions opcionesChat = new() {
    Tools = [
        AIFunctionFactory.Create(
            (string ruta) => File.ReadAllText(ruta), 
            "leer-archivo", 
            "Devuelve el contenido de un archivo de texto"),
        
        AIFunctionFactory.Create(
            (string ruta, string contenido) => { 
                File.WriteAllText(ruta, contenido); 
                return "Archivo guardado exitosamente."; 
            }, 
            "escribir-archivo", 
            "Crea o sobrescribe un archivo con el contenido indicado"),
        
        AIFunctionFactory.Create(
            (string ruta) => string.Join("\n", Directory.GetFileSystemEntries(ruta)), 
            "listar-archivos", 
            "Lista los archivos (y carpetas) de un directorio")
    ]
};

string textoAgente = "Sos un asistente útil.";
if (File.Exists("AGENTS.md")) {
    textoAgente = File.ReadAllText("AGENTS.md");
} else {
    Console.WriteLine("⚠️ No se encontró AGENTS.md. Usando texto por defecto...");
    Thread.Sleep(2000); 
}

List<ChatMessage> mensajes = [
    new(ChatRole.System, textoAgente),
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
    X = 1,
    Y = 0, 
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
        var stream = chat.GetStreamingResponseAsync(historialParaEnviar, opcionesChat);

        await foreach (var chunk in stream) {
            if (chunk.Text != null) {
                textoAcumulado += chunk.Text; 
                
                mensajes[mensajes.Count - 1] = new ChatMessage(ChatRole.Assistant, textoAcumulado);
                
                app.Invoke(() => {
                    ActualizarPantalla();
                });
            }
        }
    }
    catch (Exception ex) {
        textoAcumulado += $"\n\n**Error:** {ex.Message}";
        mensajes[mensajes.Count - 1] = new ChatMessage(ChatRole.Assistant, textoAcumulado);
        app.Invoke(() => ActualizarPantalla());
    }

    app.Invoke(() => {
        inputTexto.Enabled = true;
        btnEnviar.Enabled = true;
        inputTexto.SetFocus(); 
    });
};;

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