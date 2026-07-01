#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui;

// ── Cliente HTTP y cache local ───────────────────────────────────────────

using var http = new HttpClient();
http.BaseAddress = new Uri("http://localhost:5050/");

List<ProductoDto> todosLosProductos = new();
List<ProductoDto> productosFiltrados = new();
List<MovimientoDto> movimientosSeleccionados = new();
ProductoDto? productoSeleccionado = null;

try {
    await ActualizarDatosLocalmenteAsync(http);
} catch (Exception) {
    Console.Error.WriteLine("No se pudo conectar con el servidor en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

Application.Init();

// Menú Superior de la aplicación
var menu = new MenuBar (new MenuBarItem [] {
    new MenuBarItem ("_Archivo", new MenuItem [] {
        new MenuItem ("_Salir", "", () => Application.RequestStop())
    }),
    new MenuBarItem ("_Productos", new MenuItem [] {
        new MenuItem ("_Agregar Producto", "F2", () => AbrirDialogoProducto(null)),
        new MenuItem ("_Editar Producto", "F3", () => AbrirDialogoProducto(productoSeleccionado)),
        new MenuItem ("_Eliminar Producto", "DEL", () => EliminarProductoActual())
    }),
    new MenuBarItem ("_Stock", new MenuItem [] {
        new MenuItem ("Registrar _Movimiento", "F4", () => AbrirDialogoMovimiento())
    })
});

// Ventana Principal
var ventana = new Window () {
    Title = " Catálogo REST — Gestión de Inventario ",
    Y = 1, // Deja espacio para el MenuBar
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

// ── Componentes: Barra de Búsqueda ────────────────────────────────────────

var lblBuscar = new Label () { Text = "Buscar: ", X = 1, Y = 1 };
var txtBuscar = new TextField () { X = 9, Y = 1, Width = Dim.Percent(40) };

// ── Componentes: Maestro (Izquierda) ──────────────────────────────────────

var frameMaestro = new FrameView () {
    Title = " Productos (F2: Agregar | F3: Editar) ",
    X = 1, Y = 3,
    Width = Dim.Percent(55),
    Height = Dim.Fill()
};

var listProductos = new ListView () {
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    AllowSelectedItemBackgroundColor = true
};
frameMaestro.Add(listProductos);

// ── Componentes: Detalle (Derecha) ────────────────────────────────────────

var frameDetalle = new FrameView () {
    Title = " Historial de Stock (F4: Registrar Mov.) ",
    X = Pos.Right(frameMaestro) + 1, Y = 3,
    Width = Dim.Fill() - 1,
    Height = Dim.Fill()
};

var viewDetalleText = new TextView () {
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ReadOnly = true
};
frameDetalle.Add(viewDetalleText);

// ── Lógica de Eventos e Interacción ───────────────────────────────────────

// Al escribir en el buscador
txtBuscar.TextChanged += async (s, e) => {
    FiltrarProductos(txtBuscar.Text);
    listProductos.SetSource(productosFiltrados.Select(p => $"[{p.Codigo}] {p.Nombre} (${p.Precio}) | Stock: {p.Stock}").ToList());
    await SeleccionarProductoAsync(listProductos.SelectedItem);
};

// Al cambiar de producto en la lista
listProductos.SelectedItemChanged += async (s, e) => {
    await SeleccionarProductoAsync(e.Item);
};

// Carga Inicial del ListView
FiltrarProductos("");
listProductos.SetSource(productosFiltrados.Select(p => $"[{p.Codigo}] {p.Nombre} (${p.Precio}) | Stock: {p.Stock}").ToList());
if(productosFiltrados.Count > 0) {
    await SeleccionarProductoAsync(0);
}

ventana.Add(lblBuscar, txtBuscar, frameMaestro, frameDetalle);

// Agregar todo a la aplicación
Application.Top.Add(menu, ventana);
Application.Run();
Application.Shutdown();

// ── Métodos de Sincronización y Lógica ────────────────────────────────────

async Task ActualizarDatosLocalmenteAsync(HttpClient client) {
    todosLosProductos = await client.GetFromJsonAsync<List<ProductoDto>>("productos") ?? new();
}

void FiltrarProductos(string criterio) {
    if (string.IsNullOrWhiteSpace(criterio)) {
        productosFiltrados = todosLosProductos.ToList();
    } else {
        productosFiltrados = todosLosProductos
            .Where(p => p.Codigo.Contains(criterio, StringComparison.OrdinalIgnoreCase) || 
                        p.Nombre.Contains(criterio, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

async Task SeleccionarProductoAsync(int index) {
    if (index >= 0 && index < productosFiltrados.Count) {
        productoSeleccionado = productosFiltrados[index];
        try {
            movimientosSeleccionados = await http.GetFromJsonAsync<List<MovimientoDto>>($"productos/{productoSeleccionado.Id}/movimientos") ?? new();
            RenderizarDetalle();
        } catch {
            viewDetalleText.Text = "Error al cargar movimientos.";
        }
    } else {
        productoSeleccionado = null;
        viewDetalleText.Text = "Ningún producto seleccionado.";
    }
}

void RenderizarDetalle() {
    if (productoSeleccionado == null) return;

    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"PRODUCTO: {productoSeleccionado.Nombre}");
    sb.AppendLine($"Código:   {productoSeleccionado.Codigo}");
    sb.AppendLine($"Precio:   ${productoSeleccionado.Precio:N2}");
    sb.AppendLine($"Stock:    {productoSeleccionado.Stock} unidades");
    sb.AppendLine(new string('─', 40));
    sb.AppendLine("HISTORIAL DE MOVIMIENTOS:");
    sb.AppendLine(string.Format("{0,-12} | {1,-8} | {2}", "Tipo", "Cant.", "Fecha"));
    sb.AppendLine(new string('─', 40));

    foreach (var m in movimientosSeleccionados) {
        sb.AppendLine(string.Format("{0,-12} | {1,8} | {2:dd/MM/yyyy HH:mm}", m.Tipo, m.Cantidad, m.Fecha.ToLocalTime()));
    }

    viewDetalleText.Text = sb.ToString();
}

// ── Diálogos (Formularios) ────────────────────────────────────────────────

void AbrirDialogoProducto(ProductoDto? prod) {
    bool esEdicion = prod != null;
    var dialog = new Dialog () { Title = esEdicion ? " Editar Producto " : " Nuevo Producto ", Width = 50, Height = 12 };

    var lblCod = new Label () { Text = "Código:", X = 2, Y = 1 };
    var txtCod = new TextField () { Text = prod?.Codigo ?? "", X = 12, Y = 1, Width = Dim.Fill() - 2 };

    var lblNom = new Label () { Text = "Nombre:", X = 2, Y = 3 };
    var txtNom = new TextField () { Text = prod?.Nombre ?? "", X = 12, Y = 3, Width = Dim.Fill() - 2 };

    var lblPre = new Label () { Text = "Precio:", X = 2, Y = 5 };
    var txtPre = new TextField () { Text = prod?.Precio.ToString() ?? "0", X = 12, Y = 5, Width = Dim.Fill() - 2 };

    var btnGuardar = new Button () { Text = "Guardar", IsDefault = true };
    var btnCancelar = new Button () { Text = "Cancelar" };

    btnCancelar.Clicked += (s, e) => Application.RequestStop(dialog);
    
    btnGuardar.Clicked += async (s, e) => {
        if (!decimal.TryParse(txtPre.Text, out decimal precio)) {
            MessageBox.ErrorQuery("Error", "Precio inválido", "Ok");
            return;
        }

        var pDto = new ProductoDto(esEdicion ? prod!.Id : 0, txtCod.Text, txtNom.Text, precio, prod?.Stock ?? 0);
        
        try {
            HttpResponseMessage response;
            if (esEdicion) {
                response = await http.PutAsJsonAsync($"productos/{pDto.Id}", pDto);
            } else {
                response = await http.PostAsJsonAsync("productos", pDto);
            }

            if (response.IsSuccessStatusCode) {
                await RecargarTodoElSistemaAsync();
                Application.RequestStop(dialog);
            } else {
                MessageBox.ErrorQuery("Error", "No se pudo guardar en el servidor", "Ok");
            }
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Error", ex.Message, "Ok");
        }
    };

    dialog.Add(lblCod, txtCod, lblNom, txtNom, lblPre, txtPre, btnGuardar, btnCancelar);
    Application.Run(dialog);
}

void AbrirDialogoMovimiento() {
    if (productoSeleccionado == null) {
        MessageBox.ErrorQuery("Error", "Selecciona un producto primero", "Ok");
        return;
    }

    var dialog = new Dialog () { Title = " Registrar Movimiento ", Width = 50, Height = 12 };

    var lblTipo = new Label () { Text = "Tipo (C/V/A):", X = 2, Y = 1 };
    var txtTipo = new TextField () { Text = "Compra", X = 16, Y = 1, Width = Dim.Fill() - 2 }; // Compra, Venta, Ajuste

    var lblCant = new Label () { Text = "Cantidad:", X = 2, Y = 3 };
    var txtCant = new TextField () { Text = "1", X = 16, Y = 3, Width = Dim.Fill() - 2 };

    var btnGuardar = new Button () { Text = "Registrar", IsDefault = true };
    var btnCancelar = new Button () { Text = "Cancelar" };

    btnCancelar.Clicked += (s, e) => Application.RequestStop(dialog);

    btnGuardar.Clicked += async (s, e) => {
        string tipo = txtTipo.Text.Trim();
        // Atajos de escritura rápida
        if (tipo.Equals("c", StringComparison.OrdinalIgnoreCase)) tipo = "Compra";
        if (tipo.Equals("v", StringComparison.OrdinalIgnoreCase)) tipo = "Venta";
        if (tipo.Equals("a", StringComparison.OrdinalIgnoreCase)) tipo = "Ajuste";

        if (!int.TryParse(txtCant.Text, out int cantidad) || cantidad <= 0) {
            MessageBox.ErrorQuery("Error", "Cantidad inválida", "Ok");
            return;
        }

        var input = new { Tipo = tipo, Cantidad = cantidad };
        var response = await http.PostAsJsonAsync($"productos/{productoSeleccionado.Id}/movimientos", input);

        if (response.IsSuccessStatusCode) {
            await RecargarTodoElSistemaAsync();
            Application.RequestStop(dialog);
        } else {
            var errorMsg = await response.Content.ReadAsStringAsync();
            MessageBox.ErrorQuery("Error del Servidor", errorMsg, "Ok");
        }
    };

    dialog.Add(lblTipo, txtTipo, lblCant, txtCant, btnGuardar, btnCancelar);
    Application.Run(dialog);
}

async void EliminarProductoActual() {
    if (productoSeleccionado == null) return;

    int result = MessageBox.Query("Eliminar", $"¿Seguro que querés eliminar {productoSeleccionado.Nombre}?", "Sí", "No");
    if (result == 0) { // Click en "Sí"
        var response = await http.DeleteAsync($"productos/{productoSeleccionado.Id}");
        if (response.IsSuccessStatusCode) {
            await RecargarTodoElSistemaAsync();
        } else {
            MessageBox.ErrorQuery("Error", "No se pudo eliminar.", "Ok");
        }
    }
}

async Task RecargarTodoElSistemaAsync() {
    int itemSeleccionadoIndex = listProductos.SelectedItem;
    await ActualizarDatosLocalmenteAsync(http);
    FiltrarProductos(txtBuscar.Text);
    
    listProductos.SetSource(productosFiltrados.Select(p => $"[{p.Codigo}] {p.Nombre} (${p.Precio}) | Stock: {p.Stock}").ToList());
    
    if (productosFiltrados.Count > 0) {
        listProductos.SelectedItem = Math.Min(itemSeleccionadoIndex, productosFiltrados.Count - 1);
        await SeleccionarProductoAsync(listProductos.SelectedItem);
    } else {
        await SeleccionarProductoAsync(-1);
    }
}

// ── DTOs de comunicación ──────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);