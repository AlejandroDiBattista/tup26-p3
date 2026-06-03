#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt =>
    opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope()) {
    scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>().Iniciar();
}

app.Run("http://localhost:5050");

// ── Enumeraciones ─────────────────────────────────────────────────────────

enum TipoMovimiento { Compra, Venta, Ajuste }

// ── Modelo ────────────────────────────────────────────────────────────────

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

// ── DTOs de entrada ───────────────────────────────────────────────────────

record ProductoDatos(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDatos(TipoMovimiento Tipo, int Cantidad);

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<MovimientoDeProducto>()
            .HasOne<Producto>()
            .WithMany()
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();
        if (db.Productos.Any()) return;

        db.Productos.AddRange(
            new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g",  Precio = 1500m, Stock = 100 },
            new Producto { Codigo = "P002", Nombre = "Azucar 1kg",       Precio =  900m, Stock =  50 },
            new Producto { Codigo = "P003", Nombre = "Harina 000 1kg",   Precio =  750m, Stock =  80 }
        );
        db.SaveChanges();
    }
}