#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/productos", async (CatalogoRepositorio repo) =>
    Results.Ok(await repo.ListarProductosAsync()));

app.MapGet("/productos/{id:int}", async (int id, CatalogoRepositorio repo) => {
    var producto = await repo.BuscarProductoAsync(id);
    return producto is null ? Results.NotFound("Producto no encontrado.") : Results.Ok(producto);
});

app.MapPost("/productos", async (ProductoDatos datos, CatalogoRepositorio repo) => {
    var error = ValidarProducto(datos);
    if (error is not null) return Results.BadRequest(error);

    var resultado = await repo.CrearProductoAsync(datos);
    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Created($"/productos/{resultado.Producto!.Id}", resultado.Producto);
});

app.MapPut("/productos/{id:int}", async (int id, ProductoDatos datos, CatalogoRepositorio repo) => {
    var error = ValidarProducto(datos);
    if (error is not null) return Results.BadRequest(error);

    var resultado = await repo.ModificarProductoAsync(id, datos);
    if (resultado.NoEncontrado) return Results.NotFound("Producto no encontrado.");

    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Ok(resultado.Producto);
});

app.MapDelete("/productos/{id:int}", async (int id, CatalogoRepositorio repo) => {
    var eliminado = await repo.EliminarProductoAsync(id);
    return eliminado ? Results.NoContent() : Results.NotFound("Producto no encontrado.");
});

app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, CatalogoRepositorio repo) => {
    if (!await repo.ExisteProductoAsync(productoId)) {
        return Results.NotFound("Producto no encontrado.");
    }

    return Results.Ok(await repo.ListarMovimientosAsync(productoId));
});

app.MapPost("/productos/{productoId:int}/movimientos", async (int productoId, MovimientoDatos datos, CatalogoRepositorio repo) => {
    var resultado = await repo.RegistrarMovimientoAsync(productoId, datos);
    if (resultado.NoEncontrado) return Results.NotFound("Producto no encontrado.");

    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Created($"/productos/{productoId}/movimientos/{resultado.Movimiento!.Id}", resultado.Movimiento);
});

app.Run("http://localhost:5050");

static string? ValidarProducto(ProductoDatos datos) {
    if (string.IsNullOrWhiteSpace(datos.Codigo)) return "El codigo es obligatorio.";
    if (string.IsNullOrWhiteSpace(datos.Nombre)) return "El nombre es obligatorio.";
    if (datos.Precio < 0) return "El precio no puede ser negativo.";
    if (datos.Stock < 0) return "El stock no puede ser negativo.";

    return null;
}

// ── Modelo ────────────────────────────────────────────────────────────────
public class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    [JsonIgnore]
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}
public enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

public class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    [JsonIgnore]
    public Producto? Producto { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

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
            new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 900m, Stock = 60 },
            new Producto { Codigo = "P003", Nombre = "Cafe 250g", Precio = 3200m, Stock = 30 }
        );
            db.SaveChanges();
        }
    }

    public async Task<List<Producto>> ListarProductosAsync() =>
        await db.Productos
            .OrderBy(p => p.Codigo)
            .ToListAsync();

    public async Task<Producto?> BuscarProductoAsync(int id) =>
        await db.Productos.FindAsync(id);

    public async Task<bool> ExisteProductoAsync(int id) =>
        await db.Productos.AnyAsync(p => p.Id == id);

    public async Task<ResultadoProducto> CrearProductoAsync(ProductoDatos datos) {
        if (await CodigoRepetidoAsync(datos.Codigo, 0)) {
            return ResultadoProducto.Fallo("Ya existe un producto con ese codigo.");
        }

        var producto = new Producto {
            Codigo = datos.Codigo.Trim(),
            Nombre = datos.Nombre.Trim(),
            Precio = datos.Precio,
            Stock = datos.Stock
        };

        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        return ResultadoProducto.Ok(producto);
    }

    public async Task<ResultadoProducto> ModificarProductoAsync(int id, ProductoDatos datos) {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null) return ResultadoProducto.NoEncontradoResult();

        if (await CodigoRepetidoAsync(datos.Codigo, id)) {
            return ResultadoProducto.Fallo("Ya existe otro producto con ese codigo.");
        }

        producto.Codigo = datos.Codigo.Trim();
        producto.Nombre = datos.Nombre.Trim();
        producto.Precio = datos.Precio;
        producto.Stock = datos.Stock;

        await db.SaveChangesAsync();
        return ResultadoProducto.Ok(producto);
    }

    public async Task<List<MovimientoDeProducto>> ListarMovimientosAsync(int productoId) =>
        await db.Movimientos
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();
    
    public async Task<ResultadoMovimiento> RegistrarMovimientoAsync(int productoId, MovimientoDatos datos) {
        var producto = await db.Productos.FindAsync(productoId);
        if (producto is null) return ResultadoMovimiento.NoEncontradoResult();

        if (!Enum.TryParse<TipoMovimiento>(datos.Tipo, true, out var tipo)) {
            return ResultadoMovimiento.Fallo("El tipo debe ser Compra, Venta o Ajuste.");
        }

        if (datos.Cantidad < 0 || (tipo != TipoMovimiento.Ajuste && datos.Cantidad == 0)) {
            return ResultadoMovimiento.Fallo("La cantidad debe ser positiva.");
        }

        if (tipo == TipoMovimiento.Compra) {
            producto.Stock += datos.Cantidad;
        } else if (tipo == TipoMovimiento.Venta) {
            if (producto.Stock < datos.Cantidad) {
                return ResultadoMovimiento.Fallo("No hay stock suficiente para la venta.");
            }

            producto.Stock -= datos.Cantidad;
        } else {
            producto.Stock = datos.Cantidad;
        }

        var movimiento = new MovimientoDeProducto {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = datos.Cantidad,
            Fecha = DateTime.Now
        };

        db.Movimientos.Add(movimiento);
        await db.SaveChangesAsync();
        return ResultadoMovimiento.Ok(movimiento);
    }

    private async Task<bool> CodigoRepetidoAsync(string codigo, int idActual) =>
        await db.Productos.AnyAsync(p => p.Codigo == codigo.Trim() && p.Id != idActual);

    public async Task<bool> EliminarProductoAsync(int id) {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null) return false;

        db.Productos.Remove(producto);
        await db.SaveChangesAsync();
        return true;
    }   
}

public class ProductoDatos {
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

record ResultadoProducto(Producto? Producto, string? Error, bool NoEncontrado) {
    public static ResultadoProducto Ok(Producto producto) => new(producto, null, false);
    public static ResultadoProducto Fallo(string error) => new(null, error, false);
    public static ResultadoProducto NoEncontradoResult() => new(null, null, true);
}

public class MovimientoDatos {
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
}

record ResultadoMovimiento(MovimientoDeProducto? Movimiento, string? Error, bool NoEncontrado) {
    public static ResultadoMovimiento Ok(MovimientoDeProducto movimiento) => new(movimiento, null, false);
    public static ResultadoMovimiento Fallo(string error) => new(null, error, false);
    public static ResultadoMovimiento NoEncontradoResult() => new(null, null, true);
}