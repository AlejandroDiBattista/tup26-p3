#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;

try
{
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();

using Window ventana = new()
{
    Title = " Catalogo REST — Producto (ESC para salir) "
};

var items = new ObservableCollection<string>(
    productos.Select(p => $"{p.Codigo} - {p.Nombre}")
);

var listaProductos = new ListView()
{
    X = 1,
    Y = 1,
    Width = 40,
    Height = Dim.Fill()
};

listaProductos.SetSource(items);

var detalleProducto = new Label()
{
    Text = """
           DETALLE PRODUCTO

           Seleccione un producto
           """,
    X = 45,
    Y = 2,
    Width = 40,
    Height = 10
};

listaProductos.ValueChanged += (_, _) =>
{
    int indice = listaProductos.SelectedItem ?? 0;

    if (indice < 0 || indice >= productos.Count)
        return;

    var producto = productos[indice];

    detalleProducto.Text =
    $"""
    Id: {producto.Id}

    Código: {producto.Codigo}

    Nombre: {producto.Nombre}

    Precio: ${producto.Precio}

    Stock: {producto.Stock}
    """;
};

ventana.Add(detalleProducto);
ventana.Add(listaProductos);

if (productos.Count > 0)
{
    detalleProducto.Text =
    $"""
    Id: {productos[0].Id}

    Código: {productos[0].Codigo}

    Nombre: {productos[0].Nombre}

    Precio: ${productos[0].Precio}

    Stock: {productos[0].Stock}
    """;
}

app.Run(ventana);

// ── API ───────────────────────────────────────────────────────────────────

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
           ?? new List<ProductoDto>();
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);