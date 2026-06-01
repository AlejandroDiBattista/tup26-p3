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

        var listaProductos = new ListView
        {
            X = 1,
            Y = 1,
        };
        var agregar = new Button
{
    X = 1,
    Y = productos.Count + 5,
    Text = "Agregar"
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

        Add(listaProductos);
        Add(informacion);
        Add(agregar);
        Add(modificar);
        Add(eliminar);
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
}
// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
