#:sdk Microsoft.NET.Sdk.Web

#:package Microsoft.AspNetCore.OpenApi@*
#:package Scalar.AspNetCore@*

#:property PublishAot=false         

using Scalar.AspNetCore;

const string host = "http://localhost:5001";

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(host);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();               // http://localhost:5001/openapi/v1.json
app.MapScalarApiReference();    // http://localhost:5001/scalar

int contador = 0;

app.MapGet("/contador", () => Results.Json(new { Contador = contador }));

app.MapPost("/contador", () => {
    contador++;
    return Results.Ok();
});

app.MapDelete("/contador", () => {
    contador = 0;
    return Results.Ok();
});

Console.Clear();
Console.WriteLine("=== Servidor de Contador (C#) ===\n");
app.Run();

