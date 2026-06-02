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

    private async Task MostrarDialogoAgregar()
    {
        using DialogProducto dialog = new("Agregar producto", null);
        App!.Run(dialog);

        if (!dialog.Guardado || dialog.Resultado == null) return;

        await CargarProductos();

        bool codigoExiste = productos.Any(p => p.Codigo.Trim().Equals(dialog.Resultado.Codigo.Trim(), StringComparison.OrdinalIgnoreCase));
        if (codigoExiste)
        {
            App!.Invoke(() => {
                MessageBox.Query(App!, "Código Duplicado", $"PROHIBIDO: Ya existe un producto con el código '{dialog.Resultado.Codigo.Trim()}'.", "OK");
            });
            return;
        }

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(dialog.Resultado, jsonOpts), Encoding.UTF8, "application/json");
            var response = await http.PostAsync("/productos", content);
            
            if (response.IsSuccessStatusCode)
            {
                await CargarProductos();
            }
            else
            {
                App!.Invoke(() => {
                    MessageBox.Query(App!, "Error", $"El servidor rechazó el producto (Código: {response.StatusCode})", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            App!.Invoke(() => {
                MessageBox.Query(App!, "Error de Red", $"No se pudo enviar el producto:\n{ex.Message}", "OK");
            });
        }
    }

    private async Task MostrarDialogoEditar()
    {
        if (productoSeleccionado == null)
        {
            MessageBox.Query(App!, "Editar", "Seleccioná un producto primero.", "OK");
            return;
        }

        using DialogProducto dialog = new("Editar producto", productoSeleccionado);
        App!.Run(dialog);

        if (!dialog.Guardado || dialog.Resultado == null) return;

        await CargarProductos();

        bool codigoExisteEnOtro = productos.Any(p => p.Id != productoSeleccionado.Id && p.Codigo.Trim().Equals(dialog.Resultado.Codigo.Trim(), StringComparison.OrdinalIgnoreCase));
        if (codigoExisteEnOtro)
        {
            App!.Invoke(() => {
                MessageBox.Query(App!, "Código Duplicado", $"PROHIBIDO: No podés usar el código '{dialog.Resultado.Codigo.Trim()}' because pertenece a otro artículo.", "OK");
            });
            return;
        }

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(dialog.Resultado, jsonOpts), Encoding.UTF8, "application/json");
            var response = await http.PutAsync($"/productos/{productoSeleccionado.Id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                await CargarProductos();
            }
            else
            {
                App!.Invoke(() => {
                    MessageBox.Query(App!, "Error", $"El servidor rechazó la edición (Código: {response.StatusCode})", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            App!.Invoke(() => {
                MessageBox.Query(App!, "Error de Red", $"No se pudieron guardar los cambios:\n{ex.Message}", "OK");
            });
        }
    }

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

    private async Task RegistrarMovimiento(string tipo)
    {
        if (productoSeleccionado == null)
        {
            MessageBox.Query(App!, "Movimiento", "Seleccioná un producto primero.", "OK");
            return;
        }

        using DialogMovimiento dialog = new(tipo, productoSeleccionado.Nombre);
        App!.Run(dialog);

        if (!dialog.Registrado) return;

        try
        {
            var mov = new { tipo = tipo, cantidad = dialog.Cantidad };
            var content = new StringContent(
                JsonSerializer.Serialize(mov, jsonOpts), Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"/productos/{productoSeleccionado.Id}/movimientos", content);
            
            if (response.IsSuccessStatusCode)
            {
                await CargarProductos();
                await CargarMovimientos();
            }
            else
            {
                App!.Invoke(() => {
                    MessageBox.Query(App!, "Error", $"El servidor rechazó el movimiento (Código: {response.StatusCode})", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            App!.Invoke(() => {
                MessageBox.Query(App!, "Error de Red", $"No se pudo registrar el movimiento:\n{ex.Message}", "OK");
            });
        }
    }
}

class DialogProducto : Dialog
{
    private readonly TextField txtCodigo;
    private readonly TextField txtNombre;
    private readonly TextField txtPrecio;
    private readonly TextField txtStock;

    public bool      Guardado   { get; private set; }
    public Producto? Resultado  { get; private set; }

    public DialogProducto(string titulo, Producto? producto)
    {
        Title  = titulo;
        Width  = 52;
        Height = 16;

        Label lblCodigo = new() { Text = "Código:",  X = 1, Y = 1 };
        txtCodigo = new() { X = 12, Y = 1, Width = 30, Text = producto?.Codigo ?? "" };

        Label lblNombre = new() { Text = "Nombre:",  X = 1, Y = 3 };
        txtNombre = new() { X = 12, Y = 3, Width = 30, Text = producto?.Nombre ?? "" };

        Label lblPrecio = new() { Text = "Precio:",  X = 1, Y = 5 };
        txtPrecio = new() { X = 12, Y = 5, Width = 15, Text = producto?.Precio.ToString() ?? "0" };

        Label lblStock  = new() { Text = "Stock:",   X = 1, Y = 7 };
        txtStock  = new() { X = 12, Y = 7, Width = 10, Text = producto?.Stock.ToString()  ?? "0" };

        Button btnCancelar = new() { Title = "Cancelar" };
        btnCancelar.Accepting += (_, e) =>
        {
            Guardado = false;
            RequestStop(); 
        };

        Button btnGuardar = new() { Title = "Guardar", IsDefault = true };
        btnGuardar.Accepting += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text?.ToString()))
            {
                txtCodigo.SetFocus();
                e.Handled = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(txtNombre.Text?.ToString()))
            {
                txtNombre.SetFocus();
                e.Handled = true;
                return;
            }

            Resultado = new Producto
            {
                Id     = producto?.Id ?? 0,
                Codigo = txtCodigo.Text?.ToString() ?? "",
                Nombre = txtNombre.Text?.ToString() ?? "",
                Precio = decimal.TryParse(txtPrecio.Text?.ToString(), out var p) ? p : 0,
                Stock  = int.TryParse(txtStock.Text?.ToString(),  out var s) ? s : 0,
            };
            Guardado = true;
            RequestStop(); 
        };

        Add(lblCodigo, txtCodigo, lblNombre, txtNombre, lblPrecio, txtPrecio, lblStock, txtStock);
        AddButton(btnCancelar);
        AddButton(btnGuardar);
    }
}

class DialogMovimiento : Dialog
{
    private readonly TextField txtCantidad;

    public bool Registrado { get; private set; }
    public int  Cantidad   { get; private set; }

    public DialogMovimiento(string tipo, string nombreProducto)
    {
        Title  = $"{tipo} — {nombreProducto}";
        Width  = 46;
        Height = 10;

        Label lblCantidad = new() { Text = "Cantidad:", X = 1, Y = 1 };
        txtCantidad = new() { Text = "0", X = 12, Y = 1, Width = 15 };

        Button btnCancelar = new() { Title = "Cancelar" };
        btnCancelar.Accepting += (_, e) =>
        {
            Registrado = false;
            RequestStop(); 
        };

        Button btnRegistrar = new() { Title = "Registrar", IsDefault = true };
        btnRegistrar.Accepting += (_, e) =>
        {
            Cantidad   = int.TryParse(txtCantidad.Text?.ToString(), out var c) ? c : 0;
            Registrado = true;
            RequestStop(); 
        };

        Add(lblCantidad, txtCantidad);
        AddButton(btnCancelar);
        AddButton(btnRegistrar);
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