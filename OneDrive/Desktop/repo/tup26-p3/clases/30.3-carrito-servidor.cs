#!/usr/bin/env -S dotnet run

#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*
#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;

const string host = "http://localhost:5002";
const string dbPath = "30.3-carrito-servidor.sqlite";
string connectionString = $"Data Source={dbPath}";

if (File.Exists(dbPath)) {
	File.Delete(dbPath);
}

using (var db = new SqliteConnection(connectionString)) {
	db.Open();
	TiendaService.CrearEsquema(db);
}

var tienda = new TiendaService(connectionString);
CargarCatalogo(tienda);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(host);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new {
	Servicio = "Carrito de compras",
	Base = dbPath,
	Endpoints = new[] {
		"GET /catalogo",
		"POST /productos",
		"POST /carritos",
		"GET /carritos/{id}",
		"POST /carritos/{id}/items",
		"POST /carritos/{id}/items/quitar",
		"POST /carritos/{id}/confirmar",
		"POST /carritos/{id}/cancelar"
	}
}));

app.MapGet("/catalogo", () =>
	Ejecutar(() => Results.Ok(tienda.ListarCatalogo())));

app.MapPost("/productos", (CrearProductoRequest request) =>
	Ejecutar(() => Results.Created("/productos/" + request.Codigo, tienda.CrearProducto(
		request.Codigo,
		request.Nombre,
		request.Precio,
		request.StockDisponible))));

app.MapPost("/carritos", (CrearCarritoRequest request) =>
	Ejecutar(() => {
		var carrito = tienda.CrearCarrito(request.Cliente);
		return Results.Created($"/carritos/{carrito.Id}", carrito);
	}));

app.MapGet("/carritos/{id:int}", (int id) =>
	Ejecutar(() => Results.Ok(tienda.ObtenerCarrito(id))));

app.MapPost("/carritos/{id:int}/items", (int id, ItemCarritoRequest request) =>
	Ejecutar(() => {
		tienda.AgregarProducto(id, request.CodigoProducto, request.Cantidad);
		return Results.Ok(tienda.ObtenerCarrito(id));
	}));

app.MapPost("/carritos/{id:int}/items/quitar", (int id, ItemCarritoRequest request) =>
	Ejecutar(() => {
		tienda.QuitarProducto(id, request.CodigoProducto, request.Cantidad);
		return Results.Ok(tienda.ObtenerCarrito(id));
	}));

app.MapPost("/carritos/{id:int}/confirmar", (int id) =>
	Ejecutar(() => Results.Ok(tienda.ConfirmarCarrito(id))));

app.MapPost("/carritos/{id:int}/cancelar", (int id) =>
	Ejecutar(() => Results.Ok(tienda.CancelarCarrito(id))));

Console.WriteLine("=== API REST Carrito de compras ===");
Console.WriteLine($"Servidor: {host}");
Console.WriteLine($"Base SQLite: {dbPath}");
Console.WriteLine();

app.Run();


static void CargarCatalogo(TiendaService tienda) {
	tienda.CrearProducto("NOTE-15",  "Notebook 15 pulgadas",  1_250_000m, 5);
	tienda.CrearProducto("MOUSE-WL", "Mouse inalambrico",        28_500m, 12);
	tienda.CrearProducto("USB-C",    "Hub USB-C",                41_990m, 8);
	tienda.CrearProducto("MON-27",   "Monitor 27 pulgadas",     310_000m, 4);
	tienda.CrearProducto("TECL-WL",  "Teclado inalambrico",      45_000m, 6);
	tienda.CrearProducto("WEBCAM",   "Webcam Full HD",           85_000m, 3);
	tienda.CrearProducto("AUR-WL",   "Auriculares inalambricos", 60_000m, 7);
}

static IResult Ejecutar(Func<IResult> accion) {
	try {
		return accion();
	} catch (RecursoNoEncontradoException ex) {
		return Results.NotFound(new { error = ex.Message });
	} catch (ArgumentException ex) {
		return Results.BadRequest(new { error = string.IsNullOrWhiteSpace(ex.Message) ? "Solicitud invalida." : ex.Message });
	} catch (InvalidOperationException ex) {
		return Results.BadRequest(new { error = ex.Message });
	}
}

