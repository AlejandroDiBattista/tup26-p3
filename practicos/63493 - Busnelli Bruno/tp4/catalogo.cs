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
                    new MenuItem("_Agregar", "F2", MostrarPendiente),
                    new MenuItem("_Modificar", "F3", MostrarPendiente),
                    new MenuItem("_Eliminar", "Del", MostrarPendiente)
                ]),

                new MenuBarItem("_Movimientos",
                [
                    new MenuItem("_Registrar movimiento", "F4", MostrarPendiente)
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
            Title = "Detalle",
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

        return sb.ToString();
    }

    private string ArmarTextoDetalle()
    {
        ProductoDto? producto = ProductoSeleccionado();

        if (producto is null)
            return "No hay producto seleccionado.";

        return $"""
        Id: {producto.Id}
        Codigo: {producto.Codigo}
        Nombre: {producto.Nombre}
        Precio: ${producto.Precio:N2}
        Stock actual: {producto.Stock}

        Historial de movimientos:
        Se agregara en el proximo commit.
        """;
    }

    private ProductoDto? ProductoSeleccionado()
    {
        if (productosFiltrados.Count == 0)
            return null;

        return productosFiltrados[selectedIndex];
    }

    private void MostrarPendiente()
    {
        MessageBox.Query(App!, "Pendiente", "Esta funcion se agregara en los proximos commits.", "OK");
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

        return base.OnKeyDown(key);
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
}

record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);

record ProductoInputDto(
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);