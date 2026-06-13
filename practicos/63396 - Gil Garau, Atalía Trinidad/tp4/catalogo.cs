#:package Terminal.Gui@2.*
#:property PublishAot=false


using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;
using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5050") };

try {
   productos = CargarProductos(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var Buscar = new Label {
    Text = "Buscar:",
    X = 2, Y = 1,
};
var search = new TextField {
    
    X = 10, 
    Y = 1,
    Width = Dim.Percent(35),
};

ListView listaProductos = null!;
Label detalleProducto = null!;
Label movimientosProducto = null!;

listaProductos = new ListView
{
    X = 1,
    Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(2),
};

List<ProductoDto> filtrados = [];
bool actualizandoLista = false;

void AgregarProducto()
{
    var dialog = new ProductoDialog();
    app!.Run(dialog);

    if (!dialog.OK || dialog.Producto is null) return;

    var respuesta = http.PostAsJsonAsync("/productos", dialog.Producto).GetAwaiter().GetResult();
    if (respuesta.IsSuccessStatusCode)
    {
        RecargarProductos();
    }
    else
    {
        MessageBox.ErrorQuery(app!, "Error", "No se pudo agregar el producto. Verifique los datos.", "Aceptar");
    }
}

void EditarProducto()
{
    var actual = ObtenerSeleccionado(listaProductos, filtrados);
    if (actual is null)
    {
        MessageBox.Query(app!, "Error", "No hay un producto seleccionado para editar.", "Aceptar");
        return;
    }

    var dialog = new ProductoDialog(actual);
    app!.Run(dialog);

    if (!dialog.OK || dialog.Producto is null) return;

    var respuesta = http.PutAsJsonAsync($"/productos/{actual.Id}", dialog.Producto).GetAwaiter().GetResult();
    if (respuesta.IsSuccessStatusCode)
    {
        RecargarProductos();
    }
    else
    {
        MessageBox.ErrorQuery(app!, "Error", "No se pudo editar el producto. Verifique los datos.", "Aceptar");
    }
}



var menu = new MenuBar
{
    Menus =
    [
        new MenuBarItem("_Archivo", [
            new MenuItem("_Salir", "Ctrl+Q", Exit, Key.Q.WithCtrl)
        ]),
        new MenuBarItem("_Productos", [
            new MenuItem("_Agregar", "Ctrl+N", AgregarProducto, Key.N.WithCtrl),
            new MenuItem("_Editar", "F3", EditarProducto, Key.F3),
            new MenuItem("_Eliminar", "Ctrl+D", EliminarProducto, Key.D.WithCtrl)
        ]),
        new MenuBarItem("_Movimientos", [
            new MenuItem("_Registrar movimiento", "Ctrl+M", RegistrarMovimiento, Key.M.WithCtrl)
        ])
    ]
};

var panelIzquierdo = new FrameView
{
    Title = "Productos",
    X = Pos.Percent(0),
    Y = Pos.Bottom(search) + 1,
    Width = Dim.Percent(40),
    Height = Dim.Fill(1),
};

var panelDerecho = new FrameView
{
    Title = "Detalles y movimientos del producto",
    X = Pos.Right(panelIzquierdo),
    Y = Pos.Bottom(search) + 1,
    Width = Dim.Percent(60),
    Height = Dim.Fill(1),
};

listaProductos = new ListView
{
    X = 1,
    Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(2),
};

detalleProducto = new Label
{
    Text = """
            # PRODUCTO

            Selecciona un producto de la lista para ver su detalle y movimientos.
            """,
    X = 2,
    Y = 1,
    Width = Dim.Fill(4),
    Height = Dim.Percent(45),
};

movimientosProducto = new Label
{
    Text = """
            # MOVIMIENTOS

            Selecciona un producto de la lista para ver sus movimientos.
            """,
    X = 2,
    Y = Pos.Bottom(detalleProducto) + 1,
    Width = Dim.Fill(4),
    Height = Dim.Fill(2),
};

panelIzquierdo.Add(listaProductos);
panelDerecho.Add(detalleProducto, movimientosProducto);
ventana.Add(menu, Buscar, search, panelIzquierdo, panelDerecho);

void Exit() => app.RequestStop();

void MostrarDetalleActual()
{
    var producto = ObtenerSeleccionado(listaProductos, filtrados);

    if (producto is null)
    {
        detalleProducto.Text = """
                # PRODUCTO

                Selecciona un producto de la lista para ver su detalle y movimientos.
                """;

        movimientosProducto.Text = """
                # MOVIMIENTOS

                Selecciona un producto de la lista para ver sus movimientos.
                """;
        return;
    }

    detalleProducto.Text = $"""
            # PRODUCTO

            - Id     : {producto.Id}
            - Código : {producto.Codigo}
            - Nombre : {producto.Nombre}
            - Precio : ${producto.Precio,10:N2}
            - Stock  :  {producto.Stock,10}
            """;

    var movimientos = CargarMovimientos(http, producto.Id);

    movimientosProducto.Text = movimientos.Count == 0
        ? """
          # MOVIMIENTOS

          No hay movimientos registrados para este producto.
          """
        : $"""
          # MOVIMIENTOS

          {string.Join("\n", movimientos.Select(m => $"- {m.Accion} {m.Cantidad} unidades el {m.Fecha:dd/MM/yyyy HH:mm:ss}"))}
          """;
}

void RefrescarLista()
{
    actualizandoLista = true;

    var texto = search.Text?.ToString()?.Trim() ?? "";

    filtrados = productos
        .Where(p =>
            texto == "" ||
            p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
            p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.Codigo, StringComparer.OrdinalIgnoreCase)
        .ToList();

    listaProductos.SetSource(new ObservableCollection<string>(
        filtrados.Select(p => $"{p.Codigo} - {p.Nombre}  $ {p.Precio:N2}  Stock: {p.Stock}")
    ));

    if (filtrados.Count > 0)
    {
        listaProductos.SelectedItem = 0;
    }

    actualizandoLista = false;
    MostrarDetalleActual();
}

search.TextChanged += (_, _) => RefrescarLista();

RefrescarLista();


listaProductos.ValueChanged += (_, _) =>
{
    if (actualizandoLista) return;
    MostrarDetalleActual();
};

app.Run(ventana);


// funciones auxiliares
static List<ProductoDto> CargarProductos(HttpClient http)
{
    return http
        .GetFromJsonAsync<List<ProductoDto>>("/productos")
        .GetAwaiter()
        .GetResult()
        ?? new List<ProductoDto>();
}

static ProductoDto? ObtenerSeleccionado(ListView lista, List<ProductoDto> productosFiltrados)
{
    if (lista.SelectedItem is not int i) return null;
    if (i < 0 || i >= productosFiltrados.Count) return null;

    return productosFiltrados[i];
}

static List<MovimientosDto> CargarMovimientos(HttpClient http, int id)
{
    return http
        .GetFromJsonAsync<List<MovimientosDto>>($"/productos/{id}/movimientos")
        .GetAwaiter()
        .GetResult()
        ?? [];
}


void EliminarProducto()
{
    var actual = ObtenerSeleccionado(listaProductos, filtrados);
    if (actual is null)
    {
        MessageBox.Query(app!, "Error", "No hay un producto seleccionado para eliminar.", "Aceptar");
        return;
    }

    if (MessageBox.Query(app!, "Confirmar eliminación", $"¿Confirma que desea eliminar el producto '{actual.Nombre}'?", "Sí", "No") != 1)
        return;

    var respuesta = http.DeleteAsync($"/productos/{actual.Id}").GetAwaiter().GetResult();
    if (respuesta.IsSuccessStatusCode)
    {
        RecargarProductos();
    }
    else
    {
        MessageBox.ErrorQuery(app!, "Error", "No se pudo eliminar el producto.", "Aceptar");
    }
}

void RegistrarMovimiento()
{
    var actual = ObtenerSeleccionado(listaProductos, filtrados);
    if (actual is null)
    {
        MessageBox.Query(app!, "Error", "No hay un producto seleccionado para registrar un movimiento.", "Aceptar");
        return;
    }

    var dialog = new MovimientoDialog();
    app!.Run(dialog);

    if (!dialog.OK || dialog.Movimiento is null) return;

    var respuesta = http.PostAsJsonAsync($"/productos/{actual.Id}/movimientos", dialog.Movimiento).GetAwaiter().GetResult();
    if (respuesta.IsSuccessStatusCode)
    {
        RecargarProductos();
    }
    else
    {
        MessageBox.ErrorQuery(app!, "Error", "No se pudo registrar el movimiento. Verifique los datos.", "Aceptar");
    }
}

void RecargarProductos()
{
    productos = CargarProductos(http);
    RefrescarLista();
}

// ── DTO ───────────────────────────────────────────────────────────────────
public record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public record ProductoCrearDto(string Codigo, string Nombre, decimal Precio, int Stock);
public record MovimientosDto(int Id, int ProductoId, string Accion, int Cantidad, DateTime Fecha);
public record MovimientoCrearDto(string Accion, int Cantidad);

public sealed class ProductoDialog : Dialog
{
    readonly TextField codigo = new();
    readonly TextField nombre = new();
    readonly TextField precio = new();
    readonly TextField stock = new();

    public bool OK { get; private set; }
    public ProductoCrearDto? Producto { get; private set; }

    public ProductoDialog(ProductoDto? actual = null)
    {
        Title = actual is null ? "Agregar producto nuevo" : $"Editar producto #{actual.Id}";
        Width = 60;
        Height = 12;

        AddRow("Código:", 1, codigo);
        AddRow("Nombre:", 2, nombre);
        AddRow("Precio:", 3, precio);
        AddRow("Stock:", 4, stock);

        if (actual is not null)
        {
            codigo.Text = actual.Codigo;
            nombre.Text = actual.Nombre;
            precio.Text = actual.Precio.ToString();
            stock.Text = actual.Stock.ToString();
        }

        var guardar = new Button { Text = "_Guardar", IsDefault = true };
        guardar.Accepting += (_, e) => { Guardar(); e.Handled = true; };

        var cancelar = new Button { Text = "_Cancelar" };
        cancelar.Accepting += (_, e) => { e.Handled = true; RequestStop(); };

        AddButton(guardar);
        AddButton(cancelar);
    }

    void AddRow(string texto, int fila, TextField campo)
    {
        Add(new Label { Text = texto, X = 2, Y = fila }, campo);
        campo.X = 12;
        campo.Y = fila;
        campo.Width = Dim.Fill(2);
    }

    void Guardar()
    {
        var c = codigo.Text?.ToString()?.Trim() ?? "";
        var n = nombre.Text?.ToString()?.Trim() ?? "";
        var pTexto = precio.Text?.ToString()?.Trim() ?? "";
        var sTexto = stock.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(c) || string.IsNullOrWhiteSpace(n))
        {
            MessageBox.ErrorQuery(App!, "Error", "El código y el nombre son obligatorios.", "OK");
            return;
        }

        if (!decimal.TryParse(pTexto, out var p) || p <= 0)
        {
            MessageBox.ErrorQuery(App!, "Error", "El precio debe ser mayor a 0.", "OK");
            return;
        }

        if (!int.TryParse(sTexto, out var s) || s < 0)
        {
            MessageBox.ErrorQuery(App!, "Error", "El stock debe ser 0 o mayor.", "OK");
            return;
        }

        Producto = new ProductoCrearDto(c, n, p, s);
        OK = true;
        RequestStop();
    }
}

public sealed class MovimientoDialog : Dialog
{
    readonly TextField accion = new();
    readonly TextField cantidad = new();

    public bool OK { get; private set; }
    public MovimientoCrearDto? Movimiento { get; private set; }

    public MovimientoDialog()
    {
        Title = "Registrar movimiento de producto";
        Width = 60;
        Height = 10;

        AddRow("Acción:", 1, accion);
        AddRow("Cantidad:", 2, cantidad);

        var guardar = new Button { Text = "_Guardar", IsDefault = true };
        guardar.Accepting += (_, e) => { Guardar(); e.Handled = true; };

        var cancelar = new Button { Text = "_Cancelar" };
        cancelar.Accepting += (_, e) => { e.Handled = true; RequestStop(); };

        AddButton(guardar);
        AddButton(cancelar);
    }

    void AddRow(string texto, int fila, TextField campo)
    {
        Add(new Label { Text = texto, X = 2, Y = fila }, campo);
        campo.X = 12;
        campo.Y = fila;
        campo.Width = Dim.Fill(2);
    }

    void Guardar()
    {
        var a = accion.Text?.ToString()?.Trim() ?? "";
        var cTexto = cantidad.Text?.ToString()?.Trim() ?? "";

        if (a != "Compra" && a != "Venta" && a != "Ajuste")
        {
            MessageBox.ErrorQuery(App!, "Error", "La acción debe ser Compra, Venta o Ajuste.", "OK");
            return;
        }

        if (!int.TryParse(cTexto, out var c) || c <= 0)
        {
            MessageBox.ErrorQuery(App!, "Error", "La cantidad debe ser mayor a 0.", "OK");
            return;
        }

        Movimiento = new MovimientoCrearDto(a, c);
        OK = true;
        RequestStop();
    }
}