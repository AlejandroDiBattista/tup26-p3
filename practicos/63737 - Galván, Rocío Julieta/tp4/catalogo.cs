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
        _movimientos.Select(m => {
            string tipo = m.Tipo switch {
                1 => "Compra",
                2 => "Venta",
                3 => "Ajuste",
                _ => "?"
            };
            return $"{tipo,-8} {m.Cantidad,5}  {m.Fecha:dd/MM/yyyy HH:mm}";
        })));
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
private async void NuevoProducto() {
    var dialog = new ProductoDialog();
    App!.Run(dialog);

    if (dialog.Result is null) return;

    try {
        var response = await _http.PostAsJsonAsync("/productos", dialog.Result);
        if (!response.IsSuccessStatusCode) {
            SetStatus("Error al crear el producto");
            return;
        }
        await CargarProductosAsync();
        SetStatus($"Producto '{dialog.Result.Nombre}' creado correctamente");
    }
    catch (Exception ex) {
        SetStatus($"Error: {ex.Message}");
    }
}

private async void EditarProducto() {
    var producto = SelectedProducto();
    if (producto is null) {
        MessageBox.Query(App!, "Editar", "Seleccioná un producto", "Ok");
        return;
    }

    var dialog = new ProductoDialog(producto);
    App!.Run(dialog);

    if (dialog.Result is null) return;

    try {
        var response = await _http.PutAsJsonAsync($"/productos/{producto.Id}", dialog.Result);
        if (!response.IsSuccessStatusCode) {
            SetStatus("Error al actualizar el producto");
            return;
        }
        await CargarProductosAsync();
        SetStatus($"Producto '{dialog.Result.Nombre}' actualizado correctamente");
    }
    catch (Exception ex) {
        SetStatus($"Error: {ex.Message}");
    }


}

private async void EliminarProducto() {
    var producto = SelectedProducto();
    if (producto is null) {
        MessageBox.Query(App!, "Eliminar", "Seleccioná un producto", "Ok");
        return;
    }

 int? confirm = MessageBox.Query(
    App!,
    "Eliminar",
    $"¿Eliminar '{producto.Nombre}'?",
    "Sí", "No");

if (confirm != 0) return;


    try {
        var response = await _http.DeleteAsync($"/productos/{producto.Id}");
        if (!response.IsSuccessStatusCode) {
            SetStatus("Error al eliminar el producto");
            return;
        }
        await CargarProductosAsync();
        SetStatus($"Producto '{producto.Nombre}' eliminado correctamente");
    }
    catch (Exception ex) {
        SetStatus($"Error: {ex.Message}");
    }
}

private async void RegistrarMovimiento() {
    var producto = SelectedProducto();
    if (producto is null) {
        MessageBox.Query(App!, "Movimiento", "Seleccioná un producto", "Ok");
        return;
    }
     var dialog = new MovimientoDialog(producto!);
    App!.Run(dialog);

    if (dialog.Result is null) return;

    try {
        var response = await _http.PostAsJsonAsync(
            $"/productos/{producto.Id}/movimientos",
            dialog.Result);

        if (!response.IsSuccessStatusCode) {
            SetStatus("Error al registrar el movimiento");
            return;
        }

        await CargarProductosAsync();
        await CargarMovimientosAsync();
        SetStatus($"Movimiento registrado correctamente");
    }
    catch (Exception ex) {
        SetStatus($"Error: {ex.Message}");
    }
}



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

class ProductoDialog : Dialog {
    private readonly TextField _codigoField;
    private readonly TextField _nombreField;
    private readonly TextField _precioField;
    private readonly TextField _stockField;

    public new ProductoDto? Result { get; private set; }

