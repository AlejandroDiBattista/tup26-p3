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

List<ChatMessage> historialMensajes = [new(ChatRole.System, File.ReadAllText("AGENTS.md"))];
string textoConsolaAcumulado = "# Sistema inicializado correctamente.\n";
IChatClient clienteChat;
ChatOptions opcionesChat;

ConfigurarClienteIA();

using IApplication app = Application.Create().Init();

TextField campoTextoInput;
var ventanaPrincipal = CrearInterfaz(app, out campoTextoInput);

campoTextoInput.SetFocus();

app.Run(ventanaPrincipal);
void ConfigurarClienteIA()
{
    var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
    var urlBase   = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "http://localhost";
    var token     = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
    var modelo    = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

    if (urlBase.EndsWith("/chat/completions", StringComparison.InvariantCultureIgnoreCase))
    {
        urlBase = urlBase.Substring(0, urlBase.LastIndexOf("/chat/completions", StringComparison.InvariantCultureIgnoreCase));
    }
        IChatClient baseClient = new OpenAIClient(
        new ApiKeyCredential(token ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(urlBase) })
        .GetChatClient(modelo)
        .AsIChatClient();

    clienteChat = new ChatClientBuilder(baseClient).UseFunctionInvocation().Build();

    var herramientas = new HerramientasArchivos();
    opcionesChat = new ChatOptions
    {
        Tools = [
            AIFunctionFactory.Create(herramientas.LeerArchivo, "leer-archivo", "Devuelve el contenido de un archivo."),
            AIFunctionFactory.Create(herramientas.EscribirArchivo, "escribir-archivo", "Crea o sobrescribe un archivo."),
            AIFunctionFactory.Create(herramientas.ListarArchivos, "listar-archivos", "Lista elementos de una carpeta.")
        ]
    };
}
Window CrearInterfaz(IApplication aplicacionActiva, out TextField campoTextoOut)
{
    var win = new Window
    {
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        Title = " Interfaz de Asistencia de IA "
    };

    var marcoChat = new FrameView {
        Title = " Historial de Conversación (Markdown) ",
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Percent(82)
    };
        var vistaChat = new Markdown {
        Text = textoConsolaAcumulado,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        CanFocus = true
    };

    marcoChat.Add(vistaChat);
        var marcoEntrada = new FrameView {
        Title = " Consola de Entrada (Presione Enter para enviar / Esc para salir) ",
        X = 0,
        Y = Pos.Bottom(marcoChat),
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };

    var campoTexto = new TextField {
        X = 1,
        Y = 0,
        Width = Dim.Percent(85),
        Height = 1,
        CanFocus = true
    };

    campoTextoOut = campoTexto;

    var btnEnviar = new Button {
        Text = "Enviar",
        X = Pos.Right(campoTexto) + 2,
        Y = 0,
        Width = Dim.Fill(),
        Height = 1,
        CanFocus = true
    };

    marcoEntrada.Add(campoTexto, btnEnviar);

    win.Add(marcoChat, marcoEntrada);
    async Task ProcesarFlujoAsync()
{
    var entradaUsuario = campoTexto.Text?.ToString()?.Trim();
    if (string.IsNullOrEmpty(entradaUsuario)) return;

    textoConsolaAcumulado += $"\n# Vos\n\n{entradaUsuario}\n\n# Asistente\n\n";
    vistaChat.Text = textoConsolaAcumulado;
    campoTexto.Text = string.Empty;

    historialMensajes.Add(new ChatMessage(ChatRole.User, entradaUsuario));

    _ = Task.Run(async () =>
    {
        try
        {
            string bufferRespuesta = "";

            await foreach (var pieza in clienteChat.GetStreamingResponseAsync(historialMensajes, opcionesChat))
            {
                if (!string.IsNullOrEmpty(pieza.Text))
                {
                    bufferRespuesta += pieza.Text;

                    var actualizacion = textoConsolaAcumulado + bufferRespuesta;

                    aplicacionActiva.Invoke(() =>
                    {
                        vistaChat.Text = actualizacion;
                    });
                }
            }

            historialMensajes.Add(new ChatMessage(ChatRole.Assistant, bufferRespuesta));
            textoConsolaAcumulado += bufferRespuesta + "\n";
        }
                    catch (Exception ex)
            {
                var cadenaError = textoConsolaAcumulado + $"\n*Fallo de comunicación: {ex.Message}*\n";

                aplicacionActiva.Invoke(() =>
                {
                    vistaChat.Text = cadenaError;
                });
            }
        });
    }

    campoTexto.Accepting += async (s, e) =>
    {
        e.Handled = true;
        await ProcesarFlujoAsync();
    };

    btnEnviar.Accepting += async (s, e) =>
    {
        e.Handled = true;
        await ProcesarFlujoAsync();
    };

    win.KeyDown += (s, e) =>
    {
        if (e != null && e.ToString().Contains("Esc"))
        {
            aplicacionActiva.RequestStop();
            e.Handled = true;
        }
    };

    return win;
}

public class HerramientasArchivos
{
    // LeerArchivo
    // EscribirArchivo
    // ListarArchivos
}