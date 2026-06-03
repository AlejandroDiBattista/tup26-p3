#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args); // importamos el builder para configurar la aplicación
builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db")); // constructor principal de la app
builder.Services.AddScoped<CatalogoRepositorio>();
var app = builder.Build();

// fin de la config y inicio de la construccion para los endpoints
using (var scope = app.Services.CreateScope())
{
    var repositorio = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
    repositorio.Iniciar();
}

 var norep = app.MapGroup("/productos"); // grupo de endpoints para productos



norep.MapGet("/", (CatalogoRepositorio repositorio) =>
    Results.Ok(repositorio.ListarProductos().Select(ProductoDto.From)));
    norep .MapGet("/{id:int}", (int id, CatalogoRepositorio repositorio) =>
{
    var producto = repositorio.TraerProducto(id);

    if (producto is null)
        return Results.NotFound();

    return Results.Ok(ProductoDto.From(producto));
});
norep.MapPost("/", (ProductoEntrada entrada, CatalogoRepositorio repositorio) => {
    var error = ValidarProducto(entrada);
    if (error is not null) return Results.BadRequest(error);

    var resultado = repositorio.CrearProducto(entrada);
    if (resultado.Error is not null) return Results.BadRequest(resultado.Error);
    return Results.Created($"/productos/{resultado.Producto!.Id}", ProductoDto.From(resultado.Producto));
});
norep.MapPut("/{id:int}", (int id, ProductoEntrada entrada, CatalogoRepositorio repositorio) => {
    var error = ValidarProducto(entrada);
    if (error is not null) return Results.BadRequest(error);
    var resultado = repositorio.ModificarProducto(id, entrada);
    if (resultado.NoEncontrado) return Results.NotFound();
    if (resultado.Error is not null) return Results.BadRequest(resultado.Error);
    return Results.Ok(ProductoDto.From(resultado.Producto!));
});
norep.MapDelete("/{id:int}", (int id, CatalogoRepositorio repositorio) => repositorio.EliminarProducto(id) ? Results.NoContent() : Results.NotFound());
norep.MapGet("/{id:int}/movimientos", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.TraerProducto(id);
    if (producto is null) return Results.NotFound();
    return Results.Ok(repositorio.ListarMovimientos(id).Select(MovimientoDto.From));
});
norep.MapPost("/{id:int}/movimientos", (int id, MovimientoEntrada entrada, CatalogoRepositorio repositorio) => {
    var resultado = repositorio.RegistrarMovimiento(id, entrada);
    if (resultado.NoEncontrado) return Results.NotFound();
    if (resultado.Error is not null) return Results.BadRequest(resultado.Error);
    return Results.Created($"/productos/{id}/movimientos/{resultado.Movimiento!.Id}", MovimientoDto.From(resultado.Movimiento));
});
static string? ValidarProducto(ProductoEntrada entrada) {
    if (string.IsNullOrWhiteSpace(entrada.Codigo))
     return "El codigo es obligatorio.";
    if (string.IsNullOrWhiteSpace(entrada.Nombre)) 
    return "El nombre es obligatorio.";
    if (entrada.Stock < 0) 
    return "El stock no puede ser negativo.";
    if (entrada.Precio < 0) 
    return "El precio no puede ser negativo";


    return null;

    
}


app.Run("http://localhost:5050"); // iniciamos el servidor en el puerto 5050

enum TipoMovimiento{Compra,Venta,Ajuste} //Tipo de movimientos que se podran realizar

// definicion de la clase producto, con sus propiedades Id, Codigo, Nombre, Stock y Precio
// usamos "" para evitar warnings de null
class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public int Stock { get; set; }
    public decimal Precio { get; set; }
    public List<MovimientoDeProducto> Movimientos { get; set; } = [];
    // lista de movimientos de productos, se usa =[] para inicar la lista vacia y evitar nullreference exceptions
}

class MovimientoDeProducto {
    public int Id{get;set;}
    public int ProductoId{get;set;}
    // clase para los movimientos de productos, con sus propiedades Id y ProductoId, que se relaciona con la clase Producto a traves de ProductoId
    public Producto? Producto { get; set; } // propiedad de tipo Producto que se relaciona con la clase MovimientoDeProducto a traves de ProductoId
    // propiedades para el historial del stock 
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

record ProductoEntrada(string Codigo, string Nombre, int Stock, decimal Precio); // record para la entrada de producto
record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad); // record para la entrada de movimiento de producto
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock) {
    public static ProductoDto From(Producto producto) =>
        new(producto.Id, producto.Codigo, producto.Nombre, producto.Precio, producto.Stock);
}
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha) {
    public static MovimientoDto From(MovimientoDeProducto movimiento) =>
        new(movimiento.Id, movimiento.ProductoId, movimiento.Tipo, movimiento.Cantidad, movimiento.Fecha);
}
record OperacionProducto(Producto? Producto = null, string? Error = null, bool NoEncontrado = false);
record OperacionMovimiento(MovimientoDeProducto? Movimiento = null, string? Error = null, bool NoEncontrado = false);

class CatalogoDb : DbContext {
    public CatalogoDb(DbContextOptions<CatalogoDb> options) : base(options){
    }
    // propiedades para la tablas de movimientos y productos, podemos agregar,editas o borrar
    public DbSet<Producto> Productos => Set<Producto>(); // DbSet para la tabla de productos
    public DbSet<MovimientoDeProducto> Movimientos => Set<MovimientoDeProducto>(); // Db

