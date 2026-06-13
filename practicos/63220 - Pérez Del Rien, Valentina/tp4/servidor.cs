#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false
#:package Microsoft.EntityFrameworkCore.Sqlite@9.*

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt =>
    opt.UseSqlite("Data Source=catalogo.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();
}

app.MapGet("/productos", async (CatalogoDb db) => await db.Productos.ToListAsync());

app.MapPost("/productos", async (Producto producto, CatalogoDb db) => {
    db.Productos.Add(producto);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id}", async (int id, Producto datos, CatalogoDb db) => {
    var p = await db.Productos.FindAsync(id);
    if (p is null) return Results.NotFound();
    p.Codigo = datos.Codigo; p.Nombre = datos.Nombre; p.Precio = datos.Precio; p.Stock = datos.Stock;
    await db.SaveChangesAsync();
    return Results.Ok(p);
});

app.MapDelete("/productos/{id}", async (int id, CatalogoDb db) => {
    var p = await db.Productos.FindAsync(id);
    if (p is null) return Results.NotFound();
    db.Productos.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/productos/{productoId}/movimientos", async (int productoId, CatalogoDb db) =>
    await db.Movimientos.Where(m => m.ProductoId == productoId).OrderByDescending(m => m.Fecha).ToListAsync());

app.MapPost("/productos/{productoId}/movimientos", async (int productoId, HttpContext context, CatalogoDb db) => {
    var p = await db.Productos.FindAsync(productoId);
    if (p is null) return Results.NotFound();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var doc = System.Text.Json.JsonDocument.Parse(body);
    var tipoStr = doc.RootElement.GetProperty("tipo").GetString() ?? "";
    var cant = doc.RootElement.GetProperty("cantidad").GetInt32();
    
    var tipo = tipoStr == "Compra" ? TipoMovimiento.Compra : (tipoStr == "Venta" ? TipoMovimiento.Venta : TipoMovimiento.Ajuste);
    var mov = new MovimientoDeProducto { ProductoId = productoId, Tipo = tipo, Cantidad = cant, Fecha = DateTime.Now };
    
    if (tipo == TipoMovimiento.Compra) p.Stock += cant;
    else if (tipo == TipoMovimiento.Venta) p.Stock -= cant;
    else p.Stock = cant;
    
    db.Movimientos.Add(mov);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{productoId}/movimientos/{mov.Id}", mov);
});

app.Run("http://localhost:5000");

public enum TipoMovimiento { Compra, Venta, Ajuste }

public class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

public class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}