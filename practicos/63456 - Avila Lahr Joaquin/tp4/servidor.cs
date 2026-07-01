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
app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) =>
{
    var producto = repositorio.TraerProducto(id);

    return producto is null
        ? Results.NotFound()
        : Results.Ok(producto);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repositorio) =>
{
    var nuevo = repositorio.AgregarProducto(producto);
    return Results.Created($"/productos/{nuevo.Id}", nuevo);
});

app.MapPut("/productos/{id}", (int id, Producto producto, CatalogoRepositorio repositorio) =>
{
    producto.Id = id;
    return repositorio.ModificarProducto(id, producto)
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) =>
{
    return repositorio.EliminarProducto(id)
        ? Results.NoContent()
        : Results.NotFound();
});
app.MapGet("/productos/{productoId}/movimientos",
(int productoId, CatalogoRepositorio repositorio) =>
{
    return Results.Ok(
        repositorio.TraerMovimientos(productoId)
    );
});

app.MapPost("/productos/{productoId}/movimientos",
(int productoId,
 MovimientoDto datosMovimiento,
 CatalogoRepositorio repositorio) =>
{
    if (datosMovimiento.Cantidad <= 0)
        return Results.BadRequest("La cantidad debe ser positiva");
    return repositorio.RegistrarMovimiento(
        productoId,
        datosMovimiento.Tipo,
        datosMovimiento.Cantidad
    )
        ? Results.NoContent()
        : Results.NotFound();
});



app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto{
    public int Id {get;set;}
    public string Codigo {get;set;} = "";
    public string Nombre {get;set;} = "";
    public decimal Precio {get;set;}
    public int Stock {get;set;}
    }
enum TipoMovimiento
{
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

record MovimientoDto(
    TipoMovimiento Tipo,
    int Cantidad
);

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

    public void Iniciar()
{
    db.Database.EnsureCreated();

    if (!db.Productos.Any())
    {
        db.Productos.AddRange(
    new Producto {
        Id = 1,
        Codigo = "P001",
        Nombre = "Teclado Redragon",
        Precio = 45000m,
        Stock = 100
    },
    new Producto {
        Id = 2,
        Codigo = "P002",
        Nombre = "Mouse Logitech",
        Precio = 30000m,
        Stock = 50
    },
    new Producto {
        Id = 3,
        Codigo = "P003",
        Nombre = "Monitor LG",
        Precio = 100000m,
        Stock = 25
    }
);

        db.SaveChanges();
    }
}

public List<Producto> TraerProductos() =>
    db.Productos
      .OrderBy(p => p.Id)
      .ToList();
public Producto? TraerProducto(int id) =>
    db.Productos.Find(id);

public Producto AgregarProducto(Producto producto)
{
    db.Productos.Add(producto);
    db.SaveChanges();
    return producto;
}

public bool ModificarProducto(int id, Producto productoActualizado)
{
    var producto = db.Productos.Find(id);

    if (producto is null)
        return false;

   productoActualizado.Id = id;
db.Entry(producto).CurrentValues.SetValues(productoActualizado);
    db.SaveChanges();

    return true;
}

public bool EliminarProducto(int id)
{
    var producto = db.Productos.Find(id);

    if (producto is null)
        return false;

    db.Productos.Remove(producto);
    db.SaveChanges();

    return true;
}
public List<MovimientoDeProducto> TraerMovimientos(int productoId)
{
    return db.Movimientos
        .Where(movimiento => movimiento.ProductoId == productoId)
        .OrderByDescending(movimiento => movimiento.Fecha)
        .ToList();
}

public bool RegistrarMovimiento(
    
    int productoId,
    TipoMovimiento tipoMovimiento,
    int cantidadMovimiento)
{
     if (cantidadMovimiento <= 0)
        return false;
    var productoEncontrado = db.Productos.Find(productoId);

    if (productoEncontrado is null)
        return false;

    switch (tipoMovimiento)
    {
        case TipoMovimiento.Compra:
            productoEncontrado.Stock += cantidadMovimiento;
            break;

        case TipoMovimiento.Venta:
           if( productoEncontrado.Stock < cantidadMovimiento)
            return false;
             productoEncontrado.Stock -= cantidadMovimiento;
             break;

        case TipoMovimiento.Ajuste:
            productoEncontrado.Stock = cantidadMovimiento;
            break;
    }

    var nuevoMovimiento = new MovimientoDeProducto(
        0,
        productoId,
        tipoMovimiento,
        cantidadMovimiento,
        DateTime.Now
    );

    db.Movimientos.Add(nuevoMovimiento);

    db.SaveChanges();

    return true;
}}