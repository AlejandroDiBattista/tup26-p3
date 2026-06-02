#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);
var dbpath = Path.Combine(Environment.CurrentDirectory, "catalogo.db");

builder.Services.Configure<JsonOptions>(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    
builder.Services.AddDbContext<CatalogoDbContext>(options => {
    
    options.UseSqlite($"Data Source={dbpath}");
});

builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope()) {

    var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/", () => Results.Ok(new {
        
        Aplicacion = "Catálogo de Productos",
        Endpoints = new[] {
            
            "GET /productos",
            "GET /productos/{id}",
            "POST /productos",
            "PUT /productos/{id}",
            "DELETE /productos/{id}",
            "GET /productos/{productosId}/movimientos",
            "POST /productos/{productosId}/movimientos"

        }
}));

// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/productos", async(CatalogoDbContext db) => {

    var productos = await db.Productos
    .AsNoTracking()
    .OrderBy(producto => producto.Codigo)
    .Select(producto => producto.ToDto())
    .ToListAsync();
    
    return Results.Ok(productos);
}); 

app.MapGet("/productos/{id:int}", async(int id, CatalogoDbContext db) => {

    var producto = await db.Productos
    .AsNoTracking()
    .FirstOrDefaultAsync(producto => producto.Id == id);

    return producto is not null ? Results.NotFound(new ApiError("No existe un producto con ese ID")) : Results.Ok(producto.ToDto());
});

app.MapPost("/productos", async(ProductoRequest request, CatalogoDbContext db) => {

    var Error = ValidarProducto(request);
    if (Error is not null) {
        return Results.BadRequest(new ApiError(Error));
    }

    var codigo = NormalizarCodigo(request.Codigo);

    var existeCodigo = await db.Productos.AnyAsync(producto => producto.Codigo == codigo);
    if (existeCodigo) {
        return Results.Conflict(new ApiError("Ya existe un producto con ese código"));
    }

    var producto = new Producto {
        
        Codigo = codigo,
        Nombre = request.Nombre.Trim(),
        Precio = request.Precio,
        Stock = request.Stock
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync();

    return Results.Created($"/productos/{producto.Id}", producto.ToDto());
});

app.MapPut("/productos/{id:int}", async(int id, ProductoRequest request, CatalogoDbContext db) => {

    var producto = await db.Productos.FindAsync(id);
    if (producto is null) {
        return Results.NotFound(new ApiError("No existe un producto con ese ID"));
    }

    var Error = ValidarProducto(request);
    if (Error is not null) {
        return Results.BadRequest(new ApiError(Error));
    }

    var codigo = NormalizarCodigo(request.Codigo);

    var existeCodigo = await db.Productos.AnyAsync(otro => otro.Codigo == codigo && otro.Id != id);
    if (existeCodigo) {
        return Results.Conflict(new ApiError("Ya existe un producto con ese codigo"));
    }

    producto.Codigo = codigo;
    producto.Nombre = request.Nombre.Trim();
    producto.Precio = request.Precio;
    producto.Stock = request.Stock;

    await db.SaveChangesAsync();

    return Results.Ok(producto.ToDto());
});

app.MapDelete("/productos/{id:int}", async(int id, CatalogoDbContext db) => {

    var producto = await db.Productos.FindAsync(id);
    if (producto is null) {
        return Results.NotFound(new ApiError("No existe un producto con ese ID"));
    }

    db.Productos.Remove(producto);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/productos/{productoId:int}/movimientos", async(int productoId, CatalogoDbContext db) => {

    var existeproducto = await db.Productos.AnyAsync(producto => producto.Id == productoId);
    if (!existeproducto) {
        return Results.NotFound(new ApiError("No existe un producto con ese ID"));
    }

    var movimientos = await db.Movimiento
    .AsNoTracking()
    .Where(movimiento => movimiento.ProductoId == productoId)
    .OrderByDescending(movimiento => movimiento.Fecha)
    .Select(movimiento => movimiento.ToDto())
    .ToListAsync();

    return Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos", async(int productoId, MovimientoRequest request, CatalogoDbContext db) => {

    if (request.Cantidad <= 0) {
        return Results.BadRequest(new ApiError("La cantidad debe ser mayor a cero"));
    }

    var producto = await db.Productos.FindAsync(productoId);
    if (producto is null) {
        return Results.NotFound(new ApiError("No existe un producto con ese ID"));
    }

    var nuevoStock = request.Tipo switch {
        TipoMovimiento.Compra => producto.Stock + request.Cantidad,
        TipoMovimiento.Venta => producto.Stock - request.Cantidad,
        TipoMovimiento.Ajuste => request.Cantidad,
        _ => producto.Stock
    };

    if (nuevoStock < 0) {
        return Results.BadRequest(new ApiError("El movimiento no se puede registrar porque dejaría el stock en negativo"));
    }

    using var tx = await db.Database.BeginTransactionAsync();

    producto.Stock = nuevoStock;
    var movimiento = new MovimientoDeProducto {
        Producto = producto,
        ProductoId = productoId,
        Tipo = request.Tipo,
        Cantidad = request.Cantidad,
        Fecha = DateTime.Now
    };

    db.Movimiento.Add(movimiento);
    await db.SaveChangesAsync();
    await tx.CommitAsync();

    return Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento.ToDto());
});

