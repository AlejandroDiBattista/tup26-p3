#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "ollama").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen2.5-coder:7b";

IChatClient chatBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient chat = new ChatClientBuilder(chatBase)
    .UseFunctionInvocation()
    .Build();

[Description("Devuelve el contenido de un archivo de texto")]
string LeerArchivo([Description("ruta del archivo")] string ruta)
{
    try { return File.ReadAllText(ruta); } 
    catch (Exception ex) { return $"Error al leer: {ex.Message}"; }
}

[Description("Crea o sobrescribe un archivo con el contenido indicado")]
string EscribirArchivo([Description("ruta del archivo")] string ruta, [Description("contenido a escribir")] string contenido)
{
    try { File.WriteAllText(ruta, contenido); return "Archivo guardado exitosamente."; } 
    catch (Exception ex) { return $"Error al escribir: {ex.Message}"; }
}

[Description("Lista los archivos (y carpetas) de un directorio")]
string ListarArchivos([Description("ruta del directorio")] string ruta)
{
    try { 
        var dirs = Directory.GetDirectories(ruta);
        var files = Directory.GetFiles(ruta);
        return "Carpetas:\n" + string.Join("\n", dirs) + "\n\nArchivos:\n" + string.Join("\n", files);
    } 
    catch (Exception ex) { return $"Error al listar: {ex.Message}"; }
}

var opcionesChat = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(LeerArchivo, "leer-archivo"),
        AIFunctionFactory.Create(EscribirArchivo, "escribir-archivo"),
        AIFunctionFactory.Create(ListarArchivos, "listar-archivos")
    ]
};


List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var vistaMarkdown = new Markdown {
    Width = Dim.Fill(),
    Height = Dim.Fill(2) 
};

var campoEntrada = new TextField {
    X = 0,
    Y = Pos.AnchorEnd(1),
    Width = Dim.Fill(12) 
};

var botonEnviar = new Button {
    Title = "Enviar",
    X = Pos.AnchorEnd(10), 
    Y = Pos.AnchorEnd(1),
    IsDefault = true 
};

ventana.Add(vistaMarkdown, campoEntrada, botonEnviar);

string historialPantalla = "Escribí un mensaje para comenzar.\n\n---\n\n";
vistaMarkdown.Text = historialPantalla;

bool procesando = false;

async Task EnviarMensaje()
{
    var textoUsuario = (string)campoEntrada.Text;
    if (procesando || string.IsNullOrWhiteSpace(textoUsuario)) return;

    procesando = true;
    app.Invoke(() => {
        campoEntrada.Enabled = false;
        botonEnviar.Enabled = false;
        campoEntrada.Text = "";
    });

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
    historialPantalla += $"# Vos\n\n{textoUsuario}\n\n# Asistente\n\n";
    app.Invoke(() => {
        vistaMarkdown.Text = historialPantalla;
        vistaMarkdown.SetNeedsDraw();
    });
    
    try 
    {
        var respuestaStream = chat.GetStreamingResponseAsync(mensajes, opcionesChat);
        var respuestaCompleta = "";

        await foreach (var fragmento in respuestaStream)
        {
            respuestaCompleta += fragmento.Text;
            
            app.Invoke(() => {
                vistaMarkdown.Text = historialPantalla + respuestaCompleta;
                vistaMarkdown.SetNeedsDraw();
            });
        }
        
        historialPantalla += respuestaCompleta + "\n\n---\n\n";
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta));
    }
    catch (Exception ex)
    {
        historialPantalla += $"*Error: {ex.Message}*\n\n";
        app.Invoke(() => {
            vistaMarkdown.Text = historialPantalla;
            vistaMarkdown.SetNeedsDraw();
        });
    }
    finally
    {
        app.Invoke(() => {
            procesando = false;
            campoEntrada.Enabled = true;
            botonEnviar.Enabled = true;
            campoEntrada.SetFocus(); 
            vistaMarkdown.SetNeedsDraw();
        });
    }
}

botonEnviar.Accepting += (s, e) => {
    _ = EnviarMensaje();
    e.Handled = true;
};

ventana.KeyDown += (s, e) => {
    if (e == Terminal.Gui.Input.Key.Esc)
    {
        app.RequestStop();
    }
};

app.Run(ventana);
