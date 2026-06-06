using Terminal.Gui;
using System.Net.Http.Json;

Application.Init();
var top = Application.Top;

var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
var productosCargados = new List<Producto>();
var productoSeleccionado = new Producto();

var win = new Window ("Administración de Catálogo (TP4)") {
    X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill()
};
top.Add(win);

var lblBuscar = new Label("Buscar:") { X = 1, Y = 1 };
var txtBuscar = new TextField("") { X = 9, Y = 1, Width = Dim.Percent(40) };
win.Add(lblBuscar, txtBuscar);

var frameMaestro = new FrameView("Productos") {
    X = 1, Y = 3, Width = Dim.Percent(50), Height = Dim.Fill() - 1
};
var lstProductos = new ListView() { Width = Dim.Fill(), Height = Dim.Fill() };
frameMaestro.Add(lstProductos);

var frameDetalle = new FrameView("Historial de Movimientos") {
    X = Pos.Right(frameMaestro) + 1, Y = 3, Width = Dim.Fill() - 1, Height = Dim.Fill() - 1
};
var lstMovimientos = new ListView() { Width = Dim.Fill(), Height = Dim.Fill() };
frameDetalle.Add(lstMovimientos);

win.Add(frameMaestro, frameDetalle);

async Task CargarProductos(string filtro = "") {
    try {
        var res = await http.GetFromJsonAsync<List<Producto>>("productos");
        if (res != null) {
            productosCargados = res;
            var listaFiltrada = res.Where(p => 
                p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) || 
                p.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            lstProductos.SetSource(listaFiltrada.Select(p => $"[{p.Codigo}] {p.Nombre} - ${p.Precio} (Stock: {p.Stock})").ToList());
        }
    } catch { }
}

async Task CargarMovimientos(int prodId) {
    try {
        var res = await http.GetFromJsonAsync<List<Movimiento>>($"productos/{prodId}/movimientos");
        if (res != null) {
            lstMovimientos.SetSource(res.Select(m => $"{m.Fecha:dd/MM/yyyy HH:mm} | {m.Tipo.ToUpper()} | Cant: {m.Cantidad}").ToList());
        }
    } catch { 
        lstMovimientos.SetSource(new List<string>());
    }
}

txtBuscar.TextChanged += async (e) => {
    await CargarProductos(txtBuscar.Text.ToString());
};

lstProductos.OpenSelectedItem += async (e) => {
    if (productosCargados.Count > 0 && lstProductos.SelectedItem < productosCargados.Count) {
        productoSeleccionado = productosCargados[lstProductos.SelectedItem];
        await CargarMovimientos(productoSeleccionado.Id);
    }
};

void MostrarDialogoProducto(Producto? p = null) {
    var d = new Dialog(p == null ? "Agregar Producto" : "Editar Producto", 50, 12);
    
    var lblCod = new Label("Código:") { X = 1, Y = 1 };
    var txtCod = new TextField(p?.Codigo ?? "") { X = 10, Y = 1, Width = 35 };
    var lblNom = new Label("Nombre:") { X = 1, Y = 3 };
    var txtNom = new TextField(p?.Nombre ?? "") { X = 10, Y = 3, Width = 35 };
    var lblPre = new Label("Precio:") { X = 1, Y = 5 };
    var txtPre = new TextField(p?.Precio.ToString() ?? "0") { X = 10, Y = 5, Width = 35 };

    d.Add(lblCod, txtCod, lblNom, txtNom, lblPre, txtPre);

    var btnGuardar = new Button("Guardar", is_default: true);
    btnGuardar.Clicked += async () => {
        var nuevo = new Producto { 
            Codigo = txtCod.Text.ToString(), 
            Nombre = txtNom.Text.ToString(), 
            Precio = decimal.Parse(txtPre.Text.ToString() ?? "0") 
        };
        
        if (p == null) await http.PostAsJsonAsync("productos", nuevo);
        else await http.PutAsJsonAsync($"productos/{p.Id}", nuevo);
        
        Application.RequestStop();
        await CargarProductos();
    };

    var btnCancelar = new Button("Cancelar");
    btnCancelar.Clicked += () => Application.RequestStop();

    d.AddButton(btnGuardar);
    d.AddButton(btnCancelar);
    Application.Run(d);
}

void RegistrarMovimiento() {
    if (productoSeleccionado.Id == 0) return;

    var d = new Dialog("Registrar Movimiento", 50, 10);
    var lblTipo = new Label("Tipo (Compra/Venta/Ajuste):") { X = 1, Y = 1 };
    var txtTipo = new TextField("Compra") { X = 28, Y = 1, Width = 15 };
    var lblCant = new Label("Cantidad:") { X = 1, Y = 3 };
    var txtCant = new TextField("1") { X = 28, Y = 3, Width = 15 };

    d.Add(lblTipo, txtTipo, lblCant, txtCant);

    var btnOk = new Button("Aceptar", is_default: true);
    btnOk.Clicked += async () => {
        var req = new MovimientoReq { 
            Tipo = txtTipo.Text.ToString(), 
            Cantidad = int.Parse(txtCant.Text.ToString() ?? "0") 
        };
        await http.PostAsJsonAsync($"productos/{productoSeleccionado.Id}/movimientos", req);
        Application.RequestStop();
        await CargarProductos();
        await CargarMovimientos(productoSeleccionado.Id);
    };

    d.AddButton(btnOk);
    Application.Run(d);
}

var menu = new MenuBar (new MenuBarItem [] {
    new MenuBarItem ("_Archivo", new MenuItem [] {
        new MenuItem ("_Salir", "", () => Application.RequestStop())
    }),
    new MenuBarItem ("_Productos", new MenuItem [] {
        new MenuItem ("_Agregar", "F2", () => MostrarDialogoProducto()),
        new MenuItem ("_Editar", "F3", () => { if(productoSeleccionado.Id != 0) MostrarDialogoProducto(productoSeleccionado); }),
        new MenuItem ("_Eliminar", "F4", async () => { 
            if(productoSeleccionado.Id != 0) {
                await http.DeleteAsync($"productos/{productoSeleccionado.Id}");
                await CargarProductos();
            }
        })
    }),
    new MenuBarItem ("_Movimientos", new MenuItem [] {
        new MenuItem ("_Registrar", "F5", () => RegistrarMovimiento())
    })
});
top.Add (menu);

Task.Run(async () => await CargarProductos());

Application.Run();
Application.Shutdown();

public class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}
public class Movimiento {
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}
public class MovimientoReq {
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
}