#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

// ── Inicialización de la base de datos ───────────────────────────────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repo.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoRepositorio repo) =>
    Results.Ok(repo.TraerProductos()));

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repo) =>
    repo.TraerProductoPorId(id) is { } p ? Results.Ok(p) : Results.NotFound());

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repo) =>
    Results.Created("", repo.CrearProducto(producto)));

app.MapPut("/productos/{id}", (int id, Producto producto, CatalogoRepositorio repo) =>
    repo.ActualizarProducto(id, producto) ? Results.NoContent() : Results.NotFound());

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repo) =>
    repo.EliminarProducto(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/productos/{id}/movimientos", (int id, CatalogoRepositorio repo) =>
    Results.Ok(repo.TraerMovimientos(id)));

app.MapPost("/productos/{id}/movimientos", (int id, MovimientoDto dto, CatalogoRepositorio repo) =>
{
    var ok = repo.RegistrarMovimiento(id, dto.Tipo, dto.Cantidad);
    return ok ? Results.Ok() : Results.BadRequest("Stock inválido o producto inexistente");
});

app.Run("http://localhost:5050");

// ── DTO ───────────────────────────────────────────────────────────────────

record MovimientoDto(TipoMovimiento Tipo, int Cantidad);

enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

// ── Modelo ───────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

record class MovimientoDeProducto(
    int Id,
    int ProductoId,
    TipoMovimiento Tipo,
    int Cantidad,
    DateTime Fecha
);

// ── DbContext ────────────────────────────────────────────────────────────────────

class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> opt) : base(opt) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

// ── Repositorio ──────────────────────────────────────────────────────────────────

class CatalogoRepositorio
{
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public Producto? TraerProductoPorId(int id) => db.Productos.Find(id);

    public Producto CrearProducto(Producto p)
    {
        db.Productos.Add(p);
        db.SaveChanges();
        return p;
    }

    public bool ActualizarProducto(int id, Producto datos)
    {
        var p = db.Productos.Find(id);
        if (p is null) return false;

        db.Entry(p).CurrentValues.SetValues(datos);
        db.SaveChanges();
        return true;
    }

    public bool EliminarProducto(int id)
    {
        var p = db.Productos.Find(id);
        if (p is null) return false;

        db.Remove(p);
        db.SaveChanges();
        return true;
    }

    public List<Producto> TraerProductos() =>
        db.Productos.OrderBy(p => p.Id).ToList();

    public List<MovimientoDeProducto> TraerMovimientos(int id) =>
        db.Movimientos.Where(m => m.ProductoId == id)
                      .OrderByDescending(m => m.Fecha)
                      .ToList();

    public bool RegistrarMovimiento(int productoId, TipoMovimiento tipo, int cantidad)
    {
        var p = db.Productos.Find(productoId);
        if (p is null) return false;

        var nuevoStock = p.Stock;

        switch (tipo)
        {
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

        db.Entry(p).CurrentValues.SetValues(p with { Stock = nuevoStock });

        db.Movimientos.Add(new MovimientoDeProducto(
            0, productoId, tipo, cantidad, DateTime.Now));

        db.SaveChanges();
        return true;
    }

    public void Iniciar()
    {
        db.Database.EnsureCreated();

        if (!db.Productos.Any())
        {
            db.Productos.Add(new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100));
            db.SaveChanges();
        }
    }
}