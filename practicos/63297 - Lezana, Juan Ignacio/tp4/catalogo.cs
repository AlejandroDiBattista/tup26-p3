#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Views;

const string ApiUrl = "http://localhost:5050";

using var http = new HttpClient { BaseAddress = new Uri(ApiUrl) };

List<ProductoDto> productos = [];
List<MovimientoDto> movimientos = [];
ProductoDto? seleccionado = null;

try {
    productos = await CargarProductosAsync();
    seleccionado = productos.FirstOrDefault();
    if (seleccionado is not null) movimientos = await CargarMovimientosAsync(seleccionado.Id);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verifica que servidor.cs este corriendo en http://localhost:5050");
    return;
}
using IApplication app = Application.Create().Init();
using Window ventana = new() { Title = " Catalogo REST - ESC para salir " };

var filtro = new TextField {
    X = 10,
    Y = 1,
    Width = 35,
    Text = ""
};

var listaProductos = new ListView {
    X = 1,
    Y = 3,
    Width = 58,
    Height = 18
};

var listaMovimientos = new ListView {
    X = 61,
    Y = 3,
    Width = 58,
    Height = 18
};

var estado = new Label {
    X = 1,
    Y = 22,
    Width = 118,
    Text = "Seleccione un producto para ver su historial. Use los botones para administrar el catalogo."
};

var btnAgregar = new Button { X = 1, Y = 24, Text = "Agregar" };
var btnEditar = new Button { X = 13, Y = 24, Text = "Editar" };
var btnEliminar = new Button { X = 24, Y = 24, Text = "Eliminar" };
var btnCompra = new Button { X = 38, Y = 24, Text = "Compra" };
var btnVenta = new Button { X = 50, Y = 24, Text = "Venta" };
var btnAjuste = new Button { X = 61, Y = 24, Text = "Ajuste" };
var btnRecargar = new Button { X = 73, Y = 24, Text = "Recargar" };

ventana.Add(
    new Label { X = 1, Y = 1, Text = "Buscar:" },
    filtro,
    new Label { X = 1, Y = 2, Text = "Productos" },
    new Label { X = 61, Y = 2, Text = "Movimientos del producto seleccionado" },
    listaProductos,
    listaMovimientos,
    estado,
    btnAgregar,
    btnEditar,
    btnEliminar,
    btnCompra,
    btnVenta,
    btnAjuste,
    btnRecargar
);

ActualizarListas();

listaProductos.ValueChanged += async (_, _) => await SeleccionarProductoAsync();
filtro.TextChanged += (_, _) => ActualizarListas();
btnAgregar.Accepted += async (_, _) => await AbrirDialogoProductoAsync(null);
btnEditar.Accepted += async (_, _) => { if (seleccionado is not null) await AbrirDialogoProductoAsync(seleccionado); };
btnEliminar.Accepted += async (_, _) => await EliminarSeleccionadoAsync();
btnCompra.Accepted += async (_, _) => await AbrirDialogoMovimientoAsync(TipoMovimiento.Compra);
btnVenta.Accepted += async (_, _) => await AbrirDialogoMovimientoAsync(TipoMovimiento.Venta);
btnAjuste.Accepted += async (_, _) => await AbrirDialogoMovimientoAsync(TipoMovimiento.Ajuste);
btnRecargar.Accepted += async (_, _) => await RecargarAsync();

app.Run(ventana);

async Task<List<ProductoDto>> CargarProductosAsync() =>
    await http.GetFromJsonAsync<List<ProductoDto>>("/productos") ?? [];

async Task<List<MovimientoDto>> CargarMovimientosAsync(int productoId) =>
    await http.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoId}/movimientos") ?? [];

void ActualizarListas() {
    var texto = filtro.Text?.ToString() ?? "";
    var filtrados = productos
        .Where(p => string.IsNullOrWhiteSpace(texto)
            || p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.Codigo)
        .ToList();

    if (seleccionado is not null && !filtrados.Any(p => p.Id == seleccionado.Id)) {
        seleccionado = filtrados.FirstOrDefault();
    } else if (seleccionado is null) {
        seleccionado = filtrados.FirstOrDefault();
    }

    listaProductos.SetSource<string>(
    new ObservableCollection<string>(
        filtrados.Select(FormatearProducto)
    )
);

if (filtrados.Count > 0)
{
    var indice = seleccionado is null
        ? 0
        : filtrados.FindIndex(p => p.Id == seleccionado.Id);

    if (indice >= 0)
        listaProductos.SelectedItem = indice;
}
    listaMovimientos.SetSource<string>(new ObservableCollection<string>(movimientos.Select(FormatearMovimiento)));
}


async Task SeleccionarProductoAsync() {
    var texto = filtro.Text?.ToString() ?? "";
    var filtrados = productos
        .Where(p => string.IsNullOrWhiteSpace(texto)
            || p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.Codigo)
        .ToList();

    var indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count) return;

    seleccionado = filtrados[indice];
    movimientos = await CargarMovimientosAsync(seleccionado.Id);
    ActualizarListas();
}

async Task RecargarAsync()
{
    var idActual = seleccionado?.Id;

    productos = await CargarProductosAsync();

    seleccionado = productos
        .FirstOrDefault(p => p.Id == idActual)
        ?? productos.FirstOrDefault();

    movimientos = seleccionado is null
        ? []
        : await CargarMovimientosAsync(seleccionado.Id);

    ActualizarListas();

    estado.Text =
        seleccionado is null
        ? "No hay productos."
        : $"Producto: {seleccionado.Codigo} - Stock: {seleccionado.Stock}";

}

async Task AbrirDialogoProductoAsync(ProductoDto? producto) {
    var entrada = PedirProducto(producto);
    if (entrada is null) return;

    var respuesta = producto is null
        ? await http.PostAsJsonAsync("/productos", entrada)
        : await http.PutAsJsonAsync($"/productos/{producto.Id}", entrada);

    if (!respuesta.IsSuccessStatusCode) {
        await MostrarErrorAsync(respuesta);
    }

    await RecargarAsync();
}

async Task EliminarSeleccionadoAsync() {
    if (seleccionado is null) return;

    var opcion = MessageBox.Query(
        app,
        "Eliminar producto",
        $"Eliminar {seleccionado.Codigo} - {seleccionado.Nombre}?",
        "No",
        "Si"
    );
    if (opcion != 1) return;

    var respuesta = await http.DeleteAsync($"/productos/{seleccionado.Id}");
    if (!respuesta.IsSuccessStatusCode) await MostrarErrorAsync(respuesta);
    seleccionado = null;
    await RecargarAsync();
}

async Task AbrirDialogoMovimientoAsync(TipoMovimiento tipo)
{
    if (seleccionado is null)
        return;

    var cantidad = PedirCantidad(tipo, seleccionado);

    if (cantidad is null)
        return;

   var respuesta = await http.PostAsJsonAsync(
    $"/productos/{seleccionado.Id}/movimientos",
    new MovimientoEntrada(tipo, cantidad.Value)
);

    if (!respuesta.IsSuccessStatusCode)
    {
        await MostrarErrorAsync(respuesta);
        return;
    }

    productos = await CargarProductosAsync();

    seleccionado =
        productos.FirstOrDefault(
            p => p.Id == seleccionado.Id
        );

    movimientos =
        await CargarMovimientosAsync(
            seleccionado!.Id
        );

    ActualizarListas();
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
