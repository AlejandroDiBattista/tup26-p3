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
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string ChatCompletionsPath = "/chat/completions";
const long MaximoBytesLectura = 1024 * 1024;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

DotNetEnv.Env.TraversePath().Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL");
var raizProyecto = Directory.GetCurrentDirectory();

if (string.IsNullOrWhiteSpace(url)) {
    throw new InvalidOperationException($"Falta configurar {proveedor}_API_URL en .env.");
}

if (string.IsNullOrWhiteSpace(modelo)) {
    throw new InvalidOperationException($"Falta configurar {proveedor}_MODEL en .env.");
}

if (proveedor != "OLLAMA" && EsValorVacio(apiKey)) {
    throw new InvalidOperationException($"Falta configurar {proveedor}_API_KEY en .env.");
}

var promptSistema = CargarPromptSistema("AGENTS.md");
var opciones = CrearOpcionesDeChat();
var chat = CrearChatClient(proveedor, url, apiKey, modelo);

List<ChatMessage> mensajes = [
    new(ChatRole.System, promptSistema)
];

using IApplication app = Application.Create().Init();
using var ventana = new ChatWindow(chat, opciones, mensajes, modelo, proveedor);
app.Run(ventana);

static bool EsValorVacio(string? valor) {
    return string.IsNullOrWhiteSpace(valor) || valor.Trim().StartsWith("<");
}

static string CargarPromptSistema(string ruta) {
    if (!File.Exists(ruta)) {
        throw new FileNotFoundException($"No se encontro el archivo de sistema '{ruta}'.");
    }

    var contenido = File.ReadAllText(ruta, Encoding.UTF8).Trim();
    if (string.IsNullOrWhiteSpace(contenido)) {
        throw new InvalidOperationException($"El archivo '{ruta}' esta vacio.");
    }

    return contenido;
}

IChatClient CrearChatClient(string proveedor, string url, string? apiKey, string modelo) {
    var baseUrl = url.TrimEnd('/');

    if (baseUrl.EndsWith(ChatCompletionsPath, StringComparison.OrdinalIgnoreCase)) {
        baseUrl = baseUrl[..^ChatCompletionsPath.Length];
    }

    return new OpenAIClient(
            new ApiKeyCredential(apiKey ?? "no-requiere-key"),
            new OpenAIClientOptions { Endpoint = new Uri(baseUrl) })
        .GetChatClient(modelo)
        .AsIChatClient()
        .AsBuilder()
        .UseFunctionInvocation()
        .Build();
}

ChatOptions CrearOpcionesDeChat() {
    return new ChatOptions {
        Tools = [
            AIFunctionFactory.Create(ListarArchivos, new() {
                Name = "listar-archivos",
                Description = "Lista archivos y carpetas dentro del directorio del proyecto."
            }),
            AIFunctionFactory.Create(LeerArchivo, new() {
                Name = "leer-archivo",
                Description = "Lee el contenido completo de un archivo de texto del proyecto."
            }),
            AIFunctionFactory.Create(EscribirArchivo, new() {
                Name = "escribir-archivo",
                Description = "Crea o sobrescribe un archivo de texto dentro del proyecto."
            })
        ]
    };
}

string ListarArchivos(
    [Description("Ruta relativa del directorio. Usar punto para la raiz del proyecto.")] string ruta = ".") {
    try {
        var rutaCompleta = ResolverRutaSegura(ruta);

        if (!Directory.Exists(rutaCompleta)) {
            return $"No existe el directorio: {ruta}";
        }

        var elementos = Directory.EnumerateFileSystemEntries(rutaCompleta)
            .OrderBy(Directory.Exists)
            .ThenBy(Path.GetFileName)
            .Select(FormatearEntrada);

        return string.Join(Environment.NewLine, elementos);
    }
    catch (Exception ex) {
        return $"Error al listar archivos: {ex.Message}";
    }
}

