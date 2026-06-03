#:package Terminal.Gui@2.*
#:property PublishAot=false

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

// ── Consulta inicial al servidor ──────────────────────────────────────────

using var http = new HttpClient();

List<ProductoDto> productos;
List<ProductoDto> productosFiltrados = [];

try
{
    productos = await CargarProductosAsync(http);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () 
{ 
    Title = " Catalogo de Productos ", 
};

var txtBuscar = new TextField()
{
    Text = "",
};

var lblBuscar = new Label()
{
    Text = "Buscar:"
};

ventana.Add(lblBuscar);
ventana.Add(txtBuscar);

var frameProductos = new FrameView()
{
    Title = "Productos",
    X = 0,
    Y = 1,
    Width = Dim.Percent(40),
    Height = Dim.Fill()
};

var frameMovimientos = new FrameView()
{
    Title = "Movimientos",
    X = Pos.Right(frameProductos),
    Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var listaProductos = new ListView()
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

listaProductos.SetSource<string>(
    new ObservableCollection<string>(
        productos.Select(
            p => $"{p.Codigo} - {p.Nombre}"
        )
    )
);

var listaMovimientos = new ListView()
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};


frameProductos.Add(listaProductos);
frameMovimientos.Add(listaMovimientos);

ventana.Add(frameProductos);
ventana.Add(frameMovimientos);

void ActualizarListaProductos()
{
    listaProductos.SetSource<string>(
        new ObservableCollection<string>(
            productosFiltrados
                .Select(p => $"{p.Codigo} - {p.Nombre}")
                .ToList()
        )
    );
}

async Task CargarMovimientos()
{
    if (listaProductos.SelectedItem < 0)
        return;

    var producto = productos[listaProductos.SelectedItem ?? 0];

    try
    {
        var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>
        (
            $"http://localhost:5050/productos/{producto.Id}/movimientos"
        );

        listaMovimientos.SetSource<string>(
            new ObservableCollection<string>(
                movimientos?
                .Select(m =>
                    $"{m.Tipo,-8} {m.Cantidad,5} {m.Fecha:g}")
                .ToList()
                ?? []
            )
        );
    }
    catch
    {
        listaMovimientos.SetSource<string>(
            new ObservableCollection<string>
            {
                "Error al cargar movimientos"
            }
        );
    }
}

async Task RecargarProductos()
{
    productos = await CargarProductosAsync(http);

    productosFiltrados = productos;

    ActualizarListaProductos();
}

async Task AgregarProducto()
{
    var codigo = new TextField() { Text = "" };
    var nombre = new TextField() { Text = "" };
    var precio = new TextField() { Text = "" };

    var dialog = new Dialog() {
    Title = "Agregar Producto",
    Width = 60,
    Height = 15
    };

    dialog.Add(
        new Label()
        {
            Text = "Código"
        },

        new Label()
        {
            Text = "Nombre"
        },

        new Label()
        {
            Text = "Precio"
        }
    );

    var guardar = new Button()
    {
        Text = "Guardar"
    };
    var cancelar = new Button()
    {
        Text = "Cancelar"
    };

    guardar.Accepting += async (_, _) =>
    {
        await http.PostAsJsonAsync(
            "http://localhost:5050/productos",
            new
            {
                Codigo = codigo.Text.ToString(),
                Nombre = nombre.Text.ToString(),
                Precio = decimal.Parse(precio.Text.ToString() ?? "0"),
                Stock = 0
            });

        await RecargarProductos();

        Application.RequestStop();
    };

    cancelar.Accepting += (_, _) =>
    {
        Application.RequestStop();
    };

    dialog.AddButton(guardar);
    dialog.AddButton(cancelar);

    Application.Run(dialog);
}

async Task EditarProducto()
{
    if (listaProductos.SelectedItem < 0)
        return;

        var indice = listaProductos.SelectedItem ?? -1;
        if(indice < 0)
            return;

    var producto = productosFiltrados[indice];

    var codigo = new TextField();
    var nombre = new TextField();
    var precio = new TextField();

    var dialog = new Dialog()
    {
        Title = "Editar Producto",
        Width = 60,
        Height = 15
    };

    dialog.Add(
        new Label()
        {
            Text = "Código",
            X = 1,
            Y = 1
        },
        codigo,

        new Label()
        {
            Text = "Nombre",
            X = 1,
            Y = 3
        },
        nombre,

        new Label()
        {
            Text = "Precio",
            X = 1,
            Y = 5
        },
        precio
    );

    var guardar = new Button()
    {
        Text = "Guardar"
    };

    guardar.Accepting += async (_, _) =>
    {
        await http.PutAsJsonAsync(
            $"http://localhost:5050/productos/{producto.Id}",
            new
            {
                producto.Id,
                Codigo = codigo.Text.ToString(),
                Nombre = nombre.Text.ToString(),
                Precio = decimal.Parse(precio.Text.ToString() ?? "0"),
                producto.Stock
            });

        await RecargarProductos();

        Application.RequestStop();
    };

    dialog.AddButton(guardar);

    Application.Run(dialog);
}

async Task EliminarProducto()
{
    if (listaProductos.SelectedItem < 0)
        return;

        var indice = listaProductos.SelectedItem ?? -1;
        if(indice < 0)
            return;
    var producto = productosFiltrados[indice];

    await http.DeleteAsync(
        $"http://localhost:5050/productos/{producto.Id}");

    await RecargarProductos();
}

async Task RegistrarMovimiento(string tipo)
{
    if (listaProductos.SelectedItem < 0)
        return;

    var indice = listaProductos.SelectedItem ?? -1;
    if(indice < 0)
        return; 
    var producto = productosFiltrados[indice];

    var txtCantidad = new TextField()
    {
        Text = ""
    };

    var dialog = new Dialog()
    {
        Title = "Movimiento",
        Width = 60,
        Height = 15
    };

    dialog.Add(
        new Label() {
            Text = "Cantidad"
        },
        txtCantidad
    );

    var aceptar = new Button() {
        Text = "Aceptar"
    };
    var cancelar = new Button() {
        Text = "Cancelar"
    };

    aceptar.Accepting += async (_, _) =>
    {
        await http.PostAsJsonAsync(
            $"http://localhost:5050/productos/{producto.Id}/movimientos",
            new
            {
                Tipo = tipo,
                Cantidad = int.Parse(
                    txtCantidad.Text.ToString() ?? "0")
            });

        await RecargarProductos();
        await CargarMovimientos();

        Application.RequestStop();
    };

    cancelar.Accepting += (_, _) =>
    {
        Application.RequestStop();
    };

    dialog.AddButton(aceptar);
    dialog.AddButton(cancelar);

    Application.Run(dialog);
}

txtBuscar.TextChanged += (sender, e) =>
{
    var texto = txtBuscar.Text.ToString() ?? "";

    productosFiltrados = productos
        .Where(p =>
            p.Codigo.Contains(texto,
                StringComparison.OrdinalIgnoreCase)
            ||
            p.Nombre.Contains(texto,
                StringComparison.OrdinalIgnoreCase))
        .ToList();

    ActualizarListaProductos();
};

listaProductos.SelectedItem = 0;

listaProductos.Accepting += async (sender, e) =>
{
    await CargarMovimientos();
};

ventana.KeyDown += async (sender, e) =>
{
    if (e.KeyCode == Key.F2)
        await AgregarProducto();

    else if (e.KeyCode == Key.F3)
        await EditarProducto();

    else if (e.KeyCode == Key.F4)
        await EliminarProducto();

    else if (e.KeyCode == Key.F5)
        await RegistrarMovimiento("Compra");

    else if (e.KeyCode == Key.F6)
        await RegistrarMovimiento("Venta");

    else if (e.KeyCode == Key.F7)
        await RegistrarMovimiento("Ajuste");
};

if (productos.Count > 0)
{
    listaProductos.SelectedItem = 0;
    await CargarMovimientos();
}

app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
{
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
           ?? [];
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);


