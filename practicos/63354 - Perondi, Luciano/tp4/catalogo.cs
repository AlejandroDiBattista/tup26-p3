#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;

// ── Consulta inicial al servidor ──────────────────────────────────────────

using var http = new HttpClient();

List<ProductoDto> productos;
try {
    productos = await TraerProductosAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var listaProductos = new ListView {
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
};
listaProductos.SetSource(new ObservableCollection<string>(
    productos.Select(p => $"{p.Codigo}  {p.Nombre}  (stock {p.Stock})")
));

ventana.Add(listaProductos);

app.Run(ventana);

static async Task<List<ProductoDto>> TraerProductosAsync(HttpClient http) {
    const string url = "http://localhost:5050/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url) ?? [];
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
