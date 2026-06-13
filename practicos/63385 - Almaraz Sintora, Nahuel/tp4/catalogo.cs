#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
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

    private void AgregarProducto() {
        ProductoDialog dialogo = new("Agregar producto", null);
        App!.Run(dialogo);
        if (dialogo.Resultado is null) return;
        var respuesta = http.PostAsJsonAsync("productos", dialogo.Resultado).GetAwaiter().GetResult();
        if (!RespuestaCorrecta(respuesta)) return;
        RecargarProductos();
    }

    private void ModificarProducto() {
        var producto = ProductoSeleccionado();
        if (producto is null) { MostrarMensaje("Aviso", "Selecciona un producto."); return; }
        ProductoDialog dialogo = new("Modificar producto", producto);
        App!.Run(dialogo);
        if (dialogo.Resultado is null) return;
        var respuesta = http.PutAsJsonAsync($"productos/{producto.Id}", dialogo.Resultado).GetAwaiter().GetResult();
        if (!RespuestaCorrecta(respuesta)) return;
        RecargarProductos();
    }

    private void EliminarProducto() {
        var producto = ProductoSeleccionado();
        if (producto is null) { MostrarMensaje("Aviso", "Selecciona un producto."); return; }
        ConfirmDialog confirmar = new("Eliminar producto", $"Eliminar {producto.Codigo} - {producto.Nombre}?");
        App!.Run(confirmar);
        if (!confirmar.Confirmado) return;
        var respuesta = http.DeleteAsync($"productos/{producto.Id}").GetAwaiter().GetResult();
        if (!RespuestaCorrecta(respuesta)) return;
        RecargarProductos();
    }

    private void RegistrarMovimiento(string tipo) {
        var producto = ProductoSeleccionado();
        if (producto is null) { MostrarMensaje("Aviso", "Selecciona un producto."); return; }
        MovimientoDialog dialogo = new(tipo, producto);
        App!.Run(dialogo);
        if (dialogo.Resultado is null) return;
        var respuesta = http.PostAsJsonAsync($"productos/{producto.Id}/movimientos", dialogo.Resultado).GetAwaiter().GetResult();
        if (!RespuestaCorrecta(respuesta)) return;
        RecargarProductos();
    }

    private bool RespuestaCorrecta(HttpResponseMessage respuesta) {
        if (respuesta.IsSuccessStatusCode) return true;
        string error = respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        MostrarMensaje("Error", string.IsNullOrWhiteSpace(error) ? "La operacion no se pudo completar." : error);
        return false;
    }
}

public sealed class ProductoDialog : Dialog {
    public ProductoDatos? Resultado { get; private set; }
    private readonly TextField codigo = new(), nombre = new(), precio = new(), stock = new();

    public ProductoDialog(string titulo, ProductoDto? producto) {
        Title = titulo; Width = 60; Height = 13;
        codigo.Text = producto?.Codigo ?? ""; nombre.Text = producto?.Nombre ?? "";
        precio.Text = producto?.Precio.ToString(CultureInfo.CurrentCulture) ?? "0";
        stock.Text = producto?.Stock.ToString() ?? "0";
        codigo.X = 12; codigo.Y = 1; codigo.Width = 35; nombre.X = 12; nombre.Y = 3; nombre.Width = 35;
        precio.X = 12; precio.Y = 5; precio.Width = 35; stock.X = 12; stock.Y = 7; stock.Width = 35;

        Add(new Label { Text = "Codigo:", X = 2, Y = 1 }, codigo, new Label { Text = "Nombre:", X = 2, Y = 3 }, nombre, new Label { Text = "Precio:", X = 2, Y = 5 }, precio, new Label { Text = "Stock:", X = 2, Y = 7 }, stock);

        Button guardar = new() { Text = "_Guardar", IsDefault = true };
        guardar.Accepting += (_, e) => { if (Guardar()) App!.RequestStop(); e.Handled = true; };
        Button cancelar = new() { Text = "_Cancelar" };
        cancelar.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        AddButton(guardar); AddButton(cancelar);
    }

