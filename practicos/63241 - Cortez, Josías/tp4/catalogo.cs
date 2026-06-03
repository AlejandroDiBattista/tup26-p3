#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string ApiUrl = "http://localhost:5050";

using var http = new HttpClient { BaseAddress = new Uri(ApiUrl) };

try {
    using var respuesta = await http.GetAsync("/productos");
    respuesta.EnsureSuccessStatusCode();
} catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Primero ejecuta: dotnet run servidor.cs");
    return;
}

var productos = new List<ProductoDto>();
var productosFiltrados = new List<ProductoDto>();
var filasProductos = new ObservableCollection<string>();
var filasMovimientos = new ObservableCollection<string>();
ProductoDto? productoSeleccionado = null;

using IApplication app = Application.Create().Init();

var ventana = new Window {
    Title = " Catalogo de productos - ESC para salir ",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
};

var etiquetaBuscar = new Label {
    Text = "Buscar:",
    X = 1,
    Y = 1,
    Width = 8,
};

var buscar = new TextField {
    X = 9,
    Y = 1,
    Width = Dim.Fill(1),
};

var panelProductos = new FrameView {
    Title = " Productos ",
    X = 0,
    Y = 3,
    Width = Dim.Percent(55),
    Height = Dim.Fill(3),
};

var listaProductos = new ListView {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
};
listaProductos.SetSource(filasProductos);
panelProductos.Add(listaProductos);

var panelMovimientos = new FrameView {
    Title = " Movimientos del producto ",
    X = Pos.Right(panelProductos),
    Y = 3,
    Width = Dim.Fill(),
    Height = Dim.Fill(3),
};

var listaMovimientos = new ListView {
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
};
listaMovimientos.SetSource(filasMovimientos);
panelMovimientos.Add(listaMovimientos);

var barraEstado = new Label {
    Text = "F2 agregar | F3 editar | F4 eliminar | F5 refrescar | F6 compra | F7 venta | F8 ajuste",
    X = 1,
    Y = Pos.AnchorEnd(1),
    Width = Dim.Fill(1),
};

buscar.ValueChanged += (_, _) => AplicarFiltro();
listaProductos.ValueChanged += async (_, _) => await SeleccionarProductoAsync();

var menu = new MenuBar(new[] {
    new MenuBarItem("_Productos", new[] {
        new MenuItem("_Agregar", "F2", async () => await AgregarProductoAsync(), Key.F2),
        new MenuItem("_Modificar", "F3", async () => await ModificarProductoAsync(), Key.F3),
        new MenuItem("_Eliminar", "F4", async () => await EliminarProductoAsync(), Key.F4),
        new MenuItem("_Refrescar", "F5", async () => await RefrescarProductosAsync(), Key.F5),
    }),
    new MenuBarItem("_Stock", new[] {
        new MenuItem("_Compra", "F6", async () => await RegistrarMovimientoAsync("Compra"), Key.F6),
        new MenuItem("_Venta", "F7", async () => await RegistrarMovimientoAsync("Venta"), Key.F7),
        new MenuItem("_Ajuste", "F8", async () => await RegistrarMovimientoAsync("Ajuste"), Key.F8),
    }),
});

ventana.Add(menu, etiquetaBuscar, buscar, panelProductos, panelMovimientos, barraEstado);

await RefrescarProductosAsync();
app.Run(ventana);

async Task RefrescarProductosAsync() {
    productos = await http.GetFromJsonAsync<List<ProductoDto>>("/productos") ?? [];
    AplicarFiltro();

    if (productosFiltrados.Count > 0) {
        listaProductos.SelectedItem = Math.Clamp(listaProductos.SelectedItem ?? 0, 0, productosFiltrados.Count - 1);
        await SeleccionarProductoAsync();
    } else {
        productoSeleccionado = null;
        filasMovimientos.Clear();
    }
}

