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

FrameView panelProductos = new() {
    Title = "Productos",
    X = 0,
    Y = 1,
    Width = Dim.Percent(45),
    Height = Dim.Fill()
};

Label lblBuscar = new() {
    Text = "Buscar:",
    X = 0,
    Y = 0
};

TextField txtBuscar = new() {
    X = Pos.Right(lblBuscar) + 1,
    Y = 0,
    Width = Dim.Fill()
};

FrameView panelDetalle = new() {
    Title = "Movimientos",
    X = Pos.Right(panelProductos),
    Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

ListView listaProductos = new() {
    X = 0,
    Y = 1,
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

panelProductos.Add(lblBuscar);
panelProductos.Add(txtBuscar);
panelProductos.Add(listaProductos);
panelDetalle.Add(detalle);


List<ProductoDto> productosFiltrados = productos.ToList();
async Task AgregarProductoAsync()
{
    Dialog dialog = new()
    {
        Title = "Nuevo producto",
        Width = 60,
        Height = 15
    };

    TextField txtCodigo = new() { X = 15, Y = 1, Width = 25 };
    TextField txtNombre = new() { X = 15, Y = 3, Width = 25 };
    TextField txtPrecio = new() { X = 15, Y = 5, Width = 25 };
    TextField txtStock = new() { X = 15, Y = 7, Width = 25 };

    dialog.Add(
        new Label() { Text = "Código:", X = 1, Y = 1 },
        txtCodigo,
        new Label() { Text = "Nombre:", X = 1, Y = 3 },
        txtNombre,
        new Label() { Text = "Precio:", X = 1, Y = 5 },
        txtPrecio,
        new Label() { Text = "Stock:", X = 1, Y = 7 },
        txtStock
    );

    Button guardar = new() { Text = "Guardar" };
    Button cancelar = new() { Text = "Cancelar" };

    dialog.AddButton(guardar);
    dialog.AddButton(cancelar);

    cancelar.Accepting += (_, _) =>
    {
        dialog.RequestStop();
    };

    guardar.Accepting += async (_, _) =>
    {
        try
        {
            using HttpClient http = new();

            var nuevo = new
            {
                Codigo = txtCodigo.Text.ToString(),
                Nombre = txtNombre.Text.ToString(),
                Precio = decimal.Parse(txtPrecio.Text.ToString()!),
                Stock = int.Parse(txtStock.Text.ToString()!)
            };

            await http.PostAsJsonAsync(
                "http://localhost:5050/productos",
                nuevo);

            productos = await CargarProductosAsync(http);

            ActualizarLista();

            dialog.RequestStop();
        }
        catch (Exception ex)
        {
            detalle.Text = ex.Message;
        }
    };

    app.Run(dialog);
}

MenuBar menu = new(
[
    new MenuBarItem("_Productos",
    [
        new MenuItem("_Agregar", "", () => _ = AgregarProductoAsync()),
        new MenuItem("_Modificar", "", () => { }),
        new MenuItem("_Eliminar", "", () => { })
    ]),
    new MenuBarItem("_Movimientos",
    [
        new MenuItem("_Registrar", "", () => { })
    ]),
    new MenuBarItem("_Archivo",
    [
        new MenuItem("_Salir", "", () => app.RequestStop())
    ])
]);

ventana.Add(menu);
ventana.Add(panelProductos);
ventana.Add(panelDetalle);

void ActualizarLista()
{
    string texto = txtBuscar.Text?.ToString() ?? "";

    productosFiltrados = productos
        .Where(p =>
            p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
            ||
            p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .ToList();

    var filas = productosFiltrados
        .Select(p =>
            $"{p.Codigo,-10} {p.Nombre,-20} ${p.Precio,8:N2} Stock:{p.Stock,4}")
        .ToList();

    listaProductos.SetSource(
        new ObservableCollection<string>(filas)
    );
}
async Task MostrarMovimientosAsync(int indice)
{
    if (indice < 0 || indice >= productos.Count)
        return;

    ProductoDto producto = productosFiltrados[indice];

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
txtBuscar.TextChanged += (_, _) =>
{
    ActualizarLista();
};
ActualizarLista();
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
record MovimientoDto(int Id, int ProductoId, int Tipo, int Cantidad, DateTime Fecha);
