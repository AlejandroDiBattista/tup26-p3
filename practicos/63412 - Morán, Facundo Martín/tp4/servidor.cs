#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});
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
app.MapGet("/productos", (CatalogoRepositorio repositorio) =>
{
    return Results.Ok(repositorio.TraerProductos());
});
app.MapGet("/productos/{id:int}",
(int id, CatalogoRepositorio repositorio) =>
{
    var producto = repositorio.TraerProducto(id);

    if (producto is null)
        return Results.NotFound();

    return Results.Ok(producto);
});
app.MapPost("/productos", (ProductoRequest request, CatalogoRepositorio repositorio) =>
{
        var error = ValidarProducto(request);
        if (error is not null)
            return Results.BadRequest(error);
            if (repositorio.ExisteCodigo(request.Codigo))
        return Results.BadRequest("El código ya existe.");
    
    var producto = repositorio.CrearProducto(request);

    return Results.Created($"/productos/{producto.Id}", producto);
});
app.MapPut("/productos/{id:int}",
(int id, ProductoRequest request, CatalogoRepositorio repositorio) =>
{
    var error = ValidarProducto(request);
    if (error is not null)
        return Results.BadRequest(error);

    var producto = repositorio.ModificarProducto(id, request);

    if (producto is null)
        return Results.NotFound();

    return Results.Ok(producto);
});
app.MapDelete("/productos/{id:int}",
(int id, CatalogoRepositorio repositorio) =>
{
    var eliminado = repositorio.EliminarProducto(id);

    if (!eliminado)
        return Results.NotFound();

    return Results.NoContent();
});
app.MapGet("/productos/{productoId:int}/movimientos",
(int productoId, CatalogoRepositorio repositorio) =>
{
    var producto = repositorio.TraerProducto(productoId);

    if (producto is null)
        return Results.NotFound();

    return Results.Ok(
        repositorio.TraerMovimientos(productoId));
});
app.MapPost("/productos/{productoId:int}/movimientos",
(int productoId,
MovimientoRequest request,
CatalogoRepositorio repositorio) =>
{
    if (request.Cantidad <= 0)
        return Results.BadRequest("La cantidad debe ser positiva.");

    try
    {
        var movimiento =
            repositorio.RegistrarMovimiento(
                productoId,
                request);

        if (movimiento is null)
            return Results.NotFound();

        return Results.Created(
            $"/productos/{productoId}/movimientos/{movimiento.Id}",
            movimiento);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

static string? ValidarProducto(ProductoRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Codigo))
        return "El código es obligatorio.";

    if (string.IsNullOrWhiteSpace(request.Nombre))
        return "El nombre es obligatorio.";

    if (request.Precio <= 0)
        return "El precio debe ser mayor a cero.";

    if (request.Stock < 0)
        return "El stock no puede ser negativo.";

    return null;
}

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

class Producto 
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Codigo { get; set; } = "";
    
    [MaxLength(120)]
    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}
record class ProductoRequest(string Codigo, string Nombre, decimal Precio, int Stock);
record class MovimientoDto(
    int Id,
    int ProductoId,
    TipoMovimiento Tipo,
    int Cantidad,
    DateTime Fecha);
    record class MovimientoRequest(
    TipoMovimiento Tipo,
    int Cantidad);
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}
class MovimientoDeProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public Producto Producto { get; set; } = null!;

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }
}

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
     modelBuilder.Entity<Producto>()
        .HasIndex(p => p.Codigo)
        .IsUnique();

    modelBuilder.Entity<MovimientoDeProducto>()
        .HasOne(m => m.Producto)
        .WithMany(p => p.Movimientos)
        .HasForeignKey(m => m.ProductoId)
        .OnDelete(DeleteBehavior.Cascade);
    }
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
        public Producto? TraerProducto(int id)
{
    return db.Productos.Find(id);
}
        public List<Producto> TraerProductos() =>
    db.Productos
        .OrderBy(p => p.Codigo)
        .ToList();
        
        public Producto CrearProducto(ProductoRequest request)
{
    var nuevoId = db.Productos.Any()
        ? db.Productos.Max(p => p.Id) + 1
        : 1;

    var producto = new Producto {
        Codigo = request.Codigo,
        Nombre = request.Nombre,
        Precio = request.Precio,
        Stock = request.Stock
    };
    db.Productos.Add(producto);
    db.SaveChanges();

    return producto;
}
public Producto? ModificarProducto(int id, ProductoRequest request)
{
    var producto = db.Productos.Find(id);

    if (producto is null)
        return null;

producto.Codigo = request.Codigo;
producto.Nombre = request.Nombre;
producto.Precio = request.Precio;
producto.Stock = request.Stock;

    db.Productos.Update(producto);
    db.SaveChanges();

    return producto;
}
public bool EliminarProducto(int id)
{
    var producto = db.Productos.Find(id);

    if (producto is null)
        return false;

    db.Productos.Remove(producto);
    db.SaveChanges();

    return true;
}
public bool ExisteCodigo(string codigo)
{
    return db.Productos.Any(p => p.Codigo == codigo);
}
public List<MovimientoDto> TraerMovimientos(int productoId)
{
    return db.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .Select(m => new MovimientoDto(
            m.Id,
            m.ProductoId,
            m.Tipo,
            m.Cantidad,
            m.Fecha))
        .ToList();
}
public MovimientoDto? RegistrarMovimiento(
    int productoId,
    MovimientoRequest request)
{
    var producto = db.Productos.Find(productoId);

    if (producto is null)
        return null;

    var cantidadRegistrada = request.Cantidad;

    switch (request.Tipo)
    {
        case TipoMovimiento.Compra:
            producto.Stock += request.Cantidad;
            break;

        case TipoMovimiento.Venta:

            if (producto.Stock < request.Cantidad)
                throw new Exception("No hay stock suficiente.");

            producto.Stock -= request.Cantidad;
            cantidadRegistrada = -request.Cantidad;
            break;

        case TipoMovimiento.Ajuste:
            producto.Stock = request.Cantidad;
            break;
    }

    var movimiento = new MovimientoDeProducto
    {
        ProductoId = producto.Id,
        Tipo = request.Tipo,
        Cantidad = cantidadRegistrada,
        Fecha = DateTime.Now
    };

    db.Movimientos.Add(movimiento);

    db.SaveChanges();

    return new MovimientoDto(
        movimiento.Id,
        movimiento.ProductoId,
        movimiento.Tipo,
        movimiento.Cantidad,
        movimiento.Fecha);
}       
}
