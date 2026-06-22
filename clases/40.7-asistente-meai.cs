#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TextMateSharp.Grammars;

using OpenAIChatClient = OpenAI.Chat.ChatClient;

// Terminal.Gui recomienda EditorView para editores complejos. TextView alcanza para
// esta entrada breve y evita sumar otro paquete al ejemplo.
#pragma warning disable CS0618

DotNetEnv.Env.Load();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var modelo = "gpt-5.5";
var archivoPrompt = "AGENTS.md";

if (string.IsNullOrWhiteSpace(apiKey)) {
    throw new InvalidOperationException("Configura OPENAI_API_KEY antes de ejecutar este ejemplo.");
}

if (!File.Exists(archivoPrompt)) {
    throw new FileNotFoundException($"No se encontró el prompt de sistema: {archivoPrompt}");
}

var promptSistema = File.ReadAllText(archivoPrompt);
if (string.IsNullOrWhiteSpace(promptSistema)) {
    throw new InvalidOperationException($"El prompt de sistema está vacío: {archivoPrompt}");
}

IChatClient chat = new OpenAIChatClient(modelo, apiKey).AsIChatClient();

using IApplication app = Application.Create().Init();
app.Run(new AsistenteWindow(chat, modelo, promptSistema));

class AsistenteWindow : Window {
    private readonly IChatClient chat;
    private readonly string modelo;
    private readonly Markdown historial;
    private readonly EntradaChat entrada;
    private readonly Button enviar;
    private readonly Label estado;
    private readonly StringBuilder transcript = new();
    private readonly List<ChatMessage> mensajes;
    private readonly List<MensajeUi> conversacion = [];
    private bool respondiendo;
    private bool autoScrollActivo;
    private bool ajustandoViewport;
    private int lineaInicioRespuesta;

    public AsistenteWindow(IChatClient chat, string modelo, string promptSistema) {
        this.chat = chat;
        this.modelo = modelo;

        Title  = $" Asistente MEAI · {modelo} ";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        mensajes = [new ChatMessage(ChatRole.System, promptSistema)];

        var marcoConversacion = new FrameView {
            Title = " Conversación · mouse/↑↓/PgUp/PgDn para desplazar ",
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill(7),
            BorderStyle = LineStyle.Rounded
        };

        historial = new Markdown {
            X = 1, Y = 0,
            Width = Dim.Fill(1), Height = Dim.Fill(),
            CanFocus = true,
            ShowHeadingPrefix = false, ShowCopyButtons = true,
            SyntaxHighlighter = new TextMateSyntaxHighlighter(ThemeName.DarkPlus),
            UseThemeBackground = false,
            Text = "# Asistente MEAI\n\nEscribí un mensaje para comenzar."
        };

        historial.ViewportChanged += (_, _) => {
            if (respondiendo && !ajustandoViewport) {
                autoScrollActivo = false;
            }
        };
        historial.SubViewsLaidOut += (_, _) => {
            foreach (var bloqueCodigo in historial.SubViews.OfType<MarkdownCodeBlock>()) {
                bloqueCodigo.ThemeBackground = Color.White;
            }
        };
        marcoConversacion.Add(historial);

        var marcoEntrada = new FrameView {
            Title = " Tu mensaje ",
            X = 0, Y = Pos.AnchorEnd(7),
            Width = Dim.Fill(), Height = 5,
            BorderStyle = LineStyle.Rounded
        };

        entrada = new EntradaChat {
            X = 1, Y = 0,
            Width = Dim.Fill(15), Height = Dim.Fill(),
            WordWrap = true, Multiline = true,
            EnterKeyAddsLine = true, TabKeyAddsTab = false
        };

        // El esquema Base usa gris para texto editable. Este esquema mantiene
        // explícitamente la entrada en negro sobre blanco en todos sus estados.
        var entradaBlanca = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.White);
        SchemeManager.AddScheme("EntradaBlanca", new Scheme(entradaBlanca) {
            Focus    = entradaBlanca,
            Editable = entradaBlanca,
            ReadOnly = entradaBlanca,
            Disabled = entradaBlanca
        });
        entrada.SchemeName = "EntradaBlanca";
        entrada.Enviar = () => _ = EnviarAsync();

        enviar = new Button {
            Text = "Enviar",
            X = Pos.AnchorEnd(15), Y = Pos.Center(),
            Width = 15,
            IsDefault = true
        };
        enviar.Accepting += (_, e) => { e.Handled = true; _ = EnviarAsync(); };
        marcoEntrada.Add(entrada, enviar);

        estado = new Label {
            X = 1, Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(2), Height = 1,
            Text = "Enter enviar · Shift+Enter nueva línea · Ctrl+L limpiar · Esc salir"
        };

