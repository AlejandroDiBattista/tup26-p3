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
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();
var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "http://localhost";
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

if (url.EndsWith("/chat/completions", StringComparison.InvariantCultureIgnoreCase))
{
    url = url.Substring(0, url.LastIndexOf("/chat/completions", StringComparison.InvariantCultureIgnoreCase));
}

IChatClient clienteBase = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(url) })
    .GetChatClient(modelo)
    .AsIChatClient();

IChatClient chat = new ChatClientBuilder(clienteBase)
    .UseFunctionInvocation()
    .Build();
    List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
];

var herramientas = new HerramientasArchivos();
var chatOptions = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(herramientas.LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo de texto."),
        AIFunctionFactory.Create(herramientas.EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo con el contenido indicado."),
        AIFunctionFactory.Create(herramientas.ListarArchivos, "listar-archivos", "Lista los archivos (y carpetas) de un directorio.")
    ]
};
using IApplication app = Application.Create().Init();

using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

var panelConversacion = new Markdown {
    Text = "# Asistente de programación listo.\n\nEscribí tu consulta abajo.",
    Width = Dim.Fill(),
    Height = Dim.Percent(80),
    CanFocus = true
};

var lineaDivisoria = new Line {
    Orientation = Orientation.Horizontal,
    Y = Pos.Bottom(panelConversacion),
    Width = Dim.Fill(),
    Height = 1
};

var inputMensaje = new TextField {
    X = 1,
    Y = Pos.Bottom(lineaDivisoria),
    Width = Dim.Percent(85),
    Height = 1,
    CanFocus = true
};

var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.Right(inputMensaje) + 1,
    Y = Pos.Bottom(lineaDivisoria),
    Width = Dim.Fill(),
    Height = 1,
    CanFocus = true
};

ventana.Add(panelConversacion, lineaDivisoria, inputMensaje, botonEnviar);

string historialPantalla = "# Asistente de programación inicializado.\n";
panelConversacion.Text = historialPantalla;
async Task EnviarMensajeUsuarioAsync()
{
    var texto = inputMensaje.Text?.ToString()?.Trim();
    if (string.IsNullOrEmpty(texto)) return;

    historialPantalla += $"\n# Vos\n\n{texto}\n\n# Asistente\n\n";
    panelConversacion.Text = historialPantalla;
    inputMensaje.Text = string.Empty;

    mensajes.Add(new ChatMessage(ChatRole.User, texto));

    _ = Task.Run(async () => {
        try
        {
            string respuestaParcialAcumulada = "";
            
            await foreach (var fragmento in chat.GetStreamingResponseAsync(mensajes, chatOptions))
            {
                if (!string.IsNullOrEmpty(fragmento.Text))
                {
                    respuestaParcialAcumulada += fragmento.Text;
                    
                    var textoActualizado = historialPantalla + respuestaParcialAcumulada;
                    app.Invoke(() => {
                        panelConversacion.Text = textoActualizado;
                    });
                }
            }

            mensajes.Add(new ChatMessage(ChatRole.Assistant, respuestaParcialAcumulada));
            historialPantalla += respuestaParcialAcumulada + "\n";
        }
        catch (Exception ex)
        {
            var msgError = historialPantalla + $"\n*Error al procesar la solicitud: {ex.Message}*\n";
            app.Invoke(() => {
                panelConversacion.Text = msgError;
            });
        }
    });
}