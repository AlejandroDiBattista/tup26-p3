#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ── Constantes ────────────────────────────────────────────────────────────

const string UrlServidor    = "http://localhost:5050";
const string RutaProductos  = "/productos";
const string MensajeConexionFallida = "Verificá que servidor.cs esté corriendo en " + UrlServidor;

// ── Conexión inicial ──────────────────────────────────────────────────────

using HttpClient http = new() { BaseAddress = new Uri(UrlServidor) };
List<ProductoDto> productos;

try {
    productos = await ServicioApi.CargarProductos(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine(MensajeConexionFallida);
    return;
}

// ── Estado de la interfaz ─────────────────────────────────────────────────

List<ProductoDto> filtrados = new(productos);

// ── Construcción de la interfaz TUI ──────────────────────────────────────

Menu.DefaultBorderStyle = LineStyle.Rounded;
using IApplication app = Application.Create().Init();

Runnable raiz = new();

MenuBar menu = new([
    new("_Producto", new MenuItem[] {
        new("_Agregar",  "Ctrl+A",  AgregarProducto,  Key.A.WithCtrl),
        new("_Editar",   "Ctrl+E",  EditarProducto,   Key.E.WithCtrl),
        new("_Eliminar", "Ctrl+D",  EliminarProducto, Key.D.WithCtrl),
        null!,
        new("_Salir",    "Ctrl+Q",  () => app.RequestStop(), Key.Q.WithCtrl),
    }),
    new("_Movimiento", new MenuItem[] {
        new("_Registrar", "Ctrl+M", RegistrarMovimiento, Key.M.WithCtrl),
    }),
]);

Window ventana = new() {
    Title  = " Catalogo REST — Productos ",
    X      = 0,
    Y      = 1,
    Width  = Dim.Fill(),
    Height = Dim.Fill(),
};

Label etiquetaBuscar = new() { Text = "Buscar:", X = 1, Y = 1 };
TextField campoBuscar = new() {
    X     = Pos.Right(etiquetaBuscar) + 1,
    Y     = 1,
    Width = 30,
};

FrameView panelProductos = new() {
    Title  = "Productos",
    X      = 0,
    Y      = 3,
    Width  = Dim.Percent(50),
    Height = Dim.Fill(),
};

FrameView panelMovimientos = new() {
    Title  = "Movimientos",
    X      = Pos.Right(panelProductos),
    Y      = 3,
    Width  = Dim.Fill(),
    Height = Dim.Fill(),
};

ListView listaProductos = new() {
    X      = 0,
    Y      = 0,
    Width  = Dim.Fill(),
    Height = Dim.Fill(),
};

Label etiquetaDetalle = new() {
    Text   = "Seleccione un producto.",
    X      = 1,
    Y      = 1,
    Width  = Dim.Fill(2),
    Height = Dim.Fill(2),
};

panelProductos.Add(listaProductos);
panelMovimientos.Add(etiquetaDetalle);
ventana.Add(etiquetaBuscar, campoBuscar, panelProductos, panelMovimientos);
raiz.Add(menu, ventana);

// ── Eventos de la interfaz ────────────────────────────────────────────────

campoBuscar.TextChanged += (_, _) => ActualizarLista();

listaProductos.ValueChanged += async (_, _) => await MostrarMovimientos();

ActualizarLista();
await MostrarMovimientos();

app.Run(raiz);

// ── Lógica de la interfaz ─────────────────────────────────────────────────

void ActualizarLista() {
    string busqueda = campoBuscar.Text?.Trim() ?? "";
    filtrados = string.IsNullOrWhiteSpace(busqueda)
        ? new(productos)
        : productos.Where(p =>
            p.Codigo.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
            p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
          .ToList();

    listaProductos.SetSource(new ObservableCollection<string>(
        filtrados.Select(FormatearFila).ToList()));
}

async Task MostrarMovimientos() {
    int indice = listaProductos.SelectedItem ?? 0;
    if (indice < 0 || indice >= filtrados.Count) {
        etiquetaDetalle.Text = "Seleccione un producto.";
        return;
    }

    var producto     = filtrados[indice];
    var movimientos  = await ServicioApi.CargarMovimientos(http, producto.Id);
    var textoMovimie = movimientos.Count == 0
        ? "Sin movimientos registrados."
        : string.Join("\n", movimientos.Select(FormatearMovimiento));

    etiquetaDetalle.Text = $"""
        PRODUCTO

        Id     : {producto.Id}
        Codigo : {producto.Codigo}
        Nombre : {producto.Nombre}
        Precio : ${producto.Precio:N2}
        Stock  : {producto.Stock}

        MOVIMIENTOS

        {textoMovimie}
        """;
}

async Task RecargarYSeleccionar(int? idSeleccionado = null) {
    productos = await ServicioApi.CargarProductos(http);
    ActualizarLista();

    if (idSeleccionado.HasValue) {
        int pos = filtrados.FindIndex(p => p.Id == idSeleccionado.Value);
        if (pos >= 0) listaProductos.SelectedItem = pos;
    }

    await MostrarMovimientos();
}

void AgregarProducto() {
    using var dialogo = new DialogoProducto("Agregar producto", null);
    app.Run(dialogo);
    if (dialogo.Result is null) return;

    _ = Task.Run(async () => {
        try {
            var resp = await http.PostAsJsonAsync(RutaProductos, dialogo.Result);
            if (!resp.IsSuccessStatusCode) {
                MostrarError(await resp.Content.ReadAsStringAsync());
                return;
            }
            var nuevo = await resp.Content.ReadFromJsonAsync<ProductoDto>();
            await RecargarYSeleccionar(nuevo?.Id);
        } catch (Exception ex) {
            MostrarError(ex.Message);
        }
    });
}

void EditarProducto() {
    int indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count) {
        MessageBox.Query(app, "Editar", "Seleccione un producto primero.", "OK");
        return;
    }

    var actual = filtrados[indice];
    using var dialogo = new DialogoProducto("Editar producto", actual);
    app.Run(dialogo);
    if (dialogo.Result is null) return;

    _ = Task.Run(async () => {
        try {
            var resp = await http.PutAsJsonAsync($"{RutaProductos}/{actual.Id}", dialogo.Result);
            if (!resp.IsSuccessStatusCode) {
                MostrarError(await resp.Content.ReadAsStringAsync());
                return;
            }
            await RecargarYSeleccionar(actual.Id);
        } catch (Exception ex) {
            MostrarError(ex.Message);
        }
    });
}

void EliminarProducto() {
    int indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count) {
        MessageBox.Query(app, "Eliminar", "Seleccione un producto primero.", "OK");
        return;
    }

    var actual    = filtrados[indice];
    int respuesta = MessageBox.Query(app, "Eliminar", $"¿Eliminar '{actual.Nombre}'?", "No", "Si") ?? 0;
    if (respuesta != 1) return;

    _ = Task.Run(async () => {
        try {
            var resp = await http.DeleteAsync($"{RutaProductos}/{actual.Id}");
            if (!resp.IsSuccessStatusCode) {
                MostrarError("No se pudo eliminar el producto.");
                return;
            }
            await RecargarYSeleccionar();
        } catch (Exception ex) {
            MostrarError(ex.Message);
        }
    });
}

