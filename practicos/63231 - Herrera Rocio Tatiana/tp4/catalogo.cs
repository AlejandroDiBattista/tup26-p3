#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string ApiBase = "http://localhost:5050";
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
json.Converters.Add(new JsonStringEnumConverter());

using var http = new HttpClient { BaseAddress = new Uri(ApiBase) };
List<ProductoDto> productos = [];
List<ProductoDto> productosFiltrados = [];
var productosVista = new ObservableCollection<string>();
var movimientosVista = new ObservableCollection<string>();
ProductoDto? productoSeleccionado = null;
using IApplication app = Application.Create();
app.Init();

using Window ventana = new() { Title = " Catalogo REST - ESC para salir " };

var tituloProductos = new Label { Text = "Productos", X = 1, Y = 0 };
var tituloMovimientos = new Label { Text = "Movimientos del producto", X = 66, Y = 0 };

var buscar = new TextField { X = 9, Y = 1, Width = 52, Text = "" };
var listaProductos = new ListView { X = 1, Y = 3, Width = 62, Height = 13 };
listaProductos.SetSource(productosVista);

var listaMovimientos = new ListView { X = 66, Y = 2, Width = 56, Height = 14 };
listaMovimientos.SetSource(movimientosVista);
var codigo = new TextField { X = 9, Y = 18, Width = 20, Text = "" };
var nombre = new TextField { X = 39, Y = 18, Width = 35, Text = "" };
var precio = new TextField { X = 9, Y = 20, Width = 20, Text = "0" };
var stock = new TextField { X = 39, Y = 20, Width = 15, Text = "0" };
var cantidad = new TextField { X = 79, Y = 20, Width = 12, Text = "1" };
var estado = new Label { X = 1, Y = 24, Width = 120, Text = "Iniciando..." };
var btnNuevo = new Button { X = 1, Y = 22, Text = "Nuevo" };
var btnGuardar = new Button { X = 12, Y = 22, Text = "Guardar" };
var btnEliminar = new Button { X = 25, Y = 22, Text = "Eliminar" };
var btnCompra = new Button { X = 66, Y = 22, Text = "Compra" };
var btnVenta = new Button { X = 78, Y = 22, Text = "Venta" };
var btnAjuste = new Button { X = 89, Y = 22, Text = "Ajuste" };

ventana.Add(
    tituloProductos,
    new Label { Text = "Buscar:", X = 1, Y = 1 }, buscar,
    listaProductos,
    tituloMovimientos,
    listaMovimientos,
    new Label { Text = "Codigo:", X = 1, Y = 18 }, codigo,
    new Label { Text = "Nombre:", X = 31, Y = 18 }, nombre,
    new Label { Text = "Precio:", X = 1, Y = 20 }, precio,
    new Label { Text = "Stock:", X = 31, Y = 20 }, stock,
    new Label { Text = "Cantidad:", X = 66, Y = 20 }, cantidad,
    btnNuevo, btnGuardar, btnEliminar, btnCompra, btnVenta, btnAjuste,
    new Label { Text = "Atajos: buscar por codigo/nombre, seleccionar con flechas, usar botones con Enter.", X = 1, Y = 26 },
    estado
);
buscar.TextChanged += (_, _) => FiltrarProductos();
listaProductos.ValueChanged += async (_, _) => await SeleccionarProductoAsync();

btnNuevo.Accepting += (_, e) => {
    e.Handled = true;
    LimpiarFormulario();
};

btnGuardar.Accepting += async (_, e) => {
    e.Handled = true;
    await GuardarProductoAsync();
};

btnEliminar.Accepting += async (_, e) => {
    e.Handled = true;
    await EliminarProductoAsync();
};

btnCompra.Accepting += async (_, e) => {
    e.Handled = true;
    await RegistrarMovimientoAsync(TipoMovimiento.Compra);
};

btnVenta.Accepting += async (_, e) => {
    e.Handled = true;
    await RegistrarMovimientoAsync(TipoMovimiento.Venta);
};

btnAjuste.Accepting += async (_, e) => {
    e.Handled = true;
    await RegistrarMovimientoAsync(TipoMovimiento.Ajuste);
};
try {
    await CargarProductosAsync();
} catch (Exception ex) {
    estado.Text = "No se pudo conectar con servidor.cs en http://localhost:5050: " + ex.Message;
}

app.Run(ventana);

async Task CargarProductosAsync() {
    productos = await http.GetFromJsonAsync<List<ProductoDto>>("/productos", json) ?? [];
    FiltrarProductos();
    estado.Text = $"Productos cargados: {productos.Count}";

    if (productosFiltrados.Count > 0) {
        listaProductos.SelectedItem = 0;
        await SeleccionarProductoAsync();
    } else {
        LimpiarFormulario();
    }
}

void FiltrarProductos() {
    var texto = buscar.Text?.ToString()?.Trim() ?? "";

    productosFiltrados = productos
        .Where(p => texto.Length == 0
            || p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.Codigo)
        .ToList();

    productosVista.Clear();
    foreach (var p in productosFiltrados) {
        productosVista.Add($"{p.Codigo,-8} {Recortar(p.Nombre, 28),-28} ${p.Precio,9:N2} Stock:{p.Stock,4}");
    }

    if (productosFiltrados.Count == 0) {
        movimientosVista.Clear();
        productoSeleccionado = null;
        estado.Text = "No hay productos para mostrar.";
    } else if (listaProductos.SelectedItem >= productosFiltrados.Count) {
        listaProductos.SelectedItem = 0;
    }

    ventana.SetNeedsDraw();
}

async Task SeleccionarProductoAsync() {
    var indiceSeleccionado = listaProductos.SelectedItem;
    if (indiceSeleccionado is null || indiceSeleccionado < 0 || indiceSeleccionado >= productosFiltrados.Count) return;

    productoSeleccionado = productosFiltrados[indiceSeleccionado.Value];
    codigo.Text = productoSeleccionado.Codigo;
    nombre.Text = productoSeleccionado.Nombre;
    precio.Text = productoSeleccionado.Precio.ToString(CultureInfo.InvariantCulture);
    stock.Text = productoSeleccionado.Stock.ToString(CultureInfo.InvariantCulture);

    await CargarMovimientosAsync(productoSeleccionado.Id);
}

async Task CargarMovimientosAsync(int productoId) {
    movimientosVista.Clear();
    var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoId}/movimientos", json) ?? [];

    foreach (var m in movimientos) {
        movimientosVista.Add($"{m.Tipo,-7} Cant:{m.Cantidad,5}  {m.Fecha:dd/MM/yyyy HH:mm}");
    }

    if (movimientos.Count == 0) movimientosVista.Add("Sin movimientos registrados.");
    ventana.SetNeedsDraw();
}

async Task GuardarProductoAsync() { ... }

async Task EliminarProductoAsync() { ... }

async Task RegistrarMovimientoAsync(TipoMovimiento tipo) { ... }

void LimpiarFormulario() { ... }

static string Recortar(string texto, int largo) { ... }

static async Task<string> LeerErrorAsync(HttpResponseMessage respuesta) { ... }

record ProductoCrearDto(string Codigo, string Nombre, decimal Precio, int Stock);
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoCrearDto(TipoMovimiento Tipo, int Cantidad);
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);

enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}