#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;
ProductoDto? seleccionado = null;

try {
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
    seleccionado = productos.FirstOrDefault();
}
catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var source = new ObservableCollection<string>(
    productos.Select(p =>
        $"{p.Id} - {p.Codigo} - {p.Nombre} | ${p.Precio} | Stock: {p.Stock}"
    )
);
var listView = new ListView()
{
    X = 1,
    Y = 1,
    Width = 50,
    Height = 20
};
listView.SetSource(source);

var detalle = new Label
{
    X = 52,
    Y = 1,
    Width = 40,
    Height = 20,
    Text = "Sin selección"
};

_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(150);

        var index = listView.SelectedItem.GetValueOrDefault(-1);

        if (index >= 0 && index < productos.Count)
        {
            var p = productos[index];

            if (seleccionado?.Id != p.Id)
            {
                seleccionado = p;
                detalle.Text = RenderDetalle(p);
            }
        }
    }
});
ventana.Add(listView);
ventana.Add(detalle);
app.Run(ventana);
static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
           ?? new List<ProductoDto>();
}

static string RenderDetalle(ProductoDto? p)
{
    if (p is null) return "Sin selección";

    return $"""
    # PRODUCTO

    - Id     : {p.Id}
    - Código : {p.Codigo}
    - Nombre : {p.Nombre}
    - Precio : ${p.Precio:N2}
    - Stock  : {p.Stock}
    """;
}

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);