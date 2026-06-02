#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Globalization;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


const string miserver = "http://localhost:5050"; 
using var http = new HttpClient { BaseAddress = new Uri(miserver) };
try {
   var lista = await http.GetFromJsonAsync<List<ProductoDto>>("/productos")
    ?? throw new HttpRequestException("El servidor no respondió con una lista de productos validos.");
} catch (HttpRequestException ex) {
    Console.WriteLine($"No se pudo conectar al servidor: {ex.Message}");
    return;
}
// ── Interfaz TUI ──────────────────────────────────────────────────────────
using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };
var productos = new List<ProductoDto>();
var productosFiltrados = new List<ProductoDto>();
var productosVista = new ObservableCollection<string>();
var movimientosVista = new ObservableCollection<string>();
var buscar = new TextField { X = 11, Y = 1, Width = 36};
//36 d ancho, col 11, fila 1

var listaProductos = new ListView {
    X = 1, Y = 4, Width = Dim.Percent(50), Height = Dim.Fill(3),
}; // col 1, fila 4, ancho 50% del contenedor, alto hasta 3 filas antes del final
listaProductos.SetSource(productosVista);

var listaMovimientos = new ListView {
    X = Pos.Right(listaProductos) + 1, Y = 4, Width = Dim.Fill(1), Height = Dim.Fill(3)
}; 
listaMovimientos.SetSource(movimientosVista);

var estado = new Label { X = 1, Y = Pos.Bottom(listaProductos) + 1, Width = Dim.Fill(1)};

var botonagregar = new Button { Text = "_Agregar", X = 1, Y =2};
var botonModificar = new Button { Text = "_Modificar", X = Pos.Right(botonagregar) + 1, Y = 2};
var botonEliminar = new Button { Text = "_Eliminar", X = Pos.Right(botonModificar) + 1, Y = 2};
var botonComprar = new Button { Text = "_Comprar", X = Pos.Right(botonEliminar) + 2, Y = 2};
var botonVender = new Button { Text = "_Vender", X = Pos.Right(botonComprar) + 1, Y = 2};
var botonAjustar = new Button { Text = "_Ajustar", X = Pos.Right(botonVender) + 1, Y = 2};
var botonActualizar = new Button { Text = "_Actualizar", X = Pos.Right(botonAjustar) + 2, Y = 2};

ventana.Add(
    new Label { Text = "Buscar:", X = 1, Y = 1 }, buscar,
    new Label { Text = "Productos", X = 1, Y = 3 },
    new Label { Text = "Movimientos", X = Pos.Right(listaProductos) + 1, Y = 3 }, 
    botonagregar, botonModificar, botonEliminar,
     botonComprar, botonVender, botonAjustar, 
     botonActualizar, estado, listaProductos, listaMovimientos
);
TextField Campo(Dialog dialogo, string etiqueta, string valor, int y) {
    dialogo.Add(new Label{ Text = etiqueta, X = 2, Y = y });
    var campo = new TextField { Text = valor, X = 15, Y = y, Width = 38 };
    dialogo.Add(campo);
    return campo;
}
void MostrarError(string titulo, string mensaje) =>
    MessageBox.ErrorQuery(app, titulo, mensaje, "Aceptar");
string FormatearProducto(ProductoDto p) =>
    $"{p.Codigo,-8} {Cortar(p.Nombre, 24),-24} ${p.Precio,9:N2}  Stock:{p.Stock,5}";
string FormatearMovimiento(MovimientoDto m) =>
    $"{m.Fecha:dd/MM/yyyy HH:mm}  {m.Tipo,-7}  {m.Cantidad,6}";
string Cortar(string texto, int largo) =>
    texto.Length <= largo ? texto : texto[..Math.Max(0, largo - 3)] + "";

app.Run(ventana);

// ── DTO ──────────────────────────────────────────────────────────────────

enum TipoMovimiento{Compra,Venta,Ajuste} //enum de los movimientos que se podran hacer
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock); // no agregamos un id xq el servidor lo asigna solo
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);
