#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.7.0
#:package Microsoft.Extensions.AI.OpenAI@10.7.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


// =================== CONFIGURACION ====================
DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gemini-2.5-flash";


// ================== CLIENTE DE IA ====================

IChatClient clienteBase = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient cliente = new ChatClientBuilder(clienteBase)
    .UseFunctionInvocation()
    .Build();

// ================== HERRAMIENTAS PARA ARCHIVOS ====================
[Description("Lee el contenido de un archivo")]
static string LeerArchivo([Description("Ruta del archivo")] string ruta) =>
    File.Exists(ruta) ? File.ReadAllText(ruta) : $"No existe: {ruta}";

[Description("Crea o sobreescribe un archivo con el texto dado")]
static string EscribirArchivo(
    [Description("Ruta")] string ruta,
    [Description("Contenido")] string contenido) {
    File.WriteAllText(ruta, contenido);
    return $"Guardado: {ruta}";
}

[Description("Lista archivos y carpetas de un directorio")]
static string ListarArchivos([Description("Ruta (vacío = actual)")] string ruta = "") {
    var carpeta = string.IsNullOrEmpty(ruta) ? "." : ruta;
    var items   = Directory.GetFileSystemEntries(carpeta);
    return items.Length == 0 ? "Vacío." : string.Join("\n", items);
}

var opciones = new ChatOptions {
    Tools = [
        AIFunctionFactory.Create(LeerArchivo,      "leer-archivo"),
        AIFunctionFactory.Create(EscribirArchivo,  "escribir-archivo"),
        AIFunctionFactory.Create(ListarArchivos,   "listar-archivos")
    ]
};

// ================== HISTORIAL Y GUARDADO DE CONVERSACION ====================
var historial = new List<ChatMessage> {
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};

var archivoSalida = "salida.md";
File.WriteAllText(archivoSalida,
    $"# IAWizard\nModel: {modelo}\nDate: {DateTime.Now:dd/MM/yyyy HH:mm}\n---\n\n");


// =============================================
using IApplication app = Application.Create().Init();
app.Run(new VentanaAsistente(app, cliente, historial, opciones, modelo, archivoSalida));


// ================== VENTANA PRINCIPAL ====================

class VentanaAsistente : Window {

    Markdown _vistaChat;
    TextField _campoTexto;
    Label _estado;
    StringBuilder _textoAcumulado = new();

    readonly IChatClient _cliente;
    readonly List<ChatMessage> _historial;
    readonly ChatOptions _opciones;
    readonly string _archivoSalida;
    readonly IApplication _app;

    public VentanaAsistente(
        IApplication app,
        IChatClient cliente,
        List<ChatMessage> historial,
        ChatOptions opciones,
        string modelo,
        string archivoSalida) {

        _app          = app;
        _cliente       = cliente;
        _historial     = historial;
        _opciones      = opciones;
        _archivoSalida = archivoSalida;

        Title  = $" AsistenteIA · {modelo} ";
        Width  = Dim.Fill();
        Height = Dim.Fill();


        _vistaChat = new Markdown {
            X      = 0,
            Y      = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill() - 6
        };

    
        _estado = new Label {
            Text   = " Escribí tu mensaje y presioná Enter. ESC para salir.",
            X      = 0,
            Y      = Pos.Bottom(_vistaChat),
            Width  = Dim.Fill(),
            Height = 1
        };


        _campoTexto = new TextField {
            Text   = "",
            X      = 0,
            Y      = Pos.Bottom(_estado),
            Width  = Dim.Fill() - 2,
            Height = 3
        };

        var boton = new Button {
            Text      = "ENVIAR",
            X         = Pos.Right(_campoTexto) + 1,
            Y         = Pos.Bottom(_estado),
            IsDefault = true
        };

        _campoTexto.KeyDown += async (_, e) => {
            if (e.KeyCode == KeyCode.Enter && !string.IsNullOrWhiteSpace(_campoTexto.Text)) {
                e.Handled = true;
                await EnviarAsync();
            }
        };

    
        boton.Accepting += async (_, e) => {
            e.Handled = true;
            await EnviarAsync();
        };


        KeyDown += (_, e) => {
            if (e.KeyCode == KeyCode.Esc) {
                e.Handled = true;
                _app.RequestStop();
            }
        };

        _textoAcumulado.AppendLine(
            "# BIENVENIDO AL ASISTENTE IA DE EMMANUEL\n\n" +
            "Escribí tu consulta y presioná **Enter**.\n\n");
        _vistaChat.Text = _textoAcumulado.ToString();

        Add(_vistaChat, _estado, _campoTexto, boton);
        _campoTexto.SetFocus();
    }


