using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
var app = builder.Build();

app.MapGet("/productos", async (AppDb db) => await db.Productos.ToListAsync());

app.MapGet("/productos/{id}", async (int id, AppDb db) => {
    var prod = await db.Productos.FindAsync(id);
    if (prod == null) return Results.NotFound();
    return Results.Ok(prod);
});

app.MapPost("/productos", async (Producto p, AppDb db) => {
    db.Productos.Add(p);
    await db.SaveChangesAsync();
    return Results.Ok(p);
});

app.MapPut("/productos/{id}", async (int id, Producto p, AppDb db) => {
    var prod = await db.Productos.FindAsync(id);
    if (prod == null) return Results.NotFound();
    
    prod.Codigo = p.Codigo;
    prod.Nombre = p.Nombre;
    prod.Precio = p.Precio;
    prod.Stock = p.Stock;
    
    await db.SaveChangesAsync();
    return Results.Ok(prod);
});

app.MapDelete("/productos/{id}", async (int id, AppDb db) => {
    var p = await db.Productos.FindAsync(id);
    if (p != null) {
        db.Productos.Remove(p);
        await db.SaveChangesAsync();
    }
    return Results.Ok();
});

app.Run();

class AppDb : DbContext 
{
    public AppDb(DbContextOptions opciones) : base(opciones) 
    { 
        Database.EnsureCreated(); 
    }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Movimiento> Movimientos { get; set; }
}

class Producto 
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

class Movimiento 
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}