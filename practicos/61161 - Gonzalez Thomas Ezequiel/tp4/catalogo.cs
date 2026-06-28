#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Linq;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;
List<MovimientoDto> movimientos;

try
{
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
    static async Task<List<MovimientoDto>> CargarMovimientosAsync(
    HttpClient http,
    int productoId)
{
    string url =
        $"http://localhost:5050/productos/{productoId}/movimientos";

    return await http.GetFromJsonAsync<List<MovimientoDto>>(url)
        ?? new List<MovimientoDto>();
}
    movimientos = await CargarMovimientosAsync(http, 1);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();

using Window ventana = new()
{
    Title = " Catalogo REST — Productos (ESC para salir) "
};

var productosLabel = new Label
{
    Text =
        "PRODUCTOS\n\n" +
        string.Join(
            "\n",
            productos.Select(p =>
                $"{p.Codigo} - {p.Nombre} - Stock:{p.Stock}"
            )
        ),
    X = 2,
    Y = 1,
};

var detalleLabel = new Label
{
   Text =
    "MOVIMIENTOS\n\n" +
    (movimientos.Count == 0
        ? "Sin movimientos"
        : string.Join(
            "\n",
            movimientos.Select(m =>
                $"{m.Tipo} {m.Cantidad}"
            )
        )),
};

ventana.Add(productosLabel);
ventana.Add(detalleLabel);

app.Run(ventana);

// ── API REST ──────────────────────────────────────────────────────────────

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
    string Tipo,
    int Cantidad,
    DateTime Fecha
);