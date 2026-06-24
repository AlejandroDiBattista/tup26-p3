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
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

DotNetEnv.Env.Load();

var testKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (string.IsNullOrWhiteSpace(testKey) || testKey.Contains("<tu_clave"))
{
    Console.WriteLine("\n[ERROR CRÍTICO DEL BÚNKER] 🚨");
    Console.WriteLine("El programa no puede leer tu clave de Gemini.");
    Console.WriteLine("1. Verificá que el archivo se llame EXACTAMENTE '.env' (sin .txt oculto).");
    Console.WriteLine("2. Verificá que hayas guardado el archivo con Ctrl + S.");
    Environment.Exit(1);
}
// -------------------------------
var proveedor = (args.Length > 0 ? args[0] : "gemini").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient chatBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url ?? "https://api.openai.com/v1") })
    .GetChatClient(modelo)
    .AsIChatClient();

// El cliente base ya soporta las funciones a través de ChatOptions
IChatClient chat = chatBase;

var opciones = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(Herramientas.ListarArchivos, "listar-archivos", "Lista los archivos de un directorio"),
        AIFunctionFactory.Create(Herramientas.LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto"),
        AIFunctionFactory.Create(Herramientas.EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado")
    ]
};

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" TP6: Asistente IA - 63546 - Jeremías Sosa Paz ({modelo}) ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var vistaChat = new Markdown {
    Width = Dim.Fill(),
    Height = Dim.Fill() - 2,
    Text = "# Asistente\n¡Hola! Soy el asistente del sistema. Escribí tu mensaje abajo.\n\n",
    CanFocus = true
};

var input = new TextField {
    Y = Pos.Bottom(vistaChat) + 1,
    Width = Dim.Fill() - 35,
};

var btnEnviar = new Button {
    Title = "Enviar",
    X = Pos.Right(input) + 1,
    Y = Pos.Bottom(vistaChat) + 1,
};

var lblEstado = new Label {
    Text = "🟢 Listo",
    X = Pos.Right(btnEnviar) + 2,
    Y = Pos.Bottom(vistaChat) + 1,
};

ventana.Add(vistaChat, input, btnEnviar, lblEstado);

bool procesando = false;

async Task ProcesarMensajeAsync(string pregunta)
{
    mensajes.Add(new ChatMessage(ChatRole.User, pregunta));
    
    app.Invoke(() => {
        vistaChat.Text += $"\n\n# Vos\n\n{pregunta}\n\n# Asistente\n\n";
        lblEstado.Text = "🟡 Pensando..."; 
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
                var txt = fragmento.Text; 
                app.Invoke(() => {
                    vistaChat.Text += txt;
                });
            }
        }
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaActual));
    }
    catch (Exception ex)
    {
        app.Invoke(() => {
            vistaChat.Text += $"\n\n**Error del sistema:** {ex.Message}";
        });
    }
    finally
    {
        app.Invoke(() => {
            procesando = false;
            input.Enabled = true;
            btnEnviar.Enabled = true;
            lblEstado.Text = "🟢 Listo";
            input.SetFocus(); 
        });
    }
}

void Enviar() 
{
    if (procesando || string.IsNullOrWhiteSpace(input.Text)) return;
    
    string pregunta = input.Text;
    input.Text = "";
    
    if (pregunta.Trim().ToLower() == "/limpiar")
    {
        vistaChat.Text = "# Asistente\nHistorial borrado. El búnker está limpio.\n\n";
        mensajes.Clear();
        mensajes.Add(new ChatMessage(ChatRole.System, File.ReadAllText("AGENTS.md")));
        return;
    }
    
    procesando = true;
    input.Enabled = false;
    btnEnviar.Enabled = false;
    
    _ = ProcesarMensajeAsync(pregunta);
}

// Eventos corregidos para la v2 de Terminal.Gui
btnEnviar.Accepting += (s, e) => { Enviar(); e.Handled = true; };
input.Accepting += (s, e) => { Enviar(); e.Handled = true; }; 

ventana.KeyDown += (s, e) => {
    if (e == Terminal.Gui.Input.Key.Esc) {
        app.RequestStop();
        e.Handled = true;
    }
};

input.SetFocus();
app.Run(ventana);

// ===============================================================
// MÓDULO DE HERRAMIENTAS INCRUSTADO (Para evitar error de lectura)
// ===============================================================
public static class Herramientas
{
    public static string ListarArchivos(string ruta)
    {
        try
        {
            string dir = string.IsNullOrWhiteSpace(ruta) ? Directory.GetCurrentDirectory() : ruta;
            if (!Directory.Exists(dir)) return "Error: El directorio especificado no existe.";
            
            string[] elementos = Directory.GetFileSystemEntries(dir);
            return elementos.Length == 0 ? "El directorio está vacío." : string.Join("\n", elementos);
        }
        catch (Exception ex)
        {
            return $"Error al listar archivos: {ex.Message}";
        }
    }

    public static string LeerArchivo(string ruta)
    {
        try
        {
            if (!File.Exists(ruta)) return "Error: El archivo no existe.";
            return File.ReadAllText(ruta);
        }
        catch (Exception ex)
        {
            return $"Error al leer el archivo: {ex.Message}";
        }
    }

    public static string EscribirArchivo(string ruta, string contenido)
    {
        try
        {
            File.WriteAllText(ruta, contenido);
            return $"Archivo escrito con éxito en: {ruta}";
        }
        catch (Exception ex)
        {
            return $"Error al escribir el archivo: {ex.Message}";
        }
    }
}