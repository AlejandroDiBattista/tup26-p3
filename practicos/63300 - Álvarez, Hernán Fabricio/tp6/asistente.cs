#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();
// Define proveedor a usar en este caso Gemini
var proveedor = (args.Length > 0 ? args[0] : "gemini").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gemini-2.5-flash";
// Lee el mensaje del sistema que marca el comportamiento inicial 

var promptSistema = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "Sos un asistente de programacion. Responde en espanol.";

// Inicializa Terminal.Gui y crea la ventana principal a pantalla completa.
using IApplication app = Application.Create().Init();

using var ventana = new Window
{
    Title = $" Asistente IA - {proveedor} - {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

// Panel superior: muestra toda la conversacion con formato Markdown.
var panelConversacion = new FrameView
{
    Title = " Conversacion ",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(4)
};

// Vista Markdown: permite mostrar titulos, listas y bloques de codigo en la terminal.
var vistaConversacion = new Markdown
{
    Text = "# Asistente IA\n\nConfigura `.env` y escribi tu consulta abajo.\n\n",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ShowHeadingPrefix = false,
    ShowCopyButtons = true
};

panelConversacion.Add(vistaConversacion);

// Panel inferior: contiene el campo donde escribe el usuario y el boton de envio.
var panelEntrada = new FrameView
{
    Title = " Mensaje ",
    X = 0,
    Y = Pos.Bottom(panelConversacion),
    Width = Dim.Fill(),
    Height = 4
};

// Campo de texto donde el usuario escribe su pregunta.
var entrada = new TextField
{
    X = 1,
    Y = 0,
    Width = Dim.Fill(13),
    Height = 1,
    CanFocus = true
};

// Boton que dispara el mismo envio que la tecla Enter.
var botonEnviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = 0,
    Width = 10,
    Height = 1
};

panelEntrada.Add(entrada, botonEnviar);
ventana.Add(panelConversacion, panelEntrada);

// Si falta configuracion, la app muestra el problema y bloquea el envio.
var configuracionValida = ValidarConfiguracion(proveedor, url, apiKey, out var mensajeConfiguracion);
if (!configuracionValida)
{
    vistaConversacion.Text = mensajeConfiguracion;
    entrada.Enabled = false;
    botonEnviar.Enabled = false;
}

IChatClient? chat = null;
ChatOptions? opcionesChat = null;

// Crea el cliente de chat y activa la invocacion automatica de herramientas.
if (configuracionValida)
{
    chat = CrearClienteChat(url!, apiKey!, modelo);
    chat = chat.AsBuilder()
        .UseFunctionInvocation()
        .Build();

    opcionesChat = new ChatOptions
    {
        Tools = CrearHerramientas(),
        ToolMode = ChatToolMode.Auto
    };
}

// Historial que se envia al modelo. Incluye el prompt de sistema y todos los turnos.
var mensajesChat = new List<ChatMessage>
{
    new(ChatRole.System, promptSistema)
};

// Historial visual que se renderiza en pantalla.
var mensajesPantalla = new List<MensajePantalla>();
var respondiendo = false;

// Eventos principales de la interfaz: enviar con Enter/boton y salir con Esc.
entrada.Accepted += (_, _) => EnviarMensaje();
botonEnviar.Accepted += (_, _) => EnviarMensaje();
ventana.KeyDown += (_, tecla) =>
{
    if (tecla == Key.Esc || tecla.KeyCode == KeyCode.Esc)
    {
        app.RequestStop();
    }
};

if (configuracionValida)
{
    entrada.SetFocus();
}

app.Run(ventana);

// Toma el texto del usuario, lo agrega al historial y pide la respuesta al modelo.
void EnviarMensaje()
{
    if (respondiendo || chat is null || opcionesChat is null)
    {
        return;
    }

    var textoUsuario = entrada.Text?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(textoUsuario))
    {
        return;
    }

    respondiendo = true;
    entrada.Text = "";
    entrada.Enabled = false;
    botonEnviar.Enabled = false;

    // Guarda el mensaje del usuario en ambos historiales.
    mensajesChat.Add(new ChatMessage(ChatRole.User, textoUsuario));
    mensajesPantalla.Add(new MensajePantalla("Vos", textoUsuario));

    // Reserva un lugar para la respuesta mientras se va generando por streaming.
    var indiceRespuesta = mensajesPantalla.Count;
    mensajesPantalla.Add(new MensajePantalla("Asistente", "_Pensando..._"));
    RefrescarConversacion(true);

    // Ejecuta la consulta fuera del hilo de la interfaz para que la terminal no se congele.
    _ = Task.Run(async () =>
    {
        var respuesta = new StringBuilder();

        try
        {
            // Se envia una copia del historial para conservar el contexto de la sesion.
            var mensajesParaEnviar = mensajesChat.ToArray();

            // Streaming: cada fragmento que llega se agrega y se muestra enseguida.
            await foreach (var parte in chat.GetStreamingResponseAsync(mensajesParaEnviar, opcionesChat))
            {
                if (string.IsNullOrEmpty(parte.Text))
                {
                    continue;
                }

                respuesta.Append(parte.Text);
                ActualizarRespuestaParcial(indiceRespuesta, respuesta.ToString());
            }

            // Al terminar, la respuesta completa queda guardada en el historial del modelo.
            var textoRespuesta = respuesta.Length == 0
                ? "No recibi texto como respuesta del modelo."
                : respuesta.ToString();

            mensajesChat.Add(new ChatMessage(ChatRole.Assistant, textoRespuesta));
            ActualizarRespuestaParcial(indiceRespuesta, textoRespuesta);
        }
        catch (Exception ex)
        {
            var error = $"Error al consultar el modelo: `{ex.Message}`";
            ActualizarRespuestaParcial(indiceRespuesta, error);
        }
        finally
        {
            // Reactiva los controles cuando termina la respuesta o si hubo error.
            app.Invoke(() =>
            {
                respondiendo = false;
                entrada.Enabled = true;
                botonEnviar.Enabled = true;
                entrada.SetFocus();
            });
        }
    });
}

