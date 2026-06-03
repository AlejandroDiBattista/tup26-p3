#:package Terminal.Gui@2.*
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;


// ── Configuración inicial ────

using IApplication app = Application.Create().Init();
var clienteHttp = new HttpClient { BaseAddress = new Uri("http://localhost:5050") };
var opcionesJson = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

// ── Validar conexión con el servidor ──────

try {
    await clienteHttp.GetAsync("/productos");
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"Error: {ex.Message}");
    return;
}

// ── Estado ──────────

List<ProductoDto> todosLosProductos = new();
List<ProductoDto> productosFiltrados = new();
ProductoDto? productoSeleccionado = null;
List<MovimientoDto> movimientos = new();

// Declaración de controles
TextField txtBuscar = new TextField { Text = "" };
ListView listaProductos = new ListView();
ListView listaMovimientos = new ListView();

// ── Funciones ─────────

async Task CargarProductosAsync() {
    try {
        var resultado = await clienteHttp.GetFromJsonAsync<List<ProductoDto>>("/productos", opcionesJson);
        if (resultado != null) {
            app.Invoke(() => {
                todosLosProductos = resultado;
                FiltrarProductos();
            });
        }
    } catch (Exception ex) {
        app.Invoke(() => MessageBox.Query(app, "Error", ex.Message, "OK"));
    }
}

void FiltrarProductos() {
    var busqueda = txtBuscar.Text?.ToString()?.ToLower() ?? "";
    productosFiltrados = todosLosProductos
        .Where(p => p.Codigo.ToLower().Contains(busqueda) || p.Nombre.ToLower().Contains(busqueda))
        .ToList();
    
    listaProductos.SetSource(
    new ObservableCollection<string>(
        productosFiltrados.Select(p => $"[{p.Codigo}] {p.Nombre}")
    )
);
}

async Task CargarMovimientosAsync() {
    if (productoSeleccionado == null) return;
    var resultado = await clienteHttp.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoSeleccionado.Id}/movimientos", opcionesJson);
    if (resultado != null) {
        app.Invoke(() => {
            movimientos = resultado;
            listaMovimientos.SetSource(
            new ObservableCollection<string>(
            movimientos.Select(m =>
                $"{m.Fecha:dd/MM} | {m.Tipo} | {m.Cantidad}")
        )
    );
        });
    }
}

// ── Interfaz ──────

Window ventana = new() { Title = "Catálogo REST (ESC para salir)" };

var menu = new MenuBar(new MenuBarItem[] {
    new MenuBarItem("_Productos", new MenuItem[] {
        new MenuItem("_Salir", "", () => app.RequestStop())
    })
});

var panelIzq = new FrameView { Title = "Productos", X = 0, Y = 1, Width = 50, Height = 20 };
panelIzq.Add(new Label{Text = "Buscar:",
    X = 0,
    Y = 0});
    
txtBuscar.X = 8; txtBuscar.Width = 40;
listaProductos.X = 0; listaProductos.Y = 2; listaProductos.Width = 48; listaProductos.Height = 15;
panelIzq.Add(listaProductos);

var panelDer = new FrameView { Title = "Movimientos", X = 51, Y = 1, Width = 50, Height = 20 };
listaMovimientos.Width = 48; listaMovimientos.Height = 18;
panelDer.Add(listaMovimientos);

ventana.Add(menu, panelIzq, panelDer);

// ── Eventos ─────

txtBuscar.TextChanged += (s, e) => FiltrarProductos();

// Cambio seguro para v2: usamos SelectedItemChanged si está disponible, 
// o un evento genérico de selección
listaProductos.Accepting += (s, e) => {
    if (listaProductos.SelectedItem.HasValue &&
        listaProductos.SelectedItem.Value >= 0 &&
        listaProductos.SelectedItem.Value < productosFiltrados.Count)
    {
        productoSeleccionado =
            productosFiltrados[listaProductos.SelectedItem.Value];

        _ = Task.Run(CargarMovimientosAsync);
    }
    
};

_ = Task.Run(CargarProductosAsync);
app.Run(ventana);

// ── DTOs ────

public enum TipoMovimiento { Compra, Venta, Ajuste }
public record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
public record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);