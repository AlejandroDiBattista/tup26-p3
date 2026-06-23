#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.7.0
#:package Microsoft.Extensions.AI.OpenAI@10.7.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using Terminal.Gui.Drawing;
using Terminal.Gui.Configuration;
using System.ComponentModel;
DotNetEnv.Env.Load();

// ------------------config y arranque -------------------------------------
bool cerrar = false;
var url = Environment.GetEnvironmentVariable("GEMINI_API_URL");
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var modelo = Environment.GetEnvironmentVariable("GEMINI_MODEL");

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? ""),
        new OpenAIClientOptions { Endpoint = new Uri(url ?? "") })
    .GetChatClient(modelo)
    .AsIChatClient();
List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md") + 
    $"\n\nDirectorio de trabajo actual: {Directory.GetCurrentDirectory()}")
];

IChatClient herramientas = new ChatClientBuilder(chat)
    .UseFunctionInvocation()
    .Build();

var opcionesHerramientas = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(LeerArchivo, "LeerArchivo"),
        AIFunctionFactory.Create(EscribirArchivo, "EscribirArchivo"),
        AIFunctionFactory.Create(Listar, "Listar")
    ]
};


// --------------------Configuracion de terminal gui 2.0------------------------------------------

ConfigurationManager.Enable(ConfigLocations.All);
ConfigurationManager.Apply();
Scheme entrad = new Scheme() { Normal = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.Gray) };
using IApplication app = Application.Create().Init();
using Window gui = new() { };

// --------------------Ventanas TG ------------------------------------------

using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(),
    Height = Dim.Percent(90),
    SchemeName = "entrad"
};

var entrada = new TextField {
    X = 0,
    Y = Pos.Bottom(ventana),
    Width = Dim.Fill(),
    Text = "",
    Height = Dim.Fill()
};
var visualizador = new Markdown {
    Width = Dim.Fill(),
    Height = Dim.Fill()
};
ventana.Add(visualizador);


// ----------------------------Funcion para enviar mensajes------------------------------------------
entrada.KeyDown += async (sender, e) => {
    if (e.KeyCode == Key.Enter && !string.IsNullOrWhiteSpace(entrada.Text)) {
        if (entrada.Text == "/salir") {
            cerrar = true;
            app.RequestStop();
        }
        string prompt = entrada.Text;
        entrada.Text = "";
        mensajes.Add(new ChatMessage(ChatRole.User, prompt));
        e.Handled = true;

        try {
            var textoIA = "";
            var mensajeIA = new ChatMessage(ChatRole.Assistant, "");
            mensajes.Add(mensajeIA);

            await foreach (var pedazo in herramientas.GetStreamingResponseAsync(mensajes, opcionesHerramientas)) {
                textoIA += pedazo.Text;
                mensajes[^1] = new ChatMessage(ChatRole.Assistant, textoIA);

                // Actualizar el historial visible
                var textoAcumulado = new System.Text.StringBuilder();
                foreach (var m in mensajes) {
                    if (m.Role == ChatRole.System) continue;
                    if (m.Role == ChatRole.User) {
                        textoAcumulado.AppendLine($"# ————————————————————— YO ———————————————————————————————————————\n{m.Text}\n");
                    } else {
                        textoAcumulado.AppendLine($"# —————————————————————🤖 ASISTENTE————————————————————————————————\n{m.Text}\n");
                    }
                }
                app.Invoke(() => visualizador.Text = textoAcumulado.ToString());
            }
        } catch (Exception ex) {
            visualizador.Text += $"\n\n# Error\n\n{ex.Message}";
        }
        e.Handled = true;
    }

};


// ================== HERRAMIENTAS DEL AGENTE =================

[Description("Lee el contenido completo de un archivo de texto.")]
static string LeerArchivo(
    [Description("Ruta relativa del archivo a leer.")] string ruta) {
    if (!File.Exists(ruta)) return $""" {ruta} no existe. """;
    return File.ReadAllText(ruta);
}
;

[Description("Crea o sobrescribe un archivo con el contenido indicado.")]
static string EscribirArchivo(
    [Description("Ruta del archivo a escribir.")] string ruta,
    [Description("Contenido de texto a guardar.")] string contenido) {
    File.WriteAllText(ruta, contenido);
    return $"Correcto - Ruta : {ruta}";
}

[Description ("Listar archivos de una carpeta y que tiene")]
static string Listar(
    [Description ("Ruta del directorio")] string ruta = "")
{
    var elementos = Directory.GetFileSystemEntries(ruta);

    return $"Elementos: {string.Join("\n", elementos)}";
};

// -----------------------------CERRAR APP------------------------.
var dialogosalir = new Dialog { X = Pos.Center(), Y = Pos.Center(), Width = 50, Height = 10 };

var seguro = new Label { Text = "", X = Pos.Center(), Y = Pos.Center() };
var confirmar = new Button { IsDefault = true };
var cancelar = new Button { Text = "Cancelar" };

dialogosalir.Border.LineStyle = LineStyle.Rounded;
dialogosalir.Border.Thickness = new Thickness(1);
dialogosalir.Add(seguro);
dialogosalir.AddButton(confirmar);
dialogosalir.AddButton(cancelar);

gui.KeyDown += async (sender, e) => {
    if (e.KeyCode == Key.Esc) {
        seguro.Text = " ¿Seguro desea salir? ";
        confirmar.Title = "Confirmar";
        cancelar.Title = "Cancelar";
        e.Handled = true;
        app.Run(dialogosalir);
        cancelar.Accepting += (_, e) => {
            app!.RequestStop();
            e.Handled = true;
        };
        confirmar.Accepting += (_, e) => {
            app.RequestStop();
            cerrar = true;
        };
        if (cerrar) app.RequestStop();
    }
};


gui.Add(ventana, entrada);
app.Run(gui);
