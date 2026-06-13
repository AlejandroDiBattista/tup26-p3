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
    Results.Ok(repositorio.TraerProductos()));

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) =>
{
    var producto = repositorio.TraerProductoPorId(id);

    return producto is null
        ? Results.NotFound()
        : Results.Ok(producto);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repositorio) =>
{
    var nuevo = repositorio.AgregarProducto(producto);

    return nuevo is null
        ? Results.Conflict("Ya existe un producto con ese codigo.")
        : Results.Created($"/productos/{nuevo.Id}", nuevo);
});

app.MapPut("/productos/{id}", (int id, Producto producto, CatalogoRepositorio repositorio) =>
{
    var resultado = repositorio.ModificarProducto(id, producto);

    return resultado switch
    {
        ResultadoProducto.NoEncontrado => Results.NotFound(),
        ResultadoProducto.CodigoDuplicado => Results.Conflict("Ya existe un producto con ese codigo."),
        _ => Results.NoContent()
    };
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) =>
{
    var ok = repositorio.EliminarProducto(id);

    return ok
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoRepositorio repositorio) =>
    Results.Ok(repositorio.TraerMovimientos(productoId)));

app.MapPost("/productos/{productoId}/movimientos", (
    int productoId,
    MovimientoRequest request,
    CatalogoRepositorio repositorio) =>
{
    var resultado = repositorio.RegistrarMovimiento(productoId, request.Tipo, request.Cantidad);

    return resultado switch
    {
        ResultadoMovimiento.NoEncontrado => Results.NotFound(),
        ResultadoMovimiento.CantidadInvalida => Results.BadRequest("Cantidad invalida."),
        ResultadoMovimiento.StockInsuficiente => Results.BadRequest("Stock insuficiente."),
        _ => Results.Ok()
    };
});

app.Run("http://localhost:5050");

class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

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
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

record MovimientoRequest(TipoMovimiento Tipo, int Cantidad);

enum ResultadoProducto
{
    Ok,
    NoEncontrado,
    CodigoDuplicado
}

enum ResultadoMovimiento
{
    Ok,
    NoEncontrado,
    CantidadInvalida,
    StockInsuficiente
}

class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();
    }
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
                new Producto { Id = 1, Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
                new Producto { Id = 2, Codigo = "P002", Nombre = "Azucar 1kg", Precio = 1200m, Stock = 50 },
                new Producto { Id = 3, Codigo = "P003", Nombre = "Cafe 500g", Precio = 2500m, Stock = 30 }
            );
        }

        if (!db.Movimientos.Any())
        {
            db.Movimientos.AddRange(
                new MovimientoDeProducto { Id = 1, ProductoId = 1, Tipo = TipoMovimiento.Compra, Cantidad = 50, Fecha = DateTime.Now.AddDays(-3) },
                new MovimientoDeProducto { Id = 2, ProductoId = 1, Tipo = TipoMovimiento.Venta, Cantidad = 10, Fecha = DateTime.Now.AddDays(-2) },
                new MovimientoDeProducto { Id = 3, ProductoId = 2, Tipo = TipoMovimiento.Compra, Cantidad = 20, Fecha = DateTime.Now.AddDays(-1) }
            );
        }

        db.SaveChanges();
    }

    public List<Producto> TraerProductos() =>
        db.Productos.OrderBy(p => p.Id).ToList();

    public Producto? TraerProductoPorId(int id) =>
        db.Productos.Find(id);

    public Producto? AgregarProducto(Producto datos)
    {
        string codigo = datos.Codigo.Trim();

        if (db.Productos.Any(p => p.Codigo == codigo))
            return null;

        var producto = new Producto
        {
            Codigo = codigo,
            Nombre = datos.Nombre.Trim(),
            Precio = datos.Precio,
            Stock = datos.Stock
        };

        db.Productos.Add(producto);
        db.SaveChanges();

        return producto;
    }

    public ResultadoProducto ModificarProducto(int id, Producto datos)
    {
        var producto = db.Productos.Find(id);

        if (producto is null)
            return ResultadoProducto.NoEncontrado;

        string codigo = datos.Codigo.Trim();

        if (db.Productos.Any(p => p.Id != id && p.Codigo == codigo))
            return ResultadoProducto.CodigoDuplicado;

        producto.Codigo = codigo;
        producto.Nombre = datos.Nombre.Trim();
        producto.Precio = datos.Precio;
        producto.Stock = datos.Stock;

        db.SaveChanges();

        return ResultadoProducto.Ok;
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

    public List<MovimientoDeProducto> TraerMovimientos(int productoId) =>
        db.Movimientos
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToList();

    public ResultadoMovimiento RegistrarMovimiento(int productoId, TipoMovimiento tipo, int cantidad)
    {
        var producto = db.Productos.Find(productoId);

        if (producto is null)
            return ResultadoMovimiento.NoEncontrado;

        if (cantidad < 0 || (tipo != TipoMovimiento.Ajuste && cantidad == 0))
            return ResultadoMovimiento.CantidadInvalida;

        int nuevoStock = tipo switch
        {
            TipoMovimiento.Compra => producto.Stock + cantidad,
            TipoMovimiento.Venta => producto.Stock - cantidad,
            TipoMovimiento.Ajuste => cantidad,
            _ => producto.Stock
        };

        if (nuevoStock < 0)
            return ResultadoMovimiento.StockInsuficiente;

        producto.Stock = nuevoStock;

        db.Movimientos.Add(new MovimientoDeProducto
        {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = cantidad,
            Fecha = DateTime.Now
        });

        db.SaveChanges();

        return ResultadoMovimiento.Ok;
    }
}