    // metodo para construir la bd
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Producto>() 
        .HasIndex(p => p.Codigo) // creamos un indice para el codigo del producto, para mejorar la busqueda por codigo
        .IsUnique(); // indice unico para el codigo del producto
        modelBuilder.Entity<Producto>()
            .Property(p => p.Codigo).IsRequired();
        modelBuilder.Entity<Producto>()
            .Property(p => p.Nombre).IsRequired();
        
        modelBuilder.Entity<MovimientoDeProducto>()
            .HasOne(m => m.Producto) // un movimiento tiene un prod
            .WithMany(p => p.Movimientos) // producto tiene muchos movimientos
            .HasForeignKey(m => m.ProductoId) // FK ProductoId
            .OnDelete(DeleteBehavior.Cascade);/// si se borra un prod, se borra el historial
    }
}

// clase intermediaria para la app y bd
class CatalogoRepositorio {
    private readonly CatalogoDb db; //guarda contexto para usarlo dps
    public CatalogoRepositorio(CatalogoDb db) {
        this.db = db;
    }

    public void Iniciar() {
        db.Database.EnsureCreated(); //verifica que exista la bd, si no la crea.
        if (!db.Productos.Any()) {
            db.Productos.AddRange(
                new Producto { Codigo = "P001", Nombre = "yerba mate 500g", Stock = 10, Precio = 1040 },
                new Producto { Codigo = "P002", Nombre = "cafe 400g", Stock = 20, Precio = 2020 },
                new Producto { Codigo = "P003", Nombre = "leche 1l", Stock = 30, Precio = 3000 }
            ); // estos productos se agregan a la tabla de productos, si la tabla esta vacia, para tener datos de prueba al iniciar la app
            db.SaveChanges();
        //este if lo usamos pa verificar que la tabla este vacia
        }
    }

    //lista los productos ordenados por codigo, y trae un producto por id, devuelve null si no lo encuentra
    public List<Producto> ListarProductos() =>
        db.Productos.OrderBy(p => p.Codigo).ToList();
    //trae prod si no devuelve null
    public Producto? TraerProducto(int id) =>
        db.Productos.FirstOrDefault(p => p.Id == id);

    public OperacionProducto CrearProducto(ProductoEntrada entrada) {
        var codigo = entrada.Codigo.Trim();
        if (db.Productos.Any(p => p.Codigo == codigo)) {
            return new OperacionProducto(Error: "Ya   hay un producto con el codigo ");
        }
        var producto = new Producto{
            Codigo =codigo,
            Nombre = entrada.Nombre.Trim(),
            Stock = entrada.Stock,
            Precio = entrada.Precio};

        db.Productos.Add(producto);
        db.SaveChanges();
        return new OperacionProducto(producto);
        // se crea la entidad, se agrega al contexto y devuelve con el id
    }

    public OperacionProducto ModificarProducto(int id, ProductoEntrada entrada) {
        var producto = db.Productos.FirstOrDefault(p => p.Id == id);
        if (producto == null) {
            return new OperacionProducto(NoEncontrado: true);
        }
        var codigo = entrada.Codigo.Trim();
        if (db.Productos.Any(p => p.Id != id && p.Codigo == codigo)) {
            return new OperacionProducto(Error: "Ya hay un producto con el codigo ");
        }
        producto.Codigo = codigo; 
        producto.Nombre = entrada.Nombre.Trim();
        producto.Stock = entrada.Stock;
        producto.Precio = entrada.Precio;
        db.SaveChanges();
        return new OperacionProducto(producto);
    }

    public bool EliminarProducto(int id) {
        var producto = db.Productos.FirstOrDefault(p => p.Id == id);
        if (producto is null) 
        return false;
        db.Productos.Remove(producto);
        db.SaveChanges();
        return true;
    }

    public List<MovimientoDeProducto> ListarMovimientos(int productoId) => db.Movimientos
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToList();
        //lista recorrible para filtrar movimientos x productos y ordenarlos por fecha desc

    public OperacionMovimiento RegistrarMovimiento(int productoId, MovimientoEntrada entrada) {
        System.Console.WriteLine( $"Producto={productoId} Tipo={entrada.Tipo} Cantidad={entrada.Cantidad}");
        using var transaccion = db.Database.BeginTransaction();

        var producto = db.Productos.FirstOrDefault(p => p.Id == productoId);
        if (producto == null) return new OperacionMovimiento(NoEncontrado: true);

        if (entrada.Tipo == TipoMovimiento.Compra) {
            producto.Stock += entrada.Cantidad;
        }
        else if (entrada.Tipo == TipoMovimiento.Venta) {
            if (producto.Stock < entrada.Cantidad)
                return new OperacionMovimiento(Error: "No hay suficiente stock para realizar la venta");
            producto.Stock -= entrada.Cantidad;
        }
        else if (entrada.Tipo == TipoMovimiento.Ajuste) {
            producto.Stock = entrada.Cantidad;
        }

        var movimiento = new MovimientoDeProducto {
            ProductoId = productoId,
            Tipo = entrada.Tipo,
            Cantidad = entrada.Cantidad,
            Fecha = DateTime.Now,
        };
        db.Movimientos.Add(movimiento);
        db.SaveChanges();
        transaccion.Commit();
        return new OperacionMovimiento(movimiento);
    }    
}
