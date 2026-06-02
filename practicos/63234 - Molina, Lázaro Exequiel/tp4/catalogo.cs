#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string UrlApi = "http://localhost:5050";

var opcionesJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);
opcionesJson.Converters.Add(new JsonStringEnumConverter());

using var cliente = new HttpClient { BaseAddress = new Uri(UrlApi) };

var productos = new List<ProductoDto>();
var productosMostrados = new List<ProductoDto>();
var filasProductos = new ObservableCollection<string>();
var filasMovimientos = new ObservableCollection<string>();
ProductoDto? elegido = null;

using IApplication app = Application.Create();
app.Init();

using Window pantalla = new() { Title = " Gestor de catalogo - ESC para salir " };

var textoBuscar = new TextField { X = 10, Y = 1, Width = 48, Text = "" };
var listado = new ListView { X = 1, Y = 3, Width = 60, Height = 14 };
listado.SetSource(filasProductos);

var historial = new ListView { X = 64, Y = 3, Width = 54, Height = 14 };
historial.SetSource(filasMovimientos);

var txtCodigo = new TextField { X = 9, Y = 20, Width = 18, Text = "" };
var txtNombre = new TextField { X = 37, Y = 20, Width = 35, Text = "" };
var txtPrecio = new TextField { X = 9, Y = 22, Width = 18, Text = "0" };
var txtStock = new TextField { X = 37, Y = 22, Width = 12, Text = "0" };
var txtCantidad = new TextField { X = 78, Y = 22, Width = 10, Text = "1" };
var mensaje = new Label { X = 1, Y = 26, Width = 118, Text = "Conectando con el servidor..." };
var btnLimpiar = new Button { X = 1, Y = 24, Text = "Nuevo" };
var btnGuardar = new Button { X = 12, Y = 24, Text = "Guardar" };
var btnBorrar = new Button { X = 25, Y = 24, Text = "Borrar" };
var btnCompra = new Button { X = 64, Y = 24, Text = "Compra" };
var btnVenta = new Button { X = 76, Y = 24, Text = "Venta" };
var btnAjuste = new Button { X = 87, Y = 24, Text = "Ajuste" };

pantalla.Add(
    new Label { Text = "Productos", X = 1, Y = 0 },
    new Label { Text = "Filtrar:", X = 1, Y = 1 }, textoBuscar,
    listado,
    new Label { Text = "Movimientos de stock", X = 64, Y = 0 },
    historial,
    new Label { Text = "Codigo:", X = 1, Y = 20 }, txtCodigo,
    new Label { Text = "Nombre:", X = 29, Y = 20 }, txtNombre,
    new Label { Text = "Precio:", X = 1, Y = 22 }, txtPrecio,
    new Label { Text = "Stock:", X = 29, Y = 22 }, txtStock,
    new Label { Text = "Cantidad:", X = 64, Y = 22 }, txtCantidad,
    btnLimpiar, btnGuardar, btnBorrar, btnCompra, btnVenta, btnAjuste,
    mensaje
);
