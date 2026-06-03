#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


var http = new HttpClient{ BaseAddress = new Uri("http://localhost:5050") };

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

public class MovimientoDto {
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

        FrameView frameProductos = new() {
            Title = "Productos",
            X = 0,
            Y = 3,
            Width = Dim.Percent(55),
            Height = Dim.Fill(2)
        };
        listaProductos = new ListView() {
            X=0,
            Y=0,
            Width=Dim.Fill(),
            Height=Dim.Fill()
        };
        listaProductos.RowRender += (_, _) => CargarMovimientosDelSeleccionado();
        frameProductos.Add(listaProductos);

        FrameView frameMovimientos = new() {
            Title = "Movimientos",
            X = Pos.Percent(55),
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        listaMovimientos = new ListView() {
            X=0,
            Y=0,
            Width=Dim.Fill(),
            Height=Dim.Fill()
        };
        frameMovimientos.Add(listaMovimientos);

        statusLabel = new Label() {
            Text = "Listo  | F2 Nuevo | F3 Editar | F4 Buscar |F5 Movimiento | Del Eliminar | Ctrl+Q Salir",
            X=0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };
        Add(menu, searchLabel, searchField, frameProductos, frameMovimientos, statusLabel);
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

    private void FocoBusqueda() {
    if (searchField != null) {
        searchField.SetFocus();
    }
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
        statusLabel.Text = $"{mensaje}  | F2 Nuevo | F3 Editar | F4 Buscar |F5 Movimiento | Del Eliminar | Ctrl+Q Salir";
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.F2) {
            NuevoProducto();
            return true;
        }
        if (key == Key.F3) {
            EditarProducto();
            return true;
        }
        if (key == Key.DeleteChar) {
            EliminarProducto();
            return true;
        }
        if (key == Key.F5) {
            RegistrarMovimiento();
            return true;
        }
        if (key == Key.Q.WithCtrl) { 
            App!.RequestStop();    
            return true; }
        if (key == Key.F4) {
            FocoBusqueda();
            return true;
        }
        return base.OnKeyDown(key);
    }
}

public sealed class ProductoDialog : Dialog {
    public new bool Accepted { get; private set; }
    public ProductoDto Producto { get; private set; }
    private readonly TextField codigoField;
    private readonly TextField nombreField;
    private readonly TextField precioField;
    private readonly TextField stockField;

    public ProductoDialog(ProductoDto p) {
        Producto = p;
        Title = "Producto";
        Width = 55;
        Height = 16;

        Add(new Label(){Text="Código:", X=1, Y=1});
        codigoField = new TextField(){X=15, Y=1, Width=35, Text=p.Codigo};
        Add(codigoField);

        Add(new Label(){Text="Nombre:", X=1, Y=3});
        nombreField = new TextField(){X=15, Y=3, Width=35, Text=p.Nombre};
        Add(nombreField);

        Add(new Label(){Text="Precio:", X=1, Y=5});
        precioField = new TextField(){X=15, Y=5, Width=35, Text=p.Precio.ToString()};
        Add(precioField);

        Add(new Label(){Text="Stock:", X=1, Y=7});
        stockField = new TextField(){X=15, Y=7, Width=35, Text=p.Stock.ToString()};
        Add(stockField);

        Button guardar= new() { Text = "_Guardar", X=15, Y=10 };
        guardar.Accepting += (_, e) => {Save(); e.Handled = true;};
        AddButton(guardar);

        Button cancelar = new() { Text = "_Cancelar"};
        cancelar.Accepting += (_, e) => {App!.RequestStop(); e.Handled = true;};
        AddButton(cancelar);
    }

    private void Save() {
        string codigo = codigoField.Text.ToString() ?? "";
        string nombre = nombreField.Text.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(codigo)|| string.IsNullOrWhiteSpace(nombre)) {
            MessageBox.ErrorQuery(App!, "Error", "Código y Nombre no pueden estar vacíos", "OK");
            return;
        }
        if (!decimal.TryParse(precioField.Text.ToString(), out decimal precio) || precio < 0) {
            MessageBox.ErrorQuery(App!, "Error", "Precio debe ser un número positivo", "OK");
            return;
        }
        if (!int.TryParse(stockField.Text.ToString(), out int stock) || stock < 0) {
            MessageBox.ErrorQuery(App!, "Error", "Stock debe ser un número positivo", "OK");
            return;
        }
        Producto.Codigo = codigo;
        Producto.Nombre = nombre;
        Producto.Precio = precio;
        Producto.Stock = stock;
        Accepted = true;
        App!.RequestStop();
    }
}

public sealed class MovimientoDialog : Dialog {
    public new bool Accepted { get; private set; }
    public MovimientoDto Movimiento { get; private set; } = new();
    private readonly ListView tipoList;
    private readonly TextField cantidadField;

    public MovimientoDialog(string nombreProducto) {
        Title = $"Movimiento - {nombreProducto}";
        Width = 45;
        Height = 14;

        Add(new Label(){Text="Tipo:", X=1, Y=1});
        tipoList = new ListView() {
            X=10,
            Y=1,
            Width=15,
            Height=3
        };
        tipoList.SetSource<string>(
            new System.Collections.ObjectModel.ObservableCollection<string>(["Compra", "Venta", "Ajuste"])
        );
        Add(tipoList);

        Add(new Label(){Text="Cantidad:", X=1, Y=6});
        cantidadField = new TextField(){X=12, Y=6, Width=20, Text=""};
        Add(cantidadField);

        Button guardar= new() { Text= "_Guardar"};
        guardar.Accepting += (_, e) => {Save(); e.Handled = true;};
        AddButton(guardar);

        Button cancelar = new() { Text = "_Cancelar"};
        cancelar.Accepting += (_, e) => {App!.RequestStop(); e.Handled = true;};
        AddButton(cancelar);
    }

    private void Save() {
        if (!int.TryParse(cantidadField.Text.ToString(), out int cantidad) || cantidad <= 0) {
            MessageBox.ErrorQuery(App!, "Error", "Cantidad debe ser un número entero positivo", "OK");
            return;
        }
        string tipo = tipoList.SelectedItem.HasValue ? tipoList.SelectedItem.Value switch {
            0 => "Compra",
            1 => "Venta",
            _ => "Ajuste",
        }
        : "Compra";
        Movimiento.Tipo = tipo;
        Movimiento.Cantidad = cantidad;
        Accepted = true;
        App!.RequestStop();
    }

}