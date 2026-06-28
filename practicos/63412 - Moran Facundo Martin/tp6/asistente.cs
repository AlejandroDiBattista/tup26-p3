#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
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
if (string.IsNullOrWhiteSpace(url))
{
    Console.Error.WriteLine(
        $"Falta configurar {proveedor}_API_URL en el entorno o en .env."
    );
    return;
}

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? "no-requiere-key"),
        new OpenAIClientOptions { Endpoint = new Uri(NormalizarEndpoint(url)) })
    .GetChatClient(modelo)
    .AsIChatClient();
    chat = new ChatClientBuilder(chat)
    .UseFunctionInvocation()
    .Build();
var proyecto = Directory.GetCurrentDirectory();
var mensajes = new List<ChatMessage>
{
    new(ChatRole.System, File.ReadAllText("AGENTS.md"))
};
var opciones = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(
            LeerArchivo,
            "leer-archivo",
            "Devuelve el contenido de un archivo."
        ),

        AIFunctionFactory.Create(
            EscribirArchivo,
            "escribir-archivo",
            "Crea o sobrescribe un archivo."
        ),

        AIFunctionFactory.Create(
            ListarArchivos,
            "listar-archivos",
            "Lista archivos y carpetas."
        )
    ]
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
entrada.SetFocus();
app.Invoke(_ => entrada.SetFocus());
enviar.Accepted += (_, _) => _ = EnviarAsync();

entrada.KeyDown += (_, e) =>
{
    if (e.KeyCode == Key.Enter)
    {
        _ = EnviarAsync();
        e.Handled = true;
    }
};
ventana.KeyDown += (_, e) =>
{
    if (e.KeyCode == Key.Esc)
    {
        app.RequestStop();
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
    var turnoAsistente = new TurnoPantalla(
    "Asistente",
    ""
);
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


turnos.Add(turnoAsistente);

await foreach (
    var fragmento
    in chat.GetStreamingResponseAsync(
    mensajes,
    opciones
)
)
{
    if (string.IsNullOrEmpty(fragmento.Text))
    {
        continue;
    }

    turnoAsistente.Contenido += fragmento.Text;

    RefrescarConversacion();
}

mensajes.Add(
    new ChatMessage(
        ChatRole.Assistant,
        turnoAsistente.Contenido
    )
);


    RefrescarConversacion();
}
catch (Exception ex)
{
    turnoAsistente.Contenido =
        $"Error al consultar el modelo: `{ex.Message}`";

    RefrescarConversacion();
}
finally
{
    enviando = false;

    entrada.Enabled = true;
    enviar.Enabled = true;

    entrada.SetFocus();
}

}
void RefrescarConversacion()
{
    app.Invoke(() =>
    {
        conversacion.Text = TextoConversacion();
        conversacion.SetNeedsDraw();
    });
}
string TextoConversacion()
{
    if (turnos.Count == 0)
    {
        return "# Asistente IA\n\nEscribí tu mensaje y presiona Enter para enviarlo. Esc cierra la aplicación.";
    }

    return string.Join(
        "\n\n",
        turnos.Select(
            t => $"# {t.Rol}\n\n{t.Contenido}"
        )
    );
}
string LeerArchivo(string ruta)
{
    var path = ResolverRutaProyecto(ruta);

    return File.ReadAllText(path);
}
string EscribirArchivo(
    string ruta,
    string contenido
)
{
    var path = ResolverRutaProyecto(ruta);

    Directory.CreateDirectory(
        Path.GetDirectoryName(path)!
    );

    File.WriteAllText(
        path,
        contenido
    );

    return $"Archivo escrito: {Path.GetRelativePath(proyecto, path)}";
}
string ListarArchivos(string ruta)
{
    var path = ResolverRutaProyecto(ruta);

    return string.Join(
        Environment.NewLine,
        Directory.EnumerateFileSystemEntries(path)
    );
}
string ResolverRutaProyecto(string ruta)
{
    var combinada =
        Path.Combine(proyecto, ruta);

    return Path.GetFullPath(combinada);
}
string NormalizarEndpoint(string endpoint)
{
    const string chatCompletions = "/chat/completions";

    endpoint = endpoint.TrimEnd('/');

    if (endpoint.EndsWith(chatCompletions, StringComparison.OrdinalIgnoreCase))
    {
        endpoint = endpoint[..^chatCompletions.Length];
    }

    return endpoint;
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
