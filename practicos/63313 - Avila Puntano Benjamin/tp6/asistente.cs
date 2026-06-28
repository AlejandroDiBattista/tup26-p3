#!/usr/bin/env -S dotnet run

#:package DotNetEnv@*
#:package Microsoft.Extensions.AI@10.4.0
#:package Microsoft.Extensions.AI.OpenAI@10.4.0
#:package Terminal.Gui@2.4.3
#:property PublishAot=false

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "groq").ToUpperInvariant();

var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen/qwen3.6-27b";

if(url == null){
    Console.WriteLine("falta la API url");
    return;
}

IChatClient chat = new OpenAIClient(
    new ApiKeyCredential(apiKey ?? "no-key"),
    new OpenAIClientOptions {
        Endpoint = new Uri(url)
    })
    .GetChatClient(modelo)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();


var herramientas = new List<AITool>
{
    AIFunctionFactory.Create(
        (string ruta) => File.ReadAllText(ruta),
        "leer-archivo",
        "Devuelve el contenido de un archivo de texto"
    ),
    AIFunctionFactory.Create(
        (string ruta, string contenido) =>
        {
            File.WriteAllText(ruta, contenido);
            return "ok";
        },
        "escribir-archivo",
        "Crea o sobrescribe un archivo con el contenido indicado"
    ),
    AIFunctionFactory.Create(
        (string ruta) => string.Join("\n", Directory.GetFileSystemEntries(ruta)),
        "listar-archivos",
        "Lista los archivos y carpetas de un directorio"
    )
};



var opciones = new ChatOptions {
    Tools = herramientas
};


var sistemaPrompt = File.Exists("AGENTS.md")
    ? File.ReadAllText("AGENTS.md")
    : "responde en español.";


var mensajes = new List<ChatMessage>
{
    new(ChatRole.System , sistemaPrompt)
};


var logFile = File.AppendText("chat.log");


void Log(string texto) {
    logFile.WriteLine(texto);
    logFile.Flush();
}


using IApplication app = Application.Create().Init();


var ventana = new Window {
    Title = $" Asistente IA · {modelo} ",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};


var chatBox = new TextView {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
    ReadOnly = true,
    WordWrap = true,
    Text = "🤖 Asistente groq \n\n"
};


var entrada = new TextField {
    X = 0,
    Y = Pos.Bottom(chatBox),
    Width = Dim.Fill(10)
};


var boton = new Button {
    X = Pos.Right(entrada),
    Y = Pos.Bottom(chatBox),
    Text = "Enviar"
};


ventana.Add(chatBox, entrada, boton);


void Agregar(string texto)
{
    chatBox.Text += RenderMarkdown(texto);
    chatBox.MoveEnd();
    chatBox.SetNeedsDraw();
}


string RenderMarkdown(string texto)
{
    var resultado = texto;
    resultado = resultado
    .Replace("```csharp", "")
    .Replace("```cs", "")
    .Replace("```C#", "")
    .Replace("```", "")
    .Replace("csharp", "")
    .Replace("**", "")
    .Replace("- ", "• ");
    return resultado;
}
bool ocupado = false;


async Task Enviar()
{
    if(ocupado)
        return;


    var texto = entrada.Text?.Trim();


    if(string.IsNullOrWhiteSpace(texto))
        return;


    ocupado = true;

    entrada.Enabled = false;
    boton.Enabled = false;


    entrada.Text = "";


    Agregar($"👤 Vos:\n{texto}\n\n");


    Log($"Vos: {texto}");


    mensajes.Add(
        new ChatMessage(
            ChatRole.User,
            texto
        )
    );


    Agregar("🤖 Asistente groq:\n");


    try {

        var textoCompleto = new System.Text.StringBuilder();


        await foreach (
            var fragmento in chat.GetStreamingResponseAsync(mensajes, opciones)
        ) {

            var parte = fragmento.Text ?? "";


            Agregar(parte);


            textoCompleto.Append(parte);

        }


        Agregar("\n\n");


        Log($"IA: {textoCompleto}");


        mensajes.Add(
            new ChatMessage(
                ChatRole.Assistant,
                textoCompleto.ToString()
            )
        );

    }
    catch(Exception e) {

        Agregar($"error: {e.Message}");

        Log($"error: {e.Message}");
    }


    ocupado = false;

    entrada.Enabled = true;
    boton.Enabled = true;
}



boton.Accepting += (s, e) => {

    _ = Enviar();

};


entrada.KeyDown += (s, e) => {

    if(e == Key.Enter)
        _ = Enviar();


    if(e == Key.Esc)
        Application.RequestStop();

};


app.Run(ventana);


logFile.Close();