#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

var api = new CatalogoApi();

List<ProductoDto> productos;
try {
    productos = await api.ListarProductosAsync();
} catch (HttpRequestException ex) {
    Console.WriteLine($"Error al conectar con el servidor: {ex.Message}");
    return;
}
// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
var ventana = new CatalogoWindow(productos);
app.Run(ventana);
sealed class CatalogoWindow : Window
{
    public CatalogoWindow(List<ProductoDto> productos)
    {
        Title = $" Catalogo REST — {productos.Count} productos cargados ";

        var menu = new Label
{
    X = 1,
    Y = 0,
    Text = "[A]gregar  [M]odificar  [E]liminar"
};

Add(menu);

        var listaProductos = new ListView
        {
            X = 1,
            Y = 2,
        };
        var detalle = new Label
{
    X = 50,
    Y = 15,
    Text = "Seleccione un producto"
};
        var agregar = new Button    
{
    X = 1,
    Y = productos.Count + 5,
    Text = "Agregar"
};
agregar.Accepting += (_, _) =>
{
    var producto = ProductoDialog.Mostrar();

    if (producto is not null)
    {
        Console.WriteLine($"Producto: {producto.Nombre}");
    }
};

var modificar = new Button
{
    X = 15,
    Y = productos.Count + 5,
    Text = "Modificar"
};

var eliminar = new Button
{
    X = 30,
    Y = productos.Count + 5,
    Text = "Eliminar"
};

        listaProductos.SetSource(
            new ObservableCollection<string>(
                productos.Select(p =>
                    $"{p.Codigo,-10} {p.Nombre,-25} ${p.Precio,10:N2} Stock:{p.Stock}")
                .ToList()));

        var informacion = new Label
        {
            X = 1,
            Y = productos.Count + 3,
            Text = $"Productos encontrados: {productos.Count}"
        };

        var historial = new ListView
{
    X = 50,
    Y = 2
};

historial.SetSource(
    new ObservableCollection<string>(
        new List<string>
        {
            "Historial pendiente"
        }));
        listaProductos.Accepting += (_, _) =>
{
    if (listaProductos.SelectedItem is not int indice)
        return;

    if (indice < 0 || indice >= productos.Count)
        return;

    var p = productos[indice];

    detalle.Text =
        $"Código: {p.Codigo}\n" +
        $"Nombre: {p.Nombre}\n" +
        $"Precio: ${p.Precio:N2}\n" +
        $"Stock: {p.Stock}";
};

        


        Add(listaProductos);
        Add(historial);
        Add(informacion);
        Add(agregar);
        Add(modificar);
        Add(eliminar);
    }
    
}
static class ProductoDialog
{
    public static ProductoRequest? Mostrar()
    {
        return new ProductoRequest(
            "P000",
            "Nuevo Producto",
            1000,
            10);
    }

}
sealed class CatalogoApi
{
    private readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://localhost:5050")
    };

    public async Task<List<ProductoDto>> ListarProductosAsync()
    {
        return await http.GetFromJsonAsync<List<ProductoDto>>("/productos")
            ?? [];
    }
    public async Task CrearProductoAsync(ProductoDto producto)
{
    await http.PostAsJsonAsync("/productos", producto);
}

public async Task ModificarProductoAsync(int id, ProductoDto producto)
{
    await http.PutAsJsonAsync($"/productos/{id}", producto);
}

public async Task EliminarProductoAsync(int id)
{
    await http.DeleteAsync($"/productos/{id}");
}
public async Task<List<MovimientoDto>> ListarMovimientosAsync(int productoId)
{
    return await http.GetFromJsonAsync<List<MovimientoDto>>(
        $"/productos/{productoId}/movimientos")
        ?? [];
}
}
// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
sealed record ProductoRequest(
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock);

        enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

record MovimientoDto(
    int ProductoId,
    TipoMovimiento Tipo,
    int Cantidad,
    DateTime Fecha);
