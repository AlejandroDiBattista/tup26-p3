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

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();

var asistente = new Asistente(proveedor);
asistente.Registrar(ChatRole.System, File.ReadAllText("AGENTS.md"));

using IApplication app = Application.Create().Init();

using var ventana = new Window {
    Title = $" Asistente IA · {asistente.Modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var conversacion = new Markdown {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill() - 3,
    Text = "\nEscribi tu mensaje abajo y presiona Enter o Enviar."
};

// entrada
var entrada = new TextField {
    X = 0,
    Y = Pos.Bottom(conversacion),
    Width = Dim.Fill() - 12
};

// boton enviar
var enviar = new Button {
    Text = "Enviar",
    X = Pos.Right(entrada) + 1,
    Y = Pos.Top(entrada)
};

ventana.Add(conversacion, entrada, enviar);

async void Mandar() {
    var texto = entrada.Text?.Trim() ?? "";
    if (texto.Length == 0) return;

    asistente.Registrar(ChatRole.User, texto);
    conversacion.Text += $"\n\n# Vos\n\n{texto}\n\n# Asistente\n\n";
    entrada.Text = "";
    IrAlFinal();

    var respuesta = await asistente.Respuesta(fragmento => {
        app.Invoke(() => { conversacion.Text += fragmento; });
        IrAlFinal();
    });

    asistente.Registrar(ChatRole.Assistant, respuesta);
    
    app.Invoke(() => {
    entrada.SetFocus();
    IrAlFinal();
    });
}

void IrAlFinal() {
    var alto  = conversacion.GetContentSize().Height;
    var vista = conversacion.Viewport;
    vista.Y = Math.Max(0, alto - vista.Height);
    conversacion.Viewport = vista;
}

entrada.Accepting += (sender, e) => {
    Mandar();
    e.Handled = true;
};

enviar.Accepting += (sender, e) => {
    Mandar();
    e.Handled = true;
};

app.Run(ventana);

class Asistente {
    private readonly IChatClient cliente;
    private readonly List<ChatMessage> historia = [];

    public string Modelo { get; }

    public Asistente(string proveedor) {
        var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
        var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
        Modelo     = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";

        cliente = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(url) })
            .GetChatClient(Modelo)
            .AsIChatClient();
    }

    public void Registrar(ChatRole rol, string texto) {
        historia.Add(new(rol, texto));
    }

    public async Task<string> Respuesta(Action<string> alLlegarFragmento) {
    var completa = "";
    await foreach (var fragmento in cliente.GetStreamingResponseAsync(historia)) {
        var texto = fragmento.Text ?? "";
        completa += texto;
        alLlegarFragmento(texto);
    }
    return completa;
}
}
