#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ClientModel;
using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "openai";
var configuracion = ConfiguracionIA.Crear(proveedor);

if (args.Any(a => a.Equals("--check", StringComparison.OrdinalIgnoreCase)))
{
    Console.WriteLine("Configuracion leida correctamente.");
    return;
}

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(configuracion.ApiKey),
        new OpenAIClientOptions { Endpoint = new Uri(configuracion.UrlBase) })
    .GetChatClient(configuracion.Modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var opciones = new ChatOptions
{
    Tools = HerramientasArchivos.Crear()
};

List<ChatMessage> mensajes =
[
    new(ChatRole.System, CargarPromptSistema())
];

using IApplication app = Application.Create().Init();
using var ventana = new VentanaAsistente(app, chat, opciones, mensajes, configuracion.Modelo);
app.Run(ventana);

static string CargarPromptSistema()
{
    return File.Exists("AGENTS.md")
        ? File.ReadAllText("AGENTS.md")
        : "Sos un asistente de programacion. Responde en espanol y prioriza ejemplos en C#.";
}

static class HerramientasArchivos
{
    static readonly string Raiz = Path.GetFullPath(Directory.GetCurrentDirectory());

    public static IList<AITool> Crear()
    {
        return
        [
            AIFunctionFactory.Create(ListarArchivos, new()
            {
                Name = "listar-archivos",
                Description = "Lista los archivos y carpetas de un directorio del proyecto."
            }),
            AIFunctionFactory.Create(LeerArchivo, new()
            {
                Name = "leer-archivo",
                Description = "Lee el contenido de texto de un archivo del proyecto."
            }),
            AIFunctionFactory.Create(EscribirArchivo, new()
            {
                Name = "escribir-archivo",
                Description = "Crea o sobrescribe un archivo de texto del proyecto."
            })
        ];
    }

    static string ListarArchivos(
        [Description("Ruta relativa del directorio. Usar punto para la raiz.")] string ruta = ".")
    {
        try
        {
            var completa = ResolverRuta(ruta);
            if (!Directory.Exists(completa))
            {
                return $"No existe el directorio: {ruta}";
            }

            var carpetas = Directory.GetDirectories(completa).Select(r => "[carpeta] " + Path.GetFileName(r));
            var archivos = Directory.GetFiles(completa).Select(r => "[archivo] " + Path.GetFileName(r));
            var resultado = carpetas.Concat(archivos).ToList();

            return resultado.Count == 0 ? "El directorio esta vacio." : string.Join(Environment.NewLine, resultado);
        }
        catch (Exception ex)
        {
            return "Error al listar: " + ex.Message;
        }
    }

    static string LeerArchivo(
        [Description("Ruta relativa del archivo que se quiere leer.")] string ruta)
    {
        try
        {
            var completa = ResolverRuta(ruta);
            return File.Exists(completa)
                ? File.ReadAllText(completa)
                : $"No existe el archivo: {ruta}";
        }
        catch (Exception ex)
        {
            return "Error al leer: " + ex.Message;
        }
    }

    static string EscribirArchivo(
        [Description("Ruta relativa del archivo que se quiere escribir.")] string ruta,
        [Description("Contenido completo que se guardara en el archivo.")] string contenido)
    {
        try
        {
            var completa = ResolverRuta(ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(completa)!);
            File.WriteAllText(completa, contenido);
            return $"Archivo escrito: {ruta}";
        }
        catch (Exception ex)
        {
            return "Error al escribir: " + ex.Message;
        }
    }

    static string ResolverRuta(string ruta)
    {
        var completa = Path.GetFullPath(Path.Combine(Raiz, ruta));
        var prefijo = Raiz.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (completa != Raiz && !completa.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("La ruta esta fuera del proyecto.");
        }

        return completa;
    }
}

record ConfiguracionIA(string UrlBase, string ApiKey, string Modelo)
{
    public static ConfiguracionIA Crear(string proveedor)
    {
        var nombre = proveedor.ToUpperInvariant();
        var url = Environment.GetEnvironmentVariable($"{nombre}_API_URL");
        var apiKey = Environment.GetEnvironmentVariable($"{nombre}_API_KEY") ?? "no-requiere-key";
        var modelo = Environment.GetEnvironmentVariable($"{nombre}_MODEL") ?? "gpt-5.4-mini";

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException($"Falta configurar {nombre}_API_URL.");
        }

        var urlBase = url.Trim().TrimEnd('/');
        const string finalOpenAi = "/chat/completions";

        if (urlBase.EndsWith(finalOpenAi, StringComparison.OrdinalIgnoreCase))
        {
            urlBase = urlBase[..^finalOpenAi.Length];
        }

        return new ConfiguracionIA(urlBase, apiKey, modelo);
    }
}

