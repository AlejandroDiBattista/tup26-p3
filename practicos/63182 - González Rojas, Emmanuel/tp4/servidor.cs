#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@9.*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
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
app.MapGet("/productos", (CatalogoRepositorio repositorio) =>
    repositorio.TraerProductos());
    
app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProducto(id);
    if (producto is null) return Results.NotFound();
    return Results.Ok(producto);
});

app.Run("http://localhost:5050");

//Modelo de datos
enum TipoMovimiento { Compra, Venta, Ajuste }
class Producto {
    public int     Id     { get; set; }
    public string  Codigo { get; set; } = "";
    public string  Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int     Stock  { get; set; }
}

class MovimientoDeProducto {
    public int            Id         { get; set; }
    public int            ProductoId { get; set; }
    public TipoMovimiento Tipo       { get; set; }
    public int            Cantidad   { get; set; }
    public DateTime       Fecha      { get; set; }
}

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto>             Productos   => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo).IsUnique();
        modelBuilder.Entity<MovimientoDeProducto>()
            .Property(m => m.Tipo).HasConversion<string>();
    }
}

//Repositorio
    class CatalogoRepositorio {
        private readonly CatalogoDb db;
        public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
            db.Database.EnsureCreated();
            if (!db.Productos.Any()) {
                    db.Productos.Add(new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 });
                    db.Productos.Add(new Producto { Codigo = "P002", Nombre = "Mate Cocido x20", Precio = 1400m,  Stock = 50  });
                    db.Productos.Add(new Producto { Codigo = "P003", Nombre = "Azucar x 1kg",   Precio = 900m, Stock = 30  });
                    db.SaveChanges();
            }
        }


        public List<Producto> TraerProductos() => db.Productos.OrderBy(p => p.Id).ToList();
        public Producto? TraerProducto(int id) =>db.Productos.FirstOrDefault(p => p.Id == id);

        public bool ExisteCodigo(string codigo, int excluirId = 0) => db.Productos.Any(p => p.Codigo == codigo && p.Id != excluirId);
        
        public void AgregarProducto(Producto producto) {
            if (ExisteCodigo(producto.Codigo)) throw new Exception("El código ya existe.");
            db.Productos.Add(producto);
            db.SaveChanges();
        }

        public void ModificarProducto(int id, Producto input) {
        var producto = db.Productos.First(p => p.Id == id);
        producto.Codigo = input.Codigo;
        producto.Nombre = input.Nombre;
        producto.Precio = input.Precio;
        producto.Stock  = input.Stock;
        db.SaveChanges();
    }
        public void EliminarProducto(int id) {
            var movimientos = db.Movimientos.Where(m => m.ProductoId == id).ToList();
            db.Movimientos.RemoveRange(movimientos);
            db.Productos.Remove(db.Productos.First(p => p.Id == id));
            db.SaveChanges();
        }
            
    public List<MovimientoDeProducto> TraerMovimientos(int productoId) => db.Movimientos.Where(m => m.ProductoId == productoId)
    .OrderByDescending(m => m.Fecha)
    .ToList();
    
    public void RegistrarMovimientos(int productiId, MovimientoDeProducto movimiento) {
        var producto = db.Productos.First(p => p.Id == productiId);
        
        switch (movimiento.Tipo) {
            case TipoMovimiento.Compra:producto.Stock += movimiento.Cantidad;
                break;
            case TipoMovimiento.Venta:
                if (producto.Stock < movimiento.Cantidad) throw new Exception("Stock insuficiente."); producto.Stock -= movimiento.Cantidad;
                break;
            case TipoMovimiento.Ajuste: producto.Stock = movimiento.Cantidad;
                break;
        }

        movimiento.ProductoId = productiId;
        movimiento.Fecha = DateTime.Now;
        db.Movimientos.Add(movimiento);
        db.SaveChanges();
    }
    
    }