#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;


HttpClient http = new HttpClient();
List<ProductoDto>       productos          = new();
List<ProductoDto>       productosFiltrados = new();
List<MovimientoDeProducto> movimientos     = new();
ProductoDto?            seleccionado       = null;
string                  filtro             = "";
try {
    productos          = Http_GET_Productos();
    productosFiltrados = new(productos);
    seleccionado       = productos.FirstOrDefault();
}
catch (Exception ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    return;
}
using IApplication app    = Application.Create().Init();
using Window       ventana = new() { Title = "Catalogo REST — Productos (ESC para salir)" };
var labelBuscar = new Label { X = 1, Y = 1, Text = "Buscar: " };
var searchBox = new TextField
{
    X = Pos.Right(labelBuscar), Y = 1, Width = 45
};
var listView = new ListView
{
    X = 1, Y = 3, Width = 56, Height = 17
};
var lblDetalleTitulo = new Label
{
    X = 59, Y = 3,
    Text = "── Detalle ──────────────────────"
};
var lblDetalle = new Label
{
    X = 59, Y = 4, Width = 38, Height = 7,
    Text = "Sin selección"
};
var lblMovTitulo = new Label
{
    X = 59, Y = 12,
    Text = "── Movimientos ──────────────────"
};
var movView = new ListView
{
    X = 59, Y = 13, Width = 55, Height = 8
};
List<ProductoDto> Http_GET_Productos() =>
    Task.Run(() => http.GetFromJsonAsync<List<ProductoDto>>("http://localhost:5050/productos"))
        .GetAwaiter().GetResult() ?? new();
 
List<MovimientoDeProducto> Http_GET_Movimientos(int id) =>
    Task.Run(() => http.GetFromJsonAsync<List<MovimientoDeProducto>>(
        $"http://localhost:5050/productos/{id}/movimientos"))
        .GetAwaiter().GetResult() ?? new();
 
HttpResponseMessage Http_POST_Producto(ProductoDto p) =>
    Task.Run(() => http.PostAsJsonAsync("http://localhost:5050/productos", p))
        .GetAwaiter().GetResult();
 
HttpResponseMessage Http_PUT_Producto(int id, ProductoDto p) =>
    Task.Run(() => http.PutAsJsonAsync($"http://localhost:5050/productos/{id}", p))
        .GetAwaiter().GetResult();
 
HttpResponseMessage Http_DELETE_Producto(int id) =>
    Task.Run(() => http.DeleteAsync($"http://localhost:5050/productos/{id}"))
        .GetAwaiter().GetResult();
 
HttpResponseMessage Http_POST_Movimiento(int id, MovimientoDto m) =>
    Task.Run(() => http.PostAsJsonAsync(
        $"http://localhost:5050/productos/{id}/movimientos", m))
        .GetAwaiter().GetResult();
string MostrarInputBox(string titulo, string valorInicial = "")
{
    string resultado = "";
 
    var dialog = new Dialog { Title = titulo, Width = 52, Height = 8 };
 
    var campo = new TextField
    {
        X = 1, Y = 1, Width = 46,
        Text = valorInicial
    };
 
    var btnOk = new Button
    {
        Text = "Aceptar", X = 1, Y = 3, IsDefault = true
    };
    var btnCancelar = new Button
    {
        Text = "Cancelar", X = 13, Y = 3
    };
 
    btnOk.Accepting      += (_, _) => { resultado = campo.Text?.ToString() ?? ""; dialog.RequestStop(); };
    btnCancelar.Accepting += (_, _) => { resultado = ""; dialog.RequestStop(); };
 
    dialog.Add(campo, btnOk, btnCancelar);
    campo.SetFocus();
    app.Run(dialog); 
 
    return resultado;
}
void MostrarMensaje(string titulo, string texto) =>
    MessageBox.Query(app, titulo, texto, "Aceptar");
static string FmtProducto(ProductoDto p) =>
    $"{p.Id,-4} {p.Codigo,-8} {p.Nombre,-20} ${p.Precio,9:N2}  Stock:{p.Stock,5}";
static string FmtDetalle(ProductoDto? p) =>
    p is null ? "Sin selección" :
    $"Id     : {p.Id}\n" +
    $"Código : {p.Codigo}\n" +
    $"Nombre : {p.Nombre}\n" +
    $"Precio : ${p.Precio:N2}\n" +
    $"Stock  : {p.Stock}";
