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

app.MapGet("/productos", (CatalogoRepositorio repositorio) =>
{
    return Results.Ok(repositorio.TraerTodos());
});

app.MapGet("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) =>
{
    var producto = repositorio.TraerPorId(id);

    return producto is null
        ? Results.NotFound()
        : Results.Ok(producto);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repositorio) =>
{
    var creado = repositorio.Agregar(producto);

    return Results.Created($"/productos/{creado.Id}", creado);
});

app.MapPut("/productos/{id:int}", (int id, Producto producto, CatalogoRepositorio repositorio) =>
{
    var actualizado = repositorio.Modificar(id, producto);

    return actualizado
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapDelete("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) =>
{
    var eliminado = repositorio.Eliminar(id);

    return eliminado
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapGet(
    "/productos/{productoId:int}/movimientos",
    (int productoId, CatalogoRepositorio repositorio) =>
{
    return Results.Ok(
        repositorio.TraerMovimientos(productoId));
});

app.MapPost(
    "/productos/{productoId:int}/movimientos",
    (
        int productoId,
        MovimientoRequest movimiento,
        CatalogoRepositorio repositorio
    ) =>
{
    var ok = repositorio.RegistrarMovimiento(
        productoId,
        movimiento.Tipo,
        movimiento.Cantidad);

    return ok
        ? Results.Ok()
        : Results.BadRequest("No se pudo registrar el movimiento");
});

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

class Producto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }
}

enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

class MovimientoDeProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    public Producto? Producto { get; set; }
}

record MovimientoRequest(
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
                new Producto
                {
                    Codigo = "P001",
                    Nombre = "Yerba Mate 500g",
                    Precio = 1500m,
                    Stock = 100
                },
                new Producto
                {
                    Codigo = "P002",
                    Nombre = "Azúcar 1Kg",
                    Precio = 1200m,
                    Stock = 50
                }
            );

            db.SaveChanges();
        }
    }

    public List<Producto> TraerTodos()
    {
        return db.Productos
            .OrderBy(p => p.Id)
            .ToList();
    }

    public Producto? TraerPorId(int id)
    {
        return db.Productos.Find(id);
    }

    public Producto Agregar(Producto producto)
    {
        db.Productos.Add(producto);
        db.SaveChanges();

        return producto;
    }

    public bool Modificar(int id, Producto datos)
    {
        var producto = db.Productos.Find(id);

        if (producto is null)
            return false;

        producto.Codigo = datos.Codigo;
        producto.Nombre = datos.Nombre;
        producto.Precio = datos.Precio;
        producto.Stock = datos.Stock;

        db.SaveChanges();

        return true;
    }

    public bool Eliminar(int id)
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
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToList();
    }

    public bool RegistrarMovimiento(
    int productoId,
    TipoMovimiento tipo,
    int cantidad)
    {
    var producto = db.Productos.Find(productoId);

    if (producto is null)
        return false;

    switch (tipo)
    {
        case TipoMovimiento.Compra:
            producto.Stock += cantidad;
            break;

        case TipoMovimiento.Venta:

            if (producto.Stock < cantidad)
                return false;

            producto.Stock -= cantidad;
            break;

        case TipoMovimiento.Ajuste:
            producto.Stock = cantidad;
            break;
    }

    db.Movimientos.Add(new MovimientoDeProducto
    {
        ProductoId = productoId,
        Tipo = tipo,
        Cantidad = cantidad,
        Fecha = DateTime.Now
    });

    db.SaveChanges();

    return true;
    }
}