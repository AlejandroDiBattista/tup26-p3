#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

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
    Title = " Catálogo REST "
};

string texto = """
# PRODUCTOS

""";

foreach (ProductoDto producto in productos)
{
    texto +=
$"""
[{producto.Id}]
Código : {producto.Codigo}
Nombre : {producto.Nombre}
Precio : ${producto.Precio:N2}
Stock  : {producto.Stock}

""";
}

var listaProductos = new Label
{
    Text = texto,
    X = 2,
    Y = 1
};

ventana.Add(listaProductos);

app.Run(ventana);

// ── Cliente REST ──────────────────────────────────────────────────────────

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? [];
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);