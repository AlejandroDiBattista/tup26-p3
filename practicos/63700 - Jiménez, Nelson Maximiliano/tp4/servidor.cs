using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();
}


app.MapGet("/productos", async (CatalogoDb db) =>
    await db.Productos.ToListAsync());

app.MapGet("/productos/{id}", async (int id, CatalogoDb db) =>
    await db.Productos.FindAsync(id) is Producto p ? Results.Ok(p) : Results.NotFound());

app.MapPost("/productos", async (Producto p, CatalogoDb db) =>
{
    db.Productos.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{p.Id}", p);
});

app.MapPut("/productos/{id}", async (int id, Producto p, CatalogoDb db) =>
{
    var prod = await db.Productos.FindAsync(id);
    if (prod is null) return Results.NotFound();

    prod.Codigo = p.Codigo;
    prod.Nombre = p.Nombre;
    prod.Precio = p.Precio;

    
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/productos/{id}", async (int id, CatalogoDb db) =>
{
    var prod = await db.Productos.FindAsync(id);
    if (prod is null) return Results.NotFound();

    db.Productos.Remove(prod);
    await db.SaveChangesAsync();
    return Results.NoContent();
});



app.MapGet("/productos/{id}/movimientos", async (int id, CatalogoDb db) =>
    await db.Movimientos.Where(m => m.ProductoId == id).OrderByDescending(m => m.Fecha).ToListAsync());

app.MapPost("/productos/{id}/movimientos", async (int id, MovimientoDeProducto m, CatalogoDb db) =>
{
    var prod = await db.Productos.FindAsync(id);
    if (prod is null) return Results.NotFound();

    m.ProductoId = id;
    m.Fecha = DateTime.Now;
    m.Cantidad = Math.Abs(m.Cantidad);
    switch (m.Tipo)
    {
        case TipoMovimiento.Compra:
            prod.Stock += m.Cantidad;
            break;
        case TipoMovimiento.Venta:
            prod.Stock -= m.Cantidad;
            break;
        case TipoMovimiento.Ajuste:
            prod.Stock = m.Cantidad;
            break;
    }

    db.Movimientos.Add(m);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{id}/movimientos/{m.Id}", m);
});

app.Run("http://localhost:5000");


public class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

public class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

public class MovimientoDeProducto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

public enum TipoMovimiento { Compra, Venta, Ajuste }