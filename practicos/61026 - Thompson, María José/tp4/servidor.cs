#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt =>
    opt.UseSqlite("Data Source=catalogo.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();
}

app.MapGet("/productos", async (CatalogoDb db) =>
{
    return await db.Productos.ToListAsync();
});

app.MapGet("/productos/{id:int}", async (int id, CatalogoDb db) =>
{
    var producto = await db.Productos.FindAsync(id);

    return producto is null
        ? Results.NotFound()
        : Results.Ok(producto);
});

app.MapPost("/productos", async (Producto producto, CatalogoDb db) =>
{
    db.Productos.Add(producto);
    await db.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id:int}", async (
    int id,
    Producto datos,
    CatalogoDb db) =>
{
    var producto = await db.Productos.FindAsync(id);

    if (producto is null)
        return Results.NotFound();

    producto.Codigo = datos.Codigo;
    producto.Nombre = datos.Nombre;
    producto.Precio = datos.Precio;
    producto.Stock = datos.Stock;

    await db.SaveChangesAsync();

    return Results.Ok(producto);
});

app.MapDelete("/productos/{id:int}", async (
    int id,
    CatalogoDb db) =>
{
    var producto = await db.Productos.FindAsync(id);

    if (producto is null)
        return Results.NotFound();

    db.Productos.Remove(producto);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/productos/{productoId:int}/movimientos",
async (int productoId, CatalogoDb db) =>
{
    var movimientos = await db.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToListAsync();

    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos",
async (
    int productoId,
    MovimientoRequest request,
    CatalogoDb db) =>
{
    var producto = await db.Productos.FindAsync(productoId);

    if (producto is null)
        return Results.NotFound();

    switch (request.Tipo)
    {
        case TipoMovimiento.Compra:
            producto.Stock += request.Cantidad;
            break;

        case TipoMovimiento.Venta:
            producto.Stock -= request.Cantidad;
            break;

        case TipoMovimiento.Ajuste:
            producto.Stock = request.Cantidad;
            break;
    }

    var movimiento = new MovimientoDeProducto
    {
        ProductoId = productoId,
        Tipo = request.Tipo,
        Cantidad = request.Cantidad,
        Fecha = DateTime.Now
    };

    db.Movimientos.Add(movimiento);

    await db.SaveChangesAsync();

    return Results.Ok(movimiento);
});

app.Run();

public class Producto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public List<MovimientoDeProducto> Movimientos { get; set; } = new();
}

public class MovimientoDeProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    public Producto? Producto { get; set; }
}

public enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

public class MovimientoRequest
{
    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }
}

public class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options)
        : base(options)
    {
    }

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<MovimientoDeProducto>()
            .HasOne(m => m.Producto)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.ProductoId);
    }
}