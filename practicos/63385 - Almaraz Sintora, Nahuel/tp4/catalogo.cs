#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

using System.Collections.ObjectModel;
using System.Globalization;

using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5050") };

try {
    await http.GetFromJsonAsync<List<ProductoDto>>("productos");
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Primero ejecuta: dotnet run servidor.cs");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(http));

public sealed class CatalogoWindow : Runnable {
    private readonly HttpClient http;
    private readonly ObservableCollection<string> lineasProductos = [];
    private readonly ObservableCollection<string> lineasMovimientos = [];

    private List<ProductoDto> productos = [];
    private List<ProductoDto> productosFiltrados = [];

    private TextField buscarTexto = null!;
    private ListView listaProductos = null!;
    private ListView listaMovimientos = null!;
    private Label estado = null!;
    public CatalogoWindow(HttpClient http) {
        this.http = http;
        Title = "Catalogo REST - ESC para salir";
        Width = Dim.Fill();
        Height = Dim.Fill();
    }
}

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
