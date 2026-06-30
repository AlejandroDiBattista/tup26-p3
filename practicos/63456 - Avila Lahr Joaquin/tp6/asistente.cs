#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ClientModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OpenAI;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

Console.WriteLine($"Proveedor: {proveedor}");
Console.WriteLine($"URL: {url}");
Console.WriteLine($"Modelo: {modelo}");
Console.WriteLine($"API Key cargada: {!string.IsNullOrWhiteSpace(apiKey)}");

IChatClient chat = new ChatClientBuilder(
        new OpenAIClient(
                new ApiKeyCredential(apiKey ?? "no-requiere-key"),
                new OpenAIClientOptions { Endpoint = new Uri(url!) })
            .GetChatClient(modelo)
            .AsIChatClient())
    .UseFunctionInvocation()
    .Build();

var herramientas = new[]
{
    AIFunctionFactory.Create(LeerArchivo, name: "leer-archivo"),
    AIFunctionFactory.Create(EscribirArchivo, name: "escribir-archivo"),
    AIFunctionFactory.Create(ListarArchivos, name: "listar-archivos")
};

var opciones = new ChatOptions
{
    Tools = herramientas,
    ToolMode = ChatToolMode.Auto
};
static string RutaDelScript([CallerFilePath] string ruta = "") => ruta;
var directorioApp = Path.GetDirectoryName(RutaDelScript())!;
var rutaAgents = Path.Combine(directorioApp, "AGENTS.md");

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText(rutaAgents))
};

using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var historial = new Markdown
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    CanFocus = true
};

var entrada = new TextField
{
    X = 0,
    Y = Pos.AnchorEnd(1),
    Width = Dim.Fill(12)
};

var botonEnviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada),
    Y = Pos.AnchorEnd(1)
};

ventana.Add(historial);
ventana.Add(entrada);
ventana.Add(botonEnviar);

bool enviando = false;

void AgregarAlHistorial(string textoNuevo)
{
    app.Invoke(() =>
    {
        bool estabaAlFondo =
            historial.Viewport.Y + historial.Viewport.Height >= historial.GetContentSize().Height;

        historial.Text += textoNuevo;

        if (estabaAlFondo)
            historial.ScrollVertical(int.MaxValue);

        historial.SetNeedsDraw();
    });
}

async Task EnviarMensaje()
{
    if (enviando)
        return;

    var texto = entrada.Text?.ToString()?.Trim();

    if (string.IsNullOrWhiteSpace(texto))
        return;

    enviando = true;

    app.Invoke(() =>
    {
        entrada.Text = "";
        botonEnviar.Enabled = false;
        entrada.Enabled = false;
    });

    mensajes.Add(new ChatMessage(ChatRole.User, texto));
    AgregarAlHistorial($"\n# Vos\n\n{texto}\n");

    try
    {
        AgregarAlHistorial("\n# Asistente\n\n");

        string respuestaCompleta = "";

        await foreach (var update in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
            if (string.IsNullOrEmpty(update.Text))
                continue;

            respuestaCompleta += update.Text;
            AgregarAlHistorial(update.Text);
        }

        AgregarAlHistorial("\n");
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta));
    }
    catch (Exception ex)
    {
        AgregarAlHistorial($"\n# Error\n\n{ex.Message}\n");
    }
    finally
    {
        app.Invoke(() =>
        {
            enviando = false;
            botonEnviar.Enabled = true;
            entrada.Enabled = true;
            entrada.SetFocus();
        });
    }
}

entrada.Accepting += async (_, _) => await EnviarMensaje();
botonEnviar.Accepting += async (_, _) => await EnviarMensaje();

[Description("Devuelve el contenido de un archivo de texto del proyecto.")]
string LeerArchivo(
    [Description("Ruta del archivo a leer.")] string ruta)
{
    try
    {
        return File.ReadAllText(ruta);
    }
    catch (Exception ex)
    {
        return $"No se pudo leer '{ruta}': {ex.Message}";
    }
}

[Description("Crea un archivo nuevo o sobrescribe uno existente con el contenido indicado.")]
string EscribirArchivo(
    [Description("Ruta del archivo a crear o sobrescribir.")] string ruta,
    [Description("Contenido de texto a escribir en el archivo.")] string contenido)
{
    try
    {
        File.WriteAllText(ruta, contenido);
        return $"Archivo '{ruta}' guardado correctamente.";
    }
    catch (Exception ex)
    {
        return $"No se pudo escribir '{ruta}': {ex.Message}";
    }
}

[Description("Lista los archivos y carpetas de un directorio.")]
string ListarArchivos(
    [Description("Ruta del directorio a listar.")] string ruta)
{
    try
    {
        return string.Join("\n", Directory.GetFileSystemEntries(ruta));
    }
    catch (Exception ex)
    {
        return $"No se pudo listar '{ruta}': {ex.Message}";
    }
}

app.Run(ventana);
