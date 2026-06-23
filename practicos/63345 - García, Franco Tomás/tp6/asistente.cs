#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Terminal.Gui;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = "OLLAMA";
var url = "http://localhost:11434/v1";
var modelo = "qwen2.5-coder:7b";
var apiKey = "ollama";
Console.WriteLine($"Proveedor: {proveedor}");
OpenAIClientOptions options = new OpenAIClientOptions
{
    Endpoint = new Uri(url)
};

OpenAIClient client;

if (proveedor == "OLLAMA")
{
    client = new OpenAIClient(
        new ApiKeyCredential("ollama"), // cualquier string
        options);
}
else
{
    client = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? ""),
        options);
}

IChatClient chat = client
    .GetChatClient(modelo)
    .AsIChatClient();

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md")),
];

using IApplication app = Application.Create().Init();

var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), 
    Height = Dim.Fill()
};


// TODO: agregar el panel de conversación y el panel de entrada.
var chatView = new Markdown {
Width = Dim.Fill(),
Height = Dim.Fill() - 3,
Text = "# Asistente listo\n"
};

var input = new TextField {
X = 0,
Y = Pos.Bottom(chatView),
Width = Dim.Fill() - 10
};

var boton = new Button {
Text = "Enviar",
X = Pos.Right(input),
Y = Pos.Bottom(chatView),
Width = 10
};

ventana.Add(chatView, input, boton);

// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
async void Enviar()
{
var texto = input.Text.ToString();
if (string.IsNullOrWhiteSpace(texto)) return;

input.Text = "";

mensajes.Add(new ChatMessage(ChatRole.User, texto));
chatView.Text += $"\n# Vos\n\n{texto}\n\n# Asistente\n\n";

string respuestaCompleta = "";

input.Enabled = false;
boton.Enabled = false;

await foreach (var chunk in chat.GetStreamingResponseAsync(mensajes))
{
    if (chunk.Text != null)
    {
        respuestaCompleta += chunk.Text;

        app.Invoke(() => {
            chatView.Text += chunk.Text;
        });
    }
}


mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta));

input.Enabled = true;
boton.Enabled = true;

}
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).
boton.Accepted += (s, e) => Enviar();

input.Accepting += (s, e) =>
{
    Enviar();
    e.Handled = true;
};
ventana.KeyDown += (s, key) =>
{
    if (key.ToString() == "Esc")
    {
        app.RequestStop();
    }
};
app.Run(ventana);
