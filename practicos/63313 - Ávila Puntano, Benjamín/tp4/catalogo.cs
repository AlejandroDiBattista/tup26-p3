#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Globalization;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string miserver = "http://localhost:5050"; 
using var http = new HttpClient { BaseAddress = new Uri(miserver) };
try {
   var lista = await http.GetFromJsonAsync<List<ProductoDto>>("/productos")
    ?? throw new HttpRequestException("El servidor no respondió con una lista de productos validos.");
} catch (HttpRequestException ex) {
    Console.WriteLine($"No se pudo conectar al servidor: {ex.Message}");
    return;
}
// ── Interfaz TUI ──────────────────────────────────────────────────────────
using IApplication app = Application.Create().Init();
using Window ventana = new() { Title = " Catalogo REST — Producto (ESC para salir) " };
var productos = new List<ProductoDto>();
var productosFiltrados = new List<ProductoDto>();
var productosVista = new ObservableCollection<string>();
var movimientosVista = new ObservableCollection<string>();
var buscar = new TextField { X = 11, Y = 1, Width = 36};
//36 d ancho, col 11, fila 1
var listaProductos = new ListView {
    X = 1, Y = 4, Width = Dim.Percent(50), Height = Dim.Fill(3),
}; // col 1, fila 4, ancho 50% del contenedor, alto hasta 3 filas antes del final
listaProductos.SetSource(productosVista);
var listaMovimientos = new ListView {
    X = Pos.Right(listaProductos) + 1, Y = 4, Width = Dim.Fill(1), Height = Dim.Fill(3)
}; 
listaMovimientos.SetSource(movimientosVista);
var estado = new Label { X = 1, Y = Pos.Bottom(listaProductos) + 1, Width = Dim.Fill(1)};
var botonagregar = new Button { Text = "_Agregar", X = 1, Y =2};
var botonModificar = new Button { Text = "_Modificar", X = Pos.Right(botonagregar) + 1, Y = 2};
var botonEliminar = new Button { Text = "_Eliminar", X = Pos.Right(botonModificar) + 1, Y = 2};
var botonComprar = new Button { Text = "_Comprar", X = Pos.Right(botonEliminar) + 2, Y = 2};
var botonVender = new Button { Text = "_Vender", X = Pos.Right(botonComprar) + 1, Y = 2};
var botonAjustar = new Button { Text = "_Ajustar", X = Pos.Right(botonVender) + 1, Y = 2};
var botonActualizar = new Button { Text = "_Actualizar", X = Pos.Right(botonAjustar) + 2, Y = 2};
ventana.Add(
    new Label { Text = "Buscar:", X = 1, Y = 1 }, buscar,
    new Label { Text = "Productos", X = 1, Y = 3 },
    new Label { Text = "Movimientos", X = Pos.Right(listaProductos) + 1, Y = 3 }, 
    botonagregar, botonModificar, botonEliminar,
     botonComprar, botonVender, botonAjustar, 
     botonActualizar, estado, listaProductos, listaMovimientos
);
TextField Campo(Dialog dialogo, string etiqueta, string valor, int y) {
    dialogo.Add(new Label{ Text = etiqueta, X = 2, Y = y });
    var campo = new TextField { Text = valor, X = 15, Y = y, Width = 38 };
    dialogo.Add(campo);
    return campo;
}
void MostrarError(string titulo, string mensaje) =>
    MessageBox.ErrorQuery(app, titulo, mensaje, "Aceptar");
string FormatearProducto(ProductoDto p) =>
    $"{p.Codigo,-8} {Cortar(p.Nombre, 24),-24} ${p.Precio,9:N2}  Stock:{p.Stock,5}";
string FormatearMovimiento(MovimientoDto m) =>
    $"{m.Fecha:dd/MM/yyyy HH:mm}  {m.Tipo,-7}  {m.Cantidad,6}";
string Cortar(string texto, int largo) =>
    texto.Length <= largo ? texto : texto[..Math.Max(0, largo - 3)] + "";
