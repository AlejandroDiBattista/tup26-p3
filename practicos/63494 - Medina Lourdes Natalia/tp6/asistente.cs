#!/usr/bin/env -S dotnet run

#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
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
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "https://api.openai.com/v1";
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";
var errorConfiguracion = ValidarConfiguracion(proveedor, url, apiKey, modelo);


IChatClient chat = new ChatClientBuilder(
        new OpenAIClient(
                new ApiKeyCredential(apiKey ?? "no-requiere-key"),
                new OpenAIClientOptions { Endpoint = new Uri(url) })
            .GetChatClient(modelo)
            .AsIChatClient())
    .UseFunctionInvocation()
    .Build();


var opciones = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto del proyecto."),
        AIFunctionFactory.Create(EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo de texto dentro del proyecto."),
        AIFunctionFactory.Create(ListarArchivos, "listar-archivos", "Lista los archivos y carpetas de un directorio del proyecto.")
    ]
};

List<ChatMessage> mensajes =
[
    new(ChatRole.System, CargarPromptSistema())
];

var turnos = new List<TurnoVisible>();
var respondiendo = false;
var autoScroll = true;


using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" AsistenteIA - {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var panelConversacion = new FrameView
{
    X = 1,
    Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(5)
};

var conversacion = new Markdown
{
    Text = errorConfiguracion is null
        ? RenderizarConversacion(turnos)
        : $"# Configuracion\n\n{errorConfiguracion}",
    X = 1,
    Y = 0,
    Width = Dim.Fill(2),
    Height = Dim.Fill(),
    ShowHeadingPrefix = false
};

var panelEntrada = new FrameView
{
    X = 1,
    Y = Pos.AnchorEnd(4),
    Width = Dim.Fill(),
    Height = 4
};

var entrada = new TextField
{
    X = 1,
    Y = 1,
    Width = Dim.Fill(14)
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.AnchorEnd(12),
    Y = 1,
    Width = 10
};

panelConversacion.Add(conversacion);
panelEntrada.Add(entrada, enviar);
ventana.Add(panelConversacion, panelEntrada);

ventana.KeyDown += (_, e) =>
{
    if (e.KeyCode == Key.Esc)
    {
        app.RequestStop();
        e.Handled = true;
    }
};

entrada.KeyDown += (_, e) =>
{
    if (e.KeyCode == Key.Enter)
    {
        _ = EnviarMensajeAsync();
        e.Handled = true;
    }
};

enviar.Accepting += (_, e) =>
{
    _ = EnviarMensajeAsync();
    e.Handled = true;
};

entrada.SetFocus();

if (errorConfiguracion is not null)
{
    entrada.Enabled = false;
    enviar.Enabled = false;
}

app.Run(ventana);

async Task EnviarMensajeAsync()
{
    var texto = entrada.Text?.ToString()?.Trim();
    if (string.IsNullOrWhiteSpace(texto) || respondiendo || errorConfiguracion is not null)
    {
        return;
    }

    respondiendo = true;
    entrada.Text = "";
    entrada.Enabled = false;
    enviar.Enabled = false;

    mensajes.Add(new ChatMessage(ChatRole.User, texto));
    turnos.Add(new TurnoVisible("Vos", texto));
    var turnoAsistente = new TurnoVisible("Asistente", "");
    turnos.Add(turnoAsistente);
    ActualizarConversacion();

    try
    {
        await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
            if (!string.IsNullOrEmpty(fragmento.Text))
            {
                turnoAsistente.Texto += fragmento.Text;
                ActualizarConversacion();
            }
        }

        mensajes.Add(new ChatMessage(ChatRole.Assistant, turnoAsistente.Texto));
    }
    catch (Exception ex)
    {
        turnoAsistente.Texto += $"\n\n> Error: {ExplicarErrorApi(ex)}";
        ActualizarConversacion();
    }
    finally
    {
        respondiendo = false;
        entrada.Enabled = true;
        enviar.Enabled = true;
        entrada.SetFocus();
    }
}

void ActualizarConversacion()
{
    app.Invoke(() =>
    {
        conversacion.Text = RenderizarConversacion(turnos);

        conversacion.SetNeedsDraw();

        conversacion.ScrollVertical(100000);
    });
}

