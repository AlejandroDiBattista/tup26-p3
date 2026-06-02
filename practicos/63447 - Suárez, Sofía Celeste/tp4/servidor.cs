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

app.MapGet("/producto", (CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProducto();
    if(producto is null) return Results.NotFound();

    return Results.Ok(producto);
});

app.MapPost("/movimiento", (MovimientoDto movimiento, CatalogoRepositorio repositorio) => {
    repositorio.Registrarmovimiento(movimiento);
    return Results.Ok(movimiento);
});
app.MapPut("/producto", (Producto productoDto, CatalogoRepositorio repositorio) => {
    repositorio.ActualizarProducto(productoDto);
    return Results.Ok();
});
app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) => repositorio.ObtenerPorId(id));
app.MapPost("/productos", (Producto p, CatalogoRepositorio repositorio) => repositorio.AgregarProducto(p));
app.MapPut("/productos/{id}", (int id, Producto p, CatalogoRepositorio repositorio) => repositorio.ActualizarProducto(p));
app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) => repositorio.EliminarProducto(id));
app.MapGet("/productos/{id}/movimientos", (int id, CatalogoRepositorio repo) => repo.ObtenerMovimientos(id));

app.MapPost("/productos/{productoId}/movimientos", (int productoId, MovimientoDto movimiento, CatalogoRepositorio repositorio) => {
    repositorio.Registrarmovimiento(movimiento with { ProductoId = productoId });
    return Results.Ok();
});

app.Run("http://localhost:5050");

// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public enum TipoMovimiento{Compra, Venta}
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
     public DbSet<MovimientoDto> Movimientos => Set<MovimientoDto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

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