#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ClientModel;
using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;



DotNetEnv.Env.Load();


string nombreProveedor = args.Length > 0 ? args[0].ToUpper() : "GROQ";

string? urlApi   = Environment.GetEnvironmentVariable($"{nombreProveedor}_API_URL");
string? claveApi = Environment.GetEnvironmentVariable($"{nombreProveedor}_API_KEY");
string? nombreModelo = Environment.GetEnvironmentVariable($"{nombreProveedor}_MODEL") ?? "llama3-8b-8192";

if (string.IsNullOrWhiteSpace(urlApi))
{
    Console.Error.WriteLine($"No encontré la variable {nombreProveedor}_API_URL en el .env.");
    Console.Error.WriteLine("Revisá que el archivo .env tenga configurado ese proveedor.");
    return;
}


string urlBase = urlApi.Trim().TrimEnd('/');
if (urlBase.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
    urlBase = urlBase[..^"/chat/completions".Length];


IChatClient clienteIA = new OpenAIClient(
        new ApiKeyCredential(claveApi ?? "sin-clave"),
        new OpenAIClientOptions { Endpoint = new Uri(urlBase) })
    .GetChatClient(nombreModelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation() 
    .Build();


var herramientas = new ChatOptions { Tools = FuncionesDeArchivos.ObtenerLista() };


var historial = new List<ChatMessage>
{
    new(ChatRole.System, LeerArchivoDeAgentes())
};


using IApplication interfaz = Application.Create().Init();
using var pantalla = new PantallaChat(interfaz, clienteIA, herramientas, historial, nombreModelo);
interfaz.Run(pantalla);


static string LeerArchivoDeAgentes()
{
    string[] posiblesRutas = ["AGENTS.md", Path.Combine(AppContext.BaseDirectory, "AGENTS.md")];
    foreach (string ruta in posiblesRutas)
    {
        if (File.Exists(ruta))
            return File.ReadAllText(ruta);
    }
    return "Sos un asistente técnico. Respondé siempre en español, de forma clara y concisa.";
}



static class FuncionesDeArchivos
{
    
    public static IList<AITool> ObtenerLista() =>
    [
        AIFunctionFactory.Create(Leer,    "leer-archivo",     "Devuelve el contenido de texto de un archivo."),
        AIFunctionFactory.Create(Guardar, "escribir-archivo", "Crea o sobreescribe un archivo con el texto indicado."),
        AIFunctionFactory.Create(Listar,  "listar-archivos",  "Lista el contenido de un directorio."),
    ];

    [Description("Lee el contenido de un archivo de texto y lo devuelve como string.")]
    static string Leer([Description("Ruta al archivo.")] string ruta)
    {
        try
        {
            if (!File.Exists(ruta))
                return $"El archivo '{ruta}' no existe.";
            return File.ReadAllText(ruta);
        }
        catch (Exception ex)
        {
            return $"No se pudo leer '{ruta}': {ex.Message}";
        }
    }

    [Description("Escribe texto en un archivo. Si ya existe, lo sobreescribe.")]
    static string Guardar(
        [Description("Ruta donde guardar el archivo.")] string ruta,
        [Description("Texto a guardar en el archivo.")] string contenido)
    {
        try
        {
     
            string? carpeta = Path.GetDirectoryName(Path.GetFullPath(ruta));
            if (!string.IsNullOrEmpty(carpeta))
                Directory.CreateDirectory(carpeta);

            File.WriteAllText(ruta, contenido);
            return $"Guardado correctamente: {contenido.Length} caracteres en '{ruta}'.";
        }
        catch (Exception ex)
        {
            return $"No se pudo guardar '{ruta}': {ex.Message}";
        }
    }

    [Description("Lista archivos y carpetas dentro de un directorio.")]
    static string Listar([Description("Ruta del directorio. Usá '.' para el actual.")] string ruta = ".")
    {
        try
        {
            if (!Directory.Exists(ruta))
                return $"El directorio '{ruta}' no existe.";

            var resultado = new StringBuilder();

            foreach (string carpeta in Directory.GetDirectories(ruta))
                resultado.AppendLine($"📁 {Path.GetFileName(carpeta)}/");

            foreach (string archivo in Directory.GetFiles(ruta))
                resultado.AppendLine($"📄 {Path.GetFileName(archivo)}");

            return resultado.Length == 0 ? "(carpeta vacía)" : resultado.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"No se pudo listar '{ruta}': {ex.Message}";
        }
    }
}




sealed class ControladorChat
{
    readonly IChatClient _cliente;
    readonly ChatOptions _opciones;
    readonly List<ChatMessage> _historial;

   
    public Action<string>? AlActualizarTexto;   
    public Action<string>? AlTerminar;           
    public Action<string>? AlOcurrirError;      

    public bool EstaRespondiendo { get; private set; } = false;

    public ControladorChat(IChatClient cliente, ChatOptions opciones, List<ChatMessage> historial)
    {
        _cliente   = cliente;
        _opciones  = opciones;
        _historial = historial;
    }

   
    public string ArmarTextoConversacion(string? fragmentoActual = null)
    {
        var sb = new StringBuilder();

        foreach (var mensaje in _historial)
        {
            if (mensaje.Role == ChatRole.User)
            {
                sb.AppendLine($"[ VOS ]");
                sb.AppendLine(mensaje.Text);
                sb.AppendLine();
            }
            else if (mensaje.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(mensaje.Text))
            {
                sb.AppendLine($"[ ASISTENTE ]");
                sb.AppendLine(mensaje.Text);
                sb.AppendLine();
            }
        }

   
        if (fragmentoActual != null)
        {
            sb.AppendLine("[ ASISTENTE ]");
            sb.Append(fragmentoActual);
        }

        if (sb.Length == 0)
            sb.AppendLine("Escribí un mensaje y presioná Enter para chatear. Esc para salir.");

        return sb.ToString();
    }

   
    public async Task EnviarAsync(string textoUsuario)
    {
        if (EstaRespondiendo || string.IsNullOrWhiteSpace(textoUsuario))
            return;

        EstaRespondiendo = true;
        _historial.Add(new ChatMessage(ChatRole.User, textoUsuario));

        var respuestaCompleta = new List<ChatResponseUpdate>();
        var textoParcial      = new StringBuilder();
        Exception? falla      = null;

        int tokenesDesdeUltimoRender = 0;

        try
        {
            await Task.Run(async () =>
            {
                await foreach (var fragmento in _cliente.GetStreamingResponseAsync(_historial, _opciones))
                {
                    respuestaCompleta.Add(fragmento);

                    if (!string.IsNullOrEmpty(fragmento.Text))
                    {
                        textoParcial.Append(fragmento.Text);
                        tokenesDesdeUltimoRender++;

                       
                        if (tokenesDesdeUltimoRender >= 5)
                        {
                            tokenesDesdeUltimoRender = 0;
                            string parcial = textoParcial.ToString();
                            AlActualizarTexto?.Invoke(ArmarTextoConversacion(parcial));
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            falla = ex;
        }

        
        if (falla == null)
        {
            _historial.AddMessages(respuestaCompleta);
            AlTerminar?.Invoke(ArmarTextoConversacion());
        }
        else
        {
            string mensajeError = $"⚠️ Error al contactar al modelo: {falla.Message}";
            _historial.Add(new ChatMessage(ChatRole.Assistant, mensajeError));
            AlOcurrirError?.Invoke(ArmarTextoConversacion());
        }

        EstaRespondiendo = false;
    }
}



sealed class PantallaChat : Window
{
    readonly IApplication _interfaz;
    readonly ControladorChat _controlador;
    readonly Markdown _areaConversacion;
    readonly TextField _campoMensaje;
    readonly Button _botonEnviar;

    public PantallaChat(
        IApplication interfaz,
        IChatClient cliente,
        ChatOptions opciones,
        List<ChatMessage> historial,
        string nombreModelo)
    {
        _interfaz    = interfaz;
        _controlador = new ControladorChat(cliente, opciones, historial);

        _controlador.AlActualizarTexto = texto => EjecutarEnUI(() => RefrescarConversacion(texto));
        _controlador.AlTerminar        = texto => EjecutarEnUI(() =>
        {
            RefrescarConversacion(texto);
            HabilitarEntrada(true);
        });
        _controlador.AlOcurrirError    = texto => EjecutarEnUI(() =>
        {
            RefrescarConversacion(texto);
            HabilitarEntrada(true);
        });

        Title  = $" AsistenteIA · {nombreModelo} ";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        // ── Zona de conversación ──────────────────────────────────────────────
        var marcoConversacion = new FrameView
        {
            Title  = "Conversación",
            X      = 0,
            Y      = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(4),
        };

      
        _areaConversacion = new Markdown
        {
            X      = 0,
            Y      = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
        };

      
        try { _areaConversacion.SyntaxHighlighter = new TextMateSyntaxHighlighter(); }
        catch { }
        marcoConversacion.Add(_areaConversacion);

        // ── Zona de entrada ───────────────────────────────────────────────────
        var marcoEntrada = new FrameView
        {
            Title  = "Mensaje (Enter para enviar · Esc para salir)",
            X      = 0,
            Y      = Pos.Bottom(marcoConversacion),
            Width  = Dim.Fill(),
            Height = 4,
        };

        _botonEnviar = new Button
        {
            Text      = " Enviar ",
            X         = Pos.AnchorEnd(),
            Y         = 0,
        };

        _campoMensaje = new TextField
        {
            X      = 0,
            Y      = 0,
            Height = 1,
            Width  = Dim.Fill() - Dim.Width(_botonEnviar) - 1,
        };

        marcoEntrada.Add(_campoMensaje, _botonEnviar);
        Add(marcoConversacion, marcoEntrada);

        // ── Eventos ───────────────────────────────────────────────────────────

      
        _campoMensaje.Accepting += (_, e) =>
        {
            e.Handled = true;
            _ = ManejarEnvioAsync();
        };

       
        _botonEnviar.Accepting += (_, e) =>
        {
            e.Handled = true;
            _ = ManejarEnvioAsync();
        };

    
        Initialized += (_, _) =>
        {
            _campoMensaje.SetFocus();
            RefrescarConversacion(_controlador.ArmarTextoConversacion());
        };
    }

  
    void RefrescarConversacion(string texto)
    {
        _areaConversacion.Text = texto;

       
        int altoContenido = _areaConversacion.GetContentSize().Height;
        int altoVisible   = _areaConversacion.Viewport.Height;
        int posY          = Math.Max(0, altoContenido - altoVisible);
        _areaConversacion.Viewport = _areaConversacion.Viewport with { Y = posY };
    }

  
    void HabilitarEntrada(bool habilitar)
    {
        _campoMensaje.Enabled = habilitar;
        _botonEnviar.Enabled  = habilitar;
        if (habilitar)
            _campoMensaje.SetFocus();
    }

   
    async Task ManejarEnvioAsync()
    {
        if (_controlador.EstaRespondiendo)
            return;

        string mensaje = (_campoMensaje.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(mensaje))
            return;

        _campoMensaje.Text = string.Empty;
        HabilitarEntrada(false);

       
        RefrescarConversacion(_controlador.ArmarTextoConversacion("…"));

        await _controlador.EnviarAsync(mensaje);
    }

  
    void EjecutarEnUI(Action accion)
    {
        try { _interfaz.Invoke(accion); }
        catch { }
    }
}