void RegistrarMovimiento() {
    int indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count) {
        MessageBox.Query(app, "Movimiento", "Seleccione un producto primero.", "OK");
        return;
    }

    var actual = filtrados[indice];
    using var dialogo = new DialogoMovimiento(actual.Nombre);
    app.Run(dialogo);
    if (dialogo.Result is null) return;

    _ = Task.Run(async () => {
        try {
            var resp = await http.PostAsJsonAsync(
                $"{RutaProductos}/{actual.Id}/movimientos", dialogo.Result);
            if (!resp.IsSuccessStatusCode) {
                MostrarError(await resp.Content.ReadAsStringAsync());
                return;
            }
            await RecargarYSeleccionar(actual.Id);
        } catch (Exception ex) {
            MostrarError(ex.Message);
        }
    });
}

void MostrarError(string mensaje) =>
    MessageBox.ErrorQuery(app, "Error", mensaje, "OK");

// ── Funciones de formato ──────────────────────────────────────────────────

static string FormatearFila(ProductoDto p) =>
    $"{p.Codigo,-6}  {p.Nombre,-24}  ${p.Precio,8:N2}  Stock:{p.Stock,5}";

static string FormatearMovimiento(MovimientoDto m) =>
    $"{m.Fecha:dd/MM/yyyy HH:mm}  {m.Tipo,-7}  Cant: {m.Cantidad}";

// ── Servicio de API ───────────────────────────────────────────────────────

static class ServicioApi {
    const string RutaProductos = "/productos";

    public static Task<List<ProductoDto>> CargarProductos(HttpClient http) =>
        http.GetFromJsonAsync<List<ProductoDto>>(RutaProductos)
            .ContinueWith(t => t.Result ?? []);

    public static Task<List<MovimientoDto>> CargarMovimientos(HttpClient http, int productoId) =>
        http.GetFromJsonAsync<List<MovimientoDto>>($"{RutaProductos}/{productoId}/movimientos")
            .ContinueWith(t => t.Result ?? []);
}

