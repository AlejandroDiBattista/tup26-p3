#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(opt => {
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));

var app = builder.Build();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();

    if (!db.Productos.Any()) {
        db.Productos.AddRange(
            new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
            new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 900m, Stock = 60 },
            new Producto { Codigo = "P003", Nombre = "Cafe molido 250g", Precio = 2500m, Stock = 35 }
        );
        db.SaveChanges();
    }
}
app.MapGet("/productos", async (CatalogoDb db) =>
    await db.Productos
        .OrderBy(p => p.Codigo)
        .Select(p => ProductoDto.DesdeModelo(p))
        .ToListAsync()
);
app.MapGet("/productos/{id:int}", async (int id, CatalogoDb db) => {
    var producto = await db.Productos.FindAsync(id);
    return producto is null
        ? Results.NotFound($"No existe un producto con id {id}.")
        : Results.Ok(ProductoDto.DesdeModelo(producto));
});
app.MapPost("/productos", async (ProductoCrearDto dto, CatalogoDb db) => {
    var error = ValidarProducto(dto.Codigo, dto.Nombre, dto.Precio, dto.Stock);
    if (error is not null) return Results.BadRequest(error);

    var codigo = dto.Codigo.Trim().ToUpperInvariant();
    var existeCodigo = await db.Productos.AnyAsync(p => p.Codigo == codigo);
    if (existeCodigo) return Results.Conflict($"Ya existe un producto con codigo {codigo}.");

    var producto = new Producto {
        Codigo = codigo,
        Nombre = dto.Nombre.Trim(),
        Precio = dto.Precio,
        Stock = dto.Stock
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}", ProductoDto.DesdeModelo(producto));
});