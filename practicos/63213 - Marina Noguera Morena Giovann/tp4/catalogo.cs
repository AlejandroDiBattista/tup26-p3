#:package Terminal.Gui@2.*
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;

using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5050") };

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Sistema de Catálogo (ESC para salir) " };

List<ProductoDto> productosCargados = new();
List<ProductoDto> productosFiltrados = new(); 

var lblBuscar = new Label { Text = "Buscar:", X = 1, Y = 1 };
var txtBuscar = new TextField { X = 9, Y = 1, Width = 30 };

var listaProductos = new ListView { X = 0, Y = 0, Width = 38, Height = 18 };
var listaMovimientos = new ListView { X = 0, Y = 0, Width = 58, Height = 18 };

var menu = new MenuBar {
    Menus = [
        new MenuBarItem ("_Archivo", [ new MenuItem ("_Salir", "ESC", () => app.RequestStop ()) ]),
        new MenuBarItem ("_Acciones", [
            new MenuItem ("_Agregar Producto", "F3", () => AbrirDialogoProducto(null)),
            new MenuItem ("_Editar Producto", "F2", () => EditarProducto()),
            new MenuItem ("_Eliminar Producto", "Supr", () => EliminarProducto()),
            new MenuItem ("_Registrar Movimiento", "F4", () => AbrirDialogoMovimiento())
        ])
    ]
};
ventana.Add(menu, lblBuscar, txtBuscar);

ventana.KeyDown += (s, e) => {
    string tecla = e.ToString() ?? "";
    if (tecla.Contains("F3")) AbrirDialogoProducto(null);
    else if (tecla.Contains("F2")) EditarProducto();
    else if (tecla.Contains("F4")) AbrirDialogoMovimiento();
    else if (tecla.Contains("Delete") || tecla.Contains("Del")) EliminarProducto();
};

var marcoProductos = new FrameView { Title = " Productos [F3:Nuevo | F2:Editar | Supr:Borrar] ", X = 0, Y = 3, Width = 40, Height = 21 };
marcoProductos.Add(listaProductos);

var marcoMovimientos = new FrameView { Title = " Historial [F4:Registrar Mov. ] ", X = 42, Y = 3, Width = 60, Height = 21 };
marcoMovimientos.Add(listaMovimientos);

ventana.Add(marcoProductos, marcoMovimientos);

async Task CargarProductos() {
    try {
        var productos = await http.GetFromJsonAsync<List<ProductoDto>>("/productos");
        productosCargados = productos ?? new();
        FiltrarLista(); 
    } catch { }
}

void FiltrarLista() {
    var texto = txtBuscar.Text?.ToString()?.ToLower() ?? "";
    productosFiltrados = productosCargados.Where(p => 
        p.Nombre.ToLower().Contains(texto) || 
        p.Codigo.ToLower().Contains(texto)).ToList();

    var items = new ObservableCollection<string>(productosFiltrados.Select(p => $"[{p.Codigo}] {p.Nombre} - Stock: {p.Stock}"));
    app.Invoke(() => listaProductos.SetSource(items));
}

txtBuscar.TextChanged += (s, e) => FiltrarLista();

listaProductos.ValueChanged += async (s, e) => {
    int index = listaProductos.SelectedItem ?? -1; 
    if (index >= 0 && index < productosFiltrados.Count) {
        var prod = productosFiltrados[index];
        await CargarMovimientos(prod.Id);
    }
};

async Task CargarMovimientos(int productoId) {
    try {
        var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoId}/movimientos");
        var items = new ObservableCollection<string>(movimientos?.Select(m => $"{m.Fecha:dd/MM} | {m.Tipo} | Cant: {m.Cantidad}") ?? new List<string>());
        app.Invoke(() => listaMovimientos.SetSource(items));
    } catch { }
}

void EditarProducto() {
    int index = listaProductos.SelectedItem ?? -1;
    if (index >= 0 && index < productosFiltrados.Count) {
        AbrirDialogoProducto(productosFiltrados[index]);
    }
}

