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

var proveedor = (args.Length > 0 ? args[0] : "groq").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "qwen/qwen3-32b";

if(url == null){
    Console.WriteLine("falta la API url");
    return; }

IChatClient chat = new OpenAIClient(
new ApiKeyCredential(
 apiKey ?? "no-key"),

  new OpenAIClientOptions {
    Endpoint = new Uri(url)
  }
  )
.GetChatClient(modelo)
.AsIChatClient();

var mensajes = new List<ChatMessage> {
  new(ChatRole.System , "responde en español.")
};

using IApplication app = Application.Create().Init();
using var ventana = new Window { 

 Title = $" Asistente IA · {modelo} ", Width = Dim.Fill(), Height = Dim.Fill()
};

var chatBox = new TextView { Width = Dim.Fill(), Height = Dim.Fill(3), ReadOnly = true, WordWrap = true,
Text = """ASISTENTE GROQ"""
};

var entrada = new TextField {
    Y = Pos.Bottom(chatBox), 
    Width = Dim.Fill(10)
};

var boton = new Button {
    X = Pos.Right(entrada),
    Y = Pos.Bottom(chatBox),
    Text = "enviar"
};

ventana.Add(chatBox,entrada,boton);

void Agregar(string texto)
{ Application.Invoke(() =>
    {chatBox.Text += texto;
    chatBox.MoveEnd();
});
}
bool ocupado = false;

async Task Enviar(){
    if(ocupado)
    return;

var texto = entrada.Text?.Trim();

if(string.IsNullOrWhiteSpace(texto))

return;

ocupado = true;
entrada.Text="";


Agregar(
$"Vos:\n{texto}\n\n"
);

}




