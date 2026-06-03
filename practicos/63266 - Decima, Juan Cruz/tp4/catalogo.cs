#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

using Terminal.Gui.Views;


const string BaseUrl = "http://localhost:5050";

List<ProductoDto> productosIniciales;
try {
    using var clientePrueba = new HttpClient();
    productosIniciales = await clientePrueba.GetFromJsonAsync<List<ProductoDto>>($"{BaseUrl}/productos") ?? [];
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verifica que servidor.cs este corriendo en http://localhost:5050");
    return;
}

using IApplication appTui = Application.Create();
appTui.Init();

var ventana = new Window {
    Title = " CatalogoREST - F1 Agregar  F2 Editar  F3 Eliminar  F4 Movimiento  ESC Salir ",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
};

var lblBuscar = new Label {
    Text = "Buscar:",
    X = 1,
    Y = 0,
};

var txtBuscar = new TextField {
    X = Pos.Right(lblBuscar) + 1,
    Y = 0,
    Width = 30,
};

var listaProductos = new ListView {
    X = 0,
    Y = 2,
    Width = Dim.Percent(50),
    Height = Dim.Fill(1),
};

var lblMovimientos = new Label {
    Text = "Historial de movimientos",
    X = Pos.Percent(50) + 1,
    Y = 2,
};

var listaMovimientos = new ListView {
    X = Pos.Percent(50) + 1,
    Y = 3,
    Width = Dim.Fill(1),
    Height = Dim.Fill(1),
};

ventana.Add(lblBuscar, txtBuscar, listaProductos, lblMovimientos, listaMovimientos);


var productos = new List<ProductoDto>(productosIniciales);
var productosFiltrados = new List<ProductoDto>();
var http = new HttpClient();

txtBuscar.TextChanged += (_, _) => RefrescarLista();
listaProductos.ValueChanged += async (_, _) => await RefrescarMovimientos();

ventana.KeyDown += (_, tecla) => {
    if (tecla == Key.F1) {
        AgregarProducto();
        tecla.Handled = true;
    } else if (tecla == Key.F2) {
        EditarProducto();
        tecla.Handled = true;
    } else if (tecla == Key.F3) {
        EliminarProducto();
        tecla.Handled = true;
    } else if (tecla == Key.F4) {
        RegistrarMovimiento();
        tecla.Handled = true;
    } else if (tecla == Key.Esc) {
        appTui.RequestStop();
        tecla.Handled = true;
    }
};


RefrescarLista();

appTui.Run(ventana);
http.Dispose();

