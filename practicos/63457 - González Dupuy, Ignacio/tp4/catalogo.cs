#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// -- Consulta inicial al servidor ------------------------------------------

List<ProductoDto> productos;
List<MovimientoDto> movimientos;
try {
    using var http = new HttpClient();
    ConfigurarHttp(http);
    productos = await CargarProductosAsync(http);
    movimientos = productos.Count == 0
        ? []
        : await CargarMovimientosAsync(http, productos[0].Id);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verifica que servidor.cs este corriendo en http://localhost:5050");
    return;
}

// -- Interfaz TUI -----------------------------------------------------------

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST - Productos (ESC para salir) " };

var panelProductos = new FrameView {
    Title = " Productos ",
    X = 0, Y = 0,
    Width = Dim.Percent(58),
    Height = Dim.Fill()
};

var panelMovimientos = new FrameView {
    Title = " Movimientos ",
    X = Pos.Right(panelProductos),
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var listaProductos = new ListView {
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Source = new ListWrapper<string>(new ObservableCollection<string>(productos.Select(FormatearProducto).ToList()))
};

var detalleMovimientos = new Label {
    Text = RenderizarMovimientos(movimientos),
    X = 1, Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

listaProductos.ValueChanged += async (sender, args) =>
{
    var indice = listaProductos.SelectedItem;
    if (!indice.HasValue || indice.Value < 0 || indice.Value >= productos.Count) {
        return;
    }

    using var http = new HttpClient();
    ConfigurarHttp(http);
    var seleccionado = productos[indice.Value];
    var nuevosMovimientos = await CargarMovimientosAsync(http, seleccionado.Id);
    detalleMovimientos.Text = RenderizarMovimientos(nuevosMovimientos);
};

panelProductos.Add(listaProductos);
panelMovimientos.Add(detalleMovimientos);
ventana.Add(panelProductos, panelMovimientos);

app.Run(ventana);

static void ConfigurarHttp(HttpClient http) {
    http.BaseAddress = new Uri("http://localhost:5050");
}

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http) {
    const string url = "/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? throw new HttpRequestException("El servidor devolvio una lista vacia");
}

static async Task<List<MovimientoDto>> CargarMovimientosAsync(HttpClient http, int productoId) {
    return await http.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoId}/movimientos")
        ?? [];
}

static string FormatearProducto(ProductoDto producto) {
    return $"{producto.Codigo,-8} {producto.Nombre,-24} ${producto.Precio,9:N2} Stock: {producto.Stock,4}";
}

static string RenderizarMovimientos(List<MovimientoDto> movimientos) {
    if (movimientos.Count == 0) {
        return "Sin movimientos registrados.";
    }

    var lineas = new List<string>
    {
        $"{"TIPO",-8} {"CANT.",8} {"FECHA",-19}",
        new string('-', 34)
    };

    lineas.AddRange(movimientos.Select(m =>
        $"{m.Tipo,-8} {m.Cantidad,8} {m.Fecha:dd/MM/yyyy HH:mm}"));

    return string.Join(Environment.NewLine, lineas);
}

// -- DTO --------------------------------------------------------------------

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

[JsonConverter(typeof(JsonStringEnumConverter))]
enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);