#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

DotNetEnv.Env.Load(".env");

var nombreProveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var endpoint = Environment.GetEnvironmentVariable($"{nombreProveedor}_API_URL") ?? "";
var clave    = Environment.GetEnvironmentVariable($"{nombreProveedor}_API_KEY") ?? "no-requiere-key";
var nombreModelo = Environment.GetEnvironmentVariable($"{nombreProveedor}_MODEL") ?? "gpt-4o-mini";

IChatClient clienteIA = new OpenAIClient(
        new ApiKeyCredential(clave),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(nombreModelo)
    .AsIChatClient();

IChatClient clienteConHerramientas = new ChatClientBuilder(clienteIA)
.UseFunctionInvocation()
.Build();

string promptInicial = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "Sos un asistente de programación. Respondé en español.";

List<ChatMessage> mensajes = [
    new(ChatRole.System, promptInicial)
];

ChatOptions configuracionChat = new() {
    Tools = [
        AIFunctionFactory.Create(OperacionesArchivo.ListarArchivos, new() {
            Name        = "listar-archivos",
            Description = "Lista los archivos y carpetas de un directorio."
        }),
        AIFunctionFactory.Create(OperacionesArchivo.LeerArchivo, new() {
            Name        = "leer-archivo",
            Description = "Devuelve el contenido de un archivo de texto."
        }),
        AIFunctionFactory.Create(OperacionesArchivo.EscribirArchivo, new() {
            Name        = "escribir-archivo",
            Description = "Crea o sobrescribe un archivo con el contenido indicado."
        }),
    ]
};
Console.OutputEncoding = Encoding.UTF8;

using IApplication instanciaApp = Application.Create().Init();
instanciaApp.Run(new PantallaChat(clienteConHerramientas, mensajes, configuracionChat, nombreModelo));
class PantallaChat : Runnable {
    readonly IChatClient clienteChat;
    readonly List<ChatMessage> mensajes;
    readonly ChatOptions config;

    readonly Markdown visorConversacion;
    readonly TextField entradaTexto;
    readonly Button btnEnviar;
    readonly Label estadoActual;

    readonly StringBuilder contenidoChat = new();
    bool estaRespondiendo = false;
    bool scrollManual = false;

    public PantallaChat(IChatClient clienteChat, List<ChatMessage> mensajes, ChatOptions config, string nombreModelo) {
        this.clienteChat = clienteChat;
        this.mensajes    = mensajes;
        this.config      = config;

        Title  = $" AsistenteIA · {nombreModelo} ";
        Width  = Dim.Fill();
        Height = Dim.Fill();

    
        var marcoChat = new FrameView {
            Title  = " Conversación ",
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(4),
        };
        marcoChat.BorderStyle = LineStyle.Single;

        visorConversacion = new Markdown {
            X = 0, Y = 0,
            Width    = Dim.Fill(),
            Height   = Dim.Fill(),
            CanFocus = true,
        };
        marcoChat.Add(visorConversacion);

        var marcoEntrada = new FrameView {
            Title  = " Mensaje (Enter = enviar · Esc = salir) ",
            X = 0,
            Y = Pos.Bottom(marcoChat),
            Width  = Dim.Fill(),
            Height = 4,
        };
        marcoEntrada.BorderStyle = LineStyle.Single;

        entradaTexto = new TextField {
            X = 1, Y = 0,
            Width    = Dim.Fill(12),
            Height   = 1,
            CanFocus = true,
        };

        btnEnviar = new Button {
            Text     = "Enviar",
            X        = Pos.Right(entradaTexto) + 1,
            Y        = 0,
            CanFocus = true,
        };

        estadoActual = new Label {
            X    = 1,
            Y    = 1,
            Width = Dim.Fill(2),
            Text  = "Escribí tu mensaje y presioná Enter o hacé clic en Enviar.",
        };

        marcoEntrada.Add(entradaTexto, btnEnviar, estadoActual);
        Add(marcoChat, marcoEntrada);

         entradaTexto.KeyDown += (_, tecla) => {
            if (tecla == Key.Enter) {
                tecla.Handled = true;
                _ = ProcesarEnvio();
            }
            if (tecla == Key.Esc) {
                tecla.Handled = true;
                App!.RequestStop();
            }
        };

        btnEnviar.Accepted += (_, _) => _ = ProcesarEnvio();

        KeyDown += (_, tecla) => {
            if (tecla == Key.Esc) {
                tecla.Handled = true;
                App!.RequestStop();
            }
        };

ventana.Add(new Markdown {
    Text = $"# Vos\n\n{pregunta}\n\n# Asistente\n\n{respuesta.Text}",
    Width = Dim.Fill(), Height = Dim.Fill()
});

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
