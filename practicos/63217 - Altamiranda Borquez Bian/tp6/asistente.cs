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
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();
if (args.Contains("Validar")) {
    Console.WriteLine("Validacion de compilscion Ok");
    return;
}
var proveedor = (args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "gemini").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gemini-2.5-flash";

if(string.IsNullOrWhiteSpace(url)) {
    Console.WriteLine($"Falta configurar {proveedor}_API_URL en el archivo .env.");
    return;
}
if(string.IsNullOrWhiteSpace(apiKey)&& proveedor != "OLLAMA") {
    Console.WriteLine($"Falta configurar {proveedor}_API_KEY en el archivo .env.");
    return;
}
var rutaProducto= Directory.GetCurrentDirectory();
var endpoint= NormalizarEndpoint(url);

IChatClient chatBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = endpoint })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient chat = new ChatClientBuilder(chatBase)
    .UseFunctionInvocation()
    .Build();

var opciones = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(HerramientasArchivos.LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto."),
        AIFunctionFactory.Create(HerramientasArchivos.EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado."),
        AIFunctionFactory.Create(HerramientasArchivos.ListarArchivos, "listar-archivos", "Lista archivos y carpetas de un directorio.")
    ]
};

List<ChatMessage> mensajes =
[
    new(ChatRole.System, CargarMensajeSistema(rutaProyecto))
];
using IApplication app = Application.Create().Init();
using var ventana = new VentanaAsistente(app, chat, opciones, mensajes, modelo, proveedor);
app.Run(ventana);

static Uri NormalizarEndpoint(string url)
{
    var limpia = url.Trim().TrimEnd('/');

    if (limpia.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
    {
        limpia = limpia[..^"/chat/completions".Length];
    }

    return new Uri(limpia);
}

static string CargarMensajeSistema(string rutaProyecto)
{
    var rutaAgente = Path.Combine(rutaProyecto, "AGENTS.md");
    return File.Exists(rutaAgente)
        ? File.ReadAllText(rutaAgente)
        : "Sos un asistente de programacion. Responde en espanol, claro y breve.";
}

record MensajePantalla(string Autor, string Texto);

class VentanaAsistente : Window
{
    private readonly IApplication app;
    private readonly IChatClient chat;
    private readonly ChatOptions opciones;
    private readonly List<ChatMessage> mensajes;
    private readonly List<MensajePantalla> mensajesPantalla = [];
    private readonly FrameView marcoConversacion;
    private readonly FrameView marcoEntrada;
    private readonly Markdown panelConversacion;
    private readonly TextField entrada;
    private readonly Button botonEnviar;
    private bool respondiendo;

    public VentanaAsistente(
        IApplication app,
        IChatClient chat,
        ChatOptions opciones,
        List<ChatMessage> mensajes,
        string modelo,
        string proveedor)
    {
        this.app = app;
        this.chat = chat;
        this.opciones = opciones;
        this.mensajes = mensajes;

        Title = $" AsistenteAI · {modelo} ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        marcoConversacion = new FrameView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(7),
            BorderStyle = LineStyle.Rounded
        };

        marcoEntrada = new FrameView
        {
            X = 0,
            Y = Pos.AnchorEnd(6),
            Width = Dim.Fill(),
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        panelConversacion = new Markdown
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(2),
            Text = "# ✦ Asistente\n\nEscribi una consulta y presiona Enter. Esc cierra la aplicacion.",
            CanFocus = true,
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar
        };

        entrada = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(18),
            Height = 1,
            Text = ""
        };

        botonEnviar = new Button
        {
            X = Pos.Right(entrada) + 1,
            Y = Pos.Top(entrada),
            Width = 15,
            Height = 1,
            Text = "► Enviar ◄",
            IsDefault = true
        };

        entrada.Accepted += async (_, _) => await EnviarMensajeAsync();
        botonEnviar.Accepted += async (_, _) => await EnviarMensajeAsync();

        app.Keyboard.KeyDown += (_, e) =>
        {
            if (e == Key.Esc)
            {
                e.Handled = true;
                app.RequestStop();
            }
        };

        marcoConversacion.Add(panelConversacion);
        marcoEntrada.Add(entrada, botonEnviar);
        Add(marcoConversacion, marcoEntrada);
    }

    private async Task EnviarMensajeAsync()
    {
        if (respondiendo)
        {
            return;
        }

        var textoUsuario = entrada.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(textoUsuario))
        {
            return;
        }

        entrada.Text = "";
        CambiarEstadoEntrada(false);

        mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
        mensajesPantalla.Add(new MensajePantalla("Vos", textoUsuario));
        mensajesPantalla.Add(new MensajePantalla("Asistente", ""));
        RefrescarPantalla();

        var respuesta = new StringBuilder();
        var actualizaciones = new List<ChatResponseUpdate>();

        try
        {
            await foreach (var parte in chat.GetStreamingResponseAsync(mensajes, opciones))
            {
                actualizaciones.Add(parte);

                if (string.IsNullOrEmpty(parte.Text))
                {
                    continue;
                }

                respuesta.Append(parte.Text);
                mensajesPantalla[^1] = mensajesPantalla[^1] with { Texto = respuesta.ToString() };
                RefrescarPantalla();
            }

            if (actualizaciones.Count > 0)
            {
                mensajes.AddMessages(actualizaciones);
            }
            else
            {
                mensajes.Add(new ChatMessage(ChatRole.Assistant, respuesta.ToString()));
            }
        }
        catch (Exception ex)
        {
            var error = $"No se pudo obtener respuesta: {ex.Message}";
            mensajesPantalla[^1] = mensajesPantalla[^1] with { Texto = error };
            mensajes.Add(new ChatMessage(ChatRole.Assistant, error));
            RefrescarPantalla();
        }
        finally
        {
            CambiarEstadoEntrada(true);
        }
    }

    private void CambiarEstadoEntrada(bool habilitado)
    {
        respondiendo = !habilitado;
        entrada.Enabled = habilitado;
        botonEnviar.Enabled = habilitado;

        if (habilitado)
        {
            entrada.SetFocus();
        }
    }

    private void RefrescarPantalla()
    {
        app.Invoke(() =>
        {
            panelConversacion.Text = CrearMarkdownConversacion();
            BajarAlFinal();
        });
    }

    private string CrearMarkdownConversacion()
    {
        var markdown = new StringBuilder();

        foreach (var mensaje in mensajesPantalla)
        {
            var titulo = mensaje.Autor == "Vos" ? "👤 Vos" : "✦ Asistente";
            markdown.Append("# ").AppendLine(titulo);
            markdown.AppendLine();
            markdown.AppendLine(mensaje.Texto);
            markdown.AppendLine();
        }

        return markdown.ToString();
    }

    private void BajarAlFinal()
    {
        var altoVisible = Math.Max(1, panelConversacion.Frame.Height);
        var y = Math.Max(0, panelConversacion.LineCount - altoVisible);
        panelConversacion.Viewport = panelConversacion.Viewport with { Y = y };
    }
}



