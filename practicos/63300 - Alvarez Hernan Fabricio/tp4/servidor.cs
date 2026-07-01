#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>();

/* serializa el enum TipoMovimiento como texto */
builder.Services.ConfigureHttpJsonOptions(opciones => {
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/producto", (CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProducto();
    if(producto is null) return Results.NotFound();

    return Results.Ok(producto);
});

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    var productos = repositorio.ListarProductos();
    return Results.Ok(productos);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio repositorio) => {
    var nuevo = repositorio.CrearProducto(producto);
    return Results.Created($"/productos/{nuevo.Id} ", nuevo);
});

app.MapPut("/productos/{id}", (int id,Producto producto, CatalogoRepositorio repositorio) => {
    var actualizado = repositorio.ActualizarProducto(id, producto);
    return actualizado ? Results.NoContent() : Results.NotFound();
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var eliminado = repositorio.EliminarProducto(id);
    return eliminado ? Results.NoContent() : Results.NotFound();

});

/* EndPoints para Movimientos */

app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoRepositorio repositorio) => {
    return Results.Ok(repositorio.ListarMovimientos(productoId));

});

app.MapPost("productos/{productoId}/movimientos", (int productoId, MovimientoDeProducto movimiento, CatalogoRepositorio repositorio) => {
      try {
        var nuevoMovimiento = repositorio.RegistrarMovimiento(productoId, movimiento);
        return Results.Created($"/productos/{productoId}/movimientos/{nuevoMovimiento.Id}", nuevoMovimiento);

    }  catch (Exception ex) {
        return Results.BadRequest(ex.Message);
    }
}  );

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────
public enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

public class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
};

public class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int Codigo { get; set;}
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }

}

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
            db.Productos.Add(new Producto{ Id = 1, Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 });
            db.SaveChanges();
        }
    }

    public List<Producto> ListarProductos() => db.Productos.ToList();

    public Producto CrearProducto(Producto p) {
        db.Productos.Add(p);
        db.SaveChanges();
        return p;
    }

    public bool ActualizarProducto(int id, Producto pActualizado) {
        var p = db.Productos.Find(id);
        if (p is null) return false;
        p.Codigo = pActualizado.Codigo;
        p.Nombre = pActualizado.Nombre;
        p.Precio = pActualizado.Precio;
        db.SaveChanges();
        return true;
    }

    public bool EliminarProducto(int id) {
        var p = db.Productos.Find(id);
        if (p is null) return false;
        db.Productos.Remove(p);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientos(int productoId) => 
        db.Movimientos.Where(m => m.ProductoId == productoId).OrderByDescending(m => m.Fecha).ToList(); 

    public MovimientoDeProducto RegistrarMovimiento(int productoId, MovimientoDeProducto mov) {
        var p = db.Productos.Find(productoId);
        if (p is null) throw new Exception("Producto no encontrado");

        mov.ProductoId = productoId;
        mov.Fecha = DateTime.Now;

        if (mov.Tipo == TipoMovimiento.Compra) p.Stock += mov.Cantidad;
        else if (mov.Tipo == TipoMovimiento.Venta) {
            if (p.Stock < mov.Cantidad) throw new Exception("Stock Insuficiente");
            p.Stock -= mov.Cantidad;
        }
        else if (mov.Tipo == TipoMovimiento.Ajuste) p.Stock += mov.Cantidad;

        db.Movimientos.Add(mov);
        db.SaveChanges();
        return mov;
    }

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}