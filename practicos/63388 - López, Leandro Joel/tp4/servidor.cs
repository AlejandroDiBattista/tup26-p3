#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dbPath = Path.Combine(Environment.CurrentDirectory, "catalogo.db");

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<CatalogoDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "Movimientos" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Movimientos" PRIMARY KEY AUTOINCREMENT,
            "ProductoId" INTEGER NOT NULL,
            "Tipo" TEXT NOT NULL,
            "Cantidad" INTEGER NOT NULL,
            "Fecha" TEXT NOT NULL,
            CONSTRAINT "FK_Movimientos_Productos_ProductoId" FOREIGN KEY ("ProductoId") REFERENCES "Productos" ("Id") ON DELETE CASCADE
        );
        """);
    db.Database.ExecuteSqlRaw("""
        CREATE INDEX IF NOT EXISTS "IX_Movimientos_ProductoId" ON "Movimientos" ("ProductoId");
        """);
}

app.MapGet("/", () => Results.Ok(new
{
    Aplicacion = "Catalogo de Productos",
    Endpoints = new[]
    {
        "GET /productos",
        "GET /productos/{id}",
        "POST /productos",
        "PUT /productos/{id}",
        "DELETE /productos/{id}",
        "GET /productos/{productoId}/movimientos",
        "POST /productos/{productoId}/movimientos"
    }
}));

app.MapGet("/productos", async (CatalogoDbContext db) =>
{
    var productos = await db.Productos
        .AsNoTracking()
        .OrderBy(producto => producto.Codigo)
        .Select(producto => producto.ToDto())
        .ToListAsync();

    return Results.Ok(productos);
});

app.MapGet("/productos/{id:int}", async (int id, CatalogoDbContext db) =>
{
    var producto = await db.Productos
        .AsNoTracking()
        .FirstOrDefaultAsync(producto => producto.Id == id);

    return producto is null
        ? Results.NotFound(new ApiError("No existe un producto con ese ID."))
        : Results.Ok(producto.ToDto());
});

app.MapPost("/productos", async (ProductoRequest request, CatalogoDbContext db) =>
{
    var error = ValidarProducto(request);
    if (error is not null)
    {
        return Results.BadRequest(new ApiError(error));
    }

    var codigo = NormalizarCodigo(request.Codigo);
    var existeCodigo = await db.Productos.AnyAsync(producto => producto.Codigo == codigo);
    if (existeCodigo)
    {
        return Results.Conflict(new ApiError("Ya existe un producto con ese codigo."));
    }

    var producto = new Producto
    {
        Codigo = codigo,
        Nombre = request.Nombre.Trim(),
        Precio = request.Precio,
        Stock = request.Stock
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}", producto.ToDto());
});

app.MapPut("/productos/{id:int}", async (int id, ProductoRequest request, CatalogoDbContext db) =>
{
    var producto = await db.Productos.FindAsync(id);
    if (producto is null)
    {
        return Results.NotFound(new ApiError("No existe un producto con ese ID."));
    }

    var error = ValidarProducto(request);
    if (error is not null)
    {
        return Results.BadRequest(new ApiError(error));
    }

    var codigo = NormalizarCodigo(request.Codigo);
    var existeCodigo = await db.Productos.AnyAsync(otro => otro.Codigo == codigo && otro.Id != id);
    if (existeCodigo)
    {
        return Results.Conflict(new ApiError("Ya existe un producto con ese codigo."));
    }

    producto.Codigo = codigo;
    producto.Nombre = request.Nombre.Trim();
    producto.Precio = request.Precio;
    producto.Stock = request.Stock;

    await db.SaveChangesAsync();

    return Results.Ok(producto.ToDto());
});

app.MapDelete("/productos/{id:int}", async (int id, CatalogoDbContext db) =>
{
    var producto = await db.Productos.FindAsync(id);
    if (producto is null)
    {
        return Results.NotFound(new ApiError("No existe un producto con ese ID."));
    }

    db.Productos.Remove(producto);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, CatalogoDbContext db) =>
{
    var existeProducto = await db.Productos.AnyAsync(producto => producto.Id == productoId);
    if (!existeProducto)
    {
        return Results.NotFound(new ApiError("No existe un producto con ese ID."));
    }

    var movimientos = await db.Movimientos
        .AsNoTracking()
        .Where(movimiento => movimiento.ProductoId == productoId)
        .OrderByDescending(movimiento => movimiento.Fecha)
        .Select(movimiento => movimiento.ToDto())
        .ToListAsync();

    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos", async (int productoId, MovimientoRequest request, CatalogoDbContext db) =>
{
    if (request.Cantidad <= 0)
    {
        return Results.BadRequest(new ApiError("La cantidad debe ser mayor a cero."));
    }

    var producto = await db.Productos.FindAsync(productoId);
    if (producto is null)
    {
        return Results.NotFound(new ApiError("No existe un producto con ese ID."));
    }

    var nuevoStock = request.Tipo switch
    {
        TipoMovimiento.Compra => producto.Stock + request.Cantidad,
        TipoMovimiento.Venta => producto.Stock - request.Cantidad,
        TipoMovimiento.Ajuste => request.Cantidad,
        _ => producto.Stock
    };

    if (nuevoStock < 0)
    {
        return Results.BadRequest(new ApiError("El movimiento dejaria el stock en negativo."));
    }

    await using var tx = await db.Database.BeginTransactionAsync();

    producto.Stock = nuevoStock;
    var movimiento = new MovimientoDeProducto
    {
        ProductoId = productoId,
        Tipo = request.Tipo,
        Cantidad = request.Cantidad,
        Fecha = DateTime.Now
    };

    db.Movimientos.Add(movimiento);
    await db.SaveChangesAsync();
    await tx.CommitAsync();

    return Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento.ToDto());
});

app.Run("http://localhost:5000");

static string? ValidarProducto(ProductoRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Codigo))
    {
        return "El codigo es obligatorio.";
    }

    if (request.Codigo.Trim().Length > 30)
    {
        return "El codigo no puede exceder los 30 caracteres.";
    }

    if (string.IsNullOrWhiteSpace(request.Nombre))
    {
        return "El nombre es obligatorio.";
    }

    if (request.Nombre.Trim().Length > 100)
    {
        return "El nombre no puede exceder los 100 caracteres.";
    }

    if (request.Precio < 0)
    {
        return "El precio no puede ser negativo.";
    }

    if (request.Stock < 0)
    {
        return "El stock no puede ser negativo.";
    }

    return null;
}

static string NormalizarCodigo(string codigo) => codigo.Trim().ToUpperInvariant();

public sealed class Producto
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string Codigo { get; set; } = "";

    [MaxLength(100)]
    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}

public sealed class MovimientoDeProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public Producto? Producto { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

public sealed record ProductoRequest(string Codigo, string Nombre, decimal Precio, int Stock);

public sealed record MovimientoRequest(TipoMovimiento Tipo, int Cantidad);

public sealed record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

public sealed record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);

public sealed record ApiError(string Error);

public static class DtoExtensions
{
    public static ProductoDto ToDto(this Producto producto) =>
        new(producto.Id, producto.Codigo, producto.Nombre, producto.Precio, producto.Stock);

    public static MovimientoDto ToDto(this MovimientoDeProducto movimiento) =>
        new(movimiento.Id, movimiento.ProductoId, movimiento.Tipo, movimiento.Cantidad, movimiento.Fecha);
}

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(producto => producto.Id);
            entity.HasIndex(producto => producto.Codigo).IsUnique();
            entity.Property(producto => producto.Codigo).IsRequired().HasMaxLength(30);
            entity.Property(producto => producto.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(producto => producto.Precio).HasColumnType("decimal(18,2)");
            entity.Property(producto => producto.Stock).IsRequired();
            entity.HasMany(producto => producto.Movimientos)
                .WithOne(movimiento => movimiento.Producto)
                .HasForeignKey(movimiento => movimiento.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovimientoDeProducto>(entity =>
        {
            entity.HasKey(movimiento => movimiento.Id);
            entity.Property(movimiento => movimiento.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(movimiento => movimiento.Cantidad).IsRequired();
            entity.Property(movimiento => movimiento.Fecha).IsRequired();
        });
    }
}
