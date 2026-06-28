#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Views;

const string apiUrl = "http://localhost:5050";

using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

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

ProductoEntrada? PedirProducto(ProductoDto? producto) {
    using var dialogo = new Dialog {
        Title = producto is null ? " Agregar producto " : " Editar producto ",
        Width = 62,
        Height = 15
    };

    var codigo = new TextField { X = 12, Y = 1, Width = 42, Text = producto?.Codigo ?? "" };
    var nombre = new TextField { X = 12, Y = 3, Width = 42, Text = producto?.Nombre ?? "" };
    var precio = new TextField { X = 12, Y = 5, Width = 16, Text = (producto?.Precio ?? 0m).ToString() };
    var stock = new TextField { X = 12, Y = 7, Width = 16, Text = (producto?.Stock ?? 0).ToString() };
    var guardar = new Button { Text = "Guardar", IsDefault = true };
    var cancelar = new Button { Text = "Cancelar" };
    var aceptado = false;

    guardar.Accepted += (_, _) => { aceptado = true; dialogo.RequestStop(); };
    cancelar.Accepted += (_, _) => dialogo.RequestStop();

    dialogo.Add(
        new Label { X = 2, Y = 1, Text = "Codigo:" },
        codigo,
        new Label { X = 2, Y = 3, Text = "Nombre:" },
        nombre,
        new Label { X = 2, Y = 5, Text = "Precio:" },
        precio,
        new Label { X = 2, Y = 7, Text = "Stock:" },
        stock
    );
    dialogo.AddButton(cancelar);
    dialogo.AddButton(guardar);

    app.Run(dialogo);
    if (!aceptado) return null;

    if (!decimal.TryParse(precio.Text?.ToString(), out var precioNumero)
        || !int.TryParse(stock.Text?.ToString(), out var stockNumero)
        || string.IsNullOrWhiteSpace(codigo.Text?.ToString())
        || string.IsNullOrWhiteSpace(nombre.Text?.ToString())
        || precioNumero < 0
        || stockNumero < 0) {
        MessageBox.Query(app, "Datos invalidos", "Complete codigo, nombre, precio y stock con valores validos.", "OK");
        return null;
    }

    return new ProductoEntrada(
        codigo.Text.ToString()!.Trim(),
        nombre.Text.ToString()!.Trim(),
        precioNumero,
        stockNumero
    );
}

int? PedirCantidad(TipoMovimiento tipo, ProductoDto producto) {
    using var dialogo = new Dialog {
        Title = $" {tipo} ",
        Width = 62,
        Height = 10
    };

    var ayuda = tipo == TipoMovimiento.Ajuste
        ? "En ajuste, la cantidad es el nuevo stock final."
        : "La cantidad debe ser positiva.";
    var cantidad = new TextField { X = 12, Y = 4, Width = 16, Text = "1" };
    var guardar = new Button { Text = "Registrar", IsDefault = true };
    var cancelar = new Button { Text = "Cancelar" };
    var aceptado = false;

    guardar.Accepted += (_, _) => { aceptado = true; dialogo.RequestStop(); };
    cancelar.Accepted += (_, _) => dialogo.RequestStop();

    dialogo.Add(
        new Label { X = 2, Y = 1, Text = $"{producto.Codigo} - {producto.Nombre}" },
        new Label { X = 2, Y = 2, Text = ayuda },
        new Label { X = 2, Y = 4, Text = "Cantidad:" },
        cantidad
    );
    dialogo.AddButton(cancelar);
    dialogo.AddButton(guardar);

    app.Run(dialogo);
    if (!aceptado) return null;

    if (!int.TryParse(cantidad.Text?.ToString(), out var numero) || numero <= 0) {
        MessageBox.Query(app, "Datos invalidos", "La cantidad debe ser un entero positivo.", "OK");
        return null;
    }

    return numero;
}

async Task MostrarErrorAsync(HttpResponseMessage respuesta) {
    MessageBox.Query(app, "Error", $"Error {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}", "OK");
}

static string FormatearProducto(ProductoDto p) =>
    $"{p.Codigo,-8} {Recortar(p.Nombre,24),-24} ${p.Precio,10:N2} Stock:{p.Stock}";

static string FormatearMovimiento(MovimientoDto m) =>
    $"{m.Tipo,-7} {m.Cantidad,6}  {m.Fecha:dd/MM/yyyy HH:mm}";

static string Recortar(string texto, int largo) =>
    texto.Length <= largo ? texto : texto[..(largo - 1)] + ".";

enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);
