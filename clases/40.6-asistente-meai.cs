#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Spectre.Console@0.55.0

using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;
using System.Text.RegularExpressions;

using OpenAIChatClient = OpenAI.Chat.ChatClient;

DotNetEnv.Env.TraversePath().Load();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var modelo = "gpt-5.5";

if (string.IsNullOrWhiteSpace(apiKey)) {
    throw new InvalidOperationException("Configura OPENAI_API_KEY antes de ejecutar este ejemplo.");
}

if (Console.IsInputRedirected || Console.IsOutputRedirected) {
    throw new InvalidOperationException("Esta versión necesita una terminal interactiva.");
}

Console.InputEncoding  = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

IChatClient chat = new OpenAIChatClient(modelo, apiKey).AsIChatClient();

var mensajes = new List<ChatMessage> {
    new(ChatRole.System, """
    Sos un asistente de programación.
    Respondé en español, directo y técnico.
    Priorizá ejemplos en C# cuando el usuario no indique lenguaje.
    Si falta contexto, pedí el dato mínimo necesario.
    """)
};

var conversacion = new List<MensajeUi>();
var transcript = new StringBuilder();
var entrada = new StringBuilder();
var cursor = 0;
var desplazamiento = 0;
var salir = false;

// Usa la pantalla alternativa: al salir, la terminal recupera su contenido anterior.
Console.Write("\e[?1049h\e[?25l");

try {
    while (!salir) {
        Dibujar(conversacion, entrada.ToString(), cursor, desplazamiento, modelo);
        var tecla = Console.ReadKey(intercept: true);

        switch (tecla.Key) {
            case ConsoleKey.Enter:
                var texto = entrada.ToString().Trim();
                if (texto.Equals("/salir", StringComparison.OrdinalIgnoreCase)) {
                    salir = true;
                    break;
                }

                if (texto.Length == 0) break;

                entrada.Clear();
                cursor = 0;
                desplazamiento = 0;
                conversacion.Add(new MensajeUi("Vos", texto, Color.DeepSkyBlue1));
                mensajes.Add(new ChatMessage(ChatRole.User, texto));
                transcript.AppendLine($"## Vos\n\n{texto}\n");

                try {
                    var tarea = chat.GetResponseAsync(mensajes);
                    var cuadro = 0;
                    string[] spinner = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

                    while (!tarea.IsCompleted) {
                        Dibujar(conversacion, $"{spinner[cuadro++ % spinner.Length]} Pensando…", 0, 0, modelo, esperando: true);
                        await Task.WhenAny(tarea, Task.Delay(80));
                    }

                    var respuesta = await tarea;
                    var respuestaTexto = respuesta.Text ?? "";
                    mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaTexto));
                    conversacion.Add(new MensajeUi("Asistente", respuestaTexto, Color.Yellow));
                    transcript.AppendLine($"## Asistente\n\n{respuestaTexto}\n");
                    File.WriteAllText("salida.md", transcript.ToString());
                }
                catch (Exception ex) {
                    conversacion.Add(new MensajeUi("Error", ex.Message, Color.Red));
                }
                break;

            case ConsoleKey.Backspace when cursor > 0:
                entrada.Remove(--cursor, 1);
                break;
            case ConsoleKey.Delete when cursor < entrada.Length:
                entrada.Remove(cursor, 1);
                break;
            case ConsoleKey.LeftArrow when cursor > 0:
                cursor--;
                break;
            case ConsoleKey.RightArrow when cursor < entrada.Length:
                cursor++;
                break;
            case ConsoleKey.Home:
                cursor = 0;
                break;
            case ConsoleKey.End:
                cursor = entrada.Length;
                break;
            case ConsoleKey.PageUp:
                desplazamiento = Math.Min(conversacion.Count, desplazamiento + 1);
                break;
            case ConsoleKey.PageDown:
                desplazamiento = Math.Max(0, desplazamiento - 1);
                break;
            case ConsoleKey.Escape:
                entrada.Clear();
                cursor = 0;
                break;
            default:
                if (!char.IsControl(tecla.KeyChar)) {
                    entrada.Insert(cursor++, tecla.KeyChar);
                }
                break;
        }
    }
}
finally {
    Console.Write("\e[?25h\e[?1049l");
    Console.ResetColor();
}

