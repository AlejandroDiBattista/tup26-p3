#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url       = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey    = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo    = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

if (string.IsNullOrWhiteSpace(url))
{
    Console.Error.WriteLine(
        $"Falta la variable de entorno {proveedor}_API_URL. " +
        "Copiá .env.example a .env y configurá el proveedor.");
    return;
}

var urlBase = url.Trim().TrimEnd('/');
if (urlBase.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
{
    urlBase = urlBase[..^"/chat/completions".Length];
}

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(urlBase) })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();


var opciones = new ChatOptions { Tools = HerramientasArchivos.Crear() };

List<ChatMessage> mensajes = [new(ChatRole.System, CargarPromptDeSistema())];

using IApplication app = Application.Create().Init();
using var ventana = new VentanaAsistente(app, chat, opciones, mensajes, modelo);
app.Run(ventana);

static string CargarPromptDeSistema()
{
    foreach (var ruta in new[] { "AGENTS.md", Path.Combine(AppContext.BaseDirectory, "AGENTS.md") })
    {
        if (File.Exists(ruta))
        {
            return File.ReadAllText(ruta);
        }
    }

    return "Sos un asistente de programación. Respondé en español, de forma directa y técnica. "
         + "Si falta contexto, pedí el dato mínimo necesario.";
}
static class HerramientasArchivos
{
    public static IList<AITool> Crear() =>
    [
        AIFunctionFactory.Create(LeerArchivo,     "leer-archivo",     "Devuelve el contenido de un archivo de texto."),
        AIFunctionFactory.Create(EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado."),
        AIFunctionFactory.Create(ListarArchivos,  "listar-archivos",  "Lista los archivos y carpetas de un directorio."),
    ];

    [Description("Devuelve el contenido de texto de un archivo.")]
    static string LeerArchivo(
        [Description("Ruta del archivo a leer.")] string ruta)
    {
        try
        {
            return File.Exists(ruta)
                ? File.ReadAllText(ruta)
                : $"ERROR: no existe el archivo '{ruta}'.";
        }
        catch (Exception ex)
        {
            return $"ERROR al leer '{ruta}': {ex.Message}";
        }
    }

    [Description("Crea o sobrescribe un archivo de texto con el contenido indicado.")]
    static string EscribirArchivo(
        [Description("Ruta del archivo a crear o sobrescribir.")] string ruta,
        [Description("Contenido de texto a escribir en el archivo.")] string contenido)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(ruta));
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(ruta, contenido);
            return $"OK: se escribieron {contenido.Length} caracteres en '{ruta}'.";
        }
        catch (Exception ex)
        {
            return $"ERROR al escribir '{ruta}': {ex.Message}";
        }
    }

    [Description("Lista los archivos y carpetas de un directorio.")]
    static string ListarArchivos(
        [Description("Ruta del directorio a listar. Usá '.' para el directorio actual.")] string ruta = ".")
    {
        try
        {
            if (!Directory.Exists(ruta))
            {
                return $"ERROR: no existe el directorio '{ruta}'.";
            }

            var lineas = new List<string>();
            foreach (var d in Directory.GetDirectories(ruta))
            {
                lineas.Add($"[carpeta] {Path.GetFileName(d)}/");
            }
            foreach (var f in Directory.GetFiles(ruta))
            {
                lineas.Add($"[archivo] {Path.GetFileName(f)}");
            }

            return lineas.Count == 0 ? "(directorio vacío)" : string.Join("\n", lineas);
        }
        catch (Exception ex)
        {
            return $"ERROR al listar '{ruta}': {ex.Message}";
        }
    }
}

sealed class VistaConversacion : Markdown
{
    public bool SeguirAlFinal { get; set; } = true;

    bool _ajustando;

    public VistaConversacion()
    {
        try { SyntaxHighlighter = new TextMateSyntaxHighlighter(); }
        catch {}

        ViewportChanged += (_, _) =>
        {
            if (!_ajustando)
            {
                SeguirAlFinal = EstaAlFinal();
            }
        };
    }

    bool EstaAlFinal()
    {
        int alto    = GetContentSize().Height;
        int visible = Viewport.Height;
        return alto <= visible || Viewport.Y >= alto - visible;
    }

    void IrAlFinal()
    {
        int alto    = GetContentSize().Height;
        int visible = Viewport.Height;
        int maxY    = Math.Max(0, alto - visible);
        if (Viewport.Y != maxY)
        {
            Viewport = Viewport with { Y = maxY };
        }
    }

