#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var chat = new Agente(proveedor);
chat.Registrar(ChatRole.System, File.ReadAllText("AGENTS.md"));

using IApplication app = Application.Create().Init();


using var ventana = new Window 
{
    Title = $" Asistente IA · {chat.Modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var markdown = new Markdown
{
    Text = "# Vos\n\nEscribí un mensaje para empezar.",
    Width = Dim.Fill(),
    Height = Dim.Fill(5)
};

var entrada = new TextField
{
    X = 1,
    Y = Pos.Bottom(markdown) + 1,
    Width = Dim.Fill(12)
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.Bottom(markdown) + 1
};

string conversacion = markdown.Text;
bool ocupada = false;


async Task EnviarAsync()
{
    if (ocupada)
        return;

    var texto = entrada.Text?.ToString()?.Trim() ?? "";
    if (texto == "")
        return;

    ocupada = true;
    entrada.Enabled = false;
    enviar.Enabled = false;
    entrada.Text = "";

    chat.Registrar(ChatRole.User, texto);
    conversacion += $"\n\n# Vos\n\n{texto}\n\n# Asistente\n\n";
    markdown.Text = conversacion;

  /*  var respuesta = await chat.ResponderAsync(delta =>
    {
        conversacion += delta;
        markdown.Text = conversacion;
    });
*/

        var respuesta = await chat.ResponderAsync();
        conversacion += respuesta;
        markdown.Text = conversacion;


    chat.Registrar(ChatRole.Assistant, respuesta);
    conversacion += "\n";
    markdown.Text = conversacion;

    ocupada = false;
    entrada.Enabled = true;
    enviar.Enabled = true;
    entrada.SetFocus();
}

var respuesta = await chat.ResponderAsync();
conversacion += respuesta;
markdown.Text = conversacion;
chat.Registrar(ChatRole.Assistant, respuesta);


entrada.KeyDown += async (_, key) =>
{
    if (key == Key.Enter)
        await EnviarAsync();
};

enviar.Accepting += async (_, e) =>
{
    e.Handled = true;
    await EnviarAsync();
};

ventana.Add(markdown, entrada, enviar);
app.Run(ventana);

public sealed class Agente {
    readonly IChatClient cliente;
    readonly ChatOptions opciones;
    readonly List<ChatMessage> historia = [];
    public string Modelo { get; }

    static readonly string Workspace = Path.GetFullPath(".");

    public Agente(string proveedor)
    {
        var apiUrl = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
        var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
        Modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

        opciones = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(ListarArchivos, new()
                {
                    Name = "listar-archivos",
                    Description = "Lista los archivos y carpetas de un directorio."
                }),
                AIFunctionFactory.Create(LeerArchivo, new()
                {
                    Name = "leer-archivo",
                    Description = "Devuelve el contenido de un archivo de texto."
                }),
                AIFunctionFactory.Create(EscribirArchivo, new()
                {
                    Name = "escribir-archivo",
                    Description = "Crea o sobrescribe un archivo con el contenido indicado."
                })
            ]
        };

        cliente = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(apiUrl) })
            .GetChatClient(Modelo)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
    

public void Registrar(ChatRole rol, string texto)
    {
        historia.Add(new ChatMessage(rol, texto));
    }
/*    public async Task<string> ResponderAsync(Action<string> onDelta)
    {
        var texto = new StringBuilder();

        await foreach (var update in cliente.GetStreamingResponseAsync(historia, opciones))
        {
            var delta = update.Text ?? "";
            if (delta == "")
                continue;

            texto.Append(delta);
            onDelta(delta);
        }

        return texto.ToString();
    }
    
*/
public async Task<string> ResponderAsync()
{
    var respuesta = await cliente.GetResponseAsync(historia, opciones);
    return respuesta.Text ?? "";
}
static string ListarArchivos(
        [Description("Ruta relativa del directorio. Usá '.' para la raíz.")]
        string ruta = ".")
    {
        var dir = ResolverRuta(ruta);

        if (!Directory.Exists(dir))
            return $"No se encontró el directorio: {ruta}";

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFileSystemEntries(dir).Select(Path.GetFileName));
    }

    static string LeerArchivo(
        [Description("Ruta relativa del archivo a leer.")]
        string ruta)
    {
        var file = ResolverRuta(ruta);

        return File.Exists(file)
            ? File.ReadAllText(file)
            : $"No se encontró el archivo: {ruta}";
    }

    static string EscribirArchivo(
        [Description("Ruta relativa del archivo a crear o sobrescribir.")]
        string ruta,
        [Description("Contenido completo a guardar.")]
        string contenido)
    {
        var file = ResolverRuta(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, contenido);
        return $"Archivo guardado: {ruta}";
    }

    static string ResolverRuta(string ruta)
    {
        var workspace = Workspace;
        var rutaCompleta = Path.GetFullPath(Path.Combine(workspace, ruta));
        var prefijo = workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (rutaCompleta != workspace && !rutaCompleta.StartsWith(prefijo, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("No se puede acceder a rutas fuera del espacio de trabajo.");

        return rutaCompleta;
    }
}
