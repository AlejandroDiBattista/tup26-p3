#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(options =>
    options.UseSqlite("Data Source=catalogo.db"));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    CatalogoDb db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();
    await CargarDatosInicialesAsync(db);
}

var productos = app.MapGroup("/productos");

// READ: devuelve la lista completa para el panel maestro de la TUI.
productos.MapGet("/", async (CatalogoDb db) =>
    Results.Ok(await db.Productos
        .AsNoTracking()
        .OrderBy(p => p.Nombre)
        .ToListAsync()));

productos.MapGet("/{id:int}", async (int id, CatalogoDb db) => {
    Producto? producto = await db.Productos
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

    return producto is null
        ? Results.NotFound(new { error = "Producto no encontrado." })
        : Results.Ok(producto);
});

// CREATE: valida los datos, controla que el codigo sea unico y guarda el producto.
productos.MapPost("/", async (ProductoDatos datos, CatalogoDb db) => {
    string? error = ValidarProducto(datos);
    if (error is not null) {
        return Results.BadRequest(new { error });
    }

    string codigo = datos.Codigo.Trim();
    if (await db.Productos.AnyAsync(p => p.Codigo == codigo)) {
        return Results.Conflict(new { error = "Ya existe un producto con ese codigo." });
    }

    Producto producto = new() {
        Codigo = codigo,
        Nombre = datos.Nombre.Trim(),
        Precio = datos.Precio,
        Stock = datos.Stock
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{producto.Id}", producto);
});

// UPDATE: busca por id y modifica solamente el producto encontrado.
productos.MapPut("/{id:int}", async (int id, ProductoDatos datos, CatalogoDb db) => {
    string? error = ValidarProducto(datos);
    if (error is not null) {
        return Results.BadRequest(new { error });
    }

    Producto? producto = await db.Productos.FindAsync(id);
    if (producto is null) {
        return Results.NotFound(new { error = "Producto no encontrado." });
    }

    string codigo = datos.Codigo.Trim();
    bool codigoRepetido = await db.Productos
        .AnyAsync(p => p.Id != id && p.Codigo == codigo);
    if (codigoRepetido) {
        return Results.Conflict(new { error = "Ya existe un producto con ese codigo." });
    }

    producto.Codigo = codigo;
    producto.Nombre = datos.Nombre.Trim();
    producto.Precio = datos.Precio;
    producto.Stock = datos.Stock;

    await db.SaveChangesAsync();
    return Results.Ok(producto);
});

// DELETE: elimina el producto; sus movimientos se borran en cascada.
productos.MapDelete("/{id:int}", async (int id, CatalogoDb db) => {
    Producto? producto = await db.Productos.FindAsync(id);
    if (producto is null) {
        return Results.NotFound(new { error = "Producto no encontrado." });
    }

    db.Productos.Remove(producto);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

productos.MapGet("/{productoId:int}/movimientos", async (int productoId, CatalogoDb db) => {
    if (!await db.Productos.AnyAsync(p => p.Id == productoId)) {
        return Results.NotFound(new { error = "Producto no encontrado." });
    }

    List<MovimientoDeProducto> movimientos = await db.Movimientos
        .AsNoTracking()
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToListAsync();

    return Results.Ok(movimientos);
});

productos.MapPost("/{productoId:int}/movimientos", async (
    int productoId,
    MovimientoDatos datos,
    CatalogoDb db) => {

    if (datos.Cantidad <= 0) {
        return Results.BadRequest(new { error = "La cantidad debe ser mayor que cero." });
    }

    Producto? producto = await db.Productos.FindAsync(productoId);
    if (producto is null) {
        return Results.NotFound(new { error = "Producto no encontrado." });
    }

    int nuevoStock = datos.Tipo switch {
        TipoMovimiento.Compra => producto.Stock + datos.Cantidad,
        TipoMovimiento.Venta => producto.Stock - datos.Cantidad,
        TipoMovimiento.Ajuste => datos.Cantidad,
        _ => producto.Stock
    };

    if (nuevoStock < 0) {
        return Results.BadRequest(new { error = "No hay stock suficiente para realizar la venta." });
    }

    await using var transaccion = await db.Database.BeginTransactionAsync();

    producto.Stock = nuevoStock;
    MovimientoDeProducto movimiento = new() {
        ProductoId = productoId,
        Tipo = datos.Tipo,
        Cantidad = datos.Cantidad,
        Fecha = DateTime.Now
    };

    db.Movimientos.Add(movimiento);
    await db.SaveChangesAsync();
    await transaccion.CommitAsync();

    return Results.Created(
        $"/productos/{productoId}/movimientos/{movimiento.Id}",
        new MovimientoRegistrado(movimiento, producto.Stock));
});

string url = Environment.GetEnvironmentVariable("CATALOGO_URL")
    ?? "http://localhost:5050";
app.Run(url);

static string? ValidarProducto(ProductoDatos datos) {
    if (string.IsNullOrWhiteSpace(datos.Codigo)) {
        return "El codigo es obligatorio.";
    }

    if (string.IsNullOrWhiteSpace(datos.Nombre)) {
        return "El nombre es obligatorio.";
    }

    if (datos.Precio < 0) {
        return "El precio no puede ser negativo.";
    }

    if (datos.Stock < 0) {
        return "El stock no puede ser negativo.";
    }

    return null;
}

static async Task CargarDatosInicialesAsync(CatalogoDb db) {
    if (await db.Productos.AnyAsync()) {
        return;
    }

    db.Productos.AddRange(
        new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
        new Producto { Codigo = "P002", Nombre = "Cafe 250g", Precio = 3200m, Stock = 45 },
        new Producto { Codigo = "P003", Nombre = "Azucar 1kg", Precio = 1100m, Stock = 80 });

    await db.SaveChangesAsync();
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
    [JsonIgnore]
    public Producto? Producto { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

public enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

public sealed record ProductoDatos(string Codigo, string Nombre, decimal Precio, int Stock);
public sealed record MovimientoDatos(TipoMovimiento Tipo, int Cantidad);
public sealed record MovimientoRegistrado(MovimientoDeProducto Movimiento, int StockActual);

public sealed class CatalogoDb(DbContextOptions<CatalogoDb> options) : DbContext(options) {
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>()
            .Property(p => p.Codigo)
            .UseCollation("NOCASE");

        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<Producto>()
            .HasMany(p => p.Movimientos)
            .WithOne(m => m.Producto)
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovimientoDeProducto>()
            .Property(m => m.Tipo)
            .HasConversion<string>();
    }
}
