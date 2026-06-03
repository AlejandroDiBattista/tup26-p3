#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;


// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;
try {
    using var http = new HttpClient();
    productos = await CargarProductoAsync(http);  
} 
catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var lista = new ListView() {
    X = 1,
    Y = 2,
    Width = 30,
    Height = 20
};
var detalle = new Label() {
    X = 42,
    Y = 2,
    Width = 30,
    Height = 10,
    Text = "Seleccione un producto"
};

lista.SetSource(
    new System.Collections.ObjectModel.ObservableCollection<string>
       ( productos.Select(p => $"{p.Codigo} - {p.Nombre}" ). ToList()
    )
);

if (productos.Count > 0)
{
    using var http = new HttpClient();
    var movimientos = await CargarMovimientosAsync(http, 2);


    if (movimientos.Count == 0) {
        
        detalle.Text = "Sin movimientos registrados";
    }
    else {
        detalle.Text = string.Join("\n", 
        movimientos.Select(m => $"{m.Tipo} | Cant: {m.Cantidad} "));
    }
}
 
var tituloProductos = new Label() {
    X = 1,
    Y = 0,
    Text = "Productos"
};

var tituloDetalle = new Label() {
     X = 42,
     Y = 0,
     Text = "Movimientos"
};

ventana.Add(tituloProductos);
ventana.Add(tituloDetalle);
ventana.Add(lista);
ventana.Add(detalle);

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductoAsync(HttpClient http)
{
    const string url = "http://Localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url) 
        ?? new List<ProductoDto>();
}

static async Task<List<MovimientoDto>> CargarMovimientosAsync(HttpClient http, int productoId)
{
    string url = $"http://Localhost:5050/productos/{productoId}/movimientos";

    return await http.GetFromJsonAsync<List<MovimientoDto>>(url) 
        ?? new List<MovimientoDto>();
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

record MovimientoDto(
    int Id, int productoId, TipoMovimiento Tipo, int Cantidad
);

enum TipoMovimiento{
    Compra,
    Venta,
    Ajuste
}

