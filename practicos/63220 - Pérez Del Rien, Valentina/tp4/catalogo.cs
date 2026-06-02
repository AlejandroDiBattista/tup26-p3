#!/usr/bin/env -S dotnet run
#:sdk Microsoft.NET.Sdk
#:package Terminal.Gui@2.4.3

#pragma warning disable IL2026, IL3050

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using System.Text.Json;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization.Metadata;

var http = new HttpClient() { BaseAddress = new Uri("http://localhost:5000") };

var jsonOpts = new JsonSerializerOptions 
{ 
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver() 
};

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(http, jsonOpts));

class CatalogoWindow : Window
{
    private readonly HttpClient http;
    private readonly JsonSerializerOptions jsonOpts;

    private List<Producto> productos          = new();
    private List<Producto> productosFiltrados = new();
    private Producto?      productoSeleccionado = null;

    private readonly ListView  listaProductos;
    private readonly ListView  listaMovimientos;
    private readonly TextField campoBusqueda;

    public CatalogoWindow(HttpClient http, JsonSerializerOptions jsonOpts)
    {
        this.http     = http;
        this.jsonOpts = jsonOpts;

        Title  = "Catálogo de Productos";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Productos",
                [
                    new MenuItem("_Agregar",  "Ctrl+A", () => _ = MostrarDialogoAgregar()),
                    new MenuItem("_Editar",   "Ctrl+E", () => _ = MostrarDialogoEditar()),
                    new MenuItem("_Eliminar", "Ctrl+D", () => _ = EliminarProducto()),
                ]),
                new MenuBarItem("_Movimientos",
                [
                    new MenuItem("_Compra", "Alt+C", () => _ = RegistrarMovimiento("Compra")),
                    new MenuItem("_Venta",  "Alt+X", () => _ = RegistrarMovimiento("Venta")),
                    new MenuItem("_Ajuste", "Alt+J", () => _ = RegistrarMovimiento("Ajuste")),
                ]),
                new MenuBarItem("_Salir",
                [
                    new MenuItem("_Salir", "Alt+Q", () => App!.RequestStop()),
                ]),
            ]
        };

        FrameView panelIzq = new()
        {
            Title  = "Productos",
            X      = 0,
            Y      = 1,
            Width  = Dim.Percent(50),
            Height = Dim.Fill(1) 
        };

        Label lblBuscar = new() { Text = "Buscar: ", X = 0, Y = 0 };

        campoBusqueda = new TextField()
        {
            X     = Pos.Right(lblBuscar),
            Y     = 0,
            Width = Dim.Fill()
        };
        campoBusqueda.TextChanged += (_, _) => BuscarProductos();

        listaProductos = new ListView()
        {
            X = 0, Y = 2,
            Width  = Dim.Fill(),
            Height = Dim.Fill()
        };
        listaProductos.ValueChanged += (_, _) =>
        {
            int idx = listaProductos.SelectedItem ?? -1;
            if (idx >= 0 && idx < productosFiltrados.Count)
            {
                productoSeleccionado = productosFiltrados[idx];
                _ = CargarMovimientos();
            }
        };

        panelIzq.Add(lblBuscar, campoBusqueda, listaProductos);

        FrameView panelDer = new()
        {
            Title  = "Movimientos",
            X      = Pos.Right(panelIzq),
            Y      = 1,
            Width  = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        listaMovimientos = new ListView()
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill()
        };

        panelDer.Add(listaMovimientos);

        Label barraAtajos = new()
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = " Atajos: [Ctrl+A] Agregar │ [Ctrl+E] Editar │ [Ctrl+D] Eliminar │ [Alt+C] Compra │ [Alt+X] Venta │ [Alt+J] Ajuste │ [Alt+Q] Salir"
        };

        Add(menu, panelIzq, panelDer, barraAtajos);

        _ = Task.Run(async () => await CargarProductos());
    }

    protected override bool OnKeyDown(Key key)
    {
        switch (key)
        {
            case var k when k == Key.A.WithCtrl:
                _ = MostrarDialogoAgregar(); return true;
            case var k when k == Key.E.WithCtrl:
                _ = MostrarDialogoEditar(); return true;
            case var k when k == Key.D.WithCtrl:
                _ = EliminarProducto(); return true;
            case var k when k == Key.C.WithAlt:
                _ = RegistrarMovimiento("Compra"); return true;
            case var k when k == Key.X.WithAlt:
                _ = RegistrarMovimiento("Venta"); return true;
            case var k when k == Key.J.WithAlt:
                _ = RegistrarMovimiento("Ajuste"); return true;
            case var k when k == Key.Q.WithAlt:
                App!.RequestStop(); return true;
            default:
                return base.OnKeyDown(key);
        }
    }

    private async Task CargarProductos()
    {
        try
        {
            var resp = await http.GetStringAsync("/productos");
            productos = JsonSerializer.Deserialize<List<Producto>>(resp, jsonOpts) ?? new();
            productosFiltrados = new(productos);
            ActualizarLista();
        }
        catch (Exception ex)
        {
            App!.Invoke(() => {
                MessageBox.Query(App!, "Error de Conexión", $"No se pudieron cargar los productos:\n{ex.Message}", "OK");
            });
        }
    }

    private void BuscarProductos()
    {
        var texto = campoBusqueda.Text?.ToString()?.ToLower() ?? "";
        productosFiltrados = string.IsNullOrWhiteSpace(texto)
            ? new(productos)
            : productos.Where(p =>
                p.Codigo.ToLower().Contains(texto) ||
                p.Nombre.ToLower().Contains(texto)).ToList();
        ActualizarLista();
    }

    private void ActualizarLista()
    {
        var items = productosFiltrados
            .Select(p => $"{p.Codigo,-10} {p.Nombre,-20} ${p.Precio,8:F2}  Stock:{p.Stock}")
            .ToList();
        
        App!.Invoke(() => {
            listaProductos.SetSource(new ObservableCollection<string>(items));
        });
    }

    private async Task CargarMovimientos()
    {
        if (productoSeleccionado == null) return;
        try
        {
            var resp = await http.GetStringAsync($"/productos/{productoSeleccionado.Id}/movimientos");
            var movs = JsonSerializer.Deserialize<List<MovimientoDeProducto>>(resp, jsonOpts) ?? new();
            var items = movs.Select(m =>
            {
                var t = m.Tipo switch { 0 => "Compra", 1 => "Venta", 2 => "Ajuste", _ => "?" };
                return $"{m.Fecha:dd/MM/yy HH:mm}  {t,-8}  {m.Cantidad,6}";
            }).ToList();
            
            App!.Invoke(() => {
                listaMovimientos.SetSource(new ObservableCollection<string>(items));
            });
        }
        catch (Exception ex)
        {
            App!.Invoke(() => {
                MessageBox.Query(App!, "Error", $"Error al cargar movimientos:\n{ex.Message}", "OK");
            });
        }
    }

    private async Task MostrarDialogoAgregar() { await Task.CompletedTask; }
    private async Task MostrarDialogoEditar() { await Task.CompletedTask; }
    private async Task RegistrarMovimiento(string tipo) { await Task.CompletedTask; }

    private async Task EliminarProducto()
    {
        if (productoSeleccionado == null)
        {
            MessageBox.Query(App!, "Eliminar", "Seleccioná un producto primero.", "OK");
            return;
        }

        int? r = MessageBox.Query(App!, "Eliminar", $"¿Eliminar \"{productoSeleccionado.Nombre}\"?", "No", "Sí");

        if (r == 1)
        {
            try
            {
                var response = await http.DeleteAsync($"/productos/{productoSeleccionado.Id}");
                if (response.IsSuccessStatusCode)
                {
                    productoSeleccionado = null;
                    await CargarProductos();
                    App!.Invoke(() => {
                        listaMovimientos.SetSource(new ObservableCollection<string>());
                    });
                }
                else
                {
                    App!.Invoke(() => {
                        MessageBox.Query(App!, "Error", $"El servidor no pudo eliminar el producto (Código: {response.StatusCode})", "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                App!.Invoke(() => {
                    MessageBox.Query(App!, "Error de Red", $"No se pudo eliminar el producto:\n{ex.Message}", "OK");
                });
            }
        }
    }
}


class Producto
{
    public int     Id     { get; set; }
    public string  Codigo { get; set; } = "";
    public string  Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int     Stock  { get; set; }
}

class MovimientoDeProducto
{
    public int      Id         { get; set; }
    public int      ProductoId { get; set; }
    public int      Tipo       { get; set; }
    public int      Cantidad   { get; set; }
    public DateTime Fecha      { get; set; }
}