#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


var rutaEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(rutaEnv))
    rutaEnv = Path.Combine(AppContext.BaseDirectory, ".env");
DotNetEnv.Env.Load(rutaEnv);

var proveedor = (args.Length > 0 ? args[0] : "gemini").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "";
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY") ?? "";
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gemini-2.5-flash";

var http = new HttpClient();
http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md")),
    new(ChatRole.User, pregunta)
];
var sistemaPrompt = File.ReadAllText("AGENTS.md");
var historial = new List<object> {
    new { role = "system", content = sistemaPrompt }
};

        var texto = message.GetProperty("content").GetString() ?? "";
        historial.Add(new { role = "assistant", content = texto });
        return texto;
    }
}

using IApplication app = Application.Create().Init();

using var ventanaPrincipal = new Window {
    Title = $" Asistente IA  {modelo} ",
    Width = Dim.Fill(), Height = Dim.Fill()
};

using var panelConversacion = new Markdown {
    Text = "Asistente IA\n\n---\n",
    Width = Dim.Fill(),
    Height = Dim.Percent(85),
    CanFocus = true
};

using var lineaDivisoria = new Line {
    X = 0, Y = Pos.Bottom(panelConversacion),
    Width = Dim.Fill(), Height = 1
};

using var campoEntrada = new TextField {
    X = 1, Y = Pos.Bottom(lineaDivisoria) + 1,
    Width = Dim.Percent(80), Height = 1
};

using var botonEnviar = new Button {
    Text = "Enviar",
    X = Pos.Right(campoEntrada) + 2, Y = Pos.Top(campoEntrada),
    Width = Dim.Percent(15), Height = 1
};

ventanaPrincipal.Add(panelConversacion, lineaDivisoria, campoEntrada, botonEnviar);


StringBuilder acumulador = new StringBuilder("Asistente IA\n\n---\n");

async Task EnviarMensajeUsuario()
{
    string texto = campoEntrada.Text.ToString()?.Trim();
    if (string.IsNullOrEmpty(texto)) return;

    campoEntrada.Enabled = false;
    botonEnviar.Enabled = false;

    acumulador.AppendLine($"- Vos\n\n{texto}\n\n- Asistente\n");
    panelConversacion.Text = acumulador.ToString();
    campoEntrada.Text = string.Empty;

    try
    {
        var respuesta = await PreguntarAsync(texto);
        acumulador.AppendLine($"{respuesta}\n\n---\n");
    }
    catch (Exception ex)
    {
        acumulador.AppendLine($"Error: {ex.Message}\n\n---\n");
    }
    finally
    {
        app.Invoke(() => {
            panelConversacion.Text = acumulador.ToString();
            campoEntrada.Enabled = true;
            botonEnviar.Enabled = true;
            campoEntrada.SetFocus();
        });
    }
}

botonEnviar.Accepting += async (s, e) => await EnviarMensajeUsuario();
campoEntrada.Accepting += async (s, e) => await EnviarMensajeUsuario();

ventanaPrincipal.KeyDown += (s, e) => {
    string teclaTexto = e.KeyCode.ToString();
    if (teclaTexto.Contains("Esc") || teclaTexto.Contains("Escape") || teclaTexto.Contains("27"))
        Application.RequestStop();
};

app.Run(ventanaPrincipal);