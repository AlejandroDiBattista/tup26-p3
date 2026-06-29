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

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();

var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL");

Console.WriteLine($"Proveedor: {proveedor}");

OpenAIClientOptions options = new OpenAIClientOptions
{
    Endpoint = new Uri(url)
};

OpenAIClient client;

if (proveedor == "OLLAMA")
{
    client = new OpenAIClient(
        new ApiKeyCredential("ollama"),
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

List<ChatMessage> mensajes = new()
{
    new ChatMessage(ChatRole.System, File.Exists("AGENTS.md") ? File.ReadAllText("AGENTS.md") : "Sos un asistente útil")
};

using IApplication app = Application.Create().Init();

var ventana = new Window
{
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var chatView = new Markdown
{
    Width = Dim.Fill(),
    Height = Dim.Fill() - 3,
    Text = "# Asistente listo\n"
};

var input = new TextField
{
    X = 0,
    Y = Pos.Bottom(chatView),
    Width = Dim.Fill() - 10
};

var boton = new Button
{
    Text = "Enviar",
    X = Pos.Right(input),
    Y = Pos.Bottom(chatView),
    Width = 10
};

ventana.Add(chatView, input, boton);

async void Enviar()
{
    var texto = input.Text.ToString();
    if (string.IsNullOrWhiteSpace(texto)) return;

    input.Text = "";

    chatView.Text += $"\n# Vos\n\n{texto}\n\n# Asistente\n\n";

    input.Enabled = false;
    boton.Enabled = false;

    if (texto.StartsWith("leer-archivo"))
    {
        var ruta = texto.Replace("leer-archivo", "").Trim();
        var contenido = File.Exists(ruta) ? File.ReadAllText(ruta) : "Archivo no encontrado";

        chatView.Text += contenido;
        input.Enabled = true;
        boton.Enabled = true;
        return;
    }

    if (texto.StartsWith("listar-archivos"))
    {
        var ruta = texto.Replace("listar-archivos", "").Trim();
        if (string.IsNullOrWhiteSpace(ruta)) ruta = ".";

        var lista = Directory.Exists(ruta)
            ? string.Join("\n", Directory.GetFileSystemEntries(ruta))
            : "Directorio no existe";

        chatView.Text += lista;
        input.Enabled = true;
        boton.Enabled = true;
        return;
    }

    if (texto.StartsWith("escribir-archivo"))
    {
        // formato: escribir-archivo|ruta|contenido
        var partes = texto.Split('|');

        if (partes.Length == 3)
        {
            File.WriteAllText(partes[1], partes[2]);
            chatView.Text += "Archivo guardado correctamente";
        }
        else
        {
            chatView.Text += "Formato inválido. Usar: escribir-archivo|ruta|contenido";
        }

        input.Enabled = true;
        boton.Enabled = true;
        return;
    }

    mensajes.Add(new ChatMessage(ChatRole.User, texto));

    var response = await chat.GetResponseAsync(mensajes);

    var textoFinal = response.Text ?? "";

    chatView.Text += textoFinal;

    mensajes.Add(new ChatMessage(ChatRole.Assistant, textoFinal));

    input.Enabled = true;
    boton.Enabled = true;
}

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