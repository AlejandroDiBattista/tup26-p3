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

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

class Producto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public List<MovimientoProducto> Movimientos { get; set; } = new();
}

class MovimientoProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    public Producto? Producto { get; set; }
}

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options)
        : base(options)
    {
    }

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoProducto> Movimientos => Set<MovimientoProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>()
            .HasIndex(x => x.Codigo)
            .IsUnique();
    }
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
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

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}