    public ProductoDialog(ProductoDto? producto = null) {
        Title  = producto is null ? "Nuevo producto" : "Editar producto";
        Width  = 60;
        Height = 18;

        var lblCodigo = new Label { Text = "Código:",  X = 1, Y = 1 };
        var lblNombre = new Label { Text = "Nombre:",  X = 1, Y = 3 };
        var lblPrecio = new Label { Text = "Precio:",  X = 1, Y = 5 };
        var lblStock  = new Label { Text = "Stock:",   X = 1, Y = 7 };

        _codigoField = new TextField {
            X = 12, Y = 1, Width = Dim.Fill(2),
            Text = producto?.Codigo ?? ""
        };
        _nombreField = new TextField {
            X = 12, Y = 3, Width = Dim.Fill(2),
            Text = producto?.Nombre ?? ""
        };
        _precioField = new TextField {
            X = 12, Y = 5, Width = Dim.Fill(2),
            Text = producto?.Precio.ToString() ?? ""
        };
        _stockField = new TextField {
            X = 12, Y = 7, Width = Dim.Fill(2),
            Text = producto?.Stock.ToString() ?? ""
        };

        var btnGuardar = new Button {
            Text = "_Guardar",
            X = Pos.Center() - 10,
            Y = Pos.AnchorEnd(2),
            IsDefault = true
        };
        btnGuardar.Accepting += (_, e) => {
            if (string.IsNullOrWhiteSpace(_codigoField.Text)) {
                MessageBox.ErrorQuery(App!, "Error", "El código es obligatorio", "Ok");
                _codigoField.SetFocus();
                e.Handled = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(_nombreField.Text)) {
                MessageBox.ErrorQuery(App!, "Error", "El nombre es obligatorio", "Ok");
                _nombreField.SetFocus();
                e.Handled = true;
                return;
            }
            if (!decimal.TryParse(_precioField.Text, 
             System.Globalization.NumberStyles.Any,
             System.Globalization.CultureInfo.InvariantCulture,
             out decimal precio) || precio <= 0) {
            }
            if (!int.TryParse(_stockField.Text, out int stock) || stock < 0) {
                MessageBox.ErrorQuery(App!, "Error", "El stock debe ser un número positivo", "Ok");
                _stockField.SetFocus();
                e.Handled = true;
                return;
            }

            Result = new ProductoDto(
                producto?.Id ?? 0,
                _codigoField.Text,
                _nombreField.Text,
                precio,
                stock
            );

            RequestStop();
            e.Handled = true;
        };

        var btnCancelar = new Button {
            Text = "_Cancelar",
            X = Pos.Center() + 2,
            Y = Pos.AnchorEnd(2)
        };
        btnCancelar.Accepting += (_, e) => {
            Result = null;
            RequestStop();
            e.Handled = true;
        };

        Add(lblCodigo, lblNombre, lblPrecio, lblStock,
            _codigoField, _nombreField, _precioField, _stockField,
            btnGuardar, btnCancelar);
    }
}

class MovimientoDialog : Dialog {
    private readonly CheckBox  _chkCompra;
    private readonly CheckBox  _chkVenta;
    private readonly CheckBox  _chkAjuste;
    private readonly TextField _cantidadField;

    public new MovimientoDto? Result { get; private set; }

    public MovimientoDialog(ProductoDto producto) {
        Title  = $"Registrar movimiento — {producto.Nombre}";
        Width  = 60;
        Height = 16;

        var lblTipo = new Label { Text = "Tipo:", X = 1, Y = 1 };

        _chkCompra = new CheckBox { Text = "_Compra", X = 1, Y = 2, Value = CheckState.Checked };
        _chkVenta  = new CheckBox { Text = "_Venta",  X = 1, Y = 4 };
        _chkAjuste = new CheckBox { Text = "_Ajuste", X = 1, Y = 6 };

        _chkCompra.ValueChanged += (_, _) => {
            _chkVenta.Value  = CheckState.UnChecked;
            _chkAjuste.Value = CheckState.UnChecked;
        };
        _chkVenta.ValueChanged += (_, _) => {
            _chkCompra.Value = CheckState.UnChecked;
            _chkAjuste.Value = CheckState.UnChecked;
        };
        _chkAjuste.ValueChanged += (_, _) => {
            _chkCompra.Value = CheckState.UnChecked;
            _chkVenta.Value  = CheckState.UnChecked;
        };

        var lblCantidad = new Label { Text = "Cantidad:", X = 1, Y = 8 };
        _cantidadField  = new TextField { X = 12, Y = 8, Width = Dim.Fill(2) };


        var lblStock = new Label {
            Text = $"Stock actual: {producto.Stock}",
            X = 1, Y = 10
        };

        var btnGuardar = new Button {
            Text = "_Guardar",
            X = Pos.Center() - 10,
            Y = Pos.AnchorEnd(2),
            IsDefault = true
        };

        btnGuardar.Accepting += (_, e) => {
            if (!int.TryParse(_cantidadField.Text, out int cantidad) || cantidad <= 0) {
                MessageBox.ErrorQuery(App!, "Error", "La cantidad debe ser mayor que cero", "Ok");
                _cantidadField.SetFocus();
                e.Handled = true;
                return;
            }

           int tipo = _chkVenta.Value  == CheckState.Checked ? 2 :
           _chkAjuste.Value == CheckState.Checked ? 3 :
           1;

            Result = new MovimientoDto(0, producto.Id, tipo, cantidad, DateTime.Now);
            RequestStop();
            e.Handled = true;
        };

        var btnCancelar = new Button {
            Text = "_Cancelar",
            X = Pos.Center() + 2,
            Y = Pos.AnchorEnd(2)
        };
        btnCancelar.Accepting += (_, e) => {
            Result = null;
            RequestStop();
            e.Handled = true;
        };

        Add(lblTipo, _chkCompra, _chkVenta, _chkAjuste,
            lblCantidad, _cantidadField, lblStock,
            btnGuardar, btnCancelar);
    }
}



// ── dtos ────//

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, int Tipo, int Cantidad, DateTime Fecha);