// Actualiza una respuesta ya visible sin crear un mensaje nuevo.
void ActualizarRespuestaParcial(int indice, string texto)
{
    app.Invoke(() =>
    {
        mensajesPantalla[indice] = mensajesPantalla[indice] with { Texto = texto };
        RefrescarConversacion(true);
    });
}

// Vuelve a dibujar la conversacion completa en Markdown.
void RefrescarConversacion(bool irAlFinal)
{
    vistaConversacion.Text = RenderizarMarkdown(mensajesPantalla);

    if (irAlFinal)
    {
        vistaConversacion.ScrollToAnchor("fin");
    }
}

// Convierte la lista de mensajes visibles en un unico texto Markdown.
static string RenderizarMarkdown(IReadOnlyList<MensajePantalla> mensajes)
{
    if (mensajes.Count == 0)
    {
        return "# Asistente IA\n\nEscribi un mensaje y presiona **Enter** o el boton **Enviar**.\n\n<a id=\"fin\"></a>";
    }

    var texto = new StringBuilder();

    foreach (var mensaje in mensajes)
    {
        texto.Append("## ")
            .AppendLine(mensaje.Rol)
            .AppendLine()
            .AppendLine(mensaje.Texto)
            .AppendLine();
    }

    texto.AppendLine("<a id=\"fin\"></a>");
    return texto.ToString();
}

// Revisa que las variables de entorno necesarias existan antes de usar la IA.
static bool ValidarConfiguracion(string proveedor, string? url, string? apiKey, out string mensaje)
{
    var errores = new List<string>();

    if (string.IsNullOrWhiteSpace(url))
    {
        errores.Add($"Falta `{proveedor}_API_URL`.");
    }

    if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("<tu_clave_api_aqui>", StringComparison.OrdinalIgnoreCase))
    {
        errores.Add($"Falta `{proveedor}_API_KEY`.");
    }

    if (errores.Count == 0)
    {
        mensaje = "";
        return true;
    }

    mensaje = "# Configuracion incompleta\n\n"
        + string.Join("\n", errores.Select(error => $"- {error}"))
        + "\n\nCopia `.env.ejemplo` como `.env`, completa los datos de Gemini y ejecuta:\n\n"
        + "```bash\n"
        + "dotnet run .\\asistente.cs\n"
        + "```\n\n"
        + "Tambien podes elegir otro proveedor pasando su nombre, por ejemplo `openai`.\n";

    return false;
}

// Crea un IChatClient usando un proveedor compatible con la API de OpenAI.
static IChatClient CrearClienteChat(string url, string apiKey, string modelo)
{
    return new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(NormalizarUrlBase(url)) })
        .GetChatClient(modelo)
        .AsIChatClient();
}

// Algunos .env traen la URL completa de chat/completions; el SDK necesita la URL base.
static string NormalizarUrlBase(string url)
{
    var limpia = url.Trim().TrimEnd('/');
    const string sufijoChat = "/chat/completions";

    return limpia.EndsWith(sufijoChat, StringComparison.OrdinalIgnoreCase)
        ? limpia[..^sufijoChat.Length]
        : limpia;
}

// Registra las funciones que el modelo puede pedir ejecutar sobre archivos del proyecto.
static List<AITool> CrearHerramientas()
{
    return
    [
        AIFunctionFactory.Create(
            (Func<string, string>)LeerArchivo,
            new AIFunctionFactoryOptions
            {
                Name = "leer-archivo",
                Description = "Lee y devuelve el contenido de un archivo de texto del proyecto."
            }),
        AIFunctionFactory.Create(
            (Func<string, string, string>)EscribirArchivo,
            new AIFunctionFactoryOptions
            {
                Name = "escribir-archivo",
                Description = "Crea o sobrescribe un archivo de texto dentro del proyecto."
            }),
        AIFunctionFactory.Create(
            (Func<string, string>)ListarArchivos,
            new AIFunctionFactoryOptions
            {
                Name = "listar-archivos",
                Description = "Lista archivos y carpetas de un directorio del proyecto."
            })
    ];
}

[Description("Devuelve el contenido de un archivo de texto del proyecto.")]
// Herramienta para leer un archivo cuando el usuario se lo pide al asistente.
static string LeerArchivo([Description("Ruta relativa o absoluta del archivo dentro del proyecto.")] string ruta)
{
    var archivo = ResolverRutaDelProyecto(ruta);

    if (!File.Exists(archivo))
    {
        return $"No existe el archivo: {ruta}";
    }

    return File.ReadAllText(archivo);
}

[Description("Crea o sobrescribe un archivo de texto dentro del proyecto.")]
// Herramienta para crear o reemplazar un archivo con contenido generado o indicado.
static string EscribirArchivo(
    [Description("Ruta relativa o absoluta del archivo dentro del proyecto.")] string ruta,
    [Description("Contenido que se escribira en el archivo.")] string contenido)
{
    var archivo = ResolverRutaDelProyecto(ruta);
    var carpeta = Path.GetDirectoryName(archivo);

    if (!string.IsNullOrWhiteSpace(carpeta))
    {
        Directory.CreateDirectory(carpeta);
    }

    var existia = File.Exists(archivo);
    File.WriteAllText(archivo, contenido);

    return existia
        ? $"Archivo sobrescrito correctamente: {ruta}"
        : $"Archivo creado correctamente: {ruta}";
}

[Description("Lista archivos y carpetas de un directorio del proyecto.")]


