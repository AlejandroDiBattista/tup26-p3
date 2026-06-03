#!/usr/bin/env -S dotnet run

#:package Microsoft.EntityFrameworkCore.Sqlite@10.0.0
#:property PublishAot=false

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using static System.Console;

var dbPath = "30.1-carrito.sqlite";

Clear();
WriteLine("=== Carrito de compras con EF Core + SQLite ===");
WriteLine($"Base SQLite: {dbPath}");
WriteLine();

using var context = new TiendaContext(dbPath);

context.Database.EnsureDeleted();
context.Database.EnsureCreated();

var tienda = new TiendaService(context);

CargarCatalogo(tienda);
MostrarCatalogo(tienda.ListarCatalogo());

var carrito = tienda.CrearCarrito("Juan Pérez");
WriteLine($"Carrito creado: #{carrito.Id} para {carrito.Cliente}");
WriteLine();

tienda.AgregarProducto(carrito.Id,  "NOTE-15",  1);
tienda.AgregarProducto(carrito.Id,  "MOUSE-WL", 2);
tienda.AgregarProducto(carrito.Id,  "USB-C",    3);
tienda.QuitarProducto(carrito.Id,   "USB-C",    1);

WriteLine("Catálogo luego de reservar stock:");
MostrarCatalogo(tienda.ListarCatalogo());

var carritoActual = tienda.ObtenerCarrito(carrito.Id);
MostrarCarrito(carritoActual);

var carritoConfirmado = tienda.ConfirmarCarrito(carrito.Id);
MostrarConfirmacion(carritoConfirmado);

var carritoCancelado = tienda.CrearCarrito("Analía Gómez");
tienda.AgregarProducto(carritoCancelado.Id, "WEBCAM", 1);
tienda.AgregarProducto(carritoCancelado.Id, "TECL-WL", 1);

WriteLine();
WriteLine($"Carrito a cancelar: #{carritoCancelado.Id} para {carritoCancelado.Cliente}");
MostrarCarrito(tienda.ObtenerCarrito(carritoCancelado.Id));

var carritoCanceladoFinal = tienda.CancelarCarrito(carritoCancelado.Id);
WriteLine($"Carrito cancelado: #{carritoCanceladoFinal.Id} [{carritoCanceladoFinal.Estado}]");
WriteLine();

WriteLine("Catálogo final:");
MostrarCatalogo(tienda.ListarCatalogo());

return;


static void CargarCatalogo(TiendaService tienda) {
	tienda.CrearProducto("NOTE-15",  "Notebook 15 pulgadas",  1_250_000m, 5);
	tienda.CrearProducto("MOUSE-WL", "Mouse inalámbrico",        28_500m, 12);
	tienda.CrearProducto("USB-C",    "Hub USB-C",                41_990m, 8);
	tienda.CrearProducto("MON-27",   "Monitor 27 pulgadas",     310_000m, 4);
	tienda.CrearProducto("TECL-WL",  "Teclado inalámbrico",      45_000m, 6);
	tienda.CrearProducto("WEBCAM",   "Webcam Full HD",           85_000m, 3);
	tienda.CrearProducto("AUR-WL",   "Auriculares inalámbricos", 60_000m, 7);
}

static void MostrarCatalogo(IReadOnlyList<Producto> productos) {
	WriteLine("\n= Catálogo de productos ======================================");
	foreach (var producto in productos) {
		WriteLine($"◉ {producto.Codigo,-8} ⏐ {producto.Nombre,-24} ⏐ stock {producto.StockDisponible,2} ⏐ ${producto.Precio,10:0.00}");
	}
	WriteLine();
}

static void MostrarCarrito(Carrito carrito) {
	WriteLine("\n= Detalle del carrito ========================================");
	WriteLine($"Carrito #{carrito.Id} [{carrito.Estado}] - Cliente: {carrito.Cliente}");
	foreach (var item in carrito.Items.OrderBy(item => item.Producto.Nombre)) {
		WriteLine($"◉ {item.Producto.Nombre,-24} x {item.Cantidad,2} = ${item.Subtotal,10:0.00}");
	}
	WriteLine($" Total del carrito: ${carrito.Total:0.00}");
	WriteLine();
}

