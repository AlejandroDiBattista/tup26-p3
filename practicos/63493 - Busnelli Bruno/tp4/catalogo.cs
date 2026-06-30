#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow());

public sealed class CatalogoWindow : Runnable
{
    private readonly HttpClient http = new();

    private readonly List<ProductoDto> productos = [];
    private readonly List<ProductoDto> productosFiltrados = [];
    private readonly List<MovimientoDto> movimientos = [];

    private TextField buscarField = null!;
    private TextView productosView = null!;
    private TextView detalleView = null!;
    private Label estadoLabel = null!;

    private int selectedIndex;

    public CatalogoWindow()
    {
        Title = "CatalogoREST";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;

        BuildLayout();
        CargarProductos();
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),

                new MenuBarItem("_Productos",
                [
                    new MenuItem("_Agregar", "F2", AgregarProducto),
                    new MenuItem("_Modificar", "F3", ModificarProducto),
                    new MenuItem("_Eliminar", "Del", EliminarProducto)
                ]),

                new MenuBarItem("_Movimientos",
                [
                    new MenuItem("_Registrar movimiento", "F4", RegistrarMovimiento)
                ]),

                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", null!, MostrarAcercaDe)
                ])
            ]
        };

        Add(menu);

        Add(new Label()
        {
            Text = "Buscar:",
            X = 1,
            Y = 1
        });

        buscarField = new TextField()
        {
            X = 10,
            Y = 1,
            Width = Dim.Fill(1)
        };

        buscarField.TextChanged += (_, _) =>
        {
            selectedIndex = 0;
            AplicarFiltro("Busqueda actualizada.");
            CargarMovimientosSeleccionado();
        };

        Add(buscarField);

        FrameView panelProductos = new()
        {
            Title = "Productos",
            X = 0,
            Y = 3,
            Width = Dim.Percent(50),
            Height = Dim.Fill(1)
        };

        productosView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        panelProductos.Add(productosView);

        FrameView panelDetalle = new()
        {
            Title = "Detalle e historial",
            X = Pos.Right(panelProductos),
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        detalleView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        panelDetalle.Add(detalleView);

        estadoLabel = new Label()
        {
            Text = "Listo.",
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill()
        };

        Add(panelProductos, panelDetalle, estadoLabel);
    }

    private void CargarProductos()
    {
        try
        {
            productos.Clear();
            productos.AddRange(CatalogoApi.ListarProductosAsync(http).GetAwaiter().GetResult());

            selectedIndex = 0;
            AplicarFiltro("Productos cargados correctamente.");
            CargarMovimientosSeleccionado();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK");
        }
    }

    private void CargarMovimientosSeleccionado()
    {
        movimientos.Clear();

        ProductoDto? producto = ProductoSeleccionado();

        if (producto is null)
        {
            RefrescarVista("No hay producto seleccionado.");
            return;
        }

        try
        {
            movimientos.AddRange(
                CatalogoApi.ListarMovimientosAsync(http, producto.Id)
                    .GetAwaiter()
                    .GetResult()
            );

            RefrescarVista($"Producto seleccionado: {producto.Nombre}");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK");
        }
    }

    private void AplicarFiltro(string estado)
    {
        string busqueda = buscarField?.Text.ToString() ?? "";

        IEnumerable<ProductoDto> consulta = productos;

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            consulta = consulta.Where(p =>
                p.Codigo.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
            );
        }

        productosFiltrados.Clear();
        productosFiltrados.AddRange(consulta.OrderBy(p => p.Codigo));

        if (selectedIndex >= productosFiltrados.Count)
            selectedIndex = Math.Max(0, productosFiltrados.Count - 1);

        RefrescarVista(estado);
    }

    private void RefrescarVista(string estado)
    {
        productosView.Text = ArmarTextoProductos();
        detalleView.Text = ArmarTextoDetalle();
        estadoLabel.Text = estado;
    }

    private string ArmarTextoProductos()
    {
        if (productosFiltrados.Count == 0)
            return "No hay productos para mostrar.";

        StringBuilder sb = new();

        for (int i = 0; i < productosFiltrados.Count; i++)
        {
            ProductoDto p = productosFiltrados[i];
            string cursor = i == selectedIndex ? "> " : "  ";

            sb.AppendLine($"{cursor}{p.Codigo} | {p.Nombre} | ${p.Precio:N2} | Stock: {p.Stock}");
        }

        sb.AppendLine();
        sb.AppendLine("F2: agregar | F3: modificar | Del: eliminar | F4: movimiento");

        return sb.ToString();
    }

    private string ArmarTextoDetalle()
    {
        ProductoDto? producto = ProductoSeleccionado();

        if (producto is null)
            return "No hay producto seleccionado.";

        StringBuilder sb = new();

        sb.AppendLine($"Id: {producto.Id}");
        sb.AppendLine($"Codigo: {producto.Codigo}");
        sb.AppendLine($"Nombre: {producto.Nombre}");
        sb.AppendLine($"Precio: ${producto.Precio:N2}");
        sb.AppendLine($"Stock actual: {producto.Stock}");
        sb.AppendLine();
        sb.AppendLine("Historial de movimientos:");
        sb.AppendLine();

        if (movimientos.Count == 0)
        {
            sb.AppendLine("No hay movimientos registrados.");
        }
        else
        {
            foreach (MovimientoDto m in movimientos)
            {
                sb.AppendLine($"{m.Fecha:g} | {m.Tipo} | Cantidad: {m.Cantidad}");
            }
        }

        return sb.ToString();
    }

    private ProductoDto? ProductoSeleccionado()
    {
        if (productosFiltrados.Count == 0)
            return null;

        return productosFiltrados[selectedIndex];
    }

    private void AgregarProducto()
    {
        ProductoDialog dialog = new();
        App!.Run(dialog);

        if (dialog.Producto is null)
        {
            AplicarFiltro("Alta cancelada.");
            return;
        }

        try
        {
            CatalogoApi.CrearProductoAsync(http, dialog.Producto).GetAwaiter().GetResult();
            CargarProductos();
            AplicarFiltro("Producto agregado correctamente.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK");
        }
    }

    private void ModificarProducto()
    {
        ProductoDto? seleccionado = ProductoSeleccionado();

        if (seleccionado is null)
        {
            MessageBox.Query(App!, "Modificar", "No hay producto seleccionado.", "OK");
            return;
        }

        ProductoDialog dialog = new(new ProductoInputDto(
            seleccionado.Codigo,
            seleccionado.Nombre,
            seleccionado.Precio,
            seleccionado.Stock
        ));

        App!.Run(dialog);

        if (dialog.Producto is null)
        {
            AplicarFiltro("Modificacion cancelada.");
            return;
        }

        try
        {
            CatalogoApi.ModificarProductoAsync(http, seleccionado.Id, dialog.Producto).GetAwaiter().GetResult();
            CargarProductos();
            AplicarFiltro("Producto modificado correctamente.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK");
        }
    }

    private void EliminarProducto()
    {
        ProductoDto? seleccionado = ProductoSeleccionado();

        if (seleccionado is null)
        {
            MessageBox.Query(App!, "Eliminar", "No hay producto seleccionado.", "OK");
            return;
        }

        int respuesta = MessageBox.Query(
            App!,
            "Eliminar producto",
            $"Desea eliminar '{seleccionado.Nombre}'?",
            "Si",
            "No"
        ) ?? 1;

        if (respuesta != 0)
        {
            AplicarFiltro("Eliminacion cancelada.");
            return;
        }

        try
        {
            CatalogoApi.EliminarProductoAsync(http, seleccionado.Id).GetAwaiter().GetResult();
            CargarProductos();
            AplicarFiltro("Producto eliminado correctamente.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK");
        }
    }

    private void RegistrarMovimiento()
    {
        ProductoDto? seleccionado = ProductoSeleccionado();

        if (seleccionado is null)
        {
            MessageBox.Query(App!, "Movimiento", "No hay producto seleccionado.", "OK");
            return;
        }

        MovimientoDialog dialog = new();
        App!.Run(dialog);

        if (dialog.Movimiento is null)
        {
            AplicarFiltro("Movimiento cancelado.");
            return;
        }

        try
        {
            CatalogoApi.RegistrarMovimientoAsync(http, seleccionado.Id, dialog.Movimiento)
                .GetAwaiter()
                .GetResult();

            CargarProductos();
            CargarMovimientosSeleccionado();
            AplicarFiltro("Movimiento registrado correctamente.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK");
        }
    }
    private void MoverSeleccion(int direccion)
{
    if (productosFiltrados.Count == 0)
        return;

    selectedIndex += direccion;

    if (selectedIndex < 0)
        selectedIndex = 0;

    if (selectedIndex >= productosFiltrados.Count)
        selectedIndex = productosFiltrados.Count - 1;

    CargarMovimientosSeleccionado();

    ProductoDto? producto = ProductoSeleccionado();

    if (producto is not null)
        RefrescarVista($"Seleccionado: {producto.Nombre}");
}
    private void MostrarAcercaDe()
    {
        MessageBox.Query(
            App!,
            "Acerca de",
            "CatalogoREST - Trabajo Practico 4\nTUI + API REST + SQLite + EF Core",
            "OK"
        );
    }

    private void SolicitarSalir()
    {
        App!.RequestStop();
    }

 protected override bool OnKeyDown(Key key)
{
    if (key == Key.Q.WithCtrl)
    {
        SolicitarSalir();
        return true;
    }

    if (key == Key.F2)
    {
        AgregarProducto();
        return true;
    }

    if (key == Key.F3 || key == Key.Enter)
    {
        ModificarProducto();
        return true;
    }

    if (key == Key.Delete)
    {
        EliminarProducto();
        return true;
    }

    if (key == Key.F4)
    {
        RegistrarMovimiento();
        return true;
    }

    if (key == Key.CursorUp)
    {
        MoverSeleccion(-1);
        return true;
    }

    if (key == Key.CursorDown)
    {
        MoverSeleccion(1);
        return true;
    }

    return base.OnKeyDown(key);
}
}

