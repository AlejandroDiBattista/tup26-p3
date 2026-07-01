#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();
builder.Services.ConfigureHttpJsonOptions(opt =>
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();



using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}


app.MapGet("/productos", (CatalogoRepositorio repo) =>
    Results.Ok(repo.ListarProductos()));

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    var producto = repo.ObtenerProducto(id);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.MapPost("/productos", (ProductoNuevoDto dto, CatalogoRepositorio repo) => {
    if (!ProductoValido(dto, out var error)) return Results.BadRequest(error);

    var producto = repo.CrearProducto(dto);
    if (producto is null) return Results.Conflict("Ya existe un producto con ese codigo.");

    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id}", (int id, ProductoNuevoDto dto, CatalogoRepositorio repo) => {
    if (!ProductoValido(dto, out var error)) return Results.BadRequest(error);
    if (repo.ObtenerProducto(id) is null) return Results.NotFound();
    if (repo.CodigoEnUso(dto.Codigo, id)) return Results.Conflict("Ya existe un producto con ese codigo.");

    var producto = repo.ModificarProducto(id, dto);
    return Results.Ok(producto);
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repo) => {
    var eliminado = repo.EliminarProducto(id);
    return eliminado ? Results.NoContent() : Results.NotFound();
});



app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoRepositorio repo) => {
    if (repo.ObtenerProducto(productoId) is null) return Results.NotFound();
    return Results.Ok(repo.ListarMovimientos(productoId));
});

app.MapPost("/productos/{productoId}/movimientos", (int productoId, MovimientoNuevoDto dto, CatalogoRepositorio repo) => {
    if (dto.Cantidad <= 0) return Results.BadRequest("La cantidad del movimiento debe ser positiva.");

    var resultado = repo.RegistrarMovimiento(productoId, dto);
    if (resultado is null) return Results.NotFound();
    return Results.Created($"/productos/{productoId}/movimientos/{resultado.Id}", resultado);
});

app.Run("http://localhost:5050");

static bool ProductoValido(ProductoNuevoDto dto, out string error) {
    if (string.IsNullOrWhiteSpace(dto.Codigo)) {
        error = "El codigo es obligatorio.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(dto.Nombre)) {
        error = "El nombre es obligatorio.";
        return false;
    }

    if (dto.Precio < 0) {
        error = "El precio no puede ser negativo.";
        return false;
    }

    if (dto.Stock < 0) {
        error = "El stock no puede ser negativo.";
        return false;
    }

    error = "";
    return true;
}



enum TipoMovimiento { Compra, Venta, Ajuste }

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}


record ProductoNuevoDto(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoNuevoDto(TipoMovimiento Tipo, int Cantidad);




class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();
    }
}


class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Productos_Codigo
            ON Productos (Codigo)
            """);

        if (!db.Productos.Any()) {
            db.Productos.AddRange(
                new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 }
            );
            db.SaveChanges();
        }
    }

    public List<Producto> ListarProductos() => db.Productos.OrderBy(p => p.Codigo).ToList();

    public Producto? ObtenerProducto(int id) => db.Productos.Find(id);

    public Producto? CrearProducto(ProductoNuevoDto dto) {
        if (CodigoEnUso(dto.Codigo)) return null;

        var producto = new Producto {
            Codigo = dto.Codigo.Trim(),
            Nombre = dto.Nombre.Trim(),
            Precio = dto.Precio,
            Stock = dto.Stock,
        };
        db.Productos.Add(producto);
        db.SaveChanges();
        return producto;
    }

    public Producto? ModificarProducto(int id, ProductoNuevoDto dto) {
        var producto = db.Productos.Find(id);
        if (producto is null) return null;
        if (CodigoEnUso(dto.Codigo, id)) return null;

        producto.Codigo = dto.Codigo.Trim();
        producto.Nombre = dto.Nombre.Trim();
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        db.SaveChanges();
        return producto;
    }

    public bool EliminarProducto(int id) {
        var producto = db.Productos.Find(id);
        if (producto is null) return false;

        db.Movimientos.RemoveRange(db.Movimientos.Where(m => m.ProductoId == id));
        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientos(int productoId) =>
        db.Movimientos
          .Where(m => m.ProductoId == productoId)
          .OrderByDescending(m => m.Fecha)
          .ToList();

    public MovimientoDeProducto? RegistrarMovimiento(int productoId, MovimientoNuevoDto dto) {
        var producto = db.Productos.Find(productoId);
        if (producto is null) return null;

        producto.Stock = dto.Tipo switch {
            TipoMovimiento.Compra => producto.Stock + dto.Cantidad,
            TipoMovimiento.Venta => producto.Stock - dto.Cantidad,
            TipoMovimiento.Ajuste => dto.Cantidad,
            _ => producto.Stock
        };

        var movimiento = new MovimientoDeProducto {
            ProductoId = productoId,
            Tipo = dto.Tipo,
            Cantidad = dto.Cantidad,
            Fecha = DateTime.Now,
        };
        db.Movimientos.Add(movimiento);
        db.SaveChanges();
        return movimiento;
    }


    public bool CodigoEnUso(string codigo, int? exceptoId = null) =>
        db.Productos.Any(p => p.Codigo == codigo.Trim() && (!exceptoId.HasValue || p.Id != exceptoId.Value));

}