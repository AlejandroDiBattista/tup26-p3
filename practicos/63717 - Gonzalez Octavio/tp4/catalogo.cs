#:package Terminal.Gui@2.*
#:property PublishAot=false
using System.Net.Http.Json;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using Terminal.Gui.Drawing;
using Terminal.Gui.Configuration;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

Console.OutputEncoding = System.Text.Encoding.UTF8;
#pragma warning disable CS0618

// ── Consulta inicial al servidor ──────────────────────────────────────────
List<ProductoDto> productos;
try
{
    using var http = new HttpClient();
    productos = await ObtenerProductos(http);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"{ex.Message}");
    return;
}

ProductoDto producto = null!;
try
{
    int id = 2;
    using var http = new HttpClient();
    producto = await TraerProducto(http, id);
}
catch (Exception e)
{
    Console.Error.WriteLine($"{e.Message}");
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

ConfigurationManager.Enable(ConfigLocations.All);
ConfigurationManager.Apply();

//SCHEMES 

Color fondo = new Color(30, 30, 46);


var esquemaestro = new Scheme
{
    Normal = new Terminal.Gui.Drawing.Attribute(Color.Green, fondo),
    Focus = new Terminal.Gui.Drawing.Attribute(Color.White, Color.Green)
};

var esquedetalle = new Scheme
{
    Normal = new Terminal.Gui.Drawing.Attribute(Color.Green, Color.Black),
    Focus = new Terminal.Gui.Drawing.Attribute(Color.White, Color.Green),
    ReadOnly = new Terminal.Gui.Drawing.Attribute(Color.Green, Color.Black)
};
var esquemagui = new Scheme
{

    Normal = new Terminal.Gui.Drawing.Attribute(Color.Green, fondo)
};

var dialogo = new Scheme
{
    Normal = new Terminal.Gui.Drawing.Attribute(Color.Green, fondo),
    Focus = new Terminal.Gui.Drawing.Attribute(Color.BrightCyan, fondo)
};
var esquemabotones = new Scheme
{
    Normal = new Terminal.Gui.Drawing.Attribute(Color.Green, Color.Black),
    Focus = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.Green),

};

SchemeManager.AddScheme("esquemabotones", esquemabotones);
SchemeManager.AddScheme("Esquemaestro", esquemaestro);
SchemeManager.AddScheme("esquedetalle", esquedetalle);
SchemeManager.AddScheme("gui", esquemagui);
SchemeManager.AddScheme("dialogo", dialogo);

using IApplication app = Application.Create().Init();
using Window gui = new() { SchemeName = "gui" };


//Para poder ver bien el dialog cambie en la las settings del editor, la fuente de la terminal por {"terminal.integrated.fontFamily": "'Cascadia Code', Consolas, monospace"}

//Para salir

bool cerrarapp = false;
var dialogosalir = new Dialog
{
    X = Pos.Center(),
    Y = Pos.Center(),
    Width = 50,
    Height = 10,
    SchemeName = "dialogo",

};
var seguro = new Label
{
    Text = "",
    X = Pos.Center(),
    Y = Pos.Center()
};

dialogosalir.Border.LineStyle = LineStyle.Rounded;
dialogosalir.Border.Thickness = new Thickness(1);

var confirmar = new Button
{
    IsDefault = true,
    SchemeName = "esquemabotones"
};
var cancelar = new Button
{
    SchemeName = "esquemabotones"
};

dialogosalir.Add(seguro);
dialogosalir.AddButton(confirmar);
dialogosalir.AddButton(cancelar);

//Para evitar problemas con compilador
ListView panelmaestro = null!;
FrameView detalle = null!;
TextView listadetalles = null!;

//Menu

var menu = new MenuBar
{
    Menus = [
        new MenuBarItem("_Archivo", [
            new MenuItem("Agregar", "", () => DialogoProducto(null), Key.F2),
            new MenuItem("Editar", "", () => {
                var prod = ObtenerProductoSeleccionado("Debe seleccionar un producto para editar.");
                if (prod is not null) DialogoProducto(prod);
            }, Key.F3),
            new MenuItem("Eliminar", "", async () => await ConfirmarYEliminarProducto(), Key.Delete),
            new MenuItem("_Salir", "S", () => app.RequestStop())
        ]),
        new MenuBarItem("_Movimientos", [
            new MenuItem("_Compra", "", () => DialogoMovimiento(1), Key.F6),
            new MenuItem("_Venta", "", () => DialogoMovimiento(2), Key.F7),
            new MenuItem("_Ajuste", "", () => DialogoMovimiento(3), Key.F8),
        ])
    ],
    SchemeName = "Esquemaestro"

};

