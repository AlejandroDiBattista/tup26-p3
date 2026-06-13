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

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    return Results.Ok(repositorio.TraerProductos());

});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProductoPorId(id);

    if (producto is null) return Results.NotFound();

    return Results.Ok(producto);
});
app.MapPost("/productos", (Producto producto, CatalogoRepositorio repositorio) => {
    var nuevoAgregado = repositorio.AgregarProducto(producto);

    return Results.Created($"/productos/{nuevoAgregado.Id}", nuevoAgregado);
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var eliminado = repositorio.EliminarProducto(id);

     if(!eliminado) return Results.NotFound();

     return Results.NoContent();
});

app.MapPut("/productos/{id}", (int id, Producto producto, CatalogoRepositorio repositorio) => {
    var modificado = repositorio.ModificarProducto(id, producto);

    if (!modificado) return Results.NotFound();

    return Results.NoContent();
});

app.MapGet("/productos/{productoId}/movimientos",(int productoId, CatalogoRepositorio repositorio) => {
    return Results.Ok(repositorio.TraerMovimientos(productoId));
});

app.MapPost("/productos/{productoId}/movimientos", (int productoId, TipoMovimiento tipo, int cantidad, CatalogoRepositorio repositorio) => {
    var registrado = repositorio.RegistrarMovimiento(productoId, tipo, cantidad);

    if (!registrado) return Results.NotFound();

    return Results.Ok();

});
 
app.Run("http://localhost:5050");

// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
enum TipoMovimiento{
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
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
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

    public List<Producto> TraerProductos() {
        return db.Productos
            .OrderBy(p => p.Nombre)
            .ToList();
    }
    
    public Producto? TraerProductoPorId(int id) {
      return db.Productos.FirstOrDefault( p => p.Id == id);
    }
    
    public Producto AgregarProducto(Producto producto) {
        db.Productos.Add(producto);
        db.SaveChanges();   
        
        return producto; 
    }
    
    public bool EliminarProducto(int id) {
    var producto = db.Productos.FirstOrDefault(p => p.Id == id);

       if (producto is null) return false;

       db.Productos.Remove(producto);
       db.SaveChanges();

      return true;
    }

    public bool ModificarProducto(int id, Producto productoActualizado) {
        var producto = db.Productos.FirstOrDefault(p => p.Id == id);

        if (producto is null) return false;
        
        db.Entry(producto).CurrentValues.SetValues(productoActualizado); 
        db.SaveChanges();

        return true;
        
    }
    public List<MovimientoDeProducto> TraerMovimientos(int productoId) {
         return db.Movimientos
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToList();
    }
    
    public bool RegistrarMovimiento(int productoId, TipoMovimiento tipo, int cantidad) {
        var producto = db.Productos.FirstOrDefault(p => p.Id == productoId);

        if (producto is null) return false;
        
        int nuevoStock = producto.Stock;

        if(tipo == TipoMovimiento.Compra) 
            nuevoStock += cantidad;

        if(tipo == TipoMovimiento.Venta) 
            nuevoStock -= cantidad;
        
        if (tipo == TipoMovimiento.Ajuste)
             nuevoStock = cantidad;
        
        var productoActualizado = producto with { Stock = nuevoStock };
        
        db.Entry(producto).CurrentValues.SetValues(productoActualizado);

        db.Movimientos.Add(new MovimientoDeProducto(0, productoId, tipo, cantidad, DateTime.Now));

        db.SaveChanges();

        return true;
       
    }
}