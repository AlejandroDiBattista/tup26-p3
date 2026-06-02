#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

const string BaseUrl = "http://localhost:5050";

List<ProductoDto> productosIniciales;
try {
    using var clientePrueba = new HttpClient();
    productosIniciales = await clientePrueba.GetFromJsonAsync<List<ProductoDto>>($"{BaseUrl}/productos") ?? [];
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verifica que servidor.cs este corriendo en http://localhost:5050");
    return;
}

using IApplication appTui = Application.Create();
appTui.Init();

var ventana = new Window {
    Title = " CatalogoREST - F1 Agregar  F2 Editar  F3 Eliminar  F4 Movimiento  ESC Salir ",
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill(),
};

var lblBuscar = new Label {
    Text = "Buscar:",
    X = 1, Y = 0,
};

var txtBuscar = new TextField {
    X = Pos.Right(lblBuscar) + 1, Y = 0,
    Width = 30,
};

var listaProductos = new ListView {
    X = 0, Y = 2,
    Width = Dim.Percent(50), Height = Dim.Fill(1),
};

var lblMovimientos = new Label {
    Text = "Historial de movimientos",
    X = Pos.Percent(50) + 1, Y = 2,
};

var listaMovimientos = new ListView {
    X = Pos.Percent(50) + 1, Y = 3,
    Width = Dim.Fill(1), Height = Dim.Fill(1),
};

ventana.Add(lblBuscar, txtBuscar, listaProductos, lblMovimientos, listaMovimientos);


var productos = new List<ProductoDto>(productosIniciales);
var productosFiltrados = new List<ProductoDto>();
var http = new HttpClient();

txtBuscar.TextChanged += (_, _) => RefrescarLista();
listaProductos.ValueChanged += async (_, _) => await RefrescarMovimientos();

RefrescarLista();

appTui.Run(ventana);
http.Dispose();



// ── Interfaz TUI ──────────────────────────────────────────────────────────

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