async Task RefrescarAsync(int? seleccionarId = null) {
    try {
        productos = await http.GetFromJsonAsync<List<ProductoDto>>("/productos") ?? [];
        FiltrarProductos(seleccionarId);
        await CargarMovimientosSeleccionadosAsync();
        estado.Text = $"Productos: {productos.Count} | API: {miserver}";
    } catch (Exception ex) {
        MostrarError("Error al cargar productos", ex.Message);
    }
}
void FiltrarProductos(int? seleccionarId = null) {
    var texto = buscar.Text?.ToString()?.Trim() ?? "";
    productosFiltrados = productos
        .Where(p =>
            texto.Length == 0 ||
            p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
            p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.Codigo)
        .ToList();
    productosVista.Clear();
    foreach (var producto in productosFiltrados)
        productosVista.Add(FormatearProducto(producto));
    if (productosFiltrados.Count == 0) {
        movimientosVista.Clear();
        return;
    }
    var indice = seleccionarId is null ? 0 : productosFiltrados.FindIndex(p => p.Id == seleccionarId);
    listaProductos.SelectedItem = indice >= 0 ? indice : 0;
}
ProductoDto? ProductoSeleccionado() {
    var indice = listaProductos.SelectedItem;
    if (indice is null || indice < 0 || indice >= productosFiltrados.Count) return null;
    return productosFiltrados[indice.Value];
}
async Task AgregarProductoAsync() {
    var entrada = DialogoProducto("Agregar producto", null);
    if (entrada is null) return;
    try {
        var respuesta = await http.PostAsJsonAsync("/productos", entrada);
        if (!respuesta.IsSuccessStatusCode) {
            MostrarError("No se pudo agregar", await respuesta.Content.ReadAsStringAsync());
            return;
        }
        var creado = await respuesta.Content.ReadFromJsonAsync<ProductoDto>();
        await RefrescarAsync(creado?.Id);
    } catch (Exception ex) {
        MostrarError("No se pudo agregar", ex.Message);
    }
}
async Task EditarProductoAsync() {
    var producto = ProductoSeleccionado();
    if (producto is null) return;
    var entrada = DialogoProducto("editar producto", producto);
    if (entrada is null) return;
    try {
        var respuesta = await http.PutAsJsonAsync($"/productos/{producto.Id}", entrada);
        if (!respuesta.IsSuccessStatusCode) {
            MostrarError("No se pudo editar", await respuesta.Content.ReadAsStringAsync());
            return;
        }
        await RefrescarAsync(producto.Id);
    } catch (Exception ex) {
        MostrarError("No se pudo editar", ex.Message);
    }
}
async Task EliminarProductoAsync() {
    var producto = ProductoSeleccionado();
    if (producto is null) 
        return;
    var opcion = MessageBox.Query(app, "Eliminar producto", $"Eliminar {producto.Codigo} - {producto.Nombre}?", "No", "Si");
    if (opcion != 1) 
        return;
    try {
        var respuesta = await http.DeleteAsync($"/productos/{producto.Id}");
        if (!respuesta.IsSuccessStatusCode) {
            MostrarError("no se puede eliminar", await respuesta.Content.ReadAsStringAsync());
            return;
        }
        await RefrescarAsync();
    } catch (Exception ex) {
        MostrarError("No se pudo eliminar", ex.Message);
    }
}
ProductoEntrada? DialogoProducto(string titulo, ProductoDto? producto) {
    using var dialogo = new Dialog { Title = $" {titulo} ", Width = 62, Height = 15 };
    var codigo = Campo(dialogo, "Codigo:", producto?.Codigo ?? "", 1);
    var nombre = Campo(dialogo, "Nombre:", producto?.Nombre ?? "", 3);
    var precio = Campo(dialogo, "Precio:", producto?.Precio.ToString(CultureInfo.InvariantCulture) ?? "0", 5);
    var stock = Campo(dialogo, "Stock:", producto?.Stock.ToString(CultureInfo.InvariantCulture) ?? "0", 7);
    var aceptar = new Button {Text= "_Guardar", X = 18, Y = 10, IsDefault = true};
    var cancelar = new Button {Text ="_Cancelar",X = Pos.Right(aceptar) + 2, Y = 10};
    ProductoEntrada? resultado = null;
    aceptar.Accepted += (_, _) => {
       if (!decimal.TryParse(precio.Text?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var precioValor)) {
            MostrarError("dato invalido", "el precio debe ser numerico");
            return;
        }
        if (!int.TryParse(stock.Text?.ToString(), out var stockValor)) {
            MostrarError("Dato invalido", "El stock debe ser un numero entero.");
            return;
        }
        resultado = new ProductoEntrada(codigo.Text?.ToString() ?? "", nombre.Text?.ToString() ?? "", precioValor, stockValor);
        app.RequestStop(dialogo);
    };
    cancelar.Accepted += (_, _) => app.RequestStop(dialogo);
    dialogo.Add(aceptar, cancelar);
    app.Run(dialogo);
    return resultado;
}
async Task CargarMovimientosSeleccionadosAsync() {
    movimientosVista.Clear();
    var producto = ProductoSeleccionado();
    if (producto is null) 
        return;
    try {
        var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{producto.Id}/movimientos") ?? []; 
        if (movimientos.Count == 0) { movimientosVista.Add("Sin movimientos registrados."); return; }
        foreach (var movimiento in movimientos)
            movimientosVista.Add(FormatearMovimiento(movimiento));
    } catch (Exception ex) {
        movimientosVista.Add($"Error: {ex.Message}");
    }
}
async Task RegistrarMovimientoAsync(TipoMovimiento tipo) {
    var producto = ProductoSeleccionado();
    if (producto is null) {
        MostrarError("error","no elegiste ningun producto");
        return; 
    }
    var cantidad = DialogoMovimiento(tipo, producto);
    if (cantidad is null) 
        return;
    try {
        var respuesta = await http.PostAsJsonAsync
        ($"/productos/{producto.Id}/movimientos", new MovimientoEntrada(tipo, cantidad.Value));

        if (!respuesta.IsSuccessStatusCode) {
            MostrarError("No se pudo registrar", await respuesta.Content.ReadAsStringAsync());
            return;
        }
        await RefrescarAsync(producto.Id);
    } catch (Exception ex) {
        MostrarError("No se pudo registrar", ex.Message);
    }
}
int? DialogoMovimiento(TipoMovimiento tipo, ProductoDto producto) {
    using var dialogo = new Dialog { Title = $" Registrar {tipo} ", Width = 66, Height = 11 };
    dialogo.Add(new Label { Text = $"{producto.Codigo} - {producto.Nombre} | Stock actual: {producto.Stock}", X = 2, Y = 1 });
    var etiqueta = tipo == TipoMovimiento.Ajuste ? "Nuevo stock:" : "Cantidad:";
    var cantidad = Campo(dialogo, etiqueta, "1", 3);
    var aceptar = new Button { Text = "_Registrar", X = 18, Y = 6, IsDefault = true };
    var cancelar = new Button { Text = "_Cancelar", X = Pos.Right(aceptar) + 2, Y = 6 };
    int? resultado = null;
    aceptar.Accepted += (_, _) => {
        if (!int.TryParse(cantidad.Text?.ToString(), out var cantidadValor) || cantidadValor < 0 || (tipo != TipoMovimiento.Ajuste && cantidadValor == 0)) {
            MostrarError("Dato invalido", tipo == TipoMovimiento.Ajuste
                ? "El nuevo stock debe ser cero o un entero positivo."
                : "La cantidad debe ser un entero positivo.");
            return;
        }
        resultado = cantidadValor;
        app.RequestStop(dialogo);
    };
    cancelar.Accepted += (_, _) => app.RequestStop(dialogo);
    dialogo.Add(aceptar, cancelar);
    app.Run(dialogo);
    return resultado;
}
buscar.TextChanged += async (_, _) => {
    FiltrarProductos();
    await CargarMovimientosSeleccionadosAsync();
};
listaProductos.ValueChanged += async (_, _) => await CargarMovimientosSeleccionadosAsync();
botonagregar.Accepted += async (_, _) => await AgregarProductoAsync();
botonModificar.Accepted += async (_, _) => await EditarProductoAsync();
botonEliminar.Accepted += async (_, _) => await EliminarProductoAsync();
botonComprar.Accepted += async (_, _) => await RegistrarMovimientoAsync(TipoMovimiento.Compra);
botonVender.Accepted += async (_, _) => await RegistrarMovimientoAsync(TipoMovimiento.Venta);
botonAjustar.Accepted += async (_, _) => await RegistrarMovimientoAsync(TipoMovimiento.Ajuste);
botonActualizar.Accepted += async (_, _) => await RefrescarAsync();
await RefrescarAsync();
app.Run(ventana);
// ── DTO ──────────────────────────────────────────────────────────────────
enum TipoMovimiento{Compra,Venta,Ajuste} //enum de los movimientos que se podran hacer
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock); // no agregamos un id xq el servidor lo asigna solo
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);
