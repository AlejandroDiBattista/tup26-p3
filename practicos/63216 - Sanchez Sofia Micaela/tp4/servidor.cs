using Microsoft.EntityFrameworkCore;
#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

// ── Configuración ──────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddDbContext<CatalogoDb>(opt =>
    opt.UseSqlite("Data Source=catalogo.db"));

builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repo.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────
app.MapGet("/", () => "CatalogoREST funcionando");
app.MapGet("/productos", async (CatalogoRepositorio repo) =>
{
    return Results.Ok(await repo.ListarProductos());
});

app.MapGet("/productos/{id:int}", async (int id, CatalogoRepositorio repo) =>
{
    Producto? producto = await repo.BuscarProducto(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});
app.MapPost("/productos", async (ProductoDatos datos, CatalogoRepositorio repo) =>
{
    string? error = ValidarProducto(datos);
    if (error is not null)
    {
        return Results.BadRequest(error);
    }

    Producto? producto = await repo.CrearProducto(datos);
    if (producto is null)
    {
        return Results.Conflict("Ya existe un producto con ese codigo.");
    }

    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id:int}", async (int id, ProductoDatos datos, CatalogoRepositorio repo) =>
{
    string? error = ValidarProducto(datos);
    if (error is not null)
    {
        return Results.BadRequest(error);
    }

    Producto? producto = await repo.ModificarProducto(id, datos);

    if (producto is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(producto);
});

app.MapDelete("/productos/{id:int}", async (int id, CatalogoRepositorio repo) =>
{
    bool eliminado = await repo.EliminarProducto(id);
    return eliminado ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, CatalogoRepositorio repo) =>
{
    List<MovimientoDeProducto>? movimientos = await repo.ListarMovimientos(productoId);
    return movimientos is null ? Results.NotFound() : Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos", async (int productoId, MovimientoDatos datos, CatalogoRepositorio repo) =>
{
    if (datos.Cantidad <= 0)
    {
        return Results.BadRequest("La cantidad debe ser positiva.");
    }

// ── Modelo ────────────────────────────────────────────────────────────────
    MovimientoDeProducto? movimiento = await repo.RegistrarMovimiento(productoId, datos);

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
    if (movimiento is null)
    {
        return Results.BadRequest("No se pudo registrar el movimiento.");
    }

// ── DbContext ─────────────────────────────────────────────────────────────
    return Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento);
});

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
app.Run("http://localhost:5051");
static string? ValidarProducto(ProductoDatos datos)
{
    if (string.IsNullOrWhiteSpace(datos.Codigo))
    {
        return "El codigo es obligatorio.";
    }

    if (string.IsNullOrWhiteSpace(datos.Nombre))
    {
        return "El nombre es obligatorio.";
    }

    if (datos.Precio < 0)
    {
        return "El precio no puede ser negativo.";
    }

    if (datos.Stock < 0)
    {
        return "El stock no puede ser negativo.";
    }

    return null;
}

// ── Repositorio ───────────────────────────────────────────────────────────
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

    [JsonIgnore]
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}

class MovimientoDeProducto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }

    [JsonIgnore]
    public Producto? Producto { get; set; }
}
record ProductoDatos(string Codigo, string Nombre, decimal Precio, int Stock);

record MovimientoDatos(TipoMovimiento Tipo, int Cantidad);
class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options)
    {
    }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasIndex(p => p.Codigo).IsUnique();
            entity.Property(p => p.Codigo).IsRequired();
            entity.Property(p => p.Nombre).IsRequired();
            entity.Property(p => p.Precio).HasPrecision(18, 2);
        });

        modelBuilder.Entity<MovimientoDeProducto>(entity =>
        {
            entity.Property(m => m.Tipo).HasConversion<string>();

            entity.HasOne(m => m.Producto)
                .WithMany(p => p.Movimientos)
                .HasForeignKey(m => m.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
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
            db.Productos.AddRange(
              new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 900m, Stock = 60 },
              new Producto { Codigo = "P003", Nombre = "Harina 000 1kg", Precio = 750m, Stock = 80 },
              new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 }
            );

            db.SaveChanges();
        }
    }
    public Task<List<Producto>> ListarProductos()
    {
        return db.Productos
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public Task<Producto?> BuscarProducto(int id)
    {
        return db.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    public async Task<Producto?> CrearProducto(ProductoDatos datos)
    {
        string codigo = datos.Codigo.Trim();

        bool existeCodigo = await db.Productos.AnyAsync(p => p.Codigo == codigo);
        if (existeCodigo)
        {
            return null;
        }

        Producto producto = new()
        {
            Codigo = codigo,
            Nombre = datos.Nombre.Trim(),
            Precio = datos.Precio,
            Stock = datos.Stock
        };

        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        return producto;
    }

    public async Task<Producto?> ModificarProducto(int id, ProductoDatos datos)
    {
        Producto? producto = await db.Productos.FindAsync(id);
        if (producto is null)
        {
            return null;
        }

        producto.Codigo = datos.Codigo.Trim();
        producto.Nombre = datos.Nombre.Trim();
        producto.Precio = datos.Precio;
        producto.Stock = datos.Stock;

        await db.SaveChangesAsync();

        return producto;
    }

    public async Task<bool> EliminarProducto(int id)
    {
        Producto? producto = await db.Productos.FindAsync(id);
        if (producto is null)
        {
            return false;
        }

        db.Productos.Remove(producto);
        await db.SaveChangesAsync();

        return true;
    }
    public async Task<List<MovimientoDeProducto>?> ListarMovimientos(int productoId)
    {
        bool existeProducto = await db.Productos.AnyAsync(p => p.Id == productoId);

        if (!existeProducto)
        {
            return null;
        }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}        return await db.Movimientos
        return await db.Movimientos
            .AsNoTracking()
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();
    }

    public async Task<MovimientoDeProducto?> RegistrarMovimiento(int productoId, MovimientoDatos datos)
    {
        Producto? producto = await db.Productos.FindAsync(productoId);

        if (producto is null)
        {
            return null;
        }

        int nuevoStock = datos.Tipo switch
        {
            TipoMovimiento.Compra => producto.Stock + datos.Cantidad,
            TipoMovimiento.Venta => producto.Stock - datos.Cantidad,
            TipoMovimiento.Ajuste => datos.Cantidad,
            _ => producto.Stock
        };

        if (nuevoStock < 0)
        {
            return null;
        }

        MovimientoDeProducto movimiento = new()
        {
            ProductoId = productoId,
            Tipo = datos.Tipo,
            Cantidad = datos.Cantidad,
            Fecha = DateTime.Now
        };

        producto.Stock = nuevoStock;
        db.Movimientos.Add(movimiento);

        await db.SaveChangesAsync();

        return movimiento;
    }
}
