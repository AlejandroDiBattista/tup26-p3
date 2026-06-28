#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Microsoft.Extensions.AI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using System.ComponentModel;
using System.Text;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "groq").ToUpperInvariant();
var url = NormalizarEndpoint(Environment.GetEnvironmentVariable($"{proveedor}_API_URL")
    ?? "https://api.groq.com/openai/v1/chat/completions");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "sin-api-key";
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen/qwen3.6-27b";

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};

var opciones = new ChatOptions {
    Tools = CrearHerramientasDeArchivos()
};

var turnos = new List<TurnoMostrado>();

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown {
    Text = "# Asistente IA\n\nListo para conversar.",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    CanFocus = true
};

var entrada = new TextField {
    X = 0,
    Y = Pos.AnchorEnd(3),
    Width = Dim.Fill(12),
    Height = 1
};

var enviar = new Button {
    Text = "Enviar",
    X = Pos.AnchorEnd(10),
    Y = Pos.AnchorEnd(3),
    Width = 10,
    Height = 1,
    IsDefault = true
};

var estado = new Label {
    Text = "Enter: enviar | Esc: salir",
    X = 0,
    Y = Pos.AnchorEnd(2),
    Width = Dim.Fill(),
    Height = 1
};

ventana.Add(conversacion, entrada, enviar, estado);
entrada.SetFocus();

enviar.Accepting += (_, e) => {
    e.Handled = true;
    _ = EnviarMensajeAsync();
};

entrada.KeyDown += (_, key) => {
    if (key == Key.Enter) {
        key.Handled = true;
        _ = EnviarMensajeAsync();
    }
};

ventana.KeyDown += (_, key) => {
    if (key == Key.Esc) {
        key.Handled = true;
        app.RequestStop(ventana);
    }
};

app.Run(ventana);

async Task EnviarMensajeAsync() {
    var textoUsuario = entrada.Text?.ToString()?.Trim();
    if (string.IsNullOrWhiteSpace(textoUsuario) || !entrada.Enabled) {
        return;
    }

    entrada.Text = string.Empty;
    entrada.Enabled = false;
    enviar.Enabled = false;
    estado.Text = "El asistente esta respondiendo...";

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
    turnos.Add(new TurnoMostrado("Vos", textoUsuario));
    var respuesta = new StringBuilder();
    turnos.Add(new TurnoMostrado("Asistente", string.Empty));
    RefrescarConversacion();

    try {
        await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opciones)) {
            if (string.IsNullOrEmpty(fragmento.Text)) {
                continue;
            }

            respuesta.Append(fragmento.Text);
            turnos[^1] = turnos[^1] with { Texto = respuesta.ToString() };
            app.Invoke(RefrescarConversacion);
        }

        var textoAsistente = respuesta.ToString();
        mensajes.Add(new ChatMessage(ChatRole.Assistant, textoAsistente));
    } catch (Exception ex) {
        var error = $"No se pudo obtener respuesta del modelo.\n\nDetalle: `{ex.Message}`";
        turnos[^1] = turnos[^1] with { Texto = error };
        mensajes.Add(new ChatMessage(ChatRole.Assistant, error));
        app.Invoke(RefrescarConversacion);
    } finally {
        app.Invoke(() => {
            entrada.Enabled = true;
            enviar.Enabled = true;
            estado.Text = "Enter: enviar | Esc: salir";
            entrada.SetFocus();
        });
    }
}

void RefrescarConversacion() {
    var estabaAbajo = conversacion.VerticalScrollBar.Value >=
        Math.Max(0, conversacion.VerticalScrollBar.ScrollableContentSize - conversacion.VerticalScrollBar.VisibleContentSize - 1);

    conversacion.Text = RenderizarTurnos(turnos);
    conversacion.SetNeedsDraw();

    if (estabaAbajo) {
        conversacion.VerticalScrollBar.Value = Math.Max(0,
            conversacion.VerticalScrollBar.ScrollableContentSize - conversacion.VerticalScrollBar.VisibleContentSize);
    }
}

static string RenderizarTurnos(IEnumerable<TurnoMostrado> turnos) {
    var markdown = new StringBuilder("# Conversacion\n");

    foreach (var turno in turnos) {
        markdown.AppendLine();
        markdown.Append("## ").AppendLine(turno.Autor);
        markdown.AppendLine();
        markdown.AppendLine(string.IsNullOrWhiteSpace(turno.Texto) ? "_Escribiendo..._" : turno.Texto);
    }

    return markdown.ToString();
}

static IList<AITool> CrearHerramientasDeArchivos() {
    return
    [
        AIFunctionFactory.Create(LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto del proyecto."),
        AIFunctionFactory.Create(EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo de texto dentro del proyecto."),
        AIFunctionFactory.Create(ListarArchivos, "listar-archivos", "Lista los archivos y carpetas de un directorio del proyecto.")
    ];
}

[Description("Devuelve el contenido de un archivo de texto del proyecto.")]
static string LeerArchivo([Description("Ruta relativa del archivo a leer.")] string ruta) {
    var archivo = ResolverRutaProyecto(ruta);

    if (!File.Exists(archivo)) {
        return $"No existe el archivo: {ruta}";
    }

    return File.ReadAllText(archivo);
}

[Description("Crea o sobrescribe un archivo de texto dentro del proyecto.")]
static string EscribirArchivo(
    [Description("Ruta relativa del archivo a crear o sobrescribir.")] string ruta,
    [Description("Contenido completo que se escribira en el archivo.")] string contenido) {
    var archivo = ResolverRutaProyecto(ruta);
    Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);

    var existia = File.Exists(archivo);
    File.WriteAllText(archivo, contenido);

    return existia
        ? $"Archivo sobrescrito correctamente: {ruta}"
        : $"Archivo creado correctamente: {ruta}";
}

[Description("Lista los archivos y carpetas de un directorio del proyecto.")]
static string ListarArchivos([Description("Ruta relativa del directorio a listar.")] string ruta) {
    var directorio = ResolverRutaProyecto(string.IsNullOrWhiteSpace(ruta) ? "." : ruta);

    if (!Directory.Exists(directorio)) {
        return $"No existe el directorio: {ruta}";
    }

    var entradas = Directory.EnumerateFileSystemEntries(directorio)
        .OrderBy(entrada => entrada)
        .Select(entrada => Directory.Exists(entrada)
            ? $"[dir]  {Path.GetFileName(entrada)}"
            : $"[file] {Path.GetFileName(entrada)}");

    return string.Join(Environment.NewLine, entradas);
}

static string ResolverRutaProyecto(string ruta)
{
    var raiz = Path.GetFullPath(Directory.GetCurrentDirectory());
    var destino = Path.GetFullPath(Path.Combine(raiz, ruta));

    if (!destino.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La ruta debe estar dentro del proyecto.");
    }

    return destino;
}

static string NormalizarEndpoint(string endpoint)
{
    endpoint = endpoint.TrimEnd('/');

    if (endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
    {
        endpoint = endpoint[..^"/chat/completions".Length];
    }

    return endpoint;
}
record TurnoMostrado(string Autor, string Texto);