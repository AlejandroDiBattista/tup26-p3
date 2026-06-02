#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using HttpClient http = new();

List<ProductoDto> productos;
List<ProductoDto> filtrados = [];

try {
    productos = await CargarProductosAsync(http);
} catch (HttpRequestException ex) {
    Console.WriteLine($"Error al cargar productos: {ex.Message}");
    Console.WriteLine("Verifica que servidor.cs este corriendo en http://localhost:5050");
    return;
}

using IApplication app = Application.Create().Init();
using Window ventana = new() { Title = " Catalogo REST - Productos (ESC para salir) " };

var etiquetaBuscar = new Label {
    Text = "Buscar:",
    X = 2,
    Y = 1,
};

var buscar = new TextField {
    X = 10,
    Y = 1,
    Width = 40,
};

var listaProductos = new ListView {
    X = 2,
    Y = 3,
    Width = 60,
    Height = Dim.Fill(1),
};

var listaMovimientos = new ListView {
    X = Pos.Right(listaProductos) + 1,
    Y = 3,
    Width = Dim.Fill(2),
    Height = Dim.Fill(1),
};

void ActualizarProductos() {
    string texto = buscar.Text?.ToString() ?? "";

    filtrados = productos
        .Where(producto =>
            producto.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || producto.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .ToList();

    listaProductos.SetSource(new ObservableCollection<string>(
        filtrados.Select(FormatearProducto).ToList()
    ));
}

async Task CargarMovimientosSeleccionado() {
    int indice = listaProductos.SelectedItem ?? -1;

    if (indice < 0 || indice >= filtrados.Count) {
        listaMovimientos.SetSource(new ObservableCollection<string>());
        return;
    }

    ProductoDto seleccionado = filtrados[indice];
    var movimientos = await CargarMovimientosAsync(http, seleccionado.Id);

    listaMovimientos.SetSource(new ObservableCollection<string>(
        movimientos.Select(FormatearMovimiento).ToList()
    ));
}

buscar.TextChanged += (_, _) => {
    ActualizarProductos();
    _ = CargarMovimientosSeleccionado();
};

listaProductos.ValueChanged += async (_, _) => await CargarMovimientosSeleccionado();

ActualizarProductos();
await CargarMovimientosSeleccionado();

ventana.Add(etiquetaBuscar, buscar, listaProductos, listaMovimientos);

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http) {
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? throw new HttpRequestException("El servidor devolvio una lista vacia");
}

static async Task<List<MovimientoDto>> CargarMovimientosAsync(HttpClient http, int productoId) {
    string url = $"http://localhost:5050/productos/{productoId}/movimientos";

    return await http.GetFromJsonAsync<List<MovimientoDto>>(url) ?? [];
}

static string FormatearProducto(ProductoDto producto) {
    return $"{producto.Codigo,-8} | {producto.Nombre,-25} | ${producto.Precio,10:N2} | stock {producto.Stock,4}";
}

static string FormatearMovimiento(MovimientoDto movimiento) {
    return $"{movimiento.Tipo,-8} | {movimiento.Cantidad,4} | {movimiento.Fecha:dd/MM/yyyy HH:mm}";
}

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, DateTime Fecha, int Cantidad, string Tipo);
