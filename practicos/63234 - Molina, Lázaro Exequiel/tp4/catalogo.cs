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
