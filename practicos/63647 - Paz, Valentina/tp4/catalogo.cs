#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;


// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;
try {
    using var http = new HttpClient();
    productos = await CargarProductoAsync(http);  
} 
catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var lista = new ListView() {
    X = 0,
    Y = 0,
    Width = 60,
    Height = 20
};
var detalle = new Label() {
    X = 0,
    Y = 22,
    Width = 80,
    Height = 10,
    Text = "Seleccione un producto"
};

lista.SetSource(
    new System.Collections.ObjectModel.ObservableCollection<string>
       ( productos.Select(p => $"{p.Codigo} - {p.Nombre} - Stock: {p.Stock}" ). ToList()
    )
);

if (productos.Count > 0)
{
    var producto = productos[0];

    detalle.Text =
        $"Id: {producto.Id}\n" +
        $"Código: {producto.Codigo}\n" +
        $"Nombre: {producto.Nombre}\n" +
        $"Precio: ${producto.Precio}\n" +
        $"Stock: {producto.Stock}";
}

ventana.Add(lista);
ventana.Add(detalle);

Console.WriteLine($"Productos cargados: {productos.Count}");

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductoAsync(HttpClient http)
{
    const string url = "http://Localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url) 
        ?? new List<ProductoDto>();
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
