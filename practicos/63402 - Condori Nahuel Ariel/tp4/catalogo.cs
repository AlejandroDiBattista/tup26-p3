#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using HttpClient http = new();

List<ProductoDto> productos = [];
List<ProductoDto> filtrados = [];

try {
    productos = await CargarProductosAsync(http);
} catch (HttpRequestException ex) {
    Console.WriteLine($"Error al cargar productos: {ex.Message}");
    Console.WriteLine("Verifica que el servidor este corriendo en http://localhost:5050");
    return;
}

using IApplication app = Application.Create().Init();
using Window ventana = new() { Title = " FIX-KIT Catalogo REST - Productos (ESC para salir) " };

var etiquetaBuscar = new Label { Text = "Buscar:", X = 2, Y = 1 };
var buscar = new TextField { X = 10, Y = 1, Width = 40 };

var listaProductos = new ListView {
    X = 2, Y = 3,
    Width = 60, Height = Dim.Fill(1),
};

var listaMovimientos = new ListView {
    X = Pos.Right(listaProductos) + 1, Y = 3,
    Width = Dim.Fill(2), Height = Dim.Fill(1),
};

ProductoDto? ObtenerProductoSeleccionado() {
    int indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count) return null;
    return filtrados[indice];
}

void ActualizarProductos() {
    string texto = buscar.Text?.ToString() ?? "";
    filtrados = productos
        .Where(producto =>
            producto.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || producto.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .ToList();

    listaProductos.SetSource(new ObservableCollection<string>(
        filtrados.Select(FormatearProducto).ToList()
    ));
}

async Task CargarMovimientosSeleccionado() {
    int indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count) {
        listaMovimientos.SetSource(new ObservableCollection<string>());
        return;
    }

    ProductoDto seleccionado = filtrados[indice];
    var movimientos = await CargarMovimientosAsync(http, seleccionado.Id);

    listaMovimientos.SetSource(new ObservableCollection<string>(
        movimientos.Select(FormatearMovimiento).ToList()
    ));
}

async Task RecargarDatos() {
    try {
        productos = await CargarProductosAsync(http);
        ActualizarProductos();
        await CargarMovimientosSeleccionado();
    } catch {
        MessageBox.ErrorQuery(app, "Error", "No se pudieron recargar los datos del servidor.", "OK");
    }
}

async Task CrearProducto(string codigo, string nombre, decimal precio, int stock) {
    try {
        await http.PostAsJsonAsync("http://localhost:5050/productos", 
            new ProductoCrearDto(codigo, nombre, precio, stock));
        await RecargarDatos();
    } catch {
        MessageBox.ErrorQuery(app, "Error", "No se pudo crear el producto.", "OK");
    }
}

async Task EliminarProducto() {
    var producto = ObtenerProductoSeleccionado();
    if (producto == null) return;

    var confirmacion = MessageBox.Query(app, "Confirmar", $"¿Seguro que deseas eliminar '{producto.Nombre}'?", "Sí", "No");
    if (confirmacion == 0) {
        try {
            await http.DeleteAsync($"http://localhost:5050/productos/{producto.Id}");
            await RecargarDatos();
        } catch {
            MessageBox.ErrorQuery(app, "Error", "No se pudo eliminar el producto.", "OK");
        }
    }
}

async Task RegistrarMovimiento(string tipo, int cantidad) {
    var producto = ObtenerProductoSeleccionado();
    if (producto == null) {
        MessageBox.ErrorQuery(app, "Aviso", "Selecciona un producto primero.", "OK");
        return;
    }

    try {
        await http.PostAsJsonAsync($"http://localhost:5050/productos/{producto.Id}/movimientos", 
            new MovimientoCrearDto(tipo, cantidad));
        await RecargarDatos();
    } catch {
        MessageBox.ErrorQuery(app, "Error", "No se pudo registrar el movimiento.", "OK");
    }
}


