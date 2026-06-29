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

    productos = await CatalogoApi.ListarProductosAsync(http);
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

static class CatalogoApi
{
    private const string BaseUrl = "http://localhost:5050";

    public static async Task<List<ProductoDto>> ListarProductosAsync(HttpClient http)
    {
        return await http.GetFromJsonAsync<List<ProductoDto>>($"{BaseUrl}/productos")
               ?? [];
    }

    public static async Task<ProductoDto?> BuscarProductoAsync(HttpClient http, int id)
    {
        return await http.GetFromJsonAsync<ProductoDto>($"{BaseUrl}/productos/{id}");
    }

    public static async Task<ProductoDto?> CrearProductoAsync(HttpClient http, ProductoInputDto input)
    {
        HttpResponseMessage response =
            await http.PostAsJsonAsync($"{BaseUrl}/productos", input);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<ProductoDto>();
    }

    public static async Task<ProductoDto?> ModificarProductoAsync(HttpClient http, int id, ProductoInputDto input)
    {
        HttpResponseMessage response =
            await http.PutAsJsonAsync($"{BaseUrl}/productos/{id}", input);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<ProductoDto>();
    }

    public static async Task EliminarProductoAsync(HttpClient http, int id)
    {
        HttpResponseMessage response =
            await http.DeleteAsync($"{BaseUrl}/productos/{id}");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);

record ProductoInputDto(
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);