record CrearProductoRequest(string Codigo, string Nombre, decimal Precio, int StockDisponible);
record CrearCarritoRequest(string Cliente);
record ItemCarritoRequest(string CodigoProducto, int Cantidad);

enum EstadoCarrito {
	Abierto    = 1,
	Confirmado = 2,
	Cancelado  = 3
}

[Table("Productos")]
class Producto {
	[Key]
	public int Id { get; set; }
	public string Codigo { get; set; } = string.Empty;
	public string Nombre { get; set; } = string.Empty;
	public decimal Precio { get; set; }
	public int StockDisponible { get; set; }
	public bool Activo { get; set; } = true;

	[Write(false)]
	public ICollection<CarritoItem> ItemsCarrito { get; set; } = [];
}

[Table("Carritos")]
class Carrito {
	[Key]
	public int Id { get; set; }
	public string Cliente { get; set; } = string.Empty;
	public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
	public DateTime? ConfirmadoUtc { get; set; }
	public EstadoCarrito Estado { get; set; } = EstadoCarrito.Abierto;

	[Write(false)]
	public ICollection<CarritoItem> Items { get; set; } = [];

	[Write(false)]
	public decimal Total => Items.Sum(item => item.Subtotal);
}

[Table("CarritoItems")]
class CarritoItem {
	[Key]
	public int Id { get; set; }
	public int CarritoId { get; set; }
	public int ProductoId { get; set; }
	public int Cantidad { get; set; }
	public decimal PrecioUnitario { get; set; }

	[Write(false)]
	public Carrito Carrito { get; set; } = null!;

	[Write(false)]
	public Producto Producto { get; set; } = null!;

	[Write(false)]
	public decimal Subtotal => PrecioUnitario * Cantidad;
}

class TiendaService(string connectionString) {
	public static void CrearEsquema(IDbConnection db) {
		db.Execute("""
			PRAGMA foreign_keys = ON;

			CREATE TABLE Productos (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				Codigo TEXT NOT NULL UNIQUE,
				Nombre TEXT NOT NULL,
				Precio NUMERIC NOT NULL,
				StockDisponible INTEGER NOT NULL,
				Activo INTEGER NOT NULL DEFAULT 1
			);

			CREATE TABLE Carritos (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				Cliente TEXT NOT NULL,
				CreadoUtc TEXT NOT NULL,
				ConfirmadoUtc TEXT NULL,
				Estado INTEGER NOT NULL
			);

			CREATE TABLE CarritoItems (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				CarritoId INTEGER NOT NULL,
				ProductoId INTEGER NOT NULL,
				Cantidad INTEGER NOT NULL,
				PrecioUnitario NUMERIC NOT NULL,
				UNIQUE (CarritoId, ProductoId),
				FOREIGN KEY (CarritoId) REFERENCES Carritos(Id) ON DELETE CASCADE,
				FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE RESTRICT
			);
			""");
	}

	public Producto CrearProducto(string codigo, string nombre, decimal precio, int stockDisponible) {
		Verificar.NoVacio(codigo, "El codigo del producto es obligatorio.");
		Verificar.NoVacio(nombre, "El nombre del producto es obligatorio.");
		Verificar.Positivo(precio, "El precio debe ser mayor a cero.");
		Verificar.NoNegativo(stockDisponible, "El stock disponible debe ser mayor o igual a cero.");

		using var db = AbrirConexion();
		var codigoNormalizado = NormalizarCodigo(codigo);
		var nombreNormalizado = nombre.Trim();

		var existente = ObtenerProductoPorCodigoOpcional(db, codigoNormalizado);
		if (existente is not null) {
			return existente;
		}

		var producto = new Producto {
			Codigo = codigoNormalizado,
			Nombre = nombreNormalizado,
			Precio = precio,
			StockDisponible = stockDisponible
		};

		producto.Id = (int)db.Insert(producto);
		return producto;
	}

	public IReadOnlyList<Producto> ListarCatalogo() {
		using var db = AbrirConexion();

		return db.Query<Producto>("""
			SELECT Id, Codigo, Nombre, Precio, StockDisponible, Activo
			FROM Productos
			ORDER BY Nombre;
			""").ToList();
	}