void RefrescarLista() {
    var filtro = txtBuscar.Text?.ToString()?.Trim() ?? "";

    productosFiltrados = string.IsNullOrEmpty(filtro)
        ? productos.ToList()
        : productos
            .Where(p => p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                     || p.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            .ToList();

    listaProductos.SetSource(new ObservableCollection<string>(productosFiltrados.Select(p =>
        $" {p.Codigo,-8} {p.Nombre,-25} ${p.Precio,10:N2}  Stock: {p.Stock,5}"
    )));

    listaMovimientos.SetSource(new ObservableCollection<string>());
}

async Task RefrescarMovimientos() {
    var producto = ObtenerProductoSeleccionado();
    if (producto is null) return;

    try {
        var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>(
            $"{BaseUrl}/productos/{producto.Id}/movimientos") ?? [];

        if (movimientos.Count == 0) {
            listaMovimientos.SetSource(new ObservableCollection<string>(["  (sin movimientos)"]));
        } else {
            listaMovimientos.SetSource(new ObservableCollection<string>(movimientos.Select(m =>
                $" {m.Tipo,-8}  {(m.Tipo == "Ajuste" ? "->" : m.Tipo == "Compra" ? "+" : "-")}{m.Cantidad,5}  {m.Fecha:dd/MM/yyyy HH:mm}"
            )));
        }
    } catch {
        listaMovimientos.SetSource(new ObservableCollection<string>(["  Error al cargar movimientos"]));
    }
}


async Task RecargarProductos() {
    try {
        productos = await http.GetFromJsonAsync<List<ProductoDto>>($"{BaseUrl}/productos") ?? [];
        RefrescarLista();
    } catch {
        MessageBox.ErrorQuery(appTui, "Error", "No se pudieron recargar los productos.", "OK");
    }
}

ProductoDto? ObtenerProductoSeleccionado() {
    if (productosFiltrados.Count == 0) return null;

    var idx = listaProductos.SelectedItem;
    if (idx is null || idx < 0 || idx >= productosFiltrados.Count) return null;
    return productosFiltrados[idx.Value];
}

async void AgregarProducto() {
    var (codigo, nombre, precio, stock, confirmado) = MostrarDialogoProducto("Agregar Producto");
    if (!confirmado) return;
    if (!ProductoIngresadoValido(codigo, nombre, precio, stock)) return;

    try {
        var dto = new { Codigo = codigo, Nombre = nombre, Precio = precio, Stock = stock };
        var resp = await http.PostAsJsonAsync($"{BaseUrl}/productos", dto);
        if (resp.IsSuccessStatusCode)
            await RecargarProductos();
        else
            MessageBox.ErrorQuery(appTui, "Error", await LeerError(resp, "No se pudo agregar el producto."), "OK");
    } catch {
        MessageBox.ErrorQuery(appTui, "Error", "Error al conectar con el servidor.", "OK");
    }
}

async void EditarProducto() {
    var producto = ObtenerProductoSeleccionado();
    if (producto is null) {
        MessageBox.ErrorQuery(appTui, "Editar", "Selecciona un producto de la lista.", "OK");
        return;
    }

    var (codigo, nombre, precio, stock, confirmado) =
        MostrarDialogoProducto("Editar Producto", producto.Codigo, producto.Nombre, producto.Precio, producto.Stock);
    if (!confirmado) return;
    if (!ProductoIngresadoValido(codigo, nombre, precio, stock)) return;

    try {
        var dto = new { Codigo = codigo, Nombre = nombre, Precio = precio, Stock = stock };
        var resp = await http.PutAsJsonAsync($"{BaseUrl}/productos/{producto.Id}", dto);
        if (resp.IsSuccessStatusCode)
            await RecargarProductos();
        else
            MessageBox.ErrorQuery(appTui, "Error", await LeerError(resp, "No se pudo modificar el producto."), "OK");
    } catch {
        MessageBox.ErrorQuery(appTui, "Error", "Error al conectar con el servidor.", "OK");
    }
}

async void EliminarProducto() {
    var producto = ObtenerProductoSeleccionado();
    if (producto is null) {
        MessageBox.ErrorQuery(appTui, "Eliminar", "Selecciona un producto de la lista.", "OK");
        return;
    }

    var confirmar = MessageBox.Query(appTui, "Eliminar Producto",
        $"Eliminar '{producto.Nombre}'?", "Si", "No");
    if (confirmar != 0) return;

    try {
        var resp = await http.DeleteAsync($"{BaseUrl}/productos/{producto.Id}");
        if (resp.IsSuccessStatusCode)
            await RecargarProductos();
        else
            MessageBox.ErrorQuery(appTui, "Error", "No se pudo eliminar el producto.", "OK");
    } catch {
        MessageBox.ErrorQuery(appTui, "Error", "Error al conectar con el servidor.", "OK");
    }
}

async Task<string> LeerError(HttpResponseMessage resp, string mensajePorDefecto) {
    var detalle = await resp.Content.ReadAsStringAsync();
    return string.IsNullOrWhiteSpace(detalle) ? mensajePorDefecto : detalle.Trim('"');
}

bool ProductoIngresadoValido(string codigo, string nombre, decimal precio, int stock) {
    if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre)) {
        MessageBox.ErrorQuery(appTui, "Producto", "Codigo y nombre son obligatorios.", "OK");
        return false;
    }

    if (precio < 0 || stock < 0) {
        MessageBox.ErrorQuery(appTui, "Producto", "Precio y stock no pueden ser negativos.", "OK");
        return false;
    }

    return true;
}

