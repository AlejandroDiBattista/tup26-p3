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
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var configuracion = Configuracion.Cargar(args);
var herramientas = HerramientasDeArchivos.Crear();
var chat = configuracion.CrearCliente()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};

var opciones = new ChatOptions
{
    Tools = herramientas,
    ToolMode = ChatToolMode.Auto
};

using IApplication app = Application.Create().Init();
using var ventana = new VentanaPrincipal(configuracion.Modelo, mensajes, chat, opciones);
app.Run(ventana);

sealed record Configuracion(string Proveedor, string Url, string ApiKey, string Modelo)
{
    public static Configuracion Cargar(string[] args)
    {
        var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
        var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
        var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
        var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException($"Falta configurar {proveedor}_API_URL.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = "no-requiere-key";
        }

        return new Configuracion(proveedor, url, apiKey, modelo);
    }

    public IChatClient CrearCliente()
    {
        return new OpenAIClient(
                new ApiKeyCredential(ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(Url) })
            .GetChatClient(Modelo)
            .AsIChatClient();
    }
}

static class HerramientasDeArchivos
{
    public static IList<AITool> Crear()
    {
        return
        [
            AIFunctionFactory.Create(LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto."),
            AIFunctionFactory.Create(EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado."),
            AIFunctionFactory.Create(ListarArchivos, "listar-archivos", "Lista los archivos y carpetas de un directorio.")
        ];
    }

    [Description("Devuelve el contenido de un archivo de texto.")]
    static string LeerArchivo([Description("Ruta del archivo a leer.")] string ruta)
    {
        var rutaCompleta = Path.GetFullPath(ruta);
        return File.ReadAllText(rutaCompleta);
    }

    [Description("Crea o sobrescribe un archivo con el contenido indicado.")]
    static string EscribirArchivo(
        [Description("Ruta del archivo a crear o sobrescribir.")] string ruta,
        [Description("Contenido que se escribira en el archivo.")] string contenido)
    {
        var rutaCompleta = Path.GetFullPath(ruta);
        var carpeta = Path.GetDirectoryName(rutaCompleta);

        if (!string.IsNullOrWhiteSpace(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        File.WriteAllText(rutaCompleta, contenido);
        return $"Archivo escrito: {rutaCompleta}";
    }

    [Description("Lista los archivos y carpetas de un directorio.")]
    static string ListarArchivos([Description("Ruta del directorio a listar.")] string ruta)
    {
        var rutaCompleta = Path.GetFullPath(string.IsNullOrWhiteSpace(ruta) ? "." : ruta);

        if (!Directory.Exists(rutaCompleta))
        {
            return $"No existe el directorio: {rutaCompleta}";
        }

        var entradas = Directory.EnumerateFileSystemEntries(rutaCompleta)
            .OrderBy(entrada => entrada)
            .Select(entrada => Directory.Exists(entrada)
                ? $"[carpeta] {Path.GetFileName(entrada)}"
                : $"[archivo] {Path.GetFileName(entrada)}");

        return string.Join(Environment.NewLine, entradas);
    }
}

sealed record MensajePantalla(ChatRole Rol, string Texto);

sealed class VentanaPrincipal : Window
{
    readonly List<ChatMessage> mensajes;
    readonly List<MensajePantalla> historial = [];
    readonly IChatClient chat;
    readonly ChatOptions opciones;
    readonly Markdown conversacion;
    readonly TextField entrada;
    readonly Button enviar;
    bool respondiendo;

    public VentanaPrincipal(
        string modelo,
        List<ChatMessage> mensajes,
        IChatClient chat,
        ChatOptions opciones)
    {
        this.mensajes = mensajes;
        this.chat = chat;
        this.opciones = opciones;

        Title = $" AsistenteIA · {modelo} ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        conversacion = new Markdown
        {
            Text = "# Asistente MEAI\n\nEscribi un mensaje para comenzar.",
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
            CanFocus = true
        };

        entrada = new TextField
        {
            X = 0,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(12),
            Height = 3
        };

        enviar = new Button
        {
            Text = "Enviar",
            X = Pos.AnchorEnd(11),
            Y = Pos.AnchorEnd(3),
            Width = 11,
            Height = 3
        };

        Add(conversacion, entrada, enviar);

        enviar.Accepting += async (_, args) =>
        {
            args.Handled = true;
            await EnviarMensajeAsync();
        };

        entrada.KeyDown += async (_, args) =>
        {
            if (args.KeyCode == Key.Enter.KeyCode)
            {
                args.Handled = true;
                await EnviarMensajeAsync();
            }
        };

        KeyDown += (_, args) =>
        {
            if (args.KeyCode == Key.Esc.KeyCode)
            {
                args.Handled = true;
                Application.RequestStop();
            }
        };
    }

    async Task EnviarMensajeAsync()
    {
        if (respondiendo)
        {
            return;
        }

        var textoUsuario = entrada.Text?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(textoUsuario))
        {
            return;
        }

        respondiendo = true;
        entrada.Enabled = false;
        enviar.Enabled = false;
        entrada.Text = string.Empty;

        mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
        historial.Add(new MensajePantalla(ChatRole.User, textoUsuario));
        var respuesta = new MensajePantalla(ChatRole.Assistant, string.Empty);
        historial.Add(respuesta);
        Renderizar();

        var acumulado = new StringBuilder();

        try
        {
            await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opciones))
            {
                if (!string.IsNullOrEmpty(fragmento.Text))
                {
                    acumulado.Append(fragmento.Text);
                    historial[^1] = respuesta with { Texto = acumulado.ToString() };
                    Application.Invoke(Renderizar);
                }
            }

            mensajes.Add(new ChatMessage(ChatRole.Assistant, acumulado.ToString()));
        }
        catch (Exception ex)
        {
            var error = $"No se pudo obtener respuesta: {ex.Message}";
            historial[^1] = respuesta with { Texto = error };
            mensajes.Add(new ChatMessage(ChatRole.Assistant, error));
            Application.Invoke(Renderizar);
        }
        finally
        {
            Application.Invoke(() =>
            {
                entrada.Enabled = true;
                enviar.Enabled = true;
                respondiendo = false;
                entrada.SetFocus();
            });
        }
    }

    void Renderizar()
    {
        var markdown = new StringBuilder();

        foreach (var mensaje in historial)
        {
            markdown.AppendLine(mensaje.Rol == ChatRole.User ? "# Vos" : "# Asistente");
            markdown.AppendLine();
            markdown.AppendLine(mensaje.Texto);
            markdown.AppendLine();
        }

        if (historial.Count == 0)
        {
            markdown.AppendLine("# Asistente MEAI");
            markdown.AppendLine();
            markdown.AppendLine("Escribi un mensaje para comenzar.");
        }

        conversacion.Text = markdown.ToString();
        conversacion.SetNeedsDraw();
    }
}
