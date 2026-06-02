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
    Height = 20
};

CargarLista();
MostrarDetalle();

listaProductos.Accepting += (_, _) => {
    MostrarDetalle();
};

ventana.Add(listaProductos);
ventana.Add(detalle);

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