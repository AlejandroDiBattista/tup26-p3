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

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "no-requiere-key";
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient clienteBase = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();
 
IChatClient chat = new ChatClientBuilder(clienteBase)
    .UseFunctionInvocation()
    .Build();

string sistemaPrompt = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "Sos un asistente de programación. Respondé en español.";
 
List<ChatMessage> historial = [
    new(ChatRole.System, sistemaPrompt)
];
 
ChatOptions opcionesChat = new() {
    Tools = [
        AIFunctionFactory.Create(Herramientas.ListarArchivos, new() {
            Name        = "listar-archivos",
            Description = "Lista los archivos y carpetas de un directorio."
        }),
        AIFunctionFactory.Create(Herramientas.LeerArchivo, new() {
            Name        = "leer-archivo",
            Description = "Devuelve el contenido de un archivo de texto."
        }),
        AIFunctionFactory.Create(Herramientas.EscribirArchivo, new() {
            Name        = "escribir-archivo",
            Description = "Crea o sobrescribe un archivo con el contenido indicado."
        }),
    ]
};

Console.OutputEncoding = Encoding.UTF8;
 
using IApplication app = Application.Create().Init();
app.Run(new VentanaAsistente(chat, historial, opcionesChat, modelo));

class VentanaAsistente : Runnable {
    readonly IChatClient chat;
    readonly List<ChatMessage> historial;
    readonly ChatOptions opciones;
 
    readonly Markdown panelConversacion;
    readonly TextField campoEntrada;
    readonly Button btnEnviar;
    readonly Label lblEstado;
 
    readonly StringBuilder textoConversacion = new();
    bool respondiendo = false;
    bool usuarioScrolleo = false;
 
    public VentanaAsistente(IChatClient chat, List<ChatMessage> historial, ChatOptions opciones, string modelo) {
        this.chat      = chat;
        this.historial = historial;
        this.opciones  = opciones;
 
        Title  = $" Asistente IA · {modelo} ";
        Width  = Dim.Fill();
        Height = Dim.Fill();
 
        var frameConversacion = new FrameView {
            Title  = " Conversación ",
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(4),
        };
        frameConversacion.BorderStyle = LineStyle.Single;
 
        panelConversacion = new Markdown {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };
        frameConversacion.Add(panelConversacion);
 
        var frameEntrada = new FrameView {
            Title  = " Mensaje (Enter = enviar · Esc = salir) ",
            X = 0,
            Y = Pos.Bottom(frameConversacion),
            Width  = Dim.Fill(),
            Height = 4,
        };
        frameEntrada.BorderStyle = LineStyle.Single;
 
        campoEntrada = new TextField {
            X = 1, Y = 0,
            Width  = Dim.Fill(12),
            Height = 1,
            CanFocus = true,
        };
 
        btnEnviar = new Button {
            Text   = "Enviar",
            X      = Pos.Right(campoEntrada) + 1,
            Y      = 0,
            CanFocus = true,
        };

        lblEstado = new Label {
            X = 1, Y = 1,
            Width = Dim.Fill(2),
            Text  = "Escribí tu mensaje y presioná Enter o hacé clic en Enviar.",
        };
 
        frameEntrada.Add(campoEntrada, btnEnviar, lblEstado);
        Add(frameConversacion, frameEntrada);
 
        campoEntrada.KeyDown += (_, key) => {
            if (key == Key.Enter) {
                key.Handled = true;
                _ = EnviarMensaje();
            }
        };
 
        btnEnviar.Accepted += (_, _) => _ = EnviarMensaje();
 
        KeyDown += (_, key) => {
            if (key == Key.Esc) {
                key.Handled = true;
                App!.RequestStop();
            }
        };
        campoEntrada.KeyDown += (_, key) => {
            if (key == Key.Esc) {
                key.Handled = true;
                App!.RequestStop();
            }
        };
 
        panelConversacion.KeyDown += (_, key) => {
            usuarioScrolleo = true;
        };
 
        MostrarBienvenida();
        campoEntrada.SetFocus();
    }
 
    void MostrarBienvenida() {
        textoConversacion.Clear();
        textoConversacion.AppendLine("# Asistente IA");
        textoConversacion.AppendLine();
        textoConversacion.AppendLine("Escribí tu mensaje abajo. Podés pedirme que lea, escriba o liste archivos del directorio actual.");
        textoConversacion.AppendLine();
        ActualizarPanel();
    }
 
