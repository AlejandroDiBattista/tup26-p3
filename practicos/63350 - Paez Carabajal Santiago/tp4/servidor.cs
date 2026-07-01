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

// 1- GET/PRODUCTOS (listar todos) 
app.MapGet("/Productos", async (CatalogoDb db)=> await db.Productos.ToListAsync());

// 2- GET/PRODUCTO/{ID} (obtener pedido por id) 
app.MapGet("/productos/{id:int}", async (int id, CatalogoDb db)=> await db.Productos.FindAsync(id) is Producto p ? Results.Ok(p) : Results.NotFound());

// 3- POST/PRODUCTOS (crear producto) 
app.MapPost("/productos", async (Producto producto, CatalogoDb db)=> { db.Productos.Add(producto);
await db.SaveChangesAsync();
return Results.Created($"/productos/{producto.Id}", producto);});

// 4- PUT/PRODUCTOS/{id} (modificar productos)
app.Map("/prouctos/{id:int}", async (int id, Producto InputProducto, CatalogoDb db)=> {
    var producto = await db.Productos.FindAsync(id);
    if (producto is null) return Results.NotFound();

    // Como es un record, usamos 'with' para crear la versión actualizada y reemplazarla en el contexto
    var productoActualizado = producto with {
        Codigo = InputProducto.Codigo,
        Nombre = InputProducto.Nombre,
        Precio = InputProducto.Precio,
        Stock = InputProducto.Stock
    };

    db.Entry(producto).CurrentValues.SetValues(productoActualizado);
    await db.SaveChangesAsync();
    return Results.NoContent();
    });

// 5- DELETE/PRODUCTOS/ {id} (eliminar producto)
app.MapDelete("/productos/{id:int}", async (int id, CatalogoDb db) => { if (await db.Productos.FindAsync(id) is Producto producto) {
    db.Productos.Remove(producto);
    await db.SaveChangesAsync();
    return Results.NoContent();
    }
    return Results.NotFound();
});

// 6- GET/ PRODUCTOS /{productoId}/MOVIMIENTO (itorial de un producto)
app.MapGet ("/productos/{productoId:int}/movimiento", async(int productoId, CatalogoDb db) => {
    var movimientos = await db.Movimientos
    .Where(m => m.ProductoId == productoId)
    .OrderByDescending(m => m.Fecha)
    .ToListAsync();
    return Results.Ok(movimientos);
});

// 7- POST /PRODUCTOS/{productoId}/MOVIMIENTOS (registrar movimientos y actualizar stock)
app.MapPost("/productos/{productoId:int}/ movimientos", async (int productoId, MovimientoDeProducto nuevoMovimiento, CatalogoDb db)=> {
    var producto = await db.Productos.FindAsync(productoId);
    if (producto is null) return Results.NotFound("Porducto no encontrado.");

    //calculamos el nuevo stock segun la regla de negocio
    int nuevoStock = producto.Stock;
    if (nuevoMovimiento.Tipo == TipoMovimiento.Compra) nuevoStock += nuevoMovimiento.Cantidad;
    else if (nuevoMovimiento.Tipo == TipoMovimiento.Venta) nuevoStock -= nuevoMovimiento.Cantidad;
    else if (nuevoMovimiento.Tipo == TipoMovimiento.Ajuste) nuevoStock = nuevoMovimiento.Cantidad;

    // actualizamos el producto usando with
    var productoActualizado = producto with {Stock = nuevoStock};
    db.Entry(producto).CurrentValues.SetValues(productoActualizado);

    //creamos el movimiento con la fecha del servidor y el id correcto
    var movimientoFinal = nuevoMovimiento with { ProductoId = productoId, Fecha = DateTime.Now};
    db.Movimientos.Add(movimientoFinal);

    await db.SaveChangesAsync();
    return Results.Created($"/productos/{productoId}/movimientos/{movimientoFinal.id}", movimientoFinal);
});

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

enum TipoMovimiento {Compra, Venta, Ajuste}
record class MovimientoDeProducto(int id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet <MovimientoDeProducto> Movimientos  => Set<MovimientoDeProducto>();
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