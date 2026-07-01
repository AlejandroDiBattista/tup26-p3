#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();

builder.Services.ConfigureHttpJsonOptions(opt =>
opt.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoRepositorio repo) =>
    Results.Ok(repo.TraerProductos()));

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    var producto = repo.TraerProducto(id);
    if (producto is null) return Results.NotFound();
    return Results.Ok(producto);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repo) => {
    var creado = repo.Crear(producto);
    return Results.Created($"/productos/{creado.Id}", creado);
});

app.MapPut("/productos/{id}", (int id, Producto datos, CatalogoRepositorio repo) => {
    if (!repo.Modificar(id, datos)) return Results.NotFound();
    return Results.NoContent();
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    if (!repo.Eliminar(id)) return Results.NotFound();
    return Results.NoContent();
});

app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoRepositorio repo) =>
    Results.Ok(repo.TraerMovimientos(productoId)));

app.MapPost("/productos/{productoId}/movimientos", (int productoId, MovimientoDto dto, CatalogoRepositorio repo) => {
    var movimiento = repo.RegistrarMovimiento(productoId, dto.Tipo, dto.Cantidad);
    if (movimiento is null) return Results.NotFound();
    return Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento);
});

app.Run("http://localhost:5050");


// ── Modelo ────────────────────────────────────────────────────────────────

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}

enum TipoMovimiento { Compra, Venta, Ajuste }

class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
    public Producto Producto { get; set; } = null!;
}

record MovimientoDto(TipoMovimiento Tipo, int Cantidad); //dto

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.Add(new Producto {Id = 1, Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100});
            db.SaveChanges();
        }
    }
    public List<Producto> TraerProductos() =>
    db.Productos.OrderBy(p => p.Codigo).ToList();

    public Producto? TraerProducto(int id) =>
    db.Productos.Find(id);

    public Producto Crear(Producto producto) {
        db.Productos.Add(producto);
        db.SaveChanges();
        return producto;
    }

    public bool Modificar(int id, Producto datos) {
        var producto = db.Productos.Find(id);
        if (producto is null) return false;

        producto.Codigo = datos.Codigo;
        producto.Nombre = datos.Nombre;
        producto.Precio = datos.Precio;
        producto.Stock = datos.Stock;

        db.SaveChanges();
        return true;
    }

    public bool Eliminar(int id) {
        var producto = db.Productos.Find(id);
        if (producto is null) return false;

        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> TraerMovimientos(int productoId) =>
    db.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToList();

    public MovimientoDeProducto? RegistrarMovimiento(int productoId, TipoMovimiento tipo, int cantidad) {
        var producto = db.Productos.Find(productoId);
        if (producto is null) return null;

        if (tipo == TipoMovimiento.Compra) producto.Stock += cantidad;
        else if (tipo == TipoMovimiento.Venta) producto.Stock -= cantidad;
        else producto.Stock = cantidad;

        var movimiento = new MovimientoDeProducto {
        ProductoId = productoId,
        Tipo = tipo,
        Cantidad = cantidad,
        Fecha = DateTime.Now,
    };
    db.Movimientos.Add(movimiento);

    db.SaveChanges();
    return movimiento;
}
}