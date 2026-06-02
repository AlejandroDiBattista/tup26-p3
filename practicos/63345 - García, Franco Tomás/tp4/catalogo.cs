#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui;
using System.Collections.ObjectModel;

// ── Consulta inicial al servidor ──────────────────────────────────────────
List<ProductoDto> productos;
try {
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Productos (ESC para salir) " };

var listaStrings = new ObservableCollection<string>(
    productos.Select(p => $"{p.Codigo} - {p.Nombre} (${p.Precio}) [Stock: {p.Stock}]")
);

var listaProductos = new ListView()
{
    X = 0,
    Y = 0,
    Width = 80,
    Height = 20
};

var detalle = new Label()
{
    X = 0,
    Y = 22,
    Width = 80,
    Height = 5,
    Text = "Seleccioná un producto..."
};

listaProductos.SetSource<string>(listaStrings);

var movimientosStrings = new ObservableCollection<string>();
var listaMovimientos = new ListView()
{
    X = 82,
    Y = 0,
    Width = 60,
    Height = 20
};
listaMovimientos.SetSource<string>(movimientosStrings);

async Task ActualizarDetalle(int index)
{
    if (index < 0 || index >= productos.Count) return;

    var p = productos[index];

    detalle.Text = $"""
    Id: {p.Id}
    Código: {p.Codigo}
    Nombre: {p.Nombre}
    Precio: ${p.Precio}
    Stock: {p.Stock}
    """;

    using var http = new HttpClient();
    var movimientos = await CargarMovimientosAsync(http, p.Id);

    movimientosStrings.Clear();

    foreach (var m in movimientos)
    {
        movimientosStrings.Add($"{m.Tipo} {m.Cantidad} ({m.Fecha})");
    }
}

ActualizarDetalle(0);

app.AddTimeout(TimeSpan.FromMilliseconds(300), () =>
{
    if (listaProductos.SelectedItem.HasValue)
        _ = ActualizarDetalle(listaProductos.SelectedItem.Value);

    return true;
});

ventana.Add(listaProductos, detalle, listaMovimientos);

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http) {
    const string url = "http://localhost:5050/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? new List<ProductoDto>();
}

static async Task<List<MovimientoDto>> CargarMovimientosAsync(HttpClient http, int productoId)
{
    var url = $"http://localhost:5050/movimientos?productoId={productoId}";
    return await http.GetFromJsonAsync<List<MovimientoDto>>(url)
        ?? new List<MovimientoDto>();
}
// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);