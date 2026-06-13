#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(opt => {
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));

var app = builder.Build();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();

    if (!db.Productos.Any()) {
        db.Productos.AddRange(
            new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
            new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 900m, Stock = 60 },
            new Producto { Codigo = "P003", Nombre = "Cafe molido 250g", Precio = 2500m, Stock = 35 }
        );
        db.SaveChanges();
    }
}
app.MapGet("/productos", async (CatalogoDb db) =>
    await db.Productos
        .OrderBy(p => p.Codigo)
        .Select(p => ProductoDto.DesdeModelo(p))
        .ToListAsync()
);
app.MapGet("/productos/{id:int}", async (int id, CatalogoDb db) => {
    var producto = await db.Productos.FindAsync(id);
    return producto is null
        ? Results.NotFound($"No existe un producto con id {id}.")
        : Results.Ok(ProductoDto.DesdeModelo(producto));
});
app.MapPost("/productos", async (ProductoCrearDto dto, CatalogoDb db) => {
    var error = ValidarProducto(dto.Codigo, dto.Nombre, dto.Precio, dto.Stock);
    if (error is not null) return Results.BadRequest(error);

    var codigo = dto.Codigo.Trim().ToUpperInvariant();
    var existeCodigo = await db.Productos.AnyAsync(p => p.Codigo == codigo);
    if (existeCodigo) return Results.Conflict($"Ya existe un producto con codigo {codigo}.");

    var producto = new Producto {
        Codigo = codigo,
        Nombre = dto.Nombre.Trim(),
        Precio = dto.Precio,
        Stock = dto.Stock
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}", ProductoDto.DesdeModelo(producto));
});
app.MapPut("/productos/{id:int}", async (int id, ProductoCrearDto dto, CatalogoDb db) => {
    var producto = await db.Productos.FindAsync(id);
    if (producto is null) return Results.NotFound($"No existe un producto con id {id}.");

    var error = ValidarProducto(dto.Codigo, dto.Nombre, dto.Precio, dto.Stock);
    if (error is not null) return Results.BadRequest(error);

    var codigo = dto.Codigo.Trim().ToUpperInvariant();
    var existeCodigo = await db.Productos.AnyAsync(p => p.Id != id && p.Codigo == codigo);
    if (existeCodigo) return Results.Conflict($"Ya existe otro producto con codigo {codigo}.");

    producto.Codigo = codigo;
    producto.Nombre = dto.Nombre.Trim();
    producto.Precio = dto.Precio;
    producto.Stock = dto.Stock;

    await db.SaveChangesAsync();
    return Results.Ok(ProductoDto.DesdeModelo(producto));
});
app.MapDelete("/productos/{id:int}", async (int id, CatalogoDb db) => {
    var producto = await db.Productos.FindAsync(id);
    if (producto is null) return Results.NotFound($"No existe un producto con id {id}.");

    db.Productos.Remove(producto);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, CatalogoDb db) => {
    var existeProducto = await db.Productos.AnyAsync(p => p.Id == productoId);
    if (!existeProducto) return Results.NotFound($"No existe un producto con id {productoId}.");

    var movimientos = await db.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .Select(m => MovimientoDto.DesdeModelo(m))
        .ToListAsync();

    return Results.Ok(movimientos);
});
app.MapPost("/productos/{productoId:int}/movimientos", async (int productoId, MovimientoCrearDto dto, CatalogoDb db) => {
    if (dto.Cantidad <= 0) return Results.BadRequest("La cantidad debe ser positiva.");

    var producto = await db.Productos.FindAsync(productoId);
    if (producto is null) return Results.NotFound($"No existe un producto con id {productoId}.");

    var nuevoStock = dto.Tipo switch {
        TipoMovimiento.Compra => producto.Stock + dto.Cantidad,
        TipoMovimiento.Venta => producto.Stock - dto.Cantidad,
        TipoMovimiento.Ajuste => dto.Cantidad,
        _ => producto.Stock
    };

    if (nuevoStock < 0) return Results.BadRequest("No hay stock suficiente para registrar la venta.");

    await using var transaccion = await db.Database.BeginTransactionAsync();

    producto.Stock = nuevoStock;
    var movimiento = new MovimientoDeProducto {
        ProductoId = producto.Id,
        Tipo = dto.Tipo,
        Cantidad = dto.Cantidad,
        Fecha = DateTime.Now
    };

    db.Movimientos.Add(movimiento);
    await db.SaveChangesAsync();
    await transaccion.CommitAsync();

    return Results.Created(
        $"/productos/{producto.Id}/movimientos/{movimiento.Id}",
        new MovimientoRegistradoDto(ProductoDto.DesdeModelo(producto), MovimientoDto.DesdeModelo(movimiento))
    );
});
app.Run("http://localhost:5050");

static string? ValidarProducto(string codigo, string nombre, decimal precio, int stock) {
    if (string.IsNullOrWhiteSpace(codigo)) return "El codigo es obligatorio.";
    if (string.IsNullOrWhiteSpace(nombre)) return "El nombre es obligatorio.";
    if (precio < 0) return "El precio no puede ser negativo.";
    if (stock < 0) return "El stock no puede ser negativo.";
    return null;
}
class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
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
    Ajuste
}

record ProductoCrearDto(string Codigo, string Nombre, decimal Precio, int Stock);

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock) {
    public static ProductoDto DesdeModelo(Producto producto) =>
        new(producto.Id, producto.Codigo, producto.Nombre, producto.Precio, producto.Stock);
}

record MovimientoCrearDto(TipoMovimiento Tipo, int Cantidad);

record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha) {
    public static MovimientoDto DesdeModelo(MovimientoDeProducto movimiento) =>
        new(movimiento.Id, movimiento.ProductoId, movimiento.Tipo, movimiento.Cantidad, movimiento.Fecha);
}

record MovimientoRegistradoDto(ProductoDto Producto, MovimientoDto Movimiento);