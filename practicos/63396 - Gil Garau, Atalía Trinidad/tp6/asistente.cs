#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "groq").ToUpperInvariant();
var chat = new Agente(proveedor);
chat.Registrar(ChatRole.System, File.ReadAllText("AGENTS.md"));

using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" Asistente IA · {chat.Modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var markdown = new Markdown
{
    Text = "# Vos\n\nEscribí un mensaje para empezar.",
    Width = Dim.Fill(),
    Height = Dim.Fill(5)
};

var entrada = new TextField
{
    X = 1,
    Y = Pos.Bottom(markdown) + 1,
    Width = Dim.Fill(12)
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.Bottom(markdown) + 1
};

string conversacion = markdown.Text;
bool ocupada = false;

async Task EnviarAsync()
{
    if (ocupada)
        return;

    var texto = entrada.Text?.ToString()?.Trim() ?? "";
    if (texto == "")
        return;

    // Bloquea la entrada mientras responde.
    ocupada = true;
    entrada.Enabled = false;
    enviar.Enabled = false;
    entrada.Text = "";

    chat.Registrar(ChatRole.User, texto);
    conversacion += $"\n\n# Vos\n\n{texto}\n\n# Asistente\n\n";
    markdown.Text = conversacion;

    // Respuesta completa, sin streaming por ahora.
    var respuesta = await chat.ResponderAsync();
    chat.Registrar(ChatRole.Assistant, respuesta);

    conversacion += respuesta + "\n";
    markdown.Text = conversacion;

    ocupada = false;
    entrada.Enabled = true;
    enviar.Enabled = true;
    entrada.SetFocus();
}

entrada.KeyDown += (_, key) =>
{
    if (key == Key.Enter)
        _ = EnviarAsync();
};

enviar.Accepting += (_, e) =>
{
    e.Handled = true;
    _ = EnviarAsync();
};

ventana.Add(markdown, entrada, enviar);
app.Run(ventana);

public sealed class Agente
{
    readonly IChatClient cliente;
    readonly List<ChatMessage> historia = [];
    public string Modelo { get; }

    public Agente(string proveedor)
    {
        var apiUrl = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
        var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
        Modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.5";

        cliente = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(apiUrl) })
            .GetChatClient(Modelo)
            .AsIChatClient();
    }

    public void Registrar(ChatRole rol, string texto)
    {
        historia.Add(new ChatMessage(rol, texto));
    }

    public async Task<string> ResponderAsync()
    {
        var respuesta = await cliente.GetResponseAsync(historia);
        return respuesta.Text ?? "";
    }
}