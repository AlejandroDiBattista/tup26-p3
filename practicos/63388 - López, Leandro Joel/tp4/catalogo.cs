#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;


// ── Consulta inicial al servidor ──────────────────────────────────────────

ProductoDto producto;
try {
    using var http = new HttpClient();
    producto = await CargarProductoAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5000");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
app.Init();

using var Window = new CatalogoWindow("http://localhost:5000");
app.Run(Window);

public sealed class CatalogoWindow : Window {
    
    private readonly CatalogoApiClient _api;
    private readonly ObservableCollection<string> _productosSource = [];
    private readonly ObservableCollection<string> _movimientosSource = [];
    private readonly TextField _buscar;
    private readonly ListView _productosList;
    private readonly ListView _movimientosList;
    private readonly Label _estado;
    private bool _cargoInicial;
    private List<ProductoDto> _productos = [];
    private List<ProductoDto> _productosFiltrados = [];


    public CatalogoWindow(string baseUrl){
    
    _api = new CatalogoApiClient(baseUrl);

    Title = "Catálogo de Productos";
    Width = Dim.Fill();
    Height = Dim.Fill();

    var menu = new MenuBar([
        
        new MenuBarItem("_Productos", [
            new MenuItem("_Agregar", "F2", AgregarProducto),
            new MenuItem("_Modificar", "F3", ModificarProducto),
            new MenuItem("_Eliminar", "F4", EliminarProducto),
            new MenuItem("_Actualizar", "F9", CargarProductos)
        ]),
        new MenuBarItem("_Movimientos", [
            new MenuItem("_Registrar movimiento", "F5", RegistrarMovimiento)
        ]),
        new MenuBarItem("_Archivo", [
            new MenuItem("_Salir", "Esc", () => App?.RequestStop())
        ])

    ]) {
        X = 0,
        Y = 0,
        Width = Dim.Fill()
    };

    var buscarLabel = new Label {
        
        Text = "Buscar:",
        X = 1,
        Y = 1
    };

    _buscar = new TextField {
        X = Pos.Right(buscarLabel) + 1,
        Y = 1,
        Width = Dim.Percent(38)

    };
    _buscar.TextChanged += (_, _) => AplicarFiltro();

    var ayuda = new Label {
        
        Text = "F2: Agregar | F3: Modificar | F4: Eliminar | F5: Movimiento | F9: Actualizar | Esc: Salir",
        X = Pos.Right(_buscar) + 2,
        Y = 1,
        Width = Dim.Fill(1)
        
    };

    var panelProductos = new FrameView {
        
        Title = "Productos",
        X = 0,
        Y = 3,
        Width = Dim.Percent(58),
        Height = Dim.Fill(1)
    };

    _productosList = new ListView{
        
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };

    _productosList.SetSource(_productosSource);
    _productosList.ValueChanged += (_, _) => CargarMovimientos();
    _productosList.Accepted += (_, _) => ModificarProducto();
    panelProductos.Add(_productosList);

    var panelMovimientos = new FrameView {
        
        Title = "Movimientos del producto seleccionado",
        X = Pos.Right(panelProductos),
        Y = 3,
        Width = Dim.Fill(),
        Height = Dim.Fill(1)
    };

    _movimientosList = new ListView {
        
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };
    _movimientosList.SetSource(_movimientosSource);
    panelMovimientos.Add(_movimientosList);

    _estado = new Label {
        
        Text = "Cargando productos...",
        X = 1,
        Y = Pos.AnchorEnd(1),
        Width = Dim.Fill(1)
    };

    Add(menu, buscarLabel, _buscar, ayuda, panelProductos, panelMovimientos, _estado);

    AddCommand(Command.New, () => Ejecutar(AgregarProducto));
    AddCommand(Command.Edit, () => Ejecutar(ModificarProducto));
    AddCommand(Command.DeleteCharRight, () => Ejecutar(EliminarProducto));
    AddCommand(Command.Save, () => Ejecutar(RegistrarMovimiento));
    AddCommand(Command.Refresh, () => Ejecutar(CargarProductos));
    KeyBinding.Add(Key.F2, Command.New);
    KeyBinding.Add(Key.F3, Command.Edit);
    KeyBinding.Add(Key.F4, Command.DeleteCharRight);
    KeyBinding.Add(Key.F5, Command.Save);
    KeyBinding.Add(Key.F9, Command.Refresh);

}

protected override void OnIsRunningChanged(bool isRunning) {

    base.OnIsRunningChanged(isRunning);
    if (isRunning && !_cargoInicial) {

        _cargoInicial = true;
        CargarProductos();
    }
}

private bool? Ejecutar(Action action) {

    action();
    return true;
}

private ProductoDto? ProductoSeleccionado() {

    var index = _productosList.SelectedItem;
    if (index is null || index < 0 || index >= _productosFiltrados.Count) {
        return null;
    }
    return _productosFiltrados[index.Value];
}

private void CargarProductos() {

    try {
        var seleccionado = ProductoSeleccionado()?.Id;
        _productos = _api.ListarProductosAsync().GetAwaiter().GetResult();
        AplicarFiltro(seleccionado);
        Estado($"Productos cargados: {_productos.Count}");

    } catch (Exception ex) {
        MostrarError($"No se pudo conectar con el servidor. Ejecuta el servidor primero.", ex);
    }
}

private void AplicarFiltro(int? productoASeleccionar = null) {
        
        var texto = (_buscar.Text?.ToString() ?? "").Trim();
        _productosFiltrados = string.IsNullOrEmpty(texto)
            ? [.. _productos]
            : _productos
                .Where(producto => producto.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) || producto.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();

        _productosSource.Clear();
        foreach (var producto in _productosFiltrados) {

            _productosSource.Add(FormatearProducto(producto));
        }

        if (_productosFiltrados.Count == 0) {

            _movimientosSource.Clear();
            _productosList.SetNeedsDraw();
            _movimientosList.SetNeedsDraw();
            return;
        }

        var index = productoASeleccionar is null ? 0 : _productosFiltrados.FindIndex(producto => producto.Id == productoASeleccionar.Value);

        _productosList.SelectedItem = Math.Max(0, index);
        _productosList.EnsureSelectedItemVisible();
        _productosList.SetNeedsDraw();
        CargarMovimientos();
    }

    private void CargarMovimientos() {

        var producto = ProductoSeleccionado();
        _movimientosSource.Clear();

        if (producto is null) {
            
            _movimientosList.SetNeedsDraw();
            return;
        }

        try {

            var movimientos = _api.ListarMovimientosAsync(producto.Id).GetAwaiter().GetResult();
            
            if (movimientos.Count == 0) {

                _movimientosSource.Add("No hay movimientos para este producto.");
            }
            else {
            foreach (var movimiento in movimientos) {

                _movimientosSource.Add(FormatearMovimiento(movimiento));
            }
            }
            _movimientosList.SetNeedsDraw();

        } catch (Exception ex) {

            MostrarError($"No se pudo cargar el historial", ex);
        }
    }

var detalleProducto = new Label {
    Text = $"""
            # PRODUCTO 

            - Id     : {producto.Id}
            - Código : {producto.Codigo}
            - Nombre : {producto.Nombre}
            - Precio : ${producto.Precio,10:N2}
            - Stock  :  {producto.Stock,10}
            """,
    X = 4, Y = 2,
};

Window.Add(detalleProducto);

app.Run(Window);

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5000/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
