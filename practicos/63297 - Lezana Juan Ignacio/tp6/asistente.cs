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

        visorConversacion.KeyDown += (_, _) => { scrollManual = true; };

        MostrarMensajeBienvenida();
        entradaTexto.SetFocus();
    }

    void MostrarMensajeBienvenida() {
        contenidoChat.Clear();
        contenidoChat.AppendLine("# Asistente IA");
        contenidoChat.AppendLine();
        contenidoChat.AppendLine("Escribí tu mensaje abajo. Podés pedirme que lea, escriba o liste archivos del directorio actual.");
        contenidoChat.AppendLine();
        SincronizarVisor();
    }

    async Task ProcesarEnvio() {
        if (estaRespondiendo) return;

        string mensajeUsuario = entradaTexto.Text?.ToString()?.Trim() ?? "";
        if (string.IsNullOrEmpty(mensajeUsuario)) return;

        entradaTexto.Text    = "";
        entradaTexto.Enabled = false;
        btnEnviar.Enabled    = false;
        estaRespondiendo     = true;
        scrollManual         = false;
        estadoActual.Text    = "El asistente está respondiendo…";

        mensajes.Add(new(ChatRole.User, mensajeUsuario));
        contenidoChat.AppendLine("## 👤 Vos");
        contenidoChat.AppendLine();
        contenidoChat.AppendLine(mensajeUsuario);
        contenidoChat.AppendLine();
        contenidoChat.AppendLine("## ✦ Asistente");
        contenidoChat.AppendLine();
        SincronizarVisor();
        IrAlFinal();

        StringBuilder respuestaAcumulada = new();
        try {
            await foreach (var fragmento in clienteChat.GetStreamingResponseAsync(mensajes, config)) {
                string trozo = fragmento.Text ?? "";
                if (string.IsNullOrEmpty(trozo)) continue;

                respuestaAcumulada.Append(trozo);

                string textoVisible = System.Text.RegularExpressions.Regex.Replace(
                    respuestaAcumulada.ToString(), @"<think>.*?</think>", "",
                    System.Text.RegularExpressions.RegexOptions.Singleline).Trim();

                App!.Invoke(() => {
                    MostrarRespuestaParcial(textoVisible);
                    if (!scrollManual) IrAlFinal();
                });
            }

            mensajes.Add(new(ChatRole.Assistant, respuestaAcumulada.ToString()));
        } catch (Exception ex) {
            string msgError = $"*Error: {ex.Message}*";
            respuestaAcumulada.Append(msgError);
            mensajes.Add(new(ChatRole.Assistant, msgError));
            App!.Invoke(() => {
                estadoActual.Text = $"✗ Error: {ex.Message}";
                MostrarRespuestaParcial(msgError);
            });
        }

        string respuestaFinal = System.Text.RegularExpressions.Regex.Replace(
            respuestaAcumulada.ToString(), @"<think>.*?</think>", "",
            System.Text.RegularExpressions.RegexOptions.Singleline).Trim();

        contenidoChat.AppendLine(respuestaFinal);
        contenidoChat.AppendLine();
        contenidoChat.AppendLine("---");
        contenidoChat.AppendLine();

        App!.Invoke(() => {
            SincronizarVisor();
            if (!scrollManual) IrAlFinal();
            entradaTexto.Enabled = true;
            btnEnviar.Enabled    = true;
            estaRespondiendo     = false;
            estadoActual.Text    = "Escribí tu mensaje y presioná Enter o hacé clic en Enviar.";
            entradaTexto.SetFocus();
        });
    }
    void MostrarRespuestaParcial(string textoParcial) {
        visorConversacion.Text = contenidoChat.ToString() + textoParcial;
        visorConversacion.SetNeedsDraw();
    }

    void SincronizarVisor() {
        visorConversacion.Text = contenidoChat.ToString();
        visorConversacion.SetNeedsDraw();
    }

    void IrAlFinal() {
        visorConversacion.ScrollVertical(visorConversacion.LineCount);
    }
}

// TODO: agregar el panel de conversación y el panel de entrada.
// TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

app.Run(ventana);
