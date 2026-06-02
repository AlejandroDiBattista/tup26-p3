#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

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

// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoRepositorio r) => Results.Ok(r.GetAll()));

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio r) => {
    var p = r.GetById(id);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.MapPost("/productos", (Producto producto, CatalogoRepositorio r) => {
    if (r.ExisteCodigo(producto.Codigo))
        return Results.BadRequest("Ya existe un producto con ese código");
    r.insert(producto);
    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id}", (int id, Producto input, CatalogoRepositorio r) => {
    var p = r.GetById(id);
    if (p is null)
        return Results.NotFound();
    if (r.GetAll().Any(prod => prod.Id != id && prod.Codigo == input.Codigo)) {
        return Results.BadRequest("Ya existe otro producto con ese código");
    }
    r.Update(id, input);
    return Results.Ok(input);
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio r)=> {
    var p = r.GetById(id);
    if (p is null)
        return Results.NotFound();
    r.Delete(id);
    return Results.NoContent();
});

app.MapGet("/productos/{productoId}/movimientos", (int productoId, CatalogoRepositorio r) => {
    var p = r.GetById(productoId);
    if (p is null)
        return Results.NotFound();
    return Results.Ok(r.GetMovimientos(productoId));
});

app.MapPost("/productos/{productoId}/movimientos", (int productoId, MovimientoInput input, CatalogoRepositorio r)=> {
    var p = r.GetById(productoId);
    if (p is null) return Results.NotFound("Producto no encontrado");
    var error = RegistrarMovimiento(productoId, mov);
    if (error is not null) return Results.BadRequest(error);
    return Results.Created($"/productos/{productoId}/movimientos/{mov.Id}", mov);
});

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

public class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

public class Movimiento {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = "Compra";
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

// ── DbContext ─────────────────────────────────────────────────────────────

public class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Movimiento> Movimientos => Set<MovimientoDeProducto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

public class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();
        if (!db.Productos.Any()) {
            db.Productos.AddRange(
            new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
            new Producto { Codigo = "P002", Nombre = "Café Molido 250g", Precio = 2200m, Stock = 50 }
            );
            db.SaveChanges();
        }
    }

    public List<Producto> GetAll() => db.Productos.ToList();
    public Producto? GetById(int id) => db.Productos.Find(id);
    public bool ExisteCodigo(string codigo) => db.Productos.Any(p => p.Codigo == codigo);

    public void Insert(Producto p) {
        db.Productos.Add(p);
        db.SaveChanges();
    }

    public void Update(int id, Producto input) {
        var p = db.Productos.Find(id);
        p.Codigo = input.Codigo;
        p.Nombre = input.Nombre;
        p.Precio = input.Precio;
        p.Stock = input.Stock;
        db.SaveChanges();
    }

    public void Delete(int id) {
        var p = db.Productos.Find(id);
        db.Productos.Remove(p);
        db.SaveChanges();
    }
}