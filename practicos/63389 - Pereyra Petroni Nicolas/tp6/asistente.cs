#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";
   
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

const string pregunta = "Definí recursividad";

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md")),
  ];

string LeerArchivo(string ruta)
{
    return File.ReadAllText(ruta);
}

string EscribirArchivo(string ruta, string contenido)
{
    File.WriteAllText(ruta, contenido);
    return $"Archivo escrito: {ruta}";
}

string ListarArchivos(string ruta)
{
    return string.Join(
        "\n",
        Directory.EnumerateFileSystemEntries(ruta)
            .Select(Path.GetFileName));
}

List<AITool> herramientas = [
    AIFunctionFactory.Create(
        (Func<string, string>)LeerArchivo,
        "leer-archivo",
        "Lee el contenido de un archivo de texto."),
    AIFunctionFactory.Create(
        (Func<string, string, string>)EscribirArchivo,
        "escribir-archivo",
        "Crea o sobrescribe un archivo con el contenido indicado."),
    AIFunctionFactory.Create(
        (Func<string, string>)ListarArchivos,
        "listar-archivos",
        "Lista los archivos y carpetas de un directorio.")
];

var opciones = new ChatOptions
{
    Tools = herramientas
};

//var respuesta = await chat.GetResponseAsync(mensajes);

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var conversacion = new Markdown {
    //Text = $"# Vos\n\n{pregunta}\n\n# Asistente\n\n{respuesta.Text}",
    Width = Dim.Fill(), 
    Height = Dim.Fill()
};
var panelConversacion = new FrameView() {
    Title = " Conversación",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3)
};
var panelEntrada = new FrameView() {
    X = 0,
    Y = Pos.Bottom(panelConversacion),
    Width = Dim.Fill(),
    Height = 3 
};
var entrada = new TextField() {
    X = 0,
    Y = 0,
    Width = Dim.Fill(12)
};
var botonEnviar = new Button() {
    Text = "Enviar",
    X = Pos.Right(entrada),
    Y = 0
};

var textoConversacion = new StringBuilder();
var enviando = false;

void RefrescarConversacion()
{
    var texto = textoConversacion.ToString();

    app.Invoke(() =>
    {
        conversacion.Text = texto;
        conversacion.SetNeedsDraw();
    });
}

void CambiarEntrada(bool habilitada)
{
    app.Invoke(() =>
    {
        entrada.Enabled = habilitada;
        botonEnviar.Enabled = habilitada;

        if (habilitada)
        {
            entrada.SetFocus();
        }
    });
}

async Task EnviarMensajeAsync()
{
    if (enviando)
    {
        return;
    }

    var texto = entrada.Text.ToString();

    if (string.IsNullOrWhiteSpace(texto))
    {
        return;
    }

    enviando = true;
    CambiarEntrada(false);

    try
    {
        mensajes.Add(new ChatMessage(ChatRole.User, texto));
        textoConversacion.Append($"\n# Vos\n\n{texto}\n\n# Asistente\n\n");
        RefrescarConversacion();

        var respuestaCompleta = new StringBuilder();

        await foreach (var update in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
            if (string.IsNullOrEmpty(update.Text))
            {
                continue;
            }

            respuestaCompleta.Append(update.Text);
            textoConversacion.Append(update.Text);
            RefrescarConversacion();
        }

        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta.ToString()));

        app.Invoke(() =>
        {
            entrada.Text = "";
        });
    }
    catch (Exception ex)
    {
        app.Invoke(() =>
        {
            MessageBox.ErrorQuery(
                app,
                "Error",
                ex.ToString(),
                "OK"
            );
        });
    }
    finally
    {
        enviando = false;
        CambiarEntrada(true);
    }
}

entrada.Accepted += async (_, _) => await EnviarMensajeAsync();
botonEnviar.Accepted += async (_, _) => await EnviarMensajeAsync();






ventana.Add(panelConversacion);
panelConversacion.Add(conversacion);
panelEntrada.Add(entrada);
panelEntrada.Add(botonEnviar);
ventana.Add(panelEntrada);

app.Run(ventana);
