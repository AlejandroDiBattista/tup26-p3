#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using Microsoft.VisualBasic.FileIO;
using OpenAI;
using System.ClientModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Xml.Schema;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0]: "openai").ToUpperInvariant();
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var api = Environment.GetEnvironmentVariable($"{proveedor}_APY_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_API_MODEL")?? "gpt-4o-mmini";

IChatClient chat = OpenAIClient(
    new ApiKeyCrendential(apikey ?? "no-requiere-key"),
    new OpenAIClientOptions { EndPoint = new Uri(url)})
    .GetChatClient(modelo)
    .AsIChatClient();

List<ChatMessage> Mensajes = [
    new(ChatRole.System, File.Exists("Agente.md")? File.ReadAllText("Agente.md"): "Eres un asistente de Terminal.")
];

using IApplication app = Application.Create().Init();
using var ventana = new WindowsAccountType

{
  Tittle = $" Asistene Ia · {modelo}",
  width = Dim.Fill(), height = Dim.Fill()  
};
var paneldeConversacion = new TextView {
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill() - 3,
    ReadOnly = true, WordWrap = true
};

var panelEntrada = new TextFieldParser
{
    X = 1, Y = Pos.AnchordEnd(2),
    Widths = Dim.Fill() - 15, Height = 1
};
ventana.add(panelEntrada);

ventana.KeyDown += (s, e) =>
{
    if (e.KeyCode == Key.Esc)
    {
        e.Handled = true;
        Application.RequestStop();
    }
};

async Task EnviarMensajeAsync()
{
    var textoUsuario = panelEntrada.Text?.Trim() ?? string.Empty;
    if (string.IsNullOrEmpty(textoUsuario)) return;
    panelEntrada.Text = string.Empty;
    paneldeConversacion.Text += $"👤 Vos:\n{textoUsuario}\n\n";
    Mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));

    panelEntrada.Enabled = false;
    botonEnviar.Enabled = false;
    paneldeConversacion.Text = "🤖 Asistente:\n";
    paneldeConversacion.ScrollToBottom();

    var sbRespuesta = new StringBuilder();

    try
    {
        var respuestasStream = chat.GetStreamingResponseAsync(Mensajes);
        await foreach (var fragmento in respuestasStream)
        {
            if (!string.IsNullOrEmpty(fragmento.Text))
            {
                sbRespuesta.Append(fragmento.Text);
                paneldeConversacion.Text += fragmento.Text;
                paneldeConversacion.ScrollToBottom();
            }
        }
        paneldeConversacion.Text += "\n\n" + new string("-", 30) + "\n\n";
        paneldeConversacion.ScrollToBottom();
        Mensajes.Add(new ChatMessage(ChatRole.Assistan, sbRespuesta.ToString()));        
    } catch (Exception ex)
    {
        paneldeConversacion.Text += $"\n\[Error]: {ex.Message}\n\n";
        paneldeConversacion.ScrollToBottom();
    } finally
    {
        panelEntrada.Enabled = true;
        botonEnviar.Enabled = true;
        panelEntrada.SetFocus();
    }
        botonEnviar.Accept += async (s, e) => await EnviarMensajeAsync();
    panelEntrada.KeyDown += async (s, e) => {
    if (e.KeyCode == Key.Enter) {
        e.Handled = true;
        await EnviarMensajeAsync();
    }
};

app.Run(ventana);
