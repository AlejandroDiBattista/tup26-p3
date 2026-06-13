#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// -- Consulta inicial al servidor ------------------------------------------

List<ProductoDto> productos;
List<ProductoDto> productosFiltrados;
List<MovimientoDto> movimientos;
try {
    using var http = new HttpClient();
    ConfigurarHttp(http);
    productos = await CargarProductosAsync(http);
    productosFiltrados = productos.ToList();
    movimientos = productos.Count == 0
        ? new List<MovimientoDto>()
        : await CargarMovimientosAsync(http, productos[0].Id);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verifica que servidor.cs este corriendo en http://localhost:5050");
    return;
}

// -- Interfaz TUI -----------------------------------------------------------

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST - Productos (ESC para salir) " };

var panelProductos = new FrameView {
    Title = " Productos ",
    X = 0, Y = 0,
    Width = Dim.Percent(58),
    Height = Dim.Fill()
};

var panelMovimientos = new FrameView {
    Title = " Movimientos ",
    X = Pos.Right(panelProductos),
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var etiquetaBusqueda = new Label {
    Text = "Buscar:",
    X = 0, Y = 0
};

var campoBusqueda = new TextField {
    X = 8, Y = 0,
    Width = Dim.Fill(10)
};

var botonBuscar = new Button {
    Text = "Aplicar",
    X = Pos.AnchorEnd(8),
    Y = 0
};

var botonAgregar = new Button {
    Text = "Agregar",
    X = 0,
    Y = 1
};

var botonEditar = new Button {
    Text = "Editar",
    X = 10,
    Y = 1
};

var botonEliminar = new Button {
    Text = "Eliminar",
    X = 19,
    Y = 1
};

var botonCompra = new Button {
    Text = "Compra",
    X = 1,
    Y = 0
};

var botonVenta = new Button {
    Text = "Venta",
    X = 10,
    Y = 0
};

var botonAjuste = new Button {
    Text = "Ajuste",
    X = 18,
    Y = 0
};

var listaProductos = new ListView {
    X = 0, Y = 3,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Source = CrearFuente(productosFiltrados)
};

var detalleMovimientos = new Label {
    Text = RenderizarMovimientos(movimientos),
    X = 1, Y = 2,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

ProductoDto? ProductoSeleccionado() {
    var indice = listaProductos.SelectedItem;
    if (!indice.HasValue || indice.Value < 0 || indice.Value >= productosFiltrados.Count) {
        return null;
    }

    return productosFiltrados[indice.Value];
}

async Task RefrescarProductosAsync(int? productoIdSeleccionado = null) {
    using var http = new HttpClient();
    ConfigurarHttp(http);

    productos = await CargarProductosAsync(http);
    productosFiltrados = FiltrarProductos(productos, campoBusqueda.Text?.ToString() ?? "");
    listaProductos.Source = CrearFuente(productosFiltrados);

    if (productosFiltrados.Count == 0) {
        listaProductos.SelectedItem = null;
        detalleMovimientos.Text = "No hay productos para mostrar.";
        return;
    }

    var indice = productoIdSeleccionado.HasValue
        ? productosFiltrados.FindIndex(p => p.Id == productoIdSeleccionado.Value)
        : 0;

    listaProductos.SelectedItem = indice >= 0 ? indice : 0;
    var seleccionado = productosFiltrados[listaProductos.SelectedItem!.Value];
    var nuevosMovimientos = await CargarMovimientosAsync(http, seleccionado.Id);
    detalleMovimientos.Text = RenderizarMovimientos(nuevosMovimientos);
}

listaProductos.ValueChanged += async (sender, args) =>
{
    var seleccionado = ProductoSeleccionado();
    if (seleccionado is null) {
        return;
    }

    using var http = new HttpClient();
    ConfigurarHttp(http);
    var nuevosMovimientos = await CargarMovimientosAsync(http, seleccionado.Id);
    detalleMovimientos.Text = RenderizarMovimientos(nuevosMovimientos);
};

botonBuscar.Accepted += async (sender, args) =>
{
    await RefrescarProductosAsync();
};

botonAgregar.Accepted += async (sender, args) =>
{
    var nuevoProducto = PedirProducto(app, null);
    if (nuevoProducto is null) {
        return;
    }

    try {
        using var http = new HttpClient();
        ConfigurarHttp(http);
        var creado = await CrearProductoAsync(http, nuevoProducto);
        await RefrescarProductosAsync(creado.Id);
    } catch (HttpRequestException ex) {
        MessageBox.ErrorQuery(app, "Error", ex.Message, "Ok");
    }
};

botonEditar.Accepted += async (sender, args) =>
{
    var seleccionado = ProductoSeleccionado();
    if (seleccionado is null) {
        MessageBox.ErrorQuery(app, "Error", "Seleccione un producto.", "Ok");
        return;
    }

    var editado = PedirProducto(app, seleccionado);
    if (editado is null) {
        return;
    }

    try {
        using var http = new HttpClient();
        ConfigurarHttp(http);
                await EditarProductoAsync(http, seleccionado.Id, editado);
        await RefrescarProductosAsync(seleccionado.Id);
    } catch (HttpRequestException ex) {
        MessageBox.ErrorQuery(app, "Error", ex.Message, "Ok");
    }
};

botonEliminar.Accepted += async (sender, args) =>
{
    var seleccionado = ProductoSeleccionado();
    if (seleccionado is null) {
        MessageBox.ErrorQuery(app, "Error", "Seleccione un producto.", "Ok");
        return;
    }

    var confirma = MessageBox.Query(app, "Eliminar", $"Eliminar {seleccionado.Nombre}?", "No", "Si");
    if (confirma != 1) {
        return;
    }

    try {
        using var http = new HttpClient();
        ConfigurarHttp(http);
        await EliminarProductoAsync(http, seleccionado.Id);
        await RefrescarProductosAsync();
    } catch (HttpRequestException ex) {
        MessageBox.ErrorQuery(app, "Error", ex.Message, "Ok");
    }
};

botonCompra.Accepted += async (sender, args) => await RegistrarMovimientoAsync(TipoMovimiento.Compra);
botonVenta.Accepted += async (sender, args) => await RegistrarMovimientoAsync(TipoMovimiento.Venta);
botonAjuste.Accepted += async (sender, args) => await RegistrarMovimientoAsync(TipoMovimiento.Ajuste);

async Task RegistrarMovimientoAsync(TipoMovimiento tipo) {
    var seleccionado = ProductoSeleccionado();
    if (seleccionado is null) {
        MessageBox.ErrorQuery(app, "Error", "Seleccione un producto.", "Ok");
        return;
    }

    var cantidad = PedirCantidad(app, tipo);
    if (!cantidad.HasValue) {
        return;
    }

    try {
        using var http = new HttpClient();
        ConfigurarHttp(http);
        await CrearMovimientoAsync(http, seleccionado.Id, new MovimientoNuevoDto(tipo, cantidad.Value));
        await RefrescarProductosAsync(seleccionado.Id);
    } catch (HttpRequestException ex) {
        MessageBox.ErrorQuery(app, "Error", ex.Message, "Ok");
    }
}

panelProductos.Add(etiquetaBusqueda, campoBusqueda, botonBuscar, botonAgregar, botonEditar, botonEliminar, listaProductos);
panelMovimientos.Add(botonCompra, botonVenta, botonAjuste, detalleMovimientos);
ventana.Add(panelProductos, panelMovimientos);

app.Run(ventana);

static void ConfigurarHttp(HttpClient http) {
    http.BaseAddress = new Uri("http://localhost:5050");
}

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http) {
    const string url = "/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? throw new HttpRequestException("El servidor devolvio una lista vacia");
}

static async Task<ProductoDto> CrearProductoAsync(HttpClient http, ProductoNuevoDto producto) {
    var respuesta = await http.PostAsJsonAsync("/productos", producto);
    if (!respuesta.IsSuccessStatusCode) {
        var mensaje = await respuesta.Content.ReadAsStringAsync();
        throw new HttpRequestException(mensaje);
    }

    return await respuesta.Content.ReadFromJsonAsync<ProductoDto>()
        ?? throw new HttpRequestException("El servidor no devolvio el producto creado.");
}

static async Task EditarProductoAsync(HttpClient http, int id, ProductoNuevoDto producto) {
    var respuesta = await http.PutAsJsonAsync($"/productos/{id}", producto);
    if (!respuesta.IsSuccessStatusCode) {
        var mensaje = await respuesta.Content.ReadAsStringAsync();
        throw new HttpRequestException(mensaje);
    }
}

static async Task EliminarProductoAsync(HttpClient http, int id) {
    var respuesta = await http.DeleteAsync($"/productos/{id}");
    if (!respuesta.IsSuccessStatusCode) {
        var mensaje = await respuesta.Content.ReadAsStringAsync();
        throw new HttpRequestException(mensaje);
    }
}

static async Task CrearMovimientoAsync(HttpClient http, int productoId, MovimientoNuevoDto movimiento) {
        var respuesta = await http.PostAsJsonAsync($"/productos/{productoId}/movimientos", movimiento);
    if (!respuesta.IsSuccessStatusCode) {
        var mensaje = await respuesta.Content.ReadAsStringAsync();
        throw new HttpRequestException(mensaje);
    }
}

static async Task<List<MovimientoDto>> CargarMovimientosAsync(HttpClient http, int productoId) {
    return await http.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoId}/movimientos")
        ?? new List<MovimientoDto>();
}

