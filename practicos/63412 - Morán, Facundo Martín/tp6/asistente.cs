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

var proveedor = (args.Length > 0 ? args[0] : "groq").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();
var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};
var turnos = new List<TurnoPantalla>();   
var enviando = false;
using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var conversacion = new Markdown
{
    Text = TextoConversacion(),
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3)
};

var separador = new Line
{
    X = 0,
    Y = Pos.AnchorEnd(3),
    Width = Dim.Fill()
};

var entrada = new TextField
{
    X = 1,
    Y = Pos.AnchorEnd(2),
    Width = Dim.Fill(12),
    Text = ""
};

var enviar = new Button
{
    X = Pos.AnchorEnd(10),
    Y = Pos.AnchorEnd(2),
    Width = 9,
    Text = "Enviar"
};

ventana.Add(conversacion, separador, entrada, enviar);
enviar.Accepted += (_, _) => _ = EnviarAsync();

entrada.KeyDown += (_, e) =>
{
    if (e.KeyCode == Key.Enter)
    {
        _ = EnviarAsync();
        e.Handled = true;
    }
};
app.Run(ventana);
async Task EnviarAsync()
{
    if (enviando)
{
    return;
}
    var texto = entrada.Text?.ToString()?.Trim();

    if (string.IsNullOrWhiteSpace(texto))
    {
        return;
    }
    enviando = true;
    entrada.Enabled = false;
    enviar.Enabled = false;

    entrada.Text = "";
try {

    turnos.Add(
        new TurnoPantalla(
            "Vos",
            texto
        )
        
    );
    mensajes.Add(
    new ChatMessage(
        ChatRole.User,
        texto
    )
);
var turnoAsistente = new TurnoPantalla(
    "Asistente",
    ""
);

turnos.Add(turnoAsistente);

await foreach (
    var fragmento
    in chat.GetStreamingResponseAsync(mensajes)
)
{
    if (string.IsNullOrEmpty(fragmento.Text))
    {
        continue;
    }

    turnoAsistente.Contenido += fragmento.Text;

    conversacion.Text = TextoConversacion();
}

mensajes.Add(
    new ChatMessage(
        ChatRole.Assistant,
        turnoAsistente.Contenido
    )
);


    conversacion.Text = TextoConversacion();
}
finally
{
    enviando = false;

    entrada.Enabled = true;
    enviar.Enabled = true;

    entrada.SetFocus();
}
string TextoConversacion()
{
    if (turnos.Count == 0)
    {
        return "# Asistente IA\n\nEscribí tu mensaje y presioná Enter para comenzar.";
    }

    return string.Join(
        "\n\n",
        turnos.Select(
            t => $"# {t.Rol}\n\n{t.Contenido}"
        )
    );
}
sealed class TurnoPantalla
{
    public string Rol { get; set; }
    public string Contenido { get; set; }

    public TurnoPantalla(string rol, string contenido)
    {
        Rol = rol;
        Contenido = contenido;
    }
}