static void Dibujar(
    List<MensajeUi> conversacion,
    string entrada,
    int cursor,
    int desplazamiento,
    string modelo,
    bool esperando = false) {

    Console.Clear();
    var ancho = Math.Max(30, Console.WindowWidth);
    var altoDisponible = Math.Max(5, Console.WindowHeight - 8);

    var titulo = new Rule($"[bold deepskyblue1] Asistente MEAI [/] [grey]· {Markup.Escape(modelo)}[/]")
        .RuleStyle("grey35")
        .LeftJustified();
    AnsiConsole.Write(titulo);

    var fin = Math.Max(0, conversacion.Count - desplazamiento);
    var inicio = fin;
    var altoUsado = 0;

    while (inicio > 0) {
        var alto = AltoEstimado(conversacion[inicio - 1].Texto, ancho - 6);
        if (altoUsado + alto > altoDisponible && inicio < fin) break;
        altoUsado += alto;
        inicio--;
    }

    if (fin == 0) {
        AnsiConsole.Write(new Align(
            new Markup("[grey]Todavía no hay mensajes. Escribí algo para comenzar.[/]"),
            HorizontalAlignment.Center,
            VerticalAlignment.Middle));
    }
    else {
        var paneles = conversacion[inicio..fin].Select(CrearMensaje).Cast<IRenderable>().ToList();
        AnsiConsole.Write(new Rows(paneles) { Expand = true });
    }

    // Rellena el viewport para que la caja de entrada permanezca junto al borde inferior.
    var lineasRestantes = Math.Max(0, altoDisponible - altoUsado);
    for (var i = 0; i < lineasRestantes; i++) AnsiConsole.WriteLine();

    var contenidoEntrada = esperando
        ? $"[yellow]{Markup.Escape(entrada)}[/]"
        : EntradaConCursor(entrada, cursor);

    AnsiConsole.Write(new Panel(new Markup(contenidoEntrada))
        .Header(esperando ? "[yellow] Asistente [/]" : "[deepskyblue1] Vos [/]", Justify.Left)
        .Border(BoxBorder.Rounded)
        .BorderColor(esperando ? Color.Yellow : Color.DeepSkyBlue1)
        .Expand());

    var posicion = desplazamiento == 0 ? "últimos mensajes" : $"-{desplazamiento} mensaje(s)";
    AnsiConsole.Markup($"[grey] Enter enviar · PgUp/PgDn scroll · Esc borrar · /salir terminar · {posicion}[/]");
}

static Panel CrearMensaje(MensajeUi mensaje) {
    return new Panel(RenderizarMarkdown(mensaje.Texto))
        .Header($"[bold {mensaje.Color}]{Markup.Escape(mensaje.Autor)}[/]", Justify.Left)
        .Border(BoxBorder.Rounded)
        .BorderColor(mensaje.Color)
        .Expand();
}

