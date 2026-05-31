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


//CATALOGOWINDOW
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
        
         var lblBuscar = new Label { Text = "Search:", X = 1, Y = 0 };
        txtBuscar = new TextField { X = Pos.Right(lblBuscar) + 1, Y = 0, Width = Dim.Fill(1) };

        listaProductos = new ListaAtajos {
            X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill(1),
            AllowsMarking = false,
            Interceptor = ProcesarAtajo 
        };
        
        
        var panelMaestro = new FrameView {
            Title  = " Productos  [A]Agregar [B]Modificar [E]Eliminar [F5]Recargar ",
            X = 0, Y = 0, Width = Dim.Percent(54), Height = Dim.Fill(1)
        };
        panelMaestro.Add(lblBuscar, txtBuscar, listaProductos);

        lblMovimientos = new Label { Text = " Seleccioná un producto", X = 1, Y = 0 };
        
        listaMovimientos = new ListaAtajos {
            X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill(),
            AllowsMarking = false,
            Interceptor = ProcesarAtajo 
        };

        var panelDetalle = new FrameView {
            Title  = " Movimientos  [C]Compra [V]Venta [J]Ajuste ",
            X = Pos.Right(panelMaestro), Y = 0, Width = Dim.Fill(), Height = Dim.Fill(1)
        };
        panelDetalle.Add(lblMovimientos, listaMovimientos);

        lblStatus = new Label {
            Text = " Listo  |  [TAB] Cambiar panel  |  [F9] Abrir menú  |  [ESC] Salir",
            X = 0, Y = Pos.AnchorEnd(1)
        };
        
    //MENU
         menu = new MenuBar {
            Menus = [
                new MenuBarItem("_Productos", [
                    new MenuItem("_Agregar",       "", () => EjecutarMenu(AgregarProducto)),
                    new MenuItem("Modificar (_B)", "", () => EjecutarMenu(ModificarProducto)),
                    new MenuItem("_Eliminar",      "", () => EjecutarMenu(EliminarProducto)),
                    new MenuItem("Recargar (_F5)", "", () => EjecutarMenu(() => _ = RecargarAsync())),
                ]),
                new MenuBarItem("_Movimientos", [
                    new MenuItem("_Compra",  "", () => EjecutarMenu(() => RegistrarMovimiento(TipoMovimiento.Compra))),
                    new MenuItem("_Venta",   "", () => EjecutarMenu(() => RegistrarMovimiento(TipoMovimiento.Venta))),
                    new MenuItem("A_juste",  "", () => EjecutarMenu(() => RegistrarMovimiento(TipoMovimiento.Ajuste))),
                ]),
                new MenuBarItem("A_yuda", [
                    new MenuItem("Ver Atajos (_F1)", "", () => EjecutarMenu(MostrarAyuda))
                ]),
                new MenuBarItem("_Salir", [
                    new MenuItem("Salir (_ESC)", "", () => Application.RequestStop()),
                ]),
            ]
        };

        Add(MasterPanel, DetailPanel, lblStatus);

        txtBuscar.TextChanged += (_, _) => AplicarFiltro();

        listaProductos.SelectedItemChanged += (_, e) => {
            if (e.Item >= 0 && e.Item < productosFiltrados.Count)
                _ = CargarMovimientosAsync(productosFiltrados[e.Item]);
        };

        KeyDown += (_, e) => ProcesarAtajo(e);

        RenderizarProductos();


      }

          public MenuBar ObtenerMenu() => menu;
    private void EjecutarMenu(Action accion) {
        _ = Task.Run(async () => {
            await Task.Delay(150);
            Application.Invoke(accion);
        });
    }

    private void AccionMenu(Action accion) {
        _ = Task.Run(async () => {
            await Task.Delay(100);
            Application.Invoke(accion);
        });
    }

    
    private void MostrarAyuda() {
        var dlg = new Dialog { Title = " Ayuda - Atajos de Sistema ", Width = 65, Height = 15 };
        
        var lblInfo = new Label {
            Text = "GENERAL:\n" +
                   "  [F9]  Abrir el menú superior (pestañas)\n" +
                   "  [TAB] Cambiar de panel (Filtro -> Productos -> Movimientos)\n" +
                   "  [ESC] Salir de la aplicación\n\n" +
                   "PRODUCTOS:\n" +
                   "  [A] Agregar   [B] Modificar   [E] Eliminar   [F5] Recargar\n\n" +
                   "MOVIMIENTOS:\n" +
                   "  [C] Compra    [V] Venta       [J] Ajuste",
            X = 2, Y = 1
        };

        var btn = new Button { Text = "_Aceptar", IsDefault = true };
        btn.Accepting += (_, _) => Application.RequestStop(dlg);
        
        dlg.Add(lblInfo);
        dlg.AddButton(btn);
        
        Application.Run(dlg);
    }



    
}