static ProductoNuevoDto? PedirProducto(IApplication app, ProductoDto? productoActual) {
    var codigo = new TextField { X = 10, Y = 1, Width = 30, Text = productoActual?.Codigo ?? "" };
    var nombre = new TextField { X = 10, Y = 3, Width = 30, Text = productoActual?.Nombre ?? "" };
    var precio = new TextField { X = 10, Y = 5, Width = 30, Text = productoActual?.Precio.ToString() ?? "" };
    var stock = new TextField { X = 10, Y = 7, Width = 30, Text = productoActual?.Stock.ToString() ?? "" };

    var dialogo = new Dialog {
        Title = productoActual is null ? " Agregar producto " : " Editar producto ",
        Width = 50,
        Height = 15
    };

    dialogo.Add(
        new Label { Text = "Codigo:", X = 2, Y = 1 },
        codigo,
        new Label { Text = "Nombre:", X = 2, Y = 3 },
        nombre,
        new Label { Text = "Precio:", X = 2, Y = 5 },
        precio,
        new Label { Text = "Stock:", X = 2, Y = 7 },
        stock
    );

    ProductoNuevoDto? producto = null;

    var cancelar = new Button { Text = "Cancelar" };
    cancelar.Accepted += (sender, args) => app.RequestStop(dialogo);

    var guardar = new Button { Text = "Guardar" };
    guardar.Accepted += (sender, args) =>
    {
        var codigoTexto = codigo.Text?.ToString()?.Trim() ?? "";
        var nombreTexto = nombre.Text?.ToString()?.Trim() ?? "";

        if (codigoTexto.Length == 0 || nombreTexto.Length == 0) {
            MessageBox.ErrorQuery(app, "Error", "Codigo y nombre son obligatorios.", "Ok");
            return;
        }

        if (!decimal.TryParse(precio.Text?.ToString(), out var precioValor)) {
            MessageBox.ErrorQuery(app, "Error", "El precio debe ser un numero.", "Ok");
            return;
        }

        if (!int.TryParse(stock.Text?.ToString(), out var stockValor)) {
            MessageBox.ErrorQuery(app, "Error", "El stock debe ser un numero entero.", "Ok");
            return;
        }

        producto = new ProductoNuevoDto(codigoTexto, nombreTexto, precioValor, stockValor);
        app.RequestStop(dialogo);
    };

    dialogo.AddButton(cancelar);
    dialogo.AddButton(guardar);

    app.Run(dialogo);
    return producto;
}