    async Task EnviarMensaje() {
        if (respondiendo) return;
 
        string texto = campoEntrada.Text?.ToString()?.Trim() ?? "";
        if (string.IsNullOrEmpty(texto)) return;
 
        campoEntrada.Text    = "";
        campoEntrada.Enabled = false;
        btnEnviar.Enabled    = false;
        respondiendo         = true;
        usuarioScrolleo      = false;
        lblEstado.Text       = "El asistente está respondiendo…";
 
        historial.Add(new(ChatRole.User, texto));
        textoConversacion.AppendLine("## 👤 Vos");
        textoConversacion.AppendLine();
        textoConversacion.AppendLine(texto);
        textoConversacion.AppendLine();
        textoConversacion.AppendLine("## 🤖 Asistente");
        textoConversacion.AppendLine();
        ActualizarPanel();
        ScrollAlFinal();

        StringBuilder respuestaCompleta = new();
        try {
            await foreach (var fragmento in chat.GetStreamingResponseAsync(historial, opciones)) {
                string chunk = fragmento.Text ?? "";
                if (string.IsNullOrEmpty(chunk)) continue;
 
                respuestaCompleta.Append(chunk);
 
                string visible = System.Text.RegularExpressions.Regex.Replace(
                    respuestaCompleta.ToString(), @"<think>.*?</think>", "", 
                    System.Text.RegularExpressions.RegexOptions.Singleline).Trim();
 
                App!.Invoke(() => {
                    ActualizarPanelConRespuestaParcial(visible);
                    if (!usuarioScrolleo) ScrollAlFinal();
                });
            }
 
            string textoRespuesta = respuestaCompleta.ToString();
            historial.Add(new(ChatRole.Assistant, textoRespuesta));
        } catch (Exception ex) {
            string error = $"*Error: {ex.Message}*";
            respuestaCompleta.Append(error);
            historial.Add(new(ChatRole.Assistant, error));
            App!.Invoke(() => {
                lblEstado.Text = $"✗ Error: {ex.Message}";
                ActualizarPanelConRespuestaParcial(error);
            });
        }

        string respuestaFinal = System.Text.RegularExpressions.Regex.Replace(
            respuestaCompleta.ToString(), @"<think>.*?</think>", "",
            System.Text.RegularExpressions.RegexOptions.Singleline).Trim();
        textoConversacion.AppendLine(respuestaFinal);
        textoConversacion.AppendLine();
        textoConversacion.AppendLine("---");
        textoConversacion.AppendLine();
 
        App!.Invoke(() => {
            ActualizarPanel();
            if (!usuarioScrolleo) ScrollAlFinal();
            campoEntrada.Enabled = true;
            btnEnviar.Enabled    = true;
            respondiendo         = false;
            lblEstado.Text       = "Escribí tu mensaje y presioná Enter o hacé clic en Enviar.";
            campoEntrada.SetFocus();
        });
    }
 
     void ActualizarPanelConRespuestaParcial(string parcial) {
        panelConversacion.Text = textoConversacion.ToString() + parcial;
        panelConversacion.SetNeedsDraw();
    }
 
    void ActualizarPanel() {
        panelConversacion.Text = textoConversacion.ToString();
        panelConversacion.SetNeedsDraw();
    }
 
    void ScrollAlFinal() {
        panelConversacion.ScrollVertical(panelConversacion.LineCount);
    }
}

static class Herramientas {
    static readonly string WorkDir = Path.GetFullPath(".");
 
    public static string ListarArchivos(
        [Description("Ruta del directorio a listar. Usá '.' para el directorio actual.")] string ruta = ".") {
        try {
            string rutaCompleta = ResolverRuta(ruta);
            if (!Directory.Exists(rutaCompleta))
                return $"No se encontró el directorio: {ruta}";
 
            var elementos = Directory.EnumerateFileSystemEntries(rutaCompleta)
                .Select(e => {
                    string nombre = Path.GetFileName(e);
                    return Directory.Exists(e) ? $"[DIR] {nombre}" : nombre;
                })
                .OrderBy(e => e);
 
            return string.Join("\n", elementos);
        } catch (Exception ex) {
            return $"Error al listar '{ruta}': {ex.Message}";
        }
    }
 
    public static string LeerArchivo(
        [Description("Ruta del archivo a leer.")] string ruta) {
        try {
            string rutaCompleta = ResolverRuta(ruta);
            if (!File.Exists(rutaCompleta))
                return $"No se encontró el archivo: {ruta}";
 
            return File.ReadAllText(rutaCompleta);
        } catch (Exception ex) {
            return $"Error al leer '{ruta}': {ex.Message}";
        }
    }
 
    public static string EscribirArchivo(
        [Description("Ruta del archivo a crear o sobrescribir.")] string ruta,
        [Description("Contenido completo que se guardará en el archivo.")] string contenido) {
        try {
            string rutaCompleta = ResolverRuta(ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);
            File.WriteAllText(rutaCompleta, contenido);
            return $"Archivo guardado correctamente: {ruta}";
        } catch (Exception ex) {
            return $"Error al escribir '{ruta}': {ex.Message}";
        }
    }
 
    static string ResolverRuta(string ruta) {
        string rutaCompleta = Path.GetFullPath(Path.Combine(WorkDir, ruta));
        string prefijo = WorkDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
 
        if (rutaCompleta != WorkDir && !rutaCompleta.StartsWith(prefijo, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("No se puede acceder a rutas fuera del directorio de trabajo.");
 
        return rutaCompleta;
    }
}
 