void Refrescar()
{
    productos          = Http_GET_Productos();
    productosFiltrados = string.IsNullOrWhiteSpace(filtro)
        ? new(productos)
        : productos.Where(p =>
            p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
            p.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();
 
    var lista = string.IsNullOrWhiteSpace(filtro) ? productos : productosFiltrados;
    listView.SetSource(new ObservableCollection<string>(lista.Select(FmtProducto)));
    seleccionado   = lista.FirstOrDefault();
    lblDetalle.Text = FmtDetalle(seleccionado);
    if (seleccionado is not null)
        MostrarMovimientos(seleccionado.Id);
}
void MostrarMovimientos(int productoId)
{
    try {
        movimientos = Http_GET_Movimientos(productoId);
        movView.SetSource(new ObservableCollection<string>(
            movimientos.Select(m => $"{m.Tipo,-8} | {m.Cantidad,5} | {m.Fecha:dd/MM/yy HH:mm}")
        ));
    }
    catch { /* servidor no disponible momentáneamente */ }
}
void ActualizarSeleccion()
{
    int? raw = listView.SelectedItem;
    if (raw is null) return;
    int idx  = (int)raw;
    var lista = string.IsNullOrWhiteSpace(filtro) ? productos : productosFiltrados;
    if (idx < 0 || idx >= lista.Count) return;
    if (seleccionado?.Id == lista[idx].Id) return;
    seleccionado    = lista[idx];
    lblDetalle.Text = FmtDetalle(seleccionado);
    MostrarMovimientos(seleccionado.Id);
}
listView.KeyDown   += (_, _) => ActualizarSeleccion();
listView.MouseEvent += (_, _) => ActualizarSeleccion();
listView.SetSource(new ObservableCollection<string>(productos.Select(FmtProducto)));
 
searchBox.TextChanged += (_, _) =>
{
    filtro = searchBox.Text?.ToString() ?? "";
 
    productosFiltrados = string.IsNullOrWhiteSpace(filtro)
        ? new(productos)
        : productos.Where(p =>
            p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
            p.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();
 
    listView.SetSource(new ObservableCollection<string>(
        productosFiltrados.Select(FmtProducto)));
 
    seleccionado    = productosFiltrados.FirstOrDefault();
    lblDetalle.Text = FmtDetalle(seleccionado);
    movView.SetSource(new ObservableCollection<string>());
};
var menu = new MenuBar(new MenuBarItem[]
{
    new("_Productos", new MenuItem[]
    {
        new("_Agregar", "Ctrl+A", () =>
        {
            string nombre = MostrarInputBox("Nombre del producto");
            if (string.IsNullOrWhiteSpace(nombre)) return;
 
            string codigo = MostrarInputBox("Código");
            if (string.IsNullOrWhiteSpace(codigo)) return;
 
            string precioStr = MostrarInputBox("Precio");
            if (!decimal.TryParse(precioStr, out decimal precio)) {
                MostrarMensaje("Error", "Precio inválido.");
                return;
            }
 
            string stockStr = MostrarInputBox("Stock inicial");
            if (!int.TryParse(stockStr, out int stock)) {
                MostrarMensaje("Error", "Stock inválido.");
                return;
            }
 
            try {
                var resp = Http_POST_Producto(new ProductoDto(0, codigo, nombre, precio, stock));
                if (resp.IsSuccessStatusCode) {
                    Refrescar();
                    MostrarMensaje("OK", "Producto agregado correctamente.");
                } else {
                    MostrarMensaje("Error", $"Error del servidor: {resp.StatusCode}");
                }
            } catch (Exception ex) {
                MostrarMensaje("Error", $"Error de conexión: {ex.Message}");
            }
        }),
        new("_Editar", "Ctrl+E", () =>
        {
            ActualizarSeleccion();
            if (seleccionado is null) {
                MostrarMensaje("Aviso", "Seleccione un producto primero.");
                return;
            }
 
            int    idEditar = seleccionado.Id;
            string nombre   = MostrarInputBox("Nombre",  seleccionado.Nombre);
            if (string.IsNullOrWhiteSpace(nombre)) return;
 
            string codigo   = MostrarInputBox("Código",  seleccionado.Codigo);
            if (string.IsNullOrWhiteSpace(codigo)) return;
 
            string precioStr = MostrarInputBox("Precio", seleccionado.Precio.ToString("F2"));
            if (!decimal.TryParse(precioStr, out decimal precio)) {
                MostrarMensaje("Error", "Precio inválido.");
                return;
            }
 
            string stockStr = MostrarInputBox("Stock", seleccionado.Stock.ToString());
            if (!int.TryParse(stockStr, out int stock)) {
                MostrarMensaje("Error", "Stock inválido.");
                return;
            }
 
            try {
                var resp = Http_PUT_Producto(idEditar,
                    new ProductoDto(idEditar, codigo, nombre, precio, stock));
                if (resp.IsSuccessStatusCode) {
                    Refrescar();
                    MostrarMensaje("OK", "Producto editado correctamente.");
                } else {
                    MostrarMensaje("Error", $"Error del servidor: {resp.StatusCode}");
                }
            } catch (Exception ex) {
                MostrarMensaje("Error", $"Error de conexión: {ex.Message}");
            }
        }),
        new("Eli_minar", "Ctrl+D", () =>
        {
            ActualizarSeleccion();
            if (seleccionado is null) {
                MostrarMensaje("Aviso", "Seleccione un producto primero.");
                return;
            }
 
            int    idElim     = seleccionado.Id;
            string nombreElim = seleccionado.Nombre;
 
            int? confirm = MessageBox.Query(app, "Confirmar",
                $"¿Eliminar '{nombreElim}'?", "Sí", "No");
            if (confirm.GetValueOrDefault(1) != 0) return;
 
            try {
                var resp = Http_DELETE_Producto(idElim);
                if (resp.IsSuccessStatusCode) {
                    seleccionado    = null;
                    lblDetalle.Text = "Sin selección";
                    movView.SetSource(new ObservableCollection<string>());
                    Refrescar();
                    MostrarMensaje("OK", "Producto eliminado correctamente.");
                } else {
                    MostrarMensaje("Error", $"Error del servidor: {resp.StatusCode}");
                }
            } catch (Exception ex) {
                MostrarMensaje("Error", $"Error de conexión: {ex.Message}");
            }
        })
    }),
 
    new("_Movimientos", new MenuItem[]
    {
        new("_Compra",  "Ctrl+C", () => RegistrarMovimiento(TipoMovimiento.Compra)),
        new("_Venta",   "Ctrl+V", () => RegistrarMovimiento(TipoMovimiento.Venta)),
        new("_Ajuste",  "Ctrl+J", () => RegistrarMovimiento(TipoMovimiento.Ajuste))
    })
});
ventana.Add(labelBuscar, searchBox, listView,
            lblDetalleTitulo, lblDetalle,
            lblMovTitulo, movView,
            menu);
 
lblDetalle.Text = FmtDetalle(seleccionado);
 
app.Run(ventana);

void RegistrarMovimiento(TipoMovimiento tipo)
{
    ActualizarSeleccion();
    if (seleccionado is null) {
        MostrarMensaje("Aviso", "Seleccione un producto primero.");
        return;
    }
    int productoId = seleccionado.Id;
    string cantStr = MostrarInputBox($"Cantidad para {tipo}");
    if (string.IsNullOrWhiteSpace(cantStr)) return;
 
    if (!int.TryParse(cantStr, out int cantidad) || cantidad <= 0) {
        MostrarMensaje("Error", "Ingrese un número entero positivo.");
        return;
    }
    try {
        var resp = Http_POST_Movimiento(productoId, new MovimientoDto(tipo, cantidad));
 
        if (resp.IsSuccessStatusCode) {
            Refrescar();
            MostrarMovimientos(productoId);
            MostrarMensaje("OK", $"{tipo} registrada correctamente.");
        } else {
            string body = Task.Run(() => resp.Content.ReadAsStringAsync())
                              .GetAwaiter().GetResult();
            MostrarMensaje("Error", $"No se pudo registrar: {body}");
        }
    } catch (Exception ex) {
        MostrarMensaje("Error", $"Error de conexión: {ex.Message}");
    }
}
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(TipoMovimiento Tipo, int Cantidad);
enum TipoMovimiento { Compra, Venta, Ajuste }
record MovimientoDeProducto(
    int Id,
    int ProductoId,
    TipoMovimiento Tipo,
    int Cantidad,
    DateTime Fecha
);