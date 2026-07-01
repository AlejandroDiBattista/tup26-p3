#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

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
app.MapPost("/productos",(CatalogoRepositorio repositorio,Producto producto) => {
    repositorio.AgregarProducto(producto);
    return Results.Created($"/productos/{producto.Id}",producto);
    });

app.MapDelete("/productos/{id}",(CatalogoRepositorio repositorio,int id)=>{
    var eliminado = repositorio.EliminarProducto(id);
    if (!eliminado)
    return Results.NotFound();

    return Results.NoContent();    
});
app.MapPut("/productos/{id}",(CatalogoRepositorio repositorio,int id ,Producto productoActualizado)=> {
    var actualizado = repositorio.ActualizarProducto(id,productoActualizado);
    if (!actualizado)
    return Results.NotFound();

    return Results.Ok(productoActualizado);
});
app.MapGet("/productos/{id}", (CatalogoRepositorio repositorio, int id) =>
{
    var producto = repositorio.BuscaProducto(id);

    if (producto is null)
        return Results.NotFound();

    return Results.Ok(producto);
});

app.MapPost("/productos/{productoId}/movimientos",
(CatalogoRepositorio repositorio,int productoId,MovimientoDeProducto movimiento) => {
    Console.WriteLine("ENTRO AL ENDPOINT");
    var registrado = repositorio.RegistrarMovimiento(productoId,movimiento);
    if(!registrado)
    return Results.NotFound();

    return Results.Ok(movimiento);
});


app.MapGet("/productos/{productoId}/movimientos",(CatalogoRepositorio repositorio,int productoId) => {
    var movimiento =repositorio.TraerMovimientos(productoId);
    return Results.Ok(movimiento);
    
});


app.Run("http://localhost:5050");




// ── Modelo ────────────────────────────────────────────────────────────────

class Producto
 {
    public int Id  {get ; set;}
    public string Codigo {get ; set ;} = "";
     public string Nombre {get ; set ; } = "";
     public decimal Precio {get; set;}
     public int Stock {get; set;}
}
public enum TipoMovimiento {
    Compra= 0,
    Venta = 1,
    Ajuste = 2

}
class MovimientoDeProducto {
    public int Id {get; set;}
    public int ProductoId {get; set;}
    public TipoMovimiento Tipo {get; set;}
    public int Cantidad {get;set;}
    public DateTime Fecha {get;set;}
}

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
   public DbSet<Producto> Productos => Set <Producto>();
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
              Stock = 100 , 
          
           });
           db.SaveChanges();
        }
   
    }
    public bool RegistrarMovimiento(int productoId, MovimientoDeProducto movimiento)
{
    var producto = db.Productos.Find(productoId);

    if (producto is null)
        return false;

    movimiento.ProductoId = productoId;

    movimiento.Fecha = DateTime.Now;

    switch (movimiento.Tipo)
    {
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

    return true;
}

    public List<Producto> TraerProductos ()=>
    db.Productos.OrderBy(p => p.Id).ToList();
    public Producto? BuscaProducto(int id)
    {
        return db.Productos.Find(id);
    }
    public void AgregarProducto (Producto producto) {
        db.Productos.Add(producto);
        db.SaveChanges();
    }

    public bool EliminarProducto(int id) {
        var producto = db.Productos.Find(id);

        if (producto is null) {
            return false;
        }
        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }
    public bool ActualizarProducto (int id , Producto productoActualizado) {
        var producto = db.Productos.Find(id);
        if (producto is null)
        return false ;

        producto.Codigo =   productoActualizado.Codigo;
        producto.Nombre =   productoActualizado.Nombre;
        producto.Precio =   productoActualizado.Precio;
        producto.Stock  =   productoActualizado.Stock;

        db.SaveChanges();
        return true;

    }
    public List<MovimientoDeProducto> TraerMovimientos(int productoId) {
        return db.Movimientos
        .Where(m=> m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToList();
        
    }
}

