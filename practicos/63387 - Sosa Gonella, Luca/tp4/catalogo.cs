#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Cliente HTTP ──────────────────────────────────────────────────────────

var http = new HttpClient();

// ── Cargar productos ──────────────────────────────────────────────────────

List<ProductoDto> productos;

try
{
    productos = await CargarProductosAsync(http);
}
catch (Exception ex)
{
    Console.WriteLine($"Error al conectar con el servidor: {ex.Message}");
    return;
}

// ── Cargar movimientos del primer producto ────────────────────────────────

List<MovimientoDto> movimientos = [];

if (productos.Any())
{
    movimientos = await CargarMovimientosAsync(http, productos[0].Id);
}

// ── Interfaz ──────────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();

using Window ventana = new()
{
    Title = " Catalogo REST "
};

// ── PRODUCTOS ─────────────────────────────────────────────────────────────

var productosLabel = new Label
{
    X = 1,
    Y = 1,
    Text =
$"""
# PRODUCTOS

{string.Join("\n", productos.Select(p =>
$"- {p.Id} | {p.Codigo} | {p.Nombre} | ${p.Precio:N2} | Stock: {p.Stock}"
))}
"""
};

// ── MOVIMIENTOS ───────────────────────────────────────────────────────────

var movimientosLabel = new Label
{
    X = 1,
    Y = productos.Count + 6,
    Text =
$"""
# MOVIMIENTOS DEL PRODUCTO {productos.FirstOrDefault()?.Id}

{string.Join("\n", movimientos.Select(m =>
$"- {NombreMovimiento(m.Tipo)} | Cantidad: {m.Cantidad} | {m.Fecha:g}"
))}
"""
};

ventana.Add(productosLabel);
ventana.Add(movimientosLabel);

app.Run(ventana);

// ── Funciones HTTP ────────────────────────────────────────────────────────

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
           ?? [];
}

static async Task<List<MovimientoDto>> CargarMovimientosAsync(
    HttpClient http,
    int productoId)
{
    return await http.GetFromJsonAsync<List<MovimientoDto>>(
        $"http://localhost:5050/productos/{productoId}/movimientos"
    ) ?? [];
}

static string NombreMovimiento(int tipo) =>
    tipo switch
    {
        0 => "Compra",
        1 => "Venta",
        2 => "Ajuste",
        _ => "Desconocido"
    };

// ── DTOs ──────────────────────────────────────────────────────────────────

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