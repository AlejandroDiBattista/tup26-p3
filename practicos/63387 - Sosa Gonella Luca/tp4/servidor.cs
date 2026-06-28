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


app.MapGet("/productos/{id}" , (int id, CatalogoRepositorio repositorio) => {
    
    var producto = repositorio.TraerProductoPorId(id);
    if (producto is null) return Results.NotFound();

    return Results.Ok(producto);

});

app.MapPost("/productos" , (Producto producto, CatalogoRepositorio repositorio) => {
    
    var nuevoProducto = repositorio.AgregarProducto(producto);

    return Results.Created($"/productos/{nuevoProducto.Id}", nuevoProducto);


});


app.MapPut("/productos/{id}", (int id, Producto producto, CatalogoRepositorio repositorio) => {
    
    var actualizado = repositorio.ModificarProducto(id, producto);

    if (actualizado is null)
        return Results.NotFound();

    return Results.Ok(actualizado);    
});


app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    
    var eliminado = repositorio.EliminarProducto(id);

    if (!eliminado)
        return Results.NotFound();

    return Results.NoContent();    


});

app.MapGet("/productos/{productoId}/movimientos", 
    (int productoId, CatalogoRepositorio repositorio) => {
    
    var movimientos = repositorio.TraerMovimientosDeProducto(productoId);

    return Results.Ok(movimientos);
});


app.MapPost("/productos/{productoId}/movimientos",
(int productoId,
 MovimientoDto datos,
 CatalogoRepositorio repositorio) =>
{
    var movimiento = repositorio.RegistrarMovimiento(
        productoId,
        datos.Tipo,
        datos.Cantidad
    );

    if (movimiento is null)
        return Results.NotFound();

    return Results.Ok(movimiento);
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

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();

    public List<Producto> TraerProductos() =>
    db.Productos.OrderBy(p => p.Id).ToList();    

    public Producto? TraerProductoPorId(int id) =>
        db.Productos.FirstOrDefault(p => p.Id == id);


    public Producto AgregarProducto(Producto producto) {
        
        db.Productos.Add(producto);
        db.SaveChanges();

        return producto;
    }    

    public Producto? ModificarProducto(int id,  Producto producto) 
    {
        var existente = db.Productos.FirstOrDefault(p => p.Id == id);

        if (existente is null) return null;

        db.Entry(existente).CurrentValues.SetValues(producto);

        db.SaveChanges();

        return existente;
        
    }

    public bool EliminarProducto(int id) {
        
        var producto = db.Productos.FirstOrDefault(p => p.Id == id);

        if (producto is null)
            return false;

        db.Productos.Remove(producto);
        db.SaveChanges();

        return true;    
    }

    public List<MovimientoDeProducto> TraerMovimientosDeProducto(int productoId) => 

        db.Movimientos

        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToList();

        public MovimientoDeProducto? RegistrarMovimiento(
        int productoId,
        TipoMovimiento tipo,
        int cantidad)
    {
        var producto = db.Productos.FirstOrDefault(p => p.Id == productoId);

        if (producto is null)
            return null;

        switch (tipo)
        {
            case TipoMovimiento.Compra:
                producto.Stock += cantidad;
                break;

            case TipoMovimiento.Venta:
                producto.Stock -= cantidad;
                break;

            case TipoMovimiento.Ajuste:
                producto.Stock = cantidad;
                break;
        }

        var movimiento = new MovimientoDeProducto(
            0,
            productoId,
            tipo,
            cantidad,
            DateTime.Now
        );

        db.Movimientos.Add(movimiento);

        db.SaveChanges();

        return movimiento;
    }



}