#!/usr/bin/env dotnet
#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var jsonOpts = new JsonSerializerOptions {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

var http = new HttpClient { BaseAddress = new Uri("http://localhost:5050") };

List<ProductoDto> productos;
try {
    productos = await http.GetFromJsonAsync<List<ProductoDto>>("/productos", jsonOpts) ?? [];
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(http, jsonOpts, productos));

public sealed class CatalogoWindow : Runnable
{
    private readonly HttpClient            _http;
    private readonly JsonSerializerOptions _opts;
    private readonly List<ProductoDto>     _todos    = [];
    private readonly List<ProductoDto>     _filtrado = [];

    private TextField _buscar  = null!;
    private ListView  _lista   = null!;
    private Label     _detalle = null!;
    private ListView  _movs    = null!;

    public CatalogoWindow(HttpClient http, JsonSerializerOptions opts, List<ProductoDto> inicial)
    {
        _http  = http;
        _opts  = opts;
        Title  = " CatalogoREST ";
        Width  = Dim.Fill();
        Height = Dim.Fill();
        _todos.AddRange(inicial);
        BuildLayout();
        AplicarFiltro();
    }

    private void BuildLayout()
    {
        var lblBuscar = new Label { Text = "Buscar:", X = 1, Y = 1 };
        _buscar = new TextField { X = Pos.Right(lblBuscar) + 1, Y = 1, Width = 40 };
        _buscar.TextChanged += (_, _) => AplicarFiltro();

        var frameIzq = new FrameView {
            Title = "Productos",
            X = 0, Y = 3,
            Width = Dim.Percent(46), Height = Dim.Fill(1)
        };
        _lista = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        frameIzq.Add(_lista);

        var frameDer = new FrameView {
            Title = "Detalle",
            X = Pos.Right(frameIzq), Y = 3,
            Width = Dim.Fill(), Height = Dim.Percent(40)
        };
        _detalle = new Label { X = 1, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        frameDer.Add(_detalle);

        var frameMov = new FrameView {
            Title = "Historial de movimientos",
            X = Pos.Right(frameIzq), Y = Pos.Bottom(frameDer),
            Width = Dim.Fill(), Height = Dim.Fill(1)
        };
        _movs = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        frameMov.Add(_movs);

        var statusBar = new StatusBar();
        statusBar.Add(
            new Shortcut { Key = Key.F1,         Title = "F1 Agregar",    Action = AgregarProducto },
            new Shortcut { Key = Key.F2,         Title = "F2 Editar",     Action = EditarProducto },
            new Shortcut { Key = Key.F3,         Title = "F3 Eliminar",   Action = EliminarProducto },
            new Shortcut { Key = Key.F4,         Title = "F4 Movimiento", Action = RegistrarMovimiento },
            new Shortcut { Key = Key.Q.WithCtrl, Title = "^Q Salir",      Action = () => App!.RequestStop() }
        );

        Add(lblBuscar, _buscar, frameIzq, frameDer, frameMov, statusBar);
    }

    private void AplicarFiltro()
    {
        string q = (_buscar?.Text ?? "").Trim();
        _filtrado.Clear();
        foreach (var p in _todos) {
            if (q.Length == 0
                || p.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.Codigo.Contains(q, StringComparison.OrdinalIgnoreCase))
                _filtrado.Add(p);
        }
        _lista.SetSource<string>(new ObservableCollection<string>(
            _filtrado.Select(p => $"{p.Codigo,-8} {p.Nombre,-20} ${p.Precio,8:N2} [{p.Stock,5}u]")
        ));
        MostrarDetalle();
    }

    private void MostrarDetalle()
    {
        int idx = _lista.SelectedItem.GetValueOrDefault(-1);
        if (idx < 0 || idx >= _filtrado.Count) {
            _detalle.Text = "(ningún producto seleccionado)";
            _movs.SetSource<string>(new ObservableCollection<string>());
            return;
        }
        var p = _filtrado[idx];
        _detalle.Text =
            $"Id     : {p.Id}\n" +
            $"Código : {p.Codigo}\n" +
            $"Nombre : {p.Nombre}\n" +
            $"Precio : ${p.Precio:N2}\n" +
            $"Stock  : {p.Stock} unidades";

        Task.Run(async () => {
            try {
                var movs = await _http.GetFromJsonAsync<List<MovimientoDto>>(
                    $"/productos/{p.Id}/movimientos", _opts) ?? [];
                var filas = new ObservableCollection<string>(
                    movs.Select(m => {
                        string s = m.Tipo == TipoMovimiento.Compra ? "+" :
                                   m.Tipo == TipoMovimiento.Venta  ? "-" : "=";
                        return $"{m.Fecha:dd/MM/yy HH:mm}  {m.Tipo,-7}  {s}{m.Cantidad,5} u";
                    })
                );
                App!.Invoke(() => _movs.SetSource<string>(filas));
            } catch { }
        });
    }

    private ProductoDto? ProductoSeleccionado() {
        int idx = _lista.SelectedItem.GetValueOrDefault(-1);
        return (idx >= 0 && idx < _filtrado.Count) ? _filtrado[idx] : null;
    }

    private async Task RecargarProductos() {
        _todos.Clear();
        _todos.AddRange(await _http.GetFromJsonAsync<List<ProductoDto>>("/productos", _opts) ?? []);
        AplicarFiltro();
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.F1) { AgregarProducto();    return true; }
        if (key == Key.F2) { EditarProducto();      return true; }
        if (key == Key.F3) { EliminarProducto();    return true; }
        if (key == Key.F4) { RegistrarMovimiento(); return true; }
        if (key == Key.Q.WithCtrl) { App!.RequestStop(); return true; }
        MostrarDetalle();
        return base.OnKeyDown(key);
    }

