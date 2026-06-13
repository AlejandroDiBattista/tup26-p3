#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ── Arranque de la TUI ────────────────────────────────────────────────────

string apiUrl = args.Length > 0 ? args[0] : "http://localhost:5050";
var api = new CatalogoApiClient(apiUrl);

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(app, api));

// ── Ventana Principal ─────────────────────────────────────────────────────

public sealed class CatalogoWindow : Window 
{
    private readonly IApplication _app;
    private readonly CatalogoApiClient _api;

    private List<Producto> _todosLosProductos = new();
    private List<Producto> _productosFiltrados = new();
    
    private TextField _txtBuscar;
    private ListView _listaProductos;
    private ListView _listaMovimientos;

    public CatalogoWindow(IApplication app, CatalogoApiClient api) 
    {
        _app = app;
        _api = api;
        Title = "Catálogo de Productos - TUI";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        ConstruirInterfaz();
        
        _ = CargarProductosAsync();
    }

    private void ConstruirInterfaz() 
    {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", new MenuItem[] {
                    new MenuItem { Title = "_Salir (Ctrl+Q)", Action = () => _app.RequestStop() }
                }),
                new MenuBarItem("_Productos", new MenuItem[] {
                    new MenuItem { Title = "_Nuevo Producto (F2)", Action = NuevoProducto },
                    new MenuItem { Title = "_Editar Producto (F3)", Action = EditarProducto },
                    new MenuItem { Title = "_Eliminar Producto (Del)", Action = EliminarProducto }
                }),
                new MenuBarItem("_Stock", new MenuItem[] {
                    new MenuItem { Title = "Registrar _Movimiento (F5)", Action = RegistrarMovimiento }
                })
            ]
        };

        var lblBuscar = new Label { Text = "Buscar:", X = 0, Y = 1 };
        _txtBuscar = new TextField { X = 8, Y = 1, Width = Dim.Fill() };
        _txtBuscar.TextChanged += (_, _) => AplicarFiltros();

        var panelIzq = new FrameView { 
            Title = "Productos (Maestro)", X = 0, Y = 2, 
            Width = Dim.Percent(50), Height = Dim.Fill() 
        };
        _listaProductos = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        panelIzq.Add(_listaProductos);

        var panelDer = new FrameView { 
            Title = "Historial (Detalle)", X = Pos.Percent(50), Y = 2, 
            Width = Dim.Fill(), Height = Dim.Fill() 
        };
        _listaMovimientos = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        panelDer.Add(_listaMovimientos);

        Add(menu, lblBuscar, _txtBuscar, panelIzq, panelDer);
    }

    private async Task CargarProductosAsync() 
    {
        try {
            _todosLosProductos = await _api.GetProductosAsync();
            Application.Invoke(AplicarFiltros);
        } catch {
            Application.Invoke(() => MessageBox.ErrorQuery(_app, "Error", "No se pudo conectar al servidor.", "OK"));
        }
    }

    private void AplicarFiltros() 
    {
        var texto = _txtBuscar.Text?.ToString()?.ToLower() ?? "";
        _productosFiltrados = _todosLosProductos.Where(p => 
            string.IsNullOrEmpty(texto) || 
            p.Codigo.ToLower().Contains(texto) || p.Nombre.ToLower().Contains(texto)
        ).ToList();

        _listaProductos.SetSource(new ObservableCollection<string>(
            _productosFiltrados.Select(p => $"[{p.Codigo}] {p.Nombre} - Stock: {p.Stock}")
        ));
        
        _ = CargarDetalleAsync();
    }

    private async Task CargarDetalleAsync() 
    {
        if (_productosFiltrados.Count == 0 || _listaProductos.SelectedItem == null || _listaProductos.SelectedItem < 0) {
            _listaMovimientos.SetSource(new ObservableCollection<string>());
            return;
        }

        var producto = _productosFiltrados[(int)_listaProductos.SelectedItem];
        
        try {
            var movs = await _api.GetMovimientosAsync(producto.Id);
            Application.Invoke(() => {
                _listaMovimientos.SetSource(new ObservableCollection<string>(
                    movs.Select(m => $"{m.Fecha:dd/MM} | {m.Tipo} | Cant: {m.Cantidad}")
                ));
            });
        } catch {
            Application.Invoke(() => _listaMovimientos.SetSource(new ObservableCollection<string> { "Error al cargar." }));
        }
    }

    private async void NuevoProducto() 
    {
        var dialog = new ProductoDialog(_app);
        _app.Run(dialog);
        if (!dialog.Cancelado) {
            await _api.CrearProductoAsync(dialog.ProductoResult);
            await CargarProductosAsync();
        }
    }

    private async void EditarProducto() 
    {
        if (_productosFiltrados.Count == 0 || _listaProductos.SelectedItem == null || _listaProductos.SelectedItem < 0) return;
        
        var dialog = new ProductoDialog(_app, _productosFiltrados[(int)_listaProductos.SelectedItem]);
        _app.Run(dialog);
        if (!dialog.Cancelado) {
            await _api.ActualizarProductoAsync(dialog.ProductoResult);
            await CargarProductosAsync();
        }
    }

    private async void EliminarProducto() 
    {
        if (_productosFiltrados.Count == 0 || _listaProductos.SelectedItem == null || _listaProductos.SelectedItem < 0) return;
        
        var p = _productosFiltrados[(int)_listaProductos.SelectedItem];
        if (MessageBox.Query(_app, "Eliminar", $"¿Eliminar {p.Nombre}?", "Sí", "No") == 0) {
            await _api.EliminarProductoAsync(p.Id);
            await CargarProductosAsync();
        }
    }

    private async void RegistrarMovimiento() 
    {
        if (_productosFiltrados.Count == 0 || _listaProductos.SelectedItem == null || _listaProductos.SelectedItem < 0) {
            MessageBox.ErrorQuery(_app, "Error", "Seleccione un producto.", "OK"); return;
        }
        
        var p = _productosFiltrados[(int)_listaProductos.SelectedItem];
        var dialog = new MovimientoDialog(_app, p);
        _app.Run(dialog);
        if (!dialog.Cancelado) {
            await _api.RegistrarMovimientoAsync(p.Id, dialog.MovimientoResult);
            await CargarProductosAsync();
        }
    }

    protected override bool OnKeyDown(Key key) {
        bool manejado = false;
        if (key == Key.F2) { NuevoProducto(); manejado = true; }
        else if (key == Key.F3) { EditarProducto(); manejado = true; }
        else if (key == Key.DeleteChar) { EliminarProducto(); manejado = true; }
        else if (key == Key.F5) { RegistrarMovimiento(); manejado = true; }
        else if (key == Key.F4) { _txtBuscar.SetFocus(); manejado = true; }
        else manejado = base.OnKeyDown(key);

        if (key == Key.CursorUp || key == Key.CursorDown) _ = CargarDetalleAsync();
        return manejado;
    }
}

