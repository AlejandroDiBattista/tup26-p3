#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
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

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var configuracion = ConfiguracionProveedor.Cargar(proveedor);

IChatClient chat = CrearCliente(configuracion);
ChatOptions opciones = CrearOpcionesHerramientas();
List<ChatMessage> mensajes = [new(ChatRole.System, File.ReadAllText("AGENTS.md"))];

using IApplication app = Application.Create().Init();
using var ventana = new AsistenteWindow(configuracion, chat, opciones, mensajes);
app.Run(ventana);

static IChatClient CrearCliente(ConfiguracionProveedor config) {
    var clienteBase = new OpenAIClient(
            new ApiKeyCredential(config.ApiKey),
            new OpenAIClientOptions { Endpoint = config.Endpoint })
        .GetChatClient(config.Modelo)
        .AsIChatClient();

    return clienteBase
        .AsBuilder()
        .UseFunctionInvocation()
        .Build();
}

static ChatOptions CrearOpcionesHerramientas() => new() {
    Tools = [
        AIFunctionFactory.Create(HerramientasArchivos.LeerArchivo, new() {
            Name = "leer-archivo",
            Description = "Lee el contenido completo de un archivo de texto del workspace."
        }),
        AIFunctionFactory.Create(HerramientasArchivos.EscribirArchivo, new() {
            Name = "escribir-archivo",
            Description = "Crea o sobrescribe un archivo de texto dentro del workspace."
        }),
        AIFunctionFactory.Create(HerramientasArchivos.ListarArchivos, new() {
            Name = "listar-archivos",
            Description = "Lista archivos y carpetas de un directorio del workspace."
        })
    ]
};

class AsistenteWindow : Window {
    readonly ConfiguracionProveedor configuracion;
    readonly IChatClient chat;
    readonly ChatOptions opciones;
    readonly List<ChatMessage> mensajes;
    readonly List<TurnoVisible> turnos = [];

    readonly Markdown conversacion = new() {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        Text = "# Asistente IA\n\nEscribi un mensaje abajo y presiona Enter o Enviar."
    };

    readonly TextField entrada = new() {
        X = 1,
        Y = 0,
        Width = Dim.Fill(13),
        CanFocus = true
    };

    readonly Button enviar = new() {
        Text = "Enviar",
        X = Pos.AnchorEnd(10),
        Y = 0,
        Width = 9
    };

    readonly Label estado = new() {
        Text = "Listo",
        X = 1,
        Y = Pos.AnchorEnd(1),
        Width = Dim.Fill(2),
        Height = 1
    };

    public AsistenteWindow(
        ConfiguracionProveedor configuracion,
        IChatClient chat,
        ChatOptions opciones,
        List<ChatMessage> mensajes) {
        this.configuracion = configuracion;
        this.chat = chat;
        this.opciones = opciones;
        this.mensajes = mensajes;

        Title = $" Asistente IA - {configuracion.Modelo} ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        ArmarInterfaz();
    }

    void ArmarInterfaz() {
        FrameView panelConversacion = new() {
            Title = "Conversacion",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(5)
        };

        FrameView panelEntrada = new() {
            Title = "Mensaje",
            X = 0,
            Y = Pos.Bottom(panelConversacion),
            Width = Dim.Fill(),
            Height = 4
        };

        panelConversacion.Add(conversacion);
        panelEntrada.Add(entrada, enviar);
        Add(panelConversacion, panelEntrada, estado);
    }

    void AgregarTurno(string rol, string texto) {
        turnos.Add(new(rol, texto));
        RenderizarConversacion();
    }

    void ActualizarUltimoTurno(string texto) {
        if (turnos.Count == 0) {
            return;
        }

        var ultimo = turnos[^1];
        turnos[^1] = ultimo with { Texto = texto };
        RenderizarConversacion();
    }

    void RenderizarConversacion() {
        if (turnos.Count == 0) {
            conversacion.Text = "# Asistente IA\n\nEscribi un mensaje abajo y presiona Enter o Enviar.";
            return;
        }

        conversacion.Text = string.Join(
            "\n\n---\n\n",
            turnos.Select(t => $"## {t.Rol}\n\n{t.Texto.Trim()}"));
    }
}

record TurnoVisible(string Rol, string Texto);

record ConfiguracionProveedor(string Nombre, Uri Endpoint, string ApiKey, string Modelo) {
    public static ConfiguracionProveedor Cargar(string nombre) {
        string url = Environment.GetEnvironmentVariable($"{nombre}_API_URL") ?? "https://api.openai.com/v1";
        string apiKey = Environment.GetEnvironmentVariable($"{nombre}_API_KEY") ?? "no-requiere-key";
        string modelo = Environment.GetEnvironmentVariable($"{nombre}_MODEL") ?? "gpt-5.4-mini";

        return new(nombre, NormalizarEndpoint(url), apiKey, modelo);
    }

    static Uri NormalizarEndpoint(string url) {
        string endpoint = url.Trim();
        if (endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) {
            endpoint = endpoint[..^"/chat/completions".Length];
        }

        return new Uri(endpoint);
    }
}

static class HerramientasArchivos {
    static readonly string Workspace = Path.GetFullPath("workspace");

    public static string LeerArchivo(
        [Description("Ruta relativa del archivo que se quiere leer.")] string ruta) {
        string archivo = ResolverRuta(ruta);

        return File.Exists(archivo)
            ? File.ReadAllText(archivo, Encoding.UTF8)
            : $"No existe el archivo: {ruta}";
    }

    public static string EscribirArchivo(
        [Description("Ruta relativa del archivo que se quiere crear o sobrescribir.")] string ruta,
        [Description("Contenido completo que se guardara en el archivo.")] string contenido) {
        string archivo = ResolverRuta(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
        File.WriteAllText(archivo, contenido, Encoding.UTF8);

        return $"Archivo guardado: {ruta}";
    }

    public static string ListarArchivos(
        [Description("Ruta relativa del directorio. Usar punto para listar la raiz.")] string ruta = ".") {
        string directorio = ResolverRuta(ruta);

        if (!Directory.Exists(directorio)) {
            return $"No existe el directorio: {ruta}";
        }

        var elementos = Directory.EnumerateFileSystemEntries(directorio)
            .Select(Path.GetFileName)
            .OrderBy(nombre => nombre);

        return string.Join(Environment.NewLine, elementos);
    }

    static string ResolverRuta(string ruta) {
        Directory.CreateDirectory(Workspace);

        string rutaCompleta = Path.GetFullPath(Path.Combine(Workspace, ruta));
        string prefijo = Workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (rutaCompleta != Workspace &&
            !rutaCompleta.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)) {
            throw new UnauthorizedAccessException("La ruta esta fuera del workspace del asistente.");
        }

        return rutaCompleta;
    }
}
