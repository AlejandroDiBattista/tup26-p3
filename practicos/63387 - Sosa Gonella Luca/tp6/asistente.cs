#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

// =====================================================
// BLOQUE 1: LIBRERÍAS
// =====================================================

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// =====================================================
// BLOQUE 2: CARGAR VARIABLES DEL .env
// =====================================================

DotNetEnv.Env.Load();

// =====================================================
// BLOQUE 3: OBTENER URL, API KEY Y MODELO
// =====================================================

var proveedor = (args.Length > 0 ? args[0] : "openai")
    .ToUpperInvariant();

var url = Environment.GetEnvironmentVariable(
    $"{proveedor}_API_URL");

var apiKey = Environment.GetEnvironmentVariable(
    $"{proveedor}_API_KEY");

var modelo = Environment.GetEnvironmentVariable(
    $"{proveedor}_MODEL") ?? "gpt-5.4-mini";

// =====================================================
// BLOQUE 4: CREAR EL CLIENTE DE CHAT
// =====================================================

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(
            apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions
        {
            Endpoint = new Uri(url!)
        })
    .GetChatClient(modelo)
    .AsIChatClient();

// =====================================================
// BLOQUE 5: HISTORIAL DE MENSAJES
// Se carga el mensaje del sistema desde AGENTS.md
// =====================================================

List<ChatMessage> mensajes =
[
    new ChatMessage(
        ChatRole.System,
        File.ReadAllText("AGENTS.md"))
];

// =====================================================
// BLOQUE 6: HERRAMIENTAS DEL ASISTENTE
// =====================================================

var herramientas = new[]
{
    // Herramienta para leer archivos

    AIFunctionFactory.Create(
        (string ruta) =>
        {
            if (File.Exists(ruta))
                return File.ReadAllText(ruta);

            return "Archivo no encontrado.";
        },
        "leer-archivo"),

    // Herramienta para listar archivos

    AIFunctionFactory.Create(
        (string ruta) =>
        {
            if (Directory.Exists(ruta))
                return string.Join(
                    "\n",
                    Directory.GetFileSystemEntries(ruta));

            return "Directorio no encontrado.";
        },
        "listar-archivos"),

    // Herramienta para escribir archivos

    AIFunctionFactory.Create(
        (string ruta, string contenido) =>
        {
            File.WriteAllText(ruta, contenido);

            return "Archivo guardado correctamente.";
        },
        "escribir-archivo")
};

// =====================================================
// BLOQUE 7: INICIALIZAR TERMINAL.GUI
// =====================================================

using IApplication app =
    Application.Create().Init();

// =====================================================
// BLOQUE 8: VENTANA PRINCIPAL
// =====================================================

using var ventana = new Window
{
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

// =====================================================
// BLOQUE 9: PANEL DE CONVERSACIÓN
// =====================================================

var conversacion = new Markdown
{
    X = 0,
    Y = 0,

    Width = Dim.Fill(),

    Height = Dim.Fill(3),

    Text =
        "# Asistente IA\n\n" +
        "Escribí un mensaje para comenzar."
};

// =====================================================
// BLOQUE 10: CAMPO DE TEXTO
// =====================================================

var entrada = new TextField
{
    X = 0,

    Y = Pos.Bottom(conversacion),

    Width = Dim.Fill(12),

    Height = 1
};

// =====================================================
// BLOQUE 11: BOTÓN ENVIAR
// =====================================================

var botonEnviar = new Button
{
    X = Pos.Right(entrada) + 1,

    Y = Pos.Bottom(conversacion),

    Text = "Enviar"
};

// =====================================================
// BLOQUE 12: ENVÍO DE MENSAJES
// =====================================================

botonEnviar.Accepting += async (s, e) =>
{
    var texto = entrada.Text.ToString();

    if (string.IsNullOrWhiteSpace(texto))
        return;

    // Agregar mensaje del usuario al historial

    mensajes.Add(
        new ChatMessage(
            ChatRole.User,
            texto));

    // Mostrar mensaje del usuario

    conversacion.Text +=
        $"\n\n# Vos\n\n{texto}";

    // Limpiar entrada

    entrada.Text = "";

    // Deshabilitar controles

    entrada.Enabled = false;
    botonEnviar.Enabled = false;

    conversacion.Text +=
        "\n\n# Asistente\n\n";

    // Respuesta en streaming

    string respuestaCompleta = "";

    await foreach (var fragmento in
        chat.GetStreamingResponseAsync(
            mensajes,
            new ChatOptions
            {
                Tools = herramientas
            }))
    {
        respuestaCompleta += fragmento.Text;

        conversacion.Text += fragmento.Text;
    }

    // Guardar respuesta en el historial

    mensajes.Add(
        new ChatMessage(
            ChatRole.Assistant,
            respuestaCompleta));

    // Habilitar controles nuevamente

    entrada.Enabled = true;
    botonEnviar.Enabled = true;
};

// =====================================================
// BLOQUE 13: AGREGAR CONTROLES
// =====================================================

ventana.Add(conversacion);
ventana.Add(entrada);
ventana.Add(botonEnviar);

// =====================================================
// BLOQUE 14: EJECUTAR LA APLICACIÓN
// =====================================================

app.Run(ventana);