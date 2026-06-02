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

    static void CargarMovimientos() 
    {
        if (productoSeleccionado == null) return;

        try {
            var jsonOpciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var respuesta = http.GetFromJsonAsync<List<MovimientoDeProducto>>($"/productos/{productoSeleccionado.Id}/movimientos", jsonOpciones).Result;
            
            listaMovimientos = respuesta ?? new List<MovimientoDeProducto>();
            listMovimientosUI.SetSource(new ObservableCollection<MovimientoDeProducto>(listaMovimientos));
        } catch {
            listaMovimientos.Clear();
            listMovimientosUI.SetSource(new ObservableCollection<MovimientoDeProducto>(listaMovimientos));
        }
    }

    static void FiltrarProductos() 
    {
        var filtro = txtBuscar.Text.ToString().ToLower();
        var filtrados = listaProductos.Where(p => 
            p.Nombre.ToLower().Contains(filtro) || p.Codigo.ToLower().Contains(filtro)
        ).ToList();

        listProductosUI.SetSource(new ObservableCollection<Producto>(filtrados));
    }
    static void DialogoNuevoProducto() 
    {
        var dialog = new Dialog { Title = "Nuevo Producto", Width = 50, Height = 12 };
        
        var txtCodigo = new TextField { Text = "", X = 10, Y = 1, Width = 30 };
        var txtNombre = new TextField { Text = "", X = 10, Y = 3, Width = 30 };
        var txtPrecio = new TextField { Text = "", X = 10, Y = 5, Width = 30 };

        dialog.Add(new Label { Text = "Código:", X = 1, Y = 1 }, txtCodigo);
        dialog.Add(new Label { Text = "Nombre:", X = 1, Y = 3 }, txtNombre);
        dialog.Add(new Label { Text = "Precio:", X = 1, Y = 5 }, txtPrecio);

        var btnGuardar = new Button { Text = "Guardar", IsDefault = true };
        btnGuardar.Accepting += (s, e) => {
            var nuevo = new Producto {
                Codigo = txtCodigo.Text.ToString(),
                Nombre = txtNombre.Text.ToString(),
                Precio = decimal.TryParse(txtPrecio.Text.ToString(), out var p) ? p : 0,
                Stock = 0
            };

            var res = http.PostAsJsonAsync("/productos", nuevo).Result;
            if (res.IsSuccessStatusCode) {
                Application.RequestStop();
                CargarProductos();
            } else {
                MessageBox.ErrorQuery("Error", "No se pudo guardar el producto", "OK");
            }
        };

        var btnCancelar = new Button { Text = "Cancelar" };
        btnCancelar.Accepting += (s, e) => Application.RequestStop();

        dialog.AddButton(btnGuardar);
        dialog.AddButton(btnCancelar);

        Application.Run(dialog);
    }
    static void DialogoEditarProducto() 
    {
        if (productoSeleccionado == null) return;

        var dialog = new Dialog { Title = "Editar Producto", Width = 50, Height = 12 };
        
        var txtCodigo = new TextField { Text = productoSeleccionado.Codigo, X = 10, Y = 1, Width = 30 };
        var txtNombre = new TextField { Text = productoSeleccionado.Nombre, X = 10, Y = 3, Width = 30 };
        var txtPrecio = new TextField { Text = productoSeleccionado.Precio.ToString(), X = 10, Y = 5, Width = 30 };

        dialog.Add(new Label { Text = "Código:", X = 1, Y = 1 }, txtCodigo);
        dialog.Add(new Label { Text = "Nombre:", X = 1, Y = 3 }, txtNombre);
        dialog.Add(new Label { Text = "Precio:", X = 1, Y = 5 }, txtPrecio);

        var btnGuardar = new Button { Text = "Actualizar", IsDefault = true };
        btnGuardar.Accepting += (s, e) => {
            productoSeleccionado.Codigo = txtCodigo.Text.ToString();
            productoSeleccionado.Nombre = txtNombre.Text.ToString();
            productoSeleccionado.Precio = decimal.TryParse(txtPrecio.Text.ToString(), out var p) ? p : 0;

            var res = http.PutAsJsonAsync($"/productos/{productoSeleccionado.Id}", productoSeleccionado).Result;
            if (res.IsSuccessStatusCode) {
                Application.RequestStop();
                CargarProductos();
            }
        };

        var btnCancelar = new Button { Text = "Cancelar" };
        btnCancelar.Accepting += (s, e) => Application.RequestStop();

        dialog.AddButton(btnGuardar);
        dialog.AddButton(btnCancelar);

        Application.Run(dialog);
    }

    static void EliminarProducto() 
    {
        if (productoSeleccionado == null) return;

        var resp = MessageBox.Query("Confirmar", $"¿Seguro querés eliminar {productoSeleccionado.Nombre}?", "Sí", "No");
        if (resp == 0) {
            var res = http.DeleteAsync($"/productos/{productoSeleccionado.Id}").Result;
            if (res.IsSuccessStatusCode) {
                productoSeleccionado = null;
                CargarProductos();
                listaMovimientos.Clear();
            listMovimientosUI.SetSource(new ObservableCollection<MovimientoDeProducto>(listaMovimientos));
            }
        }
    }
    static void DialogoRegistrarMovimiento() 
    {
        if (productoSeleccionado == null) {
            MessageBox.ErrorQuery("Aviso", "Primero seleccioná un producto de la lista", "OK");
            return;
        }

        var dialog = new Dialog { Title = "Registrar Movimiento", Width = 40, Height = 12 };

        var radioTipo = new RadioGroup { RadioLabels = new string[] { "Compra (Sumar)", "Venta (Restar)", "Ajuste (Fijar)" }, X = 10, Y = 1 };
        var txtCantidad = new TextField { Text = "", X = 10, Y = 5, Width = 20 };

        dialog.Add(new Label { Text = "Tipo:", X = 1, Y = 1 }, radioTipo);
        dialog.Add(new Label { Text = "Cantidad:", X = 1, Y = 5 }, txtCantidad);

        var btnGuardar = new Button { Text = "Registrar", IsDefault = true };
        btnGuardar.Accepting += (s, e) => {
            if (int.TryParse(txtCantidad.Text.ToString(), out int cant)) {
                var tipoSeleccionado = (TipoMovimiento)radioTipo.SelectedItem;
                
                var mov = new MovimientoDeProducto {
                    Tipo = tipoSeleccionado,
                    Cantidad = cant
                };

                var res = http.PostAsJsonAsync($"/productos/{productoSeleccionado.Id}/movimientos", mov).Result;
                if (res.IsSuccessStatusCode) {
                    Application.RequestStop();
                    CargarProductos(); 
                    CargarMovimientos();
                }
            } else {
                MessageBox.ErrorQuery("Error", "La cantidad debe ser un número entero", "OK");
            }
        };

        var btnCancelar = new Button { Text = "Cancelar" };
        btnCancelar.Accepting += (s, e) => Application.RequestStop();

        dialog.AddButton(btnGuardar);
        dialog.AddButton(btnCancelar);

        Application.Run(dialog);
    }
}