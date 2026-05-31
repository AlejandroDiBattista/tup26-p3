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

app.MapGet("/productos", async (CatalogoRepositorio repo) =>
    Results.Ok(await repo.ListarProductosAsync()));

app.MapGet("/productos/{id:int}", async (int id, CatalogoRepositorio repo) => {
    var producto = await repo.BuscarProductoAsync(id);
    return producto is null ? Results.NotFound("Producto no encontrado.") : Results.Ok(producto);
});

app.Run("http://localhost:5050");

// ── Modelo ────────────────────────────────────────────────────────────────
public class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.AddRange(
            new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
            new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 900m, Stock = 60 },
            new Producto { Codigo = "P003", Nombre = "Cafe 250g", Precio = 3200m, Stock = 30 }
        );
            db.SaveChanges();
        }
    }

    public async Task<List<Producto>> ListarProductosAsync() =>
        await db.Productos
            .OrderBy(p => p.Codigo)
            .ToListAsync();

    public async Task<Producto?> BuscarProductoAsync(int id) =>
        await db.Productos.FindAsync(id);

    public async Task<bool> ExisteProductoAsync(int id) =>
        await db.Productos.AnyAsync(p => p.Id == id);
}

public class ProductoDatos {
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}