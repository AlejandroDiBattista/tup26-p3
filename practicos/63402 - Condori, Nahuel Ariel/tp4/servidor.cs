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

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

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
            db.Productos.Add(new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100));
            db.SaveChanges();
        }
    }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

class catalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<catalogoDb> options) : base(options) { }
    public DbSet<producto> Productos => Set<producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    var productos = repositorio.TraerProductos();
    return Results.Ok(productos);
});

app.MapPost("/productos", (CatalogoRepositorio repositorio, producto nuevoProducto) => {
    repositorio.AgregarProducto(nuevoProducto);
    return Results.Created($"/productos/{nuevoProducto.Id}", nuevoProducto);
});

app.MapPut("/productos/{id:int}", (CatalogoRepositorio repositorio, int id, producto productoActualizado) => {
    repositorio.ActualizarProducto(id, productoActualizado);
    return Results.Ok(productoActualizado);
});

app.MapDelete("/productos/{id:int}", (CatalogoRepositorio repositorio, int id) => {
    repositorio.EliminarProducto(id);
    return Results.NoContent();
});
