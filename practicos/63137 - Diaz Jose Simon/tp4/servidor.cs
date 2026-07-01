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

// ── Endpoints de Productos ────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoRepositorio repo) =>
    Results.Ok(repo.ListarProductos()));

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    var producto = repo.ObtenerProducto(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapPost("/productos", (ProductoDatos datos, CatalogoRepositorio repo) => {
    var producto = repo.CrearProducto(datos);
    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id}", (int id, ProductoDatos datos, CatalogoRepositorio repo) => {
    var producto = repo.ModificarProducto(id, datos);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    var eliminado = repo.EliminarProducto(id);
    return eliminado ? Results.NoContent() : Results.NotFound();
});

// ── Endpoints de Movimientos ──────────────────────────────────────────────

app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoRepositorio repo) => {
    if (repo.ObtenerProducto(productoId) is null) return Results.NotFound();
    return Results.Ok(repo.ListarMovimientos(productoId));
});

app.MapPost("/productos/{productoId}/movimientos", (int productoId, MovimientoDatos datos, CatalogoRepositorio repo) => {
    var movimiento = repo.RegistrarMovimiento(productoId, datos);
    if (movimiento is null) return Results.NotFound();
    return Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento);
});

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

    public List<Producto> ListarProductos() =>
        db.Productos.OrderBy(p => p.Codigo).ToList();

    public Producto? ObtenerProducto(int id) =>
        db.Productos.Find(id);

    public Producto CrearProducto(ProductoDatos datos) {
        var producto = new Producto {
            Codigo = datos.Codigo.Trim(),
            Nombre = datos.Nombre.Trim(),
            Precio = datos.Precio,
            Stock  = datos.Stock
        };
        db.Productos.Add(producto);
        db.SaveChanges();
        return producto;
    }

    public Producto? ModificarProducto(int id, ProductoDatos datos) {
        var producto = db.Productos.Find(id);
        if (producto is null) return null;

        producto.Codigo = datos.Codigo.Trim();
        producto.Nombre = datos.Nombre.Trim();
        producto.Precio = datos.Precio;
        producto.Stock  = datos.Stock;
        db.SaveChanges();
        return producto;
    }

    public bool EliminarProducto(int id) {
        var producto = db.Productos.Find(id);
        if (producto is null) return false;

        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientos(int productoId) =>
        db.Movimientos
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToList();

    public MovimientoDeProducto? RegistrarMovimiento(int productoId, MovimientoDatos datos) {
        var producto = db.Productos.Find(productoId);
        if (producto is null) return null;

        producto.Stock = CalcularNuevoStock(producto.Stock, datos.Tipo, datos.Cantidad);

        var movimiento = new MovimientoDeProducto {
            ProductoId = productoId,
            Tipo       = datos.Tipo,
            Cantidad   = datos.Cantidad,
            Fecha      = DateTime.Now
        };

        db.Movimientos.Add(movimiento);
        db.SaveChanges();
        return movimiento;
    }

    static int CalcularNuevoStock(int stockActual, TipoMovimiento tipo, int cantidad) =>
        tipo switch {
            TipoMovimiento.Compra => stockActual + cantidad,
            TipoMovimiento.Venta  => stockActual - cantidad,
            TipoMovimiento.Ajuste => cantidad,
            _                     => stockActual
        };
}