    private void AgregarProducto()
    {
        var dlg = new ProductoDialog(App!, "Agregar Producto", new ProductoDto(0,"","",0,0));
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;
        var d = dlg.Resultado!;
        Task.Run(async () => {
            try {
                var r = await _http.PostAsJsonAsync("/productos",
                    new { d.Codigo, d.Nombre, d.Precio, d.Stock });
                r.EnsureSuccessStatusCode();
                await RecargarProductos();
                App!.Invoke(() => MostrarInfo("Listo", "Producto agregado."));
            } catch (Exception ex) {
                App!.Invoke(() => MostrarError("Error", ex.Message));
            }
        });
    }
    private void EditarProducto()
    {
        var sel = ProductoSeleccionado();
        if (sel is null) { MostrarInfo("Aviso", "Seleccioná un producto."); return; }
        var dlg = new ProductoDialog(App!, "Editar Producto", sel);
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;
        var d = dlg.Resultado!;
        Task.Run(async () => {
            try {
                var r = await _http.PutAsJsonAsync($"/productos/{sel.Id}",
                    new { d.Codigo, d.Nombre, d.Precio, d.Stock });
                r.EnsureSuccessStatusCode();
                await RecargarProductos();
                App!.Invoke(() => MostrarInfo("Listo", "Producto modificado."));
            } catch (Exception ex) {
                App!.Invoke(() => MostrarError("Error", ex.Message));
            }
        });
    }

    private void EliminarProducto()
    {
        var sel = ProductoSeleccionado();
        if (sel is null) { MostrarInfo("Aviso", "Seleccioná un producto."); return; }
        int r = (int)(MessageBox.Query(App!, "Confirmar",
            $"¿Eliminar '{sel.Nombre}'?", "Sí", "No") ?? 1);
        if (r != 0) return;
        Task.Run(async () => {
            try {
                var resp = await _http.DeleteAsync($"/productos/{sel.Id}");
                resp.EnsureSuccessStatusCode();
                await RecargarProductos();
                App!.Invoke(() => MostrarInfo("Listo", "Producto eliminado."));
            } catch (Exception ex) {
                App!.Invoke(() => MostrarError("Error", ex.Message));
            }
        });
    }
    private void RegistrarMovimiento()
    {
        var sel = ProductoSeleccionado();
        if (sel is null) { MostrarInfo("Aviso", "Seleccioná un producto."); return; }
        var dlg = new MovimientoDialog(App!, sel.Nombre);
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;
        Task.Run(async () => {
            try {
                var r = await _http.PostAsJsonAsync($"/productos/{sel.Id}/movimientos",
                    new { Tipo = dlg.Tipo.ToString(), dlg.Cantidad });
                r.EnsureSuccessStatusCode();
                await RecargarProductos();
                App!.Invoke(() => MostrarInfo("Listo", "Movimiento registrado."));
            } catch (Exception ex) {
                App!.Invoke(() => MostrarError("Error", ex.Message));
            }
        });
    }

    private void MostrarInfo(string titulo, string msg)
        => MessageBox.Query(App!, titulo, msg, "OK");

    private void MostrarError(string titulo, string msg)
        => MessageBox.ErrorQuery(App!, titulo, msg, "OK");
}

public sealed class ProductoDialog : Dialog
{
    public bool         WasAccepted { get; private set; }
    public ProductoDto? Resultado   { get; private set; }

    private readonly IApplication _app;
    private readonly ProductoDto  _orig;
    private TextField _txtCodigo = null!;
    private TextField _txtNombre = null!;
    private TextField _txtPrecio = null!;
    private TextField _txtStock  = null!;

    public ProductoDialog(IApplication app, string titulo, ProductoDto producto)
    {
        _app   = app;
        _orig  = producto;
        Title  = titulo;
        Width  = 55;
        Height = 14;
        BuildLayout();
    }