static int? PedirCantidad(IApplication app, TipoMovimiento tipo) {
    var cantidad = new TextField { X = 12, Y = 1, Width = 20 };

    var dialogo = new Dialog {
        Title = $" Registrar {tipo} ",
        Width = 44,
        Height = 10
    };

    var texto = tipo == TipoMovimiento.Ajuste ? "Nuevo stock:" : "Cantidad:";

    dialogo.Add(
        new Label { Text = texto, X = 2, Y = 1 },
        cantidad
    );

    int? valor = null;

    var cancelar = new Button { Text = "Cancelar" };
    cancelar.Accepted += (sender, args) => app.RequestStop(dialogo);

    var guardar = new Button { Text = "Guardar" };
    guardar.Accepted += (sender, args) =>
    {
        if (!int.TryParse(cantidad.Text?.ToString(), out var cantidadValor) || cantidadValor <= 0) {
            MessageBox.ErrorQuery(app, "Error", "Ingrese un numero entero positivo.", "Ok");
            return;
        }
        
        valor = cantidadValor;
        app.RequestStop(dialogo);
    };

    dialogo.AddButton(cancelar);
    dialogo.AddButton(guardar);

    app.Run(dialogo);
    return valor;
}

static ListWrapper<string> CrearFuente(List<ProductoDto> productos) {
    return new ListWrapper<string>(new ObservableCollection<string>(productos.Select(FormatearProducto).ToList()));
}

static List<ProductoDto> FiltrarProductos(List<ProductoDto> productos, string texto) {
    texto = texto.Trim();
    if (texto.Length == 0) {
        return productos.ToList();
    }

    return productos
        .Where(p =>
            p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
            p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .ToList();
}

static string FormatearProducto(ProductoDto producto) {
    return $"{producto.Codigo,-8} {producto.Nombre,-24} ${producto.Precio,9:N2} Stock: {producto.Stock,4}";
}

static string RenderizarMovimientos(List<MovimientoDto> movimientos) {
    if (movimientos.Count == 0) {
        return "Sin movimientos registrados.";
    }

    var lineas = new List<string>
    {
        $"{"TIPO",-8} {"CANT.",8} {"FECHA",-19}",
        new string('-', 34)
    };

    lineas.AddRange(movimientos.Select(m =>
        $"{m.Tipo,-8} {m.Cantidad,8} {m.Fecha:dd/MM/yyyy HH:mm}"));

    return string.Join(Environment.NewLine, lineas);
}

// -- DTO --------------------------------------------------------------------

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

record ProductoNuevoDto(string Codigo, string Nombre, decimal Precio, int Stock);

[JsonConverter(typeof(JsonStringEnumConverter))]
enum TipoMovimiento
{
    Compra,
    Venta,
    Ajuste
}

record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);

record MovimientoNuevoDto(TipoMovimiento Tipo, int Cantidad);
    