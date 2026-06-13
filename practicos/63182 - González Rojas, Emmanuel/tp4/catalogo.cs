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

class ListaAtajos : ListView {
    public Action<Key>? Interceptor { get; set; }

    protected override bool OnKeyDown(Key keyEvent) {
        Interceptor?.Invoke(keyEvent);
        if (keyEvent.Handled) return true;
        return base.OnKeyDown(keyEvent);
    }
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
        
         var lblBuscar = new Label { Text = "Buscar:", X = 1, Y = 0 };
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

        Add(panelMaestro, panelDetalle, lblStatus);

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


    private void SetStatus(string msg) =>
        Application.Invoke(() => lblStatus.Text = $" {msg}");

    private ProductoDto? GetSeleccionado() {
        int idx = listaProductos.SelectedItem;
        return (idx >= 0 && idx < productosFiltrados.Count)
            ? productosFiltrados[idx] : null;
    }

    private void AplicarFiltro() {
        var q = txtBuscar.Text?.Trim() ?? "";
        productosFiltrados = string.IsNullOrEmpty(q)
            ? [.. productos]
            : [.. productos.Where(p =>
                p.Codigo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase))];
        RenderizarProductos();
    }


    private void RenderizarProductos() {
        var items = productosFiltrados
            .Select(p => $" {p.Codigo,-10} {p.Nombre,-22} ${p.Precio,9:N2}  Stk:{p.Stock,5}")
            .ToList();
        listaProductos.SetSource(new ObservableCollection<string>(items));

        if (productosFiltrados.Count == 0) {
            movimientosActuales.Clear();
            lblMovimientos.Text = " Sin resultados.";
            RenderizarMovimientos();
        } else {
            listaProductos.SelectedItem = 0;
            _ = CargarMovimientosAsync(productosFiltrados[0]);
        }
    }

    private void RenderizarMovimientos() {
        if (movimientosActuales.Count == 0) {
            listaMovimientos.SetSource(
                new ObservableCollection<string> { " (sin movimientos)" });
            return;
        }
        var items = movimientosActuales.Select(m => {
            var signo = m.Tipo switch {
                TipoMovimiento.Compra => "+",
                TipoMovimiento.Venta  => "-",
                _                     => "="
            };
            return $" {m.Tipo,-8}  {signo}{m.Cantidad,5}   {m.Fecha:dd/MM/yy HH:mm}";
        }).ToList();
        listaMovimientos.SetSource(new ObservableCollection<string>(items));
    }


    private void ProcesarAtajo(Key e) {
        if (e.Handled) return;

        if (e.KeyCode == KeyCode.Esc) {
            Application.RequestStop();
            e.Handled = true;
            return;
        }
        if (e.KeyCode == KeyCode.F5) {
            _ = RecargarAsync();
            e.Handled = true;
            return;
        }

        if (txtBuscar.HasFocus) return;

        var codigo = e.KeyCode & ~KeyCode.ShiftMask;

        switch (codigo) {
            case KeyCode.F1: MostrarAyuda();
                            e.Handled = true;break;
            case KeyCode.A: case (KeyCode)'a': 
                AgregarProducto(); e.Handled = true; break;
            case KeyCode.B: case (KeyCode)'b': 
                ModificarProducto(); e.Handled = true; break;
            case KeyCode.E: case (KeyCode)'e': 
                EliminarProducto(); e.Handled = true; break;
            case KeyCode.C: case (KeyCode)'c': 
                RegistrarMovimiento(TipoMovimiento.Compra); e.Handled = true; break;
            case KeyCode.V: case (KeyCode)'v': 
                RegistrarMovimiento(TipoMovimiento.Venta); e.Handled = true; break;
            case KeyCode.J: case (KeyCode)'j': 
                RegistrarMovimiento(TipoMovimiento.Ajuste); e.Handled = true; break;
            case KeyCode.M: case (KeyCode)'m': 
                menu.SetFocus(); e.Handled = true; break;
        }
    }

  private async Task RecargarAsync() {
        SetStatus("Recargando...");
        try {
            productos = await api.ObtenerProductosAsync();
            Application.Invoke(() => {
                AplicarFiltro();
                SetStatus($"✓ {productos.Count} producto(s) cargado(s).");
            });
        } catch (Exception ex) {
            SetStatus($"✗ Error: {ex.Message}");
        }
    }

  private async Task CargarMovimientosAsync(ProductoDto producto) {
        Application.Invoke(() =>
            lblMovimientos.Text = $" {producto.Nombre}  (stock: {producto.Stock})");
        try {
            var lista = await api.ObtenerMovimientosAsync(producto.Id);
            movimientosActuales = lista;
            Application.Invoke(RenderizarMovimientos);
        } catch (Exception ex) {
            SetStatus($"✗ Error movimientos: {ex.Message}");
        }
    }


 private static void MostrarError(string mensaje) {
        var dlg = new Dialog { Title = " Atención ", Width = 50, Height = 6 };
        dlg.Add(new Label { Text = mensaje, X = 1, Y = 1 });
        var btn = new Button { Text = "_Aceptar", IsDefault = true };
        btn.Accepting += (_, _) => Application.RequestStop(dlg);
        dlg.AddButton(btn);
        Application.Run(dlg);
    }

