#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Views;

const string ApiUrl = "http://localhost:5050";

using var http = new HttpClient { BaseAddress = new Uri(ApiUrl) };

List<ProductoDto> productos = [];
List<MovimientoDto> movimientos = [];
ProductoDto? seleccionado = null;

try {
    productos = await CargarProductosAsync();
    seleccionado = productos.FirstOrDefault();
    if (seleccionado is not null) movimientos = await CargarMovimientosAsync(seleccionado.Id);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verifica que servidor.cs este corriendo en http://localhost:5050");
    return;
}
using IApplication app = Application.Create().Init();
using Window ventana = new() { Title = " Catalogo REST - ESC para salir " };

var filtro = new TextField {
    X = 10,
    Y = 1,
    Width = 35,
    Text = ""
};

var listaProductos = new ListView {
    X = 1,
    Y = 3,
    Width = 58,
    Height = 18
};

var listaMovimientos = new ListView {
    X = 61,
    Y = 3,
    Width = 58,
    Height = 18
};

var estado = new Label {
    X = 1,
    Y = 22,
    Width = 118,
    Text = "Seleccione un producto para ver su historial. Use los botones para administrar el catalogo."
};

var btnAgregar = new Button { X = 1, Y = 24, Text = "Agregar" };
var btnEditar = new Button { X = 13, Y = 24, Text = "Editar" };
var btnEliminar = new Button { X = 24, Y = 24, Text = "Eliminar" };
var btnCompra = new Button { X = 38, Y = 24, Text = "Compra" };
var btnVenta = new Button { X = 50, Y = 24, Text = "Venta" };
var btnAjuste = new Button { X = 61, Y = 24, Text = "Ajuste" };
var btnRecargar = new Button { X = 73, Y = 24, Text = "Recargar" };


ventana.Add(detalleProducto);

app.Run(ventana);

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
