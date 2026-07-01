#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.Input;


// ── Consulta inicial al servidor ──────────────────────────────────────────

List<ProductoDto> productos;
try {
    using var http = new HttpClient();
    productos = await CargarProductoAsync(http);  
} 
catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };
int productoSeleccionado = 2;

var lista = new ListView() {
    X = 1,
    Y = 2,
    Width = 55,
    Height = 20
};
var detalle = new Label() {
    X = 60,
    Y = 9,
    Width = 50,
    Height = 15,
    Text = "Seleccione un producto"
};
var infoProducto = new Label() {
    X = 60,
    Y = 2,
    Width = 50,
    Height = 6,
    Text = ""
};

lista.SetSource(
    new System.Collections.ObjectModel.ObservableCollection<string>
       ( productos.Select(p => $"{p.Codigo} | {p.Nombre} | ${p.Precio} | Stock: {p.Stock}" ). ToList()
    )
);

if (productos.Count > 0)
{

     infoProducto.Text =
     $"Código: {producto.Codigo}\n" +
     $"Nombre: {producto.Nombre}\n" +
     $"Precio: ${producto.Precio:N0}\n" +
     $"Stock: {producto.Stock}";

    using var http = new HttpClient();
    var movimientos = await CargarMovimientosAsync(http, 2);


    if (movimientos.Count == 0) {
        
        detalle.Text = "Sin movimientos registrados";
    }
    else {
        detalle.Text = string.Join("\n", 
        movimientos.Select(m => $"{m.Tipo} | Cant: {m.Cantidad} | Fecha: {m.Fecha:g}"));
    }
}
 
var tituloProductos = new Label() {
    X = 1,
    Y = 0,
    Text = "Productos"
};

var tituloDetalle = new Label() {
     X = 60,
     Y = 0,
     Text = " Detalles y Movimientos"
};

ventana.Add(tituloProductos);
ventana.Add(tituloDetalle);
ventana.Add(lista);
lista.Accepting += async (_, _) =>
{
    productoSeleccionado = productos[lista.SelectedItem ?? 0 ].Id

    var producto = productos[lista.SelectedItem ?? 0];

    infoProducto.Text =
        $"Código: {producto.Codigo}\n" +
        $"Nombre: {producto.Nombre}\n" +
        $"Precio: ${producto.Precio:N0}\n" +
        $"Stock: {producto.Stock}";

    using var http = new HttpClient();

    var movimientos = await CargarMovimientosAsync(http, productoSeleccionado);

    if (movimientos.Count == 0)
    {
        detalle.Text = "Sin movimientos registrados";
    }
    else
    {
        detalle.Text = string.Join(
            "\n",
            movimientos.Select(m =>
                $"{m.Tipo} | Cant: {m.Cantidad} | Fecha: {m.Fecha:g}")
        );
    }
};
ventana.Add(infoProducto);
ventana.Add(detalle);

var botonCompra = new Button()
{
    X = 60,
    Y = 18,
    Text = "_Compra +10"
};
ventana.Add(botonCompra);
botonCompra.SetFocus();

var botonVenta = new Button()
{
    X = 60,
    Y = 20,
    Text = "_Venta -5"
};

ventana.Add(botonVenta);

botonVenta.Accepting += async (_, _) =>
{
    using var http = new HttpClient();

    await http.PostAsync(
        $"http://localhost:5050/productos/{productoSeleccionado}/movimientos?tipo=Venta&cantidad=5",
        null
    );
};

botonCompra.Accepting += async (_, _) =>
{
    using var http = new HttpClient();

    await http.PostAsync(
        $"http://localhost:5050/productos/{productoSeleccionado}/movimientos?tipo=Compra&cantidad=10",
        null
    );
};

var botonAjuste = new Button()
{
    X = 60,
    Y = 22,
    Text = "_Ajuste 100"
};

ventana.Add(botonAjuste);

botonAjuste.Accepting += async (_, _) =>
{
    using var http = new HttpClient();

    await http.PostAsync(
        $"http://localhost:5050/productos/{productoSeleccionado}/movimientos?tipo=Ajuste&cantidad=100",
        null
    );
};

var ayuda = new Label() {
    X = 1,
    Y = 22,
    Text = "Use Enter para seleccionar un producto. Alt+C Compra | Alt+V Venta | Alt+A Ajuste"
};

ventana.Add(ayuda);

var botonAgregar = new Button()
{
    X = 60,
    Y = 24,
    Text = "_Agregar"
};

var botonModificar = new Button()
{
    X = 75,
    Y = 24,
    Text = "_Modificar"
};

var botonEliminar = new Button()
{
    X = 95,
    Y = 24,
    Text = "_Eliminar"
};

ventana.Add(botonAgregar);
ventana.Add(botonModificar);
ventana.Add(botonEliminar);

botonAgregar.Accepting += (_, _) =>
{
    Console.WriteLine("Agregar producto");
};

botonModificar.Accepting += (_, _) =>
{
    Console.WriteLine("Modificar producto");
};

botonEliminar.Accepting += (_, _) =>
{
    Console.WriteLine("Eliminar producto");
};


app.Run(ventana);

static async Task<List<ProductoDto>> CargarProductoAsync(HttpClient http)
{
    const string url = "http://Localhost:5050/productos";

    return await http.GetFromJsonAsync<List<ProductoDto>>(url) 
        ?? new List<ProductoDto>();
}

static async Task<List<MovimientoDto>> CargarMovimientosAsync(HttpClient http, int productoId)
{
    string url = $"http://Localhost:5050/productos/{productoId}/movimientos";

    return await http.GetFromJsonAsync<List<MovimientoDto>>(url) 
        ?? new List<MovimientoDto>();
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

record MovimientoDto(
    int Id, int productoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha 
);

enum TipoMovimiento{
    Compra,
    Venta,
    Ajuste
}

