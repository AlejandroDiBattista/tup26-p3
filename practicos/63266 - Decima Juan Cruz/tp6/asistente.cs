#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;


DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url = NormalizarEndpoint(Environment.GetEnvironmentVariable($"{proveedor}_API_URL")
    ?? "https://api.groq.com/openai/v1/chat/completions");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "sin-api-key";
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen/qwen3.6-27b";

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};

var opciones = new ChatOptions
{
    Tools = CrearHerramientasDeArchivos()
};

var turnos = new List<TurnoMostrado>();

using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown
{
    Text = "# Asistente IA\n\nListo para conversar.",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    CanFocus = true
};

var entrada = new TextField
{
    X = 0,
    Y = Pos.AnchorEnd(3),
    Width = Dim.Fill(12),
    Height = 1
};

var enviar = new Button
{
    Text = "Enviar",
    X = Pos.AnchorEnd(10),
    Y = Pos.AnchorEnd(3),
    Width = 10,
    Height = 1,
    IsDefault = true
};

var estado = new Label
{
    Text = "Enter: enviar | Esc: salir",
    X = 0,
    Y = Pos.AnchorEnd(2),
    Width = Dim.Fill(),
    Height = 1
};

ventana.Add(conversacion, entrada, enviar, estado);
entrada.SetFocus();

enviar.Accepting += (_, e) =>
{
    e.Handled = true;
    _ = EnviarMensajeAsync();
};

entrada.KeyDown += (_, key) =>
{
    if (key == Key.Enter)
    {
        key.Handled = true;
        _ = EnviarMensajeAsync();
    }
};

ventana.KeyDown += (_, key) =>
{
    if (key == Key.Esc)
    {
        key.Handled = true;
        app.RequestStop(ventana);
    }
};

app.Run(ventana);

async Task EnviarMensajeAsync()
{
    var textoUsuario = entrada.Text?.ToString()?.Trim();
    if (string.IsNullOrWhiteSpace(textoUsuario) || !entrada.Enabled)
    {
        return;
    }

    entrada.Text = string.Empty;
    entrada.Enabled = false;
    enviar.Enabled = false;
    estado.Text = "El asistente esta respondiendo...";

    mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));
    turnos.Add(new TurnoMostrado("Vos", textoUsuario));
    var respuesta = new StringBuilder();
    turnos.Add(new TurnoMostrado("Asistente", string.Empty));
    RefrescarConversacion();

    try
    {
        await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, opciones))
        {
            if (string.IsNullOrEmpty(fragmento.Text))
            {
                continue;
            }

            respuesta.Append(fragmento.Text);
            turnos[^1] = turnos[^1] with { Texto = respuesta.ToString() };
            app.Invoke(RefrescarConversacion);
        }

        var textoAsistente = respuesta.ToString();
        mensajes.Add(new ChatMessage(ChatRole.Assistant, textoAsistente));
    }
    catch (Exception ex)
    {
        var error = $"No se pudo obtener respuesta del modelo.\n\nDetalle: `{ex.Message}`";
        turnos[^1] = turnos[^1] with { Texto = error };
        mensajes.Add(new ChatMessage(ChatRole.Assistant, error));
        app.Invoke(RefrescarConversacion);
    }
    finally
    {
        app.Invoke(() =>
        {
            entrada.Enabled = true;
            enviar.Enabled = true;
            estado.Text = "Enter: enviar | Esc: salir";
            entrada.SetFocus();
        });
    }
}

void RefrescarConversacion()
{
    var estabaAbajo = conversacion.VerticalScrollBar.Value >=
        Math.Max(0, conversacion.VerticalScrollBar.ScrollableContentSize - conversacion.VerticalScrollBar.VisibleContentSize - 1);

    conversacion.Text = RenderizarTurnos(turnos);
    conversacion.SetNeedsDraw();

    if (estabaAbajo)
    {
        conversacion.VerticalScrollBar.Value = Math.Max(0,
            conversacion.VerticalScrollBar.ScrollableContentSize - conversacion.VerticalScrollBar.VisibleContentSize);
    }
}

static string RenderizarTurnos(IEnumerable<TurnoMostrado> turnos)
{
    var markdown = new StringBuilder("# Conversacion\n");

    foreach (var turno in turnos)
    {
        markdown.AppendLine();
        markdown.Append("## ").AppendLine(turno.Autor);
        markdown.AppendLine();
        markdown.AppendLine(string.IsNullOrWhiteSpace(turno.Texto) ? "_Escribiendo..._" : turno.Texto);
    }

    return markdown.ToString();
}

record TurnoMostrado(string Autor, string Texto);