static void MostrarConfirmacion(Carrito carrito) {
	WriteLine("\n= Confirmación de compra =====================================");
	WriteLine($"Compra confirmada para el carrito #{carrito.Id}");
	WriteLine($"◉ Cliente: {carrito.Cliente}");
	WriteLine($"◉ Fecha: {carrito.ConfirmadoUtc:yyyy-MM-dd HH:mm:ss}");
	WriteLine($"Total confirmado: ${carrito.Total:0.00}");
}

enum EstadoCarrito {
	Abierto    = 1,
	Confirmado = 2,
	Cancelado  = 3
}

class Producto {
	public int Id { get; set; }
	public string Codigo { get; set; } = string.Empty;
	public string Nombre { get; set; } = string.Empty;
	public decimal Precio { get; set; }
	public int StockDisponible { get; set; }
	public bool Activo { get; set; } = true;

	public ICollection<CarritoItem> ItemsCarrito { get; set; } = [];
}

class Carrito {
	public int Id { get; set; }
	public string Cliente { get; set; } = string.Empty;
	public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
	public DateTime? ConfirmadoUtc { get; set; }
	public EstadoCarrito Estado { get; set; } = EstadoCarrito.Abierto;

	public ICollection<CarritoItem> Items { get; set; } = [];

	[NotMapped]
	public decimal Total => Items.Sum(item => item.Subtotal);
}

class CarritoItem {
	public int Id { get; set; }
	public int CarritoId { get; set; }
	public int ProductoId { get; set; }
	public int Cantidad { get; set; }
	public decimal PrecioUnitario { get; set; }

	public Carrito Carrito { get; set; } = null!;
	public Producto Producto { get; set; } = null!;

	[NotMapped]
	public decimal Subtotal => PrecioUnitario * Cantidad;
}

class TiendaService(TiendaContext context) {
	public Producto CrearProducto(string codigo, string nombre, decimal precio, int stockDisponible) {
		Verificar.NoVacio(codigo, "El código del producto es obligatorio.");
		Verificar.NoVacio(nombre, "El nombre del producto es obligatorio.");
		Verificar.Positivo(precio, "El precio debe ser mayor a cero.");
		Verificar.Positivo(stockDisponible, "El stock disponible debe ser mayor o igual a cero.");

		var codigoNormalizado = NormalizarCodigo(codigo);
		var nombreNormalizado = nombre.Trim();

		var existente = context.Productos.SingleOrDefault(producto => producto.Codigo == codigoNormalizado);

		if (existente is not null) {
			return existente;
		}

		var producto = new Producto {
			Codigo = codigoNormalizado,
			Nombre = nombreNormalizado,
			Precio = precio,
			StockDisponible = stockDisponible
		};

		context.Productos.Add(producto);
		context.SaveChanges();

		return producto;
	}

	public IReadOnlyList<Producto> ListarCatalogo() {
		return context.Productos
			.AsNoTracking()
			.OrderBy(producto => producto.Nombre)
			.ToList();
	}

	public Carrito CrearCarrito(string cliente) {
		Verificar.NoVacio(cliente, "El cliente es obligatorio.");

		var carrito = new Carrito { Cliente = cliente.Trim() };
		context.Carritos.Add(carrito);
		context.SaveChanges();

		return carrito;
	}

	public Carrito ObtenerCarrito(int carritoId) {
		var carrito = context.Carritos
			.Include(carrito => carrito.Items)
			.ThenInclude(item => item.Producto)
			.SingleOrDefault(carrito => carrito.Id == carritoId);

		Verificar.NoNulo(carrito, $"No existe el carrito #{carritoId}.");
		return carrito!;
	}

