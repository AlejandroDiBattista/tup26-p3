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

app.MapGet("/productos/{id:int}", (int id, CatalogoRepositorio repo) => {
    var p = repo.ObtenerProducto(id);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.MapPost("/productos", (ProductoDto dto, CatalogoRepositorio repo) => {
    var p = repo.CrearProducto(dto);
    return Results.Created($"/productos/{p.Id}", p);
});

app.MapPut("/productos/{id:int}", (int id, ProductoDto dto, CatalogoRepositorio repo) => {
    var p = repo.ModificarProducto(id, dto);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.MapDelete("/productos/{id:int}", (int id, CatalogoRepositorio repo) => {
    var ok = repo.EliminarProducto(id);
    return ok ? Results.NoContent() : Results.NotFound();
});

app.Run("http://localhost:5050");

record ProductoDto(string Codigo, string Nombre, decimal Precio, int Stock);

enum TipoMovimiento { Compra, Venta, Ajuste }

class Producto {
    public int     Id     { get; set; }
    public string  Codigo { get; set; } = "";
    public string  Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int     Stock  { get; set; }
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
}

class MovimientoDeProducto {
    public int            Id         { get; set; }
    public int            ProductoId { get; set; }
    public TipoMovimiento Tipo       { get; set; }
    public int            Cantidad   { get; set; }
    public DateTime       Fecha      { get; set; }
}

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto>             Productos   => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

class CatalogoRepositorio {
    private readonly CatalogoDb db;
    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();
        if (!db.Productos.Any()) {
            db.Productos.AddRange(
                new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g",   Precio = 1500m, Stock = 100 },
                new Producto { Codigo = "P002", Nombre = "Café Molido 250g",  Precio = 2200m, Stock = 50  },
                new Producto { Codigo = "P003", Nombre = "Azúcar Blanca 1kg", Precio =  800m, Stock = 200 }
            );
            db.SaveChanges();
        }
    }

    public List<Producto> ListarProductos() =>
        db.Productos.OrderBy(p => p.Id).ToList();

    public Producto? ObtenerProducto(int id) =>
        db.Productos.Find(id);

    public Producto CrearProducto(ProductoDto dto) {
        var p = new Producto {
            Codigo = dto.Codigo, Nombre = dto.Nombre,
            Precio = dto.Precio, Stock  = dto.Stock
        };
        db.Productos.Add(p);
        db.SaveChanges();
        return p;
    }

    public Producto? ModificarProducto(int id, ProductoDto dto) {
        var p = db.Productos.Find(id);
        if (p is null) return null;
        p.Codigo = dto.Codigo; p.Nombre = dto.Nombre;
        p.Precio = dto.Precio; p.Stock  = dto.Stock;
        db.SaveChanges();
        return p;
    }

    public bool EliminarProducto(int id) {
        var p = db.Productos.Find(id);
        if (p is null) return false;
        db.Productos.Remove(p);
        db.SaveChanges();
        return true;
    }
}