    private void BuildLayout()
    {
        Add(new Label { Text = "Código (*):", X = 1, Y = 1 });
        _txtCodigo = new TextField { Text = _orig.Codigo, X = 15, Y = 1, Width = Dim.Fill(2) };
        Add(_txtCodigo);

        Add(new Label { Text = "Nombre (*):", X = 1, Y = 3 });
        _txtNombre = new TextField { Text = _orig.Nombre, X = 15, Y = 3, Width = Dim.Fill(2) };
        Add(_txtNombre);

        Add(new Label { Text = "Precio (*):", X = 1, Y = 5 });
        _txtPrecio = new TextField {
            Text = _orig.Precio == 0 ? "" : _orig.Precio.ToString("F2"),
            X = 15, Y = 5, Width = 18
        };
        Add(_txtPrecio);

        Add(new Label { Text = "Stock (*):", X = 1, Y = 7 });
        _txtStock = new TextField {
            Text = _orig.Stock == 0 ? "" : _orig.Stock.ToString(),
            X = 15, Y = 7, Width = 12
        };
        Add(_txtStock);

        var btnGuardar  = new Button { Text = "_Guardar",  IsDefault = true };
        var btnCancelar = new Button { Text = "_Cancelar" };
        btnGuardar.Accepting  += (_, e) => { Guardar();  e.Handled = true; };
        btnCancelar.Accepting += (_, e) => { Cancelar(); e.Handled = true; };
        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }

    private void Guardar()
    {
        string codigo    = _txtCodigo.Text.Trim();
        string nombre    = _txtNombre.Text.Trim();
        string precioStr = _txtPrecio.Text.Trim();
        string stockStr  = _txtStock.Text.Trim();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre)) {
            MessageBox.ErrorQuery(_app, "Validación", "Código y nombre son obligatorios.", "OK");
            return;
        }
        if (!decimal.TryParse(precioStr, out decimal precio) || precio < 0) {
            MessageBox.ErrorQuery(_app, "Validación", "Precio inválido.", "OK");
            return;
        }
        if (!int.TryParse(stockStr, out int stock) || stock < 0) {
            MessageBox.ErrorQuery(_app, "Validación", "Stock inválido.", "OK");
            return;
        }

        Resultado   = new ProductoDto(_orig.Id, codigo, nombre, precio, stock);
        WasAccepted = true;
        App!.RequestStop();
    }

    private void Cancelar() { WasAccepted = false; App!.RequestStop(); }
}

public sealed class MovimientoDialog : Dialog
{
    public bool           WasAccepted { get; private set; }
    public TipoMovimiento Tipo        { get; private set; } = TipoMovimiento.Compra;
    public int            Cantidad    { get; private set; }

    private readonly IApplication _app;
    private ListView  _rdTipo  = null!;
    private TextField _txtCant = null!;

    public MovimientoDialog(IApplication app, string nombreProducto)
    {
        _app   = app;
        Title  = $"Movimiento — {nombreProducto}";
        Width  = 52;
        Height = 13;
        BuildLayout();
    }

    private void BuildLayout()
    {
        Add(new Label { Text = "Tipo:", X = 1, Y = 1 });
        _rdTipo = new ListView { X = 10, Y = 1, Width = 22, Height = 3 };
        _rdTipo.SetSource<string>(new ObservableCollection<string>(
            ["Compra  (+stock)", "Venta   (-stock)", "Ajuste  (=stock)"]
        ));
        _rdTipo.SelectedItem = 0;
        Add(_rdTipo);

        Add(new Label { Text = "Cantidad:", X = 1, Y = 6 });
        _txtCant = new TextField { Text = "1", X = 12, Y = 6, Width = 12 };
        Add(_txtCant);

        var btnOk  = new Button { Text = "_Aceptar",  IsDefault = true };
        var btnCan = new Button { Text = "_Cancelar" };
        btnOk.Accepting  += (_, e) => { Guardar();  e.Handled = true; };
        btnCan.Accepting += (_, e) => { Cancelar(); e.Handled = true; };
        AddButton(btnOk);
        AddButton(btnCan);
    }

    private void Guardar()
    {
        if (!int.TryParse(_txtCant.Text.Trim(), out int cant) || cant <= 0) {
            MessageBox.ErrorQuery(_app, "Validación", "La cantidad debe ser un entero positivo.", "OK");
            return;
        }
        Tipo = _rdTipo.SelectedItem.GetValueOrDefault(0) switch {
            1 => TipoMovimiento.Venta,
            2 => TipoMovimiento.Ajuste,
            _ => TipoMovimiento.Compra,
        };
        Cantidad    = cant;
        WasAccepted = true;
        App!.RequestStop();
    }

    private void Cancelar() { WasAccepted = false; App!.RequestStop(); }
}

public enum TipoMovimiento { Compra, Venta, Ajuste }

public record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);