#:package Terminal.Gui@2.*
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;

// ── Consulta inicial al servidor ──────────────────────────────────────────

ProductoDto producto;
try {
    using var http = new HttpClient( );
    producto = await CargarProductoAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

List<ProductoDto> todosLosProductos = new();
List<ProductoDto> productosFiltrados = new();
ProductoDto? productoSeleccionado = null;
List<MovimientoDto> movimientos = new();

using Window ventana = new() {
    Title = " Catálogo REST - Maestro/Detalle (ESC para salir) ",
    Width = Dim.Fill(), 
    Height = Dim.Fill()
};

var menuPrincipal = new MenuBar(new MenuBarItem[] {
    new MenuBarItem("_Archivo", new MenuItem[] {
        new MenuItem("_Salir", "Cierra la aplicación", () => Application.RequestStop())
    }),
    new MenuBarItem("_Productos", new MenuItem[] {
        new MenuItem("_Nuevo", "", () => MostrarDialogoProducto(null)),
        new MenuItem("_Editar", "", () => MostrarDialogoProducto(productoSeleccionado)),
        new MenuItem("_Eliminar", "", () => EliminarProductoSeleccionado())
    }),
    new MenuBarItem("_Movimientos", new MenuItem[] {
        new MenuItem("_Registrar Movimiento", "", () => MostrarDialogoMovimiento(productoSeleccionado))
    })
});

var panelIzquierdo = new FrameView("Productos") {
    X = 0, Y = Pos.Bottom(menuPrincipal),
    Width = Dim.Percent(50), Height = Dim.Fill()
};

var lblBuscar = new Label("Buscar:") { X = 0, Y = 0 };
var txtBuscar = new TextField("") {
    X = Pos.Right(lblBuscar) + 1, Y = 0,
    Width = Dim.Fill()
};

var listaProductos = new ListView() {
    X = 0, Y = Pos.Bottom(lblBuscar) + 1,
    Width = Dim.Fill(), Height = Dim.Fill(),
    AllowsMarking = false
};

panelIzquierdo.Add(lblBuscar, txtBuscar, listaProductos);

var panelDerecho = new FrameView("Movimientos de Stock") {
    X = Pos.Right(panelIzquierdo), Y = Pos.Bottom(menuPrincipal),
    Width = Dim.Fill(), Height = Dim.Fill()
};

var listaMovimientos = new ListView() {
    X = 0, Y = 0,
    Width = Dim.Fill(), Height = Dim.Fill(),
    AllowsMarking = false
};

panelDerecho.Add(listaMovimientos);
ventana.Add(menuPrincipal, panelIzquierdo, panelDerecho);


/* Eventos de la UI*/
txtBuscar.TextChanged += (s, e) => FiltrarProductos();

listaProductos.SelectedItemChanged += (s, e) => {
    if (listaProductos.SelectedItem >= 0 && listaProductos.SelectedItem < productosFiltrados.Count) {
        productoSeleccionado = productosFiltrados[listaProductos.SelectedItem];
        _ = Task.Run(CargarMovimientosAsync); // Fire & forget seguro
    } else {
        productoSeleccionado = null;
        movimientos.Clear();
        listaMovimientos.SetSource(movimientos);
    }
};

async Task CargarProductosAsync() {
    try {
        var resultado = await clienteHttp.GetFromJsonAsync<List<ProductoDto>>("/productos", opcionesJson);
        if (resultado != null) {
            Application.Invoke(() => {
                todosLosProductos = resultado;
                FiltrarProductos();
            });
        }
    } catch (Exception ex) {
        Application.Invoke(() => MessageBox.ErrorQuery("Error", "No se pudo conectar con el servidor: " + ex.Message, "OK"));
    }
}

void FiltrarProductos() {
    var busqueda = txtBuscar.Text?.ToString()?.ToLower() ?? "";
    productosFiltrados = todosLosProductos
        .Where(p => p.Codigo.ToLower().Contains(busqueda) || p.Nombre.ToLower().Contains(busqueda))
        .ToList();
    
    listaProductos.SetSource(productosFiltrados.Select(p => $"[{p.Codigo}] {p.Nombre} - ${p.Precio:N2} (Stk: {p.Stock})").ToList());
    
    if (productosFiltrados.Count > 0) {
        listaProductos.SelectedItem = 0;
        productoSeleccionado = productosFiltrados[0];
        _ = Task.Run(CargarMovimientosAsync);
    } else {
        productoSeleccionado = null;
        movimientos.Clear();
        listaMovimientos.SetSource(movimientos);
    }
}

async Task CargarMovimientosAsync() {
    if (productoSeleccionado == null) return;
    try {
        var resultado = await clienteHttp.GetFromJsonAsync<List<MovimientoDto>>($"/productos/{productoSeleccionado.Id}/movimientos", opcionesJson);
        if (resultado != null) {
            Application.Invoke(() => {
                movimientos = resultado;
                listaMovimientos.SetSource(movimientos.Select(m => $"{m.Fecha:dd/MM HH:mm} | {m.Tipo,-6} | Cant: {m.Cantidad}").ToList());
            });
        }
    } catch { /* Ocultar error si el producto fue borrado o la red no está lista*/ }
}

void MostrarDialogoProducto(ProductoDto? p) {
    var esEdicion = p != null;
    var dialogo = new Dialog(esEdicion ? "Editar Producto" : "Nuevo Producto", 50, 12);

    var txtCodigo = new TextField(esEdicion ? p!.Codigo : "") { X = 10, Y = 1, Width = Dim.Fill() - 2 };
    var txtNombre = new TextField(esEdicion ? p!.Nombre : "") { X = 10, Y = 3, Width = Dim.Fill() - 2 };
    var txtPrecio = new TextField(esEdicion ? p!.Precio.ToString() : "") { X = 10, Y = 5, Width = Dim.Fill() - 2 };

    dialogo.Add(new Label("Código:") { X = 1, Y = 1 });
    dialogo.Add(txtCodigo);
    dialogo.Add(new Label("Nombre:") { X = 1, Y = 3 });
    dialogo.Add(txtNombre);
    dialogo.Add(new Label("Precio:") { X = 1, Y = 5 });
    dialogo.Add(txtPrecio);

    var btnAceptar = new Button("Guardar");
    var btnCancelar = new Button("Cancelar");

    btnAceptar.Accept += (s, e) => {
        if (string.IsNullOrWhiteSpace(txtCodigo.Text?.ToString()) || 
            string.IsNullOrWhiteSpace(txtNombre.Text?.ToString()) || 
            !decimal.TryParse(txtPrecio.Text?.ToString(), out decimal precio)) {
            MessageBox.ErrorQuery("Error", "Los datos ingresados son inválidos", "OK");
            return;
        }

        var dto = new ProductoDto(esEdicion ? p!.Id : 0, txtCodigo.Text.ToString()!, txtNombre.Text.ToString()!, precio, esEdicion ? p!.Stock : 0);

        Task.Run(async () => {
            try {
                if (esEdicion) await clienteHttp.PutAsJsonAsync($"/productos/{p!.Id}", dto, opcionesJson);
                else await clienteHttp.PostAsJsonAsync("/productos", dto, opcionesJson);
                
                Application.Invoke(() => {
                    Application.RequestStop();
                    _ = CargarProductosAsync();
                });
            } catch (Exception ex) {
                Application.Invoke(() => MessageBox.ErrorQuery("Error", ex.Message, "OK"));
            }
        });
    };

    btnCancelar.Accept += (s, e) => Application.RequestStop();

    dialogo.AddButton(btnAceptar);
    dialogo.AddButton(btnCancelar);
    app.Run(dialogo);
}

void EliminarProductoSeleccionado() {
    if (productoSeleccionado == null) {
        MessageBox.ErrorQuery("Aviso", "Seleccione un producto primero", "OK");
        return;
    }
    
    var resultado = MessageBox.Query("Eliminar", $"¿Seguro que desea eliminar el producto '{productoSeleccionado.Nombre}'?", "No", "Sí");
    if (resultado == 1) { // 1 = Índice del botón "Sí"
        Task.Run(async () => {
            try {
                await clienteHttp.DeleteAsync($"/productos/{productoSeleccionado.Id}");
                Application.Invoke(() => _ = CargarProductosAsync());
            } catch (Exception ex) {
                Application.Invoke(() => MessageBox.ErrorQuery("Error al eliminar", ex.Message, "OK"));
            }
        });
    }
}

void MostrarDialogoMovimiento(ProductoDto? p) {
    if (p == null) {
        MessageBox.ErrorQuery("Aviso", "Seleccione un producto para registrarle un movimiento", "OK");
        return;
    }

    var dialogo = new Dialog($"Registrar Movimiento - {p.Nombre}", 50, 12);

    var radioTipo = new RadioGroup(new string[] { "Compra (Sumar)", "Venta (Restar)", "Ajuste (Establecer)" }) { X = 12, Y = 1 };
    var txtCantidad = new TextField("") { X = 12, Y = 5, Width = 15 };

    dialogo.Add(new Label("Tipo:") { X = 1, Y = 1 });
    dialogo.Add(radioTipo);
    dialogo.Add(new Label("Cantidad:") { X = 1, Y = 5 });
    dialogo.Add(txtCantidad);

    var btnAceptar = new Button("Registrar");
    var btnCancelar = new Button("Cancelar");

    btnAceptar.Accept += (s, e) => {
        if (!int.TryParse(txtCantidad.Text?.ToString(), out int cantidad) || cantidad <= 0) {
            MessageBox.ErrorQuery("Error", "La cantidad debe ser un número entero mayor que 0", "OK");
            return;
        }

        var tipo = radioTipo.SelectedItem switch {
            0 => TipoMovimiento.Compra,
            1 => TipoMovimiento.Venta,
            2 => TipoMovimiento.Ajuste,
            _ => TipoMovimiento.Compra
        };

        var dto = new MovimientoDto(0, p.Id, tipo, cantidad, DateTime.Now);

        Task.Run(async () => {
            try {
                var respuesta = await clienteHttp.PostAsJsonAsync($"/productos/{p.Id}/movimientos", dto, opcionesJson);
                if (!respuesta.IsSuccessStatusCode) {
                    var mensajeError = await respuesta.Content.ReadAsStringAsync();
                    Application.Invoke(() => MessageBox.ErrorQuery("Aviso del Servidor", mensajeError, "OK"));
                    return;
                }
                
                Application.Invoke(() => {
                    Application.RequestStop();
                    _ = CargarProductosAsync(); // Vuelve a cargar productos para ver el stock actualizado
                });
            } catch (Exception ex) {
                Application.Invoke(() => MessageBox.ErrorQuery("Error de Conexión", ex.Message, "OK"));
            }
        });
    };

    btnCancelar.Accept += (s, e) => Application.RequestStop();

    dialogo.AddButton(btnAceptar);
    dialogo.AddButton(btnCancelar);
    app.Run(dialogo);
}

/* Inicializacion */

_ = Task.Run(CargarProductosAsync); // Carga inicial de productos sin bloquear la UI


app.Run(ventana);

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