private void AgregarProducto() {
        var dlg = new Dialog { Title = " Agregar Producto ", Width = 58, Height = 14 };

        var lblCodigo = new Label     { Text = "Código :", X = 1, Y = 1 };
        var txtCodigo = new TextField { Text = "", X = 12, Y = 1, Width = Dim.Fill(2) };
        var lblNombre = new Label     { Text = "Nombre :", X = 1, Y = 3 };
        var txtNombre = new TextField { Text = "", X = 12, Y = 3, Width = Dim.Fill(2) };
        var lblPrecio = new Label     { Text = "Precio :", X = 1, Y = 5 };
        var txtPrecio = new TextField { Text = "0", X = 12, Y = 5, Width = 20 };
        var lblStock  = new Label     { Text = "Stock  :", X = 1, Y = 7 };
        var txtStock  = new TextField { Text = "0", X = 12, Y = 7, Width = 12 };

        var btnGuardar  = new Button { Text = "_Guardar",  IsDefault = true };
        var btnCancelar = new Button { Text = "_Cancelar" };

        dlg.Add(lblCodigo, txtCodigo, lblNombre, txtNombre,
                lblPrecio, txtPrecio, lblStock,  txtStock);
        dlg.AddButton(btnGuardar);
        dlg.AddButton(btnCancelar);

        btnGuardar.Accepting += (_, _) => {
            var datos = ValidarYLeer(txtCodigo, txtNombre, txtPrecio, txtStock);
            if (datos is null) return;
            _ = Task.Run(async () => {
                try {
                    var (ok, msg) = await api.AgregarProductoAsync(datos);
                    Application.Invoke(() => {
                        Application.RequestStop(dlg);
                        SetStatus(ok ? "✓ Producto agregado." : $"✗ {msg}");
                        if (ok) _ = RecargarAsync();
                    });
                } catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
            });
        };
        btnCancelar.Accepting += (_, _) => Application.RequestStop(dlg);

        Application.Run(dlg);
    }


    private void ModificarProducto() {
        var producto = GetSeleccionado();
        if (producto is null) {
            MostrarError("Seleccioná un producto primero.");
            return;
        }

        var dlg = new Dialog { Title = $" Modificar: {producto.Nombre} ", Width = 58, Height = 14 };

        var lblCodigo = new Label     { Text = "Código :", X = 1, Y = 1 };
        var txtCodigo = new TextField { Text = producto.Codigo, X = 12, Y = 1, Width = Dim.Fill(2) };
        var lblNombre = new Label     { Text = "Nombre :", X = 1, Y = 3 };
        var txtNombre = new TextField { Text = producto.Nombre, X = 12, Y = 3, Width = Dim.Fill(2) };
        var lblPrecio = new Label     { Text = "Precio :", X = 1, Y = 5 };
        var txtPrecio = new TextField {
            Text = producto.Precio.ToString(System.Globalization.CultureInfo.InvariantCulture),
            X = 12, Y = 5, Width = 20
        };
        var lblStock  = new Label     { Text = "Stock  :", X = 1, Y = 7 };
        var txtStock  = new TextField { Text = producto.Stock.ToString(), X = 12, Y = 7, Width = 12 };

        var btnGuardar  = new Button { Text = "_Guardar",  IsDefault = true };
        var btnCancelar = new Button { Text = "_Cancelar" };

        dlg.Add(lblCodigo, txtCodigo, lblNombre, txtNombre,
                lblPrecio, txtPrecio, lblStock,  txtStock);
        dlg.AddButton(btnGuardar);
        dlg.AddButton(btnCancelar);

        btnGuardar.Accepting += (_, _) => {
            var datos = ValidarYLeer(txtCodigo, txtNombre, txtPrecio, txtStock);
            if (datos is null) return;
            datos.Id = producto.Id;
            _ = Task.Run(async () => {
                try {
                    var (ok, msg) = await api.ModificarProductoAsync(datos);
                    Application.Invoke(() => {
                        Application.RequestStop(dlg);
                        SetStatus(ok ? "✓ Producto modificado." : $"✗ {msg}");
                        if (ok) _ = RecargarAsync();
                    });
                } catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
            });
        };
        btnCancelar.Accepting += (_, _) => Application.RequestStop(dlg);

        Application.Run(dlg);
    }