void AplicarFiltro() {
    var texto = buscar.Value?.ToString()?.Trim() ?? "";
    productosFiltrados = productos
        .Where(p =>
            string.IsNullOrWhiteSpace(texto) ||
            p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
            p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.Codigo)
        .ToList();

    filasProductos.Clear();
    foreach (var producto in productosFiltrados) {
        filasProductos.Add($"{producto.Codigo,-10} {producto.Nombre,-28} ${producto.Precio,10:N2} Stock: {producto.Stock,5}");
    }

    if (productosFiltrados.Count == 0) {
        filasProductos.Add("Sin productos para mostrar.");
    }

    listaProductos.SelectedItem = productosFiltrados.Count > 0 ? 0 : null;
}

async Task SeleccionarProductoAsync() {
    var indice = listaProductos.SelectedItem;
    if (indice is null || indice < 0 || indice >= productosFiltrados.Count) {
        return;
    }

    productoSeleccionado = productosFiltrados[indice.Value];
    await CargarMovimientosAsync(productoSeleccionado.Id);
}

async Task CargarMovimientosAsync(int productoId) {
    var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoId}/movimientos") ?? [];

    filasMovimientos.Clear();
    foreach (var movimiento in movimientos) {
        filasMovimientos.Add($"{movimiento.Tipo,-8} {movimiento.Cantidad,6}  {movimiento.Fecha:dd/MM/yyyy HH:mm}");
    }

    if (filasMovimientos.Count == 0) {
        filasMovimientos.Add("Sin movimientos registrados.");
    }
}

async Task AgregarProductoAsync() {
    var nuevo = MostrarDialogoProducto("Agregar producto", null);
    if (nuevo is null) {
        return;
    }

    var respuesta = await http.PostAsJsonAsync("/productos", nuevo);
    await ResolverRespuestaAsync(respuesta, "Producto agregado.");
    await RefrescarProductosAsync();
}

async Task ModificarProductoAsync() {
    if (productoSeleccionado is null) {
        MostrarMensaje("Productos", "Selecciona un producto para modificar.");
        return;
    }

    var cambios = MostrarDialogoProducto("Modificar producto", productoSeleccionado);
    if (cambios is null) {
        return;
    }

    var respuesta = await http.PutAsJsonAsync($"/productos/{productoSeleccionado.Id}", cambios);
    await ResolverRespuestaAsync(respuesta, "Producto modificado.");
    await RefrescarProductosAsync();
}

async Task EliminarProductoAsync() {
    if (productoSeleccionado is null) {
        MostrarMensaje("Productos", "Selecciona un producto para eliminar.");
        return;
    }

    var opcion = MessageBox.Query(
        app,
        "Eliminar producto",
        $"Eliminar {productoSeleccionado.Codigo} - {productoSeleccionado.Nombre}?",
        "No",
        "Si");

    if (opcion != 1) {
        return;
    }

    var respuesta = await http.DeleteAsync($"/productos/{productoSeleccionado.Id}");
    await ResolverRespuestaAsync(respuesta, "Producto eliminado.");
    await RefrescarProductosAsync();
}

async Task RegistrarMovimientoAsync(string tipo) {
    if (productoSeleccionado is null) {
        MostrarMensaje("Stock", "Selecciona un producto para registrar stock.");
        return;
    }

    var cantidad = MostrarDialogoMovimiento(tipo, productoSeleccionado);
    if (cantidad is null) {
        return;
    }

    var respuesta = await http.PostAsJsonAsync(
        $"/productos/{productoSeleccionado.Id}/movimientos",
        new MovimientoCrearDto(tipo, cantidad.Value));

    await ResolverRespuestaAsync(respuesta, $"Movimiento {tipo.ToLower()} registrado.");
    await RefrescarProductosAsync();
}

