#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt =>
    opt.UseSqlite("Data Source=catalogo.db"));

builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
var repo = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
repo.Iniciar();
}



app.MapGet("/productos", (CatalogoRepositorio repo) =>
{
return Results.Ok(repo.TraerProductos());
});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repo) =>
{
var producto = repo.TraerProducto(id);

return producto is null
    ? Results.NotFound()
    : Results.Ok(producto);
});

app.MapPost("/productos", (ProductoDto dto, CatalogoRepositorio repo) =>
{
var producto = repo.AgregarProducto(dto);

return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id}", (int id, ProductoDto dto, CatalogoRepositorio repo) =>
{
return repo.ModificarProducto(id, dto)
    ? Results.Ok()
    : Results.NotFound();
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repo) =>
{
return repo.EliminarProducto(id)
    ? Results.Ok()
    : Results.NotFound();
});

app.MapGet("/productos/{productoId}/movimientos",
(int productoId, CatalogoRepositorio repo) =>
{
return Results.Ok(repo.TraerMovimientos(productoId));
});

app.MapPost("/productos/{productoId}/movimientos",
(int productoId, MovimientoDto dto, CatalogoRepositorio repo) =>
{
return repo.RegistrarMovimiento(productoId, dto)
    ? Results.Ok()
    : Results.NotFound();
});

app.Run("http://localhost:5050");



class Producto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }
}

class MovimientoDeProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public string Tipo { get; set; } = "";

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    public Producto? Producto { get; set; }
}

record ProductoDto(
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);

record MovimientoDto(
    string Tipo,
    int Cantidad
);

class CatalogoDb : DbContext
{
    public CatalogoDb(DbContextOptions<CatalogoDb> options)
        : base(options)
    {
    }

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();