#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


List <ProductoDto> productos;
try
{
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
} catch (HttpRequestException ex) {
    Console.WriteLine($"Error al cargar productos: {ex.Message}");
    Console.WriteLine("Verificá que servidor.cs este corriendo en http://localhost:5050");
    return;
}
using IApplication app = Application.Create().Init();
using Window ventana = new() { Title = " Catalogo REST - Productos (ESC para salir) " };

var titulo = new Label {
    Text = "Productos",
    X = 2,
    Y = 1,
};

var listaProductos = new ListView {
    X = 2,
    Y = 3,
    Width = Dim.Fill(2),
    Height = Dim.Fill(1),
};

listaProductos.SetSource(new ObservableCollection<string>(
    productos.Select(FormatearProducto).ToList()
));

ventana.Add(titulo, listaProductos);

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http) {
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? throw new HttpRequestException("El servidor devolvió una lista vacía");
}

static string FormatearProducto(ProductoDto producto) {
    return $"{producto.Codigo,-8} | {producto.Nombre,-25} | ${producto.Precio,10:N2} | stock {producto.Stock,4}";
}

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);