public sealed class ProductoDialog : Dialog
{
    public ProductoInputDto? Producto { get; private set; }

    private readonly TextField codigoField;
    private readonly TextField nombreField;
    private readonly TextField precioField;
    private readonly TextField stockField;

    public ProductoDialog(ProductoInputDto? producto = null)
    {
        Title = producto is null ? "Agregar producto" : "Modificar producto";
        Width = 60;
        Height = 14;

        Add(new Label() { Text = "Codigo:", X = 1, Y = 1 });

        codigoField = new TextField()
        {
            X = 15,
            Y = 1,
            Width = 35,
            Text = producto?.Codigo ?? ""
        };

        Add(codigoField);

        Add(new Label() { Text = "Nombre:", X = 1, Y = 3 });

        nombreField = new TextField()
        {
            X = 15,
            Y = 3,
            Width = 35,
            Text = producto?.Nombre ?? ""
        };

        Add(nombreField);

        Add(new Label() { Text = "Precio:", X = 1, Y = 5 });

        precioField = new TextField()
        {
            X = 15,
            Y = 5,
            Width = 35,
            Text = producto?.Precio.ToString() ?? "0"
        };

        Add(precioField);

        Add(new Label() { Text = "Stock:", X = 1, Y = 7 });

        stockField = new TextField()
        {
            X = 15,
            Y = 7,
            Width = 35,
            Text = producto?.Stock.ToString() ?? "0"
        };

        Add(stockField);

        Button guardar = new() { Text = "_Guardar", IsDefault = true };

        guardar.Accepting += (_, e) =>
        {
            Guardar();
            e.Handled = true;
        };

        Button cancelar = new() { Text = "_Cancelar" };

        cancelar.Accepting += (_, e) =>
        {
            App!.RequestStop();
            e.Handled = true;
        };

        AddButton(guardar);
        AddButton(cancelar);
    }

