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


// este endpoint lo que hace es esperar la peticion del cliente de TODOS ls productos 

app.MapGet("/productos",(CatalogoRepositorio repositorio) => {
    
    return repositorio.TraerProductos();
});


// este ENDPOINT lo que hace es esperar el pedido y devolver un producto unico con id especifico 
app.MapGet("/Productos/{Id}",(int Id, CatalogoRepositorio repositorio) => {

    var Producto = repositorio.TraerProductoPorId(Id);

    if(Producto is null) return Results.NotFound();
    return Results.Ok(Producto);
    
});

//este devuelve TODOS los movimientos del producto unico con id especifico 
app.MapGet("/productos/{Id}/movimientos",(int Id, CatalogoRepositorio repositorio) => {
    return repositorio.TraerMovimiento(Id);
});

//este endpoint es crea un producto nuevo y se lo guarda en la DB
app.MapPost("/Productos",(CrearProducto crearProducto,CatalogoRepositorio repositorio) => {
    repositorio.CrearProducto(
        crearProducto.Codigo,
        crearProducto.Nombre,
        crearProducto.Precio,
        crearProducto.Stock
        
    );
return Results.Ok();
    
});

//este crea un movimiento especifico de un producto en particular
app.MapPost("/Productos/{Productoid}/Movimiento",(int Productoid, CrearMovimientoDeProducto movimiento,CatalogoRepositorio repositorio) => {
    repositorio.CrearMovimientoDeProducto(movimiento);
    return Results.Ok(movimiento);
});

//este endpoint lo que hace es modificar un producto especifico y guardarlo en la DB
  app.MapPut("/productos/{Id}",(int Id, ModificarProducto modificarProducto,CatalogoRepositorio repositorio) => {
    
        var producto = repositorio.ModificarProducto(
            Id,
            modificarProducto.Codigo,
            modificarProducto.Nombre,
            modificarProducto.Precio,
            modificarProducto.Stock
        );
        if (producto is null) return Results.NotFound();
        return Results.Ok(producto);
    });

    // este elimina un producto especifico de la DB
  app.MapDelete ("/Productos/{Id}",(int Id,CatalogoRepositorio repositorio) => {
      var Producto = repositorio.EliminarProducto (Id);
      if (Producto is null)
      return Results.NotFound();
        return Results.Ok(Producto);
  });
// permite correr los endpoint anteriores 
app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }

    public Producto(int Id, string codigo, string nombre, decimal precio, int stock) {
        this.Id = Id;
        Codigo = codigo;
        Nombre = nombre;
        Precio = precio;
        Stock = stock;
    }
}

enum TipoMovimiento {
    Compra  ,
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

record class CrearMovimientoDeProducto(
int ProductoId,
TipoMovimiento Tipo,
int Cantidad

);
record CrearProducto(
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock 
);
record ModificarProducto(
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock 
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
            db.Productos.Add(new Producto(0, "A001", "Producto A", 10.99m, 100));
                
            db.SaveChanges();
        }
    }

    public Producto? TraerProductoPorId(int id) {
        return db.Productos.FirstOrDefault(p => p.Id == id);
    }

    public List<MovimientoDeProducto> TraerMovimientos() {
        return db.Movimientos.ToList();
    }

    public Producto? ModificarProducto(int id, string codigo, string nombre, decimal precio, int stock) {
        var producto = db.Productos.FirstOrDefault(p => p.Id == id);
        if (producto != null) {
            
            producto.Nombre = nombre;
            producto.Codigo = codigo;
            producto.Precio = precio;
            producto.Stock = stock;
            db.SaveChanges();
           
        } 
           
        return producto;
      
    }
public void CrearMovimientoDeProducto(CrearMovimientoDeProducto movimiento) {
        
        var Producto = db.Productos.FirstOrDefault(p => p.Id == movimiento.ProductoId);
        if (Producto is null) return;

        if(Producto.Stock < movimiento.Cantidad && movimiento.Tipo == TipoMovimiento.Venta) return;

        var Movimiento = new MovimientoDeProducto(
            0,
            Producto.Id,
            movimiento.Tipo,
            movimiento.Cantidad,
            DateTime.Now
        );
        if (movimiento.Tipo == TipoMovimiento.Compra) {
            Producto.Stock += movimiento.Cantidad;
            
        }
        else if (movimiento.Tipo == TipoMovimiento.Venta) {
            Producto.Stock -= movimiento.Cantidad;
        }

        else if (movimiento.Tipo == TipoMovimiento.Ajuste) {
            Producto.Stock = movimiento.Cantidad;
        }
        db.Movimientos.Add(Movimiento);
        db.SaveChanges();

    }
    public void CrearProducto(string codigo, string nombre, decimal precio, int stock){  //metodo para crear y guardar el nuevo producto
       Producto producto = new Producto(0, codigo, nombre, precio, stock);

       db.Productos.Add(producto);
       db.SaveChanges();

    }

    public Producto? EliminarProducto(int id) {
        var producto = db.Productos.FirstOrDefault(p => p.Id == id);
        if (producto != null) {
            
            db.Productos.Remove(producto);
            db.SaveChanges();
        }
        return producto;
    }

    public List<Producto> TraerProductos() {
        return db.Productos.ToList();
    }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();

    public List<MovimientoDeProducto> TraerMovimiento(int productoId ) {
        var movimientos = db.Movimientos.Where(m => m.ProductoId == productoId);
        return movimientos.ToList();
    }
      }


        


