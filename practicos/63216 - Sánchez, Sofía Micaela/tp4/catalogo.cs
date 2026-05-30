#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────
const string ApiUrl = "http://localhost:5051";

using HttpClient http = new() { BaseAddress = new Uri(ApiUrl) };

List<ProductoDto> productos;

ProductoDto producto;
try {
    using var http = new HttpClient();
    producto = await CargarProductoAsync(http);
} catch (HttpRequestException ex) {
try
{
    productos = await CargarProductos(http);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    Console.Error.WriteLine($"Verifica que servidor.cs este corriendo en {ApiUrl}");
    return;
}

List<ProductoDto> filtrados = new(productos);

// ── Interfaz TUI ──────────────────────────────────────────────────────────
Label etiquetaBuscar = null!;
TextField campoBuscar = null!;
ListView listaProductos = null!;
Label detalle = null!;

using IApplication app = Application.Create().Init();
Menu.DefaultBorderStyle = LineStyle.Rounded;

Runnable raiz = new() { };

MenuBar menu = new(new MenuBarItem[]
{
    new("Producto", new MenuItem[]
    {
        new("_Agregar",  "Ctrl+A Agregar",   () => AgregarProducto(),  Key.A.WithCtrl),
        new("_Editar",   "Ctrl+E Editar",    () => EditarProducto(),   Key.E.WithCtrl),
        new("_Eliminar", "Ctrl+D Eliminar",  () => EliminarProducto(), Key.D.WithCtrl),
        null!,
        new("_Salir", "Ctrl+Q Salir", () => app.RequestStop(), Key.Q.WithCtrl),
    }),
    new("Movimiento", new MenuItem[]
    {
        new("_Registrar movimiento", "Ctrl+M Movimiento", () => RegistrarMovimiento(), Key.M.WithCtrl),
    }),
});


Window ventana = new()
{
    Title = " Catalogo REST - Productos ",
    X = 0, Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

etiquetaBuscar = new() { Text = "Buscar:", X = 1, Y = 1 };
campoBuscar = new()
{
    X = Pos.Right(etiquetaBuscar) + 1,
    Y = 1,
    Width = 30
};

FrameView panelProductos = new()
{
    Title = "Productos",
    X = 0, Y = 3,
    Width = Dim.Percent(55),
    Height = Dim.Fill()
};

FrameView panelDetalle = new()
{
    Title = "Detalle / Movimientos",
    X = Pos.Right(panelProductos),
    Y = 3,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

listaProductos = new()
{
    X = 1, Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(2)
};

detalle = new()
{
    Text = "Seleccione un producto.",
    X = 1, Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(2)
};

panelProductos.Add(listaProductos);
panelDetalle.Add(detalle);
ventana.Add(etiquetaBuscar, campoBuscar, panelProductos, panelDetalle);
raiz.Add(menu, ventana);

campoBuscar.TextChanged += (_, _) => ActualizarLista();

listaProductos.ValueChanged += async (_, _) =>
{
    await MostrarDetalle();
};

ActualizarLista();
await MostrarDetalle();

            - Id     : {producto.Id}
            - Código : {producto.Codigo}
            - Nombre : {producto.Nombre}
            - Precio : ${producto.Precio,10:N2}
            - Stock  :  {producto.Stock,10}
            """,
    X = 4, Y = 2,
raiz.KeyDown += (sender, e) =>
{
    if (e.KeyCode == Key.A.WithCtrl)
    {
        AgregarProducto();
        e.Handled = true;
    }
    else if (e.KeyCode == Key.E.WithCtrl)
    {
        EditarProducto();
        e.Handled = true;
    }
    else if (e.KeyCode == Key.D.WithCtrl)
    {
        EliminarProducto();
        e.Handled = true;
    }
    else if (e.KeyCode == Key.M.WithCtrl)
    {
        RegistrarMovimiento();
        e.Handled = true;
    }
    else if (e.KeyCode == Key.Q.WithCtrl)
    {
        app.RequestStop();
        e.Handled = true;
    }
};

app.Run(raiz);


void ActualizarLista()
{
    string texto = campoBuscar.Text?.Trim() ?? "";
    filtrados = string.IsNullOrWhiteSpace(texto)
        ? new(productos)
        : productos.Where(p =>
            p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
            p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();

    listaProductos.SetSource(new ObservableCollection<string>(
        filtrados.Select(FormatearProducto).ToList()));
}

async Task MostrarDetalle()
{
    int indice = listaProductos.SelectedItem ?? 0;
    if (indice < 0 || indice >= filtrados.Count)
    {
        detalle.Text = "Seleccione un producto.";
        return;
    }

    ProductoDto producto = filtrados[indice];
    List<MovimientoDto> movimientos = await CargarMovimientos(http, producto.Id);

    string textoMovimientos = movimientos.Count == 0
        ? "Sin movimientos registrados."
        : string.Join(Environment.NewLine, movimientos.Select(FormatearMovimiento));
    detalle.Text = $"""
        PRODUCTO

        Id     : {producto.Id}
        Codigo : {producto.Codigo}
        Nombre : {producto.Nombre}
        Precio : ${producto.Precio:N2}
        Stock  : {producto.Stock}

        MOVIMIENTOS

        {textoMovimientos}
        """;
}

async Task RecargarYActualizar(int? idSeleccionado = null)
{
    productos = await CargarProductos(http);
    ActualizarLista();

    if (idSeleccionado.HasValue)
    {
        int idx = filtrados.FindIndex(p => p.Id == idSeleccionado.Value);
        if (idx >= 0) listaProductos.SelectedItem = idx;
    }

    await MostrarDetalle();
}
void AgregarProducto()
{
    using DialogoProducto dialogo = new("Agregar producto", null);
    app.Run(dialogo);

    if (dialogo.Result is null) return;

    ProductoDatos datos = dialogo.Result;

    _ = Task.Run(async () =>
    {
        try
        {
            HttpResponseMessage resp = await http.PostAsJsonAsync("/productos", datos);
            if (resp.IsSuccessStatusCode)
            {
                ProductoDto? nuevo = await resp.Content.ReadFromJsonAsync<ProductoDto>();
                await RecargarYActualizar(nuevo?.Id);
            }
            else
            {
                string error = await resp.Content.ReadAsStringAsync();
                MessageBox.ErrorQuery(app, "Error", error, "OK");
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(app, "Error", ex.Message, "OK");
        }
    });
}

void EditarProducto()
{
    int indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count)
    {
        MessageBox.Query(app, "Editar", "Seleccione un producto primero.", "OK");
        return;
    }

    ProductoDto actual = filtrados[indice];
    using DialogoProducto dialogo = new("Editar producto", actual);
    app.Run(dialogo);

    if (dialogo.Result is null) return;

    ProductoDatos datos = dialogo.Result;

    _ = Task.Run(async () =>
    {
        try
        {
            HttpResponseMessage resp = await http.PutAsJsonAsync($"/productos/{actual.Id}", datos);
            if (resp.IsSuccessStatusCode)
            {
                await RecargarYActualizar(actual.Id);
            }
            else
            {
                string error = await resp.Content.ReadAsStringAsync();
                MessageBox.ErrorQuery(app, "Error", error, "OK");
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(app, "Error", ex.Message, "OK");
        }
    });
}

// ── DTO ───────────────────────────────────────────────────────────────────
void EliminarProducto()
{
    int indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count)
    {
        MessageBox.Query(app, "Eliminar", "Seleccione un producto primero.", "OK");
        return;
    }

    ProductoDto actual = filtrados[indice];
    int respuesta = MessageBox.Query(app, "Eliminar",
        $"¿Eliminar '{actual.Nombre}'?", "No", "Sí") ?? 0;

    if (respuesta != 1) return;

    _ = Task.Run(async () =>
    {
        try
        {
            HttpResponseMessage resp = await http.DeleteAsync($"/productos/{actual.Id}");
            if (resp.IsSuccessStatusCode)
            {
                await RecargarYActualizar();
            }
            else
            {
                MessageBox.ErrorQuery(app, "Error", "No se pudo eliminar.", "OK");
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(app, "Error", ex.Message, "OK");
        }
    });
}
void RegistrarMovimiento()
{
    int indice = listaProductos.SelectedItem ?? -1;
    if (indice < 0 || indice >= filtrados.Count)
    {
        MessageBox.Query(app, "Movimiento", "Seleccione un producto primero.", "OK");
        return;
    }

    ProductoDto actual = filtrados[indice];
    using DialogoMovimiento dialogo = new(actual.Nombre);
    app.Run(dialogo);

    if (dialogo.Result is null) return;

    MovimientoDatos datos = dialogo.Result;

    _ = Task.Run(async () =>
    {
        try
        {
            HttpResponseMessage resp = await http.PostAsJsonAsync(
                $"/productos/{actual.Id}/movimientos", datos);

            if (resp.IsSuccessStatusCode)
            {
                await RecargarYActualizar(actual.Id);
            }
            else
            {
                string error = await resp.Content.ReadAsStringAsync();
                MessageBox.ErrorQuery(app, "Error", error, "OK");
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(app, "Error", ex.Message, "OK");
        }
    });
}

static async Task<List<ProductoDto>> CargarProductos(HttpClient http)
    => await http.GetFromJsonAsync<List<ProductoDto>>("/productos") ?? [];
static async Task<List<MovimientoDto>> CargarMovimientos(HttpClient http, int productoId)
    => await http.GetFromJsonAsync<List<MovimientoDto>>(
        $"/productos/{productoId}/movimientos") ?? [];
static string FormatearProducto(ProductoDto p)
    => $"{p.Codigo,-6} {p.Nombre,-25} ${p.Precio,8:N2} Stock: {p.Stock,4}";
static string FormatearMovimiento(MovimientoDto m)
    => $"{m.Fecha:dd/MM/yyyy HH:mm}  {m.Tipo,-7}  Cantidad: {m.Cantidad}";
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
record ProductoDatos(string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDatos(string Tipo, int Cantidad);
class DialogoProducto : Dialog<ProductoDatos?>
{
    public DialogoProducto(string titulo, ProductoDto? existente)
    {
        Title = titulo;
        Width = 60;
        Height = 14;

        Label lCodigo = new() { Text = "Codigo :", X = 1, Y = 1 };
        TextField tCodigo = new()
        {
            Text = existente?.Codigo ?? "",
            X = 11, Y = 1, Width = 30
        };

        Label lNombre = new() { Text = "Nombre :", X = 1, Y = 3 };
        TextField tNombre = new()
        {
            Text = existente?.Nombre ?? "",
            X = 11, Y = 3, Width = 30
        };

        Label lPrecio = new() { Text = "Precio :", X = 1, Y = 5 };
        TextField tPrecio = new()
        {
            Text = existente?.Precio.ToString() ?? "0",
            X = 11, Y = 5, Width = 15
        };

        Label lStock = new() { Text = "Stock  :", X = 1, Y = 7 };
        TextField tStock = new()
        {
            Text = existente?.Stock.ToString() ?? "0",
            X = 11, Y = 7, Width = 10
        };

        Label lError = new() { Text = "", X = 1, Y = 9, Width = Dim.Fill(2) };

        Add(lCodigo, tCodigo, lNombre, tNombre, lPrecio, tPrecio, lStock, tStock, lError);

        Button btnCancelar = new() { Title = "Cancelar" };
        btnCancelar.Accepting += (_, _) => Result = null;
        AddButton(btnCancelar);

        Button btnGuardar = new() { Title = "Guardar" };
        btnGuardar.Accepting += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(tCodigo.Text))
            {
                lError.Text = "El codigo es obligatorio.";
                e.Handled = true;
                tCodigo.SetFocus();
                return;
            }
            if (string.IsNullOrWhiteSpace(tNombre.Text))
            {
                lError.Text = "El nombre es obligatorio.";
                e.Handled = true;
                tNombre.SetFocus();
                return;
            }
            if (!decimal.TryParse(tPrecio.Text, out decimal precio) || precio < 0)
            {
                lError.Text = "Precio invalido.";
                e.Handled = true;
                tPrecio.SetFocus();
                return;
            }
            if (!int.TryParse(tStock.Text, out int stock) || stock < 0)
            {
                lError.Text = "Stock invalido.";
                e.Handled = true;
                tStock.SetFocus();
                return;
            }

            Result = new ProductoDatos(tCodigo.Text.Trim(), tNombre.Text.Trim(), precio, stock);
        };
        AddButton(btnGuardar);
    }
}
class DialogoMovimiento : Dialog<MovimientoDatos?>
{
    public DialogoMovimiento(string nombreProducto)
    {
        Title = $"Movimiento: {nombreProducto}";
        Width = 55;
        Height = 12;

        Label lTipo = new() { Text = "Tipo (1=Compra 2=Venta 3=Ajuste):", X = 1, Y = 1 };
        TextField tTipo = new() { X = 1, Y = 2, Width = 10, Text = "1" };

        Label lCantidad = new() { Text = "Cantidad:", X = 1, Y = 4 };
        TextField tCantidad = new() { X = 11, Y = 4, Width = 10 };

        Label lError = new() { Text = "", X = 1, Y = 6, Width = Dim.Fill(2) };

        Add(lTipo, tTipo, lCantidad, tCantidad, lError);

        Button btnCancelar = new() { Title = "Cancelar" };
        btnCancelar.Accepting += (_, _) => Result = null;
        AddButton(btnCancelar);

        Button btnRegistrar = new() { Title = "Registrar" };
        btnRegistrar.Accepting += (_, e) =>
        {
            string tipo = tTipo.Text?.Trim() switch
            {
                "1" => "Compra",
                "2" => "Venta",
                "3" => "Ajuste",
                _ => ""
            };

            if (string.IsNullOrEmpty(tipo))
            {
                lError.Text = "Tipo invalido. Ingrese 1, 2 o 3.";
                e.Handled = true;
                tTipo.SetFocus();
                return;
            }

            if (!int.TryParse(tCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                lError.Text = "La cantidad debe ser un numero positivo.";
                e.Handled = true;
                tCantidad.SetFocus();
                return;
            }

            Result = new MovimientoDatos(tipo, cantidad);
        };
        AddButton(btnRegistrar);
    }
}