// ── Cajas de Diálogo ──────────────────────────────────────────────────────

public class ProductoDialog : Dialog 
{
    private readonly IApplication _app;
    public Producto ProductoResult { get; private set; }
    public bool Cancelado { get; private set; } = true;
    private TextField _txtCod, _txtNom, _txtPre;

    public ProductoDialog(IApplication app, Producto p = null) 
    {
        _app = app;
        Title = p == null ? "Nuevo Producto" : "Editar Producto";
        Width = 50; Height = 10;

        Add(new Label { Text = "Código:", X = 1, Y = 1 });
        Add(_txtCod = new TextField { X = 10, Y = 1, Width = Dim.Fill(), Text = p?.Codigo ?? "" });
        
        Add(new Label { Text = "Nombre:", X = 1, Y = 3 });
        Add(_txtNom = new TextField { X = 10, Y = 3, Width = Dim.Fill(), Text = p?.Nombre ?? "" });
        
        Add(new Label { Text = "Precio:", X = 1, Y = 5 });
        Add(_txtPre = new TextField { X = 10, Y = 5, Width = Dim.Fill(), Text = p?.Precio.ToString() ?? "0" });

        var btnOk = new Button { Text = "Guardar", IsDefault = true };
        btnOk.Accepting += (_, e) => {
            ProductoResult = new Producto {
                Id = p?.Id ?? 0, Codigo = _txtCod.Text.ToString(), 
                Nombre = _txtNom.Text.ToString(), Precio = decimal.Parse(_txtPre.Text.ToString()), Stock = p?.Stock ?? 0
            };
            Cancelado = false; _app.RequestStop(); e.Handled = true;
        };
        var btnCancel = new Button { Text = "Cancelar" };
        btnCancel.Accepting += (_, e) => { _app.RequestStop(); e.Handled = true; };

        AddButton(btnOk); AddButton(btnCancel);
    }
}

