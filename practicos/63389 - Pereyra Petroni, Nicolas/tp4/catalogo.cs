#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;

try
{
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}
static async Task<List<MovimientoDto>> CargarMovimientosAsync(
    HttpClient http,
    int productoId)
{
    string url = $"http://localhost:5050/productos/{productoId}/movimientos";

    return await http.GetFromJsonAsync<List<MovimientoDto>>(url)
           ?? new List<MovimientoDto>();
}
static string FormatearMovimientos(List<MovimientoDto> movimientos)
{
    if (movimientos.Count == 0)
        return "Sin movimientos";

    return string.Join(
        "\n",
        movimientos.Select(m =>
            $"{TipoTexto(m.Tipo)} | {m.Cantidad} | {m.Fecha:dd/MM HH:mm}")
    );
}

static string TipoTexto(int tipo)
{
    return tipo switch
    {
        0 => "Compra",
        1 => "Venta",
        2 => "Ajuste",
        _ => "?"
    };
}
// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();

using Window ventana = new()
{
    Title = " Catalogo REST — Producto (ESC para salir) "
};

var items = new ObservableCollection<string>(
    productos.Select(p => $"{p.Codigo} - {p.Nombre}")
);

var listaProductos = new ListView()
{
    X = 1,
    Y = 1,
    Width = 40,
    Height = Dim.Fill()
};

listaProductos.SetSource(items);

var detalleProducto = new Label()
{
    Text = """
           DETALLE PRODUCTO

           Seleccione un producto
           """,
    X = 45,
    Y = 2,
    Width = 40,
    Height = Dim.Fill()
};

listaProductos.ValueChanged += async (_, _) =>
{
    int indice = listaProductos.SelectedItem ?? 0;

    if (indice < 0 || indice >= productos.Count)
        return;

    var producto = productos[indice];

    using var http = new HttpClient();

    var movimientos = await CargarMovimientosAsync(http, producto.Id);

    detalleProducto.Text =
    $"""
    PRODUCTO

    {producto.Nombre}

    Stock: {producto.Stock}

    MOVIMIENTOS

    {FormatearMovimientos(movimientos)}
    """;
};

ventana.Add(detalleProducto);
ventana.Add(listaProductos);

if (productos.Count > 0)
{
    detalleProducto.Text =
    """
    Seleccione un producto
    para ver sus movimientos
    """;
}

app.Run(ventana);

// ── API ───────────────────────────────────────────────────────────────────

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
           ?? new List<ProductoDto>();
}


// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);
record MovimientoDto(
    int Id,
    int ProductoId,
    int Tipo,
    int Cantidad,
    DateTime Fecha
);