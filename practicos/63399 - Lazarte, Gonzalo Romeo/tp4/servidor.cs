using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDbContext>(options =>
{
options.UseSqlite("Data Source=catalogo.db");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
db.Database.EnsureCreated();
}

app.MapGet("/productos", async (CatalogoDbContext db) =>
{
return await db.Productos
.OrderBy(p => p.Codigo)
.ToListAsync();
});

app.MapGet("/productos/{id:int}", async (int id, CatalogoDbContext db) =>
{
var producto = await db.Productos.FindAsync(id);

return producto is null
    ? Results.NotFound()
    : Results.Ok(producto);

});

app.MapPost("/productos", async (Producto producto, CatalogoDbContext db) =>
{
bool existeCodigo = await db.Productos
.AnyAsync(p => p.Codigo == producto.Codigo);

if (existeCodigo)
    return Results.BadRequest("Ya existe un producto con ese código.");

db.Productos.Add(producto);
await db.SaveChangesAsync();

return Results.Created($"/productos/{producto.Id}", producto);

});

app.MapPut("/productos/{id:int}", async (
int id,
Producto actualizado,
CatalogoDbContext db) =>
{
var producto = await db.Productos.FindAsync(id);

if (producto is null)
    return Results.NotFound();

bool codigoDuplicado = await db.Productos.AnyAsync(p =>
    p.Id != id &&
    p.Codigo == actualizado.Codigo);

if (codigoDuplicado)
    return Results.BadRequest("Ya existe un producto con ese código.");

producto.Codigo = actualizado.Codigo;
producto.Nombre = actualizado.Nombre;
producto.Precio = actualizado.Precio;

await db.SaveChangesAsync();

return Results.Ok(producto);

});

app.MapDelete("/productos/{id:int}", async (
int id,
CatalogoDbContext db) =>
{
var producto = await db.Productos.FindAsync(id);

if (producto is null)
    return Results.NotFound();

db.Productos.Remove(producto);
await db.SaveChangesAsync();

return Results.NoContent();

});


app.MapGet("/productos/{productoId:int}/movimientos",
async (int productoId, CatalogoDbContext db) =>
{
bool existe = await db.Productos.AnyAsync(p => p.Id == productoId);

if (!existe)
    return Results.NotFound();

var movimientos = await db.Movimientos
    .Where(m => m.ProductoId == productoId)
    .OrderByDescending(m => m.Fecha)
    .ToListAsync();

return Results.Ok(movimientos);

});

app.MapPost("/productos/{productoId:int}/movimientos",
async (
int productoId,
MovimientoInput input,
CatalogoDbContext db) =>
{
var producto = await db.Productos.FindAsync(productoId);

if (producto is null)
    return Results.NotFound();

if (input.Cantidad <= 0)
    return Results.BadRequest("Cantidad inválida.");

switch (input.Tipo)
{
    case TipoMovimiento.Compra:
        producto.Stock += input.Cantidad;
        break;

    case TipoMovimiento.Venta:

        if (producto.Stock < input.Cantidad)
            return Results.BadRequest("Stock insuficiente.");

        producto.Stock -= input.Cantidad;
        break;

    case TipoMovimiento.Ajuste:
        producto.Stock = input.Cantidad;
        break;
}

MovimientoDeProducto movimiento = new()
{
    ProductoId = productoId,
    Tipo = input.Tipo,
    Cantidad = input.Cantidad,
    Fecha = DateTime.Now
};

db.Movimientos.Add(movimiento);

await db.SaveChangesAsync();

return Results.Created(
    $"/productos/{productoId}/movimientos/{movimiento.Id}",
    movimiento);

});

app.Run("http://localhost:5000");

public enum TipoMovimiento
{
Compra = 1,
Venta = 2,
Ajuste = 3
}

public class Producto
{
public int Id { get; set; }

public string Codigo { get; set; } = "";

public string Nombre { get; set; } = "";

public decimal Precio { get; set; }

public int Stock { get; set; }

public List<MovimientoDeProducto> Movimientos { get; set; } = [];

}

public class MovimientoDeProducto
{
public int Id { get; set; }

public int ProductoId { get; set; }

public Producto? Producto { get; set; }

public TipoMovimiento Tipo { get; set; }

public int Cantidad { get; set; }

public DateTime Fecha { get; set; }

}

public class MovimientoInput
{
public TipoMovimiento Tipo { get; set; }

public int Cantidad { get; set; }

}

public class CatalogoDbContext : DbContext
{
public CatalogoDbContext(DbContextOptions<CatalogoDbContext> options)
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

    modelBuilder.Entity<Producto>()
        .Property(p => p.Precio)
        .HasPrecision(18, 2);

    modelBuilder.Entity<MovimientoDeProducto>()
        .HasOne(m => m.Producto)
        .WithMany(p => p.Movimientos)
        .HasForeignKey(m => m.ProductoId)
        .OnDelete(DeleteBehavior.Cascade);
}

}
