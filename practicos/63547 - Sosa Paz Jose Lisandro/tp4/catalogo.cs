#:package Terminal.Gui@2.4.4
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string apiUrl = "http://localhost:5048";

var opcionesJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);
opcionesJson.Converters.Add(new JsonStringEnumConverter());

using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };
using IApplication app = Application.Create();

var productos = new List<Producto>();
var productosFiltrados = new ObservableCollection<ProductoLinea>();
var movimientos = new ObservableCollection<MovimientoLinea>();

var ventana = new Window
{
    Title = "CatalogoREST",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var buscarLabel = new Label
{
    Text = "Buscar:",
    X = 1,
    Y = 1
};

var buscarTexto = new TextField
{
    X = Pos.Right(buscarLabel) + 1,
    Y = 1,
    Width = Dim.Fill(1)
};

var panelProductos = new Window
{
    Title = "Productos",
    X = 0,
    Y = 3,
    Width = Dim.Percent(55),
    Height = Dim.Fill(3)
};

var panelMovimientos = new Window
{
    Title = "Movimientos del producto",
    X = Pos.Right(panelProductos),
    Y = 3,
    Width = Dim.Fill(),
    Height = Dim.Fill(3)
};

var listaProductos = new ListView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};
listaProductos.SetSource(productosFiltrados);

var listaMovimientos = new ListView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};
listaMovimientos.SetSource(movimientos);

panelProductos.Add(listaProductos);
panelMovimientos.Add(listaMovimientos);

var botonAgregar = Boton("F2 Agregar", 1);
var botonEditar = Boton("F3 Editar", Pos.Right(botonAgregar) + 1);
var botonEliminar = Boton("F4 Eliminar", Pos.Right(botonEditar) + 1);
var botonCompra = Boton("F5 Compra", Pos.Right(botonEliminar) + 1);
var botonVenta = Boton("F6 Venta", Pos.Right(botonCompra) + 1);
var botonAjuste = Boton("F7 Ajuste", Pos.Right(botonVenta) + 1);
var botonSalir = Boton("Esc Salir", Pos.Right(botonAjuste) + 1);

ventana.Add(
    buscarLabel,
    buscarTexto,
    panelProductos,
    panelMovimientos,
    botonAgregar,
    botonEditar,
    botonEliminar,
    botonCompra,
    botonVenta,
    botonAjuste,
    botonSalir);

buscarTexto.TextChanged += (_, _) => FiltrarProductos();
listaProductos.ValueChanged += (_, _) => CargarMovimientosDelSeleccionado();

botonAgregar.Accepted += (_, _) => AgregarProducto();
botonEditar.Accepted += (_, _) => EditarProducto();
botonEliminar.Accepted += (_, _) => EliminarProducto();
botonCompra.Accepted += (_, _) => RegistrarMovimiento(TipoMovimiento.Compra);
botonVenta.Accepted += (_, _) => RegistrarMovimiento(TipoMovimiento.Venta);
botonAjuste.Accepted += (_, _) => RegistrarMovimiento(TipoMovimiento.Ajuste);
botonSalir.Accepted += (_, _) => app.RequestStop();

ventana.KeyDown += (_, tecla) =>
{
    if (tecla == Key.F2)
    {
        AgregarProducto();
        tecla.Handled = true;
    }
    else if (tecla == Key.F3)
    {
        EditarProducto();
        tecla.Handled = true;
    }
    else if (tecla == Key.F4)
    {
        EliminarProducto();
        tecla.Handled = true;
    }
    else if (tecla == Key.F5)
    {
        RegistrarMovimiento(TipoMovimiento.Compra);
        tecla.Handled = true;
    }
    else if (tecla == Key.F6)
    {
        RegistrarMovimiento(TipoMovimiento.Venta);
        tecla.Handled = true;
    }
    else if (tecla == Key.F7)
    {
        RegistrarMovimiento(TipoMovimiento.Ajuste);
        tecla.Handled = true;
    }
};

try
{
    CargarProductos();
    app.Init();
    app.Run(ventana);
}
catch (HttpRequestException)
{
    Console.WriteLine("No se pudo conectar con el servidor. Primero ejecute: dotnet run servidor.cs");
}

Button Boton(string texto, Pos x)
{
    return new Button
    {
        Text = texto,
        X = x,
        Y = Pos.AnchorEnd(2),
        Width = texto.Length + 4
    };
}

