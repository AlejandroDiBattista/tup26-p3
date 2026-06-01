#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

// Configuración

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opt => {
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); // para permitir mandar "compra", "venta", "ajuste" como texto
});

builder.Services.AddDbContext<CatalogoDb>(opt => // para registrar la bd
    opt.UseSqlite("Data Source=Catalogo.db"));

builder.Services.AddScoped<CatalogoRepositorio>(); // AddScoped registra el repositorio

var app = builder.Build(); // crea el servidor

// Inicialización de la base de datos

using (var scope = app.Services.CreateScope()) {
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();  // funcion que hace que si la bd no existe, la crea y si no hay productos, carga productos iniciales
}

// Endpoints 

app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    return Results.Ok(repositorio.ListarProductos());
});

app.MapGet("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.ObtenerProducto(id);

    if (producto is null) {
        return Results.NotFound("Producto no encontrado");
    }

    return Results.Ok(producto);
});

app.MapPost("/productos", (ProductoDto dto, CatalogoRepositorio repositorio) => {
    var producto = repositorio.CrearProducto(dto);

    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id:int}", (int id, ProductoDto dto, CatalogoRepositorio repositorio) => {
    var producto = repositorio.ActualizarProducto(id, dto);

    if (producto is null) {
        return Results.NotFound("Producto no encontrado");
    }

    return Results.Ok(producto);
});

app.MapDelete("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) => {
    bool eliminado = repositorio.EliminarProducto(id);

    if (!eliminado) {
        return Results.NotFound("Producto no encontrado");
    }

    return Results.NoContent();
});

app.MapGet("/productos/{productoId:int}/movimientos", (int productoId, CatalogoRepositorio repositorio) => {
    return Results.Ok(repositorio.ListarMovimientosDeProducto(productoId));
});

app.MapPost("/productos/{productoId:int}/movimientos", (int productoId, MovimientoDeProductoDto dto, CatalogoRepositorio repositorio) => {
    var movimiento = repositorio.RegistrarMovimientoDeProducto(productoId, dto);

    if (movimiento is null) {
        return Results.NotFound("Producto no encontrado");
    }

    return Results.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento);
});

app.Run("http://localhost:5050");

// DTO

record ProductoDto(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDeProductoDto(TipoMovimiento Tipo, int Cantidad);

// Modelo

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public DateTime Fecha { get; set; }
    public int Cantidad { get; set; }
    public TipoMovimiento Tipo { get; set; }
}

// DbContext 

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>();
}

// Repositorio 

class CatalogoRepositorio {
    private readonly CatalogoDb db;

    public CatalogoRepositorio(CatalogoDb db) => this.db = db;

    public void Iniciar() {
        db.Database.EnsureCreated();

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS Movimientos (
            Id INTEGER NOT NULL CONSTRAINT PK_Movimientos PRIMARY KEY AUTOINCREMENT,
            ProductoId INTEGER NOT NULL,
            Fecha TEXT NOT NULL,
            Cantidad INTEGER NOT NULL,
            Tipo INTEGER NOT NULL,
            FOREIGN KEY (ProductoId) REFERENCES Productos (Id)
        );
        """);

        if (!db.Productos.Any()) {
            db.Productos.AddRange(
                new Producto {
                    Codigo = "P001",
                    Nombre = "Yerba Mate 500g",
                    Precio = 1500m,
                    Stock = 100
                },
                new Producto {
                    Codigo = "P002",
                    Nombre = "Cafe",
                    Precio = 2500m,
                    Stock = 20
                }
            );

            db.SaveChanges();
        }
    }

    public List<Producto> ListarProductos() {
        return db.Productos
            .OrderBy(producto => producto.Nombre)
            .ToList();
    }

    public Producto? ObtenerProducto(int id) {
        return db.Productos.Find(id);
    }

    public Producto CrearProducto(ProductoDto dto) {
        var producto = new Producto {
            Codigo = dto.Codigo.Trim(),
            Nombre = dto.Nombre.Trim(),
            Precio = dto.Precio,
            Stock = dto.Stock
        };

        db.Productos.Add(producto);
        db.SaveChanges();

        return producto;
    }

    public Producto? ActualizarProducto(int id, ProductoDto dto) { // busca el producto, valida q sea positivo, si es compra suma stock, si es venta resta stock, si es ajuste, reemplaza el stock, guarda el movimiento y guarda los cambios. Es una de las funciones mas importantes
        var producto = db.Productos.Find(id);

        if (producto is null) {
            return null;
        }

        producto.Codigo = dto.Codigo.Trim();
        producto.Nombre = dto.Nombre.Trim();
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;

        db.SaveChanges();

        return producto;
    }

    public bool EliminarProducto(int id) {
        var producto = db.Productos.Find(id);

        if (producto is null) {
            return false;
        }

        db.Productos.Remove(producto);
        db.SaveChanges();

        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientosDeProducto(int productoId) {
        return db.Movimientos
            .Where(movimiento => movimiento.ProductoId == productoId)
            .OrderByDescending(movimiento => movimiento.Fecha)
            .ToList();
    }

    public MovimientoDeProducto? RegistrarMovimientoDeProducto(int productoId, MovimientoDeProductoDto dto) {
        var producto = db.Productos.Find(productoId);

        if (producto is null) {
            return null;
        }

        if (dto.Cantidad <= 0) {
            throw new ArgumentException("La cantidad debe ser positiva.");
        }

        if (dto.Tipo == TipoMovimiento.Compra) {
            producto.Stock += dto.Cantidad;
        } else if (dto.Tipo == TipoMovimiento.Venta) {
            if (producto.Stock < dto.Cantidad) {
                throw new ArgumentException("No hay stock suficiente.");
            }

            producto.Stock -= dto.Cantidad;
        } else if (dto.Tipo == TipoMovimiento.Ajuste) {
            producto.Stock = dto.Cantidad;
        }

        var movimiento = new MovimientoDeProducto {
            ProductoId = productoId,
            Tipo = dto.Tipo,
            Cantidad = dto.Cantidad,
            Fecha = DateTime.Now
        };

        db.Movimientos.Add(movimiento);
        db.SaveChanges();

        return movimiento;
    }
}
