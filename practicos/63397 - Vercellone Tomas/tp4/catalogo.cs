#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

Application.Init();
Application.Run(new VentanaCatalogo());
Application.Shutdown();

// ── Enums y modelos ───────────────────────────────────────────────────────

public enum TipoMovimiento { Compra, Venta, Ajuste }

public class ProductoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }

    public override string ToString() =>
        $"{Codigo,-10} {Nombre,-25} ${Precio,-10:N2} Stock:{Stock}";
}

public class MovimientoDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }

    public override string ToString() =>
        $"{Fecha:dd/MM/yyyy HH:mm}  {Tipo,-8}  {(Cantidad >= 0 ? "+" : "")}{Cantidad}";
}

public class MovimientoRequest
{
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
}

// ── Cliente de la API ─────────────────────────────────────────────────────

public class CatalogoApi
{
    private const string BASE_URL = "http://localhost:5050";
    private readonly HttpClient http = new() { BaseAddress = new Uri(BASE_URL) };

    public async Task<List<ProductoDto>> ObtenerProductos()
    {
        return await http.GetFromJsonAsync<List<ProductoDto>>("/productos") ?? [];
    }

    public async Task<List<MovimientoDto>> ObtenerMovimientos(int productoId)
    {
        return await http.GetFromJsonAsync<List<MovimientoDto>>(
            $"/productos/{productoId}/movimientos") ?? [];
    }

    public async Task AgregarProducto(ProductoDto p)
    {
        await http.PostAsJsonAsync("/productos", p);
    }

    public async Task ModificarProducto(ProductoDto p)
    {
        await http.PutAsJsonAsync($"/productos/{p.Id}", p);
    }

    public async Task EliminarProducto(int id)
    {
        await http.DeleteAsync($"/productos/{id}");
    }

    public async Task RegistrarMovimiento(int productoId, TipoMovimiento tipo, int cantidad)
    {
        var req = new MovimientoRequest { Tipo = tipo, Cantidad = cantidad };
        await http.PostAsJsonAsync($"/productos/{productoId}/movimientos", req);
    }
}

// ── Ventana principal ─────────────────────────────────────────────────────

public class VentanaCatalogo : Window
{
    private readonly CatalogoApi api = new();
    private readonly List<ProductoDto> todosLosProductos = [];
    private List<ProductoDto> productosFiltrados = [];

    private readonly ListView listaProductos;
    private readonly ListView listaMovimientos;
    private readonly TextField txtBuscar;

