#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using System.Collections.ObjectModel;

Application.Init();
Application.Run(new CatalogoWindow());
Application.Shutdown();

public enum TipoMovimiento
{
Compra = 1,
Venta = 2,
Ajuste = 3
}

public class Producto
{
public int Id { get; set; }

public string Codigo { get; set; } = "";

public string Nombre { get; set; } = "";

public decimal Precio { get; set; }

public int Stock { get; set; }

public override string ToString()
{
    return $"{Codigo,-10} {Nombre,-25} ${Precio,-10} Stock:{Stock}";
}

}

public class MovimientoDeProducto
{
public int Id { get; set; }

public int ProductoId { get; set; }

public TipoMovimiento Tipo { get; set; }

public int Cantidad { get; set; }

public DateTime Fecha { get; set; }

public override string ToString()
{
    return $"{Fecha:dd/MM HH:mm} {Tipo,-8} {Cantidad}";
}

}

public class MovimientoInput
{
public TipoMovimiento Tipo { get; set; }

public int Cantidad { get; set; }

}

public class CatalogoApi
{
private const string BaseUrl = "http://localhost:5000";
private readonly HttpClient client = new()
{
BaseAddress = new Uri(BaseUrl)
};

public async Task<List<Producto>> ObtenerProductos()
{
    return await client.GetFromJsonAsync<List<Producto>>("/productos")
        ?? [];
}

public async Task<List<MovimientoDeProducto>> ObtenerMovimientos(int productoId)
{
    return await client.GetFromJsonAsync<List<MovimientoDeProducto>>
        ($"/productos/{productoId}/movimientos")
        ?? [];
}

public async Task CrearProducto(Producto producto)
{
    string json =
        JsonSerializer.Serialize(producto);

    StringContent contenido =
        new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

    await client.PostAsync(
        "/productos",
        contenido);
}

public async Task ModificarProducto(Producto producto)
{
    string json =
        JsonSerializer.Serialize(producto);

    StringContent contenido =
        new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

    await client.PutAsync(
        $"/productos/{producto.Id}",
        contenido);
}

public async Task EliminarProducto(int id)
{
    await client.DeleteAsync($"/productos/{id}");
}

public async Task RegistrarMovimiento(
    int productoId,
    TipoMovimiento tipo,
    int cantidad)
{
    MovimientoInput input = new()
    {
        Tipo = tipo,
        Cantidad = cantidad
    };

    string json =
        JsonSerializer.Serialize(input);

    StringContent contenido =
        new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

    await client.PostAsync(
        $"/productos/{productoId}/movimientos",
        contenido);
}

}