var buscar = new Label
{
    Text = " Buscar:",
    X = 3,
    Y = 2,
    SchemeName = "Esquemaestro"
};

var input = new TextField()
{
    X = Pos.Right(buscar),
    Y = Pos.Top(buscar),
    Width = 20,
    SchemeName = "Esquemaestro"
};

//maestro

var maestro = new FrameView
{
    Title = "Productos",
    X = 0,
    Y = 4,
    Width = 25,
    Height = Dim.Fill(2),
    SchemeName = "Esquemaestro",
    CanFocus = true

};

panelmaestro = new ListView
{
    X = 0,
    Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill(1),
    CanFocus = true
};

var cabeceramaestro = new Label
{
    Text = "ID Nombre     Cant. UM",
    X = 0,
    Y = 0,
    SchemeName = "Esquemaestro"
};

sourceabstraccion(productos);
maestro.Add(cabeceramaestro, panelmaestro);

//detalle

detalle = new FrameView
{
    Title = "Detalle",
    X = Pos.Right(maestro),
    Y = Pos.Top(maestro),
    Width = Dim.Fill(),
    Height = Dim.Fill(2),
    SchemeName = "esquedetalle",
    CanFocus = false
};


listadetalles = new TextView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ReadOnly = true,
    SchemeName = "esquedetalle",
    WordWrap = true
};
detalle.Add(listadetalles, buscar);

var teclasdisponibles = new TextView
{
    Text = " F2: Agregar | F3: Editar | Del: Eliminar | F6: Compra | F7: Venta | F8: Ajuste | Esc: Salir",
    X = 0,
    Y = Pos.Bottom(maestro),
    Width = Dim.Fill(),
    Height = 1,
    SchemeName = "Esquemaestro",
    WordWrap = true,
    CanFocus = false,
    ReadOnly = true
};

//Navegacion 
panelmaestro.KeyDown += (sender, e) =>
{
    if (e.KeyCode == Key.CursorDown && panelmaestro.SelectedItem == panelmaestro.Source?.Count - 1)
        e.Handled = true;
    if (e.KeyCode == Key.CursorUp && panelmaestro.SelectedItem == 0)
        e.Handled = true;
};

input.KeyDown += (sender, e) =>
{
    if (e.KeyCode == Key.Enter || e.KeyCode == Key.Esc)
    {
        panelmaestro.SetFocus();
        e.Handled = true;
    }
};


//Añadir las views
gui.Add(menu, maestro, detalle, buscar, input, teclasdisponibles);
panelmaestro.ValueChanged += async (sender, e) => await Refrescardetalle(e.NewValue);

// Funciones de Terminal Gui 

//para salir
gui.KeyDown += async (sender, e) =>
{
    if (e.KeyCode == Key.Esc)
    {
        seguro.Text = " ¿Seguro desea salir? ";
        confirmar.Title = "Confirmar";
        cancelar.Title = "Cancelar";
        e.Handled = true;
        app.Run(dialogosalir);
    }
    else if (e.KeyCode == Key.F2)
    {
        e.Handled = true;
        DialogoProducto(null);
    }
    else if (e.KeyCode == Key.F3)
    {
        e.Handled = true;
        var prod = ObtenerProductoSeleccionado("Debe seleccionar un producto para editar.");
        if (prod is not null) DialogoProducto(prod);
    }
    else if (e.KeyCode == Key.Delete)
    {
        e.Handled = true;
        await ConfirmarYEliminarProducto();
    }
    else if (e.KeyCode == Key.F6)
    {
        e.Handled = true;
        DialogoMovimiento(1);
    }
    else if (e.KeyCode == Key.F7)
    {
        e.Handled = true;
        DialogoMovimiento(2);
    }
    else if (e.KeyCode == Key.F8)
    {
        e.Handled = true;
        DialogoMovimiento(3);
    }

    if (cerrarapp) app.RequestStop();
};
cancelar.Accepting += (_, e) =>
{
    app!.RequestStop();
    e.Handled = true;
};
confirmar.Accepting += (_, e) =>
{
    app.RequestStop();
    cerrarapp = true;
};

ProductoDto? ObtenerProductoSeleccionado(string mensajeError)
{
    int indice = panelmaestro.SelectedItem ?? -1;
    if (indice >= 0 && indice < productos.Count)
    {
        return productos[indice];
    }
    MessageBox.ErrorQuery(app, "Error", mensajeError, "OK");
    return null;
}

