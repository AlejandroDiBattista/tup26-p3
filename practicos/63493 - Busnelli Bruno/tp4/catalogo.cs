#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Text;
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

StringBuilder texto = new();

texto.AppendLine("# PRODUCTOS");
texto.AppendLine();

foreach (ProductoDto producto in productos)
{
    texto.AppendLine($"[{producto.Id}]");
    texto.AppendLine($"Código : {producto.Codigo}");
    texto.AppendLine($"Nombre : {producto.Nombre}");
    texto.AppendLine($"Precio : ${producto.Precio:N2}");
    texto.AppendLine($"Stock  : {producto.Stock}");
    texto.AppendLine();
}

Label listaProductos = new()
{
    Text = texto.ToString(),
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