#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;
using Terminal.Gui.Input;

// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;

try
{
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}
static async Task<List<MovimientoDto>> CargarMovimientosAsync(
    HttpClient http,
    int productoId)
{
    string url = $"http://localhost:5050/productos/{productoId}/movimientos";

    return await http.GetFromJsonAsync<List<MovimientoDto>>(url)
           ?? new List<MovimientoDto>();
}
static async Task AgregarProductoAsync(NuevoProductoDto producto)
{
    using var http = new HttpClient();

    await http.PostAsJsonAsync(
        "http://localhost:5050/productos",
        producto
    );
}
static async Task EliminarProductoAsync(int productoId)
{
    using var http = new HttpClient();

    await http.DeleteAsync(
        $"http://localhost:5050/productos/{productoId}"
    );
}
static async Task ActualizarProductoAsync(
    int id,
    NuevoProductoDto producto)
{
    using var http = new HttpClient();

    await http.PutAsJsonAsync(
        $"http://localhost:5050/productos/{id}",
        producto
    );
}
static async Task RegistrarMovimientoAsync(
    int productoId,
    int tipo,
    int cantidad)
{
    using var http = new HttpClient();

    await http.PostAsJsonAsync(
        $"http://localhost:5050/productos/{productoId}/movimientos",
        new
        {
            Tipo = tipo,
            Cantidad = cantidad
        }
    );
}

static string FormatearMovimientos(List<MovimientoDto> movimientos)
{
    if (movimientos.Count == 0)
        return "Sin movimientos";

    return string.Join(
        "\n",
        movimientos.Select(m =>
            $"{TipoTexto(m.Tipo)} | {m.Cantidad} | {m.Fecha:dd/MM HH:mm}")
    );
}

static string TipoTexto(int tipo)
{
    return tipo switch
    {
        0 => "Compra",
        1 => "Venta",
        2 => "Ajuste",
        _ => "?"
    };
}
// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();

using Window ventana = new()
{
    Title = " Catalogo REST — F2 Agregar | F4 Movimiento | ESC Salir "
};

var items = new ObservableCollection<string>(
    productos.Select(p => $"{p.Codigo} - {p.Nombre}")
);

var listaProductos = new ListView()
{
    X = 1,
    Y = 1,
    Width = 40,
    Height = Dim.Fill()
};

listaProductos.SetSource(items);

var detalleProducto = new Label()
{
    Text = """
           DETALLE PRODUCTO

           Seleccione un producto
           """,
    X = 45,
    Y = 2,
    Width = 40,
    Height = Dim.Fill()
};
listaProductos.ValueChanged += async (_, _) =>
{
    int indice = listaProductos.SelectedItem ?? 0;

    if (indice < 0 || indice >= productos.Count)
        return;

    var producto = productos[indice];

    using var http = new HttpClient();

    var movimientos =
        await CargarMovimientosAsync(http, producto.Id);

    detalleProducto.Text =
    $"""
    PRODUCTO

    {producto.Nombre}

    Código: {producto.Codigo}

    Precio: ${producto.Precio}

    Stock: {producto.Stock}

    MOVIMIENTOS

    {FormatearMovimientos(movimientos)}
    """;
};