static string RenderizarConversacion(IEnumerable<TurnoVisible> turnos)
{
    var partes = turnos.Select(t => $"### {IconoRol(t.Autor)} {t.Autor}\n{t.Texto.TrimEnd()}");
    var texto = string.Join("\n\n", partes);
    return string.IsNullOrWhiteSpace(texto)
        ? "### Asistente\nEscribi tu mensaje y presiona Enter."
        : texto;
}

static string IconoRol(string autor) => autor == "Vos" ? "●" : "✦";

static string CargarPromptSistema()
{
    const string archivo = "AGENTS.md";
    return File.Exists(archivo)
        ? File.ReadAllText(archivo)
        : "Sos un asistente conversacional claro, amable y preciso. Responde siempre en espanol rioplatense.";
}

static string? ValidarConfiguracion(string proveedor, string url, string? apiKey, string modelo)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out _))
    {
        return $"La variable {proveedor}_API_URL no es una URL valida: `{url}`.";
    }

    if (string.IsNullOrWhiteSpace(apiKey) ||
        apiKey.Equals("tu-clave", StringComparison.OrdinalIgnoreCase) ||
        apiKey.Contains("pega-tu-key", StringComparison.OrdinalIgnoreCase))
    {
        return $"Falta configurar {proveedor}_API_KEY en el archivo `.env`.";
    }

    if (string.IsNullOrWhiteSpace(modelo))
    {
        return $"Falta configurar {proveedor}_MODEL en el archivo `.env`.";
    }

    return null;
}

static string ExplicarErrorApi(Exception ex)
{
    var mensaje = ex.Message;

    if (mensaje.Contains("401", StringComparison.OrdinalIgnoreCase) ||
        mensaje.Contains("invalid_api_key", StringComparison.OrdinalIgnoreCase))
    {
        return "La API key no es valida o fue revocada. Revisa OPENAI_API_KEY en `.env`.";
    }

    if (mensaje.Contains("429", StringComparison.OrdinalIgnoreCase) ||
        mensaje.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase) ||
        mensaje.Contains("quota", StringComparison.OrdinalIgnoreCase))
    {
        return "La cuenta no tiene cuota disponible o falta configurar billing. Revisa el plan/creditos del proveedor.";
    }

    if (mensaje.Contains("model", StringComparison.OrdinalIgnoreCase) &&
        (mensaje.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
         mensaje.Contains("does not exist", StringComparison.OrdinalIgnoreCase)))
    {
        return "El modelo configurado no esta disponible para esta cuenta. Revisa OPENAI_MODEL en `.env`.";
    }

    return mensaje;
}

static string LeerArchivo(string ruta)
{
    var path = ResolverRutaSegura(ruta);
    return File.Exists(path)
        ? File.ReadAllText(path)
        : $"No existe el archivo: {ruta}";
}

static string EscribirArchivo(string ruta, string contenido)
{
    var path = ResolverRutaSegura(ruta);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, contenido);
    return $"Archivo escrito: {Path.GetRelativePath(Environment.CurrentDirectory, path)}";
}

static string ListarArchivos(string ruta = ".")
{
    var path = ResolverRutaSegura(ruta);
    if (!Directory.Exists(path))
    {
        return $"No existe el directorio: {ruta}";
    }

    return string.Join(Environment.NewLine,
        Directory.EnumerateFileSystemEntries(path)
            .OrderBy(Directory.Exists)
            .ThenBy(Path.GetFileName)
            .Select(p => Directory.Exists(p)
                ? $"[dir]  {Path.GetFileName(p)}"
                : $"[file] {Path.GetFileName(p)}"));
}

static string ResolverRutaSegura(string ruta)
{
    var raiz = Path.GetFullPath(Environment.CurrentDirectory);
    var path = Path.GetFullPath(Path.Combine(raiz, ruta));

    if (!path.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La ruta debe estar dentro del directorio del proyecto.");
    }

    return path;
}

record TurnoVisible(string Autor, string Texto)
{
    public string Texto { get; set; } = Texto;
}