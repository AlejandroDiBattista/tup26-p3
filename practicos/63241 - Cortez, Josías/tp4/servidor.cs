#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opciones =>
    opciones.UseSqlite("Data Source=catalogo.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();

    if (!db.Productos.Any()) {
        var producto = new Producto {
            Codigo = "P001",
            Nombre = "Yerba Mate 500g",
            Precio = 1500m,
            Stock = 100,
        };

        db.Productos.Add(producto);
        db.SaveChanges();

        db.Movimientos.Add(new MovimientoDeProducto {
            ProductoId = producto.Id,
            Tipo = TipoMovimiento.Ajuste,
            Cantidad = producto.Stock,
            Fecha = DateTime.Now,
        });
        db.SaveChanges();
    }
}

app.MapGet("/productos", async (CatalogoDb db) =>
    await db.Productos
        .OrderBy(p => p.Codigo)
        .Select(p => ProductoDto.Desde(p))
        .ToListAsync());

app.MapGet("/productos/{id:int}", async (int id, CatalogoDb db) => {
    var producto = await db.Productos.FindAsync(id);
    return producto is null ? Results.NotFound() : Results.Ok(ProductoDto.Desde(producto));
});

app.MapPost("/productos", async (ProductoGuardarDto dto, CatalogoDb db) => {
    var error = await ValidarProductoAsync(dto, db);
    if (error is not null) {
        return Results.BadRequest(error);
    }

    var producto = new Producto {
        Codigo = dto.Codigo.Trim(),
        Nombre = dto.Nombre.Trim(),
        Precio = dto.Precio,
        Stock = dto.Stock,
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}", ProductoDto.Desde(producto));
});

app.MapPut("/productos/{id:int}", async (int id, ProductoGuardarDto dto, CatalogoDb db) => {
    var producto = await db.Productos.FindAsync(id);
    if (producto is null) {
        return Results.NotFound();
    }

    var error = await ValidarProductoAsync(dto, db, id);
    if (error is not null) {
        return Results.BadRequest(error);
    }

    producto.Codigo = dto.Codigo.Trim();
    producto.Nombre = dto.Nombre.Trim();
    producto.Precio = dto.Precio;
    producto.Stock = dto.Stock;

    await db.SaveChangesAsync();
    return Results.Ok(ProductoDto.Desde(producto));
});

app.MapDelete("/productos/{id:int}", async (int id, CatalogoDb db) => {
    var producto = await db.Productos.FindAsync(id);
    if (producto is null) {
        return Results.NotFound();
    }

    db.Productos.Remove(producto);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, CatalogoDb db) => {
    var existeProducto = await db.Productos.AnyAsync(p => p.Id == productoId);
    if (!existeProducto) {
        return Results.NotFound();
    }

    var movimientos = await db.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .Select(m => MovimientoDto.Desde(m))
        .ToListAsync();

    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos", async (
    int productoId,
    MovimientoCrearDto dto,
    CatalogoDb db) => {
    var producto = await db.Productos.FindAsync(productoId);
    if (producto is null) {
        return Results.NotFound();
    }

    if (dto.Cantidad <= 0) {
        return Results.BadRequest("La cantidad debe ser positiva.");
    }

    if (!Enum.TryParse<TipoMovimiento>(dto.Tipo, true, out var tipo)) {
        return Results.BadRequest("Tipo de movimiento invalido. Use Compra, Venta o Ajuste.");
    }

    await using var transaccion = await db.Database.BeginTransactionAsync();

    switch (tipo) {
        case TipoMovimiento.Compra:
            producto.Stock += dto.Cantidad;
            break;
        case TipoMovimiento.Venta:
            if (producto.Stock < dto.Cantidad) {
                return Results.BadRequest("No hay stock suficiente para registrar la venta.");
            }

            producto.Stock -= dto.Cantidad;
            break;
        case TipoMovimiento.Ajuste:
            producto.Stock = dto.Cantidad;
            break;
    }

    var movimiento = new MovimientoDeProducto {
        ProductoId = producto.Id,
        Tipo = tipo,
        Cantidad = dto.Cantidad,
        Fecha = DateTime.Now,
    };

    db.Movimientos.Add(movimiento);
    await db.SaveChangesAsync();
    await transaccion.CommitAsync();

    return Results.Created(
        $"/productos/{producto.Id}/movimientos/{movimiento.Id}",
        MovimientoDto.Desde(movimiento));
});

app.Run("http://localhost:5050");

static async Task<string?> ValidarProductoAsync(
    ProductoGuardarDto dto,
    CatalogoDb db,
    int? idActual = null) {
    if (string.IsNullOrWhiteSpace(dto.Codigo)) {
        return "El codigo es obligatorio.";
    }

    if (string.IsNullOrWhiteSpace(dto.Nombre)) {
        return "El nombre es obligatorio.";
    }

    if (dto.Precio < 0) {
        return "El precio no puede ser negativo.";
    }

    if (dto.Stock < 0) {
        return "El stock no puede ser negativo.";
    }

    var codigo = dto.Codigo.Trim();
    var codigoRepetido = await db.Productos.AnyAsync(p =>
        p.Codigo == codigo && (!idActual.HasValue || p.Id != idActual.Value));

    return codigoRepetido ? "Ya existe un producto con ese codigo." : null;
}

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}

class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste,
}

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock) {
    public static ProductoDto Desde(Producto producto) =>
        new(producto.Id, producto.Codigo, producto.Nombre, producto.Precio, producto.Stock);
}

record ProductoGuardarDto(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoCrearDto(string Tipo, int Cantidad);

record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha) {
    public static MovimientoDto Desde(MovimientoDeProducto movimiento) =>
        new(
            movimiento.Id,
            movimiento.ProductoId,
            movimiento.Tipo.ToString(),
            movimiento.Cantidad,
            movimiento.Fecha);
}

class CatalogoDb(DbContextOptions<CatalogoDb> options) : DbContext(options) {
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<Producto>()
            .Property(p => p.Precio)
            .HasConversion<double>();

        modelBuilder.Entity<MovimientoDeProducto>()
            .Property(m => m.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<MovimientoDeProducto>()
            .HasOne(m => m.Producto)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
