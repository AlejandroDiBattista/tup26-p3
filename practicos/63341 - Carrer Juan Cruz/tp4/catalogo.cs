#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using System.Collections.ObjectModel;

using var http = new HttpClient();

List<ProductoDto> productos = await CargarProductos();

using IApplication app = Application.Create().Init();

var ventana = new Window()
{
    Title = " Catalogo REST "
};

var listaProductos = new ListView()
{
    X = 0,
    Y = 1,
    Width = 40,
    Height = 20
};

var detalle = new Label()
{
    X = 42,
    Y = 1,
    Width = 60,
    Height = 8
};

var movimientosLabel = new Label()
{
    X = 42,
    Y = 10,
    Width = 60,
    Height = 15
};

int indiceSeleccionado = 0;

CargarLista();
MostrarDetalle();

async Task MostrarMovimientos()
{
    if (productos.Count == 0)
    {
        movimientosLabel.Text = "";
        return;
    }

    int indice = indiceSeleccionado;

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

listaProductos.Accepting += async (_, _) =>
{
    indiceSeleccionado = listaProductos.SelectedItem ?? 0;

    MostrarDetalle();
    await MostrarMovimientos();
};

var menu = new MenuBar([
    new MenuBarItem("_Producto", [
        new MenuItem("_Agregar", "", () =>
        {
            _ = AbrirAltaProducto();
        }),
        new MenuItem("_Editar", "", () =>
        {
            _ = AbrirEditarProducto();
        }),
        new MenuItem("_Eliminar", "", () =>
        {
            _ = EliminarProducto();
        }),
        new MenuItem("_Movimiento (Compra/Venta/Ajuste)", "", async () => {
            await AbrirMovimiento();
        })
    ])
]);

ventana.Add(menu);
ventana.Add(listaProductos);
ventana.Add(movimientosLabel);
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

    int indice = indiceSeleccionado;

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

async Task AbrirAltaProducto()
{
    var dialogo = new Dialog()
    {
        Title = "Nuevo producto",
        Width = 60,
        Height = 20
    };

    var codigoTxt = new TextField() { X = 15, Y = 2, Width = 30 };
    var nombreTxt = new TextField() { X = 15, Y = 4, Width = 30 };
    var precioTxt = new TextField() { X = 15, Y = 6, Width = 30 };
    var stockTxt = new TextField() { X = 15, Y = 8, Width = 30 };

    dialogo.Add(new Label() { Text = "Codigo:", X = 2, Y = 2 });
    dialogo.Add(new Label() { Text = "Nombre:", X = 2, Y = 4 });
    dialogo.Add(new Label() { Text = "Precio:", X = 2, Y = 6 });
    dialogo.Add(new Label() { Text = "Stock:", X = 2, Y = 8 });

    dialogo.Add(codigoTxt);
    dialogo.Add(nombreTxt);
    dialogo.Add(precioTxt);
    dialogo.Add(stockTxt);

    var guardarBtn = new Button() { Text = "Guardar", X = 10, Y = 12 };
    var cancelarBtn = new Button() { Text = "Cancelar", X = 25, Y = 12 };

    guardarBtn.Accepting += async (_, _) =>
    {
        var nuevo = new ProductoNuevoDto(
            0,
            codigoTxt.Text?.ToString() ?? "",
            nombreTxt.Text?.ToString() ?? "",
            decimal.Parse(precioTxt.Text?.ToString() ?? "0"),
            int.Parse(stockTxt.Text?.ToString() ?? "0")
        );

        await http.PostAsJsonAsync("http://localhost:5050/productos", nuevo);

        productos = await CargarProductos();
        CargarLista();
        MostrarDetalle();

        dialogo.RequestStop();
    };

    cancelarBtn.Accepting += (_, _) =>
    {
        dialogo.RequestStop();
    };

    app.Run(dialogo);
}

async Task AbrirMovimiento()
{
    if (productos == null || productos.Count == 0)
    return;

    int indice = indiceSeleccionado;
    var prod = productos[indice];

    var dialogo = new Dialog()
    {
        Title = $"Movimiento - {prod.Nombre}",
        Width = 60,
        Height = 18
    };

    var tipoTxt = new TextField() { X = 15, Y = 2, Width = 30 };
    var cantidadTxt = new TextField() { X = 15, Y = 4, Width = 30 };

    dialogo.Add(new Label() { Text = "Tipo (Compra/Venta/Ajuste):", X = 2, Y = 2 });
    dialogo.Add(new Label() { Text = "Cantidad:", X = 2, Y = 4 });

    dialogo.Add(tipoTxt);
    dialogo.Add(cantidadTxt);

    var guardarBtn = new Button() { Text = "Guardar", X = 10, Y = 10 };
    var cancelarBtn = new Button() { Text = "Cancelar", X = 25, Y = 10 };

    guardarBtn.Accepting += async (_, _) =>
    {
        var mov = new MovimientoNuevoDto(
            0,
            prod.Id,
            tipoTxt.Text.ToString() ?? "",
            int.Parse(cantidadTxt.Text.ToString() ?? "0"),
            DateTime.Now
        );

        await http.PostAsJsonAsync(
            $"http://localhost:5050/productos/{prod.Id}/movimientos",
            mov
        );

        productos = await CargarProductos();
        CargarLista();
        MostrarDetalle();
        await MostrarMovimientos();

        dialogo.RequestStop();
    };

    cancelarBtn.Accepting += (_, _) =>
    {
        dialogo.RequestStop();
    };

    dialogo.Add(guardarBtn);
    dialogo.Add(cancelarBtn);

    app.Run(dialogo);
}

async Task AbrirEditarProducto()
{
    if (productos.Count == 0) return;

    int indice = indiceSeleccionado;
    var prod = productos[indice];

    var dialogo = new Dialog()
    {
        Title = "Editar producto",
        Width = 60,
        Height = 20
    };

    var codigoTxt = new TextField() { X = 15, Y = 2, Width = 30, Text = prod.Codigo };
    var nombreTxt = new TextField() { X = 15, Y = 4, Width = 30, Text = prod.Nombre };
    var precioTxt = new TextField() { X = 15, Y = 6, Width = 30, Text = prod.Precio.ToString() };
    var stockTxt = new TextField() { X = 15, Y = 8, Width = 30, Text = prod.Stock.ToString() };

    dialogo.Add(new Label() { Text = "Codigo:", X = 2, Y = 2 });
    dialogo.Add(new Label() { Text = "Nombre:", X = 2, Y = 4 });
    dialogo.Add(new Label() { Text = "Precio:", X = 2, Y = 6 });
    dialogo.Add(new Label() { Text = "Stock:", X = 2, Y = 8 });

    dialogo.Add(codigoTxt);
    dialogo.Add(nombreTxt);
    dialogo.Add(precioTxt);
    dialogo.Add(stockTxt);

    var guardarBtn = new Button() { Text = "Guardar", X = 10, Y = 12 };
    var cancelarBtn = new Button() { Text = "Cancelar", X = 25, Y = 12 };

    // ✔ GUARDAR (Accepting)
    guardarBtn.Accepting += async (_, _) =>
    {
        var editado = new ProductoDto(
            prod.Id,
            codigoTxt.Text?.ToString() ?? "",
            nombreTxt.Text?.ToString() ?? "",
            decimal.Parse(precioTxt.Text?.ToString() ?? "0"),
            int.Parse(stockTxt.Text?.ToString() ?? "0")
        );

        await http.PutAsJsonAsync(
            $"http://localhost:5050/productos/{prod.Id}",
            editado
        );

        productos = await CargarProductos();
        CargarLista();
        MostrarDetalle();

        dialogo.RequestStop();
    };

    // ✔ CANCELAR (Accepting también)
    cancelarBtn.Accepting += (_, _) =>
    {
        dialogo.RequestStop();
    };

    dialogo.Add(guardarBtn);
    dialogo.Add(cancelarBtn);

    app.Run(dialogo);;
}

async Task EliminarProducto()
{
    if (productos.Count == 0) return;

    int indice = indiceSeleccionado;
    var prod = productos[indice];

    await http.DeleteAsync(
        $"http://localhost:5050/productos/{prod.Id}"
    );

    productos = await CargarProductos();
    CargarLista();
    MostrarDetalle();
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

record ProductoNuevoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);

record MovimientoNuevoDto(
    int Id,
    int ProductoId,
    string Tipo,
    int Cantidad,
    DateTime Fecha
);

