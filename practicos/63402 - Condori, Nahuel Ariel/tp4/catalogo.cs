#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjetModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewsBase;
using Terminal.Gui.Views;


List <ProductoDto> productos;
try
{
    using var http = new HttpClient();
    productos = await CargarProductosAsync(http);
} catch (httpRequestException ex) {
    Console.WriteLine($"Error al cargar productos: {ex.Message}");
    Console.WriteLine("Verificá que servidor.cs este corriendo en http://localhost:5050");
    return;
}