async Task ConfirmarYEliminarProducto()
{
    var prod = ObtenerProductoSeleccionado("Seleccionar un producto para eliminar.");
    if (prod is null) return;

    int confirmacion = MessageBox.Query(app, "Confirmar eliminación", $"¿Seguro que desea eliminar el producto \"{prod.Nombre}\"?", "Sí", "No") ?? 1;
    if (confirmacion == 0)
    {
        try
        {
            using var http = new HttpClient();
            bool eliminado = await EliminarProducto(http, prod.Id);
            if (eliminado)
            {
                productos = await ObtenerProductos(http);
                sourceabstraccion(productos);
                await Refrescardetalle(null);
            }
            else
            {
                MessageBox.ErrorQuery(app, "Error", "No se pudo eliminar el producto en el servidor.", "OK");
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(app, "Error", $"Error de conexión: {ex.Message}", "OK");
        }
    }
}

//Buscador
input.TextChanged += (sender, e) => FiltrarProductos();


app.Run(gui);

//funciones locales -----------------------------

const string url1 = $"http://localhost:3000/productos/";

void DialogoProducto(ProductoDto? productoAEditar = null)
{
    var dialogo = new Dialog
    {
        X = Pos.Center(),
        Y = Pos.Center(),
        Width = 55,
        Height = 22,
        SchemeName = "dialogo",
        Title = productoAEditar is null ? "Agregar Producto" : "Editar Producto"
    };

    dialogo.Border.LineStyle = LineStyle.Rounded;
    dialogo.Border.Thickness = new Thickness(1);

    var codigo = new Label
    {
        Text = "Código:",
        X = 2,
        Y = 1
    };
    var txtcodigo = new TextField()
    {
        Text = productoAEditar?.Codigo ?? "",
        X = 18,
        Y = 1,
        Width = 20,
        SchemeName = "dialogo"
    };

    var nombre = new Label
    {
        Text = "Nombre:",
        X = 2,
        Y = 3
    };
    var txtnombre = new TextField()
    {
        Text = productoAEditar?.Nombre ?? "",
        X = 18,
        Y = 3,
        Width = 30,
        SchemeName = "dialogo"
    };

    var precio = new Label
    {
        Text = "Precio:",
        X = 2,
        Y = 5
    };
    var txtprecio = new TextField()
    {
        Text = productoAEditar?.Precio.ToString() ?? "",
        X = 18,
        Y = 5,
        Width = 15,
        SchemeName = "dialogo"
    };

    var stock = new Label
    {
        Text = "Stock:",
        X = 2,
        Y = 7
    };
    var txtstock = new TextField()
    {
        Text = productoAEditar?.Stock.ToString() ?? "0",
        X = 18,
        Y = 7,
        Width = 15,
        SchemeName = "dialogo"
    };

    var cant = new Label
    {
        Text = "Cantidad Medida:",
        X = 2,
        Y = 9
    };
    var txtcant = new TextField()
    {
        Text = productoAEditar?.cant.ToString() ?? "0",
        X = 18,
        Y = 9,
        Width = 15,
        SchemeName = "dialogo"
    };

    var unidadmedida = new Label
    {
        Text = "Unidad Medida:",
        X = 2,
        Y = 11
    };
    var txtunidadmedida = new TextField()
    {
        Text = productoAEditar?.unidadmedida ?? "",
        X = 18,
        Y = 11,
        Width = 20,
        SchemeName = "dialogo"
    };

    var btnGuardar = new Button
    {
        Title = "_Guardar",
        IsDefault = true,
        SchemeName = "esquemabotones"
    };

    var btnCancelar = new Button
    {
        Title = "_Cancelar",
        SchemeName = "esquemabotones"
    };

    btnCancelar.Accepting += (s, e) =>
    {
        app.RequestStop();
        e.Handled = true;
    };

    btnGuardar.Accepting += async (s, e) =>
    {
        string codigoVal = txtcodigo.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(codigoVal))
        {
            MessageBox.ErrorQuery(app, "Error", "El código es obligatorio.", "OK");
            txtcodigo.SetFocus();
            return;
        }

        string nombreVal = txtnombre.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombreVal) || nombreVal.Length < 2)
        {
            MessageBox.ErrorQuery(app, "Error", "El nombre debe tener mas de 1 caracter.", "OK");
            txtnombre.SetFocus();
            return;
        }

        if (!decimal.TryParse(txtprecio.Text, out decimal precioVal) || precioVal < 0)
        {
            MessageBox.ErrorQuery(app, "Error", "El precio debe ser un numero positivo.", "OK");
            txtprecio.SetFocus();
            return;
        }

        if (!int.TryParse(txtstock.Text, out int stockVal) || stockVal < 0)
        {
            MessageBox.ErrorQuery(app, "Error", "El stock debe ser un numero entero mayor o igual a 0.", "OK");
            txtstock.SetFocus();
            return;
        }

        if (!int.TryParse(txtcant.Text, out int cantVal) || cantVal <= 0)
        {
            MessageBox.ErrorQuery(app, "Error", "La cantidad debe ser un numero entero mayor a 0.", "OK");
            txtcant.SetFocus();
            return;
        }

        string unidadmedidaVal = txtunidadmedida.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(unidadmedidaVal))
        {
            MessageBox.ErrorQuery(app, "Error", "La unidad de medida es requerida.", "OK");
            txtunidadmedida.SetFocus();
            return;
        }

        using var http = new HttpClient();

        if (productoAEditar is null)
        {
            // nuevo
            var nuevo = new ProductoDto(0, codigoVal, nombreVal, precioVal, stockVal, cantVal, unidadmedidaVal);
            await AgregarProd(http, nuevo);
        }
        else
        {
            // Actualizar 
            var actualizado = new ProductoDto(productoAEditar.Id, codigoVal, nombreVal, precioVal, stockVal, cantVal, unidadmedidaVal);
            await ActualizarProducto(http, productoAEditar.Id, actualizado);
        }

        // Refrescar lista principal
        productos = await ObtenerProductos(http);
        sourceabstraccion(productos);
        app.RequestStop();
        e.Handled = true;
    };

    dialogo.Add(codigo, txtcodigo, nombre, txtnombre, precio, txtprecio, stock, txtstock, cant, txtcant, unidadmedida, txtunidadmedida);
    dialogo.AddButton(btnGuardar);
    dialogo.AddButton(btnCancelar);

    app.Run(dialogo);
}

