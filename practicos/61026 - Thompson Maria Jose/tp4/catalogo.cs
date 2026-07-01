#:sdk Microsoft.NET.Sdk
#:package Terminal.Gui@1.15.1
#:property AssemblyName=tp4-catalogo

using System.Net.Http.Json;
using Terminal.Gui;

Application.Init();

var http = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5000/")
};

var productosVisibles = new List<Producto>();
var productoSeleccionado = new Producto();

var top = Application.Top;
var window = new Window("Administracion de Catalogo (TP4)")
{
    X = 0,
    Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var buscarLabel = new Label("Buscar:")
{
    X = 1,
    Y = 1
};

var buscarText = new TextField("")
{
    X = 9,
    Y = 1,
    Width = Dim.Percent(40)
};

var productosFrame = new FrameView("Productos")
{
    X = 1,
    Y = 3,
    Width = Dim.Percent(50),
    Height = Dim.Fill() - 1
};

var productosList = new ListView
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

productosFrame.Add(productosList);

var movimientosFrame = new FrameView("Historial de Movimientos")
{
    X = Pos.Right(productosFrame) + 1,
    Y = 3,
    Width = Dim.Fill() - 1,
    Height = Dim.Fill() - 1
};

var movimientosList = new ListView
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

movimientosFrame.Add(movimientosList);
window.Add(buscarLabel, buscarText, productosFrame, movimientosFrame);

top.Add(window);
top.Add(new MenuBar(new MenuBarItem[]
{
    new("_Archivo", new MenuItem[]
    {
        new("_Salir", string.Empty, () => Application.RequestStop())
    }),
    new("_Productos", new MenuItem[]
    {
        new("_Agregar", "F2", () => MostrarDialogoProducto()),
        new("_Editar", "F3", () =>
        {
            if (productoSeleccionado.Id != 0)
            {
                MostrarDialogoProducto(productoSeleccionado);
            }
        }),
        new("_Eliminar", "F4", async () =>
        {
            if (productoSeleccionado.Id == 0)
            {
                return;
            }

            await http.DeleteAsync($"productos/{productoSeleccionado.Id}");
            productoSeleccionado = new Producto();
            movimientosList.SetSource(Array.Empty<string>());
            await CargarProductos(buscarText.Text.ToString() ?? string.Empty);
        })
    }),
    new("_Movimientos", new MenuItem[]
    {
        new("_Registrar", "F5", RegistrarMovimiento)
    })
}));

buscarText.TextChanged += async _ =>
{
    await CargarProductos(buscarText.Text.ToString() ?? string.Empty);
};

productosList.SelectedItemChanged += async args =>
{
    if (args.Item < 0 || args.Item >= productosVisibles.Count)
    {
        productoSeleccionado = new Producto();
        movimientosList.SetSource(Array.Empty<string>());
        return;
    }

    productoSeleccionado = productosVisibles[args.Item];
    await CargarMovimientos(productoSeleccionado.Id);
};

_ = Task.Run(async () => await CargarProductos());

Application.Run();
Application.Shutdown();

async Task CargarProductos(string filtro = "")
{
    try
    {
        var productos = await http.GetFromJsonAsync<List<Producto>>("productos") ?? new List<Producto>();

        productosVisibles = productos
            .Where(producto =>
                producto.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                producto.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            .ToList();

        productosList.SetSource(productosVisibles
            .Select(producto => $"[{producto.Codigo}] {producto.Nombre} - ${producto.Precio:F2} (Stock: {producto.Stock})")
            .ToList());

        if (productoSeleccionado.Id != 0)
        {
            productoSeleccionado = productosVisibles.FirstOrDefault(producto => producto.Id == productoSeleccionado.Id) ?? new Producto();
        }

        if (productoSeleccionado.Id == 0)
        {
            movimientosList.SetSource(Array.Empty<string>());
        }
    }
    catch (Exception ex)
    {
        MessageBox.ErrorQuery("Error", $"No se pudieron cargar los productos.\n{ex.Message}", "Aceptar");
    }
}

async Task CargarMovimientos(int productoId)
{
    try
    {
        var movimientos = await http.GetFromJsonAsync<List<Movimiento>>($"productos/{productoId}/movimientos") ?? new List<Movimiento>();

        movimientosList.SetSource(movimientos
            .Select(movimiento => $"{movimiento.Fecha:dd/MM/yyyy HH:mm} | {movimiento.Tipo} | Cant: {movimiento.Cantidad}")
            .ToList());
    }
    catch (Exception ex)
    {
        movimientosList.SetSource(Array.Empty<string>());
        MessageBox.ErrorQuery("Error", $"No se pudieron cargar los movimientos.\n{ex.Message}", "Aceptar");
    }
}

void MostrarDialogoProducto(Producto? producto = null)
{
    var dialog = new Dialog(producto is null ? "Agregar Producto" : "Editar Producto", 60, 14);

    var codigoLabel = new Label("Codigo:") { X = 1, Y = 1 };
    var codigoText = new TextField(producto?.Codigo ?? string.Empty) { X = 12, Y = 1, Width = 44 };
    var nombreLabel = new Label("Nombre:") { X = 1, Y = 3 };
    var nombreText = new TextField(producto?.Nombre ?? string.Empty) { X = 12, Y = 3, Width = 44 };
    var precioLabel = new Label("Precio:") { X = 1, Y = 5 };
    var precioText = new TextField(producto?.Precio.ToString("0.##") ?? "0") { X = 12, Y = 5, Width = 44 };

    dialog.Add(codigoLabel, codigoText, nombreLabel, nombreText, precioLabel, precioText);

    var guardarButton = new Button("Guardar", is_default: true);
    guardarButton.Clicked += async () =>
    {
        if (!decimal.TryParse(precioText.Text.ToString(), out var precio))
        {
            MessageBox.ErrorQuery("Validacion", "El precio debe ser numerico.", "Aceptar");
            return;
        }

        var request = new ProductoRequest
        {
            Codigo = codigoText.Text.ToString() ?? string.Empty,
            Nombre = nombreText.Text.ToString() ?? string.Empty,
            Precio = precio
        };

        HttpResponseMessage response = producto is null
            ? await http.PostAsJsonAsync("productos", request)
            : await http.PutAsJsonAsync($"productos/{producto.Id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            MessageBox.ErrorQuery("Error", string.IsNullOrWhiteSpace(error) ? "No se pudo guardar el producto." : error, "Aceptar");
            return;
        }

        Application.RequestStop();
        await CargarProductos(buscarText.Text.ToString() ?? string.Empty);
    };

    var cancelarButton = new Button("Cancelar");
    cancelarButton.Clicked += () => Application.RequestStop();

    dialog.AddButton(guardarButton);
    dialog.AddButton(cancelarButton);
    Application.Run(dialog);
}

void RegistrarMovimiento()
{
    if (productoSeleccionado.Id == 0)
    {
        MessageBox.ErrorQuery("Validacion", "Seleccione un producto primero.", "Aceptar");
        return;
    }

    var dialog = new Dialog("Registrar Movimiento", 60, 12);
    var tipoLabel = new Label("Tipo:") { X = 1, Y = 1 };
    var tipoText = new TextField("Compra") { X = 12, Y = 1, Width = 44 };
    var cantidadLabel = new Label("Cantidad:") { X = 1, Y = 3 };
    var cantidadText = new TextField("1") { X = 12, Y = 3, Width = 44 };

    dialog.Add(tipoLabel, tipoText, cantidadLabel, cantidadText);

    var aceptarButton = new Button("Aceptar", is_default: true);
    aceptarButton.Clicked += async () =>
    {
        if (!int.TryParse(cantidadText.Text.ToString(), out var cantidad) || cantidad <= 0)
        {
            MessageBox.ErrorQuery("Validacion", "La cantidad debe ser un entero positivo.", "Aceptar");
            return;
        }

        var response = await http.PostAsJsonAsync(
            $"productos/{productoSeleccionado.Id}/movimientos",
            new MovimientoRequest
            {
                Tipo = tipoText.Text.ToString() ?? string.Empty,
                Cantidad = cantidad
            });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            MessageBox.ErrorQuery("Error", string.IsNullOrWhiteSpace(error) ? "No se pudo registrar el movimiento." : error, "Aceptar");
            return;
        }

        Application.RequestStop();
        await CargarProductos(buscarText.Text.ToString() ?? string.Empty);
        await CargarMovimientos(productoSeleccionado.Id);
    };

    var cancelarButton = new Button("Cancelar");
    cancelarButton.Clicked += () => Application.RequestStop();

    dialog.AddButton(aceptarButton);
    dialog.AddButton(cancelarButton);
    Application.Run(dialog);
}

public sealed class Producto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public int Stock { get; set; }
}

public sealed class ProductoRequest
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public decimal Precio { get; set; }
}

public sealed class Movimiento
{
    public string Tipo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }
}

public sealed class MovimientoRequest
{
    public string Tipo { get; set; } = string.Empty;

    public int Cantidad { get; set; }
}