string LeerArchivo(
    [Description("Ruta relativa del archivo que se quiere leer.")] string ruta) {
    try {
        var rutaCompleta = ResolverRutaSegura(ruta);

        if (!File.Exists(rutaCompleta)) {
            return $"No existe el archivo: {ruta}";
        }

        var info = new FileInfo(rutaCompleta);
        if (info.Length > MaximoBytesLectura) {
            return $"El archivo '{ruta}' es demasiado grande para leerlo completo.";
        }

        if (PareceArchivoBinario(rutaCompleta)) {
            return $"El archivo '{ruta}' no parece ser un archivo de texto.";
        }

        return File.ReadAllText(rutaCompleta, Encoding.UTF8);
    }
    catch (Exception ex) {
        return $"Error al leer archivo: {ex.Message}";
    }
}

string EscribirArchivo(
    [Description("Ruta relativa del archivo que se quiere escribir.")] string ruta,
    [Description("Contenido completo que se guardara en el archivo.")] string contenido) {
    try {
        var rutaCompleta = ResolverRutaSegura(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);
        File.WriteAllText(rutaCompleta, contenido, Encoding.UTF8);
        return $"Archivo escrito: {ruta}";
    }
    catch (Exception ex) {
        return $"Error al escribir archivo: {ex.Message}";
    }
}

string ResolverRutaSegura(string ruta) {
    var rutaCompleta = Path.GetFullPath(Path.Combine(raizProyecto, ruta));
    var raizNormalizada = Path.GetFullPath(raizProyecto)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var prefijoValido = raizNormalizada + Path.DirectorySeparatorChar;

    if (rutaCompleta != raizNormalizada &&
        !rutaCompleta.StartsWith(prefijoValido, StringComparison.OrdinalIgnoreCase)) {
        throw new UnauthorizedAccessException("La ruta esta fuera del directorio del proyecto.");
    }

    return rutaCompleta;
}

static string FormatearEntrada(string ruta) {
    var nombre = Path.GetFileName(ruta);
    return Directory.Exists(ruta) ? $"[dir] {nombre}" : $"[archivo] {nombre}";
}

static bool PareceArchivoBinario(string ruta) {
    Span<byte> buffer = stackalloc byte[512];
    using var archivo = File.OpenRead(ruta);
    var leidos = archivo.Read(buffer);

    return buffer[..leidos].Contains((byte)0);
}

sealed class ChatWindow : Window {
    private readonly IChatClient chat;
    private readonly ChatOptions opciones;
    private readonly List<ChatMessage> mensajes;
    private readonly List<MensajePantalla> visibles = [];
    private readonly Markdown conversacion;
    private readonly TextField entrada;
    private readonly Button enviar;
    private readonly Label estado;
    private bool respondiendo;
    private bool seguirRespuesta = true;

    public ChatWindow(
        IChatClient chat,
        ChatOptions opciones,
        List<ChatMessage> mensajes,
        string modelo,
        string proveedor) {
        this.chat = chat;
        this.opciones = opciones;
        this.mensajes = mensajes;

        Title = $" Asistente IA - {proveedor} / {modelo} ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        var panelConversacion = new FrameView {
            Title = " Conversacion ",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(4)
        };

        conversacion = new Markdown {
            Text = $"""
            # Asistente IA

            Proveedor: `{proveedor}`

            Modelo: `{modelo}`

            Escribi una pregunta y presiona Enter para comenzar. Presiona Esc para salir.
            """,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ShowHeadingPrefix = false
        };

        conversacion.KeyDown += (_, _) => seguirRespuesta = false;
        conversacion.MouseEvent += (_, _) => seguirRespuesta = false;
        panelConversacion.Add(conversacion);

        var panelEntrada = new FrameView {
            Title = " Entrada ",
            X = 0,
            Y = Pos.Bottom(panelConversacion),
            Width = Dim.Fill(),
            Height = 4
        };

