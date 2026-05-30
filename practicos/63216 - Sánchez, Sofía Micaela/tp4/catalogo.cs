#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────
const string ApiUrl = "http://localhost:5051";

using HttpClient http = new() { BaseAddress = new Uri(ApiUrl) };

List<ProductoDto> productos;

ProductoDto producto;
try {
    using var http = new HttpClient();
    producto = await CargarProductoAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}
List<ProductoDto> filtrados = new(productos);

// ── Interfaz TUI ──────────────────────────────────────────────────────────
Label etiquetaBuscar = null!;
TextField campoBuscar = null!;
ListView listaProductos = null!;
Label detalle = null!;

using IApplication app = Application.Create().Init();
Menu.DefaultBorderStyle = LineStyle.Rounded;

Runnable raiz = new() { };

MenuBar menu = new(new MenuBarItem[]
{
    new("Producto", new MenuItem[]
    {
        null!,
        new("_Salir", "Ctrl+Q Salir", () => app.RequestStop(), Key.Q.WithCtrl),
    }),
});


Window ventana = new()
{
    Title = " Catalogo REST - Productos ",
    X = 0, Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

etiquetaBuscar = new() { Text = "Buscar:", X = 1, Y = 1 };
campoBuscar = new()
{
    X = Pos.Right(etiquetaBuscar) + 1,
    Y = 1,
    Width = 30
};

FrameView panelProductos = new()
{
    Title = "Productos",
    X = 0, Y = 3,
    Width = Dim.Percent(55),
    Height = Dim.Fill()
};

FrameView panelDetalle = new()
{
    Title = "Detalle / Movimientos",
    X = Pos.Right(panelProductos),
    Y = 3,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

listaProductos = new()
{
    X = 1, Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(2)
};

detalle = new()
{
    Text = "Seleccione un producto.",
    X = 1, Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(2)
};

panelProductos.Add(listaProductos);
panelDetalle.Add(detalle);
ventana.Add(etiquetaBuscar, campoBuscar, panelProductos, panelDetalle);
raiz.Add(menu, ventana);

campoBuscar.TextChanged += (_, _) => ActualizarLista();

listaProductos.ValueChanged += async (_, _) =>
{
    await MostrarDetalle();
};

ActualizarLista();
await MostrarDetalle();

            - Id     : {producto.Id}
            - Código : {producto.Codigo}
            - Nombre : {producto.Nombre}
            - Precio : ${producto.Precio,10:N2}
            - Stock  :  {producto.Stock,10}
            """,
    X = 4, Y = 2,
};
app.Run(raiz);


void ActualizarLista()
{
    string texto = campoBuscar.Text?.Trim() ?? "";
    filtrados = string.IsNullOrWhiteSpace(texto)
        ? new(productos)
        : productos.Where(p =>
            p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
            p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();

    listaProductos.SetSource(new ObservableCollection<string>(
        filtrados.Select(FormatearProducto).ToList()));
}

async Task MostrarDetalle()
{
    int indice = listaProductos.SelectedItem ?? 0;
    if (indice < 0 || indice >= filtrados.Count)
    {
        detalle.Text = "Seleccione un producto.";
        return;
    }

    ProductoDto producto = filtrados[indice];
    detalle.Text = $"""
        PRODUCTO

        Id     : {producto.Id}
        Codigo : {producto.Codigo}
        Nombre : {producto.Nombre}
        Precio : ${producto.Precio:N2}
        Stock  : {producto.Stock}

        MOVIMIENTOS

        {textoMovimientos}
        """;
}

async Task RecargarYActualizar(int? idSeleccionado = null)
{
    productos = await CargarProductos(http);
    ActualizarLista();

    if (idSeleccionado.HasValue)
    {
        int idx = filtrados.FindIndex(p => p.Id == idSeleccionado.Value);
        if (idx >= 0) listaProductos.SelectedItem = idx;
    }

    await MostrarDetalle();
}

// ── DTO ───────────────────────────────────────────────────────────────────

static async Task<List<ProductoDto>> CargarProductos(HttpClient http)
    => await http.GetFromJsonAsync<List<ProductoDto>>("/productos") ?? [];
static string FormatearProducto(ProductoDto p)
    => $"{p.Codigo,-6} {p.Nombre,-25} ${p.Precio,8:N2} Stock: {p.Stock,4}";
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
