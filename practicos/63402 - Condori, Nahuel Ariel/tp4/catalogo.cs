#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

List<ProductoDto> productos;
List<ProductoDto> filtrados = [];

try {
    using var http = new HttpClient();
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

buscar.TextChanged += (_, _) => ActualizarProductos();

ActualizarProductos();

ventana.Add(etiquetaBuscar, buscar, listaProductos);

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http) {
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? throw new HttpRequestException("El servidor devolvio una lista vacia");
}

static string FormatearProducto(ProductoDto producto) {
    return $"{producto.Codigo,-8} | {producto.Nombre,-25} | ${producto.Precio,10:N2} | stock {producto.Stock,4}";
}

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
