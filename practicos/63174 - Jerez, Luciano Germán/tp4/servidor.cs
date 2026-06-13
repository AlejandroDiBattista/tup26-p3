#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

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

// ── Endpoints de Productos ────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoRepositorio repo) => Results.Ok(repo.ListarProductos()));

app.MapGet("/productos/{id:int}", (int id, CatalogoRepositorio repo) => {
    var p = repo.ObtenerProductoPorId(id);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repo) => {
    var nuevo = repo.CrearProducto(producto);
    return Results.Created($"/productos/{nuevo.Id}", nuevo);
});

app.MapPut("/productos/{id:int}", (int id, Producto producto, CatalogoRepositorio repo) => {
    var modificado = repo.ModificarProducto(id, producto);
    return modificado is null ? Results.NotFound() : Results.Ok(modificado);
});

app.MapDelete("/productos/{id:int}", (int id, CatalogoRepositorio repo) => {
    return repo.EliminarProducto(id) ? Results.NoContent() : Results.NotFound();
});

// ── Endpoints de Movimientos ──────────────────────────────────────────────

app.MapGet("/productos/{productoId:int}/movimientos", (int productoId, CatalogoRepositorio repo) => {
    return Results.Ok(repo.ListarMovimientos(productoId));
});

app.MapPost("/productos/{productoId:int}/movimientos", (int productoId, [FromBody] MovimientoInput input, CatalogoRepositorio repo) => {
    try {
        var mov = repo.RegistrarMovimiento(productoId, input.Tipo, input.Cantidad);
        return Results.Created($"/productos/{productoId}/movimientos", mov);
    } catch (KeyNotFoundException ex) {
        return Results.NotFound(ex.Message);
    } catch (ArgumentException ex) {
        return Results.BadRequest(ex.Message);
    }
});

app.Run("http://localhost:5050");

// ── Modelo ────────────────────────────────────────────────────────────────

public class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

public class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Compra", "Venta", "Ajuste"
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

public record MovimientoInput(string Tipo, int Cantidad);

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;
    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();
        if (!db.Productos.Any()) {
            db.Productos.Add(new Producto { Id = 1, Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 });
            db.Movimientos.Add(new MovimientoDeProducto { Id = 1, ProductoId = 1, Tipo = "Ajuste", Cantidad = 100, Fecha = DateTime.Now });
            db.SaveChanges();
        }
    }

    public List<Producto> ListarProductos() => db.Productos.OrderBy(p => p.Id).ToList();
    
    public Producto? ObtenerProductoPorId(int id) => db.Productos.Find(id);

    public Producto CrearProducto(Producto p) {
        p.Id = 0; // Dejar que SQLite maneje el autoincrement
        db.Productos.Add(p);
        db.SaveChanges();
        return p;
    }

    public Producto? ModificarProducto(int id, Producto p) {
        var existente = db.Productos.Find(id);
        if (existente is null) return null;
        
        existente.Codigo = p.Codigo;
        existente.Nombre = p.Nombre;
        existente.Precio = p.Precio;
        // El stock no se modifica directamente desde acá según la consigna (se hace por movimientos)
        
        db.SaveChanges();
        return existente;
    }

    public bool EliminarProducto(int id) {
        var existente = db.Productos.Find(id);
        if (existente is null) return false;
        
        // Eliminar movimientos asociados primero para evitar conflictos
        var movs = db.Movimientos.Where(m => m.ProductoId == id);
        db.Movimientos.RemoveRange(movs);

        db.Productos.Remove(existente);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientos(int productoId) =>
        db.Movimientos.Where(m => m.ProductoId == productoId).OrderByDescending(m => m.Fecha).ToList();

    public MovimientoDeProducto RegistrarMovimiento(int productoId, string tipo, int cantidad) {
        var prod = db.Productos.Find(productoId) ?? throw new KeyNotFoundException("Producto no encontrado");
        if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero");

        if (tipo == "Compra") {
            prod.Stock += cantidad;
        } else if (tipo == "Venta") {
            if (prod.Stock < cantidad) throw new ArgumentException("Stock insuficiente para realizar la venta");
            prod.Stock -= cantidad;
        } else if (tipo == "Ajuste") {
            prod.Stock = cantidad; // Ajuste establece el stock al valor indicado
        } else {
            throw new ArgumentException("Tipo de movimiento inválido (Compra, Venta, Ajuste)");
        }

        var mov = new MovimientoDeProducto {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = cantidad,
            Fecha = DateTime.Now
        };

        db.Movimientos.Add(mov);
        db.SaveChanges();
        return mov;
    }
}