app.Run("http://localhost:5050");

static string? ValidarProducto(ProductoRequest request) {

    if (string.IsNullOrWhiteSpace(request.Codigo)) {

        return "El código es obligatorio";
    }

    if (string.Codigo.Trim().Length > 30) {

        return "El código no puede exceder los 30 caracteres";
    }

    if (string.IsNullOrWhiteSpace(request.Nombre)) {

        return "El nombre es obligatorio";
    }

    if (string.Nombre.Trim().Length > 100) {

        return "El nombre no puede exceder los 100 caracteres";
    }

    if (request.Precio < 0) {

        return "El precio no puede ser negativo";
    }

    if (request.Stock < 0) {

        return "El stock no puede ser negativo";
    }

    return null;
}

static string NormalizarCodigo(string codigo){

     return codigo.Trim().ToUpperInvariant(); 
     }


});
// ── Modelo ────────────────────────────────────────────────────────────────

public sealed class Producto {

    public int Id { get; set; }

    [MaxLength(30)]
    public string Codigo { get; set; } = "";

    [MaxLength(100)]
    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public List<MovimientoDeProducto> Movimientos { get; set; } = ;

}
public class MovimientoDeProducto {

    public int Id { get; set; }

    public int ProductoId { get; set; }

    public Producto? Producto { get; set; }

    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

}

[JsonConverter(typeof(JsonStringEnumConverter))]

public enum TipoMovimiento {

    Compra,
    Venta,
    Ajuste
}

public sealed record ProductoRequest(string Codigo, string Nombre, decimal Precio, int Stock);

public sealed record MovimientoRequest(TipoMovimiento Tipo, int Cantidad);

public sealed record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

public sealed record MovimientoDto(int Id, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);

public sealed record ApiError(string Error);

public static record DtoExtensions {

    public static ProductoDto ToDto(this Producto producto) =>
        new ProductoDto(producto.Id, producto.Codigo, producto.Nombre, producto.Precio, producto.Stock);

    public static MovimientoDto ToDto(this MovimientoDeProducto movimiento) =>
        new MovimientoDto(movimiento.Id, movimiento.Tipo, movimiento.Cantidad, movimiento.Fecha);

// ── DbContext ─────────────────────────────────────────────────────────────

public sealedclass CatalogoDbContext (DbContextOptions<CatalogoDbContext> options): DbContext {

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {

        modelBuilder.Entity<Producto>(entity => {

            entity.HasKey(producto => producto.Id);
            entity.HasIndex(producto => producto.Codigo).IsUnique();
            entity.Property(producto => producto.Codigo).IsRequired().HasMaxLength(30);
            entity.Property(producto => producto.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(producto => producto.Precio)().HasColumnType("decimal(18,2)");
            entity.Property(producto => producto.Stock).IsRequired();
            entity.HasMany(Producto => Producto.Movimientos)
                .WithOne(movimiento => movimiento.Producto)
                .HasForeignKey(movimiento => movimiento.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovimientoDeProducto>(entity => {
            
            entity.HasKey(movimiento => movimiento.Id);
            entity.Property(movimiento => movimiento.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(movimiento => movimiento.Cantidad).IsRequired();
            entity.Property(movimiento => movimiento.Fecha).IsRequired();
        });
    }

}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDbContext db;

    public CatalogoRepositorio(CatalogoDbContext db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.Add(new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100));
            db.SaveChanges();
        }
    }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}