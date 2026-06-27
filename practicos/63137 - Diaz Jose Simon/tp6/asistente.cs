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
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? throw new InvalidOperationException($"Falta {proveedor}_API_URL en .env");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var opciones = Herramientas.CrearOpciones();

List<ChatMessage> historial = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

using IApplication app = Application.Create().Init();
app.Run(new VentanaChat(chat, historial, opciones, modelo));


class VentanaChat : Window {
    readonly IChatClient chat;
    readonly List<ChatMessage> historial;
    readonly ChatOptions opciones;
    readonly Markdown panel;
    readonly TextField entrada;
    readonly Button boton;
    bool ocupado;
    readonly StringBuilder conversacion = new();

    public VentanaChat(IChatClient chat, List<ChatMessage> historial, ChatOptions opciones, string modelo) {
        this.chat = chat;
        this.historial = historial;
        this.opciones = opciones;

        Title = $" Asistente IA · {modelo} ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        panel = new() {
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };

        entrada = new() {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(12),
        };

        boton = new() {
            Text = "  Enviar  ",
            X = Pos.Right(entrada) + 1,
            Y = Pos.AnchorEnd(1),
        };

        entrada.Accepting += async (_, _) => await Enviar();
        boton.Accepting += async (_, _) => await Enviar();

        Add(panel, entrada, boton);
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Esc) {
            App!.RequestStop();
            return true;
        }
        return base.OnKeyDown(key);
    }

    async Task Enviar() {
        if (ocupado) return;
        var texto = entrada.Text?.Trim();
        if (string.IsNullOrEmpty(texto)) return;

        ocupado = true;
        entrada.Enabled = false;
        boton.Enabled = false;
        entrada.Text = "";

        historial.Add(new(ChatRole.User, texto));
        conversacion.Append($"\n\n### Vos\n\n{texto}\n\n### Asistente\n\n");
        panel.Text = conversacion.ToString();

        try {
            var respuesta = new StringBuilder();
            await foreach (var item in chat.GetStreamingResponseAsync(historial, opciones)) {
                if (item.Text is not null) {
                    respuesta.Append(item.Text);
                    panel.Text = conversacion + respuesta.ToString();
                }
            }

            historial.Add(new(ChatRole.Assistant, respuesta.ToString()));
            conversacion.Append(respuesta);
        } catch (Exception ex) {
            conversacion.Append($"\n\n*Error: {ex.Message}*");
            panel.Text = conversacion.ToString();
        } finally {
            ocupado = false;
            entrada.Enabled = true;
            boton.Enabled = true;
            entrada.SetFocus();
        }
    }
}


static class Herramientas {
    static readonly string Raiz = Directory.GetCurrentDirectory();

    public static ChatOptions CrearOpciones() => new() {
        Tools = [
            AIFunctionFactory.Create(Leer, new() {
                Name = "leer-archivo",
                Description = "Devuelve el contenido de un archivo de texto"
            }),
            AIFunctionFactory.Create(Escribir, new() {
                Name = "escribir-archivo",
                Description = "Crea o sobrescribe un archivo con el contenido indicado"
            }),
            AIFunctionFactory.Create(Listar, new() {
                Name = "listar-archivos",
                Description = "Lista los archivos y carpetas de un directorio"
            })
        ]
    };

    [Description("Devuelve el contenido de un archivo de texto")]
    public static string Leer([Description("Ruta del archivo")] string ruta) {
        try {
            var completa = RutaSegura(ruta);
            if (!File.Exists(completa))
                return $"No se encontró el archivo: {ruta}";
            return File.ReadAllText(completa);
        } catch (Exception ex) {
            return $"Error al leer '{ruta}': {ex.Message}";
        }
    }

    [Description("Crea o sobrescribe un archivo con el contenido indicado")]
    public static string Escribir(
        [Description("Ruta del archivo")] string ruta,
        [Description("Contenido del archivo")] string contenido) {
        try {
            var completa = RutaSegura(ruta);
            var dir = Path.GetDirectoryName(completa);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(completa, contenido);
            return $"Archivo escrito: {ruta}";
        } catch (Exception ex) {
            return $"Error al escribir '{ruta}': {ex.Message}";
        }
    }

    [Description("Lista los archivos y carpetas de un directorio")]
    public static string Listar([Description("Ruta del directorio")] string ruta = ".") {
        try {
            var completa = RutaSegura(ruta);
            if (!Directory.Exists(completa))
                return $"No se encontró el directorio: {ruta}";
            var resultado = new StringBuilder();
            foreach (var entry in Directory.EnumerateFileSystemEntries(completa))
                resultado.AppendLine(Path.GetFileName(entry));
            return resultado.ToString().TrimEnd();
        } catch (Exception ex) {
            return $"Error al listar '{ruta}': {ex.Message}";
        }
    }

    static string RutaSegura(string ruta) {
        var completa = Path.GetFullPath(Path.Combine(Raiz, ruta));
        var prefijo = Raiz.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!completa.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Acceso denegado");
        return completa;
    }
}
