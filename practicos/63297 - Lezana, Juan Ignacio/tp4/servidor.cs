#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "Movimientos" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Movimientos" PRIMARY KEY AUTOINCREMENT,
            "ProductoId" INTEGER NOT NULL,
            "Tipo" INTEGER NOT NULL,
            "Cantidad" INTEGER NOT NULL,
            "Fecha" TEXT NOT NULL,
            CONSTRAINT "FK_Movimientos_Productos_ProductoId"
                FOREIGN KEY ("ProductoId") REFERENCES "Productos" ("Id") ON DELETE CASCADE
        );
        """);
    db.Database.ExecuteSqlRaw("""
        CREATE INDEX IF NOT EXISTS "IX_Movimientos_ProductoId"
        ON "Movimientos" ("ProductoId");
        """);
    db.Database.ExecuteSqlRaw("""
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Productos_Codigo"
        ON "Productos" ("Codigo");
        """);

    if (!db.Productos.Any()) {
        db.Productos.AddRange(
            new Producto { Codigo = "P001", Nombre = "Yerba Mate CBSÉ 500g", Precio = 1500m, Stock = 100 },
            new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 900m, Stock = 75 },
            new Producto { Codigo = "P003", Nombre = "Cafe la virginia 500g", Precio = 3200m, Stock = 30 }
        );
        db.SaveChanges();
    }
}

app.MapGet("/productos", async (CatalogoRepositorio repo) =>
    Results.Ok((await repo.ListarProductosAsync()).Select(ProductoSalida.Desde)));

app.MapGet("/productos/{id:int}", async (int id, CatalogoRepositorio repo) => {
    var producto = await repo.TraerProductoAsync(id);
    return producto is null ? Results.NotFound() : Results.Ok(ProductoSalida.Desde(producto));
});

app.MapPost("/productos", async (ProductoEntrada entrada, CatalogoRepositorio repo) => {
    var error = ValidarProducto(entrada);
    if (error is not null) return Results.BadRequest(error);

    try {
        var producto = await repo.CrearProductoAsync(entrada);
        return Results.Created($"/productos/{producto.Id}", ProductoSalida.Desde(producto));
    } catch (CodigoDuplicadoException) {
        return Results.Conflict("Ya existe un producto con ese codigo.");
    }
});

app.MapPut("/productos/{id:int}", async (int id, ProductoEntrada entrada, CatalogoRepositorio repo) => {
    var error = ValidarProducto(entrada);
    if (error is not null) return Results.BadRequest(error);

    try {
        var producto = await repo.ModificarProductoAsync(id, entrada);
        return producto is null ? Results.NotFound() : Results.Ok(ProductoSalida.Desde(producto));
    } catch (CodigoDuplicadoException) {
        return Results.Conflict("Ya existe un producto con ese codigo.");
    }
});

app.MapDelete("/productos/{id:int}", async (int id, CatalogoRepositorio repo) =>
    await repo.EliminarProductoAsync(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, CatalogoRepositorio repo) => {
    if (!await repo.ExisteProductoAsync(productoId)) return Results.NotFound();
    return Results.Ok((await repo.ListarMovimientosAsync(productoId)).Select(MovimientoSalida.Desde));
});

app.MapPost("/productos/{productoId:int}/movimientos", async (int productoId, MovimientoEntrada entrada, CatalogoRepositorio repo) => {
    if (entrada.Cantidad <= 0) return Results.BadRequest("La cantidad debe ser positiva.");

    try {
        var movimiento = await repo.RegistrarMovimientoAsync(productoId, entrada);
        return movimiento is null
            ? Results.NotFound()
            : Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", MovimientoSalida.Desde(movimiento));
    } catch (StockInsuficienteException) {
        return Results.BadRequest("No hay stock suficiente para registrar la venta.");
    }
});

app.Run("http://localhost:5050");

static string? ValidarProducto(ProductoEntrada entrada) {
    if (string.IsNullOrWhiteSpace(entrada.Codigo)) return "El codigo es obligatorio.";
    if (string.IsNullOrWhiteSpace(entrada.Nombre)) return "El nombre es obligatorio.";
    if (entrada.Precio < 0) return "El precio no puede ser negativo.";
    if (entrada.Stock < 0) return "El stock no puede ser negativo.";
    return null;
}

enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

class Producto {
    public int Id { get; set; }
    [MaxLength(40)]
    public string Codigo { get; set; } = "";
    [MaxLength(120)]
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

record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);
record ProductoSalida(int Id, string Codigo, string Nombre, decimal Precio, int Stock) {
    public static ProductoSalida Desde(Producto producto) =>
        new(producto.Id, producto.Codigo, producto.Nombre, producto.Precio, producto.Stock);
}

record MovimientoSalida(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha) {
    public static MovimientoSalida Desde(MovimientoDeProducto movimiento) =>
        new(movimiento.Id, movimiento.ProductoId, movimiento.Tipo, movimiento.Cantidad, movimiento.Fecha);
}

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<MovimientoDeProducto>()
            .HasOne(m => m.Producto)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.Add(new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100));
            db.SaveChanges();
        }
    }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}