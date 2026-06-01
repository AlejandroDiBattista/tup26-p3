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


