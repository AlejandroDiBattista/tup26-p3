#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ── Estado de la aplicación y componentes globales ────────────────────────
List<ProductoDto> todosLosProductos = new();
ProductoDto? productoSeleccionado = null;

// Componentes para el Layout Maestro/Detalle (Terminal.Gui v2)
ListView vistaProductos = new() { Width = Dim.Fill(), Height = Dim.Fill() };
ListView vistaMovimientos = new() { Width = Dim.Fill(), Height = Dim.Fill() };
TextField txtBuscar = new() { Width = Dim.Fill() };

// ── Consulta inicial al servidor ──────────────────────────────────────────

ProductoDto producto;
try {
    using var http = new HttpClient();
    producto = await CargarProductoAsync(http);

    todosLosProductos = await http.GetFromJsonAsync<List<ProductoDto>>("http://localhost:5050/productos") ?? new();

} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var detalleProducto = new Label {
    Text = $"""
            # PRODUCTO 

            - Id     : {producto.Id}
            - Código : {producto.Codigo}
            - Nombre : {producto.Nombre}
            - Precio : ${producto.Precio,10:N2}
            - Stock  :  {producto.Stock,10}
            """,
    X = 4, Y = 2,
};

ventana.Add(detalleProducto);

//1- creamos el panel izquierdo (maestro) abajo del detalle del profesor
FrameView panelMaestro = new() {
    Title = " productos ",
    X = 0,
    Y = 8, // Ajustado a 8 para que entre en monitores chicos
    Width = Dim.Percent(50),
    Height = 15 // Altura fija para forzar el renderizado completo
};

Label lblBuscar = new() { Text = "Buscar: ", X = 1, Y = 1};

//configuramos el buscador adentro del panel izquierdo
txtBuscar.X = Pos.Right(lblBuscar);
txtBuscar.Y = 1;
txtBuscar.Width = Dim.Fill(1);

//ubicamos la lista de productos
vistaProductos.X = 0;
vistaProductos.Y = 3;
vistaProductos.Width = Dim.Fill();
vistaProductos.Height = Dim.Fill();

// metemos los componentes dentro del panel izquierdo
panelMaestro.Add(lblBuscar, txtBuscar, vistaProductos);

// 2- creamos el panel derecho (detalle) pegado al borde izquierdo del maestro
FrameView panelDetalle = new () {
    Title = " Historial de Movimientos ",
    X = Pos.Right(panelMaestro),
    Y = 8, 
    Width = Dim.Fill(),
    Height = 15 
};

//ubicamos la lista de movimientos
vistaMovimientos.X = 0;
vistaMovimientos.Y = 1;
vistaMovimientos.Width = Dim.Fill();
vistaMovimientos.Height = Dim.Fill();

panelDetalle.Add(vistaMovimientos);

//agregamos los paneles a la ventana
ventana.Add(panelMaestro, panelDetalle);

// 1. Creamos la lista de texto normal
var listaNombres = todosLosProductos.Select(p => $"[{p.Codigo}] {p.Nombre} (Stock: {p.Stock})").ToList();

// 2. La transformamos al tipo exacto que exige la v2 usando ObservableCollection
vistaProductos.SetSource<string>(new System.Collections.ObjectModel.ObservableCollection<string>(listaNombres));

// para filtrar la lista de productos en tiempo real
txtBuscar.TextChanged += (s, e) => {
    var texto = txtBuscar.Text.ToLower().Trim();

    //Si esta vacio, muestra todos
    if (string.IsNullOrEmpty(texto)) {
        var listaCompleta = todosLosProductos.Select(p => $"[{p.Codigo}] {p.Nombre} (Stock: {p.Stock})").ToList();
        vistaProductos.SetSource<string>(new System.Collections.ObjectModel.ObservableCollection<string>(listaCompleta));
        return;
    }

    //si tiene texto, filtra por codigo o nombre
    var filtrados = todosLosProductos
        .Where(p => p.Nombre.ToLower().Contains(texto) || p.Codigo.ToLower().Contains(texto))
        .Select(p => $"[{p.Codigo}] {p.Nombre} (Stock: {p.Stock})")
        .ToList();

    vistaProductos.SetSource<string>(new System.Collections.ObjectModel.ObservableCollection<string>(filtrados));
};
 
// ── Cambiado a una función de acción directa 
vistaProductos.SelectedItemChanged += async (args) => {
    if (args.Item >= 0 && args.Item < todosLosProductos.Count) {
        productoSeleccionado = todosLosProductos[args.Item];

        // Actualizamos el detalle de arriba en base al seleccionado
        detalleProducto.Text = $"""
                # PRODUCTO 

                - Id     : {productoSeleccionado.Id}
                - Código : {productoSeleccionado.Codigo}
                - Nombre : {productoSeleccionado.Nombre}
                - Precio : ${productoSeleccionado.Precio,10:N2}
                - Stock  :  {productoSeleccionado.Stock,10}
                """;

        // Buscamos los movimientos en el servidor de forma asíncrona
        try {
            using var http = new HttpClient();
            var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>($"http://localhost:5050/productos/{productoSeleccionado.Id}/movimientos") ?? new();
            
            var listaMovsTexto = movimientos.Select(m => $"[{m.Fecha:dd/MM/yy HH:mm}] {m.Tipo} - Cantidad: {m.Cantidad}").ToList();
            
            if (listaMovsTexto.Count == 0) {
                listaMovsTexto.Add("Sin movimientos registrados");
            }

            vistaMovimientos.SetSource<string>(new System.Collections.ObjectModel.ObservableCollection<string>(listaMovsTexto));
        }
        catch (Exception ex) {
            var errorList = new List<string> { "Error al cargar movimientos", ex.Message };
            vistaMovimientos.SetSource<string>(new System.Collections.ObjectModel.ObservableCollection<string>(errorList));
        }
    }
}; 

app.Run(ventana);

static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
} 

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);