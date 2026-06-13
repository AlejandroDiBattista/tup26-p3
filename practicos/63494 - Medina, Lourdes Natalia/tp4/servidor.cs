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
builder.Services.AddDbContext<CatalogoDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

    WebApplication app = builder.Build();

  using (IServiceScope scope = app.Services.CreateScope()) {
    CatalogoDbContext db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
    db.Database.EnsureCreated();
    if (!db.Productos.Any()) {
        db.Productos.Add(new Producto {
            Codigo = "P001",
            Nombre = "Yerba Mate 500g",
            Precio = 1500m,
            Stock = 100
        });

        db.SaveChanges();
    }
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

app.MapGet("/productos", async (CatalogoDbContext db) =>
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

app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, CatalogoDbContext db) => {
    bool exists = await db.Productos.AnyAsync(p => p.Id == productoId);
    if (!exists) {
        return Results.NotFound();
    }

    List<MovimientoDeProducto> movimientos = await db.Movimientos
        .AsNoTracking()
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToListAsync();

    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos", async (int productoId, MovimientoRequest request, CatalogoDbContext db) => {
    Producto? producto = await db.Productos.FindAsync(productoId);
    if (producto is null) {
        return Results.NotFound();
    }

    if (request.Cantidad < 0) {
        return Results.BadRequest(new { Error = "La cantidad no puede ser negativa." });
    }

    int cantidadRegistrada;
    int stockAnterior = producto.Stock;

    switch (request.Tipo) {
        case TipoMovimiento.Compra:
            if (request.Cantidad == 0) {
                return Results.BadRequest(new { Error = "La compra debe indicar una cantidad mayor que cero." });
            }

            producto.Stock += request.Cantidad;
            cantidadRegistrada = request.Cantidad;
            break;

        case TipoMovimiento.Venta:
            if (request.Cantidad == 0) {
                return Results.BadRequest(new { Error = "La venta debe indicar una cantidad mayor que cero." });
            }

            if (producto.Stock < request.Cantidad) {
                return Results.BadRequest(new { Error = "No hay stock suficiente para registrar la venta." });
            }

            producto.Stock -= request.Cantidad;
            cantidadRegistrada = -request.Cantidad;
            break;

        case TipoMovimiento.Ajuste:
            producto.Stock = request.Cantidad;
            cantidadRegistrada = producto.Stock - stockAnterior;
            break;

        default:
            return Results.BadRequest(new { Error = "Tipo de movimiento invalido." });
    }

    MovimientoDeProducto movimiento = new() {
        ProductoId = producto.Id,
        Tipo = request.Tipo,
        Cantidad = cantidadRegistrada,
        Fecha = DateTime.Now
    };

    db.Movimientos.Add(movimiento);
    await db.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}/movimientos/{movimiento.Id}", new MovimientoResponse(
        movimiento.Id,
        movimiento.ProductoId,
        movimiento.Tipo,
        movimiento.Cantidad,
        movimiento.Fecha,
        producto.Stock));
});

app.Run("http://localhost:5000");

static async Task<string?> ValidateProduct(ProductoRequest request, CatalogoDbContext db, int? currentId = null) {
    string codigo = request.Codigo.Trim();
    string nombre = request.Nombre.Trim();

    if (string.IsNullOrWhiteSpace(codigo)) {
        return "El codigo no puede estar vacio.";
    }

    if (string.IsNullOrWhiteSpace(nombre)) {
        return "El nombre no puede estar vacio.";
    }

    if (request.Precio < 0) {
        return "El precio no puede ser negativo.";
    }

    if (request.Stock < 0) {
        return "El stock no puede ser negativo.";
    }

    bool duplicated = await db.Productos.AnyAsync(p =>
        p.Codigo == codigo && (!currentId.HasValue || p.Id != currentId.Value));

    return duplicated ? "Ya existe un producto con ese codigo." : null;
}

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options) {
    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>(entity => {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Codigo).IsUnique();
            entity.Property(p => p.Codigo).IsRequired();
            entity.Property(p => p.Nombre).IsRequired();
            entity.Property(p => p.Precio).HasColumnType("decimal(18,2)");
            entity.HasMany(p => p.Movimientos)
                .WithOne(m => m.Producto)
                .HasForeignKey(m => m.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovimientoDeProducto>(entity => {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Tipo).HasConversion<string>();
            entity.Property(m => m.Fecha).IsRequired();
        });
    }
}

public sealed class Producto {
    public int Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    [JsonIgnore]
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}

public sealed class MovimientoDeProducto {
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    [JsonIgnore]
    public Producto? Producto { get; set; }
}

public enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

public sealed record ProductoRequest(string Codigo, string Nombre, decimal Precio, int Stock);

public sealed record MovimientoRequest(TipoMovimiento Tipo, int Cantidad);

public sealed record MovimientoResponse(
    int Id,
    int ProductoId,
    TipoMovimiento Tipo,
    int Cantidad,
    DateTime Fecha,
    int StockActual);