public class CatalogoWindow : Window
{
private readonly CatalogoApi api = new();

private readonly List<Producto> productos = [];

private readonly ListView listaProductos;
private readonly ListView listaMovimientos;

private readonly TextField txtBuscar;

private List<Producto> productosFiltrados = [];

public CatalogoWindow()
{
    Title = "Catalogo REST";

    Width = Dim.Fill();
    Height = Dim.Fill();

    FrameView maestro = new()
    {
        Title = "Productos",
        X = 0,
        Y = 1,
        Width = Dim.Percent(50),
        Height = Dim.Fill(2)
    };

    FrameView detalle = new()
    {
        Title = "Movimientos",
        X = Pos.Right(maestro),
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill(2)
    };

    txtBuscar = new TextField()
    {
        X = 1,
        Y = 0,
        Width = Dim.Fill(2)
    };

    txtBuscar.TextChanged += (_, _) =>
    {
        Filtrar();
    };

    listaProductos = new ListView()
    {
        X = 0,
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };

    listaMovimientos = new ListView()
    {
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };

    listaProductos.Accepting += async (_, _) =>
    {
        await CargarMovimientos();
    };

    maestro.Add(txtBuscar);
    maestro.Add(listaProductos);

    detalle.Add(listaMovimientos);

    Add(maestro);
    Add(detalle);


    MenuBar menu = CrearMenu();
    Add(menu);

    Task.Run(async () =>
    {
        await RefrescarProductos();
    });

}

private MenuBar CrearMenu()
{
    return new MenuBar(
    [
        new MenuBarItem(
            "_Productos",
            new MenuItem[]
            {
                new MenuItem(
                    "_Agregar",
                    "",
                    async () => await AgregarProducto()),

                new MenuItem(
                    "_Editar",
                    "",
                    async () => await EditarProducto()),

                new MenuItem(
                    "_Eliminar",
                    "",
                    async () => await EliminarProducto())
            }
        ),

        new MenuBarItem(
            "_Movimientos",
            new MenuItem[]
            {
                new MenuItem(
                    "_Compra",
                    "",
                    async () => await RegistrarMovimiento(
                        TipoMovimiento.Compra)),

                new MenuItem(
                    "_Venta",
                    "",
                    async () => await RegistrarMovimiento(
                        TipoMovimiento.Venta)),

                new MenuItem(
                    "_Ajuste",
                    "",
                    async () => await RegistrarMovimiento(
                        TipoMovimiento.Ajuste))
            }
        )
    ]);
}

private async Task RefrescarProductos()
{
    productos.Clear();

    var datos = await api.ObtenerProductos();

    Console.WriteLine($"Productos recibidos: {datos.Count}");

    productos.AddRange(datos);

    Filtrar();
}

private void Filtrar()
{
    string texto = txtBuscar.Text?.ToString() ?? "";

    productosFiltrados = productos
        .Where(p =>
            p.Nombre.Contains(texto,
                StringComparison.OrdinalIgnoreCase)
            ||
            p.Codigo.Contains(texto,
                StringComparison.OrdinalIgnoreCase))
        .ToList();

    listaProductos.SetSource(
    new ObservableCollection<Producto>(productosFiltrados)
    );
}

private Producto? ProductoActual()
{
    if (listaProductos.SelectedItem < 0)
        return null;

    if (listaProductos.SelectedItem >= productosFiltrados.Count)
        return null;

    if (listaProductos.SelectedItem is null)
    return null;

    return productosFiltrados[listaProductos.SelectedItem.Value];
}

private async Task CargarMovimientos()
{
    var producto = ProductoActual();

    if (producto is null)
        return;

    var movimientos =
        await api.ObtenerMovimientos(producto.Id);

    listaMovimientos.SetSource(
    new ObservableCollection<MovimientoDeProducto>(movimientos)
    );
}

private async Task AgregarProducto()
{
    ProductoDialog dialog = new();

    Application.Run(dialog);

    if (dialog.Resultado is null)
        return;

    await api.CrearProducto(dialog.Resultado);

    await RefrescarProductos();
}

private async Task EditarProducto()
{
    var producto = ProductoActual();

    if (producto is null)
        return;

    ProductoDialog dialog = new(producto);

    Application.Run(dialog);

    if (dialog.Resultado is null)
        return;

    dialog.Resultado.Id = producto.Id;

    await api.ModificarProducto(dialog.Resultado);

    await RefrescarProductos();
}

private async Task EliminarProducto()
{
    var producto = ProductoActual();

    if (producto is null)
        return;

    await api.EliminarProducto(producto.Id);

    await RefrescarProductos();
}

private async Task RegistrarMovimiento(
    TipoMovimiento tipo)
{
    var producto = ProductoActual();

    if (producto is null)
        return;

    MovimientoDialog dialog = new(tipo);

    Application.Run(dialog);

    if (dialog.Cantidad <= 0)
        return;

    await api.RegistrarMovimiento(
        producto.Id,
        tipo,
        dialog.Cantidad);

    await RefrescarProductos();
    await CargarMovimientos();
}

}

public class ProductoDialog : Dialog
{
public Producto? Resultado { get; private set; }

public ProductoDialog(Producto? producto = null)
{
    Title = producto is null
        ? "Agregar Producto"
        : "Editar Producto";

    Width = 60;
    Height = 15;

    TextField txtCodigo = new()
    {
        X = 15,
        Y = 1,
        Width = 25,
        Text = producto?.Codigo ?? ""
    };

    TextField txtNombre = new()
    {
        X = 15,
        Y = 3,
        Width = 25,
        Text = producto?.Nombre ?? ""
    };

    TextField txtPrecio = new()
    {
        X = 15,
        Y = 5,
        Width = 25,
        Text = producto?.Precio.ToString() ?? "0"
    };

    Add(
        new Label(){ X=1,Y=1,Text="Codigo" },
        txtCodigo,
        new Label(){ X=1,Y=3,Text="Nombre" },
        txtNombre,
        new Label(){ X=1,Y=5,Text="Precio" },
        txtPrecio
    );

    Button aceptar = new()
    {
        Title = "Aceptar"
    };

    aceptar.Accepting += (_, _) =>
    {
        decimal.TryParse(
            txtPrecio.Text?.ToString(),
            out decimal precio);

        Resultado = new Producto
        {
            Codigo = txtCodigo.Text?.ToString() ?? "",
            Nombre = txtNombre.Text?.ToString() ?? "",
            Precio = precio
        };

        RequestStop();
    };

    AddButton(aceptar);
    Button cancelar = new()
    {
        Title = "Cancelar"
    };
    cancelar.Accepting += (_, _) =>
    {
        RequestStop();
    };

    AddButton(cancelar);
}

}


public class MovimientoDialog : Dialog
{
public int Cantidad { get; private set; }

public MovimientoDialog(TipoMovimiento tipo)
{
    Title = $"Movimiento {tipo}";

    Width = 50;
    Height = 10;

    TextField txtCantidad = new()
    {
        X = 15,
        Y = 2,
        Width = 15
    };

    Add(
        new Label()
        {
            X = 1,
            Y = 2,
            Text = "Cantidad"
        },
        txtCantidad
    );

    Button aceptar = new()
    {
        Title = "Aceptar"
    };

    aceptar.Accepting += (_, _) =>
    {
        int.TryParse(
            txtCantidad.Text?.ToString(),
            out int valor);

        Cantidad = valor;

        RequestStop();
    };

    AddButton(aceptar);
    AddButton(new Button(){ Title="Cancelar" });
}

}
