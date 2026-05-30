using Microsoft.EntityFrameworkCore;

// ── Configuración ──────────────────────────────────────────────────────────
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CatalogoDb>(opt =>
    opt.UseSqlite("Data Source=catalogo.db"));

builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repo.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────
app.MapGet("/", () => "CatalogoREST funcionando");
app.MapGet("/productos", async (CatalogoRepositorio repo) =>
{
    return Results.Ok(await repo.ListarProductos());
});

app.MapGet("/productos/{id:int}", async (int id, CatalogoRepositorio repo) =>
{
    Producto? producto = await repo.BuscarProducto(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapGet("/producto", (CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProducto();
    if(producto is null) return Results.NotFound();

    return Results.Ok(producto);
});

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
app.Run("http://localhost:5051");
}

// ── Repositorio ───────────────────────────────────────────────────────────
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

    [JsonIgnore]
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}

class MovimientoDeProducto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }

    [JsonIgnore]
    public Producto? Producto { get; set; }
}
class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options)
    {
    }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasIndex(p => p.Codigo).IsUnique();
            entity.Property(p => p.Codigo).IsRequired();
            entity.Property(p => p.Nombre).IsRequired();
            entity.Property(p => p.Precio).HasPrecision(18, 2);
        });

        modelBuilder.Entity<MovimientoDeProducto>(entity =>
        {
            entity.Property(m => m.Tipo).HasConversion<string>();

            entity.HasOne(m => m.Producto)
                .WithMany(p => p.Movimientos)
                .HasForeignKey(m => m.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

class CatalogoRepositorio
{
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db)
    {
        this.db = db;
    }

    public void Iniciar()
    {
        db.Database.EnsureCreated();

        if (!db.Productos.Any())
        {
            db.Productos.AddRange(
              new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 900m, Stock = 60 },
              new Producto { Codigo = "P003", Nombre = "Harina 000 1kg", Precio = 750m, Stock = 80 },
              new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 }
            );

            db.SaveChanges();
        }
    }
    public Task<List<Producto>> ListarProductos()
    {
        return db.Productos
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public Task<Producto?> BuscarProducto(int id)
    {
        return db.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}