#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    return Results.Ok(repositorio.ListarProductos());
});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.ObtenerProductoPorId(id);
    if (producto is null) return Results.NotFound();
    
    return Results.Ok(producto);
});

app.MapPost("/productos", (Producto nuevoProducto, CatalogoRepositorio repositorio) => {
    var productoCreado = repositorio.CrearProducto(nuevoProducto);
    return Results.Created($"/productos/{productoCreado.Id}", productoCreado);
});

app.MapPut("/productos/{id}", (int id, Producto productoActualizado, CatalogoRepositorio repositorio) => {
    var exito = repositorio.ModificarProducto(id, productoActualizado);
    if (!exito) return Results.NotFound();
    
    return Results.NoContent();
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var exito = repositorio.EliminarProducto(id);
    if (!exito) return Results.NotFound();
    
    return Results.NoContent();
});

app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoRepositorio repositorio) => {
    var producto = repositorio.ObtenerProductoPorId(productoId);
    if (producto is null) return Results.NotFound("Producto no encontrado");
    
    return Results.Ok(repositorio.ListarMovimientos(productoId));
});

app.MapPost("/productos/{productoId}/movimientos", (int productoId, RequestMovimiento request, CatalogoRepositorio repositorio) => {
    if (request.Cantidad <= 0) return Results.BadRequest("La cantidad ingresada debe ser positiva.");

    var (exito, mensaje, movimiento) = repositorio.RegistrarMovimiento(productoId, request.Tipo, request.Cantidad);
    if (!exito) {
        if (mensaje == "Producto no encontrado") return Results.NotFound(mensaje);
        return Results.BadRequest(mensaje);
    }

    return Results.Created($"/productos/{productoId}/movimientos/{movimiento?.Id}", movimiento);
});

app.Run("http://localhost:5050");

enum TipoMovimiento { Compra, Venta, Ajuste }

class Producto 
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }

    public Producto() { }
    public Producto(int id, string codigo, string nombre, decimal precio, int stock) {
        Id = id;
        Codigo = codigo;
        Nombre = nombre;
        Precio = precio;
        Stock = stock;
    }
}

class MovimientoDeProducto 
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}

record RequestMovimiento(TipoMovimiento Tipo, int Cantidad);

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

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
    
    public List<Producto> ListarProductos() => db.Productos.OrderBy(p => p.Id).ToList();

    public Producto? ObtenerProductoPorId(int id) => db.Productos.Find(id);

    public Producto CrearProducto(Producto nuevo) {
        nuevo.Id = 0;
        db.Productos.Add(nuevo);
        db.SaveChanges();
        return nuevo;
    }

    public bool ModificarProducto(int id, Producto actualizado) {
        var prod = db.Productos.Find(id);
        if (prod is null) return false;

        prod.Codigo = actualizado.Codigo;
        prod.Nombre = actualizado.Nombre;
        prod.Precio = actualizado.Precio;
        
        db.SaveChanges();
        return true;
    }

    public bool EliminarProducto(int id) {
        var prod = db.Productos.Find(id);
        if (prod is null) return false;

        var movimientosAsociados = db.Movimientos.Where(m => m.ProductoId == id);
        db.Movimientos.RemoveRange(movimientosAsociados);

        db.Productos.Remove(prod);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientos(int productoId) =>
        db.Movimientos.Where(m => m.ProductoId == productoId).OrderByDescending(m => m.Fecha).ToList();

    public (bool Exito, string Mensaje, MovimientoDeProducto? Movimiento) RegistrarMovimiento(int productoId, TipoMovimiento tipo, int cantidad) {
        var prod = db.Productos.Find(productoId);
        if (prod is null) return (false, "Producto no encontrado", null);

        switch (tipo) {
            case TipoMovimiento.Compra:
                prod.Stock += cantidad;
                break;
                
            case TipoMovimiento.Venta:
                if (prod.Stock < cantidad) {
                    return (false, "Stock insuficiente para realizar la venta.", null);
                }
                prod.Stock -= cantidad;
                break;
                
            case TipoMovimiento.Ajuste:
                prod.Stock = cantidad;
                break;
        }

        var nuevoMovimiento = new MovimientoDeProducto {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = cantidad,
            Fecha = DateTime.Now
        };

        db.Movimientos.Add(nuevoMovimiento);
        
        db.SaveChanges(); 

        return (true, "Movimiento registrado con éxito", nuevoMovimiento);
    }
}