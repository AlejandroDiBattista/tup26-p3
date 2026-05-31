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

// DTOs
enum TipoMovimiento { Compra, Venta, Ajuste }

class ProductoDto {
    public int     Id     { get; set; }
    public string  Codigo { get; set; } = "";
    public string  Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int     Stock  { get; set; }
}

class MovimientoDto {
    public int            Id         { get; set; }
    public int            ProductoId { get; set; }
    public TipoMovimiento Tipo       { get; set; }
    public int            Cantidad   { get; set; }
    public DateTime       Fecha      { get; set; }
}

class CatalogoApi {
    private readonly HttpClient            http;
    private readonly JsonSerializerOptions opts;
    private const    string                Base = "http://localhost:5050";

    public CatalogoApi(HttpClient http) {
        this.http = http;
        opts = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<List<ProductoDto>> ObtenerProductosAsync() =>
        await http.GetFromJsonAsync<List<ProductoDto>>($"{Base}/productos", opts)
        ?? throw new HttpRequestException("El servidor devolvió una respuesta vacía.");

    public async Task<(bool ok, string msg)> AgregarProductoAsync(ProductoDto p) {
        var resp = await http.PostAsJsonAsync($"{Base}/productos", p, opts);
        return (resp.IsSuccessStatusCode, await resp.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string msg)> ModificarProductoAsync(ProductoDto p) {
        var resp = await http.PutAsJsonAsync($"{Base}/productos/{p.Id}", p, opts);
        return (resp.IsSuccessStatusCode, await resp.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string msg)> EliminarProductoAsync(int id) {
        var resp = await http.DeleteAsync($"{Base}/productos/{id}");
        return (resp.IsSuccessStatusCode, await resp.Content.ReadAsStringAsync());
    }

    public async Task<List<MovimientoDto>> ObtenerMovimientosAsync(int productoId) =>
        await http.GetFromJsonAsync<List<MovimientoDto>>(
            $"{Base}/productos/{productoId}/movimientos", opts) ?? [];

    public async Task<(bool ok, string msg)> RegistrarMovimientoAsync(int productoId, MovimientoDto m) {
        var resp = await http.PostAsJsonAsync($"{Base}/productos/{productoId}/movimientos", m, opts);
        return (resp.IsSuccessStatusCode, await resp.Content.ReadAsStringAsync());
    }

    public async Task ApagarServidorAsync() {
        try { await http.DeleteAsync($"{Base}/shutdown"); } catch { }
    }
}


//CatalogoWindow
class CatalogoWindow : Window {
    
private readonly CatalogoApi api;
    private List<ProductoDto> productos = [];
    private List<ProductoDto> productosFiltrados = [];
    private List<MovimientoDto>  movimientosActuales = [];

    private readonly MenuBar     menu;
    private readonly TextField   txtBuscar;
    private readonly ListaAtajos listaProductos;
    private readonly Label       lblMovimientos;
    private readonly ListaAtajos listaMovimientos;
    private readonly Label       lblStatus;

     public CatalogoWindow(CatalogoApi api, List<ProductoDto> productosIniciales) {
        this.api = api;
        this.productos = productosIniciales;
        productosFiltrados = [.. productos];

        Title = "Catálogo REST";
        X = 0;
        Y = 1;
        Width  = Dim.Fill();
        Height = Dim.Fill();
        
         var lblBuscar = new Label { Text = "Buscar:", X = 1, Y = 0 };
        txtBuscar = new TextField { X = Pos.Right(lblBuscar) + 1, Y = 0, Width = Dim.Fill(1) };

        listaProductos = new ListaAtajos {
            X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill(1),
            AllowsMarking = false,
            Interceptor = ProcesarAtajo 
        };
        
        
        
        }
    
}