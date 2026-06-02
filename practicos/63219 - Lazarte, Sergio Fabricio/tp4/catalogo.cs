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

    private void AgregarProducto()    { }
    private void EditarProducto()     { }
    private void EliminarProducto()   { }
    private void RegistrarMovimiento(){ }

    private void MostrarInfo(string titulo, string msg)
        => MessageBox.Query(App!, titulo, msg, "OK");

    private void MostrarError(string titulo, string msg)
        => MessageBox.ErrorQuery(App!, titulo, msg, "OK");
}

public enum TipoMovimiento { Compra, Venta, Ajuste }

public record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);