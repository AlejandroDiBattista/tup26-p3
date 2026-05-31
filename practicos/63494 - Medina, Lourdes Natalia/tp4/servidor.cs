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

app.MapGet("/productos", async (CatalogoContext db) => 
await db.Productos
.AsNoTracking()
        .OrderBy(p => p.Codigo)
        .ToListAsync());

app.MapGet("/productos/{id:int}", async (int id, CatalogoDbContext db) => {
    Producto? producto = await db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapPost("/productos", async (ProductoRequest request, CatalogoDbContext db) => {
    string? error = await ValidateProduct(request, db);
    if (error is not null) {
        return Results.BadRequest(new { Error = error });
    }

    Producto producto = new() {
        Codigo = request.Codigo.Trim(),
        Nombre = request.Nombre.Trim(),
        Precio = request.Precio,
        Stock = request.Stock
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id:int}", async (int id, ProductoRequest request, CatalogoDbContext db) => {
    Producto? producto = await db.Productos.FindAsync(id);
    if (producto is null) {
        return Results.NotFound();
    }

    string? error = await ValidateProduct(request, db, id);
    if (error is not null) {
        return Results.BadRequest(new { Error = error });
    }

    producto.Codigo = request.Codigo.Trim();
    producto.Nombre = request.Nombre.Trim();
    producto.Precio = request.Precio;
    producto.Stock = request.Stock;

    await db.SaveChangesAsync();
    return Results.Ok(producto);
});

app.MapDelete("/productos/{id:int}", async (int id, CatalogoDbContext db) => {
    Producto? producto = await db.Productos.FindAsync(id);
    if (producto is null) {
        return Results.NotFound();
    }

    db.Productos.Remove(producto);
    await db.SaveChangesAsync();
    return Results.NoContent();
});