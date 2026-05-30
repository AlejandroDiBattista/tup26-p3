#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

// ── Configuración ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<CatalogoRepositorio>(); // PREGUNTAR AL CHAT

var app = builder.Build(); // PREGUNTAR AL CHAT


// ── Inicialización de la base de datos ────────────────────────────────────

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
    }


// ── Endpoints ─────────────────────────────────────────────────────────────

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    var productos = repositorio.TraerProductos();
    if(productos is null) return Results.NotFound();

    return Results.Ok(productos);
});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProducto(id);
    if(producto is null) return Results.NotFound();

    return Results.Ok(producto);
});

app.MapGet("/productos/{id}/movimientos", (int id, CatalogoRepositorio repositorio) => {
    var movimientos = repositorio.TraerMovimientosDeProducto(id);
    if(movimientos is null) return Results.NotFound();

    return Results.Ok(movimientos);
});
app.MapPost("/productos/", (ProductoCrearDto dto, CatalogoRepositorio repositorio) => {
    if (dto is null) return Results.BadRequest();
    if (string.IsNullOrWhiteSpace(dto.Codigo, dto.Nombre)) return Results.BadRequest("El codigo y el nombre es obligatorio.");
    if (dto.Precio <= 0 || dto.Stock < 0) return Results.BadRequest("El precio y el Stock deben ser mayores a 0.");
    if (repositorio.CodigoExistente(dto.Codigo)) return Results.Conflict("Y existe un producto con ese código.");

    var nuevoProducto = repositorio.CrearProducto(dto.Codigo, dto.Nombre, dto.Precio, dto.Stock);
    return Results.Created($"/productos/{nuevoProducto.Id}", nuevoProducto);
});

app.MapPost("/productos/{id}/movimientos", (int id, MovimientoCrearDto dto, CatalogoRepositorio repositorio) => {
    if (string.IsNullOrWhiteSpace(dto.Accion)) return Results.BadRequest("La acción es obligatoria.");
    if (dto.Cantidad <= 0) return Results.BadRequest("La cantidad debe ser mayor a 0.");

    var movimiento = repositorio.registrarMovimiento(id, dto.Accion, dto.Cantidad);
    if (movimiento is null) return Results.BadRequest("No se pudo registrar el movimiento. Verifique el producto y la acción.");

    return Results.Created($"/productos/{id}/movimientos/{movimiento.Id}", movimiento);
});

app.MapPut("/productos/{id}", (int id, ProductoActualizarDto dto, CatalogoRepositorio repositorio) => {
    if (string.IsNullOrWhiteSpace(dto.Codigo, dto.Nombre)) return Results.BadRequest("El codigo y el nombre es obligatorio.");
    if (dto.Precio <= 0 || dto.Stock < 0) return Results.BadRequest("El precio y el Stock deben ser mayores a 0.");
    if (repositorio.CodigoExistente(dto.Codigo, id)) return Results.Conflict("Y existe un producto con ese código.");

    var productoActualizado = repositorio.ActualizarProducto(id, dto.Codigo, dto.Nombre, dto.Precio, dto.Stock);
    if (productoActualizado is null) return Results.NotFound();

    return Results.Ok(productoActualizado);
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repositorio) => {
    var eliminado = repositorio.EliminarProducto(id);
    if (!eliminado) return Results.NotFound();

    return Results.NoContent();
});

app.Run("http://localhost:5050");



// ── Modelo ────────────────────────────────────────────────────────────────


record class ProductoCrearDto(string Codigo, string Nombre, decimal Precio, int Stock);
record class ProductoActualizarDto(string Codigo, string Nombre, decimal Precio, int Stock);
record class MovimientoCrearDto(string Tipo, int Cantidad);



record class Producto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record class MovimientoDeProducto(int Id, int ProductoId, string Accion, int Cantidad, DateTime Fecha);


// ── DbContext ─────────────────────────────────────────────────────────────

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> MovimientosDeProductos => Set<MovimientoDeProducto>();
}

// ── Repositorio ───────────────────────────────────────────────────────────

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        if (!db.Productos.Any()) {
            db.Productos.Add(new Producto(1, "P001", "Yerba Mate", 1500m, 100));
            db.Productos.Add(new Producto(2, "P002", "Azúcar 1kg", 1200m, 50));
            db.SaveChanges();
        }
    }

    public List<Producto> TraerProductos() =>
        db.Productos.OrderBy(p => p.Id).ToList();

    public Producto? TraerProducto(int id) =>
        db.Productos.FirstOrDefault(p => p.Id == id);

    public List<MovimientoDeProducto> TraerMovimientosDeProducto(int productoId) =>
        db.MovimientosDeProductos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToList(); 

}