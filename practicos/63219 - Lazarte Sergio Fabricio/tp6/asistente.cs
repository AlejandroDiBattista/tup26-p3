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
using System.Text;
using System.Drawing;
using System.ComponentModel;

bool EsValorValido(string? valor)
{
    if (string.IsNullOrWhiteSpace(valor)) return false;
    var v = valor.Trim();
    return !(v.StartsWith('<') && v.EndsWith('>'))
        && !v.Contains("tu_clave_api_aqui", StringComparison.OrdinalIgnoreCase);
}

string LeerVariableObligatoria(string clave)
{
    var valor = Environment.GetEnvironmentVariable(clave);
    if (!EsValorValido(valor))
        throw new InvalidOperationException($"Falta configurar {clave} en .env.");
    return valor!;
}

string ResolverProveedorPredeterminado()
{
    var lista = new[] { "OPENAI", "GROQ", "GEMINI", "OPENROUTER", "FIREWORK", "GROK", "HHGG", "OLLAMA" };
    return lista.FirstOrDefault(p => EsValorValido(Environment.GetEnvironmentVariable($"{p}_API_KEY")))
        ?? "OPENAI";
}

ConfiguracionServicio InicializarConfiguracion(string[] argumentos)
{
    var proveedor = (argumentos.Length > 0
            ? argumentos[0]
            : Environment.GetEnvironmentVariable("ASISTENTE_PROVIDER") ?? ResolverProveedorPredeterminado())
        .Trim().ToUpperInvariant();

    var urlBase = LeerVariableObligatoria($"{proveedor}_API_URL");
    var clave = Environment.GetEnvironmentVariable($"{proveedor}_API_KEY");
    var modelo = Environment.GetEnvironmentVariable($"{proveedor}_MODEL") ?? "gpt-4o-mini";

    if (proveedor == "GROQ")
        clave = string.IsNullOrWhiteSpace(clave) ? "groq" : clave;
    else if (!EsValorValido(clave))
        throw new InvalidOperationException(
            $"Falta configurar {proveedor}_API_KEY en .env. " +
            "Pegá la clave sin los signos < >, por ejemplo: GROQ_API_KEY=gsk_...");

    return new ConfiguracionServicio(proveedor, urlBase, clave!, modelo);
}

Uri PrepararEndpoint(string endpoint)
{
    var url = endpoint.TrimEnd('/');
    if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        url = url[..^"/chat/completions".Length];
    return new Uri(url);
}

record InteraccionUI(string Autor, string Texto);
record ConfiguracionServicio(string Proveedor, string Url, string ApiKey, string Modelo);