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

Window.Add(detalleProducto);

app.Run(Window);

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5000/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

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

    private void AgregarProducto() {

        using var dialog = new ProductoDialog();
        App!.Run(dialog);

        if (dialog.Result is null) {
            return;
        }

        try {
            var producto = _api.CrearProductoAsync(dialog.Result).GetAwaiter().GetResult();
            CargarProductos();
            AplicarFiltro(producto.Id);
            Estado($"Producto agregado: {producto.Codigo}");

        } 
        catch (Exception ex) {

            MostrarError("No se pudo agregar el producto", ex);
        }
    }
    
    private void ModificarProducto() {

        var seleccionado = ProductoSeleccionado();
        if (seleccionado is null) {
            Aviso("Selecciona un producto para modificar");
            return;
        }

        using var dialog = new ProductoDialog(seleccionado);
        App!.Run(dialog);

        if (dialog.Result is null) {
            return;
        }

        try {
            var producto = _api.ModificarProductoAsync(seleccionado.Id, dialog.Result).GetAwaiter().GetResult();
            CargarProductos();
            AplicarFiltro(producto.Id);
            Estado($"Producto modificado: {producto.Codigo}");

        } 
        catch (Exception ex) {

            MostrarError("No se pudo modificar el producto", ex);
        }
    }

    private void EliminarProducto() {

        var seleccionado = ProductoSeleccionado();
        if (seleccionado is null) {
            Aviso("Selecciona un producto para eliminar");
            return;
        }

        var respuesta = MessageBox.Query(App!, "Eliminar", $"Eliminar {seleccionado.Codigo} - {seleccionado.Nombre}?", "No", "Sí");

        if (respuesta != 1) {

            return;
        }

        try {
            _api.EliminarProductoAsync(seleccionado.Id).GetAwaiter().GetResult();
            CargarProductos();
            Estado($"Producto eliminado: {seleccionado.Codigo}");

        } 
        catch (Exception ex) {

            MostrarError("No se pudo eliminar el producto", ex);
        }
    }

    private void RegistrarMovimiento() {

        var seleccionado = ProductoSeleccionado();
        if (seleccionado is null) {
            Aviso("Selecciona un producto para registrar un movimiento");
            return;
        }

        using var dialog = new MovimientoDialog(seleccionado);
        App!.Run(dialog);

        if (dialog.Result is null) {
            return;
        }

        try {
            _api.RegistrarMovimientoAsync(seleccionado.Id, dialog.Result).GetAwaiter().GetResult();
            CargarMovimientos();
            AplicarFiltro(seleccionado.Id);
            Estado($"Movimiento registrado para {seleccionado.Codigo}");

        } 
        catch (Exception ex) {

            MostrarError("No se pudo registrar el movimiento", ex);
        }
    }

    private void Aviso(string mensaje) {

        MessageBox.Query(App!, "Catalogo", mensaje, "Ok");
    }

    private void MostrarError(string mensaje, Exception ex) {

        MessageBox.ErrorQuery(App!, "Error", $"{mensaje}\n\n{LimpiarMensaje(ex)}", "Ok");
        Estado("Error: " + LimpiarMensaje(ex));
    }

    private void Estado(string mensaje) {

        _estado.Text = mensaje;
        _estado.SetNeedsDraw();
    }

    private static string LimpiarMensaje(Exception ex) {

        return ex is ApiException apiException ? apiException.Message : ex.GetBaseException().Message;

    }

    private static string FormatearProducto(ProductoDto producto) {

        return $"{producto.Codigo,-12} {Recortar(producto.Nombre, 32),-32} ${producto.Precio,10:0.00} Stock: {producto.Stock,5}";
    }

    private static string FormatearMovimiento(MovimientoDto movimiento) {

        var fecha = movimiento.Fecha.ToString("dd/MM/yyyy HH:mm",CultureInfo.InvariantCulture);
        return $"{fecha} | {movimiento.Tipo,-7} | Cantidad: {movimiento.Cantidad,6}";
    }

    private static string Recortar(string valor, int ancho) {

        if (valor.Length <= ancho) {

            return valor;
        }
        return valor[.. Math.Max(0, ancho - 3)] + "...";
    }
}

public sealed class ProductoDialog : Dialog<ProductoRequest>
{
    private readonly TextField _codigo;
    private readonly TextField _nombre;
    private readonly TextField _precio;
    private readonly TextField _stock;

    public ProductoDialog(ProductoDto? producto = null)
    {
        Title = producto is null ? "Agregar producto" : "Modificar producto";
        Width = 64;
        Height = 14;

        var codigoLabel = new Label { Text = "Codigo:", X = 1, Y = 1 };
        _codigo = new TextField { Text = producto?.Codigo ?? "", X = 12, Y = 1, Width = Dim.Fill(2) };

        var nombreLabel = new Label { Text = "Nombre:", X = 1, Y = 3 };
        _nombre = new TextField { Text = producto?.Nombre ?? "", X = 12, Y = 3, Width = Dim.Fill(2) };

        var precioLabel = new Label { Text = "Precio:", X = 1, Y = 5 };
        _precio = new TextField { Text = (producto?.Precio ?? 0m).ToString("0.00", CultureInfo.InvariantCulture), X = 12, Y = 5, Width = 16 };

        var stockLabel = new Label { Text = "Stock:", X = 1, Y = 7 };
        _stock = new TextField { Text = (producto?.Stock ?? 0).ToString(CultureInfo.InvariantCulture), X = 12, Y = 7, Width = 16 };

        Add(codigoLabel, _codigo, nombreLabel, _nombre, precioLabel, _precio, stockLabel, _stock);

        AddButton(new Button { Text = "_Cancelar" });

        var guardar = new Button { Text = "_Guardar", IsDefault = true };
        guardar.Accepting += (_, e) =>
        {
            if (!TryCrearRequest(out var request, out var error))
            {
                MessageBox.ErrorQuery(App!, "Datos invalidos", error, "Ok");
                e.Handled = true;
                return;
            }

            Result = request;
            RequestStop();
            e.Handled = true;
        };
        AddButton(guardar);
    }

