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
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL");

if (string.IsNullOrWhiteSpace(url)) {
    Console.Error.WriteLine($"Falta configurar {proveedor}_API_URL en el archivo .env");
    return;
}

if (string.IsNullOrWhiteSpace(apiKey) && proveedor != "OLLAMA") {
    Console.Error.WriteLine($"Falta configurar {proveedor}_API_KEY en el archivo .env");
    return;
}

if (string.IsNullOrWhiteSpace(modelo)) {
    Console.Error.WriteLine($"Falta configurar {proveedor}_MODEL en el archivo .env");
    return;
}

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

const string pregunta = "Definí recursividad";

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md")),
    new(ChatRole.User, pregunta)
];

string ObtenerConversacion() {
    return string.Join("\n\n", mensajes.Select(m => {
        var titulo = m.Role == ChatRole.User
            ? "# Vos"
            : m.Role == ChatRole.Assistant
                ? "# Asistente"
                : "";

        return titulo == ""
            ? ""
            : $"{titulo}\n\n{m.Text}";
    }));
}

var respuesta = await chat.GetResponseAsync(mensajes);

mensajes.Add(new ChatMessage(ChatRole.Assistant, respuesta.Text));

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    Text = ObtenerConversacion()
};

var entrada = new TextField {
    X = 0,
    Y = Pos.AnchorEnd(2),
    Width = Dim.Fill(14),
    Height = 1
};

var botonEnviar = new Button {
    X = Pos.AnchorEnd(12),
    Y = Pos.AnchorEnd(2),
    Text = "Enviar"

};

botonEnviar.Accepting += async (_, _) =>
{
    var texto = entrada.Text.ToString();

    if (string.IsNullOrWhiteSpace(texto))
        return;

    entrada.Text = "";
    entrada.Enabled = false;
    botonEnviar.Enabled = false;

    mensajes.Add(new ChatMessage(ChatRole.User, texto));
    conversacion.Text = ObtenerConversacion();

    var respuestaCompleta = "";

    mensajes.Add(new ChatMessage(ChatRole.Assistant, ""));

    await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes))
    {
        respuestaCompleta += fragmento.Text;

        mensajes[^1] = new ChatMessage(ChatRole.Assistant, respuestaCompleta);

        conversacion.Text = ObtenerConversacion();
        conversacion.SetNeedsDraw();
    }

    entrada.Enabled = true;
    botonEnviar.Enabled = true;
};

ventana.Add(conversacion, entrada, botonEnviar);

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