    private bool Guardar() {
        string codigoValor = codigo.Text.ToString().Trim(); string nombreValor = nombre.Text.ToString().Trim();
        if (codigoValor == "" || nombreValor == "") { MostrarError("Codigo y nombre son obligatorios."); return false; }
        if (!LeerDecimal(precio.Text.ToString(), out decimal precioValor) || precioValor < 0) { MostrarError("El precio debe ser un numero mayor o igual a cero."); return false; }
        if (!int.TryParse(stock.Text.ToString(), out int stockValor) || stockValor < 0) { MostrarError("El stock debe ser un numero entero mayor o igual a cero."); return false; }
        Resultado = new ProductoDatos(codigoValor, nombreValor, precioValor, stockValor); return true;
    }

    private static bool LeerDecimal(string texto, out decimal valor) => decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out valor) || decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    private void MostrarError(string mensaje) => App!.Run(new MensajeDialog("Dato invalido", mensaje));
}

public sealed class MovimientoDialog : Dialog {
    public MovimientoDatos? Resultado { get; private set; }
    private readonly string tipo; private readonly TextField cantidad = new();

    public MovimientoDialog(string tipo, ProductoDto producto) {
        this.tipo = tipo; Title = $"Registrar {tipo.ToLowerInvariant()}"; Width = 62; Height = 10;
        string textoCantidad = tipo == "Ajuste" ? "Nuevo stock:" : "Cantidad:";
        string ayuda = tipo == "Compra" ? "La compra aumenta el stock." : tipo == "Venta" ? "La venta disminuye el stock." : "El ajuste deja el stock en el valor indicado.";
        cantidad.Text = "1"; cantidad.X = 16; cantidad.Y = 4; cantidad.Width = 20;

        Add(new Label { Text = $"{producto.Codigo} - {producto.Nombre}", X = 2, Y = 1 }, new Label { Text = $"Stock actual: {producto.Stock}", X = 2, Y = 2 }, new Label { Text = ayuda, X = 2, Y = 3 }, new Label { Text = textoCantidad, X = 2, Y = 4 }, cantidad);

        Button guardar = new() { Text = "_Guardar", IsDefault = true };
        guardar.Accepting += (_, e) => { if (Guardar()) App!.RequestStop(); e.Handled = true; };
        Button cancelar = new() { Text = "_Cancelar" };
        cancelar.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        AddButton(guardar); AddButton(cancelar);
    }

    private bool Guardar() {
        if (!int.TryParse(cantidad.Text.ToString(), out int cantidadValor)) { MostrarError("La cantidad debe ser un numero entero."); return false; }
        if (cantidadValor < 0 || (tipo != "Ajuste" && cantidadValor == 0)) { MostrarError("La cantidad debe ser positiva."); return false; }
        Resultado = new MovimientoDatos(tipo, cantidadValor); return true;
    }
    private void MostrarError(string mensaje) => App!.Run(new MensajeDialog("Dato invalido", mensaje));
}

public sealed class ConfirmDialog : Dialog {
    public bool Confirmado { get; private set; }
    public ConfirmDialog(string titulo, string mensaje) {
        Title = titulo; Width = 62; Height = 8; Add(new Label { Text = mensaje, X = 2, Y = 2 });
        Button si = new() { Text = "_Si", IsDefault = true };
        si.Accepting += (_, e) => { Confirmado = true; App!.RequestStop(); e.Handled = true; };
        Button no = new() { Text = "_No" };
        no.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        AddButton(si); AddButton(no);
    }
}

public sealed class MensajeDialog : Dialog {
    public MensajeDialog(string titulo, string mensaje) {
        Title = titulo; Width = 70; Height = 8; Add(new Label { Text = mensaje, X = 2, Y = 2 });
        Button cerrar = new() { Text = "_Cerrar", IsDefault = true };
        cerrar.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        AddButton(cerrar);
    }
}
public record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
public record ProductoDatos(string Codigo, string Nombre, decimal Precio, int Stock);
public record MovimientoDatos(string Tipo, int Cantidad);