ProductoGuardarDto? MostrarDialogoProducto(string titulo, ProductoDto? producto) {
    var dialogo = new Dialog {
        Title = $" {titulo} ",
        Width = 58,
        Height = 15,
    };

    var codigo = Campo(dialogo, "Codigo:", 1, producto?.Codigo ?? "");
    var nombre = Campo(dialogo, "Nombre:", 3, producto?.Nombre ?? "");
    var precio = Campo(dialogo, "Precio:", 5, producto?.Precio.ToString("0.##") ?? "0");
    var stock = Campo(dialogo, "Stock:", 7, producto?.Stock.ToString() ?? "0");

    ProductoGuardarDto? resultado = null;

    var cancelar = new Button {
        Text = "Cancelar",
        X = Pos.AnchorEnd(24),
        Y = 10,
    };
    cancelar.Accepted += (_, _) => dialogo.RequestStop();

    var guardar = new Button {
        Text = "Guardar",
        X = Pos.AnchorEnd(12),
        Y = 10,
        IsDefault = true,
    };
    guardar.Accepted += (_, _) => {
        if (!decimal.TryParse(precio.Value?.ToString(), out var precioValor)) {
            MostrarMensaje("Dato invalido", "El precio debe ser numerico.");
            return;
        }

        if (!int.TryParse(stock.Value?.ToString(), out var stockValor)) {
            MostrarMensaje("Dato invalido", "El stock debe ser un numero entero.");
            return;
        }

        resultado = new ProductoGuardarDto(
            codigo.Value?.ToString() ?? "",
            nombre.Value?.ToString() ?? "",
            precioValor,
            stockValor);
        dialogo.RequestStop();
    };

    dialogo.Add(cancelar, guardar);
    app.Run(dialogo);
    return resultado;
}

int? MostrarDialogoMovimiento(string tipo, ProductoDto producto) {
    var dialogo = new Dialog {
        Title = $" Registrar {tipo.ToLower()} ",
        Width = 56,
        Height = 11,
    };

    dialogo.Add(new Label {
        Text = $"{producto.Codigo} - {producto.Nombre} | Stock actual: {producto.Stock}",
        X = 1,
        Y = 1,
        Width = Dim.Fill(2),
    });

    var cantidad = Campo(dialogo, tipo == "Ajuste" ? "Nuevo stock:" : "Cantidad:", 4, "1");
    int? resultado = null;

    var cancelar = new Button {
        Text = "Cancelar",
        X = Pos.AnchorEnd(24),
        Y = 7,
    };
    cancelar.Accepted += (_, _) => dialogo.RequestStop();

    var guardar = new Button {
        Text = "Registrar",
        X = Pos.AnchorEnd(12),
        Y = 7,
        IsDefault = true,
    };
    guardar.Accepted += (_, _) => {
        if (!int.TryParse(cantidad.Value?.ToString(), out var cantidadValor) || cantidadValor <= 0) {
            MostrarMensaje("Dato invalido", "La cantidad debe ser un entero positivo.");
            return;
        }

        resultado = cantidadValor;
        dialogo.RequestStop();
    };

    dialogo.Add(cancelar, guardar);
    app.Run(dialogo);
    return resultado;
}

TextField Campo(Dialog dialogo, string etiqueta, int y, string valorInicial) {
    dialogo.Add(new Label {
        Text = etiqueta,
        X = 2,
        Y = y,
        Width = 12,
    });

    var campo = new TextField {
        Text = valorInicial,
        X = 15,
        Y = y,
        Width = Dim.Fill(2),
    };

    dialogo.Add(campo);
    return campo;
}

async Task ResolverRespuestaAsync(HttpResponseMessage respuesta, string mensajeOk) {
    if (respuesta.IsSuccessStatusCode) {
        MostrarMensaje("Catalogo", mensajeOk);
        return;
    }

    var detalle = await respuesta.Content.ReadAsStringAsync();
    MostrarMensaje("Error", string.IsNullOrWhiteSpace(detalle) ? respuesta.ReasonPhrase ?? "Operacion rechazada." : detalle);
}

void MostrarMensaje(string titulo, string mensaje) {
    MessageBox.Query(app, titulo, mensaje, "Aceptar");
}

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record ProductoGuardarDto(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoCrearDto(string Tipo, int Cantidad);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
