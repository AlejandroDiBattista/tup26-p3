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
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();

var proveedor = (args.Length > 0 ? args[0] : "openai").ToUpperInvariant();
var url = Environment.GetEnvironmentVariable($"{proveedor}_API_URL") ?? "https://api.openai.com/v1";
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-5.4-mini";
var errorConfiguracion = ValidarConfiguracion(proveedor, url, apiKey, modelo);


IChatClient chat = new ChatClientBuilder(
        new OpenAIClient(
                new ApiKeyCredential(apiKey ?? "no-requiere-key"),
                new OpenAIClientOptions { Endpoint = new Uri(url) })
            .GetChatClient(modelo)
            .AsIChatClient())
    .UseFunctionInvocation()
    .Build();
