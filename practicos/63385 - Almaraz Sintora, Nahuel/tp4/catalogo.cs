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
        private void RecargarProductos() {
        try {
            productos = http.GetFromJsonAsync<List<ProductoDto>>("productos")
                .GetAwaiter()
                .GetResult() ?? [];

            AplicarFiltro();
            estado.Text = $"Productos cargados: {productos.Count}";
        } catch (Exception ex) {
            MostrarMensaje("Error", $"No se pudieron cargar los productos.\n{ex.Message}");
        }
    }

    private void AplicarFiltro() {
        string texto = buscarTexto.Text.ToString().Trim().ToLowerInvariant();

        productosFiltrados = productos
            .Where(p =>
                string.IsNullOrEmpty(texto) ||
                p.Codigo.ToLowerInvariant().Contains(texto) ||
                p.Nombre.ToLowerInvariant().Contains(texto))
            .ToList();

        lineasProductos.Clear();

        foreach (var producto in productosFiltrados) {
            lineasProductos.Add(FormatearProducto(producto));
        }

        listaProductos.SelectedItem = lineasProductos.Count > 0 ? 0 : null;
        CargarMovimientosDelSeleccionado();
    }
        RecargarProductos();
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

        Button agregar = new() { Text = "_Agregar", X = 1, Y = 24 };
        agregar.Accepting += (_, e) => { AgregarProducto(); e.Handled = true; };

        Button modificar = new() { Text = "_Modificar", X = 13, Y = 24 };
        modificar.Accepting += (_, e) => { ModificarProducto(); e.Handled = true; };

        Button eliminar = new() { Text = "_Eliminar", X = 28, Y = 24 };
        eliminar.Accepting += (_, e) => { EliminarProducto(); e.Handled = true; };

        Button compra = new() { Text = "_Compra", X = 44, Y = 24 };
        compra.Accepting += (_, e) => { RegistrarMovimiento("Compra"); e.Handled = true; };

        Button venta = new() { Text = "_Venta", X = 56, Y = 24 };
        venta.Accepting += (_, e) => { RegistrarMovimiento("Venta"); e.Handled = true; };

        Button ajuste = new() { Text = "_Ajuste", X = 67, Y = 24 };
        ajuste.Accepting += (_, e) => { RegistrarMovimiento("Ajuste"); e.Handled = true; };

        Add(menu, buscarLabel, buscarTexto, buscarBoton, productosTitulo, listaProductos, movimientosTitulo, listaMovimientos, agregar, modificar, eliminar, compra, venta, ajuste, estado);
    }

    private void CargarMovimientosDelSeleccionado() {
        lineasMovimientos.Clear();

        var producto = ProductoSeleccionado();
        if (producto is null) return;

        try {
            var movimientos = http.GetFromJsonAsync<List<MovimientoDto>>($"productos/{producto.Id}/movimientos")
                .GetAwaiter()
                .GetResult() ?? [];

            if (movimientos.Count == 0) {
                lineasMovimientos.Add("Sin movimientos.");
                return;
            }

            foreach (var movimiento in movimientos) {
                lineasMovimientos.Add(FormatearMovimiento(movimiento));
            }
        } catch (Exception ex) {
            lineasMovimientos.Add($"Error: {ex.Message}");
        }
    }

    private ProductoDto? ProductoSeleccionado() {
        int? indice = listaProductos.SelectedItem;
        if (indice is null || indice < 0 || indice >= productosFiltrados.Count) return null;

        return productosFiltrados[indice.Value];
    }

    private static string FormatearProducto(ProductoDto p) {
        string nombre = Cortar(p.Nombre, 24);
        return $"{p.Codigo,-8} {nombre,-24} ${p.Precio,9:N2} Stock:{p.Stock,4}";
    }

    private static string FormatearMovimiento(MovimientoDto m) =>
        $"{m.Fecha:dd/MM/yyyy HH:mm}  {m.Tipo,-7}  Cantidad: {m.Cantidad}";

    private static string Cortar(string texto, int largo) =>
        texto.Length <= largo ? texto : texto[..(largo - 3)] + "...";

    private void MostrarMensaje(string titulo, string mensaje) {
        MensajeDialog dialogo = new(titulo, mensaje);
        App!.Run(dialogo);
    }

    private void Salir() {
        App!.RequestStop();
    }
    
    protected override bool OnKeyDown(Key key) {
        if (key == Key.F2) { AgregarProducto(); return true; }
        if (key == Key.F3) { ModificarProducto(); return true; }
        if (key == Key.F4) { EliminarProducto(); return true; }
        if (key == Key.F5) { RecargarProductos(); return true; }
        if (key == Key.C.WithCtrl) { RegistrarMovimiento("Compra"); return true; }
        if (key == Key.V.WithCtrl) { RegistrarMovimiento("Venta"); return true; }
        if (key == Key.A.WithCtrl) { RegistrarMovimiento("Ajuste"); return true; }
        if (key == Key.Q.WithCtrl || key == Key.Esc) { Salir(); return true; }
        return base.OnKeyDown(key);
    }
}
