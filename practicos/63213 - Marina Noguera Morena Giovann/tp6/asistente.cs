using System;
using System.ClientModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;
using Terminal.Gui;

DotNetEnv.Env.Load(".env");

var proveedorSolicitado = args.Length > 0 ? args[0] : "GEMINI";
var proveedor = proveedorSolicitado.ToUpperInvariant();
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gemini-1.5-flash"; //

if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Faltan variables de entorno. Revisa el archivo .env.");
    return;
}


var openAIClient = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) });

IChatClient chatClient = openAIClient.GetChatClient(modelo).AsIChatClient();

Application.Init();

var servicioChat = new ServicioChat(chatClient);
var ventana = new VentanaAsistente(servicioChat);

// Salir limpiamente con la tecla ESC
Application.Top.KeyDown += (e) => 
{
    if (e.KeyEvent.Key == Key.Esc)
    {
        Application.RequestStop();
        e.Handled = true;
    }
};

Application.Run(ventana);
Application.Shutdown();

public class ServicioChat
{
    private readonly IChatClient _client;
    private readonly List<ChatMessage> _historial = new();
    private readonly ChatOptions _opcionesChat;

    public ServicioChat(IChatClient client)
    {
        _client = client;
        
        // Carga del mensaje de sistema
        var systemPrompt = File.Exists("AGENTS.md") ? File.ReadAllText("AGENTS.md") : "Sos un asistente de programación útil y claro.";
        _historial.Add(new ChatMessage(ChatRole.System, systemPrompt));

        // Definición de las herramientas de archivos
        _opcionesChat = new ChatOptions
        {
            Tools = new List<AITool>
            {
                AIFunctionFactory.Create((string ruta) => File.Exists(ruta) ? File.ReadAllText(ruta) : "Archivo no encontrado", "leer-archivo", "Devuelve el contenido de un archivo de texto"),
                AIFunctionFactory.Create((string ruta, string contenido) => { File.WriteAllText(ruta, contenido); return "Archivo guardado exitosamente"; }, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado"),
                AIFunctionFactory.Create((string ruta) => Directory.Exists(ruta) ? string.Join(", ", Directory.GetFileSystemEntries(ruta)) : "Directorio no encontrado", "listar-archivos", "Lista los archivos y carpetas de un directorio")
            }
        };
    }

    public async IAsyncEnumerable<string> EnviarMensajeAsync(string mensaje)
    {
        _historial.Add(new ChatMessage(ChatRole.User, mensaje));
        
        var respuestaCompleta = string.Empty;

        // Streaming de la respuesta considerando las herramientas
        await foreach (var update in _client.GetStreamingResponseAsync(_historial, _opcionesChat))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                respuestaCompleta += update.Text;
                yield return update.Text;
            }
        }
        
        _historial.Add(new ChatMessage(ChatRole.Assistant, respuestaCompleta));
    }

    public string GetFormattedHistory()
    {
        var builder = new StringBuilder();
        foreach (var mensaje in _historial.Where(m => m.Role != ChatRole.System))
        {
            var rol = mensaje.Role == ChatRole.User ? "Usuario" : "Asistente";
            builder.AppendLine($"## {rol}:");
            builder.AppendLine(FormatMarkdown(mensaje.Text));
            builder.AppendLine();
        }
        return builder.ToString().TrimEnd();
    }

    private static string FormatMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var lines = text.Replace("\r\n", "\n").Split('\n');
        var builder = new StringBuilder();
        var inCodeBlock = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine;
            if (line.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                builder.AppendLine(inCodeBlock ? "┌─ Bloque de código ─────────────────" : "└───────────────────────────────────");
                continue;
            }

            if (inCodeBlock)
            {
                builder.AppendLine("    " + line);
                continue;
            }

            if (line.StartsWith("### "))
            {
                builder.AppendLine("=== " + line[4..].Trim() + " ===");
                continue;
            }

            if (line.StartsWith("## "))
            {
                builder.AppendLine("== " + line[3..].Trim() + " ==");
                continue;
            }

            if (line.StartsWith("# "))
            {
                builder.AppendLine("= " + line[2..].Trim() + " =");
                continue;
            }

            if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* "))
            {
                builder.AppendLine("  " + line.Trim());
                continue;
            }

            builder.AppendLine(line);
        }

        if (inCodeBlock)
        {
            builder.AppendLine("└───────────────────────────────────");
        }

        return builder.ToString().TrimEnd();
    }
}

public class VentanaAsistente : Window
{
    private readonly ServicioChat _servicio;
    private readonly TextView _tvHistorial;
    private readonly TextField _tfEntrada;
    private readonly Button _btnEnviar;

    public VentanaAsistente(ServicioChat servicio)
    {
        _servicio = servicio;
        Title = "Asistente AI - TP6";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        // Panel de conversación
        _tvHistorial = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            ReadOnly = true,
            WordWrap = true,
            Text = string.Empty
        };

        // Panel de entrada
        _tfEntrada = new TextField
        {
            X = 0,
            Y = Pos.Bottom(_tvHistorial),
            Width = Dim.Fill(10),
            Height = 1
        };

        _btnEnviar = new Button("Enviar")
        {
            X = Pos.Right(_tfEntrada),
            Y = Pos.Top(_tfEntrada)
        };

        Add(_tvHistorial, _tfEntrada, _btnEnviar);

        // Eventos
        _btnEnviar.Clicked += () => _ = ProcesarEnvioAsync();
        _tfEntrada.KeyPress += (e) =>
        {
            if (e.KeyEvent.Key == Key.Enter)
            {
                e.Handled = true;
                _ = ProcesarEnvioAsync();
            }
        };

        _tvHistorial.KeyPress += (e) =>
        {
            if (e.KeyEvent.Key == Key.PageUp || e.KeyEvent.Key == Key.PageDown)
            {
                e.Handled = false;
            }
        };
    }

    private async Task ProcesarEnvioAsync()
    {
        var texto = _tfEntrada.Text?.ToString();
        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        var estabaAlFinal = IsAtBottom();

        _tfEntrada.Text = string.Empty;
        _tfEntrada.Enabled = false;
        _btnEnviar.Enabled = false;

        Application.MainLoop.Invoke(() =>
        {
            RenderHistorial(estabaAlFinal);
            _tvHistorial.Text += "\n> Asistente: ";
        });

        try
        {
            await foreach (var fragmento in _servicio.EnviarMensajeAsync(texto))
            {
                Application.MainLoop.Invoke(() =>
                {
                    _tvHistorial.Text += fragmento;
                    if (estabaAlFinal)
                    {
                        _tvHistorial.MoveEnd();
                    }
                });
            }

            Application.MainLoop.Invoke(() => RenderHistorial(estabaAlFinal));
        }
        catch (Exception ex)
        {
            Application.MainLoop.Invoke(() => _tvHistorial.Text += $"\n[Error de conexión: {ex.Message}]");
        }
        finally
        {
            Application.MainLoop.Invoke(() =>
            {
                _tfEntrada.Enabled = true;
                _btnEnviar.Enabled = true;
                _tfEntrada.SetFocus();
            });
        }
    }

    private bool IsAtBottom()
    {
        return _tvHistorial.TopRow + _tvHistorial.Frame.Height >= Math.Max(_tvHistorial.Lines, 1);
    }

    private void RenderHistorial(bool scrollToBottom)
    {
        _tvHistorial.Text = _servicio.GetFormattedHistory();
        if (scrollToBottom)
        {
            _tvHistorial.MoveEnd();
        }
    }
}