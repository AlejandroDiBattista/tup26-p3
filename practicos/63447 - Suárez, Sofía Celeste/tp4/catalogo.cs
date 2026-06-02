#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Net.Http.Json;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Input;
using System.Collections.ObjectModel;

// ── Consulta inicial al servidor ──────────────────────────────────────────

ProductoDto producto;
try {
    using var http = new HttpClient();
    producto = await Ventana.CargarProductoAsync(http);
} catch (HttpRequestException ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Verificá que servidor.cs esté corriendo en http://localhost:5050");
    return;
}

// ── Interfaz TUI ──────────────────────────────────────────────────────────

using  IApplication app = Application.Create().Init();
var miventana=new Ventana(app);
app.Run(miventana);

class Ventana : Window
{
    private readonly IApplication app ;
    private ProductoDto producto = new ProductoDto(0, "", "", 0m, 0);
    private Window ventanaProducto;
    private Window ventanaMovimientos;
    private Label mensaje;
    private ListView listaHistorial;
    private Label lblDetalle;
    public Ventana(IApplication app)
    {
        this.app = app;
       ventanaProducto = new Window() { Title = "Producto", X = 0, Y = 2, Width = Dim.Percent(50), Height = Dim.Fill() };
       lblDetalle = new Label() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
       ventanaProducto.Add(lblDetalle);

       ventanaMovimientos = new Window() { Title = "Historial", X = Pos.Right(ventanaProducto), Y = 2, Width = Dim.Fill(), Height = Dim.Fill() };
       listaHistorial = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
       ventanaMovimientos.Add(listaHistorial);

       mensaje = new Label() { Text = "Bienvenido", X = 1, Y = 1, Width = Dim.Fill(), Height = 1 };

       Add(mensaje, ventanaProducto, ventanaMovimientos);

       CargarDatosIniciales();
        
        Menu.DefaultBorderStyle = LineStyle.Rounded;
        mensaje = new() {
        Text = "menu > opcion",
        X = 1, Y = 1,
        Width = Dim.Fill(2), Height = 1,
        };


        MenuBar menu = new(new MenuBarItem[] {

         new("productos", new MenuItem[] {
               new("_Agregar", "Agregar producto", () => agregarproducto(), Key.A.WithCtrl),
               new("_editar", "Editar producto",            () => editarproducto(), Key.L.WithCtrl),
               new("_eliminar", "Eliminar producto",          () => eliminarproducto(), Key.D.WithCtrl),
          }),
         new("movimientos", new MenuItem[] {
              new("_registrar compra", "registrar",    () => compradialogo(), Key.O.WithCtrl),
              new("_registrar venta", "registrar",    () => ventadialogo(), Key.N.WithCtrl),
           })
        })
        {
         X = 0, Y = 0,
         Width = Dim.Fill(), Height = 1,
        };
        Add(menu);
    }
    public static async Task<ProductoDto> CargarProductoAsync (HttpClient http) {
    const string url = "http://localhost:5050/producto";
    return await http.GetFromJsonAsync<ProductoDto>(url) ?? throw new HttpRequestException("El servidor devolvió un producto vacío");
    }

    private async void agregarproducto() {
      var dialogo = new Agregarproducto(producto);
      app.Run(dialogo);
      if (dialogo.Result is not null) 
        {
            using var http = new HttpClient();
            await http.PostAsJsonAsync("http://localhost:5050/productos", dialogo.Result);
            await Refrescarpantalla();
        }
    }
    private async void editarproducto() {
      var dialogo = new Editarproducto(producto);
      if (dialogo.Result is not null) 
        {
            try
            {
            using var http = new HttpClient();
            var response = await http.PutAsJsonAsync($"http://localhost:5050/productos/{producto.Id}", dialogo.Result);
            response.EnsureSuccessStatusCode(); // Lanza una excepción si el código de estado no es 2xx
            await Refrescarpantalla();  
            }catch(Exception ex)
            {
                mensaje.Text = $"Error al editar producto: {ex.Message}";
            }
            
        }
        app.Run(dialogo);
    }
    private async void eliminarproducto() {
      var dialogo = new Eliminarproducto(producto);
      app.Run(dialogo);
      if (dialogo.Result == true) 
        {
            try
            {
            using var http = new HttpClient();
            var response = await http.DeleteAsync($"http://localhost:5050/productos/{producto.Id}");
            response.EnsureSuccessStatusCode();
            await Refrescarpantalla();  
            } catch(Exception ex)
            {
                mensaje.Text = $"Error al eliminar producto: {ex.Message}";
            }
            
        }
    }
    private async void compradialogo() {
      var dialogo = new Compraproducto(producto);
      app.Run(dialogo);
      if (dialogo.Result is not null) 
        {
            using var http = new HttpClient();
            await http.PostAsJsonAsync($"http://localhost:5050/productos/{producto.Id}/movimientos", dialogo.Result);
            await Refrescarpantalla();
        }
    }
    private async void ventadialogo() {
      var dialogo = new Ventaproducto(producto);
      app.Run(dialogo);
      if (dialogo.Result is not null) 
        {
            using var http = new HttpClient();
            await http.PostAsJsonAsync($"http://localhost:5050/productos/{producto.Id}/movimientos", dialogo.Result);
            await Refrescarpantalla();
        }
    }  

