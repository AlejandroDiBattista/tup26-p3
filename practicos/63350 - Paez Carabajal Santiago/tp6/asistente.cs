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

// 1. CARGA DE VARIABLES MANUAL (.env.ejemplo)
// Leo línea por línea el archivo de configuración para sacar las credenciales de Groq
// sin tener que dejar la API key expuesta directamente metida en el código fuente.
DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();

var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL");

// Limpio el final de la URL si viene con la ruta de completado para que no falle el cliente de OpenAI
if (url.EndsWith("/chat/completions")) {
    url = url.Replace("/chat/completions", "");
}

// 2. INICIALIZAR CLIENTE CHAT LISO
// Configuro el cliente de chat usando la interfaz genérica IChatClient. Esto me sirve
// por si el día de mañana quiero cambiar de proveedor de IA sin romper la estructura de la app.
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

// Registro las funciones locales que armé abajo para mapearlas como herramientas (Tools)
// que queden disponibles para que el modelo las llame cuando el usuario se lo pida.
var opcionesChat = new ChatOptions
{
    Tools = new List<AITool>
    {
        AIFunctionFactory.Create(HerramientasArchivos.LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto."),
        AIFunctionFactory.Create(HerramientasArchivos.GuardarArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado."),
        AIFunctionFactory.Create(HerramientasArchivos.ListarArchivos, "listar-archivos", "Lista los archivos y carpetas de un directorio.")
    }
};

// 3. HISTORIAL DE MENSAJES
// Inicializo la lista de mensajes cargando las instrucciones de comportamiento desde AGENTS.md.
List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

// Creo este objeto para usarlo como cerrojo en los bloques lock. Lo necesito para que el hilo
// de la interfaz y el hilo de la IA no toquen la lista de mensajes al mismo tiempo y tire error.
object cerrojo = new object();

// 4. INICIALIZAR INTERFAZ GRÁFICA (TUI)
// Configuro la ventana principal a pantalla completa y defino las dimensiones de los controles.
using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var visorMarkdown = new Markdown {
    X = 0, Y = 0,
    Width = Dim.Fill(), 
    Height = Dim.Fill() - 3, // Le resto 3 espacio para que entre la caja de texto abajo
    Text = "# ¡Bienvenido!\nEscribí tu consulta abajo y presioná Enviar.",
    CanFocus = false
};

var cajaTexto = new TextField {
    X = 1, 
    Y = Pos.AnchorEnd(2), // Posicionamiento dinámico para mantenerlo fijo en el borde inferior
    Width = Dim.Fill() - 15,
    Text = ""
};
cajaTexto.CanFocus = true;

var botonEnviar = new Button {
    X = Pos.Right(cajaTexto) + 1, 
    Y = Pos.AnchorEnd(2),
    Text = "Enviar"
};

ventana.Add(visorMarkdown, cajaTexto, botonEnviar);

// Hago que el cursor se posicione directamente en el campo de texto apenas arranca la app
ventana.Initialized += (s, e) => {
    cajaTexto.SetFocus();
};

// 5. LÓGICA DE ENVÍO CON DESACOPLE DE HILOS Y MANEJO MANUAL DE HERRAMIENTAS
async Task EnviarMensaje()
{
    string textoUsuario = cajaTexto.Text.ToString().Trim();
    if (string.IsNullOrEmpty(textoUsuario)) return;

    // Deshabilito los controles apenas se manda el texto para que el usuario no tire dos clicks seguidos
    cajaTexto.Enabled = false;
    botonEnviar.Enabled = false;

    string historialPrevio = "";
    List<ChatMessage> snapshotMensajes;

    // Protejo el acceso a la lista compartida con un lock y creo un clon (ToList)
    lock (cerrojo)
    {
        mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
        
        // Filtro el historial para la pantalla omitiendo el System prompt, así no se muestra
        // todo el choclo de texto de AGENTS.md en el visor del usuario.
        historialPrevio = string.Join("\n\n", mensajes
            .Where(m => m.Role != ChatRole.System)
            .Select(m => m.Role == ChatRole.User ? $"# Vos\n\n{m.Text}" : $"# Asistente\n\n{m.Text}"));
            
        snapshotMensajes = mensajes.ToList();
    }

    cajaTexto.Text = "";
    visorMarkdown.Text = $"{historialPrevio}\n\n# Asistente\n\n... (Pensando)";
    app.LayoutAndDraw();

    // Uso Task.Run para sacar la petición de red del hilo principal de la interfaz.
    // De esta manera evito que la pantalla de la consola se congele mientras la IA genera el texto.
    await Task.Run(async () =>
    {
        try
        {
            string respuestaAcumulada = "";
            var respuestaStream = chat.GetStreamingResponseAsync(snapshotMensajes, opcionesChat);
            
            await foreach (var fragmento in respuestaStream)
            {
                if (!string.IsNullOrEmpty(fragmento.Text))
                {
                    respuestaAcumulada += fragmento.Text;
                    string tempRespuesta = respuestaAcumulada; 
                    
                    // Invoco la actualización del visor dentro del hilo de la interfaz gráfica
                    // para asegurarme de que Terminal.Gui dibuje los fragmentos en tiempo real y sin colgarse.
                    app.Invoke(() => {
                        visorMarkdown.Text = $"{historialPrevio}\n\n# Asistente\n\n{tempRespuesta}";
                        app.LayoutAndDraw();
                    });
                }
            }
            
            // Cuando termina el stream, guardo la respuesta completa en el historial original usando lock
            lock (cerrojo)
            {
                mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaAcumulada));
            }
        }
        catch (Exception ex)
        {
            app.Invoke(() => {
                visorMarkdown.Text += $"\n\n**Error al conectar con la IA:** {ex.Message}";
                app.LayoutAndDraw();
            });
        }
        finally
        {
            // Vuelvo a habilitar los controles de entrada y le regreso el foco a la caja de texto
            app.Invoke(() => {
                cajaTexto.Enabled = true;
                botonEnviar.Enabled = true;
                cajaTexto.SetFocus();
                app.LayoutAndDraw();
            });
        }
    });
}

