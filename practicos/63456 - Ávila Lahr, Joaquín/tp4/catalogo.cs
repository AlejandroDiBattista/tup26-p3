#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;

try {
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
}
catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var lista = new Label {
    Text = string.Join("\n", productos.Select(p =>
        $"{p.Id} - {p.Codigo} - {p.Nombre} - ${p.Precio} - Stock: {p.Stock}"
    )),
    X = 2,
    Y = 1,
};

ventana.Add(lista);

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
           ?? new List<ProductoDto>();
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