    async Task EnviarAsync() {
        var texto = _campoTexto.Text?.ToString()?.Trim() ?? "";
        if (string.IsNullOrEmpty(texto)) return;

        _campoTexto.Text    = "";
        _campoTexto.Enabled = false;

        // MOSTRAR MENSAJE DEL USUARIO
        _textoAcumulado.AppendLine($"# ── YO\n{texto}\n");
        _app.Invoke(() => _vistaChat.Text = _textoAcumulado.ToString());
        File.AppendAllText(_archivoSalida, $"[ YO | {DateTime.Now:HH:mm:ss} ]\n{texto}\n\n");

        _historial.Add(new(ChatRole.User, texto));
        _textoAcumulado.AppendLine("# ── ASISTENTE\n");
        _app.Invoke(() => {
            _vistaChat.Text = _textoAcumulado.ToString();
            _estado.Text    = "Escribiendo...";
        });

        var respuesta = new StringBuilder();

        try {
            // MOSTRAR CADA PARTE DEL MENSAJE QUE LLEGA
            await foreach (var parte in _cliente.GetStreamingResponseAsync(_historial, _opciones)) {
                var fragmento = parte.Text ?? "";
                if (!string.IsNullOrEmpty(fragmento)) {
                    respuesta.Append(fragmento);
                    var parcial = _textoAcumulado + respuesta.ToString();
                    _app.Invoke(() => _vistaChat.Text = parcial);
                }
            }

            // FIJO LA RESPUESTA COMPLETA EN EL ACUMULADOR
            _textoAcumulado.Append(respuesta);
            _textoAcumulado.AppendLine("\n---\n");
            _app.Invoke(() => _vistaChat.Text = _textoAcumulado.ToString());

            var final = respuesta.ToString();
            File.AppendAllText(_archivoSalida, $"[ ASISTENTE | {DateTime.Now:HH:mm:ss} ]\n{final}\n\n");
            _historial.Add(new(ChatRole.Assistant, final));

            _app.Invoke(() => _estado.Text = $" Historial guardado en: {_archivoSalida}");

        } catch (Exception ex) {
            var errorMessage = ObtenerMensajeError(ex);
            _textoAcumulado.AppendLine($"\n**Error:** {errorMessage}\n");
            _app.Invoke(() => _vistaChat.Text = _textoAcumulado.ToString());
            _app.Invoke(() => _estado.Text    = $" ✗ {errorMessage}");
            File.AppendAllText(_archivoSalida, $"[ ERROR | {DateTime.Now:HH:mm:ss} ]\n{errorMessage}\n\n");
        }

        _app.Invoke(() => {
            _campoTexto.Enabled = true;
            _campoTexto.SetFocus();
        });
    }

    static string ObtenerMensajeError(Exception error) {
        var builder = new StringBuilder();
        builder.Append(error.GetType().Name).Append(": ").Append(error.Message);

        var inner = error.InnerException;
        while (inner != null) {
            builder.Append(" -> ")
                   .Append(inner.GetType().Name)
                   .Append(": ")
                   .Append(inner.Message);
            inner = inner.InnerException;
        }

        return builder.ToString();
    }

}