// 6. ASIGNACIÓN DE EVENTOS
// Vinculo el método de envío tanto al botón como al presionar Enter en el TextField
botonEnviar.Accepting += async (s, e) => { await EnviarMensaje(); };
cajaTexto.Accepting += async (s, e) => { await EnviarMensaje(); };

// Capturo la tecla Escape para cerrar la aplicación de manera limpia controlando el bucle de la app
ventana.KeyDown += (s, e) => {
    if (e.ToString().Contains("Esc") || e.ToString().Contains("Escape"))
    {
        ventana.RequestStop();
    }
};

app.Run(ventana);

// CLASE DE HERRAMIENTAS DE ARCHIVOS NATIVAS
// Armé esta clase estática con métodos simples del sistema de archivos para resolver lo que me pide
// el enunciado sobre lectura, escritura y listado de directorios.
public static class HerramientasArchivos
{
    public static string LeerArchivo(string ruta)
    {
        if (!File.Exists(ruta)) return $"Error: El archivo '{ruta}' no existe.";
        return File.ReadAllText(ruta);
    }

    public static string ListarArchivos(string ruta)
    {
        string directorio = string.IsNullOrEmpty(ruta) ? Directory.GetCurrentDirectory() : ruta;
        if (!Directory.Exists(directorio)) return $"Error: El directorio '{directorio}' no existe.";
        
        var elementos = Directory.GetFileSystemEntries(directorio);
        return string.Join("\n", elementos.Select(Path.GetFileName));
    }

    public static string GuardarArchivo(string ruta, string contenido)
    {
        try
        {
            File.WriteAllText(ruta, contenido);
            return $"Éxito: Archivo '{ruta}' guardado correctamente.";
        }
        catch (Exception ex)
        {
            return $"Error al escribir el archivo: {ex.Message}";
        }
    }
}