Producto? ProductoSeleccionado()
{
    var indice = listaProductos.SelectedItem;
    if (indice is null || indice < 0 || indice >= productosFiltrados.Count)
    {
        return null;
    }

    return productosFiltrados[(int)indice].Producto;
}

void CargarProductos()
{
    var resultado = http.GetFromJsonAsync<List<Producto>>("/productos", opcionesJson)
        .GetAwaiter()
        .GetResult();

    productos = resultado ?? new List<Producto>();
    FiltrarProductos();
}

void FiltrarProductos()
{
    var texto = buscarTexto.Text?.ToString()?.Trim() ?? "";
    productosFiltrados.Clear();

    foreach (var producto in productos)
    {
        var coincide = string.IsNullOrWhiteSpace(texto)
            || producto.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || producto.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase);

        if (coincide)
        {
            productosFiltrados.Add(new ProductoLinea(producto));
        }
    }

    if (productosFiltrados.Count > 0)
    {
        listaProductos.SelectedItem = 0;
        CargarMovimientosDelSeleccionado();
    }
    else
    {
        movimientos.Clear();
    }
}

void CargarMovimientosDelSeleccionado()
{
    var producto = ProductoSeleccionado();
    movimientos.Clear();

    if (producto is null)
    {
        return;
    }

    var resultado = http.GetFromJsonAsync<List<MovimientoDeProducto>>(
            $"/productos/{producto.Id}/movimientos",
            opcionesJson)
        .GetAwaiter()
        .GetResult();

    foreach (var movimiento in resultado ?? new List<MovimientoDeProducto>())
    {
        movimientos.Add(new MovimientoLinea(movimiento));
    }
}

void AgregarProducto()
{
    var entrada = MostrarDialogoProducto("Agregar producto", null);
    if (entrada is null)
    {
        return;
    }

    var respuesta = http.PostAsJsonAsync("/productos", entrada, opcionesJson)
        .GetAwaiter()
        .GetResult();

    if (!respuesta.IsSuccessStatusCode)
    {
        MostrarError(LeerError(respuesta));
        return;
    }

    CargarProductos();
}

void EditarProducto()
{
    var producto = ProductoSeleccionado();
    if (producto is null)
    {
        MostrarError("Seleccione un producto.");
        return;
    }

    var entrada = MostrarDialogoProducto("Editar producto", producto);
    if (entrada is null)
    {
        return;
    }

    var respuesta = http.PutAsJsonAsync($"/productos/{producto.Id}", entrada, opcionesJson)
        .GetAwaiter()
        .GetResult();

    if (!respuesta.IsSuccessStatusCode)
    {
        MostrarError(LeerError(respuesta));
        return;
    }

    CargarProductos();
}

void EliminarProducto()
{
    var producto = ProductoSeleccionado();
    if (producto is null)
    {
        MostrarError("Seleccione un producto.");
        return;
    }

    var confirmar = MessageBox.Query(
        app,
        "Eliminar",
        $"Eliminar {producto.Codigo} - {producto.Nombre}?",
        "No",
        "Si");

    if (confirmar != 1)
    {
        return;
    }

    var respuesta = http.DeleteAsync($"/productos/{producto.Id}")
        .GetAwaiter()
        .GetResult();

    if (!respuesta.IsSuccessStatusCode)
    {
        MostrarError(LeerError(respuesta));
        return;
    }

    CargarProductos();
}

void RegistrarMovimiento(TipoMovimiento tipo)
{
    var producto = ProductoSeleccionado();
    if (producto is null)
    {
        MostrarError("Seleccione un producto.");
        return;
    }

    var cantidad = MostrarDialogoCantidad(tipo);
    if (cantidad is null)
    {
        return;
    }

    var entrada = new MovimientoEntrada(tipo, cantidad.Value);
    var respuesta = http.PostAsJsonAsync(
            $"/productos/{producto.Id}/movimientos",
            entrada,
            opcionesJson)
        .GetAwaiter()
        .GetResult();

    if (!respuesta.IsSuccessStatusCode)
    {
        MostrarError(LeerError(respuesta));
        return;
    }

    CargarProductos();
}

