#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


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

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Salir", "Ctrl+Q", ()=> App!.RequestStop()),
                ]),
                new MenuBarItem("_Productos", [
                    new MenuItem("_Nuevo",   "F2",     NuevoProducto),
                    new MenuItem("_Editar",  "F3",     EditarProducto),
                    new MenuItem("_Eliminar","Del",  EliminarProducto),
                ]),
                new MenuBarItem("_Movimientos",[
                    new MenuItem("_Registrar", "F5", RegistrarMovimiento),
                ])
            ]
        };

        Label searchLabel = new() { Text = "Buscar:", X=1, Y=1 };
        searchField = new TextField() {X=10, Y=1, Width=40, Text=""};
        searchField.TextChanged += (_, _)=> AplicarFiltro();

    }

    private async Task CargarProductos() {
        try {
            var lista = await http.GetFromJsonAsync<List<ProductoDto>>("/productos");
            productos = lista ?? [];
            AplicarFiltro();
        } catch {
            SetStatus("Error no se pudo conectar con el servidor");
        }
    }

    private void AplicarFiltro() {
        string search = searchField?.Text.ToString()?.ToLower() ?? "";
        filtrados = productos.Where(p=> p.Codigo.ToLower().Contains(search) || p.Nombre.ToLower().Contains(search)).ToList();
        var items = new ObservableCollection<string>(filtrados.Select(p=> $"{p.Codigo, -10} - {p.Nombre, -20} ${p.Precio,8:N2}  [{p.Stock}]"));
        listaProductos.SetSource<string>(items);
        CargarMovimientosDelSeleccionado();
    }

    private void CargarMovimientosDelSeleccionado() {
        var p = GetSelected();
        if (p is null) {
            listaMovimientos.SetSource<string>(new ObservableCollection<string>());
            return;
        }
        Task.Run(async ()=> {
            try {
                var movs = await http.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{p.Id}/movimientos");
                ObservableCollection<string> items;
                if (movs is null || movs.Count == 0) {
                    items = new ObservableCollection<string>(["No hay movimientos registrados"]);
                } else {
                    items = new ObservableCollection<string>(movs.Select(m=> $"{m.Tipo,-8} {m.Cantidad,6}  {m.Fecha:dd/MM HH:mm}" ));
                }
                listaMovimientos.SetSource<string>(items);
            }
            catch{}
        });
    }
    private ProductoDto? GetSelected() {
        if (filtrados.Count == 0) return null;
        int index = listaProductos.SelectedItem.HasValue ? listaProductos.SelectedItem.Value : 0;
        if (index < 0 || index >= filtrados.Count) return null;
        return filtrados[index];
    }

    private void NuevoProducto() {
        var dialog = new ProductoDialog(new ProductoDto());
        App!.Run(dialog);
        if(!dialog.Accepted) return;
        Task.Run(async ()=> {
            try {
                var resp = await http.PostAsJsonAsync("/productos", dialog.Producto);
                if (resp.IsSuccessStatusCode) {
                    await CargarProductos();
                    SetStatus("Producto agregado");
                } else {
                    var msg = await resp.Content.ReadAsStringAsync();
                    SetStatus("Error al agregar, ya existe un producto con ese código");
                }
            } catch {
                SetStatus("Error al conectar con el servidor");
            }
        });
    }

    private void EditarProducto() {
        var selected = GetSelected();
        if (selected is null) return;
        var dialog = new ProductoDialog(selected.Clone());
        App!.Run(dialog);
        if(!dialog.Accepted) return;
        Task.Run(async () => {
            try {
                var resp = await http.PutAsJsonAsync($"/productos/{selected.Id}", dialog.Producto);
                if (resp.IsSuccessStatusCode) {
                    await CargarProductos();
                    SetStatus("Producto actualizado");
                } else {
                    SetStatus("Error al actualizar, ya existe otro producto con ese código");
                }
            } catch {
                SetStatus("Error al conectar con el servidor");
            }
        });
    }

    private void EliminarProducto() {
        var selected = GetSelected();
        if (selected is null) return;
        int result = MessageBox.Query(App!,"Confirmar", $"¿Confirma que desea eliminar el producto '{selected.Nombre}'?", "Sí", "No") ?? 0;
        if (result != 0) return;
        Task.Run(async () => {
            try {
                var resp = await http.DeleteAsync($"/productos/{selected.Id}");
                if (resp.IsSuccessStatusCode) {
                    await CargarProductos();
                    SetStatus("Producto eliminado");
                } else {
                    SetStatus("Error al eliminar el producto");
                }
            } catch {
                SetStatus("Error al conectar con el servidor");
            }
        });
    }

    private void RegistrarMovimiento() {
        var selected = GetSelected();
        if (selected is null) {
            MessageBox.ErrorQuery(App!, "Error", "No hay ningún producto seleccionado", "OK");
            return;
        }
        var dialog = new MovimientoDialog(selected.Nombre);
        App!.Run(dialog);
        if (!dialog.Accepted) return;
        Task.Run(async () => {
            try {
                var resp = await http.PostAsJsonAsync( $"/productos/{selected.Id}/movimientos", dialog.Movimiento);
                if (resp.IsSuccessStatusCode) {
                    int productoId = selected.Id;
                    await CargarProductos();
                    int indice = filtrados.FindIndex(p => p.Id == productoId);
                    if (indice >= 0)
                    listaProductos.SelectedItem = indice;
                    CargarMovimientosDelSeleccionado();
                    SetStatus("Movimiento registrado");
                } else {
                    var msg = await resp.Content.ReadAsStringAsync();
                    SetStatus($"Error al registrar movimiento: {msg}");
                }
            } catch {
                SetStatus("Error al conectar con el servidor");
            }
        });
    }
    private void SetStatus(string mensaje) {
        statusLabel.Text = $"{mensaje}  | F2 Nuevo | F3 Editar | Del Eliminar | F5 Movimiento | Ctrl+Q Salir";
    }

    

}
