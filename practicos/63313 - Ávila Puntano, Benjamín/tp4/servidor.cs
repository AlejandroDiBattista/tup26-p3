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
app.MapGet("/productos", (CatalogoRepositorio repositorio) =>
    Results.Ok(repositorio.ListarProductos()));
app.MapPost("/productos", (ProductoEntrada entrada, CatalogoRepositorio repositorio) => {
    var error = ValidarProducto(entrada);
    if (error is not null) return Results.BadRequest(error);
    var resultado = repositorio.CrearProducto(entrada);
    return Results.Created($"/productos/{resultado.Producto!.Id}", resultado.Producto);
});
app.MapPut("/productos/{id:int}", (int id, ProductoEntrada entrada, CatalogoRepositorio repositorio) => {
    var error = ValidarProducto(entrada);
    if (error is not null) return Results.BadRequest(error);
    var resultado = repositorio.ModificarProducto(id, entrada);
    if (resultado.NoEncontrado) return Results.NotFound();
    if (resultado.Error is not null) return Results.BadRequest(resultado.Error);
    return Results.Ok(resultado.Producto);
});
app.MapDelete("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) => repositorio.EliminarProducto(id) ? Results.NoContent() : Results.NotFound());
static string? ValidarProducto(ProductoEntrada entrada) {
    if (string.IsNullOrWhiteSpace(entrada.Codigo)) return "El codigo es obligatorio.";
    return null;
}

app.Run("http://localhost:5000"); // iniciamos el servidor en el puerto 5000

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
            db.Productos.AddRange();
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
        var producto = new Producto();
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