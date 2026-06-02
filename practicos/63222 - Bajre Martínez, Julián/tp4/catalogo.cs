#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

using var http = new HttpClient();

List<ProductoDto> productos;

try
{
    productos = await CargarProductosAsync(http);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () 
{ 
    Title = " Catalogo de Productos ", 
};

var frameProductos = new FrameView()
{
    Title = "Productos",
    X = 0,
    Y = 0,
    Width = Dim.Percent(40),
    Height = Dim.Fill()
};

var frameMovimientos = new FrameView()
{
    Title = "Movimientos",
    X = Pos.Right(frameProductos),
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var listaProductos = new ListView(
    productos.Select(
        p => $"{p.Codigo} - {p.Nombre}"
    ).ToList()
)
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var listaMovimientos = new ListView()
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};


frameProductos.Add(listaProductos);
frameMovimientos.Add(listaMovimientos);

ventana.Add(frameProductos);
ventana.Add(frameMovimientos);

async Task CargarMovimientos()
{
    if (listaProductos.SelectedItem < 0)
        return;

    var producto = productos[listaProductos.SelectedItem];

    try
    {
        var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>
        (
            $"http://localhost:5050/productos/{producto.Id}/movimientos"
        );

        listaMovimientos.SetSource(
            movimientos?
            .Select(m =>
                $"{m.Tipo,-8} {m.Cantidad,5} {m.Fecha:g}")
            .ToList()
            ?? []
        );
    }
    catch
    {
        listaMovimientos.SetSource(
            new List<string>
            {
                "Error al cargar movimientos"
            }
        );
    }
}

listaProductos.SelectedItemChanged += async _ =>
{
    await CargarMovimientos();
};

if (productos.Count > 0)
{
    listaProductos.SelectedItem = 0;
    await CargarMovimientos();
}

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
           ?? [];
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