    protected override bool OnKeyDown(Key key) {
        if (key == Key.S) 
        {
           agregarproducto();
           return true;
        }
        if (key== Key.L)
        {
            editarproducto();
            return true;
        }
        if (key == Key.D)
        {
            eliminarproducto();
            return true;
        }
        if (key == Key.O)
        {
            compradialogo();
            return true;
        }
        if (key == Key.N)
        {
            ventadialogo();
            return true;
        }
        return base.OnKeyDown(key);
    }

    async Task Refrescarpantalla()
    {
        try
        {
            using var http = new HttpClient();  
            var lista =await http.GetFromJsonAsync<List<ProductoDto>>("http://localhost:5050/productos");
            if (lista != null && lista.Count > 0)
            {
                producto = lista.First();
                lblDetalle.Text = $"""
                    # PRODUCTO 
                   - Id     : {producto.Id}
                   - Código : {producto.Codigo}
                   - Nombre : {producto.Nombre}
                   - Precio : ${producto.Precio,10:N2}
                   - Stock  :  {producto.Stock,10}
                  """;
                   
            }
            var movimientos = await http.GetFromJsonAsync<List<MovimientoDto>>($"http://localhost:5050/productos/{producto.Id}/movimientos");
            if (movimientos != null)
            {
              listaHistorial.Source = new ListWrapper<MovimientoDto>(new ObservableCollection<MovimientoDto>(movimientos));
            }
            
        }
        catch (Exception ex)
        {
            mensaje.Text = $"Error al conectar: {ex.Message}";
        }

    }

    private async void CargarDatosIniciales()
    {
    try 
    {
        using var http = new HttpClient();
        var prod = await http.GetFromJsonAsync<ProductoDto>("http://localhost:5050/producto");
        if (prod != null) {
            producto = prod;
            await Refrescarpantalla();
        }
    }
    catch (Exception ex)
    {
        mensaje.Text = $"Error al conectar: {ex.Message}";
    }
   }


};

