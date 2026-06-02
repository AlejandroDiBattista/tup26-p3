#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui;
using System.Collections.ObjectModel;

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
using Window ventana = new () { Title = " Catalogo REST — Productos (ESC para salir) " };

var listaStrings = new ObservableCollection<string>(
    productos.Select(p => $"{p.Codigo} - {p.Nombre} (${p.Precio}) [Stock: {p.Stock}]")
);

var listaProductos = new ListView()
{
    X = 0,
    Y = 0,
    Width = 80,
    Height = 20
};

listaProductos.SetSource<string>(listaStrings);

ventana.Add(listaProductos);

app.Run(ventana);


static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http) {
    const string url = "http://localhost:5050/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? new List<ProductoDto>();
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