	public Carrito CrearCarrito(string cliente) {
		Verificar.NoVacio(cliente, "El cliente es obligatorio.");

		using var db = AbrirConexion();
		var carrito = new Carrito { Cliente = cliente.Trim() };
		carrito.Id = (int)db.Insert(carrito);
		return carrito;
	}

	public Carrito ObtenerCarrito(int carritoId) {
		using var db = AbrirConexion();
		return ObtenerCarrito(db, carritoId);
	}

	public void AgregarProducto(int carritoId, string codigoProducto, int cantidad) {
		Verificar.Positivo(cantidad, "La cantidad debe ser mayor a cero.");

		EjecutarEnTransaccion((db, tx) => {
			var carrito = ObtenerCarrito(db, carritoId);
			Verificar.Verdadero(carrito.Estado == EstadoCarrito.Abierto, "Solo se pueden agregar items a carritos abiertos.");

			var producto = ObtenerProductoPorCodigo(db, codigoProducto);
			Verificar.Verdadero(producto.StockDisponible >= cantidad, $"El producto {producto.Codigo} no tiene stock disponible.");

			var existente = carrito.Items.SingleOrDefault(item => item.ProductoId == producto.Id);
			if (existente is null) {
				var item = new CarritoItem {
					CarritoId       = carrito.Id,
					ProductoId      = producto.Id,
					Cantidad        = cantidad,
					PrecioUnitario  = producto.Precio
				};
				db.Insert(item, tx);
			} else {
				existente.Cantidad += cantidad;
				db.Update(existente, tx);
			}

			producto.StockDisponible -= cantidad;
			db.Update(producto, tx);
		});
	}

	public void QuitarProducto(int carritoId, string codigoProducto, int cantidad) {
		Verificar.Positivo(cantidad, "La cantidad debe ser mayor a cero.");

		EjecutarEnTransaccion((db, tx) => {
			var carrito = ObtenerCarrito(db, carritoId);
			Verificar.Verdadero(carrito.Estado == EstadoCarrito.Abierto, "Solo se pueden eliminar items de carritos abiertos.");

			var producto = ObtenerProductoPorCodigo(db, codigoProducto);
			var item = carrito.Items.SingleOrDefault(item => item.ProductoId == producto.Id);
			Verificar.NoNulo(item, $"El carrito #{carritoId} no contiene el producto {producto.Codigo}.");

			var cantidadARestituir = Math.Min(cantidad, item!.Cantidad);
			Verificar.Verdadero(cantidadARestituir > 0, "La cantidad a restituir debe ser mayor a cero.");

			if (item.Cantidad <= cantidad) {
				db.Delete(item, tx);
			} else {
				item.Cantidad -= cantidad;
				db.Update(item, tx);
			}

			producto.StockDisponible += cantidadARestituir;
			db.Update(producto, tx);
		});
	}

	public Carrito ConfirmarCarrito(int carritoId) {
		return EjecutarEnTransaccion((db, tx) => {
			var carrito = ObtenerCarrito(db, carritoId);
			Verificar.Verdadero(carrito.Estado == EstadoCarrito.Abierto, "Solo se pueden confirmar compras de carritos abiertos.");
			Verificar.NoVacio(carrito.Items, "No se puede confirmar una compra con el carrito vacio.");

			carrito.Estado = EstadoCarrito.Confirmado;
			carrito.ConfirmadoUtc = DateTime.UtcNow;
			db.Update(carrito, tx);

			return ObtenerCarrito(db, carritoId);
		});
	}

	public Carrito CancelarCarrito(int carritoId) {
		return EjecutarEnTransaccion((db, tx) => {
			var carrito = ObtenerCarrito(db, carritoId);
			Verificar.Verdadero(carrito.Estado == EstadoCarrito.Abierto, "Solo se pueden cancelar carritos abiertos.");

			foreach (var item in carrito.Items) {
				item.Producto.StockDisponible += item.Cantidad;
				db.Update(item.Producto, tx);
			}

			carrito.Estado = EstadoCarrito.Cancelado;
			db.Update(carrito, tx);

			return ObtenerCarrito(db, carritoId);
		});
	}