ventana.KeyDown += async (_, e) =>
{
 detalleProducto.Text = $"Tecla: {e.KeyCode}";
    if (e.KeyCode == Key.F2)
    {
        using AgregarProductoDialog dialog = new();

        app.Run(dialog);
    listaProductos.SetFocus();
        if (dialog.Guardado)
        {
            await AgregarProductoAsync(
                new NuevoProductoDto(
                    dialog.Codigo,
                    dialog.Nombre,
                    dialog.Precio,
                    dialog.Stock
                )
            );

            using var http = new HttpClient();

            productos = await CargarProductosAsync(http);

            items.Clear();

            foreach (var p in productos)
            {
                items.Add($"{p.Codigo} - {p.Nombre}");
            }

            listaProductos.SetSource(items);

            detalleProducto.Text =
            """
            PRODUCTO AGREGADO
            """;
        }
    }

    if (e.KeyCode == Key.F4 )
    {
        int indice = listaProductos.SelectedItem ?? 0;

        if (indice < 0 || indice >= productos.Count)
            return;

        var producto = productos[indice];

        using MovimientoDialog dialog = new();

        app.Run(dialog);
    listaProductos.SetFocus();
        if (dialog.Guardado)
{
    if (dialog.Cantidad <= 0)
        return;

    await RegistrarMovimientoAsync(
        producto.Id,
        dialog.Tipo,
        dialog.Cantidad
    );

    using var http = new HttpClient();

    productos = await CargarProductosAsync(http);

    var productoActualizado =
        productos.FirstOrDefault(p => p.Id == producto.Id);

    var movimientos =
        await CargarMovimientosAsync(http, producto.Id);

    if (productoActualizado != null)
    {
        detalleProducto.Text =
        $"""
        PRODUCTO

        {productoActualizado.Nombre}

        Stock: {productoActualizado.Stock}

        MOVIMIENTOS

        {FormatearMovimientos(movimientos)}
        """;
    }
}
    }
    if (e.KeyCode == Key.F7)
{
    int indice = listaProductos.SelectedItem ?? 0;

    if (indice < 0 || indice >= productos.Count)
        return;

    var producto = productos[indice];

    await EliminarProductoAsync(producto.Id);

    using var http = new HttpClient();

    productos = await CargarProductosAsync(http);

    items.Clear();

    foreach (var p in productos)
    {
        items.Add($"{p.Codigo} - {p.Nombre}");
    }

    listaProductos.SetSource(items);

    detalleProducto.Text =
    """
    PRODUCTO ELIMINADO
    """;
}
if (e.KeyCode == Key.F8)
{
    int indice = listaProductos.SelectedItem ?? 0;

    if (indice < 0 || indice >= productos.Count)
        return;

    var producto = productos[indice];

    using AgregarProductoDialog dialog = new(
        producto.Codigo,
        producto.Nombre,
        producto.Precio,
        producto.Stock
    );

    app.Run(dialog);

    if (dialog.Guardado)
    {
        await ActualizarProductoAsync(
            producto.Id,
            new NuevoProductoDto(
                dialog.Codigo,
                dialog.Nombre,
                dialog.Precio,
                dialog.Stock
            )
        );

        using var http = new HttpClient();

        productos = await CargarProductosAsync(http);

        items.Clear();

        foreach (var p in productos)
        {
            items.Add($"{p.Codigo} - {p.Nombre}");
        }

        listaProductos.SetSource(items);

        detalleProducto.Text =
        """
        PRODUCTO ACTUALIZADO
        """;
    }
}
if (e.KeyCode == Key.F9)
{
    using BuscarProductoDialog dialog = new();

    app.Run(dialog);

   if (dialog.Buscar)
{
    var filtrados = productos
        .Where(p =>
            p.Codigo.Contains(
                dialog.Texto,
                StringComparison.OrdinalIgnoreCase)
            ||
            p.Nombre.Contains(
                dialog.Texto,
                StringComparison.OrdinalIgnoreCase))
        .ToList();

    items.Clear();

    foreach (var p in filtrados)
    {
        items.Add($"{p.Codigo} - {p.Nombre}");
    }

    listaProductos.SetSource(items);

    detalleProducto.Text =
    $"""
    RESULTADOS

    Encontrados: {filtrados.Count}
    """;
}
}
};

ventana.Add(detalleProducto);
ventana.Add(listaProductos);
if (productos.Count > 0)
{
    listaProductos.SelectedItem = 0;
}

app.Run(ventana);
// ── API ───────────────────────────────────────────────────────────────────

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
           ?? new List<ProductoDto>();
}


// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);
record MovimientoDto(
    int Id,
    int ProductoId,
    int Tipo,
    int Cantidad,
    DateTime Fecha
);
record NuevoProductoDto(
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);
class AgregarProductoDialog : Dialog
{
   public TextField TxtCodigo;
    public TextField TxtNombre;
    public TextField TxtPrecio;
    public TextField TxtStock;
    public bool Guardado { get; private set; }

    public string Codigo => TxtCodigo.Text?.ToString() ?? "";
    public string Nombre => TxtNombre.Text?.ToString() ?? "";

    public decimal Precio =>
    decimal.TryParse(TxtPrecio.Text?.ToString(), out var p) ? p : 0;

    public int Stock =>
    int.TryParse(TxtStock.Text?.ToString(), out var s) ? s : 0;
    
