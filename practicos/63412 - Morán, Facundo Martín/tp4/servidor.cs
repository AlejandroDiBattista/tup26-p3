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

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record class ProductoRequest(string Codigo, string Nombre, decimal Precio, int Stock);

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
            db.Productos.Add(new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100));
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

    var producto = new Producto(
        nuevoId,
        request.Codigo,
        request.Nombre,
        request.Precio,
        request.Stock
    );

    db.Productos.Add(producto);
    db.SaveChanges();

    return producto;
}
public Producto? ModificarProducto(int id, ProductoRequest request)
{
    var producto = db.Productos.Find(id);

    if (producto is null)
        return null;

    producto = producto with
    {
        Codigo = request.Codigo,
        Nombre = request.Nombre,
        Precio = request.Precio,
        Stock = request.Stock
    };

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
        
}