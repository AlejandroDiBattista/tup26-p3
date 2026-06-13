#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;

// ── Consulta inicial al servidor ──────────────────────────────────────────

using var http = new HttpClient();

List<ProductoDto> productos;
try {
    productos = await TraerProductosAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var listaProductos = new ListView {
    X = 0, Y = 2,
    Width = Dim.Percent(45),
    Height = Dim.Fill(),
};

var detalleMovimientos = new Label {
    X = Pos.Right(listaProductos) + 1,
    Y = 2,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Text = "(seleccioná un producto)",
};

listaProductos.ValueChanged += (sender, args) => {
    int? i = listaProductos.SelectedItem;
    if (i >= 0 && i < productos.Count) {
        _ = MostrarMovimientos(productos[i.Value]);
    }
};

async Task RecargarLista(string filtro = "") {
    productos = await TraerProductosAsync(http);
    if (!string.IsNullOrWhiteSpace(filtro)) {
        productos = productos
            .Where(p => p.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                     || p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
    listaProductos.SetSource(new ObservableCollection<string>(
        productos.Select(p => $"{p.Codigo}  {p.Nombre}  ${p.Precio}  (stock {p.Stock})")
    ));
}

async Task MostrarMovimientos(ProductoDto p) {
    var movimientos = await TraerMovimientosAsync(http, p.Id);
    if (movimientos.Count == 0) {
        detalleMovimientos.Text = $"Movimientos de {p.Nombre}:\n\n(sin movimientos)";
        return;
    }
    var lineas = movimientos.Select(m =>
        $"{m.Fecha:dd/MM HH:mm}  {m.Tipo,-7} {m.Cantidad,5}");
    detalleMovimientos.Text = $"Movimientos de {p.Nombre}:\n\n" + string.Join("\n", lineas);
}

async Task EditarProducto(ProductoDto? existente) {
    var codigo = new TextField { X = 12, Y = 1, Width = 30, Text = existente?.Codigo ?? "" };
    var nombre = new TextField { X = 12, Y = 3, Width = 30, Text = existente?.Nombre ?? "" };
    var precio = new TextField { X = 12, Y = 5, Width = 30, Text = existente?.Precio.ToString() ?? "0" };

    var ok = new Button { Text = "Guardar", IsDefault = true };
    bool guardar = false;
    ok.Accepting += (s, e) => { guardar = true; app.RequestStop(); };
    var dlg = new Dialog { Title = existente is null ? "Nuevo producto" : "Editar producto", Width = 50, Height = 10 };
    dlg.Add(new Label { X = 1, Y = 1, Text = "Código:" });
    dlg.Add(new Label { X = 1, Y = 3, Text = "Nombre:" });
    dlg.Add(new Label { X = 1, Y = 5, Text = "Precio:" });
    dlg.Add(codigo, nombre, precio);
    dlg.AddButton(ok);
    app.Run(dlg);

    if (!guardar) return;

    decimal.TryParse(precio.Text, out decimal precioNum);
    int stock = existente?.Stock ?? 0;
    var p = new ProductoDto(existente?.Id ?? 0, codigo.Text, nombre.Text, precioNum, stock);

    if (existente is null) await CrearProductoAsync(http, p);
    else await ModificarProductoAsync(http, existente.Id, p);

    await RecargarLista();
}

async Task EliminarProducto(ProductoDto p) {
    var si = new Button { Text = "Sí" };
    var no = new Button { Text = "No", IsDefault = true };
    bool confirmar = false;
    si.Accepting += (s, e) => { confirmar = true; app.RequestStop(); };
    no.Accepting += (s, e) => { app.RequestStop(); };

    var dlg = new Dialog { Title = "Eliminar", Width = 50, Height = 7 };
    dlg.Add(new Label { X = 1, Y = 1, Text = $"¿Eliminar {p.Nombre}?" });
    dlg.AddButton(si);
    dlg.AddButton(no);
    app.Run(dlg);

    if (!confirmar) return;
    await EliminarProductoAsync(http, p.Id);
    await RecargarLista();
}

async Task RegistrarMov(ProductoDto p) {
    var cantidadField = new TextField { X = 12, Y = 1, Width = 20, Text = "0" };
    string tipoElegido = "";

    var compra = new Button { Text = "Compra" };
    var venta  = new Button { Text = "Venta" };
    var ajuste = new Button { Text = "Ajuste" };
    compra.Accepting += (s, e) => { tipoElegido = "Compra"; app.RequestStop(); };
    venta.Accepting  += (s, e) => { tipoElegido = "Venta";  app.RequestStop(); };
    ajuste.Accepting += (s, e) => { tipoElegido = "Ajuste"; app.RequestStop(); };

    var dlg = new Dialog { Title = $"Movimiento — {p.Nombre}", Width = 50, Height = 9 };
    dlg.Add(new Label { X = 1, Y = 1, Text = "Cantidad:" });
    dlg.Add(cantidadField);
    dlg.AddButton(compra);
    dlg.AddButton(venta);
    dlg.AddButton(ajuste);
    app.Run(dlg);

    if (tipoElegido == "") return;

    int.TryParse(cantidadField.Text, out int cantidad);
    await RegistrarMovimientoAsync(http, p.Id, tipoElegido, cantidad);
    await RecargarLista();
    await MostrarMovimientos(p);
}

var botonNuevo = new Button { X = 0, Y = 0, Text = "Nuevo" };
botonNuevo.Accepting += (s, e) => { _ = EditarProducto(null); e.Handled = true; };

var botonEditar = new Button { X = Pos.Right(botonNuevo) + 1, Y = 0, Text = "Editar" };
botonEditar.Accepting += (s, e) => {
    int? i = listaProductos.SelectedItem;
    if (i >= 0 && i < productos.Count) _ = EditarProducto(productos[i.Value]);
    e.Handled = true;
};

var botonEliminar = new Button { X = Pos.Right(botonEditar) + 1, Y = 0, Text = "Eliminar" };
botonEliminar.Accepting += (s, e) => {
    int? i = listaProductos.SelectedItem;
    if (i >= 0 && i < productos.Count) _ = EliminarProducto(productos[i.Value]);
    e.Handled = true;
};

var botonMovimiento = new Button { X = Pos.Right(botonEliminar) + 1, Y = 0, Text = "Movimiento" };
botonMovimiento.Accepting += (s, e) => {
    int? i = listaProductos.SelectedItem;
    if (i >= 0 && i < productos.Count) _ = RegistrarMov(productos[i.Value]);
    e.Handled = true;
};

var buscarField = new TextField { X = Pos.Right(botonMovimiento) + 2, Y = 0, Width = 20 };

var botonBuscar = new Button { X = Pos.Right(buscarField) + 1, Y = 0, Text = "Buscar" };
botonBuscar.Accepting += (s, e) => { _ = RecargarLista(buscarField.Text); e.Handled = true; };

await RecargarLista();
ventana.Add(botonNuevo, botonEditar, botonEliminar, botonMovimiento, buscarField, botonBuscar, listaProductos, detalleMovimientos);
app.Run(ventana);

static async Task<List<ProductoDto>> TraerProductosAsync(HttpClient http) {
    const string url = "http://localhost:5050/productos";
    return await http.GetFromJsonAsync<List<ProductoDto>>(url) ?? [];
}

static async Task<List<MovimientoDto>> TraerMovimientosAsync(HttpClient http, int productoId) {
    string url = $"http://localhost:5050/productos/{productoId}/movimientos";
    return await http.GetFromJsonAsync<List<MovimientoDto>>(url) ?? [];
}

static async Task CrearProductoAsync(HttpClient http, ProductoDto p) {
    await http.PostAsJsonAsync("http://localhost:5050/productos", p);
}

static async Task ModificarProductoAsync(HttpClient http, int id, ProductoDto p) {
    await http.PutAsJsonAsync($"http://localhost:5050/productos/{id}", p);
}

static async Task EliminarProductoAsync(HttpClient http, int id) {
    await http.DeleteAsync($"http://localhost:5050/productos/{id}");
}

static async Task RegistrarMovimientoAsync(HttpClient http, int productoId, string tipo, int cantidad) {
    var dto = new { Tipo = tipo, Cantidad = cantidad };
    await http.PostAsJsonAsync($"http://localhost:5050/productos/{productoId}/movimientos", dto);
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);

record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
