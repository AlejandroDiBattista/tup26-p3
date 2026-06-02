#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;

using var http = new HttpClient();

var productos = await CargarProductos();

using IApplication app = Application.Create().Init();

var ventana = new Window()
{
    Title = " Catalogo REST "
};

var listaProductos = new ListView()
{
    X = 0,
    Y = 0,
    Width = 40,
    Height = 20
};

var detalle = new Label()
{
    X = 42,
    Y = 1,
    Width = 50,
    Height = 8
};

var movimientosLabel = new Label()
{
    X = 42,
    Y = 10,
    Width = 60,
    Height = 15
};

CargarLista();
MostrarDetalle();

async Task MostrarMovimientos()
{
    if (productos.Count == 0)
    {
        movimientosLabel.Text = "";
        return;
    }

    int indice = listaProductos.SelectedItem ?? 0;

    var prod = productos[indice];

    var movimientos =
        await http.GetFromJsonAsync<List<MovimientoDto>>(
            $"http://localhost:5050/productos/{prod.Id}/movimientos"
        );

    movimientos ??= new();

    if (movimientos.Count == 0)
    {
        movimientosLabel.Text =
            """
            MOVIMIENTOS

            Sin movimientos
            """;

        return;
    }

    var texto =
        """
        MOVIMIENTOS

        """;

    foreach (var mov in movimientos)
    {
        texto +=
            $"{mov.Tipo} | " +
            $"{mov.Cantidad} | " +
            $"{mov.Fecha:g}\n";
    }

    movimientosLabel.Text = texto;
}

await MostrarMovimientos();

listaProductos.Accepting += async (_, _) => {
    MostrarDetalle();
    await MostrarMovimientos();
};

ventana.Add(listaProductos);
ventana.Add(movimientosLabel);

app.Run(ventana);


async Task<List<ProductoDto>> CargarProductos()
{
    var datos = await http.GetFromJsonAsync<List<ProductoDto>>(
        "http://localhost:5050/productos"
    );

    return datos ?? new();
}

void CargarLista()
{
    var items = new ObservableCollection<string>();

    foreach (var p in productos)
    {
        items.Add($"{p.Codigo} | {p.Nombre} | ${p.Precio}");
    }

    listaProductos.SetSource<string>(items);
}

void MostrarDetalle()
{
    if (productos.Count == 0)
    {
        detalle.Text = "Sin productos";
        return;
    }

    int indice = listaProductos.SelectedItem ?? 0;

    var prod = productos[indice];

    detalle.Text =
        $"""
        Id: {prod.Id}

        Codigo: {prod.Codigo}

        Nombre: {prod.Nombre}

        Precio: ${prod.Precio}

        Stock: {prod.Stock}
        """;
}


record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);

record MovimientoDto(
    int Id,
    int ProductoId,
    string Tipo,
    int Cantidad,
    DateTime Fecha
);