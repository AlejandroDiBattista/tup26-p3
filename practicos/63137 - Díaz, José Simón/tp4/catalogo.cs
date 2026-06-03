#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ── Constantes ────────────────────────────────────────────────────────────

const string UrlServidor   = "http://localhost:5050";
const string RutaProductos = "/productos";

// ── Conexión inicial ──────────────────────────────────────────────────────

using HttpClient http = new() { BaseAddress = new Uri(UrlServidor) };
List<ProductoDto> productos;

try {
    productos = await ServicioApi.CargarProductos(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine($"Verificá que servidor.cs esté corriendo en {UrlServidor}");
    return;
}

// ── Estado de la interfaz ─────────────────────────────────────────────────

List<ProductoDto> filtrados = new(productos);

// ── Construcción de la interfaz TUI ──────────────────────────────────────

Menu.DefaultBorderStyle = LineStyle.Rounded;
using IApplication app = Application.Create().Init();

Runnable raiz = new();

Window ventana = new() {
    Title  = " Catalogo REST — Productos ",
    X      = 0,
    Y      = 0,
    Width  = Dim.Fill(),
    Height = Dim.Fill(),
};

Label etiquetaBuscar = new() { Text = "Buscar:", X = 1, Y = 1 };
TextField campoBuscar = new() {
    X     = Pos.Right(etiquetaBuscar) + 1,
    Y     = 1,
    Width = 30,
};

FrameView panelProductos = new() {
    Title  = "Productos",
    X      = 0,
    Y      = 3,
    Width  = Dim.Percent(50),
    Height = Dim.Fill(),
};

FrameView panelMovimientos = new() {
    Title  = "Movimientos",
    X      = Pos.Right(panelProductos),
    Y      = 3,
    Width  = Dim.Fill(),
    Height = Dim.Fill(),
};

ListView listaProductos = new() {
    X      = 0,
    Y      = 0,
    Width  = Dim.Fill(),
    Height = Dim.Fill(),
};

Label etiquetaDetalle = new() {
    Text   = "Seleccione un producto.",
    X      = 1,
    Y      = 1,
    Width  = Dim.Fill(2),
    Height = Dim.Fill(2),
};

panelProductos.Add(listaProductos);
panelMovimientos.Add(etiquetaDetalle);
ventana.Add(etiquetaBuscar, campoBuscar, panelProductos, panelMovimientos);
raiz.Add(ventana);

// ── Eventos de la interfaz ────────────────────────────────────────────────

campoBuscar.TextChanged += (_, _) => ActualizarLista();
listaProductos.ValueChanged += async (_, _) => await MostrarMovimientos();

ActualizarLista();
await MostrarMovimientos();

app.Run(raiz);

// ── Lógica de la interfaz ─────────────────────────────────────────────────

void ActualizarLista() {
    string busqueda = campoBuscar.Text?.Trim() ?? "";
    filtrados = string.IsNullOrWhiteSpace(busqueda)
        ? new(productos)
        : productos.Where(p =>
            p.Codigo.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
            p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
          .ToList();

    listaProductos.SetSource(new ObservableCollection<string>(
        filtrados.Select(FormatearFila).ToList()));
}

async Task MostrarMovimientos() {
    int indice = listaProductos.SelectedItem ?? 0;
    if (indice < 0 || indice >= filtrados.Count) {
        etiquetaDetalle.Text = "Seleccione un producto.";
        return;
    }

    var producto    = filtrados[indice];
    var movimientos = await ServicioApi.CargarMovimientos(http, producto.Id);
    var textoMov    = movimientos.Count == 0
        ? "Sin movimientos registrados."
        : string.Join("\n", movimientos.Select(FormatearMovimiento));

    etiquetaDetalle.Text = $"""
        PRODUCTO

        Id     : {producto.Id}
        Codigo : {producto.Codigo}
        Nombre : {producto.Nombre}
        Precio : ${producto.Precio:N2}
        Stock  : {producto.Stock}

        MOVIMIENTOS

        {textoMov}
        """;
}

static string FormatearFila(ProductoDto p) =>
    $"{p.Codigo,-6}  {p.Nombre,-24}  ${p.Precio,8:N2}  Stock:{p.Stock,5}";

static string FormatearMovimiento(MovimientoDto m) =>
    $"{m.Fecha:dd/MM/yyyy HH:mm}  {m.Tipo,-7}  Cant: {m.Cantidad}";

// ── Servicio de API ───────────────────────────────────────────────────────

static class ServicioApi {
    const string RutaProductos = "/productos";

    public static Task<List<ProductoDto>> CargarProductos(HttpClient http) =>
        http.GetFromJsonAsync<List<ProductoDto>>(RutaProductos)
            .ContinueWith(t => t.Result ?? []);

    public static Task<List<MovimientoDto>> CargarMovimientos(HttpClient http, int productoId) =>
        http.GetFromJsonAsync<List<MovimientoDto>>($"{RutaProductos}/{productoId}/movimientos")
            .ContinueWith(t => t.Result ?? []);
}

// ── DTOs ──────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
record ProductoDatos(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDatos(string Tipo, int Cantidad);