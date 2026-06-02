#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

const string miserver = "http://localhost:5050"; 
using var http = new HttpClient { BaseAddress = new Uri(miserver) };
try {
   var lista = await http.GetFromJsonAsync<List<ProductoDto>>("/productos")
    ?? throw new HttpRequestException("El servidor no respondió con una lista de productos validos.");
} catch (HttpRequestException ex) {
    Console.WriteLine($"No se pudo conectar al servidor: {ex.Message}");
    return;
}
// ── Interfaz TUI ──────────────────────────────────────────────────────────
var productos = new List<ProductoDto>();
var productosFiltrados = new List<ProductoDto>();
var productosVista = new ObservableCollection<string>();
var movimientosVista = new ObservableCollection<string>();
app.Run(ventana);

// ── DTO ───────────────────────────────────────────────────────────────────

enum TipoMovimiento{Compra,Venta,Ajuste} //enum de los movimientos que se podran hacer
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock); // no agregamos un id xq el servidor lo asigna solo
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);
