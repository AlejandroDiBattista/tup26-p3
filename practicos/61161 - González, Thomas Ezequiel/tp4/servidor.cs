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

app.MapGet("/productos", (CatalogoRepositorio repositorio) =>
{
    return repositorio.TraerProductos();
});

app.Run("http://localhost:5050");
// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, 
 string Codigo, 
string Nombre, 
 decimal Precio, 
int Stock);
enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

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
    public DbSet<MovimientoDeProducto> Movimientos =>
    Set<MovimientoDeProducto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;
    public List<Producto> TraerProductos()
{
    return db.Productos
        .OrderBy(p => p.Nombre)
        .ToList();
}
    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any())
{
    db.Productos.Add(new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100));
    db.Productos.Add(new Producto(2, "P002", "Azúcar 1kg", 900m, 50));
    db.Productos.Add(new Producto(3, "P003", "Café 250g", 3200m, 20));

    db.SaveChanges();
}
        

        if (!db.Productos.Any()) {
            db.Productos.Add(new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100));
            db.SaveChanges();
        }
    }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}