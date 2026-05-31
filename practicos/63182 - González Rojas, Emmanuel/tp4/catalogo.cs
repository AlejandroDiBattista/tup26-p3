#!sdk Microsoft.NET.Sdk
#:package Terminal.Gui@2.0.0-*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using Terminal.Gui;

//Configuración inicial y carga de datos

using var http = new HttpClient();
var api = new CatalogoApi(http);
List<ProductoDto> productosIniciales;

try {
    productosIniciales = await api.ObtenerProductosAsync();
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"\n✗ No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("  Verificá que servidor.cs está corriendo con: dotnet run servidor.cs\n");
    return;
}
//Apagar el servidor al cerrar la aplicación con Ctrl+C
Console.CancelKeyPress += (s, e) => {
    try { api.ApagarServidorAsync().Wait(); } catch { }
};

Application.Init();

var ventana  = new CatalogoWindow(api, productosIniciales);
var toplevel = new Toplevel();
toplevel.Add(ventana.ObtenerMenu(), ventana);

Application.Run(toplevel);

Application.Shutdown();

try { 
    api.ApagarServidorAsync().Wait(500); 
} catch { }


// // ── Interfaz TUI ──────────────────────────────────────────────────────────

// using IApplication app = Application.Create().Init();
// using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

// var detalleProducto = new Label {
//     Text = $"""
//             # PRODUCTO 

//             - Id     : {producto.Id}
//             - Código : {producto.Codigo}
//             - Nombre : {producto.Nombre}
//             - Precio : ${producto.Precio,10:N2}
//             - Stock  :  {producto.Stock,10}
//             """,
//     X = 4, Y = 2,
// };

// ventana.Add(detalleProducto);

// app.Run(ventana);

// static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
//     const string url = "http://localhost:5050/producto";
//     return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
// }

// // ── DTO ───────────────────────────────────────────────────────────────────

// record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
