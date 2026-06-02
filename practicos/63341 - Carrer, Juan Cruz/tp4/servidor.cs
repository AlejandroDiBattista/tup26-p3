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

app.MapGet("/productos", (CatalogoRepositorio repo) => {
    return Results.Ok(repo.TraerProductos());
});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    var prod = repo.TraerProductoPorId(id);

    if (prod is null)
        return Results.NotFound();

    return Results.Ok(prod);
});

app.MapPost("/productos", (Producto prod, CatalogoRepositorio repo) => {
    repo.AgregarProducto(prod);

    return Results.Created($"/productos/{prod.Id}", prod);
});

app.MapPut("/productos/{id}", (int id, Producto datos, CatalogoRepositorio repo) => {
    var prod = repo.EditarProducto(id, datos);

    if (prod is null)
        return Results.NotFound();

    return Results.Ok(prod);
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    var ok = repo.BorrarProducto(id);

    if (!ok)
        return Results.NotFound();

    return Results.NoContent();
});

app.MapGet("/productos/{id}/movimientos", (int id, CatalogoRepositorio repo) => {
    return Results.Ok(repo.TraerMovimientos(id));
});

app.MapPost("/productos/{id}/movimientos",
(int id, MovimientoProducto mov, CatalogoRepositorio repo) => {

    var ok = repo.AgregarMovimiento(id, mov);

    if (!ok)
        return Results.NotFound();

    return Results.Ok(mov);
});

app.Run("http://localhost:5050");

enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

class Producto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public List<MovimientoProducto> Movimientos { get; set; } = new();
}

class MovimientoProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    public Producto? Producto { get; set; }
}

class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options)
        : base(options)
    {
    }

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoProducto> Movimientos => Set<MovimientoProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>()
            .HasIndex(x => x.Codigo)
            .IsUnique();
    }
}

class CatalogoRepositorio
{
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db)
    {
        this.db = db;
    }

    public void Iniciar()
    {
        db.Database.EnsureCreated();

        if (!db.Productos.Any())
        {
            db.Productos.Add(new Producto {
                Codigo = "P001",
                Nombre = "Yerba Mate 500g",
                Precio = 1500m,
                Stock = 100
            });

            db.Productos.Add(new Producto {
                Codigo = "P002",
                Nombre = "Azucar 1kg",
                Precio = 1200m,
                Stock = 50
            });

            db.SaveChanges();
        }
    }

    public List<Producto> TraerProductos()
    {
        return db.Productos
            .OrderBy(x => x.Id)
            .ToList();
    }

    public Producto? TraerProductoPorId(int id)
    {
        return db.Productos.Find(id);
    }

    public void AgregarProducto(Producto prod)
    {
        db.Productos.Add(prod);

        db.SaveChanges();
    }

    public Producto? EditarProducto(int id, Producto datos)
    {
        var prod = db.Productos.Find(id);

        if (prod is null)
            return null;

        prod.Codigo = datos.Codigo;
        prod.Nombre = datos.Nombre;
        prod.Precio = datos.Precio;
        prod.Stock = datos.Stock;

        db.SaveChanges();

        return prod;
    }

    public bool BorrarProducto(int id)
    {
        var prod = db.Productos.Find(id);

        if (prod is null)
            return false;

        db.Productos.Remove(prod);

        db.SaveChanges();

        return true;
    }

    public List<MovimientoProducto> TraerMovimientos(int productoId)
    {
        return db.Movimientos
            .Where(x => x.ProductoId == productoId)
            .OrderByDescending(x => x.Fecha)
            .ToList();
    }

    public bool AgregarMovimiento(int productoId, MovimientoProducto mov)
    {
        var prod = db.Productos.Find(productoId);

        if (prod is null)
            return false;

        mov.ProductoId = productoId;

        mov.Fecha = DateTime.Now;

        switch (mov.Tipo)
        {
            case TipoMovimiento.Compra:
                prod.Stock += mov.Cantidad;
                break;

            case TipoMovimiento.Venta:
                prod.Stock -= mov.Cantidad;
                break;

            case TipoMovimiento.Ajuste:
                prod.Stock = mov.Cantidad;
                break;
        }

        db.Movimientos.Add(mov);

        db.SaveChanges();

        return true;
    }
}