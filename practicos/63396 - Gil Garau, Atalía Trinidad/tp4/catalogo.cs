#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;

using Terminal.Gui.Drawing;

// ── Consulta inicial al servidor ──────────────────────────────────────────

using var http = new HttpClient(BaseAddress: new Uri("http://localhost:5050/"));
List<ProductoDto> productos;
// ProductoDto producto;
try {
    using var http = new HttpClient();
    productos = CargarProductosAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

List<ProductoDto> filtrados = productos.ToList();
bool actualizarLista = false;


// ── Interfaz TUI ──────────────────────────────────────────────────────────

using IApplication app = Application.Create().Init();
using Window ventana = new () { Title = " Catalogo REST — Producto (ESC para salir) " };

var Buscar = new Label {
    Text = "Buscar:",
    X = 2, Y = 1,
};
var search = new TextField {
    
    X = 10, 
    Y = 1,
    Width = Dim.Percent(35),
};



var panelizquierdo = new FrameView {
    Title = "Productos",
    X = Pos.Percent(0), Y = Pos.Bottom(search) + 1,
    Width = Dim.Percent(40),
    Height = Dim.Percent(100),
};
var panelDerecho = new FrameView {
    Title = "Detalles y movimientos del producto",
    X = Pos.Right(panelizquierdo),
    Y = Pos.Bottom(search) + 1,
    Width = Dim.Percent(60),
    Height = Dim.Percent(100),
};

var listaProductos = new ListView {
    X = 1, Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(2),
};


var detalleProducto = new Label {
    Text = $"""
            # PRODUCTO 

            Selecciona un producto de la lista para ver su detalle y movimientos.
            """,
    X = 2, Y = 1,
    Width = Dim.Fill(4),
    Height = Dim.Percent(45),
};

var movimientosProducto = new Label {
   Text = """
            # MOVIMIENTOS

            Selecciona un producto de la lista para ver sus movimientos.
            """,
   
    X = 2,
    Y = Pos.Bottom(detalleProducto) + 1,
    Width = Dim.Fill(4),
    Height = Dim.Fill(2),
};

panelizquierdo.Add(listaProductos);
panelDerecho.Add(detalleProducto, movimientosProducto);
ventana.Add(Buscar, search, panelizquierdo, panelDerecho);








listaPoductos.ValueChanged += (_,_) => {
    var producto = Obtenerproductos(listaProductos, producto);

    if (producto is null) {
        detalleProducto.Text = "No hay productos disponibles/seleccionados.";
        movimientosProducto.Text = "";
        return;
    }

    detalleProducto.Text = $"""
            # PRODUCTO 

            - Id     : {producto.Id}
            - Código : {producto.Codigo}
            - Nombre : {producto.Nombre}
            - Precio : ${producto.Precio,10:N2}
            - Stock  :  {producto.Stock,10}
            """;
}        

using var http = new HttpClient(BaseAddress: new Uri("http://localhost:5050/"));
var movimientos = cargarMovimientos(http, producto.Id);

detalleMovimientos.Text = movimientos.count == 0 ? "No hay movimientos registrados para este producto." 
: $"""
        # MOVIMIENTOS

        {string.Join("\n", movimientos.Select(m => $"- {m.Accion} {m.Cantidad} unidades el {m.Fecha:dd/MM/yyyy HH:mm:ss}"))}
        """;


app.Run(ventana);










// funciones auxiliares

static List<ProductoDto> CargarProductosAsync(HttpClient http) {
    return http.GetFromJsonAsync<List<ProductoDto>>("/productos").Result ?? new List<ProductoDto>();
}
static ProductoDto CargarProducto(HttpClient http, int id)
{
    return http.GetFromJsonAsync<ProductoDto>($"/productos/{id}").Result
        ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
}
static List<MovimientosDto> CargarMovimientos(HttpClient http, int id)
{
    return http.GetFromJsonAsync<List<MovimientosDto>>($"/productos/{id}/movimientos").Result ?? [];
}

// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
