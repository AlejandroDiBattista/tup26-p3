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