    private bool TryCrearRequest(out ProductoRequest request, out string error)
    {
        request = new ProductoRequest("", "", 0m, 0);
        error = "";

        var codigo = (_codigo.Text?.ToString() ?? "").Trim();
        var nombre = (_nombre.Text?.ToString() ?? "").Trim();

        if (codigo.Length == 0)
        {
            error = "El codigo es obligatorio.";
            return false;
        }

        if (nombre.Length == 0)
        {
            error = "El nombre es obligatorio.";
            return false;
        }

        if (!decimal.TryParse((_precio.Text?.ToString() ?? "").Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var precio) || precio < 0)
        {
            error = "El precio debe ser un numero mayor o igual a cero.";
            return false;
        }

        if (!int.TryParse(_stock.Text?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock) || stock < 0)
        {
            error = "El stock debe ser un entero mayor o igual a cero.";
            return false;
        }

        request = new ProductoRequest(codigo, nombre, precio, stock);
        return true;
    }
}

public sealed class MovimientoDialog : Dialog<MovimientoRequest>
{
    private readonly OptionSelector<TipoMovimiento> _tipo;
    private readonly TextField _cantidad;

    public MovimientoDialog(ProductoDto producto)
    {
        Title = $"Movimiento - {producto.Codigo}";
        Width = 62;
        Height = 13;

        Add(new Label
        {
            Text = $"{producto.Nombre} | Stock actual: {producto.Stock}",
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        });

        Add(new Label { Text = "Tipo:", X = 1, Y = 3 });
        _tipo = new OptionSelector<TipoMovimiento>
        {
            X = 12,
            Y = 3,
            Value = TipoMovimiento.Compra
        };

        Add(new Label { Text = "Cantidad:", X = 1, Y = 7 });
        _cantidad = new TextField { Text = "1", X = 12, Y = 7, Width = 16 };

        Add(_tipo, _cantidad);
        AddButton(new Button { Text = "_Cancelar" });

        var aceptar = new Button { Text = "_Registrar", IsDefault = true };
        aceptar.Accepting += (_, e) =>
        {
            if (!int.TryParse(_cantidad.Text?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var cantidad) || cantidad <= 0)
            {
                MessageBox.ErrorQuery(App!, "Datos invalidos", "La cantidad debe ser un entero positivo.", "Ok");
                e.Handled = true;
                return;
            }

            var tipo = _tipo.Value ?? TipoMovimiento.Compra;

            Result = new MovimientoRequest(tipo, cantidad);
            RequestStop();
            e.Handled = true;
        };
        AddButton(aceptar);
    }
}

public sealed class CatalogoApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CatalogoApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<List<ProductoDto>> ListarProductosAsync()
    {
        return await LeerAsync<List<ProductoDto>>(await _http.GetAsync("/productos")) ?? [];
    }

    public async Task<ProductoDto> CrearProductoAsync(ProductoRequest request)
    {
        return await LeerAsync<ProductoDto>(await _http.PostAsJsonAsync("/productos", request, _jsonOptions))
            ?? throw new ApiException("El servidor no devolvio el producto creado.");
    }

    public async Task<ProductoDto> ModificarProductoAsync(int id, ProductoRequest request)
    {
        return await LeerAsync<ProductoDto>(await _http.PutAsJsonAsync($"/productos/{id}", request, _jsonOptions))
            ?? throw new ApiException("El servidor no devolvio el producto modificado.");
    }

    public async Task EliminarProductoAsync(int id)
    {
        await LeerAsync<object>(await _http.DeleteAsync($"/productos/{id}"));
    }

    public async Task<List<MovimientoDto>> ListarMovimientosAsync(int productoId)
    {
        return await LeerAsync<List<MovimientoDto>>(await _http.GetAsync($"/productos/{productoId}/movimientos")) ?? [];
    }

    public async Task RegistrarMovimientoAsync(int productoId, MovimientoRequest request)
    {
        await LeerAsync<MovimientoDto>(await _http.PostAsJsonAsync($"/productos/{productoId}/movimientos", request, _jsonOptions));
    }

    private async Task<T?> LeerAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }

        var cuerpo = await response.Content.ReadAsStringAsync();
        var mensaje = ExtraerError(cuerpo);
        throw new ApiException(string.IsNullOrWhiteSpace(mensaje)
            ? $"{(int)response.StatusCode} {response.ReasonPhrase}"
            : mensaje);
    }

    private string ExtraerError(string cuerpo)
    {
        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            return "";
        }

        try
        {
            var error = JsonSerializer.Deserialize<ApiError>(cuerpo, _jsonOptions);
            return error?.Error ?? cuerpo;
        }
        catch
        {
            return cuerpo;
        }
    }
}

public sealed class ApiException(string message) : Exception(message);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

public sealed record ProductoRequest(string Codigo, string Nombre, decimal Precio, int Stock);
public sealed record MovimientoRequest(TipoMovimiento Tipo, int Cantidad);
public sealed record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public sealed record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
public sealed record ApiError(string Error);