static IRenderable RenderizarMarkdown(string markdown) {
    var elementos = new List<IRenderable>();
    var codigo = new StringBuilder();
    var enCodigo = false;
    var lenguaje = "";

    foreach (var linea in markdown.Replace("\r\n", "\n").Split('\n')) {
        if (linea.StartsWith("```")) {
            if (!enCodigo) {
                enCodigo = true;
                lenguaje = linea[3..].Trim();
                codigo.Clear();
            }
            else {
                elementos.Add(new Panel(new Text(codigo.ToString().TrimEnd()))
                    .Header(string.IsNullOrEmpty(lenguaje) ? " código " : $" {Markup.Escape(lenguaje)} ")
                    .Border(BoxBorder.Square)
                    .BorderColor(Color.Grey35)
                    .Padding(1, 0));
                enCodigo = false;
            }
            continue;
        }

        if (enCodigo) {
            codigo.AppendLine(linea);
            continue;
        }

        if (Regex.IsMatch(linea, @"^\s*([-*_])(?:\s*\1){2,}\s*$")) {
            elementos.Add(new Rule().RuleStyle("grey35"));
        }
        else if (Regex.Match(linea, @"^(#{1,6})\s+(.+)$") is { Success: true } h) {
            var color = h.Groups[1].Length <= 2 ? "deepskyblue1" : "aqua";
            elementos.Add(new Markup($"[bold {color}]{Inline(h.Groups[2].Value)}[/]"));
        }
        else if (Regex.Match(linea, @"^\s*[-*+]\s+(.+)$") is { Success: true } item) {
            elementos.Add(new Markup($"  [deepskyblue1]•[/] {Inline(item.Groups[1].Value)}"));
        }
        else if (Regex.Match(linea, @"^\s*(\d+)\.\s+(.+)$") is { Success: true } numero) {
            elementos.Add(new Markup($"  [deepskyblue1]{numero.Groups[1].Value}.[/] {Inline(numero.Groups[2].Value)}"));
        }
        else if (Regex.Match(linea, @"^>\s?(.*)$") is { Success: true } cita) {
            elementos.Add(new Markup($"[grey]│ [italic]{Inline(cita.Groups[1].Value)}[/][/]"));
        }
        else {
            elementos.Add(new Markup(Inline(linea)));
        }
    }

    if (enCodigo) {
        elementos.Add(new Panel(new Text(codigo.ToString().TrimEnd()))
            .Header(string.IsNullOrEmpty(lenguaje) ? " código " : $" {Markup.Escape(lenguaje)} ")
            .Border(BoxBorder.Square)
            .BorderColor(Color.Grey35));
    }

    return new Rows(elementos) { Expand = true };
}

static string Inline(string texto) {
    var tokens = new List<string>();
    string Guardar(Match m) {
        var token = $"\uE000{tokens.Count}\uE001";
        var etiqueta = Markup.Escape(m.Groups[1].Value);
        if (m.Groups.Count == 3) {
            var url = Markup.Escape(m.Groups[2].Value);
            tokens.Add($"[link={url}]{etiqueta}[/]");
        }
        else {
            tokens.Add($"[black on grey82] {etiqueta} [/]");
        }
        return token;
    }

    texto = Regex.Replace(texto, @"\[([^\]]+)\]\((https?://[^)]+)\)", Guardar);
    texto = Regex.Replace(texto, @"`([^`]+)`", Guardar);
    texto = Markup.Escape(texto);
    texto = Regex.Replace(texto, @"\*\*(.+?)\*\*|__(.+?)__", m => $"[bold]{m.Groups[1].Value}{m.Groups[2].Value}[/]");
    texto = Regex.Replace(texto, @"~~(.+?)~~", "[strikethrough]$1[/]");
    texto = Regex.Replace(texto, @"(?<!\*)\*([^*]+)\*(?!\*)|(?<!_)_([^_]+)_(?!_)", m => $"[italic]{m.Groups[1].Value}{m.Groups[2].Value}[/]");

    for (var i = 0; i < tokens.Count; i++) {
        texto = texto.Replace($"\uE000{i}\uE001", tokens[i]);
    }
    return texto;
}

static string EntradaConCursor(string texto, int cursor) {
    var izquierda = Markup.Escape(texto[..cursor]);
    var caracter = cursor < texto.Length ? Markup.Escape(texto[cursor].ToString()) : " ";
    var derecha = cursor < texto.Length ? Markup.Escape(texto[(cursor + 1)..]) : "";
    return $"{izquierda}[black on white]{caracter}[/]{derecha}";
}

static int AltoEstimado(string texto, int ancho) {
    var lineas = texto.Replace("\r\n", "\n").Split('\n');
    return 2 + lineas.Sum(linea => Math.Max(1, (linea.Length + Math.Max(1, ancho) - 1) / Math.Max(1, ancho)));
}

record MensajeUi(string Autor, string Texto, Color Color);
