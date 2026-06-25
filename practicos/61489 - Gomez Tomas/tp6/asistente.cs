#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui; 
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input; 
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

DotNetEnv.Env.Load(".env.ejemplo");

var proveedor = (args.Length > 0 ? args[0] : "groq").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen/qwen3.6-27b";

Uri uriBase = null;
if (!string.IsNullOrWhiteSpace(url))
{
    string urlLimpia = url.Replace("/chat/completions", "");
    uriBase = new Uri(urlLimpia);
}

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = uriBase })
    .GetChatClient(modelo)
    .AsIChatClient();

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.Exists("AGENTS.md") ? File.ReadAllText("AGENTS.md") : "Sos un asistente útil.")
];

ChatOptions opcionesChat = new()
{
    Tools = [
        AIFunctionFactory.Create(LeerArchivo),
        AIFunctionFactory.Create(EscribirArchivo),
        AIFunctionFactory.Create(ListarArchivos)
    ]
};

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" TP6 Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var vistaChat = new Markdown {
    Text = "> **Asistente:** ¡Hola! Escribí tu mensaje abajo para empezar.\n\n",
    Width = Dim.Fill(), 
    Height = Dim.Fill(2) 
};

var divisor = new Line {
    Orientation = Orientation.Horizontal,
    Y = Pos.Bottom(vistaChat),
    Width = Dim.Fill()
};

var campoEntrada = new TextField {
    Y = Pos.Bottom(divisor),
    Width = Dim.Fill(12)
};

var botonEnviar = new Button {
    Title = "Enviar",
    X = Pos.Right(campoEntrada) + 1,
    Y = Pos.Bottom(divisor),
    IsDefault = true 
};

string textoPantalla = vistaChat.Text;

EventHandler<CommandEventArgs> manejarEnvio = (s, e) =>
{
    e.Handled = true; 

    var textoUsuario = campoEntrada.Text;
    if (string.IsNullOrWhiteSpace(textoUsuario)) return;

    campoEntrada.Text = "";
    campoEntrada.Enabled = false;
    botonEnviar.Enabled = false;

    textoPantalla += $"# Usuario\n\n{textoUsuario}\n\n# Asistente\n\n";
    vistaChat.Text = textoPantalla;
    mensajes.Add(new(ChatRole.User, textoUsuario));

    _ = Task.Run(async () =>
    {
        try
        {
            string contenidoAsistente = ""; 
            
            await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opcionesChat))
            {
                if (!string.IsNullOrEmpty(fragmento.Text))
                {
                    contenidoAsistente += fragmento.Text;
                    
                    app.Invoke(() => {
                        textoPantalla += fragmento.Text;
                        vistaChat.Text = textoPantalla;
                    });
                }
            }
            
            app.Invoke(() => {
                textoPantalla += "\n\n---\n\n"; 
                vistaChat.Text = textoPantalla;
            });

            mensajes.Add(new ChatMessage(ChatRole.Assistant, contenidoAsistente));
        }
        catch (Exception ex)
        {
            app.Invoke(() => {
                textoPantalla += $"\n> **Error de conexión:** {ex.Message}\n\n";
                vistaChat.Text = textoPantalla;
            });
        }
        finally
        {
            app.Invoke(() => {
                campoEntrada.Enabled = true;
                botonEnviar.Enabled = true;
                campoEntrada.SetFocus(); 
            });
        }
    });
};

botonEnviar.Accepting += manejarEnvio;
campoEntrada.Accepting += manejarEnvio;

ventana.Add(vistaChat, divisor, campoEntrada, botonEnviar);

campoEntrada.SetFocus();

app.Run(ventana);

[Description("Devuelve el contenido de un archivo de texto")]
string LeerArchivo([Description("La ruta del archivo a leer")] string ruta)
{
    try { return File.ReadAllText(ruta); }
    catch (Exception ex) { return $"Error: {ex.Message}"; }
}

[Description("Crea o sobrescribe un archivo con el contenido indicado")]
string EscribirArchivo([Description("La ruta del archivo")] string ruta, [Description("Contenido")] string contenido)
{
    try { File.WriteAllText(ruta, contenido); return "Guardado con éxito."; }
    catch (Exception ex) { return $"Error: {ex.Message}"; }
}

[Description("Lista los archivos y carpetas de un directorio")]
string ListarArchivos([Description("La ruta de la carpeta")] string ruta)
{
    try { return string.Join("\n", Directory.GetFileSystemEntries(ruta)); }
    catch (Exception ex) { return $"Error: {ex.Message}"; }
}