void DialogoMovimiento(int tipo)
{
    int indice = panelmaestro.SelectedItem ?? -1;
   
    if (indice < 0 || indice >= productos.Count)
    {
        MessageBox.ErrorQuery(app, "Error", "Debe seleccionar un producto del panel maestro.", "OK");
        return;
    }
    var prod = productos[indice];

    string titulo = tipo == 1 ? "Registrar Compra" : (tipo == 2 ? "Registrar Venta" : "Registrar Ajuste");

    var dialogo = new Dialog
    {
        X = Pos.Center(),
        Y = Pos.Center(),
        Width = 45,
        Height = 10,
        SchemeName = "dialogo",
        Title = titulo
    };

    dialogo.Border.LineStyle = LineStyle.Rounded;
    dialogo.Border.Thickness = new Thickness(1);

    var Cant = new Label
    {
        Text = "Cantidad:",
        X = 2,
        Y = 2
    };

    var txtCant = new TextField
    {
        Text = "",
        X = 15,
        Y = 2,
        Width = 20,
        SchemeName = "dialogo"
    };

    var btnGuardar = new Button
    {
        Title = "_Guardar",
        IsDefault = true,
        SchemeName = "esquemabotones"
    };

    var btnCancelar = new Button
    {
        Title = "_Cancelar",
        SchemeName = "esquemabotones"
    };

    btnCancelar.Accepting += (s, e) =>
    {
        app.RequestStop();
        e.Handled = true;
    };

    btnGuardar.Accepting += async (s, e) =>
    {
        if (!int.TryParse(txtCant.Text, out int cantidadVal) || (tipo != 3 && cantidadVal <= 0) || (tipo == 3 && cantidadVal < 0))
        {
            MessageBox.ErrorQuery(app, "Error", tipo == 3 ? "La cantidad debe ser mayor o igual a 0" : "La cantidad debe ser mayor a 0", "OK");
            txtCant.SetFocus();
            return;
        }

        if (tipo == 2 && prod.Stock < cantidadVal)
        {
            MessageBox.ErrorQuery(app, "Error", "Stock insuficiente", "OK");
            txtCant.SetFocus();
            return;
        }

        try
        {
            using var http = new HttpClient();
            var mov = new MovimientoDto(0, prod.Id, tipo, cantidadVal, DateTime.Now);
            string url = $"{url1}{prod.Id}/movimientos";
            var respuesta = await http.PostAsJsonAsync(url, mov);

            if (!respuesta.IsSuccessStatusCode)
            {
                MessageBox.ErrorQuery(app, "Error", "No se pudo registrar.", "OK");
                return;
            }

            // Recargar productos y actualizar detalle
            productos = await ObtenerProductos(http);
            sourceabstraccion(productos);
            await Refrescardetalle(indice);

            app.RequestStop();
            e.Handled = true;
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(app, "Error", $"Error de conexión: {ex.Message}", "OK");
        }
    };

    dialogo.Add(Cant, txtCant);
    dialogo.AddButton(btnGuardar);
    dialogo.AddButton(btnCancelar);

    app.Run(dialogo);
}


