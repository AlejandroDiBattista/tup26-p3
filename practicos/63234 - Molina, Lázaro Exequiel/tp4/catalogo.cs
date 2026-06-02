#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string UrlApi = "http://localhost:5050";

var opcionesJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);
opcionesJson.Converters.Add(new JsonStringEnumConverter());

using var cliente = new HttpClient { BaseAddress = new Uri(UrlApi) };

var productos = new List<ProductoDto>();
var productosMostrados = new List<ProductoDto>();
var filasProductos = new ObservableCollection<string>();
var filasMovimientos = new ObservableCollection<string>();
ProductoDto? elegido = null;

using IApplication app = Application.Create();
app.Init();

using Window pantalla = new() { Title = " Gestor de catalogo - ESC para salir " };

var textoBuscar = new TextField { X = 10, Y = 1, Width = 48, Text = "" };
var listado = new ListView { X = 1, Y = 3, Width = 60, Height = 14 };
listado.SetSource(filasProductos);

var historial = new ListView { X = 64, Y = 3, Width = 54, Height = 14 };
historial.SetSource(filasMovimientos);

var txtCodigo = new TextField { X = 9, Y = 20, Width = 18, Text = "" };
var txtNombre = new TextField { X = 37, Y = 20, Width = 35, Text = "" };
var txtPrecio = new TextField { X = 9, Y = 22, Width = 18, Text = "0" };
var txtStock = new TextField { X = 37, Y = 22, Width = 12, Text = "0" };
var txtCantidad = new TextField { X = 78, Y = 22, Width = 10, Text = "1" };
var mensaje = new Label { X = 1, Y = 26, Width = 118, Text = "Conectando con el servidor..." };
var btnLimpiar = new Button { X = 1, Y = 24, Text = "Nuevo" };
var btnGuardar = new Button { X = 12, Y = 24, Text = "Guardar" };
var btnBorrar = new Button { X = 25, Y = 24, Text = "Borrar" };
var btnCompra = new Button { X = 64, Y = 24, Text = "Compra" };
var btnVenta = new Button { X = 76, Y = 24, Text = "Venta" };
var btnAjuste = new Button { X = 87, Y = 24, Text = "Ajuste" };

pantalla.Add(
    new Label { Text = "Productos", X = 1, Y = 0 },
    new Label { Text = "Filtrar:", X = 1, Y = 1 }, textoBuscar,
    listado,
    new Label { Text = "Movimientos de stock", X = 64, Y = 0 },
    historial,
    new Label { Text = "Codigo:", X = 1, Y = 20 }, txtCodigo,
    new Label { Text = "Nombre:", X = 29, Y = 20 }, txtNombre,
    new Label { Text = "Precio:", X = 1, Y = 22 }, txtPrecio,
    new Label { Text = "Stock:", X = 29, Y = 22 }, txtStock,
    new Label { Text = "Cantidad:", X = 64, Y = 22 }, txtCantidad,
    btnLimpiar, btnGuardar, btnBorrar, btnCompra, btnVenta, btnAjuste,
    mensaje
);

textoBuscar.TextChanged += (_, _) => AplicarFiltro();
listado.ValueChanged += async (_, _) => await TomarSeleccionAsync();

btnLimpiar.Accepting += (_, e) => {
    e.Handled = true;
    VaciarFormulario();
};

btnGuardar.Accepting += async (_, e) => {
    e.Handled = true;
    await GuardarAsync();
};

btnBorrar.Accepting += async (_, e) => {
    e.Handled = true;
    await BorrarAsync();
};

btnCompra.Accepting += async (_, e) => {
    e.Handled = true;
    await AgregarMovimientoAsync(TipoMovimiento.Compra);
};

btnVenta.Accepting += async (_, e) => {
    e.Handled = true;
    await AgregarMovimientoAsync(TipoMovimiento.Venta);
};

btnAjuste.Accepting += async (_, e) => {
    e.Handled = true;
    await AgregarMovimientoAsync(TipoMovimiento.Ajuste);
};

try {
    await RefrescarProductosAsync();
} catch (Exception ex) {
    mensaje.Text = $"No pude conectar con {UrlApi}. Ejecuta primero servidor.cs. {ex.Message}";
}

app.Run(pantalla);

async Task RefrescarProductosAsync() {
    productos = await cliente.GetFromJsonAsync<List<ProductoDto>>("/productos", opcionesJson) ?? [];
    AplicarFiltro();
    mensaje.Text = $"Productos encontrados: {productos.Count}";

    if (productosMostrados.Count > 0) {
        listado.SelectedItem = 0;
        await TomarSeleccionAsync();
    } else {
        VaciarFormulario();
    }
}

void AplicarFiltro() {
    var filtro = textoBuscar.Text?.ToString()?.Trim() ?? "";

    productosMostrados = productos
        .Where(p => filtro.Length == 0
            || p.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
            || p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.Codigo)
        .ToList();

    filasProductos.Clear();
    foreach (var p in productosMostrados) {
        filasProductos.Add($"{p.Codigo,-8} {Cortar(p.Nombre, 26),-26} ${p.Precio,8:N2}  St:{p.Stock,4}");
    }

    if (productosMostrados.Count == 0) {
        elegido = null;
        filasMovimientos.Clear();
        mensaje.Text = "No hay productos para mostrar.";
    } else if (listado.SelectedItem is null || listado.SelectedItem >= productosMostrados.Count) {
        listado.SelectedItem = 0;
    }

    pantalla.SetNeedsDraw();
}

async Task TomarSeleccionAsync() {
    var indice = listado.SelectedItem;
    if (indice is null || indice < 0 || indice >= productosMostrados.Count) return;

    elegido = productosMostrados[indice.Value];
    txtCodigo.Text = elegido.Codigo;
    txtNombre.Text = elegido.Nombre;
    txtPrecio.Text = elegido.Precio.ToString(CultureInfo.InvariantCulture);
    txtStock.Text = elegido.Stock.ToString(CultureInfo.InvariantCulture);

    await RefrescarMovimientosAsync(elegido.Id);
}

async Task RefrescarMovimientosAsync(int productoId) {
    filasMovimientos.Clear();
    var movimientos = await cliente.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoId}/movimientos", opcionesJson) ?? [];

    foreach (var mov in movimientos) {
        filasMovimientos.Add($"{mov.Tipo,-7}  {mov.Cantidad,5} u.  {mov.Fecha:dd/MM HH:mm}");
    }

    if (movimientos.Count == 0) filasMovimientos.Add("Sin movimientos.");
    pantalla.SetNeedsDraw();
}
