#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

const string BaseUrl = "http://localhost:5050";

List<ProductoDto> productosIniciales;
try {
    using var clientePrueba = new HttpClient();
    productosIniciales = await clientePrueba.GetFromJsonAsync<List<ProductoDto>>($"{BaseUrl}/productos") ?? [];
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verifica que servidor.cs este corriendo en http://localhost:5050");
    return;
}

using IApplication appTui = Application.Create();
appTui.Init();

var ventana = new Window {
    Title = " CatalogoREST - F1 Agregar  F2 Editar  F3 Eliminar  F4 Movimiento  ESC Salir ",
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill(),
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var detalleProducto = new Label {
    Text = $"""
            # PRODUCTO 

            - Id     : {producto.Id}
            - Código : {producto.Codigo}
            - Nombre : {producto.Nombre}
            - Precio : ${producto.Precio,10:N2}
            - Stock  :  {producto.Stock,10}
            """,
    X = 4, Y = 2,
};

ventana.Add(detalleProducto);

app.Run(ventana);

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
