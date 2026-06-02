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
    if (producto is null) return Results.NotFound();

    return Results.Ok(producto);
});

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

enum TipoMovimiento { Compra, Venta, Ajuste }

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}


record ProductoNuevoDto(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoNuevoDto(TipoMovimiento Tipo, int Cantidad);


// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    public override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();
    }
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Productos_Codigo
            ON Productos (Codigo)
            """);

        if (!db.Productos.Any()) {
            db.Productos.AddRange(
                new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 }
            );
            db.SaveChanges();
        }
    }

    public List<Producto> TraerProductos() => db.Productos.OrderBy(p => p.Codigo).ToList();

    public Producto? ObtenerProducto(int id) => db.Productos.Find(id);

    public Producto? CrearProducto(ProductoNuevoDto dto) {
        if (CodigoEnUso(dto.Codigo)) return null;

        var producto = new Producto {
            Codigo = dto.Codigo.Trim(),
            Nombre = dto.Nombre.Trim(),
            Precio = dto.Precio,
            Stock = dto.Stock,
        };
        db.Productos.Add(producto);
        db.SaveChanges();
        return producto;
    }

    public Producto? ModificarProducto(int id, ProductoNuevoDto dto) {
        var producto = db.Productos.Find(id);
        if (producto is null) return null;
        if (CodigoEnUso(dto.Codigo, id)) return null;

        producto.Codigo = dto.Codigo.Trim();
        producto.Nombre = dto.Nombre.Trim();
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        db.SaveChanges();
        return producto;
    }

    public bool EliminarProducto(int id) {
        var producto = db.Productos.Find(id);
        if (producto is null) return false;

        db.Movimientos.RemoveRange(db.Movimientos.Where(m => m.ProductoId == id));
        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }

    public bool CodigoEnUso(string codigo, int? exceptoId = null) =>
        db.Productos.Any(p => p.Codigo == codigo.Trim() && (!exceptoId.HasValue || p.Id != exceptoId.Value));

}