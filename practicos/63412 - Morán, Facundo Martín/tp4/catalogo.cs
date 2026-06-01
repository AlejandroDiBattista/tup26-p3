#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;
try {
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}
// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — {productos.Count} productos guardados " };

var listaProductos = new ListView
{
    X = 1,
    Y = 1,
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
ventana.Add(listaProductos);
ventana.Add(informacion);


app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync (HttpClient http) {
    const string url = "http://localhost:5050/productos";
      return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? [];
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
