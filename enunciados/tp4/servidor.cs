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



app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.Add(new Producto(1, "P001", "Yerba Mate 500g", 1500m, 100));
            db.SaveChanges();
        }
    }

    public Producto? GetById(int id) => db.Productos.Find(id);
}