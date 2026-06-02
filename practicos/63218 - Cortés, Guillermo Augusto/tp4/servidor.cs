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

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    return Results.Ok(repositorio.TraerProductos());
});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProductoPorId(id);

    return producto is null
        ? Results.NotFound()
        : Results.Ok(producto);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repositorio) => {
    var nuevo = repositorio.AgregarProducto(producto);
    return Results.Created($"/productos/{nuevo.Id}", nuevo);
});

app.MapPut("/productos/{id}", (int id, Producto producto, CatalogoRepositorio repositorio) => {
    var ok = repositorio.ModificarProducto(id, producto);

    return ok
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var ok = repositorio.EliminarProducto(id);

    return ok
        ? Results.NoContent()
        : Results.NotFound();
});

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(
    int Id,
    string Codigo, 
    string Nombre, 
    decimal Precio, 
    int Stock
);

enum TipoMovimiento {
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

    public DbSet<MovimientoDeProducto> Moviminetos => Set<MovimientoDeProducto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.AddRange(
            new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100),
            new Producto(2, "P002", "Azucar 1kg", 1200m, 50),
            new Producto(3, "P003", "Cafe 500g", 2500m, 30)   
            );
            db.SaveChanges();
        }
    }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
    public List<Producto> TraerProductos() =>
        db.Productos.OrderBy(p => p.Id).ToList();

    public Producto? TraerProductoPorId(int id) =>
        db.Productos.Find(id);

    public Producto AgregarProducto(Producto producto) {
        db.Productos.Add(producto);
        db.SaveChanges();
        return producto;
    }
    public bool ModificarProducto(int id, Producto datos) {
        var producto = db.Productos.Find(id);

        if (producto is null) return false;

        db.Entry(producto).CurrentValues.SetValues(datos);

        db.SaveChanges();

        return true;
    }

    public bool EliminarProducto(int id) {

        var producto = db.Productos.Find(id);

        if (producto is null)
            return false;

        db.Productos.Remove(producto);
        db.SaveChanges();

        return true;
    }
}