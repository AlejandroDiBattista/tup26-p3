#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

// -- Configuracion ----------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();
builder.Services.ConfigureHttpJsonOptions(opt =>
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// -- Inicializacion de la base de datos ------------------------------------

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

// -- Endpoints --------------------------------------------------------------

app.MapGet("/productos", (CatalogoRepositorio repositorio) =>
{
    return repositorio.ObtenerTodos();
});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) =>
{
    var producto = repositorio.ObtenerPorId(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapPost("/productos", (ProductoDto producto, CatalogoRepositorio repositorio) =>
{
    var creado = repositorio.Crear(producto);
    return creado is null
        ? Results.BadRequest("Ya existe un producto con ese codigo.")
        : Results.Created($"/productos/{creado.Id}", creado);
});

app.MapPut("/productos/{id}", (int id, ProductoDto producto, CatalogoRepositorio repositorio) =>
{
    var actualizado = repositorio.Actualizar(id, producto);
    return actualizado.Resultado switch
    {
        ResultadoProducto.NoEncontrado => Results.NotFound(),
        ResultadoProducto.CodigoDuplicado => Results.BadRequest("Ya existe un producto con ese codigo."),
        _ => Results.Ok(actualizado.Producto)
    };
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) =>
{
    return repositorio.Eliminar(id) ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/productos/{id}/movimientos",
(int id, CatalogoRepositorio repositorio) =>
{
    return repositorio.ObtenerMovimientos(id);
});

app.MapPost("/productos/{productoId}/movimientos",
(int productoId, MovimientoDto movimiento, CatalogoRepositorio repositorio) =>
{
    var registrado = repositorio.RegistrarMovimiento(productoId, movimiento);
    return registrado.Resultado switch
    {
        ResultadoMovimiento.NoEncontrado => Results.NotFound(),
        ResultadoMovimiento.CantidadInvalida => Results.BadRequest("La cantidad debe ser positiva."),
        ResultadoMovimiento.TipoInvalido => Results.BadRequest("El tipo de movimiento no es valido."),
        ResultadoMovimiento.StockInsuficiente => Results.BadRequest("No hay stock suficiente para registrar la venta."),
        _ => Results.Created($"/productos/{productoId}/movimientos", registrado.Movimiento)
    };
});

app.Run("http://localhost:5050");

// -- Modelo -----------------------------------------------------------------

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

record ProductoDto(string Codigo, string Nombre, decimal Precio, int Stock);

record ProductoActualizado(ResultadoProducto Resultado, Producto? Producto);

enum ResultadoProducto
{
    Ok,
    NoEncontrado,
    CodigoDuplicado
}

enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

record class MovimientoDeProducto(
    int Id,
    int ProductoId,
    TipoMovimiento Tipo,
    int Cantidad,
    DateTime Fecha
);

record MovimientoDto(
    TipoMovimiento Tipo,
    int Cantidad);

record MovimientoRegistrado(ResultadoMovimiento Resultado, MovimientoDeProducto? Movimiento);

enum ResultadoMovimiento
{
    Ok,
    NoEncontrado,
    CantidadInvalida,
    TipoInvalido,
    StockInsuficiente
}

// -- DbContext --------------------------------------------------------------

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

// -- Repositorio ------------------------------------------------------------

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.AddRange(
                new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100),
                new Producto(2, "P002", "Azucar 1kg", 900m, 50),
                new Producto(3, "P003", "Arroz 500g", 1200m, 80)
            );
            db.SaveChanges();
        }
    }

    public Producto? ObtenerPorId(int id) =>
        db.Productos.Find(id);

    public List<Producto> ObtenerTodos() =>
        db.Productos.OrderBy(p => p.Id).ToList();

    public Producto? Crear(ProductoDto dto)
    {
        if (ExisteCodigo(dto.Codigo)) {
            return null;
        }

        var producto = new Producto(0, dto.Codigo, dto.Nombre, dto.Precio, dto.Stock);
        db.Productos.Add(producto);
        db.SaveChanges();
        return producto;
    }

    public ProductoActualizado Actualizar(int id, ProductoDto dto)
    {
        var producto = ObtenerPorId(id);
        if (producto is null) {
            return new ProductoActualizado(ResultadoProducto.NoEncontrado, null);
        }

        if (ExisteCodigo(dto.Codigo, id)) {
            return new ProductoActualizado(ResultadoProducto.CodigoDuplicado, null);
        }

        var actualizado = producto with {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Precio = dto.Precio,
            Stock = dto.Stock
        };

        db.Entry(producto).CurrentValues.SetValues(actualizado);
        db.SaveChanges();
        return new ProductoActualizado(ResultadoProducto.Ok, actualizado);
    }

    public bool Eliminar(int id)
    {
        var producto = ObtenerPorId(id);
        if (producto is null) {
            return false;
        }

        var movimientos = db.Movimientos.Where(m => m.ProductoId == id).ToList();
        db.Movimientos.RemoveRange(movimientos);
        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> ObtenerMovimientos(int productoId)
    {
        return db.Movimientos
                 .Where(m => m.ProductoId == productoId)
                 .OrderByDescending(m => m.Fecha)
                 .ToList();
    }

    public MovimientoRegistrado RegistrarMovimiento(int productoId, MovimientoDto dto)
    {
        if (dto.Cantidad <= 0) {
            return new MovimientoRegistrado(ResultadoMovimiento.CantidadInvalida, null);
        }

        if (!Enum.IsDefined(dto.Tipo)) {
            return new MovimientoRegistrado(ResultadoMovimiento.TipoInvalido, null);
        }

        var producto = ObtenerPorId(productoId);
        if (producto is null) {
            return new MovimientoRegistrado(ResultadoMovimiento.NoEncontrado, null);
        }

        var nuevoStock = dto.Tipo switch
        {
            TipoMovimiento.Compra => producto.Stock + dto.Cantidad,
            TipoMovimiento.Venta => producto.Stock - dto.Cantidad,
            TipoMovimiento.Ajuste => dto.Cantidad,
            _ => producto.Stock
        };

        if (nuevoStock < 0) {
            return new MovimientoRegistrado(ResultadoMovimiento.StockInsuficiente, null);
        }

        using var transaccion = db.Database.BeginTransaction();

        var movimiento = new MovimientoDeProducto(
            0,
            productoId,
            dto.Tipo,
            dto.Cantidad,
            DateTime.Now
        );

        var actualizado = producto with { Stock = nuevoStock };
        db.Entry(producto).CurrentValues.SetValues(actualizado);
        db.Movimientos.Add(movimiento);
        db.SaveChanges();
        transaccion.Commit();

        return new MovimientoRegistrado(ResultadoMovimiento.Ok, movimiento);
    }

    private bool ExisteCodigo(string codigo, int? ignorarId = null)
    {
        return db.Productos.Any(p =>
            p.Codigo == codigo && (!ignorarId.HasValue || p.Id != ignorarId.Value));
    }
}