        entrada = new TextField {
            X = 1,
            Y = 0,
            Width = Dim.Fill(13),
            CanFocus = true
        };

        enviar = new Button {
            Text = "Enviar",
            X = Pos.Right(entrada) + 1,
            Y = 0,
            Width = 10
        };

        estado = new Label {
            Text = "Enter envia - Esc sale",
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };

        entrada.Accepting += (_, e) => {
            e.Handled = true;
            _ = EnviarAsync();
        };

        enviar.Accepting += (_, e) => {
            e.Handled = true;
            _ = EnviarAsync();
        };

        panelEntrada.Add(entrada, enviar, estado);
        Add(panelConversacion, panelEntrada);
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Esc) {
            App!.RequestStop();
            return true;
        }

        return base.OnKeyDown(key);
    }

    private async Task EnviarAsync() {
        if (respondiendo) {
            return;
        }

        var texto = (entrada.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(texto)) {
            estado.Text = "Escribi un mensaje antes de enviar.";
            estado.SetNeedsDraw();
            return;
        }

        respondiendo = true;
        seguirRespuesta = true;
        HabilitarEntrada(false);

        entrada.Text = "";
        visibles.Add(new MensajePantalla("Vos", texto));
        mensajes.Add(new ChatMessage(ChatRole.User, texto));
        var indiceRespuesta = visibles.Count;
        visibles.Add(new MensajePantalla("Asistente", ""));
        ActualizarConversacion();

        var respuesta = new StringBuilder();

        try {
            await foreach (var parte in chat.GetStreamingResponseAsync(mensajes, opciones)) {
                if (string.IsNullOrEmpty(parte.Text)) {
                    continue;
                }

                respuesta.Append(parte.Text);
                var textoParcial = respuesta.ToString();

                App!.Invoke(() => {
                    visibles[indiceRespuesta] = new MensajePantalla("Asistente", textoParcial);
                    ActualizarConversacion();
                });
            }

            var textoFinal = respuesta.ToString();
            mensajes.Add(new ChatMessage(ChatRole.Assistant, textoFinal));

            if (string.IsNullOrWhiteSpace(textoFinal)) {
                App!.Invoke(() => {
                    visibles[indiceRespuesta] = new MensajePantalla(
                        "Asistente",
                        "No se recibio texto del modelo.");
                    ActualizarConversacion();
                });
            }
        }
        catch (Exception ex) {
            var mensaje = ex.Message;

            App!.Invoke(() => {
                visibles.RemoveAt(indiceRespuesta);
                visibles.Add(new MensajePantalla("Error", mensaje));
                ActualizarConversacion();
            });
        }
        finally {
            App!.Invoke(() => {
                respondiendo = false;
                HabilitarEntrada(true);
                estado.Text = "Enter envia - Esc sale";
                entrada.SetFocus();
            });
        }
    }

    private void HabilitarEntrada(bool habilitada) {
        entrada.ReadOnly = !habilitada;
        entrada.Enabled = habilitada;
        enviar.Enabled = habilitada;
        estado.Text = habilitada ? "Enter envia - Esc sale" : "El asistente esta respondiendo...";
        entrada.SetNeedsDraw();
        enviar.SetNeedsDraw();
        estado.SetNeedsDraw();
    }

    private void ActualizarConversacion() {
        conversacion.Text = RenderizarMarkdown();
        conversacion.SetNeedsDraw();

        if (seguirRespuesta) {
            conversacion.ScrollVertical(conversacion.LineCount);
        }
    }

    private string RenderizarMarkdown() {
        var salida = new StringBuilder();

        foreach (var mensaje in visibles) {
            salida.Append("## ");
            salida.AppendLine(mensaje.Rol);
            salida.AppendLine();
            salida.AppendLine(mensaje.Texto);
            salida.AppendLine();
        }

        return salida.ToString();
    }
}

record MensajePantalla(string Rol, string Texto);