class Agregarproducto : Dialog<ProductoDto>
{
    private ProductoDto producto = new ProductoDto(0, "", "", 0m, 0);
    public TextField codigoproducto {get;}
    public TextField nombreproducto {get;}
    public TextField precioproducto {get;}
    public TextField stockproducto {get;}
    private Label erroragregarproducto;
    public Agregarproducto(ProductoDto producto) 
    {
        Title = "Agregar producto";
        Width = Dim.Percent(35);
        Height = Dim.Percent(80);

        Add(new Label() { 
            Text = "Codigo:",
            X = 1, Y = 3 
        });
        codigoproducto = new TextField() { 
            X = 15, Y = 3, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(codigoproducto);

        Add(new Label() { 
            Text = "Nombre:",
            X = 1, Y = 5 
        });
        nombreproducto = new TextField() { 
            X = 15, Y = 5, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(nombreproducto);

        Add(new Label() { 
            Text = "Precio:",
            X = 1, Y = 7 
        });
        precioproducto = new TextField() { 
            X = 15, Y = 7, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(precioproducto);

        Add(new Label() { 
            Text = "Stock:",
            X = 1, Y = 9
        });
        stockproducto = new TextField() { 
            X = 15, Y =9, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(stockproducto);

        erroragregarproducto = new Label() { 
            Text = "",
            X = 1, Y = 13,
            Width = Dim.Fill(2), Height = 1,
        };

        Button cancelar = new Button() { 
            Text = "Cancelar", 
            X = Pos.Center(), Y = 11 
        };
        cancelar.Accepting += (_,_) => Result = null;

        Button guardar = new Button() { 
            Text = "Guardar", 
            X = Pos.Right(cancelar), Y = 11
        };
        guardar.Accepting += (_,e) => {
            if(String.IsNullOrEmpty(codigoproducto.Text))
            {
                e.Handled = true;
                erroragregarproducto.Text = "El código es obligatorio.";
                return;
            }
            if(String.IsNullOrEmpty(nombreproducto.Text))              
            {
                e.Handled = true;
                erroragregarproducto.Text = "El nombre es obligatorio.";
                return;
            }
            if(String.IsNullOrEmpty(precioproducto.Text))
            {
                e.Handled = true;
                erroragregarproducto.Text = "El precio es obligatorio.";
                return;
            }
            if(String.IsNullOrEmpty(stockproducto.Text))
            {
                e.Handled = true;
                erroragregarproducto.Text = "El stock es obligatorio.";
                return;
            }

            Result = new ProductoDto(
                0,
                codigoproducto.Text.ToString(),
                nombreproducto.Text.ToString(),
                decimal.Parse(precioproducto.Text.ToString()),
                int.Parse(stockproducto.Text.ToString())
            );
        };

        Add(cancelar);
        Add(guardar);
        Add(erroragregarproducto);

    }
}

class Editarproducto : Dialog<ProductoDto>
{
    public TextField idproducto {get;}
    public TextField codigoproducto {get;}
    public TextField nombreproducto {get;}
    public TextField precioproducto {get;}
    public TextField stockproducto {get;}
    private Label erroreditarproducto;
    public Editarproducto(ProductoDto producto) 
    {
        Title = "Editar producto";
        Width = Dim.Percent(35);
        Height = Dim.Percent(80);

        Add(new Label() { 
            Text = "Id:",
            X = 1, Y = 1 
        });
        idproducto = new TextField() { 
            X = 15, Y = 1, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(idproducto);

        Add(new Label() { 
            Text = "Codigo:",
            X = 1, Y = 3 
        });
        codigoproducto = new TextField() { 
            X = 15, Y = 3, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(codigoproducto);

        Add(new Label() { 
            Text = "Nombre:",
            X = 1, Y = 5 
        });
        nombreproducto = new TextField() { 
            X = 15, Y = 5, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(nombreproducto);

        Add(new Label() { 
            Text = "Precio:",
            X = 1, Y = 7 
        });
        precioproducto = new TextField() { 
            X = 15, Y = 7, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(precioproducto);

        Add(new Label() { 
            Text = "Stock:",
            X = 1, Y = 9
        });
        stockproducto = new TextField() { 
            X = 15, Y =9, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(stockproducto);

        erroreditarproducto = new Label() { 
            Text = "",
            X = 1, Y = 13,
            Width = Dim.Fill(), Height = 1,
        };

        Button cancelar = new Button() { 
            Text = "Cancelar", 
            X = Pos.Center(), Y = 11 
        };
        cancelar.Accepting += (_,_) => Result = null;

        Button guardar = new Button() { 
            Text = "Guardar", 
            X = Pos.Right(cancelar), Y = 11
        };
        guardar.Accepting += (_,e) => {
            if(string.IsNullOrEmpty(idproducto.Text))
            {
                e.Handled = true;
                erroreditarproducto.Text = "El ID es obligatorio.";
                return;
            }
            if(String.IsNullOrEmpty(codigoproducto.Text))
            {
                e.Handled = true;
                erroreditarproducto.Text = "El código es obligatorio.";
                return;
            }
            if(String.IsNullOrEmpty(nombreproducto.Text))              
            {
                e.Handled = true;
                erroreditarproducto.Text = "El nombre es obligatorio.";
                return;
            }
            if(String.IsNullOrEmpty(precioproducto.Text))
            {
                e.Handled = true;
                erroreditarproducto.Text = "El precio es obligatorio.";
                return;
            }
            if(String.IsNullOrEmpty(stockproducto.Text))
            {
                e.Handled = true;
                erroreditarproducto.Text = "El stock es obligatorio.";
                return;
            }

            Result = new ProductoDto(
                int.Parse(idproducto.Text.ToString()),
                codigoproducto.Text.ToString(),
                nombreproducto.Text.ToString(),
                decimal.Parse(precioproducto.Text.ToString()),
                int.Parse(stockproducto.Text.ToString())
            );
        };

        Add(cancelar);
        Add(guardar);
        Add(erroreditarproducto);

    }
}

class Compraproducto : Dialog<MovimientoDto>
{
    private TextField cantidadcompra;
    private Label errorcompraproducto;
    public Compraproducto(ProductoDto producto) 
    {
        Title = "Comprar producto";
        Width = Dim.Percent(35);
        Height = Dim.Percent(50);

        Add(new Label() { 
            Text = "Cantidad:",
            X = 1, Y = 2 
        });
        cantidadcompra = new TextField() { 
            X = 15, Y = 2, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(cantidadcompra);

        errorcompraproducto = new Label() { 
            Text = "",
            X = 1, Y = 6,
            Width = Dim.Fill(), Height = 1,
        };

        Button cancelar = new Button() { 
            Text = "Cancelar", 
            X = Pos.Center(), Y = 4
        };
        cancelar.Accepting += (_,_) => Result = null;

        Button guardar = new Button() { 
            Text = "Guardar", 
            X = Pos.Right(cancelar), Y = 4
        };
        guardar.Accepting += (_,e) => {
            if(int.TryParse(cantidadcompra.Text.ToString(), out int cant)&&cant>0)
            {
                Result = new MovimientoDto(0, producto.Id, TipoMovimiento.Compra, cant, DateTime.Now);
            }
            else
            {
                e.Handled = true;
                errorcompraproducto.Text = "La cantidad es obligatoria";
                return;
            }
    
        };

        Add(cancelar);
        Add(guardar);
        Add(errorcompraproducto);

    }
}

class Ventaproducto : Dialog<MovimientoDto>
{
    private TextField cantidadventa;
    private Label errorventaproducto;
    public Ventaproducto(ProductoDto producto) 
    {
        Title = "Venta de producto";
        Width = Dim.Percent(35);
        Height = Dim.Percent(50);

        Add(new Label() { 
            Text = "cantidad:",
            X = 1, Y = 2 
        });
        cantidadventa = new TextField() { 
            X = 15, Y = 2, 
            Width = Dim.Percent(50),
            Height = 1,
            Text=""
        };
        Add(cantidadventa);

        errorventaproducto = new Label() { 
            Text = "",
            X = 1, Y = 6,
            Width = Dim.Fill(), Height = 1,
        };

        Button cancelar = new Button() { 
            Text = "Cancelar", 
            X = Pos.Center(), Y = 4 
        };
        cancelar.Accepting += (_,_) => Result = null;

        Button guardar = new Button() { 
            Text = "Guardar", 
            X = Pos.Right(cancelar), Y = 4
        };
        guardar.Accepting += (_,e) => {
            if(int.TryParse(cantidadventa.Text.ToString(), out int cant)&&cant>0)
            {
                Result = new MovimientoDto(0, producto.Id, TipoMovimiento.Venta, cant, DateTime.Now);
            }
            else
            {
                e.Handled = true;
                errorventaproducto.Text = "La cantidad es obligatoria";
                return;
            }
    
        };
        Add(cancelar);
        Add(guardar);
        Add(errorventaproducto);

    }
}

class Eliminarproducto : Dialog<bool>
{
    public Eliminarproducto(ProductoDto producto) 
    {
        Title = "Eliminar producto";
        Width = Dim.Percent(35);
        Height = Dim.Percent(50);

        Add(new Label() { 
            Text = "¿esta seguro de que desea eliminar el producto?",
            X = 1, Y = 2,
            Width = Dim.Fill(), Height = 1,
        });

        Button cancelar = new Button() { 
            Text = "Cancelar", 
            X = Pos.Center(), Y = 4 
        };
        cancelar.Accepting += (_,_) => Result = false;

        Button aceptar = new Button() { 
            Text = "Aceptar", 
            X = Pos.Right(cancelar), Y = 4
        };
        aceptar.Accepting += (_,e) => {
            Result = true;
        };

        Add(cancelar);
        Add(aceptar);

    }
}



// ── DTO ───────────────────────────────────────────────────────────────────

record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public enum TipoMovimiento{Compra, Venta}
record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
