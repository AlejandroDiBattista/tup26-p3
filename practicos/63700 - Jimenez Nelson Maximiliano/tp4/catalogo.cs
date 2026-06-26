using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Terminal.Gui;

class Program
{
    static readonly HttpClient http = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
    
    static List<Producto> listaProductos = new List<Producto>();
    static ListView lvProductos = new ListView();
    static ListView lvMovimientos = new ListView();
    static TextField txtBuscar = new TextField();
    
    static Producto? productoSeleccionado = null;

    static void Main()
    {
        Application.Init();

    
        var top = new Toplevel();
        
        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem("_Archivo", new MenuItem[] {
                new MenuItem("_Salir", "Cierra la aplicación", () => Application.RequestStop())
            }),
            new MenuBarItem("_Productos", new MenuItem[] {
                new MenuItem("_Agregar", "", () => MostrarDialogoProducto(null)),
                new MenuItem("_Editar", "", () => { if(productoSeleccionado != null) MostrarDialogoProducto(productoSeleccionado); }),
                new MenuItem("E_liminar", "", () => EliminarProducto())
            }),
            new MenuBarItem("_Movimientos", new MenuItem[] {
                new MenuItem("_Registrar Movimiento", "", () => RegistrarMovimiento())
            })
        });

        var win = new Window("Catálogo de Productos REST") {
            X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill()
        };

        
        var panelIzq = new FrameView("Productos (Maestro)") {
            X = 0, Y = 0, Width = Dim.Percent(50), Height = Dim.Fill()
        };
        var panelDer = new FrameView("Movimientos de Stock (Detalle)") {
            X = Pos.Right(panelIzq), Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
        };

    
        var lblBuscar = new Label("Buscar:") { X = 0, Y = 0 };
        txtBuscar = new TextField("") { X = Pos.Right(lblBuscar) + 1, Y = 0, Width = Dim.Fill() };
        txtBuscar.TextChanged += (e) => FiltrarProductos();

        lvProductos = new ListView() { X = 0, Y = Pos.Bottom(lblBuscar) + 1, Width = Dim.Fill(), Height = Dim.Fill() };
        lvProductos.SelectedItemChanged += (e) => 
        {
            if (listaProductos.Any()) {
                productoSeleccionado = listaProductos[e.Item];
                CargarMovimientos();
            }
        };

        panelIzq.Add(lblBuscar, txtBuscar, lvProductos);
        
    
        lvMovimientos = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        panelDer.Add(lvMovimientos);

        win.Add(panelIzq, panelDer);
        top.Add(menu, win);

    
        _ = CargarProductosAsync();

        Application.Run(top);
        Application.Shutdown();
    }


    static async Task CargarProductosAsync()
    {
        try {
            listaProductos = await http.GetFromJsonAsync<List<Producto>>("productos") ?? new List<Producto>();
            Application.MainLoop.Invoke(FiltrarProductos);
        } catch {
            Application.MainLoop.Invoke(() => MessageBox.ErrorQuery("Error", "No se pudo conectar al servidor. ¿Está corriendo en el puerto 5000?", "Ok"));
        }
    }

    static void FiltrarProductos()
    {
        var busqueda = txtBuscar.Text?.ToString()?.ToLower() ?? "";
        var filtrados = listaProductos
            .Where(p => p.Nombre.ToLower().Contains(busqueda) || p.Codigo.ToLower().Contains(busqueda))
            .ToList();
            
        listaProductos = filtrados.Count > 0 ? filtrados : listaProductos;
        
        lvProductos.SetSource(listaProductos.Select(p => $"[{p.Codigo}] {p.Nombre} | ${p.Precio} | Stock: {p.Stock}").ToList());
        lvProductos.SetNeedsDisplay();
    }

    static async void CargarMovimientos()
    {
        if (productoSeleccionado == null) return;
        try {
            var movs = await http.GetFromJsonAsync<List<MovimientoDeProducto>>($"productos/{productoSeleccionado.Id}/movimientos");
            Application.MainLoop.Invoke(() => {
                if (movs != null) {
                    lvMovimientos.SetSource(movs.Select(m => $"{m.Fecha:dd/MM/yyyy HH:mm} | {m.Tipo.ToString().ToUpper()} | Cant: {m.Cantidad}").ToList());
                    lvMovimientos.SetNeedsDisplay();
                }
            });
        } catch { }
    }



    static void MostrarDialogoProducto(Producto? prod)
    {
        var esNuevo = prod == null;
        var dialog = new Dialog(esNuevo ? "Agregar Producto" : "Editar Producto", 50, 15);

        var lblCodigo = new Label("Código:") { X = 1, Y = 1 };
        var txtCodigo = new TextField(prod?.Codigo ?? "") { X = Pos.Right(lblCodigo) + 1, Y = 1, Width = Dim.Fill() - 1 };
        
        var lblNombre = new Label("Nombre:") { X = 1, Y = 3 };
        var txtNombre = new TextField(prod?.Nombre ?? "") { X = Pos.Right(lblNombre) + 1, Y = 3, Width = Dim.Fill() - 1 };
        
        var lblPrecio = new Label("Precio:") { X = 1, Y = 5 };
        var txtPrecio = new TextField(prod?.Precio.ToString() ?? "0") { X = Pos.Right(lblPrecio) + 1, Y = 5, Width = Dim.Fill() - 1 };

        var btnGuardar = new Button("Guardar", is_default: true);
        btnGuardar.Clicked += async () => {
            var nuevoProd = new Producto {
                Id = prod?.Id ?? 0,
                Codigo = txtCodigo.Text.ToString()!,
                Nombre = txtNombre.Text.ToString()!,
                Precio = decimal.TryParse(txtPrecio.Text.ToString(), out var p) ? p : 0
            };

            try {
                if (esNuevo) await http.PostAsJsonAsync("productos", nuevoProd);
                else await http.PutAsJsonAsync($"productos/{prod!.Id}", nuevoProd);
                
                Application.RequestStop();
                await CargarProductosAsync(); // Refrescar grilla
            } catch { MessageBox.ErrorQuery("Error", "Fallo al guardar.", "Ok"); }
        };

        var btnCancelar = new Button("Cancelar");
        btnCancelar.Clicked += () => Application.RequestStop();

        dialog.Add(lblCodigo, txtCodigo, lblNombre, txtNombre, lblPrecio, txtPrecio);
        dialog.AddButton(btnGuardar);
        dialog.AddButton(btnCancelar);

        Application.Run(dialog);
    }

    static async void EliminarProducto()
    {
        if (productoSeleccionado == null) return;
        var result = MessageBox.Query("Eliminar", $"¿Seguro de eliminar {productoSeleccionado.Nombre}?", "No", "Sí");
        if (result == 1) {
            await http.DeleteAsync($"productos/{productoSeleccionado.Id}");
            productoSeleccionado = null;
            await CargarProductosAsync();
            lvMovimientos.SetSource(new List<string>());
        }
    }

    static void RegistrarMovimiento()
    {
        if (productoSeleccionado == null) {
            MessageBox.ErrorQuery("Error", "Debe seleccionar un producto primero.", "Ok");
            return;
        }

        var dialog = new Dialog("Registrar Movimiento", 40, 12);
        
        var lblTipo = new Label("Tipo:") { X = 1, Y = 1 };
        var radioGroup = new RadioGroup(new ustring[] { "Compra", "Venta", "Ajuste" }) { X = Pos.Right(lblTipo) + 1, Y = 1 };
        
        var lblCant = new Label("Cantidad:") { X = 1, Y = 5 };
        var txtCant = new TextField("1") { X = Pos.Right(lblCant) + 1, Y = 5, Width = Dim.Fill() - 1 };

        var btnGuardar = new Button("Guardar", is_default: true);
        btnGuardar.Clicked += async () => {
            if (int.TryParse(txtCant.Text.ToString(), out int cant)) {
                var mov = new MovimientoDeProducto {
                    Tipo = (TipoMovimiento)radioGroup.SelectedItem,
                    Cantidad = cant
                };
                
                await http.PostAsJsonAsync($"productos/{productoSeleccionado.Id}/movimientos", mov);
                Application.RequestStop();
                await CargarProductosAsync(); // Refresca stock en maestro
                CargarMovimientos(); // Refresca historial en detalle
            }
        };

        var btnCancelar = new Button("Cancelar");
        btnCancelar.Clicked += () => Application.RequestStop();

        dialog.Add(lblTipo, radioGroup, lblCant, txtCant);
        dialog.AddButton(btnGuardar);
        dialog.AddButton(btnCancelar);

        Application.Run(dialog);
    }

    

    public class Producto {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
    }

    public class MovimientoDeProducto {
        public int Id { get; set; }
        public TipoMovimiento Tipo { get; set; }
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; }
    }

    public enum TipoMovimiento { Compra, Venta, Ajuste }
}