    protected override void OnSubViewLayout(LayoutEventArgs args)
    {
        bool previo = _ajustando;
        _ajustando = true;

        base.OnSubViewLayout(args);
        if (SeguirAlFinal)
        {
            IrAlFinal();
        }

        _ajustando = previo;
    }
}
sealed class VentanaAsistente : Window
{
    readonly IApplication _app;
    readonly IChatClient _chat;
    readonly ChatOptions _opciones;
    readonly List<ChatMessage> _mensajes;

    readonly VistaConversacion _conversacion;
    readonly TextField _entrada;
    readonly Button _enviar;

    bool _ocupado;

    public VentanaAsistente(
        IApplication app,
        IChatClient chat,
        ChatOptions opciones,
        List<ChatMessage> mensajes,
        string modelo)
    {
        _app      = app;
        _chat     = chat;
        _opciones = opciones;
        _mensajes = mensajes;

        Title  = $" AsistenteIA · {modelo} ";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        var panelConversacion = new FrameView
        {
            X = 0,
            Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(3),
        };

        _conversacion = new VistaConversacion
        {
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
        };
        panelConversacion.Add(_conversacion);

        var panelEntrada = new FrameView
        {
            X = 0,
            Y = Pos.Bottom(panelConversacion),
            Width  = Dim.Fill(),
            Height = 3,
        };

        _enviar = new Button
        {
            Text = "Enviar",
            IsDefault = true,      
            X = Pos.AnchorEnd(),      
            Y = 0,
        };

        _entrada = new TextField
        {
            X = 0,
            Y = 0,
            Height = 1,
            Width  = Dim.Fill() - Dim.Width(_enviar) - 1,
        };

        panelEntrada.Add(_entrada, _enviar);

        Add(panelConversacion, panelEntrada);

        _enviar.Accepting += (_, e) =>
        {
            e.Handled = true;
            _ = EnviarMensajeAsync();
        };

        _entrada.Accepting += (_, e) =>
        {
            e.Handled = true;
            _ = EnviarMensajeAsync();
        };

        Initialized += (_, _) => _entrada.SetFocus();

        Render(null);
    }

    void Render(string? respuestaEnCurso)
    {
        var sb = new StringBuilder();

        foreach (var m in _mensajes)
        {
            if (m.Role == ChatRole.User)
            {
                sb.Append("# 👤 Vos\n\n").Append(m.Text).Append("\n\n");
            }
            else if (m.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(m.Text))
            {
                sb.Append("# 🤖 Asistente\n\n").Append(m.Text).Append("\n\n");
            }
        }

        if (respuestaEnCurso is not null)
        {
            sb.Append("# 🤖 Asistente\n\n").Append(respuestaEnCurso).Append("\n\n");
        }

        if (sb.Length == 0)
        {
            sb.Append("_Escribí tu mensaje abajo y presioná Enter. Esc para salir._\n");
        }

        _conversacion.Text = sb.ToString();
    }

    async Task EnviarMensajeAsync()
    {
        if (_ocupado)
        {
            return;
        }

        string texto = (_entrada.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        _ocupado = true;
        _entrada.Enabled = false;
        _enviar.Enabled = false;
        _entrada.Text = string.Empty;

        _mensajes.Add(new ChatMessage(ChatRole.User, texto));
        _conversacion.SeguirAlFinal = true;    
        Render("…");                           

        var fragmentos = new List<ChatResponseUpdate>();
        var sb         = new StringBuilder();
        Exception? error = null;

        try
        {
           
            await Task.Run(async () =>
            {
                var reloj = Stopwatch.StartNew();
                long ultimoRender = 0;

                await foreach (var fragmento in _chat.GetStreamingResponseAsync(_mensajes, _opciones))
                {
                    fragmentos.Add(fragmento);
                    if (string.IsNullOrEmpty(fragmento.Text))
                    {
                        continue;
                    }

                    sb.Append(fragmento.Text);

                    long ahora = reloj.ElapsedMilliseconds;
                    if (ahora - ultimoRender >= 50)
                    {
                        ultimoRender = ahora;
                        string parcial = sb.ToString();
                        EnHiloDeUI(() => Render(parcial));
                    }
                }
            });
        }
        catch (Exception ex)
        {
            error = ex;
        }

        EnHiloDeUI(() =>
        {
            if (error is null)
            {
                _mensajes.AddMessages(fragmentos);
            }
            else
            {
                _mensajes.Add(new ChatMessage(ChatRole.Assistant,
                    $"⚠️ No se pudo obtener respuesta del modelo: {error.Message}"));
            }

            Render(null);
            _entrada.Enabled = true;
            _enviar.Enabled = true;
            _entrada.SetFocus();
            _ocupado = false;
        });
    }

    void EnHiloDeUI(Action accion)
    {
        try { _app.Invoke(accion); }
        catch {}
    }
}
