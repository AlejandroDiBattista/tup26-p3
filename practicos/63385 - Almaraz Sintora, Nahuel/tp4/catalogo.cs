#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

using System.Collections.ObjectModel;
using System.Globalization;

using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5050") };

try {
    await http.GetFromJsonAsync<List<ProductoDto>>("productos");
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Primero ejecuta: dotnet run servidor.cs");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(http));

public sealed class CatalogoWindow : Runnable {
    private readonly HttpClient http;
    private readonly ObservableCollection<string> lineasProductos = [];
    private readonly ObservableCollection<string> lineasMovimientos = [];

    private List<ProductoDto> productos = [];
    private List<ProductoDto> productosFiltrados = [];

    private TextField buscarTexto = null!;
    private ListView listaProductos = null!;
    private ListView listaMovimientos = null!;
    private Label estado = null!;
    public CatalogoWindow(HttpClient http) {
        this.http = http;
        Title = "Catalogo REST - ESC para salir";
        Width = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;
        CrearInterfaz();
    }

    private void CrearInterfaz() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Productos", [
                    new MenuItem("_Agregar", "F2", AgregarProducto),
                    new MenuItem("_Modificar", "F3", ModificarProducto),
                    new MenuItem("_Eliminar", "F4", EliminarProducto),
                    null!,
                    new MenuItem("_Recargar", "F5", RecargarProductos)
                ]),
                new MenuBarItem("_Movimientos", [
                    new MenuItem("_Compra", "Ctrl+C", () => RegistrarMovimiento("Compra")),
                    new MenuItem("_Venta", "Ctrl+V", () => RegistrarMovimiento("Venta")),
                    new MenuItem("_Ajuste", "Ctrl+A", () => RegistrarMovimiento("Ajuste"))
                ]),
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Salir", "Ctrl+Q", Salir)
                ])
            ]
        };

        Label buscarLabel = new() { Text = "Buscar:", X = 1, Y = 2 };
        buscarTexto = new TextField { X = 9, Y = 2, Width = 38 };

        Button buscarBoton = new() { Text = "_Buscar", X = 49, Y = 2 };
        buscarBoton.Accepting += (_, e) => { AplicarFiltro(); e.Handled = true; };
        buscarTexto.Accepting += (_, e) => { AplicarFiltro(); e.Handled = true; };

        Label productosTitulo = new() { Text = "Productos (F2 agregar, F3 modificar, F4 eliminar)", X = 1, Y = 4 };
        listaProductos = new ListView { X = 1, Y = 5, Width = 72, Height = 18 };
        listaProductos.SetSource(lineasProductos);
        listaProductos.ValueChanged += (_, _) => CargarMovimientosDelSeleccionado();

        Label movimientosTitulo = new() { Text = "Historial de movimientos", X = 75, Y = 4 };
        listaMovimientos = new ListView { X = 75, Y = 5, Width = 52, Height = 18 };
        listaMovimientos.SetSource(lineasMovimientos);

        estado = new Label { Text = "", X = 1, Y = 27, Width = 120 };

        Add(menu, buscarLabel, buscarTexto, buscarBoton, productosTitulo, listaProductos, movimientosTitulo, listaMovimientos, estado);
    }
}

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
