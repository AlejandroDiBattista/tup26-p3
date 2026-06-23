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

var configuracion = CargarConfiguracion(args);
var promptSistema = CargarPromptSistema();
IChatClient chat = CrearCliente(configuracion);

List<ChatMessage> mensajes = [
    new(ChatRole.System, promptSistema)
];

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA - {configuracion.Modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var panelConversacion = new FrameView {
    Title = " Conversacion ",
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill(6)
};

var conversacion = new Markdown {
    Text = "# Asistente IA\n\nBienvenido. Escribi una consulta en el panel inferior para iniciar la conversacion.\n\n## Estado\n\nEsperando tu primer mensaje...",
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill()
};

var panelEntrada = new FrameView {
    Title = " Nuevo mensaje ",
    X = 0, Y = Pos.AnchorEnd(6),
    Width = Dim.Fill(), Height = 6
};

var entrada = new TextField {
    X = 1, Y = 0,
    Width = Dim.Fill(15)
};

var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.AnchorEnd(12), Y = 0,
    Width = 11
};

var ayuda = new Label {
    Text = "Enter: enviar | Esc: salir | La respuesta aparecera en el panel superior",
    X = 1, Y = 2,
    Width = Dim.Fill(2), Height = 1
};

panelConversacion.Add(conversacion);
panelEntrada.Add(entrada, botonEnviar, ayuda);
ventana.Add(panelConversacion, panelEntrada);

// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);

static ConfiguracionIA CargarConfiguracion(string[] args)
{
    var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
    var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
    var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "no-requiere-key";
    var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL");

    if (string.IsNullOrWhiteSpace(url))
    {
        throw new InvalidOperationException($"Falta configurar la variable {proveedor}_API_URL.");
    }

    if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint))
    {
        throw new InvalidOperationException($"La variable {proveedor}_API_URL no contiene una URL valida.");
    }

    if (string.IsNullOrWhiteSpace(modelo))
    {
        throw new InvalidOperationException($"Falta configurar la variable {proveedor}_MODEL.");
    }

    return new ConfiguracionIA(proveedor, endpoint, apiKey, modelo);
}

static string CargarPromptSistema()
{
    const string rutaPrompt = "AGENTS.md";

    if (!File.Exists(rutaPrompt))
    {
        throw new FileNotFoundException("No se encontro el archivo AGENTS.md junto a la aplicacion.", rutaPrompt);
    }

    return File.ReadAllText(rutaPrompt);
}

static IChatClient CrearCliente(ConfiguracionIA configuracion)
{
    return new OpenAIClient(
            new ApiKeyCredential(configuracion.ApiKey),
            new OpenAIClientOptions { Endpoint = configuracion.Endpoint })
        .GetChatClient(configuracion.Modelo)
        .AsIChatClient();
}

record ConfiguracionIA(string Proveedor, Uri Endpoint, string ApiKey, string Modelo);
