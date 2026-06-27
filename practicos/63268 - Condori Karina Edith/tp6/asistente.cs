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

var configuracion = ConfiguracionProveedor.Cargar(args);
var opciones = HerramientasArchivos.CrearOpciones();
var chat = configuracion.CrearCliente()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText(RutasProyecto.Archivo("AGENTS.md")))
};

using IApplication app = Application.Create().Init();
using var ventana = new VentanaPrincipal(configuracion.Modelo, mensajes, chat, opciones);
app.Run(ventana);

sealed class VentanaPrincipal : Window
{
    readonly List<ChatMessage> mensajes;
    readonly List<TurnoVisible> turnos = [];
    readonly IChatClient chat;
    readonly ChatOptions opciones;
    readonly Markdown conversacion;
    readonly TextField entrada;
    readonly Button enviar;
    readonly Label estado;

    public VentanaPrincipal(
        string modelo,
        List<ChatMessage> mensajes,
        IChatClient chat,
        ChatOptions opciones)
    {
        this.mensajes = mensajes;
        this.chat = chat;
        this.opciones = opciones;

        Title = $" Asistente IA - {modelo} ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        var panelConversacion = new FrameView
        {
            Title = "Conversacion",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(5)
        };

        conversacion = new Markdown
        {
            Text = "# Asistente IA\n\nEscribi un mensaje para comenzar.",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar
        };

        var panelEntrada = new FrameView
        {
            Title = "Mensaje",
            X = 0,
            Y = Pos.Bottom(panelConversacion),
            Width = Dim.Fill(),
            Height = 4
        };

        entrada = new TextField
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(13),
            Height = 1
        };

        enviar = new Button
        {
            Text = "Enviar",
            X = Pos.AnchorEnd(10),
            Y = 0,
            Width = 9
        };

        estado = new Label
        {
            Text = "Enter: enviar | Esc: salir",
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(2),
            Height = 1
        };

        panelConversacion.Add(conversacion);
        panelEntrada.Add(entrada, enviar);
        Add(panelConversacion, panelEntrada, estado);

        enviar.Accepting += async (_, args) =>
        {
            args.Handled = true;
            await EnviarMensajeAsync();
        };

        entrada.KeyDown += async (_, args) =>
        {
            if (args == Key.Enter)
            {
                args.Handled = true;
                await EnviarMensajeAsync();
            }
        };

        entrada.SetFocus();
    }

    async Task EnviarMensajeAsync()
    {
        var textoUsuario = entrada.Text?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(textoUsuario))
        {
            return;
        }

        entrada.Text = string.Empty;
        mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
        turnos.Add(new TurnoVisible("Vos", textoUsuario));
        RenderizarConversacion();
        await Task.CompletedTask;
    }

    void RenderizarConversacion()
    {
        if (turnos.Count == 0)
        {
            conversacion.Text = "# Asistente IA\n\nEscribi un mensaje para comenzar.";
            conversacion.SetNeedsDraw();
            return;
        }

        var markdown = new StringBuilder();
        foreach (var turno in turnos)
        {
            markdown.AppendLine($"# {turno.Rol}");
            markdown.AppendLine();
            markdown.AppendLine(string.IsNullOrWhiteSpace(turno.Texto) ? "_Procesando..._" : turno.Texto);
            markdown.AppendLine();
        }

        conversacion.Text = markdown.ToString();
        conversacion.SetNeedsDraw();
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Esc)
        {
            App?.RequestStop();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

sealed record TurnoVisible(string Rol, string Texto);

sealed record ConfiguracionProveedor(string Proveedor, string Url, string ApiKey, string Modelo)
{
    public static ConfiguracionProveedor Cargar(string[] args)
    {
        var proveedor = (args.Length > 0 ? args[0] : "openai").Trim().ToUpperInvariant();
        var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL")
            ?? "https://api.openai.com/v1";
        var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY")
            ?? "no-requiere-key";
        var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL")
            ?? "gpt-4o-mini";

        return new ConfiguracionProveedor(proveedor, NormalizarUrl(url), apiKey, modelo);
    }

    public IChatClient CrearCliente()
    {
        return new OpenAIClient(
                new ApiKeyCredential(ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(Url) })
            .GetChatClient(Modelo)
            .AsIChatClient();
    }

    static string NormalizarUrl(string url)
    {
        const string chatCompletions = "/chat/completions";
        var limpia = url.Trim().TrimEnd('/');

        return limpia.EndsWith(chatCompletions, StringComparison.OrdinalIgnoreCase)
            ? limpia[..^chatCompletions.Length]
            : limpia;
    }
}

static class RutasProyecto
{
    public static readonly string Raiz = Directory.GetCurrentDirectory();

    public static string Archivo(string nombre)
    {
        var ruta = Path.Combine(Raiz, nombre);
        if (File.Exists(ruta))
        {
            return ruta;
        }

        throw new FileNotFoundException($"No se encontro el archivo requerido: {nombre}", ruta);
    }

    public static string ResolverDentroDelProyecto(string ruta)
    {
        var relativa = string.IsNullOrWhiteSpace(ruta) ? "." : ruta;
        var completa = Path.GetFullPath(Path.Combine(Raiz, relativa));
        var prefijo = Raiz.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (completa != Raiz &&
            !completa.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La ruta debe estar dentro de la carpeta del proyecto.");
        }

        return completa;
    }
}

static class HerramientasArchivos
{
    public static ChatOptions CrearOpciones() => new()
    {
        Tools =
        [
            AIFunctionFactory.Create(LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto."),
            AIFunctionFactory.Create(EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado."),
            AIFunctionFactory.Create(ListarArchivos, "listar-archivos", "Lista los archivos y carpetas de un directorio.")
        ],
        ToolMode = ChatToolMode.Auto
    };

    [Description("Devuelve el contenido de un archivo de texto.")]
    static string LeerArchivo([Description("Ruta relativa del archivo a leer.")] string ruta)
    {
        var archivo = RutasProyecto.ResolverDentroDelProyecto(ruta);
        return File.Exists(archivo)
            ? File.ReadAllText(archivo)
            : $"No existe el archivo: {ruta}";
    }

    [Description("Crea o sobrescribe un archivo con el contenido indicado.")]
    static string EscribirArchivo(
        [Description("Ruta relativa del archivo a escribir.")] string ruta,
        [Description("Contenido completo que se guardara.")] string contenido)
    {
        var archivo = RutasProyecto.ResolverDentroDelProyecto(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
        File.WriteAllText(archivo, contenido);
        return $"Archivo escrito: {ruta}";
    }

    [Description("Lista los archivos y carpetas de un directorio.")]
    static string ListarArchivos([Description("Ruta relativa del directorio a listar.")] string ruta = ".")
    {
        var directorio = RutasProyecto.ResolverDentroDelProyecto(ruta);
        if (!Directory.Exists(directorio))
        {
            return $"No existe el directorio: {ruta}";
        }

        var entradas = Directory.EnumerateFileSystemEntries(directorio)
            .OrderBy(e => e)
            .Select(e => Directory.Exists(e)
                ? $"[carpeta] {Path.GetFileName(e)}"
                : $"[archivo] {Path.GetFileName(e)}");

        return string.Join(Environment.NewLine, entradas);
    }
}
