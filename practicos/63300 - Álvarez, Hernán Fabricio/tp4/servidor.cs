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

    public Producto? TraerProducto() =>
        db.Productos.OrderBy(p => p.Id).FirstOrDefault();
}