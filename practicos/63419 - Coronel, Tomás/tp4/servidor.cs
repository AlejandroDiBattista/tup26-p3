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
}



class CatalogoRepositorio
{
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db)
    {
        this.db = db;
    }

    public void Iniciar()
    {
        db.Database.EnsureCreated();

        if (!db.Productos.Any())
        {
            db.Productos.Add(new Producto
            {
                Codigo = "#0042",
                Nombre = "Cookie Oreo",
                Precio = 4000,
                Stock = 8
            });

            db.Productos.Add(new Producto
            {
                Codigo = "#0043",
                Nombre = "Cookie Pistacho",
                Precio = 4500,
                Stock = 6
            });

            db.Productos.Add(new Producto
            {
                Codigo = "#0044",
                Nombre = "Cookie Chocolate",
                Precio = 4200,
                Stock = 10
            });

            db.Productos.Add(new Producto
            {
                Codigo = "#0045",
                Nombre = "Cookie Nutella",
                Precio = 4800,
                Stock = 5
            });

            db.Productos.Add(new Producto
            {
                Codigo = "#0046",
                Nombre = "Cookie Kinder",
                Precio = 5000,
                Stock = 7
            });

            db.Productos.Add(new Producto
            {
                Codigo = "#0047",
                Nombre = "Cookie Red Velvet",
                Precio = 4600,
                Stock = 4
            });

            db.Productos.Add(new Producto
            {
                Codigo = "#0048",
                Nombre = "Cookie Frutos Rojos",
                Precio = 4700,
                Stock = 5
            });

            db.Productos.Add(new Producto
            {
                Codigo = "#0049",
                Nombre = "Cookie Dulce de Leche",
                Precio = 4300,
                Stock = 9
            });

            db.SaveChanges();
        }
    }
    public List<Producto> TraerProductos()
        => db.Productos.OrderBy(x => x.Nombre).ToList();

    public Producto? TraerProducto(int id)
        => db.Productos.FirstOrDefault(x => x.Id == id);

    public Producto AgregarProducto(ProductoDto dto)
    {
        var producto = new Producto
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Precio = dto.Precio,
            Stock = dto.Stock
        };

        db.Productos.Add(producto);

        db.SaveChanges();

        return producto;
    }

    public bool ModificarProducto(int id, ProductoDto dto)
    {
        var producto = TraerProducto(id);

        if (producto is null)
            return false;

        producto.Codigo = dto.Codigo;
        producto.Nombre = dto.Nombre;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;

        db.SaveChanges();

        return true;
    }

    public bool EliminarProducto(int id)
    {
        var producto = TraerProducto(id);

        if (producto is null)
            return false;

        var movimientos = db.Movimientos
            .Where(x => x.ProductoId == id)
            .ToList();

        db.Movimientos.RemoveRange(movimientos);

        db.Productos.Remove(producto);

        db.SaveChanges();

        return true;
    }

    public List<MovimientoDeProducto> TraerMovimientos(int productoId)
        => db.Movimientos
            .Where(x => x.ProductoId == productoId)
            .OrderByDescending(x => x.Fecha)
            .ToList();

    public bool RegistrarMovimiento(int productoId, MovimientoDto dto)
    {
        var producto = TraerProducto(productoId);

        if (producto is null)
            return false;

        var movimiento = new MovimientoDeProducto
        {
            ProductoId = productoId,
            Tipo = dto.Tipo,
            Cantidad = dto.Cantidad,
            Fecha = DateTime.Now
        };

        if (dto.Tipo == "Compra")
        {
            producto.Stock += dto.Cantidad;
        }
        else if (dto.Tipo == "Venta")
        {
            producto.Stock -= dto.Cantidad;
        }
        else if (dto.Tipo == "Ajuste")
        {
            producto.Stock = dto.Cantidad;
        }

        db.Movimientos.Add(movimiento);

        db.SaveChanges();

        return true;
    }
}