#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

// ── Configuración ────//

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(
    opt => opt.UseSqlite("Data Source=catalogo.db"));

builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

// ── Inicialización de la base de datos ───//

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider
        .GetRequiredService<CatalogoRepositorio>();

    repositorio.Iniciar();
}


// ── endpoints de productos ─────//

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    var productos = repositorio.TraerTodos();
    return Results.Ok(productos);
});

app.MapGet("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerPorId(id);

    if (producto is null) {
        return Results.NotFound();
    }

    return Results.Ok(producto);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repositorio) => {
    if (string.IsNullOrWhiteSpace(producto.Codigo))
        return Results.BadRequest("El código es obligatorio");

    if (string.IsNullOrWhiteSpace(producto.Nombre))
        return Results.BadRequest("El nombre es obligatorio");

    if (producto.Precio <= 0)
        return Results.BadRequest("El precio debe ser mayor que cero");

    var nuevo = repositorio.Agregar(producto);

    return Results.Created($"/productos/{nuevo.Id}", nuevo);
});

app.MapPut("/productos/{id:int}",
(int id, Producto datos, CatalogoRepositorio repositorio) => {

    var producto = repositorio.Modificar(id, datos);

    if (producto is null) {
        return Results.NotFound();
    }

    return Results.Ok(producto);
});

app.MapDelete("/productos/{id:int}",
(int id, CatalogoRepositorio repositorio) => {

    var eliminado = repositorio.Eliminar(id);

    if (!eliminado) {
        return Results.NotFound();
    }

    return Results.NoContent();
});

// ── endpoints de los movimientos ───//

app.MapGet("/productos/{productoId:int}/movimientos",
(int productoId, CatalogoRepositorio repositorio) => {

    var movimientos = repositorio.TraerMovimientos(productoId);

    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos",
(int productoId,
 MovimientoDeProducto movimiento,
 CatalogoRepositorio repositorio) => {

    var nuevoMovimiento =
        repositorio.RegistrarMovimiento(productoId, movimiento);

    if (nuevoMovimiento is null) {
        return Results.NotFound();
    }

    return Results.Created(
        $"/productos/{productoId}/movimientos/{nuevoMovimiento.Id}",
        nuevoMovimiento);
});

app.Run("http://localhost:5050");



// ── Modelos ───────//

public class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

public enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

public class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

// ── DbContext ───────────//

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options)
        : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoDeProducto> Movimientos  => Set<MovimientoDeProducto>();
}

// ── Repositorio ───────//

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) {
        this.db = db;
    }

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.Add(new Producto {
                Id = 1,
                Codigo = "P001",
                Nombre = "Yerba Mate 500g",
                Precio = 1500m,
                Stock = 100
            });

            db.SaveChanges();
        }
    }

    public List<Producto> TraerTodos() {
        return db.Productos.ToList();
    }

    public Producto? TraerPorId(int id) {
        return db.Productos.FirstOrDefault(p => p.Id == id);
    }

    public Producto Agregar(Producto producto) {
        db.Productos.Add(producto);
        db.SaveChanges();

        return producto;
    }

    public Producto? Modificar(int id, Producto datos) {
        var producto = db.Productos.FirstOrDefault(p => p.Id == id);

        if (producto is null) {
            return null;
        }

        producto.Codigo = datos.Codigo;
        producto.Nombre = datos.Nombre;
        producto.Precio = datos.Precio;
        producto.Stock = datos.Stock;

        db.SaveChanges();

        return producto;
    }

    public bool Eliminar(int id) {
        var producto = db.Productos.FirstOrDefault(p => p.Id == id);

        if (producto is null) {
            return false;
        }

        db.Productos.Remove(producto);
        db.SaveChanges();

        return true;
    }

    public List<MovimientoDeProducto> TraerMovimientos(int productoId) {
        return db.Movimientos
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToList();
    }

    public MovimientoDeProducto? RegistrarMovimiento(
        int productoId,
        MovimientoDeProducto movimiento) {

        var producto = db.Productos
            .FirstOrDefault(p => p.Id == productoId);

        if (producto is null) {
            return null;
        }

        movimiento.ProductoId = productoId;
        movimiento.Fecha = DateTime.Now;

        switch (movimiento.Tipo) {
            case TipoMovimiento.Compra:
                producto.Stock += movimiento.Cantidad;
                break;

            case TipoMovimiento.Venta:
                producto.Stock -= movimiento.Cantidad;
                break;

            case TipoMovimiento.Ajuste:
                producto.Stock = movimiento.Cantidad;
                break;
        }

        db.Movimientos.Add(movimiento);

        db.SaveChanges();

        return movimiento;
    }
}