sealed class VentanaAsistente : Window
{
    readonly IChatClient chat;
    readonly IApplication app;
    readonly ChatOptions opciones;
    readonly List<ChatMessage> mensajes;
    readonly Markdown conversacion;
    readonly TextField entrada;
    readonly Button enviar;
    readonly Label estado;
    bool ocupado;

    public VentanaAsistente(IApplication app, IChatClient chat, ChatOptions opciones, List<ChatMessage> mensajes, string modelo)
    {
        this.app = app;
        this.chat = chat;
        this.opciones = opciones;
        this.mensajes = mensajes;

        Title = $" AsistenteIA - {modelo} ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        var panelConversacion = new FrameView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };

        conversacion = new Markdown
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = "_Escribi una consulta y presiona Enter._"
        };

        panelConversacion.Add(conversacion);

        var panelEntrada = new FrameView
        {
            X = 0,
            Y = Pos.Bottom(panelConversacion),
            Width = Dim.Fill(),
            Height = 3
        };

        enviar = new Button
        {
            Text = "Enviar",
            IsDefault = true,
            X = Pos.AnchorEnd(),
            Y = 0
        };

        entrada = new TextField
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill() - Dim.Width(enviar) - 1
        };

        estado = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "Enter envia el mensaje. Esc sale del asistente."
        };

        panelEntrada.Add(entrada, enviar);
        Add(panelConversacion, panelEntrada, estado);

        enviar.Accepting += (_, e) =>
        {
            e.Handled = true;
            _ = EnviarMensajeAsync();
        };

        entrada.Accepting += (_, e) =>
        {
            e.Handled = true;
            _ = EnviarMensajeAsync();
        };

        Initialized += (_, _) => entrada.SetFocus();

        KeyDown += (_, key) =>
        {
            if (key != Key.Esc)
            {
                return;
            }

            key.Handled = true;
            App!.RequestStop();
        };
    }

    async Task EnviarMensajeAsync()
    {
        if (ocupado)
        {
            return;
        }

        string texto = (entrada.Text?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        ocupado = true;
        entrada.Enabled = false;
        enviar.Enabled = false;
        estado.Text = "El asistente esta respondiendo...";
        entrada.Text = "";
        mensajes.Add(new ChatMessage(ChatRole.User, texto));
        Renderizar("...");

        var fragmentos = new List<ChatResponseUpdate>();
        var parcial = new StringBuilder();
        Exception? error = null;

        try
        {
            await Task.Run(async () =>
            {
                await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opciones))
                {
                    fragmentos.Add(fragmento);

                    if (!string.IsNullOrEmpty(fragmento.Text))
                    {
                        parcial.Append(fragmento.Text);
                        var textoParcial = parcial.ToString();
                        app.Invoke(() => Renderizar(textoParcial));
                    }
                }
            });
        }
        catch (Exception ex)
        {
            error = ex;
        }

        app.Invoke(() =>
        {
            if (error is null)
            {
                mensajes.AddMessages(fragmentos);
            }
            else
            {
                mensajes.Add(new ChatMessage(ChatRole.Assistant, "No se pudo obtener respuesta: " + error.Message));
            }

            Renderizar();
            entrada.Enabled = true;
            enviar.Enabled = true;
            estado.Text = "Enter envia el mensaje. Esc sale del asistente.";
            entrada.SetFocus();
            ocupado = false;
        });
    }

    void Renderizar(string? respuestaEnCurso = null)
    {
        var texto = new StringBuilder();

        foreach (var mensaje in mensajes)
        {
            if (mensaje.Role == ChatRole.User)
            {
                texto.Append("# Vos\n\n").Append(mensaje.Text).Append("\n\n");
            }

            if (mensaje.Role == ChatRole.Assistant)
            {
                texto.Append("# Asistente\n\n").Append(mensaje.Text).Append("\n\n");
            }
        }

        if (respuestaEnCurso is not null)
        {
            texto.Append("# Asistente\n\n").Append(respuestaEnCurso).Append("\n\n");
        }

        conversacion.Text = texto.Length == 0
            ? "_Escribi una consulta y presiona Enter._"
            : texto.ToString();
    }
}