public class MovimientoDialog : Dialog 
{
    private readonly IApplication _app;
    public MovimientoDeProducto MovimientoResult { get; private set; }
    public bool Cancelado { get; private set; } = true;

    public MovimientoDialog(IApplication app, Producto p) 
    {
        _app = app;
        Title = $"Movimiento: {p.Nombre}";
        Width = 40; Height = 12;

        Add(new Label { Text = "Tipo:", X = 1, Y = 1 });
        var listaTipos = new ListView { X = 10, Y = 1, Width = 15, Height = 3 };
        listaTipos.SetSource(new ObservableCollection<string>(new[] { "Compra", "Venta", "Ajuste" }));
        Add(listaTipos);

        Add(new Label { Text = "Cant:", X = 1, Y = 5 });
        var txtCant = new TextField { X = 10, Y = 5, Width = Dim.Fill(), Text = "1" };
        Add(txtCant);

        var btnOk = new Button { Text = "Registrar", IsDefault = true };
        btnOk.Accepting += (_, e) => {
            if (listaTipos.SelectedItem == null || listaTipos.SelectedItem < 0) {
                MessageBox.ErrorQuery(_app, "Error", "Debe seleccionar un tipo de movimiento.", "OK");
                e.Handled = true; return;
            }

            MovimientoResult = new MovimientoDeProducto {
                ProductoId = p.Id, 
                Tipo = new[] { "Compra", "Venta", "Ajuste" }[(int)listaTipos.SelectedItem], 
                Cantidad = int.Parse(txtCant.Text.ToString())
            };
            Cancelado = false; _app.RequestStop(); e.Handled = true;
        };
        var btnCancel = new Button { Text = "Cancelar" };
        btnCancel.Accepting += (_, e) => { _app.RequestStop(); e.Handled = true; };

        AddButton(btnOk); AddButton(btnCancel);
    }
}

// ── Cliente HTTP ──────────────────────────────────────────────────────────

public class CatalogoApiClient
{
    private readonly HttpClient _http;

    public CatalogoApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<List<Producto>> GetProductosAsync() =>
        await _http.GetFromJsonAsync<List<Producto>>("/productos") ?? new();

    public async Task CrearProductoAsync(Producto producto) =>
        await _http.PostAsJsonAsync("/productos", producto);

    public async Task ActualizarProductoAsync(Producto producto) =>
        await _http.PutAsJsonAsync($"/productos/{producto.Id}", producto);

    public async Task EliminarProductoAsync(int id) =>
        await _http.DeleteAsync($"/productos/{id}");

    public async Task<List<MovimientoDeProducto>> GetMovimientosAsync(int productoId) =>
        await _http.GetFromJsonAsync<List<MovimientoDeProducto>>($"/productos/{productoId}/movimientos") ?? new();

    public async Task RegistrarMovimientoAsync(int productoId, MovimientoDeProducto movimiento) =>
        await _http.PostAsJsonAsync($"/productos/{productoId}/movimientos", movimiento);
}

// ── Modelos de Datos ──────────────────────────────────────────────────────

public class Producto 
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

public class MovimientoDeProducto 
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = ""; 
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}
