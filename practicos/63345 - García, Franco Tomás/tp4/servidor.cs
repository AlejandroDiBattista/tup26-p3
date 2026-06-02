#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    var productos = repositorio.TraerProductos();
    return Results.Ok(productos);
});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProductoPorId(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repositorio) => {
    var nuevo = repositorio.CrearProducto(producto);
    return Results.Created($"/productos/{nuevo.Id}", nuevo);
});

app.MapPut("/productos/{id}", (int id, Producto producto, CatalogoRepositorio repo) => {
    var ok = repo.ActualizarProducto(id, producto);
    return ok ? Results.NoContent() : Results.NotFound();
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    var ok = repo.EliminarProducto(id);
    return ok ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/productos/{id}/movimientos", (int id, CatalogoRepositorio repo) => {
    var movimientos = repo.TraerMovimientos(id);
    return Results.Ok(movimientos);
});

app.MapPost("/productos/{id}/movimientos", (int id, MovimientoDto dto, CatalogoRepositorio repo) => {
    var ok = repo.RegistrarMovimiento(id, dto.Tipo, dto.Cantidad);
    return ok ? Results.Ok() : Results.BadRequest();
});

app.Run("http://localhost:5050");

record MovimientoDto(TipoMovimiento Tipo, int Cantidad);
enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record class MovimientoDeProducto(
    int Id,
    int ProductoId,
    TipoMovimiento Tipo,
    int Cantidad,
    DateTime Fecha
);
// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;
    public Producto? TraerProductoPorId(int id) => db.Productos.Find(id);
    public Producto CrearProducto(Producto producto) {
        db.Productos.Add(producto);
        db.SaveChanges();
        return producto;
    }
    public bool ActualizarProducto(int id, Producto datos) {
        var producto = db.Productos.Find(id);
        if (producto is null) return false;

        var actualizado = producto with {
            Codigo = datos.Codigo,
            Nombre = datos.Nombre,
            Precio = datos.Precio,
            Stock = datos.Stock
        };

        db.Entry(producto).CurrentValues.SetValues(actualizado);
        db.SaveChanges();
        return true;
    }
    public bool EliminarProducto(int id) {
        var producto = db.Productos.Find(id);
        if (producto is null) return false;

        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }
    public bool RegistrarMovimiento(int productoId, TipoMovimiento tipo, int cantidad) {
        var producto = db.Productos.Find(productoId);
        if (producto is null) return false;

        int nuevoStock = producto.Stock;

        switch (tipo) {
            case TipoMovimiento.Compra:
                nuevoStock += cantidad;
                break;

            case TipoMovimiento.Venta:
                nuevoStock -= cantidad;
                break;

            case TipoMovimiento.Ajuste:
                nuevoStock = cantidad;
                break;
        }

        if (nuevoStock < 0) return false;

        var actualizado = producto with { Stock = nuevoStock };
        db.Entry(producto).CurrentValues.SetValues(actualizado);

        var movimiento = new MovimientoDeProducto(
            0,
            productoId,
            tipo,
            cantidad,
            DateTime.Now
        );

        db.Movimientos.Add(movimiento);

        db.SaveChanges();
        return true;
    }
    public List<MovimientoDeProducto> TraerMovimientos(int productoId) =>
    db.Movimientos
      .Where(m => m.ProductoId == productoId)
      .OrderByDescending(m => m.Fecha)
      .ToList();
    public List<Producto> TraerProductos() => db.Productos.OrderBy(p => p.Id).ToList();
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