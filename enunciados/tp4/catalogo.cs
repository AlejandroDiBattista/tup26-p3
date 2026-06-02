#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Input;
using System.Collections.ObjectModel;

var http = new HttpClient{ BaseAddress = new Uri("http://localhost:5050/") };

// ── Consulta inicial al servidor ──────────────────────────────────────────


try {
    await http.GetAsync("/productos");
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(http));


// ── DTO ───────────────────────────────────────────────────────────────────

public class ProductoDto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public ProductoDto Clone() => new() {
        Id=Id, Codigo=Codigo, Nombre=Nombre, Precio=Precio, Stock=Stock
    };
}

public class MovimientoDeProductoDto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = "Compra";
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

public sealed class CatalogoWindow : Runnable {
    private readonly HttpClient http;
    private List<ProductoDto> productos = [];
    private List<ProductoDto> filtrados = [];
    private ListView listaProductos = null!;
    private ListView listaMovimientos = null!;
    private TextField searchField = null!;
    private Label statusLabel = null!;

    public CatalogoWindow(HttpClient http) {
        this.http = http;
        Title = "Catálogo de Productos";
        Width = Dim.Fill();
        Height = Dim.Fill();
        BuildLayout();
        Task.Run(CargarProductos);
    }
}
