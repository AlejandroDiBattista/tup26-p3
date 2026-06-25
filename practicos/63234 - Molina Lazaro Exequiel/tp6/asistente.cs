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