    private void Guardar()
    {
        string codigo = codigoField.Text.ToString() ?? "";
        string nombre = nombreField.Text.ToString() ?? "";
        string precioTexto = precioField.Text.ToString() ?? "";
        string stockTexto = stockField.Text.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(codigo))
        {
            MessageBox.ErrorQuery(App!, "Error", "El codigo es obligatorio.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.ErrorQuery(App!, "Error", "El nombre es obligatorio.", "OK");
            return;
        }

        if (!decimal.TryParse(precioTexto, out decimal precio))
        {
            MessageBox.ErrorQuery(App!, "Error", "El precio debe ser numerico.", "OK");
            return;
        }

        if (!int.TryParse(stockTexto, out int stock))
        {
            MessageBox.ErrorQuery(App!, "Error", "El stock debe ser numerico.", "OK");
            return;
        }

        Producto = new ProductoInputDto(codigo.Trim(), nombre.Trim(), precio, stock);
        App!.RequestStop();
    }
}

public sealed class MovimientoDialog : Dialog
{
    public MovimientoInputDto? Movimiento { get; private set; }

    private readonly TextField tipoField;
    private readonly TextField cantidadField;

    public MovimientoDialog()
    {
        Title = "Registrar movimiento";
        Width = 65;
        Height = 12;

        Add(new Label()
        {
            Text = "Tipo (Compra/Venta/Ajuste):",
            X = 1,
            Y = 1
        });

        tipoField = new TextField()
        {
            X = 30,
            Y = 1,
            Width = 25,
            Text = "Compra"
        };

        Add(tipoField);

        Add(new Label()
        {
            Text = "Cantidad:",
            X = 1,
            Y = 3
        });

        cantidadField = new TextField()
        {
            X = 30,
            Y = 3,
            Width = 25,
            Text = "1"
        };

        Add(cantidadField);

        Add(new Label()
        {
            Text = "Compra suma stock, Venta descuenta, Ajuste establece stock.",
            X = 1,
            Y = 5
        });

        Button guardar = new()
        {
            Text = "_Guardar",
            IsDefault = true
        };

        guardar.Accepting += (_, e) =>
        {
            Guardar();
            e.Handled = true;
        };

        Button cancelar = new()
        {
            Text = "_Cancelar"
        };

        cancelar.Accepting += (_, e) =>
        {
            App!.RequestStop();
            e.Handled = true;
        };

        AddButton(guardar);
        AddButton(cancelar);
    }