void MostrarDialogoAgregarProducto() {
    var dialog = new Dialog { Title = "Agregar Producto", Width = 40, Height = 12 };
    
    var lblCodigo = new Label { Text = "Código:", X = 1, Y = 1 };
    var txtCodigo = new TextField { X = 10, Y = 1, Width = 20 };
    
    var lblNombre = new Label { Text = "Nombre:", X = 1, Y = 3 };
    var txtNombre = new TextField { X = 10, Y = 3, Width = 20 };
    
    var lblPrecio = new Label { Text = "Precio:", X = 1, Y = 5 };
    var txtPrecio = new TextField { X = 10, Y = 5, Width = 20 };
    
    var btnGuardar = new Button { Text = "Guardar", X = 5, Y = 8 };
    var btnCancelar = new Button { Text = "Cancelar", X = Pos.Right(btnGuardar) + 2, Y = 8 };

    btnCancelar.Accepting += (_, _) => app.RequestStop();
    btnGuardar.Accepting += async (_, _) => {
        if (decimal.TryParse(txtPrecio.Text?.ToString(), out decimal precio)) {
            await CrearProducto(txtCodigo.Text?.ToString() ?? "", txtNombre.Text?.ToString() ?? "", precio, 0);
            app.RequestStop();
        } else {
            MessageBox.ErrorQuery(app, "Error", "El precio debe ser un número válido.", "OK");
        }
    };

    dialog.Add(lblCodigo, txtCodigo, lblNombre, txtNombre, lblPrecio, txtPrecio, btnGuardar, btnCancelar);
    app.Run(dialog);
}

void MostrarDialogoMovimiento(string tipo) {
    var producto = ObtenerProductoSeleccionado();
    if (producto == null) {
        MessageBox.ErrorQuery(app, "Aviso", "Selecciona un producto primero.", "OK");
        return;
    }

    var dialog = new Dialog { Title = $"Registrar {tipo}", Width = 40, Height = 8 };
    var lblCantidad = new Label { Text = "Cantidad:", X = 1, Y = 2 };
    var txtCantidad = new TextField { X = 12, Y = 2, Width = 15 };
    
    var btnGuardar = new Button { Text = "Guardar", X = 5, Y = 5 };
    var btnCancelar = new Button { Text = "Cancelar", X = Pos.Right(btnGuardar) + 2, Y = 5 };

    btnCancelar.Accepting += (_, _) => app.RequestStop();
    btnGuardar.Accepting += async (_, _) => {
        if (int.TryParse(txtCantidad.Text?.ToString(), out int cantidad) && cantidad > 0) {
            await RegistrarMovimiento(tipo, cantidad);
            app.RequestStop();
        } else {
            MessageBox.ErrorQuery(app, "Error", "Ingresa una cantidad entera mayor a 0.", "OK");
        }
    };

    dialog.Add(lblCantidad, txtCantidad, btnGuardar, btnCancelar);
    app.Run(dialog);
}

var menu = new MenuBar([
    new MenuBarItem("_Productos", [
        new MenuItem("_Agregar", "", MostrarDialogoAgregarProducto),
        new MenuItem("_Eliminar", "", async () => await EliminarProducto())
    ]),
    new MenuBarItem("_Movimientos", [
        new MenuItem("_Compra", "", () => MostrarDialogoMovimiento("Compra")),
        new MenuItem("_Venta", "", () => MostrarDialogoMovimiento("Venta")),
        new MenuItem("_Ajuste", "", () => MostrarDialogoMovimiento("Ajuste"))
    ])
]);


buscar.TextChanged += (_, _) => {
    ActualizarProductos();
    _ = CargarMovimientosSeleccionado();
};

listaProductos.ValueChanged += async (_, _) => await CargarMovimientosSeleccionado();

ActualizarProductos();
await CargarMovimientosSeleccionado();

ventana.Add(menu, etiquetaBuscar, buscar, listaProductos, listaMovimientos);

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http) {
    const string url = "http://localhost:5050/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url) 
        ?? throw new HttpRequestException("El servidor devolvio una lista vacia");
}

static async Task<List<MovimientoDto>> CargarMovimientosAsync(HttpClient http, int productoId) {
    string url = $"http://localhost:5050/productos/{productoId}/movimientos";
    return await http.GetFromJsonAsync<List<MovimientoDto>>(url) ?? [];
}

static string FormatearProducto(ProductoDto producto) {
    return $"{producto.Codigo,-8} | {producto.Nombre,-25} | ${producto.Precio,10:N2} | stock {producto.Stock,4}";
}

static string FormatearMovimiento(MovimientoDto movimiento) {
    return $"{movimiento.Tipo,-8} | {movimiento.Cantidad,4} | {movimiento.Fecha:dd/MM/yyyy HH:mm}";
}

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, DateTime Fecha, int Cantidad, string Tipo);
record ProductoCrearDto(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoCrearDto(string Tipo, int Cantidad);