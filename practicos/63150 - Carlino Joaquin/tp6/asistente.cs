
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
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Arranque del programa

DotNetEnv.Env.Load();

// Elegimos el servicio por argumento (por defecto "openai").
// dotnet run agente.cs            -> usa OPENAI
// dotnet run agente.cs -- groq    -> usa GROQ
var servicio = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var direccion = Environment.GetEnvironmentVariable($"{servicio}_API_URL");
var clave     = Environment.GetEnvironmentVariable($"{servicio}_API_KEY");
var motor     = Environment.GetEnvironmentVariable($"{servicio}_MODEL") ?? "gpt-4o-mini";

// Si no hay URL, avisamos y cortamos.
if (string.IsNullOrWhiteSpace(direccion))
{
    Console.WriteLine($"  Falta la variable {servicio}_API_URL.");
    Console.WriteLine("   Copiá '.env.ejemplo' a '.env' y completá la URL y la clave del servicio.");
    return;
}

// Sacamos el sufijo para quedarnos con la URL base.
var raiz = direccion.EndsWith("/chat/completions") ? direccion[..^"/chat/completions".Length] : direccion;

// Armamos el cliente que habla con la IA.
IChatClient agente = new ChatClientBuilder(
        new OpenAIClient(
                new ApiKeyCredential(string.IsNullOrWhiteSpace(clave) ? "no-requiere-key" : clave),
                new OpenAIClientOptions { Endpoint = new Uri(raiz) })
            .GetChatClient(motor)
            .AsIChatClient())
    .UseFunctionInvocation()
    .Build();

// Leemos las instrucciones desde AGENTS.md.
var instrucciones = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "Sos un asistente de programación. Respondé en español, directo y técnico.";

// Registramos las herramientas que puede usar la IA.
var ajustes = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(Utiles.VerArchivo,      "ver-archivo"),
        AIFunctionFactory.Create(Utiles.GuardarArchivo,  "guardar-archivo"),
        AIFunctionFactory.Create(Utiles.MostrarArchivos, "mostrar-archivos"),
    ]
};

// Historial que se manda al modelo.
List<ChatMessage> historial = [new(ChatRole.System, instrucciones)];

// Lo que se ve en pantalla: cada par es un rol y su texto (puede ir creciendo).
List<(string Rol, StringBuilder Texto)> dialogos =
[
    ("Asistente", new StringBuilder(
        "Soy tu asistente de programación.\n\n" +
        "Escribí tu mensaje abajo y presioná **Enter** para enviar.\n" +
        "Puedo trabajar con los archivos de esta carpeta: probá con " +
        "*\"mostrame qué archivos hay acá\"* o *\"abrí README.md\"*.\n\n" +
        "Presioná **Esc** para salir."))
];

var ocupado = false; // bloquea envíos mientras la IA responde

// Interfaz de terminal

using IApplication app = Application.Create().Init();

using var marco = new Window
{
    Title = $" Asistente IA · {motor}  (Esc para salir) ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

// Panel del chat con scroll.
var panelChat = new FrameView
{
    Title = " Conversación ",
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(5)
};

var vistaChat = new Markdown
{
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    CanFocus = true        // scroll por teclado
};
panelChat.Add(vistaChat);

// Panel donde se escribe el mensaje.
var panelInput = new FrameView
{
    Title = " Mensaje ",
    X = 0,
    Y = Pos.Bottom(panelChat),
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var caja = new TextField
{
    X = 0, Y = 0,
    Width = Dim.Fill(12),
    Height = 1
};

var boton = new Button
{
    X = Pos.Right(caja) + 1,
    Y = 0,
    Text = "Enviar"
};

panelInput.Add(caja, boton);
marco.Add(panelChat, panelInput);

// Envío del mensaje

// Enter en la caja o click en el botón disparan el envío.
caja.Accepting  += (_, e) => { e.Handled = true; Mandar(); };
boton.Accepting += (_, e) => { e.Handled = true; Mandar(); };

Refrescar();
caja.SetFocus();
app.Run(marco);


// Toma el texto de la caja y dispara el envío.
void Mandar()
{
    if (ocupado) return;
    var texto = (caja.Text ?? "").ToString()!.Trim();
    if (texto.Length == 0) return;
    _ = MandarAsync(texto);
}

// Agrega el mensaje del usuario y pide la respuesta en streaming.
async Task MandarAsync(string texto)
{
    ocupado      = true;
    caja.Enabled = false;
    boton.Enabled = false;
    caja.Text    = "";

    // Turno del usuario.
    dialogos.Add(("Vos", new StringBuilder(texto)));
    historial.Add(new ChatMessage(ChatRole.User, texto));

    // Turno del asistente (se va llenando de a poco).
    var respuesta = new StringBuilder();
    dialogos.Add(("Asistente", respuesta));
    Refrescar();

    try
    {
        var fragmentos = new List<ChatResponseUpdate>();
        await foreach (var parte in agente.GetStreamingResponseAsync(historial, ajustes))
        {
            fragmentos.Add(parte);
            if (!string.IsNullOrEmpty(parte.Text))
            {
                respuesta.Append(parte.Text);
                Refrescar();
            }
        }
        // Guardamos lo respondido en el historial.
        historial.AddMessages(fragmentos);
    }
    catch (Exception ex)
    {
        respuesta.Append($"\n\n> Error: {ex.Message}");
        Refrescar();
    }
    finally
    {
        ocupado      = false;
        caja.Enabled = true;
        boton.Enabled = true;
        caja.SetFocus();
    }
}

// Arma el markdown con toda la conversación y lo muestra.
void Refrescar()
{
    var sb = new StringBuilder();
    foreach (var (rol, txt) in dialogos)
    {
        sb.Append("# ").Append(rol == "Vos" ? " Vos" : " Asistente").Append("\n\n");
        sb.Append(txt).Append("\n\n");
    }

    var texto = sb.ToString();
    vistaChat.Text = texto;
    vistaChat.SetNeedsDraw();

    // Baja el scroll hasta el final.
    try { vistaChat.ScrollVertical(texto.AsSpan().Count('\n') + 50); }
    catch { }
}

// Herramientas de archivos
static class Utiles
{
    [Description("Devuelve el contenido de un archivo de texto.")]
    public static string VerArchivo(
        [Description("Ruta del archivo a leer")] string ruta)
        => File.Exists(ruta)
            ? File.ReadAllText(ruta)
            : $"No se encontró el archivo: {ruta}";

    [Description("Crea o sobrescribe un archivo de texto con el contenido indicado.")]
    public static string GuardarArchivo(
        [Description("Ruta del archivo a escribir")] string ruta,
        [Description("Contenido que se guardará en el archivo")] string contenido)
    {
        File.WriteAllText(ruta, contenido);
        return $"Archivo guardado: {ruta} ({contenido.Length} caracteres).";
    }

    [Description("Lista los archivos y carpetas de un directorio.")]
    public static string MostrarArchivos(
        [Description("Ruta del directorio (vacío = carpeta actual)")] string ruta)
    {
        var carpeta = string.IsNullOrWhiteSpace(ruta) ? "." : ruta;
        if (!Directory.Exists(carpeta)) return $"No se encontró el directorio: {carpeta}";

        var items = Directory.EnumerateFileSystemEntries(carpeta)
            .Select(p => Directory.Exists(p) ? $"[carpeta] {Path.GetFileName(p)}"
                                             : $"          {Path.GetFileName(p)}")
            .OrderBy(x => x);

        var listado = string.Join("\n", items);
        return listado.Length == 0 ? $"(El directorio '{carpeta}' está vacío)" : listado;
    }
}
