#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ComponentModel;
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
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

var urlBase = url!.EndsWith("/chat/completions")
    ? url[..^"/chat/completions".Length]
    : url;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(urlBase) })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

[Description("Lee el contenido de un archivo de texto.")]
string LeerArchivo([Description("Ruta del archivo a leer.")] string ruta)
{
    if (!File.Exists(ruta))
        return "El archivo no existe.";

    return File.ReadAllText(ruta);
}

[Description("Crea o sobrescribe un archivo con el contenido indicado.")]
string EscribirArchivo(
    [Description("Ruta del archivo a escribir.")] string ruta,
    [Description("Contenido que se va a guardar.")] string contenido)
{
    File.WriteAllText(ruta, contenido);
    return "Archivo guardado correctamente.";
}

[Description("Lista los archivos y carpetas de un directorio.")]
string ListarArchivos([Description("Ruta del directorio a listar.")] string ruta = ".")
{
    if (!Directory.Exists(ruta))
        return "El directorio no existe.";

    return string.Join(Environment.NewLine, Directory.GetFileSystemEntries(ruta));
}

ChatOptions opciones = new()
{
    Tools =
    [
        AIFunctionFactory.Create(LeerArchivo, new() {
            Name = "leer-archivo",
            Description = "Lee el contenido de un archivo de texto."
        }),
        AIFunctionFactory.Create(EscribirArchivo, new() {
            Name = "escribir-archivo",
            Description = "Crea o sobrescribe un archivo con el contenido indicado."
        }),
        AIFunctionFactory.Create(ListarArchivos, new() {
            Name = "listar-archivos",
            Description = "Lista los archivos y carpetas de un directorio."
        })
    ]
};

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

string textoConversacion = "# Asistente IA\n\nEscribí un mensaje para comenzar.";
bool enviando = false;

using IApplication app = Application.Create().Init();

using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown {
    Text = textoConversacion,
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3)
};

var entrada = new TextField {
    X = 0,
    Y = Pos.AnchorEnd(3),
    Width = Dim.Fill(12),
    Height = 1
};

var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.AnchorEnd(3),
    Width = 10
};

async Task EnviarMensaje()
{
    string texto = entrada.Text?.ToString() ?? "";

    if (string.IsNullOrWhiteSpace(texto) || enviando) {
        return;
    }

    enviando = true;

    app.Invoke(() =>
    {
        entrada.Text = "";
        entrada.Enabled = false;
        botonEnviar.Enabled = false;
    });

    textoConversacion += $"\n\n# Vos\n\n{texto}";
    ActualizarConversacion();

    mensajes.Add(new ChatMessage(ChatRole.User, texto));

    textoConversacion += "\n\n# Asistente\n\n";
    ActualizarConversacion();

    string respuestaCompleta = "";

    try
    {
        await foreach (var parte in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
            var textoParte = parte.Text ?? "";
            respuestaCompleta += textoParte;
            textoConversacion += textoParte;
            ActualizarConversacion();
        }

        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta));
    }
    catch (Exception ex)
    {
        respuestaCompleta = $"Error: {ex.Message}";
        textoConversacion += $"Error: {ex.Message}";
        ActualizarConversacion();
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta));
    }
    finally
    {
        app.Invoke(() =>
        {
            enviando = false;
            entrada.Enabled = true;
            botonEnviar.Enabled = true;
            entrada.SetFocus();
        });
    }
}

void ActualizarConversacion()
{
    app.Invoke(() =>
    {
        conversacion.Text = textoConversacion;
        conversacion.SetNeedsDraw();
    });
}

botonEnviar.Accepting += async (sender, e) => {
    await EnviarMensaje();
};

entrada.Accepting += async (sender, e) => {
    await EnviarMensaje();
};

ventana.Add(conversacion, entrada, botonEnviar);

app.Run(ventana);
