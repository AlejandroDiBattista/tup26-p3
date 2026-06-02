#:package Terminal.Gui@2.*
#:property PublishAot=false


using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;


// ── Consulta inicial al servidor ──────────────────────────────────────────

ProductoDto producto;
try {
    using var http = new HttpClient();
    producto = await CargarProductoAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
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
    const string url = "http://localhost:5050/productos";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

enum TipoMovimiento{Compra,Venta,Ajuste} //enum de los movimientos que se podran hacer
record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record ProductoEntrada(string Codigo, string Nombre, decimal Precio, int Stock); // no agregamos un id xq el servidor lo asigna solo
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
record MovimientoEntrada(TipoMovimiento Tipo, int Cantidad);
