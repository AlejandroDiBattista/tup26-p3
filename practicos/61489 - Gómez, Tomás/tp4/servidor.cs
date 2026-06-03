using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CatalogoDb>();
var app = builder.Build();
using (var scope = app.Services.CreateScope()) 
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();
}

app.MapGet("/productos", (CatalogoDb db) => 
{
    return Results.Ok(db.Productos.ToList());
});
app.MapGet("/productos/{id}", (int id, CatalogoDb db) => 
{
    var producto = db.Productos.Find(id);
    return producto != null ? Results.Ok(producto) : Results.NotFound("Producto no encontrado.");
});
app.MapPost("/productos", (Producto nuevoProd, CatalogoDb db) => 
{
    db.Productos.Add(nuevoProd);
    db.SaveChanges();
    return Results.Created($"/productos/{nuevoProd.Id}", nuevoProd);
});
app.MapPut("/productos/{id}", (int id, Producto prodModificado, CatalogoDb db) => 
{
    var producto = db.Productos.Find(id);
    if (producto == null) return Results.NotFound();

    producto.Codigo = prodModificado.Codigo;
    producto.Nombre = prodModificado.Nombre;
    producto.Precio = prodModificado.Precio;
    db.SaveChanges();
    return Results.Ok(producto);
});

app.MapDelete("/productos/{id}", (int id, CatalogoDb db) => 
{
    var producto = db.Productos.Find(id);
    if (producto == null) return Results.NotFound();

    db.Productos.Remove(producto);
    db.SaveChanges();
    return Results.Ok("Producto eliminado.");
});

app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoDb db) => 
{
    var historial = db.Movimientos
                      .Where(m => m.ProductoId == productoId)
                      .OrderByDescending(m => m.Fecha)
                      .ToList();
    return Results.Ok(historial);
});
app.MapPost("/productos/{productoId}/movimientos", (int productoId, MovimientoDeProducto mov, CatalogoDb db) => 
{
    var producto = db.Productos.Find(productoId);
    if (producto == null) return Results.NotFound("Producto no existe.");

    var cantidadReal = Math.Abs(mov.Cantidad);
    mov.Cantidad = cantidadReal;
    mov.ProductoId = productoId;
    mov.Fecha = DateTime.Now;

    if (mov.Tipo == TipoMovimiento.Compra) {
        producto.Stock += cantidadReal;
    } 
    else if (mov.Tipo == TipoMovimiento.Venta) {
        producto.Stock -= cantidadReal;
    } 
    else if (mov.Tipo == TipoMovimiento.Ajuste) {
        producto.Stock = cantidadReal;
    }

    db.Movimientos.Add(mov);
    db.SaveChanges();

    return Results.Ok(mov);
});

app.Run("http://localhost:5000");

public enum TipoMovimiento { Compra, Venta, Ajuste }

public class Producto 
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

public class MovimientoDeProducto 
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}
public class CatalogoDb : DbContext 
{
    public DbSet<Producto> Productos { get; set; }
    public DbSet<MovimientoDeProducto> Movimientos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options) 
    {
        options.UseSqlite("Data Source=catalogo.db");
    }
}