// ── DTOs ──────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
record ProductoDatos(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDatos(string Tipo, int Cantidad);

// ── Diálogo de producto ───────────────────────────────────────────────────

class DialogoProducto : Dialog<ProductoDatos?> {
    public DialogoProducto(string titulo, ProductoDto? existente) {
        Title  = titulo;
        Width  = 55;
        Height = 15;

        Label etiquetaCodigo = new() { Text = "Codigo :", X = 1, Y = 1 };
        TextField campoCodigo = new() {
            Text  = existente?.Codigo ?? "",
            X     = 11, Y = 1, Width = 30,
        };

        Label etiquetaNombre = new() { Text = "Nombre :", X = 1, Y = 3 };
        TextField campoNombre = new() {
            Text  = existente?.Nombre ?? "",
            X     = 11, Y = 3, Width = 30,
        };

        Label etiquetaPrecio = new() { Text = "Precio :", X = 1, Y = 5 };
        TextField campoPrecio = new() {
            Text  = existente?.Precio.ToString() ?? "0",
            X     = 11, Y = 5, Width = 15,
        };

        Label etiquetaStock = new() { Text = "Stock  :", X = 1, Y = 7 };
        TextField campoStock = new() {
            Text  = existente?.Stock.ToString() ?? "0",
            X     = 11, Y = 7, Width = 10,
        };

        Label etiquetaError = new() { Text = "", X = 1, Y = 9, Width = Dim.Fill(2) };

        Add(etiquetaCodigo, campoCodigo, etiquetaNombre, campoNombre,
            etiquetaPrecio, campoPrecio, etiquetaStock, campoStock, etiquetaError);

        Button btnCancelar = new() { Title = "Cancelar" };
        btnCancelar.Accepting += (_, _) => Result = null;
        AddButton(btnCancelar);

        Button btnGuardar = new() { Title = "Guardar" };
        btnGuardar.Accepting += (_, e) => {
            if (string.IsNullOrWhiteSpace(campoCodigo.Text)) {
                etiquetaError.Text = "El codigo es obligatorio.";
                e.Handled = true;
                campoCodigo.SetFocus();
                return;
            }
            if (string.IsNullOrWhiteSpace(campoNombre.Text)) {
                etiquetaError.Text = "El nombre es obligatorio.";
                e.Handled = true;
                campoNombre.SetFocus();
                return;
            }
            if (!decimal.TryParse(campoPrecio.Text, out decimal precio) || precio < 0) {
                etiquetaError.Text = "Precio invalido.";
                e.Handled = true;
                campoPrecio.SetFocus();
                return;
            }
            if (!int.TryParse(campoStock.Text, out int stock) || stock < 0) {
                etiquetaError.Text = "Stock invalido.";
                e.Handled = true;
                campoStock.SetFocus();
                return;
            }
            Result = new ProductoDatos(campoCodigo.Text.Trim(), campoNombre.Text.Trim(), precio, stock);
        };
        AddButton(btnGuardar);
    }
}

// ── Diálogo de movimiento ─────────────────────────────────────────────────

class DialogoMovimiento : Dialog<MovimientoDatos?> {
    static readonly string[] OpcionesTipo = ["Compra", "Venta", "Ajuste"];

    public DialogoMovimiento(string nombreProducto) {
        Title  = $"Movimiento: {nombreProducto}";
        Width  = 55;
        Height = 13;

        Label etiquetaTipo = new() { Text = "Tipo:", X = 1, Y = 1 };
        RadioGroup selectorTipo = new() {
            RadioLabels = OpcionesTipo,
            X           = 1,
            Y           = 2,
        };

        Label etiquetaCantidad = new() { Text = "Cantidad:", X = 1, Y = 6 };
        TextField campoCantidad = new() { X = 11, Y = 6, Width = 12 };

        Label etiquetaError = new() { Text = "", X = 1, Y = 8, Width = Dim.Fill(2) };

        Add(etiquetaTipo, selectorTipo, etiquetaCantidad, campoCantidad, etiquetaError);

        Button btnCancelar = new() { Title = "Cancelar" };
        btnCancelar.Accepting += (_, _) => Result = null;
        AddButton(btnCancelar);

        Button btnRegistrar = new() { Title = "Registrar" };
        btnRegistrar.Accepting += (_, e) => {
            if (!int.TryParse(campoCantidad.Text, out int cantidad) || cantidad <= 0) {
                etiquetaError.Text = "La cantidad debe ser un numero positivo.";
                e.Handled = true;
                campoCantidad.SetFocus();
                return;
            }
            string tipo = OpcionesTipo[selectorTipo.SelectedItem];
            Result = new MovimientoDatos(tipo, cantidad);
        };
        AddButton(btnRegistrar);
    }
}