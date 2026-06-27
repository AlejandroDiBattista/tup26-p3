#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false
#:package Microsoft.Extensions.AI@10.4.0

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using System.ComponentModel;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();

var asistente = new Asistente(proveedor);
asistente.Registrar(ChatRole.System, File.ReadAllText("AGENTS.md"));

using IApplication app = Application.Create().Init();

using var ventana = new Window {
    Title = $" Asistente IA · {asistente.Modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill() - 3,
    Text = "\nEscribi tu mensaje abajo y presiona Enter o Enviar."
};

// entrada
var entrada = new TextField {
    X = 0,
    Y = Pos.Bottom(conversacion),
    Width = Dim.Fill() - 12
};

// boton enviar
var enviar = new Button {
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.Top(entrada)
};

ventana.Add(conversacion, entrada, enviar);

async void Mandar() {
    var texto = entrada.Text?.Trim() ?? "";
    if (texto.Length == 0) return;

    asistente.Registrar(ChatRole.User, texto);
    conversacion.Text += $"\n\n# Vos\n\n{texto}\n\n# Asistente\n\n";
    entrada.Text = "";
    IrAlFinal();

    var respuesta = await asistente.Respuesta(fragmento => {
        app.Invoke(() => { conversacion.Text += fragmento; });
        IrAlFinal();
    });

    asistente.Registrar(ChatRole.Assistant, respuesta);

    app.Invoke(() => {
    entrada.SetFocus();
    IrAlFinal();
    });
}

void IrAlFinal() {
    var alto  = conversacion.GetContentSize().Height;
    var vista = conversacion.Viewport;
    vista.Y = Math.Max(0, alto - vista.Height);
    conversacion.Viewport = vista;
}

entrada.Accepting += (sender, e) => {
    Mandar();
    e.Handled = true;
};

enviar.Accepting += (sender, e) => {
    Mandar();
    e.Handled = true;
};

app.Run(ventana);

class Asistente {
    private readonly IChatClient cliente;
    private readonly List<ChatMessage> historia = [];
    private readonly ChatOptions herramientas;

    public string Modelo { get; }

    public Asistente(string proveedor) {
        var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
        var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
        Modelo     = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";
        herramientas = new ChatOptions {
        Tools = [
            AIFunctionFactory.Create(Escribir, new() {
                Name        = "escribir-archivo",
                Description = "Crea o reemplaza un archivo de texto dentro del espacio de trabajo."
            }),
            AIFunctionFactory.Create(Leer, new() {
            Name        = "leer-archivo",
            Description = "Lee el contenido completo de un archivo dentro del espacio de trabajo."
            }),
            AIFunctionFactory.Create(Listar, new() {
            Name        = "listar-archivos",
            Description = "Lista los archivos y directorios de una ruta dentro del espacio de trabajo."
            })
            ]
        };

        cliente = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(url) })
            .GetChatClient(Modelo)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }

    public void Registrar(ChatRole rol, string texto) {
        historia.Add(new(rol, texto));
    }

    public async Task<string> Respuesta(Action<string> alLlegarFragmento) {
    var completa = "";
    await foreach (var fragmento in cliente.GetStreamingResponseAsync(historia, herramientas)) {
        var texto = fragmento.Text ?? "";
        completa += texto;
        alLlegarFragmento(texto);
    }
    return completa;
    }

    static string ResolverRuta(string ruta) {
    var workspace = Path.GetFullPath("src");
    Directory.CreateDirectory(workspace);

    var rutaCompleta  = Path.GetFullPath(Path.Combine(workspace, ruta));
    var prefijoValido = workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

    if (rutaCompleta != workspace && !rutaCompleta.StartsWith(prefijoValido, StringComparison.Ordinal)) {
        throw new UnauthorizedAccessException("No se puede acceder a rutas fuera del espacio de trabajo.");
    }

    return rutaCompleta;
    }

    static string Escribir(
    [Description("Ruta del archivo que se va a crear o reemplazar.")] string ruta,
    [Description("Contenido completo que se va a guardar en el archivo.")] string contenido) {

    var rutaCompleta = ResolverRuta(ruta);
    Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);
    File.WriteAllText(rutaCompleta, contenido);

    return $"Archivo guardado: {ruta}";
    }
    static string Leer([Description("Ruta del archivo que se va a leer.")] string ruta) {
    var rutaCompleta = ResolverRuta(ruta);
    return File.Exists(rutaCompleta)
        ? File.ReadAllText(rutaCompleta)
        : $"No se encontró el archivo: {ruta}";
    }

    static string Listar([Description("Ruta del directorio que se va a listar. Usa un punto para indicar la raiz.")] string ruta = ".") {
    var rutaCompleta = ResolverRuta(ruta);
    if (!Directory.Exists(rutaCompleta)) return $"No se encontró el directorio: {ruta}";

    var elementos = Directory.EnumerateFileSystemEntries(rutaCompleta).Select(Path.GetFileName);
    return string.Join(Environment.NewLine, elementos);
    }
}