    public VentanaCatalogo()
    {
        Title = "Catálogo REST";
        Width = Dim.Fill();
        Height = Dim.Fill();

        var panelMaestro = new FrameView
        {
            Title = "Productos",
            X = 0, Y = 1,
            Width = Dim.Percent(50),
            Height = Dim.Fill(2)
        };

        var panelDetalle = new FrameView
        {
            Title = "Movimientos",
            X = Pos.Right(panelMaestro), Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };

        txtBuscar = new TextField
        {
            X = 1, Y = 0,
            Width = Dim.Fill(2)
        };
        txtBuscar.TextChanged += (_, _) => Filtrar();

        listaProductos = new ListView
        {
            X = 0, Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        listaProductos.Accepting += async (_, _) => await CargarMovimientos();

        listaMovimientos = new ListView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        panelMaestro.Add(txtBuscar, listaProductos);
        panelDetalle.Add(listaMovimientos);

        Add(panelMaestro, panelDetalle, CrearMenu());

        Task.Run(async () => await RefrescarProductos());
    }

    private MenuBar CrearMenu()
    {
        return new MenuBar([
            new MenuBarItem("_Productos", new MenuItem[]
            {
                new MenuItem("_Agregar",  "", async () => await AgregarProducto()),
                new MenuItem("_Editar",   "", async () => await EditarProducto()),
                new MenuItem("_Eliminar", "", async () => await EliminarProducto()),
            }),
            new MenuBarItem("_Movimientos", new MenuItem[]
            {
                new MenuItem("_Compra", "", async () => await RegistrarMovimiento(TipoMovimiento.Compra)),
                new MenuItem("_Venta",  "", async () => await RegistrarMovimiento(TipoMovimiento.Venta)),
                new MenuItem("_Ajuste", "", async () => await RegistrarMovimiento(TipoMovimiento.Ajuste)),
            }),
        ]);
    }

    private async Task RefrescarProductos()
    {
        todosLosProductos.Clear();
        var datos = await api.ObtenerProductos();
        todosLosProductos.AddRange(datos);
        Filtrar();
    }

    private void Filtrar()
    {
        string texto = txtBuscar.Text?.ToString() ?? "";
        productosFiltrados = todosLosProductos
            .Where(p =>
                p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();
        listaProductos.SetSource(new ObservableCollection<ProductoDto>(productosFiltrados));
    }

    private ProductoDto? ProductoActual()
    {
        int idx = listaProductos.SelectedItem ?? -1;
        if (idx < 0 || idx >= productosFiltrados.Count) return null;
        return productosFiltrados[idx];
    }

    private async Task CargarMovimientos()
    {
        var producto = ProductoActual();
        if (producto is null) return;
        var movimientos = await api.ObtenerMovimientos(producto.Id);
        listaMovimientos.SetSource(new ObservableCollection<MovimientoDto>(movimientos));
    }

    private async Task AgregarProducto()
    {
        var dialog = new DialogProducto();
        Application.Run(dialog);
        if (dialog.Resultado is null) return;
        await api.AgregarProducto(dialog.Resultado);
        await RefrescarProductos();
    }

    private async Task EditarProducto()
    {
        var producto = ProductoActual();
        if (producto is null) return;
        var dialog = new DialogProducto(producto);
        Application.Run(dialog);
        if (dialog.Resultado is null) return;
        dialog.Resultado.Id = producto.Id;
        await api.ModificarProducto(dialog.Resultado);
        await RefrescarProductos();
    }

    private async Task EliminarProducto()
    {
        var producto = ProductoActual();
        if (producto is null) return;
        await api.EliminarProducto(producto.Id);
        await RefrescarProductos();
    }

    private async Task RegistrarMovimiento(TipoMovimiento tipo)
    {
        var producto = ProductoActual();
        if (producto is null) return;
        var dialog = new DialogMovimiento(tipo);
        Application.Run(dialog);
        if (dialog.Cantidad <= 0) return;
        await api.RegistrarMovimiento(producto.Id, tipo, dialog.Cantidad);
        await RefrescarProductos();
        await CargarMovimientos();
    }
}

// ── Diálogo de producto ───────────────────────────────────────────────────

public class DialogProducto : Dialog
{
    public ProductoDto? Resultado { get; private set; }

    public DialogProducto(ProductoDto? producto = null)
    {
        Title = producto is null ? "Agregar Producto" : "Editar Producto";
        Width = 55;
        Height = 16;

        var txtCodigo = new TextField { X = 12, Y = 1, Width = 30, Text = producto?.Codigo ?? "" };
        var txtNombre = new TextField { X = 12, Y = 3, Width = 30, Text = producto?.Nombre ?? "" };
        var txtPrecio = new TextField { X = 12, Y = 5, Width = 20, Text = producto?.Precio.ToString() ?? "0" };
        var txtStock  = new TextField { X = 12, Y = 7, Width = 10, Text = producto?.Stock.ToString() ?? "0" };

        Add(
            new Label { X = 1, Y = 1, Text = "Código:" }, txtCodigo,
            new Label { X = 1, Y = 3, Text = "Nombre:" }, txtNombre,
            new Label { X = 1, Y = 5, Text = "Precio:" }, txtPrecio,
            new Label { X = 1, Y = 7, Text = "Stock:" },  txtStock
        );

        var btnAceptar = new Button { Title = "Aceptar" };
        btnAceptar.Accepting += (_, _) =>
        {
            decimal.TryParse(txtPrecio.Text?.ToString(), out decimal precio);
            int.TryParse(txtStock.Text?.ToString(), out int stock);
            Resultado = new ProductoDto
            {
                Codigo = txtCodigo.Text?.ToString() ?? "",
                Nombre = txtNombre.Text?.ToString() ?? "",
                Precio = precio,
                Stock  = stock
            };
            RequestStop();
        };

        var btnCancelar = new Button { Title = "Cancelar" };
        btnCancelar.Accepting += (_, _) => RequestStop();

        AddButton(btnAceptar);
        AddButton(btnCancelar);
    }
}

// ── Diálogo de movimiento ─────────────────────────────────────────────────

public class DialogMovimiento : Dialog
{
    public int Cantidad { get; private set; }

    public DialogMovimiento(TipoMovimiento tipo)
    {
        Title = $"Registrar {tipo}";
        Width = 45;
        Height = 10;

        string labelTexto = tipo == TipoMovimiento.Ajuste ? "Stock nuevo:" : "Cantidad:";
        var txtCantidad = new TextField { X = 15, Y = 2, Width = 15 };

        Add(
            new Label { X = 1, Y = 2, Text = labelTexto },
            txtCantidad
        );

        var btnAceptar = new Button { Title = "Aceptar" };
        btnAceptar.Accepting += (_, _) =>
        {
            int.TryParse(txtCantidad.Text?.ToString(), out int valor);
            Cantidad = valor;
            RequestStop();
        };

        var btnCancelar = new Button { Title = "Cancelar" };
        btnCancelar.Accepting += (_, _) => RequestStop();

        AddButton(btnAceptar);
        AddButton(btnCancelar);
    }
}