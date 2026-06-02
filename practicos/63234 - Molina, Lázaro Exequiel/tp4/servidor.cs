#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opt => {
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<TiendaDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<ServicioCatalogo>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var servicio = scope.ServiceProvider.GetRequiredService<ServicioCatalogo>();
    servicio.PrepararBase();
}

app.MapGet("/productos", async (ServicioCatalogo servicio) =>
    Results.Ok(await servicio.ListarProductosAsync()));

app.MapGet("/productos/{id:int}", async (int id, ServicioCatalogo servicio) => {
    var producto = await servicio.BuscarProductoAsync(id);
    return producto is null
        ? Results.NotFound($"No se encontro el producto con id {id}.")
        : Results.Ok(producto);
});

app.MapPost("/productos", async (ProductoEntrada entrada, ServicioCatalogo servicio) => {
    var resultado = await servicio.CrearProductoAsync(entrada);
    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Created($"/productos/{resultado.Producto!.Id}", resultado.Producto);
});

app.MapPut("/productos/{id:int}", async (int id, ProductoEntrada entrada, ServicioCatalogo servicio) => {
    var resultado = await servicio.ActualizarProductoAsync(id, entrada);
    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Ok(resultado.Producto);
});

app.MapDelete("/productos/{id:int}", async (int id, ServicioCatalogo servicio) =>
    await servicio.BorrarProductoAsync(id)
        ? Results.NoContent()
        : Results.NotFound($"No se encontro el producto con id {id}."));

app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, ServicioCatalogo servicio) => {
    var movimientos = await servicio.ListarMovimientosAsync(productoId);
    return movimientos is null
        ? Results.NotFound($"No se encontro el producto con id {productoId}.")
        : Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos", async (int productoId, MovimientoEntrada entrada, ServicioCatalogo servicio) => {
    var resultado = await servicio.RegistrarMovimientoAsync(productoId, entrada);
    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Created($"/productos/{productoId}/movimientos/{resultado.Movimiento!.Id}", resultado);
});

app.Run("http://localhost:5050");

class ServicioCatalogo {
    private readonly TiendaDb db;

    public ServicioCatalogo(TiendaDb db) => this.db = db;
    public void PrepararBase() {
        db.Database.EnsureCreated();

        if (db.Productos.Any()) return;

        db.Productos.AddRange(
            new Producto { Codigo = "A100", Nombre = "Yerba Cachamate 500g", Precio = 1450m, Stock = 80 },
            new Producto { Codigo = "B220", Nombre = "Azucar  1kg", Precio = 890m, Stock = 45 },
            new Producto { Codigo = "C315", Nombre = "Cafe instantaneo 250g", Precio = 2600m, Stock = 30 }
        );

        db.SaveChanges();
    }

    public async Task<List<ProductoDto>> ListarProductosAsync() =>
        await db.Productos
            .OrderBy(p => p.Codigo)
            .Select(p => ProductoDto.Desde(p))
            .ToListAsync();

    public async Task<ProductoDto?> BuscarProductoAsync(int id) {
        var producto = await db.Productos.FindAsync(id);
        return producto is null ? null : ProductoDto.Desde(producto);
    }

    public async Task<ResultadoProducto> CrearProductoAsync(ProductoEntrada entrada) {
        var error = ValidarProducto(entrada);
        if (error is not null) return new(null, error);

        var codigo = entrada.Codigo.Trim().ToUpperInvariant();
        if (await db.Productos.AnyAsync(p => p.Codigo == codigo)) {
            return new(null, $"Ya existe un producto con codigo {codigo}.");
        }

        var producto = new Producto {
            Codigo = codigo,
            Nombre = entrada.Nombre.Trim(),
            Precio = entrada.Precio,
            Stock = entrada.Stock
        };

        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        return new(ProductoDto.Desde(producto), null);
    }

    public async Task<ResultadoProducto> ActualizarProductoAsync(int id, ProductoEntrada entrada) {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null) return new(null, $"No se encontro el producto con id {id}.");