	SqliteConnection AbrirConexion() {
		var db = new SqliteConnection(connectionString);
		db.Open();
		db.Execute("PRAGMA foreign_keys = ON;");
		return db;
	}

	static Carrito ObtenerCarrito(IDbConnection db, int carritoId) {
		var carrito = db.QuerySingleOrDefault<Carrito>("""
			SELECT Id, Cliente, CreadoUtc, ConfirmadoUtc, Estado
			FROM Carritos
			WHERE Id = @carritoId;
			""", new { carritoId });

		if (carrito is null) {
			throw new RecursoNoEncontradoException($"No existe el carrito #{carritoId}.");
		}

		var items = db.Query<CarritoItem, Producto, CarritoItem>("""
			SELECT
				i.Id, i.CarritoId, i.ProductoId, i.Cantidad, i.PrecioUnitario,
				p.Id, p.Codigo, p.Nombre, p.Precio, p.StockDisponible, p.Activo
			FROM CarritoItems i
			INNER JOIN Productos p ON p.Id = i.ProductoId
			WHERE i.CarritoId = @carritoId;
			""",
			(item, producto) => {
				item.Producto = producto;
				return item;
			},
			new { carritoId },
			splitOn: "Id").ToList();

		carrito.Items = items;
		return carrito;
	}

	static Producto ObtenerProductoPorCodigo(IDbConnection db, string codigo) {
		Verificar.NoVacio(codigo, "El codigo del producto es obligatorio.");

		var codigoNormalizado = NormalizarCodigo(codigo);
		var producto = ObtenerProductoPorCodigoOpcional(db, codigoNormalizado);
		if (producto is null) {
			throw new RecursoNoEncontradoException($"No existe el producto {codigoNormalizado}.");
		}

		return producto;
	}

	static Producto? ObtenerProductoPorCodigoOpcional(IDbConnection db, string codigo) {
		var codigoNormalizado = NormalizarCodigo(codigo);

		return db.QuerySingleOrDefault<Producto>("""
			SELECT Id, Codigo, Nombre, Precio, StockDisponible, Activo
			FROM Productos
			WHERE Codigo = @codigoNormalizado;
			""", new { codigoNormalizado });
	}

	void EjecutarEnTransaccion(Action<IDbConnection, IDbTransaction> accion) {
		using var db = AbrirConexion();
		using var tx = db.BeginTransaction();

		try {
			accion(db, tx);
			tx.Commit();
		} catch {
			tx.Rollback();
			throw;
		}
	}

	T EjecutarEnTransaccion<T>(Func<IDbConnection, IDbTransaction, T> accion) {
		using var db = AbrirConexion();
		using var tx = db.BeginTransaction();

		try {
			var resultado = accion(db, tx);
			tx.Commit();
			return resultado;
		} catch {
			tx.Rollback();
			throw;
		}
	}

	static string NormalizarCodigo(string codigo) {
		return codigo.Trim().ToUpperInvariant();
	}
}

class RecursoNoEncontradoException(string mensaje) : Exception(mensaje);

static class Verificar {
	public static void NoNulo<T>(T? valor, string mensaje = "") where T : class {
		if (valor is null) {
			throw new ArgumentException(mensaje);
		}
	}

	public static void NoVacio(string valor, string mensaje = "") {
		if (string.IsNullOrWhiteSpace(valor)) {
			throw new ArgumentException("El valor no puede ser vacio.", mensaje);
		}
	}

	public static void NoVacio<T>(IEnumerable<T> coleccion, string mensaje = "") {
		if (coleccion is null || !coleccion.Any()) {
			throw new ArgumentException("La coleccion no puede ser vacia.", mensaje);
		}
	}

	public static void Verdadero(bool condicion, string mensaje = "") {
		if (!condicion) {
			throw new ArgumentException(mensaje);
		}
	}

	public static void Positivo(decimal valor, string mensaje = "") {
		if (valor <= 0) {
			throw new ArgumentOutOfRangeException(mensaje);
		}
	}

	public static void NoNegativo(decimal valor, string mensaje = "") {
		if (valor < 0) {
			throw new ArgumentOutOfRangeException(mensaje);
		}
	}
}