(string Codigo, string Nombre, decimal Precio, int Stock, bool Confirmado)
MostrarDialogoProducto(string titulo,
    string codigoInicial = "", string nombreInicial = "",
    decimal precioInicial = 0m, int stockInicial = 0) {

    var dialogo = new Dialog {
        Title = titulo,
        Width = 50,
        Height = 14,
    };

    var lblCodigo = new Label { Text = "Codigo:", X = 1, Y = 1 };
    var lblNombre = new Label { Text = "Nombre:", X = 1, Y = 3 };
    var lblPrecio = new Label { Text = "Precio:", X = 1, Y = 5 };
    var lblStock  = new Label { Text = "Stock:",  X = 1, Y = 7 };

    var txtCodigo = new TextField { Text = codigoInicial,               X = 10, Y = 1, Width = 35 };
    var txtNombre = new TextField { Text = nombreInicial,               X = 10, Y = 3, Width = 35 };
    var txtPrecio = new TextField { Text = precioInicial.ToString("F2"), X = 10, Y = 5, Width = 15 };
    var txtStock  = new TextField { Text = stockInicial.ToString(),     X = 10, Y = 7, Width = 10 };

    bool confirmado = false;

    var btnAceptar = new Button {
        Text = "Aceptar",
        X = Pos.Center() - 10, Y = 10,
        IsDefault = true,
    };
    btnAceptar.Accepting += (_, _) => {
        confirmado = true;
        appTui.RequestStop(dialogo);
    };

    var btnCancelar = new Button {
        Text = "Cancelar",
        X = Pos.Center() + 2, Y = 10,
    };
    btnCancelar.Accepting += (_, _) => appTui.RequestStop(dialogo);

    dialogo.Add(lblCodigo, lblNombre, lblPrecio, lblStock,
                txtCodigo, txtNombre, txtPrecio, txtStock,
                btnAceptar, btnCancelar);

    appTui.Run(dialogo);

    if (!confirmado) return ("", "", 0m, 0, false);

    decimal.TryParse(txtPrecio.Text?.ToString(), out var precio);
    int.TryParse(txtStock.Text?.ToString(), out var stock);

    return (txtCodigo.Text?.ToString() ?? "",
            txtNombre.Text?.ToString() ?? "",
            precio, stock, true);
}

async void RegistrarMovimiento() {
    var producto = ObtenerProductoSeleccionado();
    if (producto is null) {
        MessageBox.ErrorQuery(appTui, "Movimiento", "Selecciona un producto de la lista.", "OK");
        return;
    }

    var (tipo, cantidad, confirmado) = MostrarDialogoMovimiento(producto.Nombre);
    if (!confirmado) return;
    if (cantidad <= 0) {
        MessageBox.ErrorQuery(appTui, "Movimiento", "La cantidad debe ser positiva.", "OK");
        return;
    }

    try {
        var dto = new { Tipo = tipo, Cantidad = cantidad };
        var resp = await http.PostAsJsonAsync($"{BaseUrl}/productos/{producto.Id}/movimientos", dto);
        if (resp.IsSuccessStatusCode) {
            await RecargarProductos();
            await RefrescarMovimientos();
        } else {
            MessageBox.ErrorQuery(appTui, "Error", await LeerError(resp, "No se pudo registrar el movimiento."), "OK");
        }
    } catch {
        MessageBox.ErrorQuery(appTui, "Error", "Error al conectar con el servidor.", "OK");
    }
}


(string Tipo, int Cantidad, bool Confirmado)
MostrarDialogoMovimiento(string nombreProducto) {

    var dialogo = new Dialog {
        Title = $"Registrar Movimiento - {nombreProducto}",
        Width = 50,
        Height = 12,
    };

    var lblTipo     = new Label { Text = "Tipo:",     X = 1, Y = 1 };
    var lblCantidad = new Label { Text = "Cantidad:", X = 1, Y = 5 };

    var tipos = new[] { "Compra", "Venta", "Ajuste" };
    var listaTipos = new ListView {
        X = 10, Y = 1,
        Width = 15,
        Height = 3,
    };
    listaTipos.SetSource(new ObservableCollection<string>(tipos));

    var txtCantidad = new TextField { X = 10, Y = 5, Width = 10 };

    bool confirmado = false;

    var btnAceptar = new Button {
        Text = "Aceptar",
        X = Pos.Center() - 10, Y = 8,
        IsDefault = true,
    };
    btnAceptar.Accepting += (_, _) => {
        confirmado = true;
        appTui.RequestStop(dialogo);
    };

    var btnCancelar = new Button {
        Text = "Cancelar",
        X = Pos.Center() + 2, Y = 8,
    };
    btnCancelar.Accepting += (_, _) => appTui.RequestStop(dialogo);

    dialogo.Add(lblTipo, lblCantidad, listaTipos, txtCantidad, btnAceptar, btnCancelar);

    appTui.Run(dialogo);

    if (!confirmado) return ("", 0, false);

    var tipo = tipos[listaTipos.SelectedItem ?? 0];
    int.TryParse(txtCantidad.Text?.ToString(), out var cantidad);

    return (tipo, cantidad, true);
}


static async Task<ProductoDto> CargarProductoAsync(HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}


record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
