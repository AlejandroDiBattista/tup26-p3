#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

app.MapGet("/productos", (CatalogoRepositorio repositorio) =>
{
    return Results.Ok(repositorio.ListarProductos());
});

app.MapGet("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) =>
{
    var producto = repositorio.BuscarProducto(id);

    if (producto is null)
        return Results.NotFound();

    return Results.Ok(producto);
});

app.MapPost("/productos", (ProductoInput input, CatalogoRepositorio repositorio) =>
{
    string? error = ValidarProducto(input);

    if (error is not null)
        return Results.BadRequest(error);

    if (repositorio.ExisteCodigo(input.Codigo))
        return Results.BadRequest("Ya existe un producto con ese codigo.");

    Producto producto = repositorio.CrearProducto(input);

    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id:int}", (int id, ProductoInput input, CatalogoRepositorio repositorio) =>
{
    string? error = ValidarProducto(input);

    if (error is not null)
        return Results.BadRequest(error);

    if (repositorio.ExisteCodigoEnOtroProducto(input.Codigo, id))
        return Results.BadRequest("Ya existe otro producto con ese codigo.");

    Producto? producto = repositorio.ModificarProducto(id, input);

    if (producto is null)
        return Results.NotFound();

    return Results.Ok(producto);
});

app.MapDelete("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) =>
{
    bool eliminado = repositorio.EliminarProducto(id);

    if (!eliminado)
        return Results.NotFound();

    return Results.NoContent();
});

app.MapGet("/productos/{productoId:int}/movimientos", (int productoId, CatalogoRepositorio repositorio) =>
{
    Producto? producto = repositorio.BuscarProducto(productoId);

    if (producto is null)
        return Results.NotFound();

    return Results.Ok(repositorio.ListarMovimientos(productoId));
});

app.MapPost("/productos/{productoId:int}/movimientos", (int productoId, MovimientoInput input, CatalogoRepositorio repositorio) =>
{
    if (input.Cantidad <= 0)
        return Results.BadRequest("La cantidad debe ser positiva.");

    MovimientoDeProducto? movimiento = repositorio.RegistrarMovimiento(productoId, input);

    if (movimiento is null)
        return Results.NotFound();

    return Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento);
});

app.Run("http://localhost:5050");

static string? ValidarProducto(ProductoInput input)
{
    if (string.IsNullOrWhiteSpace(input.Codigo))
        return "El codigo es obligatorio.";

    if (string.IsNullOrWhiteSpace(input.Nombre))
        return "El nombre es obligatorio.";

    if (input.Precio < 0)
        return "El precio no puede ser negativo.";

    if (input.Stock < 0)
        return "El stock no puede ser negativo.";

    return null;
}

class Producto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }
}

record class ProductoInput(string Codigo, string Nombre, decimal Precio, int Stock);

class MovimientoDeProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }
}

record class MovimientoInput(TipoMovimiento Tipo, int Cantidad);

enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

class CatalogoRepositorio
{
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar()
    {
        db.Database.EnsureCreated();

        if (!db.Productos.Any())
        {
            db.Productos.AddRange(
                new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
                new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 1200m, Stock = 80 },
                new Producto { Codigo = "P003", Nombre = "Cafe Molido 250g", Precio = 3500m, Stock = 35 }
            );

            db.SaveChanges();
        }
    }

    public List<Producto> ListarProductos() =>
        db.Productos.OrderBy(p => p.Codigo).ToList();

    public Producto? BuscarProducto(int id) =>
        db.Productos.FirstOrDefault(p => p.Id == id);

    public bool ExisteCodigo(string codigo) =>
        db.Productos.Any(p => p.Codigo.ToLower() == codigo.Trim().ToLower());

    public bool ExisteCodigoEnOtroProducto(string codigo, int id) =>
        db.Productos.Any(p => p.Id != id && p.Codigo.ToLower() == codigo.Trim().ToLower());

    public Producto CrearProducto(ProductoInput input)
    {
        Producto producto = new()
        {
            Codigo = input.Codigo.Trim(),
            Nombre = input.Nombre.Trim(),
            Precio = input.Precio,
            Stock = input.Stock
        };

        db.Productos.Add(producto);
        db.SaveChanges();

        return producto;
    }

    public Producto? ModificarProducto(int id, ProductoInput input)
    {
        Producto? producto = BuscarProducto(id);

        if (producto is null)
            return null;

        producto.Codigo = input.Codigo.Trim();
        producto.Nombre = input.Nombre.Trim();
        producto.Precio = input.Precio;
        producto.Stock = input.Stock;

        db.SaveChanges();

        return producto;
    }

    public bool EliminarProducto(int id)
    {
        Producto? producto = BuscarProducto(id);

        if (producto is null)
            return false;

        db.Productos.Remove(producto);
        db.SaveChanges();

        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientos(int productoId)
    {
        return db.Movimientos
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToList();
    }

    public MovimientoDeProducto? RegistrarMovimiento(int productoId, MovimientoInput input)
    {
        Producto? producto = BuscarProducto(productoId);

        if (producto is null)
            return null;

        if (input.Tipo == TipoMovimiento.Compra)
        {
            producto.Stock += input.Cantidad;
        }
        else if (input.Tipo == TipoMovimiento.Venta)
        {
            producto.Stock -= input.Cantidad;
        }
        else if (input.Tipo == TipoMovimiento.Ajuste)
        {
            producto.Stock = input.Cantidad;
        }

        MovimientoDeProducto movimiento = new()
        {
            ProductoId = productoId,
            Tipo = input.Tipo,
            Cantidad = input.Cantidad,
            Fecha = DateTime.Now
        };

        db.Movimientos.Add(movimiento);
        db.SaveChanges();

        return movimiento;
    }
}