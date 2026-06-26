#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using Terminal.Gui.App;
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
    .AsIChatClient();

List<ChatMessage> mensajes =
[
    new(ChatRole.System, CargarPromptSistema())
];

using IApplication app = Application.Create().Init();
using var ventana = new VentanaAsistente(chat, mensajes, configuracion.Modelo);
app.Run(ventana);

static string CargarPromptSistema()
{
    return File.Exists("AGENTS.md")
        ? File.ReadAllText("AGENTS.md")
        : "Sos un asistente de programacion. Responde en espanol y prioriza ejemplos en C#.";
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
    readonly List<ChatMessage> mensajes;
    readonly Markdown conversacion;
    readonly TextField entrada;
    readonly Button enviar;

    public VentanaAsistente(IChatClient chat, List<ChatMessage> mensajes, string modelo)
    {
        this.chat = chat;
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

        panelEntrada.Add(entrada, enviar);
        Add(panelConversacion, panelEntrada);

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
    }

    async Task EnviarMensajeAsync()
    {
        string texto = (entrada.Text?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        entrada.Text = "";
        mensajes.Add(new ChatMessage(ChatRole.User, texto));
        Renderizar();

        var respuesta = await chat.GetResponseAsync(mensajes);
        mensajes.Add(new ChatMessage(ChatRole.Assistant, respuesta.Text ?? ""));
        Renderizar();
        entrada.SetFocus();
    }

    void Renderizar()
    {
        var texto = "";

        foreach (var mensaje in mensajes)
        {
            if (mensaje.Role == ChatRole.User)
            {
                texto += $"# Vos\n\n{mensaje.Text}\n\n";
            }

            if (mensaje.Role == ChatRole.Assistant)
            {
                texto += $"# Asistente\n\n{mensaje.Text}\n\n";
            }
        }

        conversacion.Text = string.IsNullOrWhiteSpace(texto)
            ? "_Escribi una consulta y presiona Enter._"
            : texto;
    }
}
