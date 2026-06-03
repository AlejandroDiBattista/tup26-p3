#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

const string ApiBase = "http://localhost:5050";
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
json.Converters.Add(new JsonStringEnumConverter());

using var http = new HttpClient { BaseAddress = new Uri(ApiBase) };
List<ProductoDto> productos = [];
List<ProductoDto> productosFiltrados = [];
var productosVista = new ObservableCollection<string>();
var movimientosVista = new ObservableCollection<string>();
ProductoDto? productoSeleccionado = null;
using IApplication app = Application.Create();
app.Init();

using Window ventana = new() { Title = " Catalogo REST - ESC para salir " };

var tituloProductos = new Label { Text = "Productos", X = 1, Y = 0 };
var tituloMovimientos = new Label { Text = "Movimientos del producto", X = 66, Y = 0 };

var buscar = new TextField { X = 9, Y = 1, Width = 52, Text = "" };
var listaProductos = new ListView { X = 1, Y = 3, Width = 62, Height = 13 };
listaProductos.SetSource(productosVista);

var listaMovimientos = new ListView { X = 66, Y = 2, Width = 56, Height = 14 };
listaMovimientos.SetSource(movimientosVista);