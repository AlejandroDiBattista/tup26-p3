#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
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

// 1. Inicialización y Carga de Configuración desde el .env
DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen/qwen3.6-27b";

// Lista de herramientas del sistema de archivos mapeadas como tipo AITool
var herramientas = new List<AITool>
{
    AIFunctionFactory.Create(LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto"),
    AIFunctionFactory.Create(EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado"),
    AIFunctionFactory.Create(ListarArchivos, "listar-archivos", "Lista los archivos y carpetas de un directorio")
};

// Cliente base de chat compatible con OpenAI
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url ?? "https://api.groq.com/openai/v1") })
    .GetChatClient(modelo)
    .AsIChatClient();

// 2. Historial de Conversación
List<ChatMessage> mensajes = [];

if (File.Exists("AGENTS.md"))
{
    mensajes.Add(new ChatMessage(ChatRole.System, File.ReadAllText("AGENTS.md")));
}
else
{
    mensajes.Add(new ChatMessage(ChatRole.System, "Sos un asistente útil de terminal. Disponés de herramientas para leer, escribir y listar archivos."));
}

// 3. Inicialización de la Interfaz Gráfica de Terminal (TUI)
using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var panelConversacion = new Markdown {
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Percent(80),
    Text = "# ¡Bienvenido al Asistente IA!\nEscribí tu mensaje abajo para empezar a conversar."
};

var panelEntrada = new FrameView {
    Title = " Tu Mensaje ",
    X = 0, Y = Pos.Bottom(panelConversacion),
    Width = Dim.Fill(), Height = Dim.Fill()
};

var campoTexto = new TextField {
    X = 1, Y = 1,
    Width = Dim.Percent(85), Height = 1
};

var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.Right(campoTexto) + 1, Y = 1
};

panelEntrada.Add(campoTexto, botonEnviar);
ventana.Add(panelConversacion, panelEntrada);

// 4. Lógica de Envío de Mensajes y Streaming
string historialMarkdown = "";

async Task EnviarMensajeUsuario()
{
    var textoUsuario = campoTexto.Text.Trim();
    if (string.IsNullOrEmpty(textoUsuario)) return;

    campoTexto.Enabled = false;
    botonEnviar.Enabled = false;
    campoTexto.Text = string.Empty;

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
    historialMarkdown += $"\n\n# Vos\n{textoUsuario}\n\n# Asistente\n";
    panelConversacion.Text = historialMarkdown;

    try
    {
        var opcionesChat = new ChatOptions { Tools = herramientas };
        string respuestaAcumulada = "";

        await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opcionesChat))
        {
            if (fragmento.Text != null)
            {
                respuestaAcumulada += fragmento.Text;
                panelConversacion.Text = historialMarkdown + respuestaAcumulada;
                
                panelConversacion.SetNeedsDraw();
            }
        }

        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaAcumulada));
        historialMarkdown += respuestaAcumulada;
    }
    catch (Exception ex)
    {
        historialMarkdown += $"*Error al conectar con la IA: {ex.Message}*";
        panelConversacion.Text = historialMarkdown;
    }
    finally
    {
        campoTexto.Enabled = true;
        botonEnviar.Enabled = true;
        campoTexto.SetFocus();
    }
}

// 5. Manejo de Eventos A PRUEBA DE BALAS mediante comparación de strings
botonEnviar.Accepting += async (s, e) => {
    await EnviarMensajeUsuario();
};

campoTexto.KeyDown += async (s, e) => {
    // Convertimos la tecla a string (suele devolver "Enter", "Return" o similar)
    string teclaStr = e?.ToString()?.ToUpperInvariant() ?? "";
    if (teclaStr.Contains("ENTER") || teclaStr.Contains("RETURN"))
    {
        e?.Handled = true;
        await EnviarMensajeUsuario();
    }
};

ventana.KeyDown += (s, e) => {
    string teclaStr = e?.ToString()?.ToUpperInvariant() ?? "";
    if (teclaStr.Contains("ESC") || teclaStr.Contains("ESCAPE"))
    {
        e?.Handled = true;
        app!.RequestStop();
    }
};

// Ejecución de la aplicación de terminal
app!.Run(ventana);


// --- IMPLEMENTACIÓN DE LAS HERRAMIENTAS (Function Calling) ---

static string LeerArchivo([Description("Ruta del archivo de texto a leer")] string ruta)
{
    try
    {
        if (!File.Exists(ruta)) return $"Error: El archivo '{ruta}' no existe.";
        return File.ReadAllText(ruta);
    }
    catch (Exception ex)
    {
        return $"Error al leer el archivo: {ex.Message}";
    }
}

static string EscribirArchivo(
    [Description("Ruta del archivo a crear o modificar")] string ruta, 
    [Description("Contenido de texto a escribir en el archivo")] string contenido)
{
    try
    {
        File.WriteAllText(ruta, contenido);
        return $"Éxito: Archivo '{ruta}' guardado correctamente.";
    }
    catch (Exception ex)
    {
        return $"Error al escribir el archivo: {ex.Message}";
    }
}

static string ListarArchivos([Description("Ruta del directorio a listar")] string ruta)
{
    try
    {
        string directorio = string.IsNullOrEmpty(ruta) ? Directory.GetCurrentDirectory() : ruta;
        if (!Directory.Exists(directorio)) return $"Error: El directorio '{directorio}' no existe.";

        var archivos = Directory.GetFileSystemEntries(directorio);
        if (archivos.Length == 0) return $"El directorio '{directorio}' está vacío.";

        return $"Archivos en '{directorio}':\n" + string.Join("\n", archivos.Select(Path.GetFileName));
    }
    catch (Exception ex)
    {
        return $"Error al listar archivos: {ex.Message}";
    }
}