	public void AgregarProducto(int carritoId, string codigoProducto, int cantidad) {
		var carrito = ObtenerCarrito(carritoId);
		Verificar.NoNulo(carrito, $"No existe el carrito #{carritoId}.");
		Verificar.Verdadero(carrito.Estado == EstadoCarrito.Abierto, "Solo se pueden agregar ítems a carritos abiertos.");

		var producto = ObtenerProductoPorCodigo(codigoProducto);
		Verificar.NoNulo(producto, $"No existe el producto {codigoProducto}.");

		Verificar.Positivo(cantidad, "La cantidad debe ser mayor a cero.");
		Verificar.Verdadero(producto.StockDisponible >= cantidad,  $"El producto {producto.Codigo} no tiene stock disponible.");


		EjecutarEnTransaccion(() => {
			var existente = carrito.Items.SingleOrDefault(item => item.ProductoId == producto.Id);
			if (existente is null) {
				carrito.Items.Add(new CarritoItem {
					ProductoId     = producto.Id,
					Cantidad       = cantidad,
					PrecioUnitario = producto.Precio
				});
			} else {
				existente.Cantidad += cantidad;
			}

			producto.StockDisponible -= cantidad;
		});
	}

	public void QuitarProducto(int carritoId, string codigoProducto, int cantidad) {
		var carrito = ObtenerCarrito(carritoId);
		Verificar.NoNulo(carrito, $"No existe el carrito #{carritoId}.");
		Verificar.Verdadero(carrito.Estado == EstadoCarrito.Abierto, "Solo se pueden eliminar ítems de carritos abiertos.");
		
		var producto = ObtenerProductoPorCodigo(codigoProducto);
		Verificar.NoNulo(producto, $"No existe el producto {codigoProducto}.");

		var item = carrito.Items.SingleOrDefault(item => item.ProductoId == producto.Id);
		Verificar.NoNulo(item, $"El carrito #{carritoId} no contiene el producto {producto.Codigo}.");

		Verificar.Positivo(cantidad, "La cantidad debe ser mayor a cero.");
		var cantidadARestituir = Math.Min(cantidad, item!.Cantidad);
		Verificar.Verdadero(cantidadARestituir > 0, "La cantidad a restituir debe ser mayor a cero.");

		EjecutarEnTransaccion(() => {
			if (item.Cantidad <= cantidad) {
				context.CarritoItems.Remove(item);
			} else {
				item.Cantidad -= cantidad;
			}
			producto.StockDisponible += cantidadARestituir;
		});
	}

	public Carrito ConfirmarCarrito(int carritoId) {
		return EjecutarEnTransaccion(() => {
			var carrito = ObtenerCarrito(carritoId);
			Verificar.NoNulo(carrito, $"No existe el carrito #{carritoId}.");
			Verificar.Verdadero(carrito.Estado == EstadoCarrito.Abierto, "Solo se pueden confirmar compras de carritos abiertos.");
			Verificar.NoVacio(carrito.Items, "No se puede confirmar una compra con el carrito vacío.");

			carrito.Estado = EstadoCarrito.Confirmado;
			carrito.ConfirmadoUtc = DateTime.UtcNow;

			return carrito;
		});
	}

	public Carrito CancelarCarrito(int carritoId) {
		var carrito = ObtenerCarrito(carritoId);
		Verificar.NoNulo(carrito, $"No existe el carrito #{carritoId}.");
		Verificar.Verdadero(carrito.Estado == EstadoCarrito.Abierto, "Solo se pueden cancelar carritos abiertos.");

		return EjecutarEnTransaccion(() => {
			foreach (var item in carrito.Items) {
				item.Producto.StockDisponible += item.Cantidad;
			}
			carrito.Estado = EstadoCarrito.Cancelado;
			return carrito;
		});
	}

