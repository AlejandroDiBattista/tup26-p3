#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false

#:package Microsoft.EntityFrameworkCore.Sqlite@10.0.0

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

string databasePath = args.Length > 0 ? args[0] : "catalogo.db";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddDbContext<CatalogoContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

    WebApplication app = builder.Build();

    using (IServiceScope scope = app.Services.CreateScope()) {
        CatalogoDbContext db = scope.ServiceProvider.GetRequiredService<CatalogoContext>();
        db.Database.EnsureCreated();
    }

    app.MapGet("/", () => Results.Ok(new {
    Aplicacion = "Catalogo de productos",
    Endpoints = new[] {
        "GET /productos",
        "GET /productos/{id}",
        "POST /productos",
        "PUT /productos/{id}",
        "DELETE /productos/{id}",
        "GET /productos/{productoId}/movimientos",
        "POST /productos/{productoId}/movimientos"
    }
}));