void EliminarProducto() {
    int index = listaProductos.SelectedItem ?? -1;
    if (index < 0 || index >= productosFiltrados.Count) return;
    var prod = productosFiltrados[index];

    var dlg = new Dialog { Title = "Eliminar Producto" };
    dlg.Add(new Label { Text = $"¿Seguro que deseas eliminar {prod.Nombre}?", X = 1, Y = 1 });

    var btnSi = new Button { Text = "Sí, eliminar" };
    btnSi.Accepting += async (s, e) => {
        app.RequestStop();
        await http.DeleteAsync($"/productos/{prod.Id}");
        await CargarProductos();
    };

    var btnNo = new Button { Text = "Cancelar" };
    btnNo.Accepting += (s, e) => app.RequestStop();

    dlg.AddButton(btnSi);
    dlg.AddButton(btnNo);
    app.Run(dlg);
}

void AbrirDialogoProducto(ProductoDto? prod) {
    var dlg = new Dialog { Title = prod == null ? "Nuevo Producto" : "Editar Producto" };
    var txtCodigo = new TextField { X = 10, Y = 1, Width = 20, Text = prod?.Codigo ?? "" };
    var txtNombre = new TextField { X = 10, Y = 3, Width = 20, Text = prod?.Nombre ?? "" };
    var txtPrecio = new TextField { X = 10, Y = 5, Width = 20, Text = prod?.Precio.ToString() ?? "" };
    var txtStock = new TextField { X = 10, Y = 7, Width = 20, Text = prod?.Stock.ToString() ?? "" };
    
    dlg.Add(new Label { Text = "Código:", X = 1, Y = 1 }, txtCodigo,
            new Label { Text = "Nombre:", X = 1, Y = 3 }, txtNombre,
            new Label { Text = "Precio:", X = 1, Y = 5 }, txtPrecio,
            new Label { Text = "Stock:", X = 1, Y = 7 }, txtStock);

    var btnGuardar = new Button { Text = "Guardar" };
    btnGuardar.Accepting += async (s, e) => { 
        app.RequestStop();
        var nuevo = new ProductoDto(
            prod?.Id ?? 0, 
            txtCodigo.Text?.ToString() ?? "", 
            txtNombre.Text?.ToString() ?? "", 
            decimal.Parse(txtPrecio.Text?.ToString() ?? "0"), 
            int.Parse(txtStock.Text?.ToString() ?? "0")
        );
        if (prod == null) await http.PostAsJsonAsync("/productos", nuevo);
        else await http.PutAsJsonAsync($"/productos/{prod.Id}", nuevo);
        await CargarProductos();
    };

    var btnCancelar = new Button { Text = "Cancelar" };
    btnCancelar.Accepting += (s, e) => app.RequestStop();

    dlg.AddButton(btnGuardar);
    dlg.AddButton(btnCancelar);
    app.Run(dlg);
}

void AbrirDialogoMovimiento() {
    int index = listaProductos.SelectedItem ?? -1;
    if (index < 0 || index >= productosFiltrados.Count) return;
    var prod = productosFiltrados[index];
    var dlg = new Dialog { Title = $"Movimiento: {prod.Nombre}" };
    
    var txtCantidad = new TextField { X = 12, Y = 1, Width = 10 };
    dlg.Add(new Label { Text = "Cantidad:", X = 1, Y = 1 }, txtCantidad);

    async Task EnviarMovimiento(int tipo) {
        app.RequestStop();
        await http.PostAsJsonAsync($"/productos/{prod.Id}/movimientos", new { Tipo = tipo, Cantidad = int.Parse(txtCantidad.Text?.ToString() ?? "0") });
        await CargarProductos();
        await CargarMovimientos(prod.Id);
    }

    var btnCompra = new Button { Text = "Compra" };
    btnCompra.Accepting += async (s,e) => await EnviarMovimiento(0);
    
    var btnVenta = new Button { Text = "Venta" };
    btnVenta.Accepting += async (s,e) => await EnviarMovimiento(1);

    var btnAjuste = new Button { Text = "Ajuste" };
    btnAjuste.Accepting += async (s,e) => await EnviarMovimiento(2);

    var btnCancelar = new Button { Text = "Cancelar" };
    btnCancelar.Accepting += (s, e) => app.RequestStop();
    
    dlg.AddButton(btnCompra);
    dlg.AddButton(btnVenta);
    dlg.AddButton(btnAjuste);
    dlg.AddButton(btnCancelar);
    app.Run(dlg);
}

_ = CargarProductos();
app.Run(ventana);

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, int Tipo, int Cantidad, DateTime Fecha);