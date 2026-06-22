#!/usr/bin/env -S dotnet run
#:package DotNetEnv@*
#:package Microsoft.Extensions.AI.OpenAI@10.7.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using Terminal.Gui.Drawing;
using Terminal.Gui.Configuration;
DotNetEnv.Env.Load();

// ------------------config -------------------------------------
bool cerrar = false;
var url    = Environment.GetEnvironmentVariable("GEMINI_API_URL");
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var modelo = Environment.GetEnvironmentVariable("GEMINI_MODEL");

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(apiKey ?? ""),
        new OpenAIClientOptions { Endpoint = new Uri(url ?? "") })
    .GetChatClient(modelo)
    .AsIChatClient();
// ---------------------------------------------------------------

ConfigurationManager.Enable(ConfigLocations.All);
ConfigurationManager.Apply();

Scheme entrad = new Scheme()
{
    Normal = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.Gray)
};

using Window gui = new () { };

/*

GUI =>
=========>Ventana principal (85%)
                ==>El markdown de respuesta y la entrada.text)
=========>entrada de texto (10%) 
            ==> textfield 

*/
using IApplication app = Application.Create().Init();
using var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    Width = Dim.Fill(), Height = Dim.Percent(90),
    SchemeName = "entrad"
};

var entrada = new TextField {X = 0, 
Y = Pos.Bottom(ventana), Width = Dim.Fill(), Text = "",
Height = Dim.Fill()
};

List<ChatMessage> mensajes = [
    new(ChatRole.System, File.ReadAllText("AGENTS.md")),
    new(ChatRole.User, entrada.Text)];

var respuesta = await chat.GetResponseAsync(mensajes);


ventana.Add(new Markdown {
    Text = $"""
    # ————————————————————— YO ———————————————————————————————————————
    {entrada.Text}
    # —————————————————————🤖 ASISTENTE————————————————————————————————
    {respuesta.Text}
    """,
    Width = Dim.Fill(), Height = Dim.Fill()
});

// Panel de conversación y el panel de entrada.

// // CERRAR APP.
var dialogosalir = new Dialog {X = Pos.Center(), Y = Pos.Center(), Width = 50, Height = 10};

var seguro = new Label{Text = "", X = Pos.Center(), Y = Pos.Center()};
var confirmar = new Button { IsDefault = true };
var cancelar = new Button { Text = "Cancelar" };

dialogosalir.Border.LineStyle = LineStyle.Rounded;
dialogosalir.Border.Thickness = new Thickness(1);
dialogosalir.Add(seguro);
dialogosalir.AddButton(confirmar);
dialogosalir.AddButton(cancelar);

// Funciones de Terminal Gui 

//para salir
gui.KeyDown += async (sender, e) =>
{
    if (e.KeyCode == Key.Esc)
    {
        seguro.Text = " ¿Seguro desea salir? ";
        confirmar.Title = "Confirmar";
        cancelar.Title = "Cancelar";
        e.Handled = true;
        app.Run(dialogosalir);
        cancelar.Accepting += (_, e) =>
{
    app!.RequestStop();
    e.Handled = true;
};
        confirmar.Accepting += (_, e) =>
    {
        app.RequestStop();
        cerrar = true;
    };
        if (cerrar) app.RequestStop();
    }
};

// // TODO: enviar mensajes con 'chat' y conservarlos en 'mensajes'.
// // TODO: mostrar la respuesta con chat.GetStreamingResponseAsync(mensajes).

gui.Add(ventana, entrada);
app.Run(gui);
