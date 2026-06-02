using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.ObjectModel;
using Terminal.Gui;

public enum TipoMovimiento { Compra, Venta, Ajuste }

public class Producto 
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public override string ToString() => $"[{Codigo}] {Nombre} - ${Precio} (Stock: {Stock})";
}

public class MovimientoDeProducto 
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }

    public override string ToString() => $"{Fecha:dd/MM HH:mm} | {Tipo} -> {Cantidad} unid.";
}

public class Program 
{
    static readonly HttpClient http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    static List<Producto> listaProductos = new();
    static List<MovimientoDeProducto> listaMovimientos = new();
    
    static ListView listProductosUI;
    static ListView listMovimientosUI;
    static TextField txtBuscar;
    static Producto productoSeleccionado = null;

    public static void Main() 
    {
        Application.Init();
        var win = new Window { Title = "Sistema de Catálogo REST - TUI",
            X = 0, Y = 1, 
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        var menu = new MenuBar { Menus = new MenuBarItem[] {
            new MenuBarItem("_Archivo", new MenuItem[] {
                new MenuItem("_Salir", "", () => Application.RequestStop())
            }),
            new MenuBarItem("_Productos", new MenuItem[] {
                new MenuItem("_Nuevo Producto", "", DialogoNuevoProducto),
                new MenuItem("_Editar Producto", "", DialogoEditarProducto),
                new MenuItem("E_liminar Producto", "", EliminarProducto)
            }),
            new MenuBarItem("_Stock", new MenuItem[] {
                new MenuItem("Registrar _Movimiento", "", DialogoRegistrarMovimiento)
            })
        } };
        var frameIzq = new FrameView { Title = "Productos",
            X = 0, Y = 0,
            Width = Dim.Percent(50), Height = Dim.Fill()
        };

        var lblBuscar = new Label { Text = "Buscar:", X = 1, Y = 0 };
        txtBuscar = new TextField { Text = "", X = Pos.Right(lblBuscar) + 1, Y = 0, Width = Dim.Fill() - 1 };
        txtBuscar.TextChanged += (s, e) => FiltrarProductos();

        listProductosUI = new ListView {
            X = 0, Y = 2,
            Width = Dim.Fill(), Height = Dim.Fill(),
            AllowsMarking = false
        };
        listProductosUI.SelectedItemChanged += (s, e) => {
            if (listaProductos.Count > 0 && e.Item >= 0) {
                productoSeleccionado = listaProductos[e.Item];
                CargarMovimientos();
            }
        };

        frameIzq.Add(lblBuscar, txtBuscar, listProductosUI);
        var frameDer = new FrameView { Title = "Historial de Stock",
            X = Pos.Right(frameIzq), Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        listMovimientosUI = new ListView {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        frameDer.Add(listMovimientosUI);
        win.Add(frameIzq, frameDer);

        var miTop = new Toplevel();
        miTop.Add(menu, win);
        CargarProductos();

        Application.Run(miTop);
        Application.Shutdown();
    }
    static void CargarProductos() 
    {
        try {
            var jsonOpciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var respuesta = http.GetFromJsonAsync<List<Producto>>("/productos", jsonOpciones).Result;
            
            listaProductos = respuesta ?? new List<Producto>();
            FiltrarProductos(); 
        } catch {
            MessageBox.ErrorQuery("Error", "No se pudo conectar al servidor. Asegurate de que esté corriendo.", "OK");
        }
    }


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
