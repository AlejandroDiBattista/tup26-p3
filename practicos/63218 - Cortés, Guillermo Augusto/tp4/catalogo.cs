#!/usr/bin/env -S dotnet run

#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;
try {
    using HttpClient http = new();

    productos = await CargarProductosAsync(http);

    if (productos.Count == 0) {
        Console.WriteLine("No hay productos.");
        return;
    }
}
catch (Exception ex) {
    Console.Error.WriteLine($"Error conectando con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté ejecutándose en http://localhost:5050");
    return;
}
// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();

Window ventana = new() {
    Title = " Catálogo REST - Productos "
};

// Panel izquierdo
FrameView panelProductos = new() {
    Title = "Productos",
    X = 0,
    Y = 0,
    Width = Dim.Percent(45),
    Height = Dim.Fill()
};

// Panel derecho
FrameView panelDetalle = new() {
    Title = "Movimientos",
    X = Pos.Right(panelProductos),
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

ListView listaProductos = new() {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

TextView detalle = new() {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ReadOnly = true
};

panelProductos.Add(listaProductos);
panelDetalle.Add(detalle);

ventana.Add(panelProductos);
ventana.Add(panelDetalle);

var filas = productos
    .Select(p =>
        $"{p.Codigo,-10} {p.Nombre,-20} ${p.Precio,8:N2} Stock:{p.Stock,4}")
    .ToList();

listaProductos.SetSource(
    new ObservableCollection<string>(filas)
);
async Task MostrarMovimientosAsync(int indice)
{
    if (indice < 0 || indice >= productos.Count)
        return;

    ProductoDto producto = productos[indice];

    try {
        using HttpClient http = new();

        List<MovimientoDto> movimientos =
            await CargarMovimientosAsync(http, producto.Id);

        detalle.Text =
$"""
Producto

Código:    {producto.Codigo}
Nombre:    {producto.Nombre}
Precio:    ${producto.Precio:N2}
Stock:     {producto.Stock}

Movimientos
────────────────────────────

{string.Join("\n",
movimientos.Select(m =>
$"{m.Fecha:g} | {m.Tipo,-8} | {m.Cantidad,4}"))}
""";
    }
    catch (Exception ex) {
        detalle.Text = $"Error cargando movimientos:\n\n{ex.Message}";
    }
}

listaProductos.ValueChanged += (_, _) =>
{
    int indice = listaProductos.SelectedItem ?? 0;

    _ = MostrarMovimientosAsync(indice);
};
await MostrarMovimientosAsync(0);

app.Run(ventana);
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

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