    public AgregarProductoDialog
        (
        string codigo = "",
        string nombre = "",
        decimal precio = 0,
        int stock = 0)
        {
        
        Title = "Agregar Producto";
        Width = 60;
        Height = 18;

        var lblCodigo = new Label()
        {
            Text = "Código:",
            X = 2,
            Y = 2
        };

        TxtCodigo = new TextField()
        {
           Text = codigo,
            X = 15,
            Y = 2,
            Width = 25
        };

        var lblNombre = new Label()
        {
            Text = "Nombre:",
            X = 2,
            Y = 4
        };

        TxtNombre = new TextField()
        {
           Text = nombre,
            X = 15,
            Y = 4,
            Width = 25
        };

        var lblPrecio = new Label()
        {
            Text = "Precio:",
            X = 2,
            Y = 6
        };

    TxtPrecio = new TextField()
        {
            Text = precio.ToString(),
            X = 15,
            Y = 6,
            Width = 25
        };

        var lblStock = new Label()
        {
            Text = "Stock:",
            X = 2,
            Y = 8
        };

        TxtStock = new TextField()
        {
            Text = stock.ToString(),
            X = 15,
            Y = 8,
            Width = 25
        };

        Add(
            lblCodigo, TxtCodigo,
            lblNombre, TxtNombre,
            lblPrecio, TxtPrecio,
            lblStock, TxtStock
        );

        var btnCancelar = new Button()
{
    Title = "Cancelar"
};

btnCancelar.Accepting += (_, _) =>
{
    RequestStop();
};

var btnGuardar = new Button()
{
    Title = "Guardar"
};

btnGuardar.Accepting += (_, _) =>
{
    Guardado = true;
    RequestStop();
};

AddButton(btnCancelar);
AddButton(btnGuardar);
    }
}
class MovimientoDialog : Dialog
{
    public TextField TxtTipo;
    public TextField TxtCantidad;

    public bool Guardado { get; private set; }

    public int Tipo =>
        int.TryParse(TxtTipo.Text?.ToString(), out var t) ? t : 0;

    public int Cantidad =>
        int.TryParse(TxtCantidad.Text?.ToString(), out var c) ? c : 0;

    public MovimientoDialog()
    {
        Title = "Registrar Movimiento";
        Width = 60;
        Height = 14;

        Add(new Label()
        {
            Text = "Tipo (0=Compra,1=Venta,2=Ajuste):",
            X = 2,
            Y = 2
        });

        TxtTipo = new TextField()
        {
            X = 38,
            Y = 2,
            Width = 10
        };

        Add(new Label()
        {
            Text = "Cantidad:",
            X = 2,
            Y = 5
        });

        TxtCantidad = new TextField()
        {
            X = 38,
            Y = 5,
            Width = 10
        };

        Add(TxtTipo, TxtCantidad);

        var btnCancelar = new Button()
        {
            Title = "Cancelar"
        };

        btnCancelar.Accepting += (_, _) =>
        {
            RequestStop();
        };

        var btnGuardar = new Button()
        {
            Title = "Guardar"
        };

        btnGuardar.Accepting += (_, _) =>
        {
            Guardado = true;
            RequestStop();
        };

        AddButton(btnCancelar);
        AddButton(btnGuardar);
    }
}
class BuscarProductoDialog : Dialog
{
    public TextField TxtBusqueda;

    public bool Buscar { get; private set; }

    public string Texto =>
        TxtBusqueda.Text?.ToString() ?? "";

   public BuscarProductoDialog()
{
    Title = "Buscar Producto";

    Width = 60;
    Height = 14;

    Add(new Label()
    {
        Text = "Código o nombre:",
        X = 2,
        Y = 2
    });

    TxtBusqueda = new TextField()
    {
        X = 2,
        Y = 5,
        Width = 40
    };

    Add(TxtBusqueda);

    var btnCancelar = new Button()
    {
        Title = "Cancelar"
    };

    btnCancelar.Accepting += (_, _) =>
    {
        RequestStop();
    };

    var btnBuscar = new Button()
    {
        Title = "Buscar"
    };

    btnBuscar.Accepting += (_, _) =>
    {
        Buscar = true;
        RequestStop();
    };

    AddButton(btnCancelar);
    AddButton(btnBuscar);
}
}