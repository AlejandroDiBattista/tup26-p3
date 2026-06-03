#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@9.0.0
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BaseDeDatos>(op => op.UseSqlite("Data Source=catalogo.db"));

var app = builder.Build();

// crear tablas si no existen todavia
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<BaseDeDatos>().Database.EnsureCreated();

app.MapGet("/productos", async (BaseDeDatos db) =>
    await db.Productos.ToListAsync());

app.MapGet("/productos/{id}", async (int id, BaseDeDatos db) =>
{
    var producto = await db.Productos.FindAsync(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapPost("/productos", async (Producto nuevo, BaseDeDatos db) =>
{
    // no permitir codigos duplicados
    if (await db.Productos.AnyAsync(p => p.Codigo == nuevo.Codigo))
        return Results.BadRequest("Ya existe un producto con ese código.");

    db.Productos.Add(nuevo);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{nuevo.Id}", nuevo);
});

app.MapPut("/productos/{id}", async (int id, Producto datos, BaseDeDatos db) =>
{
    var producto = await db.Productos.FindAsync(id);
    if (producto is null) return Results.NotFound();

    if (await db.Productos.AnyAsync(p => p.Codigo == datos.Codigo && p.Id != id))
        return Results.BadRequest("Ya existe otro producto con ese código.");

    producto.Codigo = datos.Codigo;
    producto.Nombre = datos.Nombre;
    producto.Precio = datos.Precio;
    producto.Stock = datos.Stock;

    await db.SaveChangesAsync();
    return Results.Ok(producto);
});

app.MapDelete("/productos/{id}", async (int id, BaseDeDatos db) =>
{
    var producto = await db.Productos.FindAsync(id);
    if (producto is null) return Results.NotFound();

    db.Productos.Remove(producto);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/productos/{productoId}/movimientos", async (int productoId, BaseDeDatos db) =>
{
    if (!await db.Productos.AnyAsync(p => p.Id == productoId))
        return Results.NotFound();

    var movimientos = await db.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToListAsync();

    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId}/movimientos", async (int productoId, Movimiento mov, BaseDeDatos db) =>
{
    var producto = await db.Productos.FindAsync(productoId);
    if (producto is null) return Results.NotFound();

    if (mov.Cantidad <= 0)
        return Results.BadRequest("La cantidad debe ser mayor a cero.");

    switch (mov.Tipo)
    {
        case TipoMovimiento.Compra:
            producto.Stock += mov.Cantidad;
            break;
        case TipoMovimiento.Venta:
            if (producto.Stock < mov.Cantidad)
                return Results.BadRequest("Stock insuficiente para realizar la venta.");
            producto.Stock -= mov.Cantidad;
            break;
        case TipoMovimiento.Ajuste:
            producto.Stock = mov.Cantidad;
            break;
    }

    mov.ProductoId = productoId;
    mov.Fecha = DateTime.Now;

    db.Movimientos.Add(mov);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{productoId}/movimientos/{mov.Id}", mov);
});

app.Run("http://localhost:5050");

// modelos

enum TipoMovimiento { Compra, Venta, Ajuste }

class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

class Movimiento
{
    public int Id { get; set; }
    public int ProductoId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public Producto? Producto { get; set; }
}

class BaseDeDatos : DbContext
{
    public BaseDeDatos(DbContextOptions<BaseDeDatos> opciones) : base(opciones) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
}