	Producto ObtenerProductoPorCodigo(string codigo) {
		Verificar.NoVacio(codigo, "El código del producto es obligatorio.");

		var codigoNormalizado = NormalizarCodigo(codigo);
		var producto = context.Productos.SingleOrDefault(producto => producto.Codigo == codigoNormalizado);
		Verificar.NoNulo(producto, $"No existe el producto {codigoNormalizado}.");

		return producto!;
	}
	
	void EjecutarEnTransaccion(Action accion) {
		using var transaction = context.Database.BeginTransaction();

		try {
			accion();
			context.SaveChanges();
			transaction.Commit();
		} catch {
			transaction.Rollback();
			throw;
		}
	}

	T EjecutarEnTransaccion<T>(Func<T> accion) {
		using var transaction = context.Database.BeginTransaction();

		try {
			var resultado = accion();
			context.SaveChanges();
			transaction.Commit();
			return resultado;
		} catch {
			transaction.Rollback();
			throw;
		}
	}

	static string NormalizarCodigo(string codigo) {
		return codigo.Trim().ToUpperInvariant();
	}

	static string Requerido(string valor, string mensaje) {
		Verificar.NoVacio(valor, mensaje);

		return valor.Trim();
	}
}

class TiendaContext(string dbPath) : DbContext {
	public DbSet<Producto> Productos => Set<Producto>();
	public DbSet<Carrito> Carritos => Set<Carrito>();
	public DbSet<CarritoItem> CarritoItems => Set<CarritoItem>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		optionsBuilder.UseSqlite($"Data Source={dbPath}");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<Producto>(entity => {
			entity.ToTable("Productos");
			entity.HasKey(producto => producto.Id);
			entity.HasIndex(producto => producto.Codigo).IsUnique();
			entity.Property(producto => producto.Codigo).IsRequired().HasMaxLength(20);
			entity.Property(producto => producto.Nombre).IsRequired().HasMaxLength(120);
			entity.Property(producto => producto.Precio).HasPrecision(10, 2);
			entity.Property(producto => producto.StockDisponible).IsRequired();
		});

		modelBuilder.Entity<Carrito>(entity => {
			entity.ToTable("Carritos");
			entity.HasKey(carrito => carrito.Id);
			entity.Property(carrito => carrito.Cliente).IsRequired().HasMaxLength(120);
			entity.Property(carrito => carrito.Estado).HasConversion<string>().HasMaxLength(20);
		});

		modelBuilder.Entity<CarritoItem>(entity => {
			entity.ToTable("CarritoItems");
			entity.HasKey(item => item.Id);
			entity.HasIndex(item => new { item.CarritoId, item.ProductoId }).IsUnique();
			entity.Property(item => item.PrecioUnitario).HasPrecision(10, 2);
			entity.HasOne(item => item.Carrito)
				.WithMany(carrito => carrito.Items)
				.HasForeignKey(item => item.CarritoId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(item => item.Producto)
				.WithMany(producto => producto.ItemsCarrito)
				.HasForeignKey(item => item.ProductoId)
				.OnDelete(DeleteBehavior.Restrict);
		});
	}
}

static class Verificar {
	public static void NoNulo<T>(T? valor, string mensaje="") where T : class {
		if (valor is null) {
			throw new ArgumentNullException(mensaje);
		}
	}
	public static void NoVacio(string valor, string mensaje="") {
		if (string.IsNullOrWhiteSpace(valor)) {
			throw new ArgumentException("El valor no puede ser vacío.", mensaje);
		}
	}

	public static void NoVacio<T>(IEnumerable<T> coleccion, string mensaje="") {
		if (coleccion is null || !coleccion.Any()) {
			throw new ArgumentException("La colección no puede ser vacía.", mensaje);
		}
	}

	public static void Verdadero(bool condicion, string mensaje="") {
		if (!condicion) {
			throw new ArgumentException(mensaje);
		}
	}

    internal static void Positivo(decimal valor, string mensaje = "") {
        if(valor <= 0) {
			throw new ArgumentOutOfRangeException(mensaje);
		}
    }
}