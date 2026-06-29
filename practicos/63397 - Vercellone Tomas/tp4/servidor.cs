#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@9.*
#:package Microsoft.EntityFrameworkCore.Design@9.*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

// ── Configuración del servidor ────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CatalogoDb>(opt =>
    opt.UseSqlite("Data Source=catalogo.db"));
var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();
    if (!db.Productos.Any()) {
        db.Productos.AddRange(
            new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g",   Precio = 1500m, Stock = 100 },
            new Producto { Codigo = "P002", Nombre = "Café Molido 250g",  Precio = 2200m, Stock = 50  },
            new Producto { Codigo = "P003", Nombre = "Azúcar 1kg",        Precio = 800m,  Stock = 200 }
        );
        db.SaveChanges();
    }
}

// ── Endpoints de Productos ─────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoDb db) =>
    Results.Ok(db.Productos.ToList()));

app.MapGet("/productos/{id}", (int id, CatalogoDb db) => {
    var producto = db.Productos.Find(id);
    if (producto is null) return Results.NotFound();
    return Results.Ok(producto);
});

app.MapPost("/productos", (Producto nuevo, CatalogoDb db) => {
    bool codigoRepetido = db.Productos.Any(p => p.Codigo == nuevo.Codigo);
    if (codigoRepetido) return Results.Conflict("Ya existe un producto con ese código.");
    db.Productos.Add(nuevo);
    db.SaveChanges();
    return Results.Created($"/productos/{nuevo.Id}", nuevo);
});

app.MapPut("/productos/{id}", (int id, Producto datos, CatalogoDb db) => {
    var producto = db.Productos.Find(id);
    if (producto is null) return Results.NotFound();
    producto.Codigo  = datos.Codigo;
    producto.Nombre  = datos.Nombre;
    producto.Precio  = datos.Precio;
    producto.Stock   = datos.Stock;
    db.SaveChanges();
    return Results.Ok(producto);
});

app.MapDelete("/productos/{id}", (int id, CatalogoDb db) => {
    var producto = db.Productos.Find(id);
    if (producto is null) return Results.NotFound();
    var movimientos = db.Movimientos.Where(m => m.ProductoId == id).ToList();
    db.Movimientos.RemoveRange(movimientos);
    db.Productos.Remove(producto);
    db.SaveChanges();
    return Results.NoContent();
});

// ── Endpoints de Movimientos ───────────────────────────────────────────────

app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoDb db) => {
    bool existe = db.Productos.Any(p => p.Id == productoId);
    if (!existe) return Results.NotFound();
    var movimientos = db.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToList();
    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId}/movimientos", (int productoId, MovimientoRequest req, CatalogoDb db) => {
    var producto = db.Productos.Find(productoId);
    if (producto is null) return Results.NotFound();

  
    int cantidadReal;
    if (req.Tipo == TipoMovimiento.Compra) {
        cantidadReal = req.Cantidad;        
        producto.Stock += cantidadReal;
    } else if (req.Tipo == TipoMovimiento.Venta) {
        cantidadReal = -req.Cantidad;          
        producto.Stock += cantidadReal;
    } else {
       
        cantidadReal = req.Cantidad - producto.Stock;
        producto.Stock = req.Cantidad;
    }

    var movimiento = new MovimientoDeProducto {
        ProductoId = productoId,
        Tipo       = req.Tipo,
        Cantidad   = cantidadReal,
        Fecha      = DateTime.Now
    };
    db.Movimientos.Add(movimiento);
    db.SaveChanges();
    return Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento);
});

app.Run("http://localhost:5050");

// ── Modelos ───────────────────────────────────────────────────────────────

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


record MovimientoRequest(TipoMovimiento Tipo, int Cantidad);

// ── DbContext ──────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto>             Productos   => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}