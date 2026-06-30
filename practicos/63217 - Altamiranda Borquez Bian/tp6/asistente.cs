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
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

DotNetEnv.Env.Load();
if (args.Contains("Validar")) {
    Console.WriteLine("Validacion de compilscion Ok");
    return;
}
var proveedor = (args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "gemini").ToUpperInvariant();
var url    = Environment.GetEnvironmentVariable($"{proveedor}_API_URL");
var apiKey = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gemini-2.5-flash";

if(string.IsNullOrWhiteSpace(url)) {
    Console.WriteLine($"Falta configurar {proveedor}_API_URL en el archivo .env.");
    return;
}
if(string.IsNullOrWhiteSpace(apiKey)&& proveedor != "OLLAMA") {
    Console.WriteLine($"Falta configurar {proveedor}_API_KEY en el archivo .env.");
    return;
}
var rutaProducto= Directory.GetCurrentDirectory();
var endpoint= NormalizarEndpoint(url);



