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
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

// 1. Inyecto las herramientas que cree en el 1er archivo Herramientas.cs
var opciones = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(Herramientas.ListarArchivos, "listar-archivos", "Lista los archivos (y carpetas) de un directorio"),
        AIFunctionFactory.Create(Herramientas.LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto"),
        AIFunctionFactory.Create(Herramientas.EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado")
    ]
};

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

// ==========================================
// 2. CONFIGURACIÓN DE LA INTERFAZ (Terminal.Gui)
// ==========================================
using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · Búnker Local ({modelo}) ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var vistaChat = new Markdown {
    Width = Dim.Fill(),
    Height = Dim.Fill() - 2, // Dejo 2 líneas libres abajo
    Text = "# Asistente\n¡Hola! Soy el asistente del sistema. Escribí tu mensaje abajo.\n\n",
    CanFocus = true
};

var separador = new LineView {
    Orientation = Terminal.Gui.Orientation.Horizontal,
    Y = Pos.Bottom(vistaChat),
    Width = Dim.Fill()
};

var input = new TextField {
    Y = Pos.Bottom(vistaChat) + 1,
    Width = Dim.Fill() - 10,
};

var btnEnviar = new Button {
    Title = "Enviar",
    X = Pos.Right(input) + 1,
    Y = Pos.Bottom(vistaChat) + 1,
};

ventana.Add(vistaChat, separador, input, btnEnviar);

// ==========================================
// 3. LÓGICA DE EVENTOS Y STREAMING
// ==========================================
bool procesando = false;

async Task ProcesarMensajeAsync(string pregunta)
{
    mensajes.Add(new ChatMessage(ChatRole.User, pregunta));
    
    // Actualizo la UI desde el hilo principal
    Application.Invoke(() => {
        vistaChat.Text += $"\n\n# Vos\n\n{pregunta}\n\n# Asistente\n\n";
    });
    
    try 
    {
        var streaming = chat.GetStreamingResponseAsync(mensajes, opciones);
        string respuestaActual = "";

        await foreach (var fragmento in streaming)
        {
            if (fragmento.Text != null)
            {
                respuestaActual += fragmento.Text;
                var txt = fragmento.Text; // Clono para evitar problemas de closure
                Application.Invoke(() => {
                    vistaChat.Text += txt;
                });
            }
        }
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaActual));
    }
    catch (Exception ex)
    {
        Application.Invoke(() => {
            vistaChat.Text += $"\n\n**Error del sistema:** {ex.Message}";
        });
    }
    finally
    {
        Application.Invoke(() => {
            procesando = false;
            input.Enabled = true;
            btnEnviar.Enabled = true;
            input.SetFocus(); // Devuelve el cursor a la caja de texto 

            
        });
    }
}

void Enviar() 
{
    if (procesando || string.IsNullOrWhiteSpace(input.Text)) return;
    
    procesando = true;
    string pregunta = input.Text;
    input.Text = "";
    
    // Bloqueo controles para evitar envíos superpuestos
    input.Enabled = false;
    btnEnviar.Enabled = false;
    
    // Lanzo la tarea en segundo plano
    _ = ProcesarMensajeAsync(pregunta);
}

// 4. Atajos de teclado
btnEnviar.Accept += (s, e) => Enviar();
input.Accept += (s, e) => Enviar(); // El Enter en el TextField envía el mensaje automáticamente

ventana.KeyDown += (s, e) => {
    if (e.Key == Key.Esc) {
        Application.RequestStop();
        e.Handled = true;
    }
};

input.SetFocus();
app.Run(ventana);