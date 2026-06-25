#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-4o-mini";

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url!) })
    .GetChatClient(modelo)
    .AsIChatClient();

AIFunction leerArchivo = AIFunctionFactory.Create(
    ([System.ComponentModel.Description("Ruta del archivo a leer")] string ruta) =>
        File.Exists(ruta)
            ? File.ReadAllText(ruta)
            : $"Error: el archivo '{ruta}' no existe.",
    "leer-archivo",
    "Devuelve el contenido de un archivo de texto.");

AIFunction escribirArchivo = AIFunctionFactory.Create(
    ([System.ComponentModel.Description("Ruta del archivo a crear o sobrescribir")] string ruta,
     [System.ComponentModel.Description("Contenido a escribir en el archivo")] string contenido) =>
    {
        var dir = Path.GetDirectoryName(ruta);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(ruta, contenido);
        return $"Archivo '{ruta}' escrito correctamente ({contenido.Length} caracteres).";
    },
    "escribir-archivo",
    "Crea o sobrescribe un archivo con el contenido indicado.");

AIFunction listarArchivos = AIFunctionFactory.Create(
    ([System.ComponentModel.Description("Ruta del directorio a listar")] string ruta) =>
        Directory.Exists(ruta)
            ? string.Join("\n", Directory.EnumerateFileSystemEntries(ruta)
                .Select(e => (Directory.Exists(e) ? "[DIR] " : "      ") + Path.GetFileName(e)))
            : $"Error: el directorio '{ruta}' no existe.",
    "listar-archivos",
    "Lista los archivos y carpetas de un directorio.");

var herramientas = new List<AITool> { leerArchivo, escribirArchivo, listarArchivos };
var opcionesChat = new ChatOptions { Tools = herramientas };

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" AsistenteIA . {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var etiquetaTitulo = new Label
{
    Text = "[ Asistente MEAI ]",
    X = 0,
    Y = 0,
    Width = Dim.Auto(),
    Height = 1,
};

var panelConversacion = new Markdown
{
    Text = "Escribi un mensaje para comenzar.",
    Width = Dim.Fill(),
    Height = Dim.Fill() - 5,
    X = 0,
    Y = 1,
};

var etiquetaAyuda = new Label
{
    Text = "[Enter] Enviar  [Esc] Salir",
    X = 0,
    Y = Pos.Bottom(panelConversacion),
    Width = Dim.Fill(),
    Height = 1,
};

var campoTexto = new TextField
{
    X = 0,
    Y = Pos.Bottom(etiquetaAyuda) + 1,
    Width = Dim.Fill() - 12,
    Height = 1,
};

var botonEnviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(campoTexto) + 1,
    Y = Pos.Top(campoTexto),
    IsDefault = false,
};

ventana.Add(etiquetaTitulo, panelConversacion, etiquetaAyuda, campoTexto, botonEnviar);

var textoConversacion = new System.Text.StringBuilder();
textoConversacion.Append("Escribi un mensaje para comenzar.");
bool respondiendo = false;

void ActualizarPanel()
{
    panelConversacion.Text = textoConversacion.ToString();
    panelConversacion.SetNeedsDraw();
    ventana.SetNeedsDraw();
}

void HabilitarEntrada(bool habilitar)
{
    campoTexto.Enabled  = habilitar;
    botonEnviar.Enabled = habilitar;
}

async Task<string> EjecutarHerramientaAsync(FunctionCallContent llamada)
{
    var herramienta = herramientas.OfType<AIFunction>()
        .FirstOrDefault(h => h.Name == llamada.Name);
    if (herramienta is null)
        return $"Error: herramienta '{llamada.Name}' no encontrada.";
    try
    {
        var resultado = await herramienta.InvokeAsync(
            new AIFunctionArguments(llamada.Arguments ?? new Dictionary<string, object?>()));
        return resultado?.ToString() ?? string.Empty;
    }
    catch (Exception ex)
    {
        return $"Error al ejecutar '{llamada.Name}': {ex.Message}";
    }
}

async Task EnviarMensajeAsync()
{
    if (respondiendo) return;

    var textoUsuario = campoTexto.Text?.Trim() ?? string.Empty;
    if (string.IsNullOrEmpty(textoUsuario)) return;

    campoTexto.Text = string.Empty;
    HabilitarEntrada(false);
    respondiendo = true;

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
    textoConversacion.Append($"\n\n---\n\n**Vos:** {textoUsuario}\n\n**Asistente:** Pensando...");
    ActualizarPanel();

    var bufferRespuesta = new System.Text.StringBuilder();

    try
    {
        bool seguir = true;
        while (seguir)
        {
            seguir = false;
            bufferRespuesta.Clear();

            var llamadas = new List<FunctionCallContent>();

            await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opcionesChat))
            {
                if (!string.IsNullOrEmpty(fragmento.Text))
                {
                    bufferRespuesta.Append(fragmento.Text);

                    var textoActual = textoConversacion.ToString();
                
                    var marcador = "**Asistente:** Pensando...";
                    var idx = textoActual.LastIndexOf(marcador, StringComparison.Ordinal);
                    if (idx >= 0)
                    {
                        textoConversacion.Clear();
                        textoConversacion.Append(textoActual[..idx]);
                        textoConversacion.Append("**Asistente:** ");
                        textoConversacion.Append(bufferRespuesta);
                    }
                    else
                    {
                        marcador = "**Asistente:** ";
                        idx = textoActual.LastIndexOf(marcador, StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            textoConversacion.Clear();
                            textoConversacion.Append(textoActual[..(idx + marcador.Length)]);
                            textoConversacion.Append(bufferRespuesta);
                        }
                    }

                    ActualizarPanel();
                }

                foreach (var contenido in fragmento.Contents.OfType<FunctionCallContent>())
                    llamadas.Add(contenido);
            }

            if (llamadas.Count > 0)
            {
                var msgAsistente = new ChatMessage(ChatRole.Assistant,
                    llamadas.Select(l => (AIContent)l).ToList());
                mensajes.Add(msgAsistente);

                foreach (var llamada in llamadas)
                {
                    var resultadoTexto = await EjecutarHerramientaAsync(llamada);
                    mensajes.Add(new ChatMessage(ChatRole.Tool,
                        [new FunctionResultContent(llamada.CallId ?? llamada.Name, resultadoTexto)]));
                }

                textoConversacion.Append("\n\n**Asistente:** Pensando...");
                ActualizarPanel();
                seguir = true;
            }
            else
            {
                mensajes.Add(new ChatMessage(ChatRole.Assistant, bufferRespuesta.ToString()));
            }
        }
    }
    catch (Exception ex)
    {
        File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n\n");
        textoConversacion.Append($"\n\n> Error: {ex.Message}");
        ActualizarPanel();
    }
    finally
    {
        respondiendo = false;
        HabilitarEntrada(true);
    }
}

botonEnviar.Accepting += async (_, _) => await EnviarMensajeAsync();

campoTexto.KeyDown += async (_, e) =>
{
    if (e.KeyCode == Key.Enter)
    {
        e.Handled = true;
        await EnviarMensajeAsync();
    }
};

ventana.KeyDown += (_, e) =>
{
    if (e.KeyCode == Key.Esc)
    {
        Application.RequestStop();
        e.Handled = true;
    }
};

campoTexto.SetFocus();
app.Run(ventana);