// Dialogos para agregar o editar productos desde la TUI.
ProductoEntrada? MostrarDialogoProducto(string titulo, Producto? producto)
{
    var dialogo = DialogoBase(titulo, 52, 15);

    var codigo = Campo(dialogo, "Codigo:", producto?.Codigo ?? "", 1);
    var nombre = Campo(dialogo, "Nombre:", producto?.Nombre ?? "", 3);
    var precio = Campo(dialogo, "Precio:", producto?.Precio.ToString(CultureInfo.InvariantCulture) ?? "0", 5);
    var stock = Campo(dialogo, "Stock:", producto?.Stock.ToString(CultureInfo.InvariantCulture) ?? "0", 7);

    var guardar = new Button { Text = "Guardar", X = 14, Y = 10, Width = 12 };
    var cancelar = new Button { Text = "Cancelar", X = 28, Y = 10, Width = 12 };
    ProductoEntrada? entrada = null;

    guardar.Accepted += (_, _) =>
    {
        if (!decimal.TryParse(precio.Text?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var precioValor)
            || !int.TryParse(stock.Text?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stockValor))
        {
            MostrarError("Precio o stock invalido.");
            return;
        }

        entrada = new ProductoEntrada(
            codigo.Text?.ToString() ?? "",
            nombre.Text?.ToString() ?? "",
            precioValor,
            stockValor);

        app.RequestStop(dialogo);
    };

    cancelar.Accepted += (_, _) => app.RequestStop(dialogo);

    dialogo.Add(guardar, cancelar);
    app.Run(dialogo);
    return entrada;
}

// Dialogo para cargar compra, venta o ajuste de stock.
int? MostrarDialogoCantidad(TipoMovimiento tipo)
{
    var titulo = tipo == TipoMovimiento.Ajuste ? "Ajustar stock" : $"Registrar {tipo}";
    var dialogo = DialogoBase(titulo, 42, 10);
    var texto = tipo == TipoMovimiento.Ajuste ? "Nuevo stock:" : "Cantidad:";
    var cantidad = Campo(dialogo, texto, "1", 1);
    var aceptar = new Button { Text = "Aceptar", X = 9, Y = 5, Width = 12 };
    var cancelar = new Button { Text = "Cancelar", X = 23, Y = 5, Width = 12 };
    int? valor = null;

    aceptar.Accepted += (_, _) =>
    {
        if (!int.TryParse(cantidad.Text?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero)
            || numero <= 0)
        {
            MostrarError("La cantidad debe ser positiva.");
            return;
        }

        valor = numero;
        app.RequestStop(dialogo);
    };

    cancelar.Accepted += (_, _) => app.RequestStop(dialogo);

    dialogo.Add(aceptar, cancelar);
    app.Run(dialogo);
    return valor;
}

Window DialogoBase(string titulo, int ancho, int alto)
{
    return new Window
    {
        Title = titulo,
        X = Pos.Center(),
        Y = Pos.Center(),
        Width = ancho,
        Height = alto
    };
}

TextField Campo(Window contenedor, string etiqueta, string valor, int y)
{
    var label = new Label
    {
        Text = etiqueta,
        X = 2,
        Y = y
    };

    var campo = new TextField
    {
        Text = valor,
        X = 14,
        Y = y,
        Width = Dim.Fill(2)
    };

    contenedor.Add(label, campo);
    return campo;
}

void MostrarError(string mensaje)
{
    MessageBox.ErrorQuery(app, "Error", mensaje, "Aceptar");
}

string LeerError(HttpResponseMessage respuesta)
{
    var texto = respuesta.Content.ReadAsStringAsync()
        .GetAwaiter()
        .GetResult();

    return string.IsNullOrWhiteSpace(texto) ? "No se pudo completar la operacion." : texto;
}

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
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

public enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

public record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock);

public record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);

public class ProductoLinea
{
    public ProductoLinea(Producto producto)
    {
        Producto = producto;
    }

    public Producto Producto { get; }

    public override string ToString()
    {
        return $"{Producto.Codigo,-10} {Producto.Nombre,-22} ${Producto.Precio,8:0.00} Stock: {Producto.Stock}";
    }
}

public class MovimientoLinea
{
    private readonly MovimientoDeProducto movimiento;

    public MovimientoLinea(MovimientoDeProducto movimiento)
    {
        this.movimiento = movimiento;
    }

    public override string ToString()
    {
        return $"{movimiento.Tipo,-7} {movimiento.Cantidad,6} {movimiento.Fecha:dd/MM/yyyy HH:mm}";
    }
}

