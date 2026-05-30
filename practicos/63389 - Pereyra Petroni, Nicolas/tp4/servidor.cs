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

    public List<Producto> TraerProductos ()=>
    db.Productos.OrderBy(p => p.Id).ToList();

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
}