void FiltrarProductos()
{
    string busqueda = input.Text?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(busqueda))
    {
        sourceabstraccion(productos);
        return;
    }
    var productosfiltrados = productos
    .Where(p => p.Nombre.ToLower().Contains(busqueda)
    ||
    p.Id.ToString().Contains(busqueda))
    .ToList();

    sourceabstraccion(productosfiltrados);
}

void sourceabstraccion(List<ProductoDto> uso)
{
    panelmaestro.SetSource(new ObservableCollection<string>(uso
    .Select(p => $"{p.Id,-2} {p.Nombre,-10} {p.cant,3} {p.unidadmedida}")
    .ToList()
    ));
}

//Mostrar detalle
async Task Refrescardetalle(int? indice)
{

    if (indice is null || indice < 0 || indice > productos.Count)
    {
        detalle.Text = "Nada seleccionado";
        return;
    }
    var prodseleccionado = productos.FirstOrDefault(p => p.Id == productos[indice.Value].Id) ?? productos[indice.Value];

    List<MovimientoDto> movimientosDelProducto = new();
    try
    {
        using var http = new HttpClient();
        string url = $"http://localhost:3000/productos/{prodseleccionado.Id}/movimientos";
        movimientosDelProducto = await http.GetFromJsonAsync<List<MovimientoDto>>(url) ?? new();
    }
    catch (Exception ex)
    {
        detalle.Text = $"No se puedo obtener movimientos - Error : {ex.Message}";
        return;
    }

    listadetalles.Text = string.Join("\n\n", movimientosDelProducto.Select(m =>
    $"""    
        ID Movimiento: {m.Id}

        Tipo:          {m.Tipo switch
    {
        1 => "Compra",
        2 => "Venta",
        3 => "Ajuste",
        _ => "No asignado"
    }}

        Descripcion:   {m.ProductoId}

        Cantidad:      {(m.Tipo == 2 ? $"-{m.Cantidad}" : $"+{m.Cantidad}")}

        Stock : {prodseleccionado.Stock} unidades

        Fecha:         {m.Fecha}
        
    """));
}

async void Refrescar()
{
    try
    {
        using var http = new HttpClient();
        productos = await ObtenerProductos(http);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{ex.Message}");
        return;
    }

}


static async Task<List<ProductoDto>> ObtenerProductos(HttpClient http)
{
    return await http.GetFromJsonAsync<List<ProductoDto>>(url1) ?? throw new HttpRequestException("No hay productos");
}
static async Task<ProductoDto> TraerProducto(HttpClient http, int id)
{
    string url = $"{url1}{id}";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("No existe un producto con este ID");
}
static async Task<ProductoDto> AgregarProd(HttpClient http, ProductoDto nuevo)
{
    var respuesta = await http.PostAsJsonAsync(url1, nuevo);
    return await respuesta.Content.ReadFromJsonAsync<ProductoDto>() ?? throw new InvalidOperationException("No se pudo agregar el producto");

}
static async Task<ProductoDto> ActualizarProducto(HttpClient http, int id, ProductoDto actualizacion)
{
    string url = $"{url1}{id}";
    var respuesta = await http.PutAsJsonAsync(url, actualizacion);
    return await respuesta.Content.ReadFromJsonAsync<ProductoDto>() ?? throw new InvalidOperationException("No se pudo actualizar el producto");
}
static async Task<bool> EliminarProducto(HttpClient http, int id)
{
    string url = $"{url1}{id}";
    var respuesta = await http.DeleteAsync(url);
    return respuesta.IsSuccessStatusCode;
}


// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock, int cant, string unidadmedida);
record MovimientoDto(int Id, int ProductoId, int Tipo, int Cantidad, DateTime Fecha);
