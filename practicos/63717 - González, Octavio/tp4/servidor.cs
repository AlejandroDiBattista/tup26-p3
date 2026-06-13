#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@10
#:property PublishAot=false


using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using SQLitePCL;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(options => options.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<Repositorio>();
var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var repositorio = scope.ServiceProvider.GetRequiredService<Repositorio>();
    repositorio.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────

//productos///
app.MapGet("/productos", (Repositorio repositorio) =>
{
    var productos = repositorio.TraerProducto();
    if (productos is null) return Results.NotFound();
    return Results.Ok(productos);
});
app.MapGet("/productos/{id:int}", (int id, Repositorio repositorio) =>
{
    var producto = repositorio.TraerProducto(id);
    if (producto is null) return Results.NotFound();
    return Results.Ok(producto);
});

app.MapPost("/productos", async (Producto nuevo, Repositorio repositorio) =>
{
    await repositorio.AgregarProducto(nuevo);
    return Results.Ok(nuevo);
});

app.MapPut("/productos/{id:int}", async (int id, Producto actualizacion, Repositorio repositorio) =>
{
    await repositorio.Actualizar(id, actualizacion);
    return Results.Ok(actualizacion);
});

app.MapDelete("/productos/{id:int}", async (int id, Repositorio repositorio) =>
{
    await repositorio.Eliminar(id);
    return Results.Ok();
});
//Movimiento de productos///

app.MapGet("/productos/{id:int}/movimientos", (int id, Repositorio repositorio) =>
{
    var movimiento = repositorio.TraerMovimiento(id);
    if (movimiento is null) return Results.NotFound();
    return Results.Ok(movimiento);
});

app.MapPost("/productos/{id:int}/movimientos", async (int id, Movimiento mov, Repositorio repositorio) =>
{
    var resultado = await repositorio.RegistrarMov(id, mov);
    if (resultado is null) return Results.BadRequest("No se pudo registrar el movimiento.");
    return Results.Ok(resultado);
});



app.Run("http://localhost:3000");


// ── Modelo ────────────────────────────────────────────────────────────────

//relacion 1 producto  a N movs
class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public decimal Precio { get; set; }
    public int Cant { get; set; }
    public string Unidadmedida { get; set; }
    public int Stock { get; set; }
    public Producto(int id, string codigo, string nombre, decimal precio, int stock, int cant, string unidadmedida)
    {
        Id = id;
        Codigo = codigo;
        Nombre = nombre;
        Precio = precio;
        Stock = stock;
        Cant = cant;
        Unidadmedida = unidadmedida;
    }
}

class Movimiento
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
    public Producto Producto { get; set; } = null!;
    public Movimiento(int id, int productoId, TipoMovimiento tipo, int cantidad, DateTime fecha)
    {
        Id = id;
        Tipo = tipo;
        Cantidad = cantidad;
        Fecha = fecha;
        ProductoId = productoId;
    }
}

enum TipoMovimiento { compra = 1, venta = 2, ajuste = 3 }


// ── DbContext ─────────────────────────────────────────────────────────────
class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>(); //tabla producto. 
    public DbSet<Movimiento> Movimientos => Set<Movimiento>(); //tabla Movimientos. 
}

// ── Repositorio ───────────────────────────────────────────────────────────

class Repositorio
{
    private readonly CatalogoDb db;

    public Repositorio(CatalogoDb db) => this.db = db;

    public void Iniciar()
    {
        db.Database.EnsureCreated();

        if (!db.Productos.Any())
        {
            db.Productos.Add(new Producto(0, "P001", "Yerba Mate", 1500m, 100, 500, "gr"));
            db.Productos.Add(new Producto(0, "P002", "Coca Cola", 2000m, 150, 2000, "ml"));
            db.Productos.Add(new Producto(0, "P003", "Papas Fritas", 800m, 200, 100, "gr"));
            db.SaveChanges();
        }
        if (!db.Movimientos.Any())
        {
            db.Movimientos.Add(new Movimiento(0, 1, TipoMovimiento.compra, 10, DateTime.Now));
            db.Movimientos.Add(new Movimiento(0, 2, TipoMovimiento.compra, 10, DateTime.Now));
            db.Movimientos.Add(new Movimiento(0, 3, TipoMovimiento.compra, 10, DateTime.Now));
            db.SaveChanges();
        }
    }

    public List<Producto> TraerProducto() =>
        db.Productos.ToList();

    public Producto? TraerProducto(int id) =>
        db.Productos.FirstOrDefault(p => p.Id == id);

    public List<Movimiento> TraerMovimiento(int id) =>
    db.Movimientos.Where(m => m.ProductoId == id).ToList();

    async public Task<Producto> AgregarProducto(Producto producto)
    {

        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        return producto;
    }
    async public Task<Producto?> Actualizar(int id, Producto actualizacion)
    {
        var cambio = db.Productos.FirstOrDefault(p => p.Id == id);

        if (cambio is null) return null;
        cambio.Nombre = actualizacion.Nombre;
        cambio.Precio = actualizacion.Precio;
        cambio.Stock = actualizacion.Stock;
        cambio.Cant = actualizacion.Cant;
        cambio.Unidadmedida = actualizacion.Unidadmedida;
        await db.SaveChangesAsync();
        return cambio;
    }

    async public Task<Movimiento> RegistrarMov (int productoid,Movimiento mov)
    {
        var producto = await db.Productos.FindAsync(productoid);

        if (producto is null) return null!;
        if (mov.Tipo == TipoMovimiento.compra) producto.Stock += mov.Cantidad;
        if (mov.Tipo == TipoMovimiento.venta) producto.Stock -= mov.Cantidad;
        if (mov.Tipo == TipoMovimiento.ajuste) producto.Stock = mov.Cantidad;
        
        mov.ProductoId = productoid;
        mov.Fecha = DateTime.Now;

        await db.Movimientos.AddAsync(mov);
        await db.SaveChangesAsync();
        return mov;          
    
    }
    async public Task<bool> Eliminar(int id)
    {
        var eliminado = db.Productos.FirstOrDefault(p => p.Id == id);
        if (eliminado is null) return false;
        db.Productos.Remove(eliminado);
        await db.SaveChangesAsync();
        return true;
    }

}


