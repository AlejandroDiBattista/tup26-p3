#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package OpenAI@2.9.1
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

var proveedor = (args.Length > 0 ? args[0] : "groq").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient chat = new ChatClientBuilder(
    new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient())
    .UseFunctionInvocation()
    .Build();

    string LeerArchivo(string ruta)
{
    if (!File.Exists(ruta))
        return "El archivo no existe.";

    return File.ReadAllText(ruta);
}

string EscribirArchivo(string ruta, string contenido)
{
    File.WriteAllText(ruta, contenido);
    return "Archivo guardado correctamente.";
}

string ListarArchivos(string ruta)
{
    if (!Directory.Exists(ruta))
        return "La carpeta no existe.";

    return string.Join("\n", Directory.GetFileSystemEntries(ruta));
}

var herramientas = new[]
{
    AIFunctionFactory.Create(LeerArchivo),
    AIFunctionFactory.Create(EscribirArchivo),
    AIFunctionFactory.Create(ListarArchivos)
};

var opciones = new ChatOptions
{
    Tools = herramientas
};

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA",
    Width = Dim.Fill(),
     Height = Dim.Fill()
};

/* ventana.Add(new Markdown {
    Text = $"# Vos\n\n{pregunta}\n\n# Asistente\n\n{respuesta.Text}",
    Width = Dim.Fill(), Height = Dim.Fill()
});*/

var panelConversacion = new FrameView()
{
    Title = " Chat ",
    Width = Dim.Fill(),
    Height = Dim.Fill(3)
};

var conversacion = new Markdown()
{
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Text = """
    ¡Hola! ¿En qué puedo ayudarte? 
    """
};

panelConversacion.Add(conversacion);

var panelEntrada = new FrameView()
{
    Title = " Mensaje ",
    Y = Pos.Bottom(panelConversacion),
    Width = Dim.Fill(),
    Height = 3
};

var entrada = new TextField()
{
    X = 1,
    Y = 0,
    Width = Dim.Fill(12)
};

var botonEnviar = new Button
{
    X = Pos.Right(entrada) + 1,
    Y = 0,
    Text = "Enviar"
};

panelEntrada.Add(entrada);
panelEntrada.Add(botonEnviar);

ventana.Add(panelConversacion);
ventana.Add(panelEntrada);

botonEnviar.Accepted += async (_, _) =>
{
    string texto = entrada.Text.ToString();

    if (string.IsNullOrWhiteSpace(texto))
        return;

    mensajes.Add(new(ChatRole.User, texto));

    conversacion.Text += $"\n# Vos\n\n{texto}\n";

    entrada.Text = "";
    
    entrada.Enabled = false;
    botonEnviar.Enabled = false;

    conversacion.Text += "\n# Asistente\n\n";

    string respuestaCompleta = "";

    await foreach (var update in chat.GetStreamingResponseAsync(mensajes, opciones))
   {
        respuestaCompleta += update.Text;
        conversacion.Text += update.Text;
    }

    mensajes.Add(new(ChatRole.Assistant, respuestaCompleta));
    
    entrada.Enabled = true;
    botonEnviar.Enabled = true;

    entrada.SetFocus();
};

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
