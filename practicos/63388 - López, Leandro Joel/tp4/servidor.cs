#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

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

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────

record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDbContext : DbContext {
    public CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
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