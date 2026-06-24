#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

[Description ("Devuelve el contenido de un archivo de texto.")]
string LeerArchivo([Description("Ruta del archivo a leer.")] string ruta)
{
    if (!File.Exists(ruta))
        return $"Error: el archivo '{ruta}' no existe.";
        return File.ReadAllText(ruta);
}

[Description("Crea o sobrescribe un archivo con el contenido indicado.")]
string EscribirArchivo(
    [Description("Ruta del archivo a escribir.")] string ruta,
    [Description("Contenido a escribir en el archivo.")] string contenido)
    {
        try
        {
            var dir = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ruta, contenido);
            return $"Archivo '{ruta}' guardado correctamente.";
        }
        catch (Exception ex)
        {
             return $"Error al escribir '{ruta}': {ex.Message}";
        }
    }

    [Description("Lista los archivos y carpetas de un directorio.")]
    string ListarArchivos([Description("Ruta del directorio a listar.")] string ruta)
    {
        if(!Directory.Exists(ruta))
            return $"Error: el directorio '{ruta}' no existe.";

        var entradas = Directory.GetFileSystemEntries(ruta)
            .Select(e => Directory.Exists(e)
                ? $"[DIR]  {Path.GetFileName(e)}"
                : $"[FILE] {Path.GetFileName(e)}")
                .OrderBy(e => e);

         return string.Join("\n", entradas);
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

var respuesta = await chat.GetResponseAsync(mensajes);

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

ventana.Add(new Markdown {
    Text = $"# Vos\n\n{pregunta}\n\n# Asistente\n\n{respuesta.Text}",
    Width = Dim.Fill(), Height = Dim.Fill()
});

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