    private void Guardar()
    {
        string tipoTexto = tipoField.Text.ToString() ?? "";
        string cantidadTexto = cantidadField.Text.ToString() ?? "";

        if (!Enum.TryParse(tipoTexto, true, out TipoMovimientoDto tipo))
        {
            MessageBox.ErrorQuery(
                App!,
                "Error",
                "El tipo debe ser Compra, Venta o Ajuste.",
                "OK"
            );

            return;
        }

        if (!int.TryParse(cantidadTexto, out int cantidad) || cantidad <= 0)
        {
            MessageBox.ErrorQuery(
                App!,
                "Error",
                "La cantidad debe ser un numero positivo.",
                "OK"
            );

            return;
        }

        Movimiento = new MovimientoInputDto(tipo, cantidad);
        App!.RequestStop();
    }
}

static class CatalogoApi
{
    private const string BaseUrl = "http://localhost:5050";

    public static async Task<List<ProductoDto>> ListarProductosAsync(HttpClient http)
    {
        return await http.GetFromJsonAsync<List<ProductoDto>>($"{BaseUrl}/productos") ?? [];
    }

    public static async Task<ProductoDto?> BuscarProductoAsync(HttpClient http, int id)
    {
        return await http.GetFromJsonAsync<ProductoDto>($"{BaseUrl}/productos/{id}");
    }

    public static async Task<ProductoDto?> CrearProductoAsync(HttpClient http, ProductoInputDto input)
    {
        HttpResponseMessage response = await http.PostAsJsonAsync($"{BaseUrl}/productos", input);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<ProductoDto>();
    }

    public static async Task<ProductoDto?> ModificarProductoAsync(HttpClient http, int id, ProductoInputDto input)
    {
        HttpResponseMessage response = await http.PutAsJsonAsync($"{BaseUrl}/productos/{id}", input);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<ProductoDto>();
    }

    public static async Task EliminarProductoAsync(HttpClient http, int id)
    {
        HttpResponseMessage response = await http.DeleteAsync($"{BaseUrl}/productos/{id}");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }

    public static async Task<List<MovimientoDto>> ListarMovimientosAsync(HttpClient http, int productoId)
    {
        return await http.GetFromJsonAsync<List<MovimientoDto>>($"{BaseUrl}/productos/{productoId}/movimientos") ?? [];
    }

    public static async Task RegistrarMovimientoAsync(HttpClient http, int productoId, MovimientoInputDto input)
    {
        HttpResponseMessage response =
            await http.PostAsJsonAsync($"{BaseUrl}/productos/{productoId}/movimientos", input);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }
}

public record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);

public record ProductoInputDto(
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);

public record MovimientoDto(
    int Id,
    int ProductoId,
    TipoMovimientoDto Tipo,
    int Cantidad,
    DateTime Fecha
);

public record MovimientoInputDto(
    TipoMovimientoDto Tipo,
    int Cantidad
);

public enum TipoMovimientoDto
{
    Compra,
    Venta,
    Ajuste
}