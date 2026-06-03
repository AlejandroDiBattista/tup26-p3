#!/usr/bin/env -S dotnet run

#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System.Net.Http.Json;
using static System.Console;

string baseUrl = args.Length > 0 ? args[0] : "http://localhost:5002";

Clear();
WriteLine("=== Cliente REST Carrito de compras ===");
WriteLine($"API REST: {baseUrl}");
WriteLine();

using HttpClient http = new() { BaseAddress = new Uri(baseUrl) };
var tienda = new TiendaApiClient(http);

MostrarCatalogo(await tienda.ListarCatalogo());

var carrito = await tienda.CrearCarrito("Juan Perez");
WriteLine($"Carrito creado: #{carrito.Id} para {carrito.Cliente}");
WriteLine();

await tienda.AgregarProducto(carrito.Id,  "NOTE-15",  1);
await tienda.AgregarProducto(carrito.Id,  "MOUSE-WL", 2);
await tienda.AgregarProducto(carrito.Id,  "USB-C",    3);
await tienda.QuitarProducto(carrito.Id,   "USB-C",    1);

WriteLine("Catalogo luego de reservar stock:");
MostrarCatalogo(await tienda.ListarCatalogo());

var carritoActual = await tienda.ObtenerCarrito(carrito.Id);
MostrarCarrito(carritoActual);

var carritoConfirmado = await tienda.ConfirmarCarrito(carrito.Id);
MostrarConfirmacion(carritoConfirmado);

var carritoCancelado = await tienda.CrearCarrito("Analia Gomez");
await tienda.AgregarProducto(carritoCancelado.Id, "WEBCAM", 1);
await tienda.AgregarProducto(carritoCancelado.Id, "TECL-WL", 1);

WriteLine();
WriteLine($"Carrito a cancelar: #{carritoCancelado.Id} para {carritoCancelado.Cliente}");
MostrarCarrito(await tienda.ObtenerCarrito(carritoCancelado.Id));

var carritoCanceladoFinal = await tienda.CancelarCarrito(carritoCancelado.Id);
WriteLine($"Carrito cancelado: #{carritoCanceladoFinal.Id} [{carritoCanceladoFinal.Estado}]");
WriteLine();

WriteLine("Catalogo final:");
MostrarCatalogo(await tienda.ListarCatalogo());


static void MostrarCatalogo(IReadOnlyList<Producto> productos) {
	WriteLine("\n= Catalogo de productos ======================================");
	foreach (var producto in productos) {
		WriteLine($"- {producto.Codigo,-8} | {producto.Nombre,-24} | stock {producto.StockDisponible,2} | ${producto.Precio,10:0.00}");
	}
	WriteLine();
}

static void MostrarCarrito(Carrito carrito) {
	WriteLine("\n= Detalle del carrito ========================================");
	WriteLine($"Carrito #{carrito.Id} [{carrito.Estado}] - Cliente: {carrito.Cliente}");
	foreach (var item in carrito.Items.OrderBy(item => item.Producto.Nombre)) {
		WriteLine($"- {item.Producto.Nombre,-24} x {item.Cantidad,2} = ${item.Subtotal,10:0.00}");
	}
	WriteLine($" Total del carrito: ${carrito.Total:0.00}");
	WriteLine();
}

static void MostrarConfirmacion(Carrito carrito) {
	WriteLine("\n= Confirmacion de compra =====================================");
	WriteLine($"Compra confirmada para el carrito #{carrito.Id}");
	WriteLine($"- Cliente: {carrito.Cliente}");
	WriteLine($"- Fecha: {carrito.ConfirmadoUtc:yyyy-MM-dd HH:mm:ss}");
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
}

class Carrito {
	public int Id { get; set; }
	public string Cliente { get; set; } = string.Empty;
	public DateTime CreadoUtc { get; set; }
	public DateTime? ConfirmadoUtc { get; set; }
	public EstadoCarrito Estado { get; set; }
	public List<CarritoItem> Items { get; set; } = [];
	public decimal Total => Items.Sum(item => item.Subtotal);
}

class CarritoItem {
	public int Id { get; set; }
	public int CarritoId { get; set; }
	public int ProductoId { get; set; }
	public int Cantidad { get; set; }
	public decimal PrecioUnitario { get; set; }
	public Producto Producto { get; set; } = new();
	public decimal Subtotal => PrecioUnitario * Cantidad;
}

record CrearCarritoRequest(string Cliente);
record ItemCarritoRequest(string CodigoProducto, int Cantidad);

class TiendaApiClient(HttpClient http) {
	public async Task<IReadOnlyList<Producto>> ListarCatalogo() {
		return await Get<IReadOnlyList<Producto>>("/catalogo");
	}

	public async Task<Carrito> CrearCarrito(string cliente) {
		var response = await http.PostAsJsonAsync("/carritos", new CrearCarritoRequest(cliente));
		return await LeerRespuesta<Carrito>(response);
	}

	public async Task<Carrito> ObtenerCarrito(int carritoId) {
		return await Get<Carrito>($"/carritos/{carritoId}");
	}

	public async Task<Carrito> AgregarProducto(int carritoId, string codigoProducto, int cantidad) {
		var response = await http.PostAsJsonAsync($"/carritos/{carritoId}/items", new ItemCarritoRequest(codigoProducto, cantidad));
		return await LeerRespuesta<Carrito>(response);
	}

	public async Task<Carrito> QuitarProducto(int carritoId, string codigoProducto, int cantidad) {
		var response = await http.PostAsJsonAsync($"/carritos/{carritoId}/items/quitar", new ItemCarritoRequest(codigoProducto, cantidad));
		return await LeerRespuesta<Carrito>(response);
	}

	public async Task<Carrito> ConfirmarCarrito(int carritoId) {
		var response = await http.PostAsync($"/carritos/{carritoId}/confirmar", content: null);
		return await LeerRespuesta<Carrito>(response);
	}

	public async Task<Carrito> CancelarCarrito(int carritoId) {
		var response = await http.PostAsync($"/carritos/{carritoId}/cancelar", content: null);
		return await LeerRespuesta<Carrito>(response);
	}

	async Task<T> Get<T>(string ruta) {
		var response = await http.GetAsync(ruta);
		return await LeerRespuesta<T>(response);
	}

	static async Task<T> LeerRespuesta<T>(HttpResponseMessage response) {
		if (!response.IsSuccessStatusCode) {
			string error = await response.Content.ReadAsStringAsync();
			throw new InvalidOperationException($"HTTP {(int)response.StatusCode}: {error}");
		}

		var valor = await response.Content.ReadFromJsonAsync<T>();
		return valor ?? throw new InvalidOperationException("La API devolvio una respuesta vacia.");
	}
}
