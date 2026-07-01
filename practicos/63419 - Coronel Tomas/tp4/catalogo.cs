#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

List<ProductoDto> productos;

try
{
    using var http = new HttpClient();

    productos = await http.GetFromJsonAsync<List<ProductoDto>>
    (
        "http://localhost:5050/productos"
    ) ?? [];
}
catch
{
    Console.WriteLine("No se pudo conectar con el servidor.");
    return;
}

using IApplication app = Application.Create().Init();

using Window ventana = new()
{
    Title = " Catalogo de Cookies "
};

string textoProductos = "";

productos.ForEach(p =>
{
    textoProductos +=
        $"{p.Codigo} | {p.Nombre} | ${p.Precio} | Stock: {p.Stock}\n";
});

var lista = new Label()
{
    X = 1,
    Y = 1,
    Text = textoProductos
};

var detalle = new Label()
{
    X = 1,
    Y = productos.Count + 4,
    Text =
    """
    Opciones futuras:

    - Agregar producto
    - Modificar producto
    - Eliminar producto
    - Registrar compra
    - Registrar venta
    - Registrar ajuste
    """
};

ventana.Add(lista);
ventana.Add(detalle);

app.Run(ventana);

record ProductoDto
(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock
);