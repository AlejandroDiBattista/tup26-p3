#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.Text;

// Carga las variables de entorno desde el archivo .env.
DotNetEnv.Env.TraversePath().Load();

// Define el proveedor. De manera predeterminada, usa OpenAI.
var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();

// Crea y configura el agente de programación.
var chat = new Agente(proveedor);

chat.Registrar(ChatRole.System, $"""
    Sos un asistente util.
    Respondé en español claro, directo y técnico.
    Priorizá ejemplos en C# cuando el usuario no indique lenguaje.
    Si falta contexto, pedí solo el dato mínimo necesario.
    Usá las herramientas disponibles para listar, leer y escribir archivos.
    Cuando te pidan generar código, guardalo en un archivo dentro del espacio de trabajo.
""");

Console.OutputEncoding = Encoding.UTF8;
Console.Clear();
Console.WriteLine("Agente de programación");
Console.WriteLine("Escribí tu consulta. Presioná Enter sin escribir nada para salir.\n");

while (true) {
    Console.Write("👤 > ");
    var entrada = Console.ReadLine()?.Trim() ?? "";

    // Finaliza cuando la entrada está vacía.
    if (entrada.Length == 0) { break; }

    // Registra el mensaje del usuario.
    chat.Registrar(ChatRole.User, entrada);
    
    // Genera una respuesta a partir del historial de la conversación.
    var texto = await chat.Respuesta();
    
    // Registra el mensaje del asistente.
    chat.Registrar(ChatRole.Assistant, texto);

    // Muestra la respuesta.
    Console.WriteLine($"\n 🤖 > {texto}\n");
}

class Agente {
    private IChatClient cliente;
    private List<ChatMessage> historia = [];
    private ChatOptions herramientas;

    public Agente(string proveedor) {
        proveedor = proveedor.ToUpperInvariant();

        var apiUrl = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
        var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
        var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL")   ?? "";

        herramientas = new ChatOptions {
            Tools = [
                AIFunctionFactory.Create(Listar, new() {
                    Name        = "listar-archivos",
                    Description = "Lista los archivos y directorios de una ruta dentro del espacio de trabajo."
                }),
                AIFunctionFactory.Create(Leer, new() {
                    Name        = "leer-archivo",
                    Description = "Lee el contenido completo de un archivo dentro del espacio de trabajo."
                }),
                AIFunctionFactory.Create(Escribir, new() {
                    Name        = "escribir-archivo",
                    Description = "Crea o reemplaza un archivo de texto dentro del espacio de trabajo."
                })
            ]
        };
        
        cliente = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(apiUrl) })
            .GetChatClient(modelo)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }

    public void Registrar(ChatRole rol, string texto) {
        historia.Add(new(rol, texto));
    }

    public async Task<string> Respuesta() {
        var respuesta = await cliente.GetResponseAsync(historia, herramientas);
        return respuesta.Text ?? "";
    }

    static string Listar([Description("Ruta relativa del directorio que se va a listar. Usá un punto para indicar la raíz.")] string ruta = ".") {
        var rutaCompleta = ResolverRuta(ruta);
        if (!Directory.Exists(rutaCompleta)) return $"No se encontró el directorio: {ruta}";

        var elementos = Directory.EnumerateFileSystemEntries(rutaCompleta)
            .Select(Path.GetFileName);

        return string.Join(Environment.NewLine, elementos);
    }

    static string Leer([Description("Ruta relativa del archivo que se va a leer.")] string ruta) {
        var rutaCompleta = ResolverRuta(ruta);
        return File.Exists(rutaCompleta)
            ? File.ReadAllText(rutaCompleta)
            : $"No se encontró el archivo: {ruta}";
    }

    static string Escribir(
        [Description("Ruta relativa del archivo que se va a crear o reemplazar.")] string ruta,
        [Description("Contenido completo que se guardará en el archivo.")] string contenido) {
        
        var rutaCompleta = ResolverRuta(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);
        File.WriteAllText(rutaCompleta, contenido);

        return $"Archivo guardado: {ruta}";
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
}