        var error = ValidarProducto(entrada);
        if (error is not null) return new(null, error);

        var codigo = entrada.Codigo.Trim().ToUpperInvariant();
        if (await db.Productos.AnyAsync(p => p.Id != id && p.Codigo == codigo)) {
            return new(null, $"Otro producto ya usa el codigo {codigo}.");
        }

        producto.Codigo = codigo;
        producto.Nombre = entrada.Nombre.Trim();
        producto.Precio = entrada.Precio;
        producto.Stock = entrada.Stock;

        await db.SaveChangesAsync();
        return new(ProductoDto.Desde(producto), null);
    }

    public async Task<bool> BorrarProductoAsync(int id) {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null) return false;

        db.Productos.Remove(producto);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<MovimientoDto>?> ListarMovimientosAsync(int productoId) {
        if (!await db.Productos.AnyAsync(p => p.Id == productoId)) return null;

        return await db.Movimientos
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .Select(m => MovimientoDto.Desde(m))
            .ToListAsync();
    }

    public async Task<ResultadoMovimiento> RegistrarMovimientoAsync(int productoId, MovimientoEntrada entrada) {
        if (entrada.Cantidad <= 0) return new(null, null, "La cantidad debe ser positiva.");

        var producto = await db.Productos.FindAsync(productoId);
        if (producto is null) return new(null, null, $"No se encontro el producto con id {productoId}.");

        var stockActualizado = entrada.Tipo switch {
            TipoMovimiento.Compra => producto.Stock + entrada.Cantidad,
            TipoMovimiento.Venta => producto.Stock - entrada.Cantidad,
            TipoMovimiento.Ajuste => entrada.Cantidad,
            _ => producto.Stock
        };

        if (stockActualizado < 0) return new(null, null, "No hay stock suficiente para la venta.");

        await using var tx = await db.Database.BeginTransactionAsync();

        producto.Stock = stockActualizado;
        var movimiento = new MovimientoDeStock {
            ProductoId = producto.Id,
            Tipo = entrada.Tipo,
            Cantidad = entrada.Cantidad,
            Fecha = DateTime.Now
        };

        db.Movimientos.Add(movimiento);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new(ProductoDto.Desde(producto), MovimientoDto.Desde(movimiento), null);
    }

    private static string? ValidarProducto(ProductoEntrada entrada) {
        if (string.IsNullOrWhiteSpace(entrada.Codigo)) return "El codigo es obligatorio.";
        if (string.IsNullOrWhiteSpace(entrada.Nombre)) return "El nombre es obligatorio.";
        if (entrada.Precio < 0) return "El precio no puede ser negativo.";
        if (entrada.Stock < 0) return "El stock no puede ser negativo.";
        return null;
    }
}

class TiendaDb : DbContext {
    public TiendaDb(DbContextOptions<TiendaDb> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeStock> Movimientos => Set<MovimientoDeStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>().HasIndex(p => p.Codigo).IsUnique();
        modelBuilder.Entity<Producto>().Property(p => p.Precio).HasPrecision(18, 2);
        modelBuilder.Entity<MovimientoDeStock>()
            .HasOne(m => m.Producto)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public List<MovimientoDeStock> Movimientos { get; set; } = [];
}

class MovimientoDeStock {
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
    Ajuste
}

record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock) {
    public static ProductoDto Desde(Producto producto) =>
        new(producto.Id, producto.Codigo, producto.Nombre, producto.Precio, producto.Stock);
}

record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha) {
    public static MovimientoDto Desde(MovimientoDeStock movimiento) =>
        new(movimiento.Id, movimiento.ProductoId, movimiento.Tipo, movimiento.Cantidad, movimiento.Fecha);
}

record ResultadoProducto(ProductoDto? Producto, string? Error);
record ResultadoMovimiento(ProductoDto? Producto, MovimientoDto? Movimiento, string? Error);
