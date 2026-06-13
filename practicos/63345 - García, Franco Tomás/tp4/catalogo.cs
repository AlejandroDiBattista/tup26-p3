#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;

using var http = new HttpClient();

// ───────────────────────── DATA ─────────────────────────

List<ProductoDto> productos =
    await http.GetFromJsonAsync<List<ProductoDto>>("http://localhost:5050/productos")
    ?? new();

List<MovimientoDto> movimientos = new();

// ───────────────────────── APP ─────────────────────────

using IApplication app = Application.Create().Init();
using Window win = new() { Title = "CatalogoREST TP4 - VERSION FINAL" };

// ───────────────────────── UI DATA ─────────────────────────

var prodData = new ObservableCollection<string>();
var movData = new ObservableCollection<string>();

var lista = new ListView()
{
    X = 0,
    Y = 1,
    Width = 60,
    Height = 20
};

var detalle = new Label()
{
    X = 62,
    Y = 1,
    Width = 60,
    Height = 10
};

var inputCantidad = new TextField()
{
    X = 62,
    Y = 12,
    Width = 10,
    Text = "1"
};

var movList = new ListView()
{
    X = 62,
    Y = 15,
    Width = 60,
    Height = 10
};

lista.SetSource<string>(prodData);
movList.SetSource<string>(movData);

// ───────────────────────── MENU ─────────────────────────

var menu = new MenuBar(new[]
{
    new MenuBarItem("_Productos", new[]
    {
        new MenuItem("Agregar", "", () => FormProducto(null)),
        new MenuItem("Editar", "", () => EditarProducto()),
        new MenuItem("Eliminar", "", () => EliminarProducto())
    }),
    new MenuBarItem("_Movimientos", new[]
    {
        new MenuItem("Compra", "", async () => await Movimiento("Compra")),
        new MenuItem("Venta", "", async () => await Movimiento("Venta")),
        new MenuItem("Ajuste", "", async () => await Movimiento("Ajuste"))
    })
});

// ───────────────────────── RENDER ─────────────────────────

void RenderProductos()
{
    prodData.Clear();

    foreach (var p in productos)
        prodData.Add($"{p.Codigo} - {p.Nombre} | ${p.Precio} | Stock:{p.Stock}");
}

void RenderMovimientos()
{
    movData.Clear();

    foreach (var m in movimientos)
        movData.Add($"{m.Tipo} {m.Cantidad}");
}


async Task Update()
{
    if (!lista.SelectedItem.HasValue) return;

    var p = productos[lista.SelectedItem.Value];

    detalle.Text =
        $"ID: {p.Id}\n" +
        $"Codigo: {p.Codigo}\n" +
        $"Nombre: {p.Nombre}\n" +
        $"Precio: {p.Precio}\n" +
        $"Stock: {p.Stock}";

    movimientos =
        await http.GetFromJsonAsync<List<MovimientoDto>>(
            $"http://localhost:5050/productos/{p.Id}/movimientos"
        ) ?? new();

    RenderMovimientos();
}

// ───────────────────────── MOVIMIENTOS ─────────────────────────

async Task Movimiento(string tipo)
{
    if (!lista.SelectedItem.HasValue) return;

    var p = productos[lista.SelectedItem.Value];

    if (!int.TryParse(inputCantidad.Text.ToString(), out int cant))
        return;

    await http.PostAsJsonAsync(
        $"http://localhost:5050/productos/{p.Id}/movimientos",
        new MovimientoDto(tipo, cant)
    );

    await Refrescar();
}

// ───────────────────────── CRUD PRODUCTOS ─────────────────────────

async Task Refrescar()
{
    productos =
        await http.GetFromJsonAsync<List<ProductoDto>>("http://localhost:5050/productos")
        ?? new();

    RenderProductos();
    await Update();
}

async Task EliminarProducto()
{
    if (!lista.SelectedItem.HasValue) return;

    var p = productos[lista.SelectedItem.Value];

    await http.DeleteAsync($"http://localhost:5050/productos/{p.Id}");

    await Refrescar();
}

// ───────────────────────── FORM PRODUCTO ─────────────────────────

void FormProducto(ProductoDto? edit)
{
    var w = new Window()
    {
        Title = edit == null ? "Nuevo Producto" : "Editar Producto",
        Width = 60,
        Height = 12,
        X = 10,
        Y = 5
    };

    var txtCodigo = new TextField() { X = 1, Y = 1, Width = 20, Text = edit?.Codigo ?? "" };
    var txtNombre = new TextField() { X = 1, Y = 3, Width = 20, Text = edit?.Nombre ?? "" };
    var txtPrecio = new TextField() { X = 1, Y = 5, Width = 20, Text = edit?.Precio.ToString() ?? "0" };
    var txtStock  = new TextField() { X = 1, Y = 7, Width = 20, Text = edit?.Stock.ToString() ?? "0" };

    var info = new Label()
    {
        X = 1,
        Y = 9,
        Text = "ENTER = Guardar | ESC = Cancelar"
    };

    w.Add(txtCodigo, txtNombre, txtPrecio, txtStock, info);

w.Accepting += async (sender, args) =>
{
    var prod = new ProductoDto(
        edit?.Id ?? 0,
        txtCodigo.Text.ToString(),
        txtNombre.Text.ToString(),
        decimal.Parse(txtPrecio.Text.ToString()),
        int.Parse(txtStock.Text.ToString())
    );

    if (edit == null)
        await http.PostAsJsonAsync("http://localhost:5050/productos", prod);
    else
        await http.PutAsJsonAsync($"http://localhost:5050/productos/{edit.Id}", prod);

    win.Remove(w);

    args.Handled = true;

    await Refrescar();
};

    win.Add(w);
}


void EditarProducto()
{
    if (!lista.SelectedItem.HasValue) return;

    var p = productos[lista.SelectedItem.Value];
    FormProducto(p);
}

app.AddTimeout(TimeSpan.FromMilliseconds(200), () =>
{
    _ = Update();
    return true;
});

// ───────────────────────── UI ─────────────────────────

win.Add(menu, lista, detalle, movList, inputCantidad);

RenderProductos();

app.Run(win);

// ───────────────────────── DTO ─────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(string Tipo, int Cantidad);