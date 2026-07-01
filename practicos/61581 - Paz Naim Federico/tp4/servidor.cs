#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();
builder.Services.ConfigureHttpJsonOptions(opt => {
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoRepositorio repositorio) =>
    Results.Ok(repositorio.ListarProductos()));

app.MapGet("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.ObtenerProducto(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapPost("/productos", (ProductoRequest request, CatalogoRepositorio repositorio) => {
    var resultado = repositorio.CrearProducto(request);
    return resultado.Exito
        ? Results.Created($"/productos/{resultado.Producto!.Id}", resultado.Producto)
        : Results.BadRequest(resultado.Mensaje);
});

app.MapPut("/productos/{id:int}", (int id, ProductoRequest request, CatalogoRepositorio repositorio) => {
    var resultado = repositorio.ModificarProducto(id, request);
    if (resultado.NoEncontrado) return Results.NotFound();

    return resultado.Exito ? Results.Ok(resultado.Producto) : Results.BadRequest(resultado.Mensaje);
});

app.MapDelete("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) =>
    repositorio.EliminarProducto(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/productos/{productoId:int}/movimientos", (int productoId, CatalogoRepositorio repositorio) => {
    if (repositorio.ObtenerProducto(productoId) is null) return Results.NotFound();

    return Results.Ok(repositorio.ListarMovimientos(productoId));
});

app.MapPost("/productos/{productoId:int}/movimientos", (int productoId, MovimientoRequest request, CatalogoRepositorio repositorio) => {
    var resultado = repositorio.RegistrarMovimiento(productoId, request);
    if (resultado.NoEncontrado) return Results.NotFound();

    return resultado.Exito
        ? Results.Created($"/productos/{productoId}/movimientos/{resultado.Movimiento!.Id}", resultado.Movimiento)
        : Results.BadRequest(resultado.Mensaje);
});

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    [JsonIgnore]
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}

class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    [JsonIgnore]
    public Producto? Producto { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

record ProductoRequest(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoRequest(TipoMovimiento Tipo, int Cantidad);
record ResultadoProducto(bool Exito, Producto? Producto = null, string? Mensaje = null, bool NoEncontrado = false);
record ResultadoMovimiento(bool Exito, MovimientoDeProducto? Movimiento = null, string? Mensaje = null, bool NoEncontrado = false);

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<Producto>()
            .HasMany(p => p.Movimientos)
            .WithOne(m => m.Producto)
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
            db.Productos.AddRange(
                new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
                new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 950m, Stock = 80 },
                new Producto { Codigo = "P003", Nombre = "Harina 000 1kg", Precio = 780m, Stock = 60 },
                new Producto { Codigo = "P004", Nombre = "Aceite Girasol 900ml", Precio = 2100m, Stock = 45 },
                new Producto { Codigo = "P005", Nombre = "Arroz Largo Fino 1kg", Precio = 1200m, Stock = 70 }
            );
            db.SaveChanges();
        }
    }

    public List<Producto> ListarProductos() =>
        db.Productos.OrderBy(p => p.Codigo).ToList();

    public Producto? ObtenerProducto(int id) =>
        db.Productos.FirstOrDefault(p => p.Id == id);

    public ResultadoProducto CrearProducto(ProductoRequest request) {
        var validacion = ValidarProducto(request);
        if (validacion is not null) return new ResultadoProducto(false, Mensaje: validacion);

        if (ExisteCodigo(request.Codigo)) {
            return new ResultadoProducto(false, Mensaje: "Ya existe un producto con ese codigo.");
        }

        var producto = new Producto {
            Codigo = request.Codigo.Trim(),
            Nombre = request.Nombre.Trim(),
            Precio = request.Precio,
            Stock = request.Stock
        };

        db.Productos.Add(producto);
        db.SaveChanges();

        return new ResultadoProducto(true, producto);
    }

    public ResultadoProducto ModificarProducto(int id, ProductoRequest request) {
        var producto = ObtenerProducto(id);
        if (producto is null) return new ResultadoProducto(false, NoEncontrado: true);

        var validacion = ValidarProducto(request);
        if (validacion is not null) return new ResultadoProducto(false, Mensaje: validacion);

        if (ExisteCodigo(request.Codigo, id)) {
            return new ResultadoProducto(false, Mensaje: "Ya existe otro producto con ese codigo.");
        }

        producto.Codigo = request.Codigo.Trim();
        producto.Nombre = request.Nombre.Trim();
        producto.Precio = request.Precio;
        producto.Stock = request.Stock;
        db.SaveChanges();

        return new ResultadoProducto(true, producto);
    }

    public bool EliminarProducto(int id) {
        var producto = ObtenerProducto(id);
        if (producto is null) return false;

        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientos(int productoId) =>
        db.Movimientos
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToList();

    public ResultadoMovimiento RegistrarMovimiento(int productoId, MovimientoRequest request) {
        var producto = ObtenerProducto(productoId);
        if (producto is null) return new ResultadoMovimiento(false, NoEncontrado: true);

        if (request.Cantidad <= 0) {
            return new ResultadoMovimiento(false, Mensaje: "La cantidad debe ser positiva.");
        }

        if (request.Tipo == TipoMovimiento.Venta && request.Cantidad > producto.Stock) {
            return new ResultadoMovimiento(false, Mensaje: "No hay stock suficiente para registrar la venta.");
        }

        using var transaccion = db.Database.BeginTransaction();

        producto.Stock = request.Tipo switch {
            TipoMovimiento.Compra => producto.Stock + request.Cantidad,
            TipoMovimiento.Venta => producto.Stock - request.Cantidad,
            TipoMovimiento.Ajuste => request.Cantidad,
            _ => producto.Stock
        };

        var movimiento = new MovimientoDeProducto {
            ProductoId = productoId,
            Tipo = request.Tipo,
            Cantidad = request.Cantidad,
            Fecha = DateTime.Now
        };

        db.Movimientos.Add(movimiento);
        db.SaveChanges();
        transaccion.Commit();

        return new ResultadoMovimiento(true, movimiento);
    }

    private bool ExisteCodigo(string codigo, int? exceptoId = null) {
        var codigoNormalizado = codigo.Trim().ToUpper();
        return db.Productos.Any(p => p.Codigo.ToUpper() == codigoNormalizado && p.Id != exceptoId);
    }

    private static string? ValidarProducto(ProductoRequest request) {
        if (string.IsNullOrWhiteSpace(request.Codigo)) return "El codigo es obligatorio.";
        if (string.IsNullOrWhiteSpace(request.Nombre)) return "El nombre es obligatorio.";
        if (request.Precio < 0) return "El precio no puede ser negativo.";
        if (request.Stock < 0) return "El stock no puede ser negativo.";

        return null;
    }
}