        Add(marcoConversacion, marcoEntrada, estado);
        Initialized += (_, _) => entrada.SetFocus();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Esc || key == Key.Q.WithCtrl) {
            App!.RequestStop();
            return true;
        }

        if (key == Key.L.WithCtrl && !respondiendo) {
            conversacion.Clear();
            transcript.Clear();
            autoScrollActivo = false;
            lineaInicioRespuesta = 0;
            historial.Text = "# Asistente MEAI\n\nConversación limpiada.";
            entrada.SetFocus();
            return true;
        }

        return base.OnKeyDown(key);
    }

    private async Task EnviarAsync() {
        if (respondiendo) return;

        var texto = entrada.Text?.ToString()?.Trim() ?? "";
        if (texto.Length == 0) return;

        if (texto.Equals("/salir", StringComparison.OrdinalIgnoreCase)) {
            App!.RequestStop();
            return;
        }

        entrada.Text = "";
        conversacion.Add(new MensajeUi("Vos", texto));
        mensajes.Add(new ChatMessage(ChatRole.User, texto));
        transcript.AppendLine($"## Vos\n\n{texto}\n");
        respondiendo = true;
        CambiarEstado(true, "⏳ El asistente está escribiendo…");
        ActualizarHistorial();

        // La respuesta comienza inmediatamente después del contenido ya renderizado.
        // Este valor limita el auto-scroll para que su encabezado no salga por arriba.
        lineaInicioRespuesta = historial.LineCount;
        autoScrollActivo = true;
        var mensajeRespuesta = new MensajeUi("Asistente", "");
        conversacion.Add(mensajeRespuesta);
        ActualizarHistorial(seguirRespuesta: true);

        try {
            var respuesta = new StringBuilder();

            await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes)) {
                if (string.IsNullOrEmpty(fragmento.Text)) continue;

                respuesta.Append(fragmento.Text);
                var textoParcial = respuesta.ToString();
                App!.Invoke(() => {
                    mensajeRespuesta.Texto = textoParcial;
                    ActualizarHistorial(seguirRespuesta: true);
                });
            }

            var textoRespuesta = respuesta.ToString();
            mensajes.Add(new ChatMessage(ChatRole.Assistant, textoRespuesta));
            transcript.AppendLine($"## Asistente\n\n{textoRespuesta}\n");
            File.WriteAllText("salida.md", transcript.ToString());

            App!.Invoke(() => {
                ActualizarHistorial(seguirRespuesta: true);
                CambiarEstado(false, "Listo · Enter enviar · Shift+Enter nueva línea · Esc salir");
                entrada.SetFocus();
            });
        } catch (Exception ex) {
            App!.Invoke(() => {
                conversacion.Add(new MensajeUi("Error", $"No se pudo obtener la respuesta:\n\n`{ex.Message}`"));
                ActualizarHistorial(seguirRespuesta: true);
                CambiarEstado(false, "Error al consultar el modelo · podés volver a intentar");
                entrada.SetFocus();
            });
        }
    }

    private void CambiarEstado(bool ocupado, string texto) {
        respondiendo    = ocupado;
        if (!ocupado) autoScrollActivo = false;
        entrada.Enabled = !ocupado;
        enviar.Enabled  = !ocupado;
        estado.Text     = texto;
    }

    private void ActualizarHistorial(bool seguirRespuesta = false) {
        // Al reemplazar Text, Markdown reinicia su viewport durante el nuevo layout.
        // Guardamos la ubicación para que agregar texto no cambie lo que el usuario
        // estaba leyendo.
        var ubicacionViewport = historial.Viewport.Location;
        var documento = new StringBuilder();

        foreach (var mensaje in conversacion) {
            documento.AppendLine(mensaje.Autor switch {
                "Vos"   => "## 👤 Vos",
                "Error" => "## ⚠ Error",
                _       => "## ✦ Asistente"
            });
            // documento.AppendLine();
            documento.AppendLine(mensaje.Texto);
            documento.AppendLine();
        }

        ajustandoViewport = true;
        try {
            historial.Text = documento.ToString();
            historial.SetNeedsLayout();
            historial.Layout();

            var viewport = historial.Viewport;
            viewport.Location = ubicacionViewport;

            if (seguirRespuesta && autoScrollActivo) {
                var ultimaLineaVisible = Math.Max(0, historial.LineCount - viewport.Height);
                viewport.Y = Math.Min(lineaInicioRespuesta, ultimaLineaVisible);
            }

            historial.Viewport = viewport;
        } catch (Exception ex) {
            Console.WriteLine($"Error al actualizar el historial: {ex.Message}");
        } finally { 
            ajustandoViewport = false;
        }
    }
}

class EntradaChat : TextView {
    public Action? Enviar { get; set; }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Enter) {
            Enviar?.Invoke();
            return true;
        }

        if (key == Key.Enter.WithShift) {
            InsertText("\n");
            return true;
        }

        return base.OnKeyDown(key);
    }
}

class MensajeUi(string autor, string texto) {
    public string Autor { get; } = autor;
    public string Texto { get; set; } = texto;
}
