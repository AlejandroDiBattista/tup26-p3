#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@10.0.8
#:property PublishAot=false

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoContexto>(opciones =>
    opciones.UseSqlite("Data Source=catalogo.db"));
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var contexto = scope.ServiceProvider.GetRequiredService<CatalogoContexto>();
    contexto.Database.EnsureCreated();
}

// Endpoints para administrar productos del catalogo.
app.MapGet("/productos", async (CatalogoContexto contexto) =>
    await contexto.Productos.OrderBy(p => p.Codigo).ToListAsync());

app.MapGet("/productos/{id:int}", async (int id, CatalogoContexto contexto) =>
{
    var producto = await contexto.Productos.FindAsync(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapPost("/productos", async (ProductoEntrada entrada, CatalogoContexto contexto) =>
{
    if (string.IsNullOrWhiteSpace(entrada.Codigo) || string.IsNullOrWhiteSpace(entrada.Nombre))
    {
        return Results.BadRequest("El codigo y el nombre son obligatorios.");
    }

    var codigo = entrada.Codigo.Trim();
    var existeCodigo = await contexto.Productos.AnyAsync(p => p.Codigo == codigo);
    if (existeCodigo)
    {
        return Results.BadRequest("Ya existe un producto con ese codigo.");
    }

    var producto = new Producto
    {
        Codigo = codigo,
        Nombre = entrada.Nombre.Trim(),
        Precio = entrada.Precio,
        Stock = entrada.Stock
    };

    contexto.Productos.Add(producto);
    await contexto.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id:int}", async (int id, ProductoEntrada entrada, CatalogoContexto contexto) =>
{
    var producto = await contexto.Productos.FindAsync(id);
    if (producto is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(entrada.Codigo) || string.IsNullOrWhiteSpace(entrada.Nombre))
    {
        return Results.BadRequest("El codigo y el nombre son obligatorios.");
    }

    var codigo = entrada.Codigo.Trim();
    var existeCodigo = await contexto.Productos.AnyAsync(p => p.Id != id && p.Codigo == codigo);
    if (existeCodigo)
    {
        return Results.BadRequest("Ya existe un producto con ese codigo.");
    }

    producto.Codigo = codigo;
    producto.Nombre = entrada.Nombre.Trim();
    producto.Precio = entrada.Precio;
    producto.Stock = entrada.Stock;

    await contexto.SaveChangesAsync();
    return Results.Ok(producto);
});

app.MapDelete("/productos/{id:int}", async (int id, CatalogoContexto contexto) =>
{
    var producto = await contexto.Productos.FindAsync(id);
    if (producto is null)
    {
        return Results.NotFound();
    }

    contexto.Productos.Remove(producto);
    await contexto.SaveChangesAsync();
    return Results.NoContent();
});

// Endpoints para consultar y registrar movimientos de stock.
app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, CatalogoContexto contexto) =>
{
    var existeProducto = await contexto.Productos.AnyAsync(p => p.Id == productoId);
    if (!existeProducto)
    {
        return Results.NotFound();
    }

    var movimientos = await contexto.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToListAsync();

    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos", async (
    int productoId,
    MovimientoEntrada entrada,
    CatalogoContexto contexto) =>
{
    if (entrada.Cantidad <= 0)
    {
        return Results.BadRequest("La cantidad debe ser positiva.");
    }

    var producto = await contexto.Productos.FindAsync(productoId);
    if (producto is null)
    {
        return Results.NotFound();
    }

    if (entrada.Tipo == TipoMovimiento.Venta && producto.Stock < entrada.Cantidad)
    {
        return Results.BadRequest("No hay stock suficiente para la venta.");
    }

    producto.Stock = entrada.Tipo switch
    {
        TipoMovimiento.Compra => producto.Stock + entrada.Cantidad,
        TipoMovimiento.Venta => producto.Stock - entrada.Cantidad,
        TipoMovimiento.Ajuste => entrada.Cantidad,
        _ => producto.Stock
    };

    var movimiento = new MovimientoDeProducto
    {
        ProductoId = producto.Id,
        Tipo = entrada.Tipo,
        Cantidad = entrada.Cantidad,
        Fecha = DateTime.Now
    };

    contexto.Movimientos.Add(movimiento);
    await contexto.SaveChangesAsync();

    return Results.Created($"/productos/{productoId}/movimientos", movimiento);
});

app.Run();

public class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
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

public enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

public record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock);

public record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);

public class CatalogoContexto : DbContext
{
    public CatalogoContexto(DbContextOptions<CatalogoContexto> opciones) : base(opciones)
    {
    }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<Producto>()
            .Property(p => p.Precio)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<MovimientoDeProducto>()
            .HasOne<Producto>()
            .WithMany()
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
