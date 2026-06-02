#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Input;


using var http = new HttpClient();
http.BaseAddress = new Uri("http://localhost:5050");

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(http));

// ── ventana principal ──────//

class CatalogoWindow : Window {
    private readonly HttpClient _http;
    private List<ProductoDto> _productos = [];
    private List<ProductoDto> _filteredProductos = [];
    private List<MovimientoDto> _movimientos = [];

    private TextField  _searchField     = null!;
    private ListView   _listView        = null!;
    private ListView   _movimientosView = null!;
    private StatusBar  _statusBar       = null!;

    public CatalogoWindow(HttpClient http) {
        _http  = http;
        Title  = "CATALOGO DE PRODUCTOS";
        X      = 0;
        Y      = 0;
        Width  = Dim.Fill();
        Height = Dim.Fill();

        BuildLayout();
        _ = CargarProductosAsync();
    }

    private void BuildLayout() {
        var menu = new MenuBar {
            Menus = [
                new MenuBarItem("_Productos", [
                    new MenuItem("_Nuevo",    "F2",  NuevoProducto),
                    new MenuItem("_Editar",   "F3",  EditarProducto),
                    new MenuItem("_Eliminar", "Del", EliminarProducto)
                ]),
                new MenuBarItem("_Movimientos", [
                    new MenuItem("_Registrar movimiento", "F5", RegistrarMovimiento)
                ])
            ]
        };

        var searchLabel = new Label { Text = "Buscar:", X = 1, Y = 1 };

        _searchField = new TextField {
            X = Pos.Right(searchLabel) + 1,
            Y = 1,
            Width = Dim.Fill(1)
        };
        _searchField.TextChanged += (_, _) => ApplyFilter();

        var listFrame = new FrameView {
            Title  = "Productos",
            X      = 1,
            Y      = 3,
            Width  = Dim.Percent(45),
            Height = Dim.Fill(2)
        };

        _listView = new ListView {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        _listView.ValueChanged += (_, _) => _ = CargarMovimientosAsync();
        listFrame.Add(_listView);

        var movFrame = new FrameView {
            Title  = "Movimientos",
            X      = Pos.Right(listFrame) + 1,
            Y      = 3,
            Width  = Dim.Fill(1),
            Height = Dim.Fill(2)
        };

        _movimientosView = new ListView {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        movFrame.Add(_movimientosView);

        _statusBar = new StatusBar([
            new Shortcut(Key.F2,         "Nuevo",      NuevoProducto),
            new Shortcut(Key.F3,         "Editar",     EditarProducto),
            new Shortcut(Key.Delete,     "Eliminar",   EliminarProducto),
            new Shortcut(Key.F5,         "Movimiento", RegistrarMovimiento),
            new Shortcut(Key.Q.WithCtrl, "Salir",      () => App!.RequestStop())
        ]);

        Add(menu, searchLabel, _searchField, listFrame, movFrame, _statusBar);
    }

    private async Task CargarProductosAsync() {
        try {
            _productos = await _http.GetFromJsonAsync<List<ProductoDto>>("/productos") ?? [];
            ApplyFilter();
            SetStatus("Productos cargados correctamente");
        }
        catch (Exception ex) {
            SetStatus($"Error: {ex.Message}");
        }
    }

    private async Task CargarMovimientosAsync() {
        var producto = SelectedProducto();
        if (producto is null) {
            _movimientos = [];
            RefreshMovimientos();
            return;
        }
        try {
            _movimientos = await _http.GetFromJsonAsync<List<MovimientoDto>>(
                $"/productos/{producto.Id}/movimientos") ?? [];
            RefreshMovimientos();
        }
        catch (Exception ex) {
            SetStatus($"Error: {ex.Message}");
        }
    }

    private void ApplyFilter() {
        string busqueda = _searchField.Text.ToLower();
        _filteredProductos = _productos
            .Where(p => p.Nombre.ToLower().Contains(busqueda) ||
                        p.Codigo.ToLower().Contains(busqueda))
            .ToList();
        RefreshList();
    }

    private void RefreshList() {
        _listView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(
            _filteredProductos.Select(p => $"{p.Codigo,-8} {p.Nombre,-20} ${p.Precio,8:N2} [{p.Stock}]")));
    }

    private void RefreshMovimientos() {
        _movimientosView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(
            _movimientos.Select(m => $"{m.Tipo,-8} {m.Cantidad,5}  {m.Fecha:dd/MM/yyyy HH:mm}")));
    }

    private void SetStatus(string mensaje) {
        _statusBar.Title = mensaje;
        _statusBar.SetNeedsDraw();
    }

    private ProductoDto? SelectedProducto() {
        int idx = _listView.SelectedItem ?? -1;
        return idx >= 0 && idx < _filteredProductos.Count
            ? _filteredProductos[idx]
            : null;
    }

    private void NuevoProducto() { }
    private void EditarProducto() { }
    private void EliminarProducto() { }
    private void RegistrarMovimiento() { }

    protected override bool OnKeyDown(Key key) {
        switch (key) {
            case var k when k == Key.F2: NuevoProducto();      return true;
            case var k when k == Key.F3: EditarProducto();     return true;
            case var k when k == Key.F5: RegistrarMovimiento(); return true;
            case var k when k == Key.Q.WithCtrl: App!.RequestStop(); return true;
            default: return base.OnKeyDown(key);
        }
    }
}

// ── dtos ────//

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);