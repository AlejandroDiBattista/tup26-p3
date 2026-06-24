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

// 1. Cliente Base
IChatClient chatBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

// 2. PILOTO AUTOMÁTICO: Invocación automática de funciones
IChatClient chat = new ChatClientBuilder(chatBase)
    .UseFunctionInvocation()
    .Build();

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

var separador = new LineView {
    Orientation = Terminal.Gui.Orientation.Horizontal,
    Y = Pos.Bottom(vistaChat),
    Width = Dim.Fill()
};

var input = new TextField {
    Y = Pos.Bottom(vistaChat) + 1,
    Width = Dim.Fill() - 35, // Achico un poco para que entre el estado
};

var btnEnviar = new Button {
    Title = "Enviar",
    X = Pos.Right(input) + 1,
    Y = Pos.Bottom(vistaChat) + 1,
};

// 3. NUEVO: Etiqueta de estado para mejorar la UX
var lblEstado = new Label {
    Text = "🟢 Listo",
    X = Pos.Right(btnEnviar) + 2,
    Y = Pos.Bottom(vistaChat) + 1,
};

ventana.Add(vistaChat, separador, input, btnEnviar, lblEstado);

bool procesando = false;

async Task ProcesarMensajeAsync(string pregunta)
{
    mensajes.Add(new ChatMessage(ChatRole.User, pregunta));
    
    Application.Invoke(() => {
        vistaChat.Text += $"\n\n# Vos\n\n{pregunta}\n\n# Asistente\n\n";
        lblEstado.Text = "🟡 Pensando y ejecutando..."; // Avisa que está trabajando
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
            lblEstado.Text = "🟢 Listo"; // vuelve a la normalidad 
            input.SetFocus(); 
        });
    }
}

void Enviar() 
{
    if (procesando || string.IsNullOrWhiteSpace(input.Text)) return;
    
    string pregunta = input.Text;
    input.Text = "";
    
    // --- NUEVO: Comando para limpiar el chat ---
    if (pregunta.Trim().ToLower() == "/limpiar")
    {
        vistaChat.Text = "# Asistente\nHistorial de la sesión borrado. El búnker está limpio. ¿En qué te ayudo ahora?\n\n";
        mensajes.Clear();
        mensajes.Add(new ChatMessage(ChatRole.System, File.ReadAllText("AGENTS.md")));
        return;
    }
    
    procesando = true;
    input.Enabled = false;
    btnEnviar.Enabled = false;
    
    _ = ProcesarMensajeAsync(pregunta);
}

btnEnviar.Accept += (s, e) => Enviar();
input.Accept += (s, e) => Enviar(); 

ventana.KeyDown += (s, e) => {
    if (e.Key == Key.Esc) {
        Application.RequestStop();
        e.Handled = true;
    }
};

input.SetFocus();
app.Run(ventana);