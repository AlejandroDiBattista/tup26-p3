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

// ── Consulta inicial al servidor ──────────────────────────────────────────

ProductoDto producto;
try {
    using var http = new HttpClient( );
    producto = await CargarProductoAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

List<ProductoDto> todosLosProductos = new();
List<ProductoDto> productosFiltrados = new();
ProductoDto? productoSeleccionado = null;
List<MovimientoDto> movimientos = new();

using Window ventana = new() {
    Title = " Catálogo REST - Maestro/Detalle (ESC para salir) ",
    Width = Dim.Fill(), 
    Height = Dim.Fill()
};

var menuPrincipal = new MenuBar(new MenuBarItem[] {
    new MenuBarItem("_Archivo", new MenuItem[] {
        new MenuItem("_Salir", "Cierra la aplicación", () => Application.RequestStop())
    }),
    new MenuBarItem("_Productos", new MenuItem[] {
        new MenuItem("_Nuevo", "", () => MostrarDialogoProducto(null)),
        new MenuItem("_Editar", "", () => MostrarDialogoProducto(productoSeleccionado)),
        new MenuItem("_Eliminar", "", () => EliminarProductoSeleccionado())
    }),
    new MenuBarItem("_Movimientos", new MenuItem[] {
        new MenuItem("_Registrar Movimiento", "", () => MostrarDialogoMovimiento(productoSeleccionado))
    })
});

var panelIzquierdo = new FrameView("Productos") {
    X = 0, Y = Pos.Bottom(menuPrincipal),
    Width = Dim.Percent(50), Height = Dim.Fill()
};

var lblBuscar = new Label("Buscar:") { X = 0, Y = 0 };
var txtBuscar = new TextField("") {
    X = Pos.Right(lblBuscar) + 1, Y = 0,
    Width = Dim.Fill()
};

var listaProductos = new ListView() {
    X = 0, Y = Pos.Bottom(lblBuscar) + 1,
    Width = Dim.Fill(), Height = Dim.Fill(),
    AllowsMarking = false
};

panelIzquierdo.Add(lblBuscar, txtBuscar, listaProductos);

var panelDerecho = new FrameView("Movimientos de Stock") {
    X = Pos.Right(panelIzquierdo), Y = Pos.Bottom(menuPrincipal),
    Width = Dim.Fill(), Height = Dim.Fill()
};

var listaMovimientos = new ListView() {
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill(),
    AllowsMarking = false
};

panelDerecho.Add(listaMovimientos);
ventana.Add(menuPrincipal, panelIzquierdo, panelDerecho);




app.Run(ventana);

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
