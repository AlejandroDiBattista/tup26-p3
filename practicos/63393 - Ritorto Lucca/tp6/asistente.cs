#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";


Console.WriteLine($"PROVEEDOR = {proveedor}");
Console.WriteLine($"URL = {url}");
Console.WriteLine($"MODELO = {modelo}");
var clienteBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url!) })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient chat = new ChatClientBuilder(clienteBase)
    .UseFunctionInvocation()
    .Build();
    //metodo leer archivo 
string LeerArchivo(string ruta)
{
    if (!File.Exists(ruta))
        return "El archivo no existe.";

    return File.ReadAllText(ruta);
}
//creamos la herramienta leer archivo
var leerArchivoTool = AIFunctionFactory.Create(
    LeerArchivo,
    "leer_archivo",
    "Lee el contenido de un archivo de texto."
);
//metodo escribir archivo
string EscribirArchivo(string ruta, string contenido)
{
    var rutaAbsoluta = Path.GetFullPath(ruta, Directory.GetCurrentDirectory());
    var directorio = Path.GetDirectoryName(rutaAbsoluta);

    if (!string.IsNullOrWhiteSpace(directorio))
        Directory.CreateDirectory(directorio);

    bool existe = File.Exists(rutaAbsoluta);
    File.WriteAllText(rutaAbsoluta, contenido ?? string.Empty);

    if (existe)
        return $"El archivo '{rutaAbsoluta}' ya existía y fue sobrescrito correctamente.";

    return $"Archivo '{rutaAbsoluta}' creado correctamente.";
}
//creamos la herramienta escribir archivo 
var escribirArchivoTool = AIFunctionFactory.Create(
    EscribirArchivo,
    "escribir_archivo",
    "Crea o sobrescribe un archivo de texto. Usa esta herramienta cuando el usuario pida crear o modificar un archivo."
);
//creamos el metodo listar archivo 
string ListarArchivos(string ruta)
{
    if (!Directory.Exists(ruta))
        return "El directorio no existe.";

    return string.Join("\n", Directory.GetFileSystemEntries(ruta));
}
//creamos la herramienta listar archivo
var listarArchivosTool = AIFunctionFactory.Create(
    ListarArchivos,
    "listar-archivos",
    "Lista los archivos y carpetas de un directorio."
);
ChatOptions opciones = new()
{
    Tools = [leerArchivoTool,
    escribirArchivoTool,
    listarArchivosTool
    
    ]
};
var instruccionesSistema = """
Eres un asistente que puede leer, escribir y listar archivos.
Si el usuario pide crear o modificar un archivo, usa la herramienta escribir_archivo.
Si el usuario pide leer un archivo, usa la herramienta leer_archivo.
Si el usuario pide ver los archivos de un directorio, usa la herramienta listar_archivos.
""";

List<ChatMessage> mensajes = [
    new(ChatRole.System, instruccionesSistema + "\n\n" + File.ReadAllText("AGENTS.md"))
];
using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};
var conversacion = new Markdown
{
    Text = "# Asistente IA\n\nEscribí un mensaje para comenzar.",
    Width = Dim.Fill(),
 Height = Dim.Fill(2)
};

ventana.Add(conversacion);

var entrada = new TextField
{
    X = 0,
    Y = Pos.Bottom(conversacion),
    Width = Dim.Fill(12),
    Height = 1
};


ventana.Add(entrada);
var botonEnviar = new Button
{
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.Bottom(conversacion)
};

ventana.Add(botonEnviar);

async Task EnviarMensaje()
{
    var texto = entrada.Text.ToString();

    if (string.IsNullOrWhiteSpace(texto))
        return;

    mensajes.Add(new ChatMessage(ChatRole.User, texto));

    entrada.Text = "";
    entrada.SetFocus();
    entrada.Enabled = false;
    botonEnviar.Enabled = false;

    conversacion.Text += $"\n\n# Vos\n\n{texto}";
    conversacion.Text += "\n\n# Asistente\n\n";

    string textoRespuesta;
    string respuestaCompleta;

    try
    {
        respuestaCompleta = "";

await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes,opciones))
{
    respuestaCompleta += fragmento.Text;
}

textoRespuesta = respuestaCompleta;
    }
   catch (Exception ex)
{
    textoRespuesta = $"Error: {ex.Message}";
}
    if (textoRespuesta.Contains("<think>") && textoRespuesta.Contains("</think>"))
    {
        var inicio = textoRespuesta.IndexOf("<think>");
        var fin = textoRespuesta.IndexOf("</think>") + "</think>".Length;

        textoRespuesta = textoRespuesta.Remove(inicio, fin - inicio).Trim();
    }

    mensajes.Add(new ChatMessage(ChatRole.Assistant, textoRespuesta));

    conversacion.Text += textoRespuesta;
    entrada.Enabled = true;
botonEnviar.Enabled = true;
entrada.SetFocus();
}
entrada.Accepted += async (s, e) =>
{
    await EnviarMensaje();
};
botonEnviar.Accepting += async (s, e) =>
{
    await EnviarMensaje();
};

//boton esc
ventana.KeyDown += (s, e) =>
{
    if (e == Key.Esc)
    {
        app.RequestStop();
    }
};

app.Run(ventana);
