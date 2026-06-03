#:package Terminal.Gui@*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// Cargar productos

List<ProductoDto> productos;
try
{
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    return;
}

// Ventana

using IApplication app = Application.Create().Init();

    using Window ventana = new()
   {
    Title = "Catalogo REST"
    };
var txtBuscar = new TextField()
{
    X = 1,
    Y = 18,
    Width = 40
};
var menu = new MenuBar
{
    Menus = new[]
    {
    new MenuBarItem(
            "_Productos",
            new[]
            {
                new MenuItem("_Agregar", "", () => { }),
                new MenuItem("_Editar", "", () => { }),
                new MenuItem("_Eliminar", "", () => { })
            }
        ),
     new MenuBarItem(
            "_Movimientos",
            new[]
            {
                new MenuItem("_Compra", "", () => { }),
                new MenuItem("_Venta", "", () => { }),
                new MenuItem("_Ajuste", "", () => { })
            }
        )
    }
};
var lineasProductos = new List<string>();

foreach (var p in productos)
{
    lineasProductos.Add(
        $"[{p.Id}] {p.Nombre} - ${p.Precio} - Stock:{p.Stock}"
    );
}
var listaProductos = new ListView()
{
    X = 1,
    Y = 1,
    Width = 50,
    Height = 15
};
listaProductos.SetSource(
    new System.Collections.ObjectModel.ObservableCollection<string>(
        lineasProductos
    )
);
var producto = productos[0];
var detalleProducto = new Label
{
    X = 55,
    Y = 1,
    Text = $"""
            Id: {producto.Id}
            Código: {producto.Codigo}
            Nombre: {producto.Nombre}
            Precio: ${producto.Precio}
            Stock: {producto.Stock}
            """
};
var movimientosProducto = new Label
{
    X = 55,
    Y = 12,
    Width = 60,
    Height = 10,
    Text = "Movimientos:"
};
    listaProductos.Accepting += async (s, e) =>
    {
        int indice = listaProductos.SelectedItem ?? 0;
        var productoSeleccionado = productos[indice];
        using var http = new HttpClient();
    var movimientos =
    await CargarMovimientosAsync(
        http,
        productoSeleccionado.Id
    );

     string textoMovimientos = "Movimientos:\n\n";

     foreach (var m in movimientos)
      {
        textoMovimientos +=
        $"{m.Tipo} | {m.Cantidad} | {m.Fecha:d}\n";}
       movimientosProducto.Text = textoMovimientos;
        detalleProducto.Text = $"""
            Id: {productoSeleccionado.Id}
             Código: {productoSeleccionado.Codigo}
             Nombre: {productoSeleccionado.Nombre}
             Precio: ${productoSeleccionado.Precio}
             Stock: {productoSeleccionado.Stock}
            """;
};

ventana.Add(listaProductos);

ventana.Add(detalleProducto);
ventana.Add(movimientosProducto);
ventana.Add(txtBuscar);

app.Run(ventana);

    static async Task<List<ProductoDto>> CargarProductosAsync(HttpClient http)
   {
    const string url = "http://localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url)
        ?? throw new HttpRequestException("Lista vacía");}



     static async Task<List<MovimientoDto>> CargarMovimientosAsync(
      HttpClient http,
      int productoId
)

{
    return await http.GetFromJsonAsync<List<MovimientoDto>>(
        $"http://localhost:5050/productos/{productoId}/movimientos"
    ) ?? [];}
    record MovimientoDto(
    int Id,
    int ProductoId,
    int Tipo,
    int Cantidad,
    DateTime Fecha);


    record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal Precio,
    int Stock);