private void EliminarProducto() {
        var producto = GetSeleccionado();
        if (producto is null) {
            var dlgError = new Dialog { Title = " Atención ", Width = 50, Height = 6 };
            dlgError.Add(new Label { Text = "Seleccioná un producto primero.", X = 1, Y = 1 });
            var btnOk = new Button { Text = "_Aceptar", IsDefault = true };
            btnOk.Accepting += (_, _) => Application.RequestStop(dlgError);
            dlgError.AddButton(btnOk);
            Application.Run(dlgError);
            return;
        }

        var dlg = new Dialog { Title = " Confirmar eliminación ", Width = 55, Height = 8 };
        var lblInfo = new Label {
            Text = $"¿Eliminar '{producto.Nombre}'?\nSe borrarán también sus movimientos.",
            X = 1, Y = 1
        };
        
        var btnCancelar = new Button { Text = "_Cancelar", IsDefault = true };
        var btnEliminar = new Button { Text = "_Eliminar",};

        dlg.Add(lblInfo);
        dlg.AddButton(btnCancelar);
        dlg.AddButton(btnEliminar); 

        
        btnEliminar.Accepting += (_, _) => {
            _ = Task.Run(async () => {
                try {
                    var (ok, msg) = await api.EliminarProductoAsync(producto.Id);
                    Application.Invoke(() => {
                        Application.RequestStop(dlg);
                        SetStatus(ok ? "✓ Producto eliminado." : $"✗ {msg}");
                        if (ok) {
                            movimientosActuales.Clear();
                            lblMovimientos.Text = " Seleccioná un producto";
                            RenderizarMovimientos();
                            _ = RecargarAsync();
                        }
                    });
                } catch (Exception ex) {
                    Application.Invoke(() => {
                        Application.RequestStop(dlg);
                        SetStatus($"✗ {ex.Message}");
                    });
                }
            });
        };
        
        btnCancelar.Accepting += (_, _) => Application.RequestStop(dlg);

        Application.Run(dlg);
    }

    
private void RegistrarMovimiento(TipoMovimiento tipo) {
        var producto = GetSeleccionado();
        if (producto is null) {
            MostrarError("Seleccioná un producto primero.");
            return;
        }

        string titulo = tipo switch {
            TipoMovimiento.Compra => "Registrar Compra (+stock)",
            TipoMovimiento.Venta  => "Registrar Venta  (-stock)",
            _                     => "Ajuste de Stock  (=stock)"
        };
        string etiqueta = tipo switch {
            TipoMovimiento.Compra => "Cantidad a sumar  :",
            TipoMovimiento.Venta  => "Cantidad a restar :",
            _                     => "Nuevo valor stock :"
        };

        var dlg     = new Dialog { Title = $" {titulo} ", Width = 58, Height = 10 };
        var lblInfo = new Label  {
            Text = $" {producto.Codigo} — {producto.Nombre}  (stock: {producto.Stock})",
            X = 1, Y = 1
        };
        var lblCant = new Label   { Text = etiqueta, X = 1, Y = 3 };
        var txtCant = new TextField { Text = "", X = Pos.Right(lblCant) + 1, Y = 3, Width = 10 };

        var btnOk    = new Button { Text = "_Aceptar",  IsDefault = true };
        var btnCance = new Button { Text = "_Cancelar" };

        dlg.Add(lblInfo, lblCant, txtCant);
        dlg.AddButton(btnOk);
        dlg.AddButton(btnCance);

        btnOk.Accepting += (_, _) => {
            if (!int.TryParse(txtCant.Text?.Trim(), out int cantidad) || cantidad <= 0) {
                MostrarError("Ingresá un número entero positivo.");
                return;
            }
            var mov = new MovimientoDto { ProductoId = producto.Id, Tipo = tipo, Cantidad = cantidad };
            _ = Task.Run(async () => {
                try {
                    var (ok, msg) = await api.RegistrarMovimientoAsync(producto.Id, mov);
                    Application.Invoke(() => {
                        Application.RequestStop(dlg);
                        SetStatus(ok ? "✓ Movimiento registrado." : $"✗ {msg}");
                        if (ok) _ = Task.Run(async () => {
                            await RecargarAsync();
                            var act = productos.FirstOrDefault(p => p.Id == producto.Id);
                            if (act is not null) await CargarMovimientosAsync(act);
                        });
                    });
                } catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
            });
        };
        btnCance.Accepting += (_, _) => Application.RequestStop(dlg);

        Application.Run(dlg);
    }

private static ProductoDto? ValidarYLeer(
        TextField txtCodigo, TextField txtNombre,
        TextField txtPrecio, TextField txtStock) {

        var codigo = txtCodigo.Text?.Trim() ?? "";
        var nombre = txtNombre.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(codigo)) {
            MostrarError("El código no puede estar vacío.");
            return null;
        }
        if (string.IsNullOrEmpty(nombre)) {
            MostrarError("El nombre no puede estar vacío.");
            return null;
        }
        if (!decimal.TryParse(
                txtPrecio.Text?.Trim().Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal precio) || precio < 0) {
            MostrarError("El precio debe ser un decimal no negativo.");
            return null;
        }
        if (!int.TryParse(txtStock.Text?.Trim(), out int stock) || stock < 0) {
            MostrarError("El stock debe ser un entero no negativo.");
            return null;
        }
        return new ProductoDto { Codigo = codigo, Nombre = nombre, Precio = precio, Stock = stock };
    }
    
}