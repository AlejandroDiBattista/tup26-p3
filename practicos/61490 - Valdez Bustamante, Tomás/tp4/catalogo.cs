#:package Terminal.Gui@1.17.1
#:property PublishAot=false

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using Terminal.Gui;

enum TipoMovimiento { Compra, Venta, Ajuste }

class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }

    // para mostrar en la lista
    public override string ToString() =>
        $"{Codigo,-12} {Nombre,-25} ${Precio,8:F2}  Stock: {Stock}";
}

class Movimiento
{
    public int Id { get; set; }
    public int ProductoId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TipoMovimiento Tipo { get; set; }

    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }

    public override string ToString() =>
        $"{Fecha:dd/MM/yyyy HH:mm}  {Tipo,-8}  Cant: {Cantidad}";
}

static class Api
{
    static readonly HttpClient http = new() { BaseAddress = new Uri("http://localhost:5050") };

    static readonly JsonSerializerOptions opciones = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<List<Producto>> ObtenerProductos()
    {
        try { return await http.GetFromJsonAsync<List<Producto>>("/productos", opciones) ?? []; }
        catch { return []; }
    }

    public static async Task<List<Movimiento>> ObtenerMovimientos(int productoId)
    {
        try { return await http.GetFromJsonAsync<List<Movimiento>>($"/productos/{productoId}/movimientos", opciones) ?? []; }
        catch { return []; }
    }

    public static async Task<(bool ok, string mensaje)> CrearProducto(Producto p)
        => await EnviarYLeer(() => http.PostAsJsonAsync("/productos", p));

    public static async Task<(bool ok, string mensaje)> ModificarProducto(Producto p)
        => await EnviarYLeer(() => http.PutAsJsonAsync($"/productos/{p.Id}", p));

    public static async Task<(bool ok, string mensaje)> EliminarProducto(int id)
        => await EnviarYLeer(() => http.DeleteAsync($"/productos/{id}"));

    public static async Task<(bool ok, string mensaje)> RegistrarMovimiento(int productoId, Movimiento m)
        => await EnviarYLeer(() => http.PostAsJsonAsync($"/productos/{productoId}/movimientos", m, opciones));

    static async Task<(bool ok, string mensaje)> EnviarYLeer(Func<Task<HttpResponseMessage>> llamada)
    {
        try
        {
            var respuesta = await llamada();
            if (respuesta.IsSuccessStatusCode) return (true, "Operación exitosa.");
            var error = await respuesta.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }
}

class Program
{
    static async Task Main()
    {
        Application.Init();

        var productos = new List<Producto>();
        string busqueda = "";

        var ventana = new Window
        {
            Title = "Productos de almacen",
            X = 0, Y = 1,
            Width = Dim.Fill(), Height = Dim.Fill()
        };

        // panel izquierdo: lista de productos
        var etiquetaBusqueda = new Label { Text = "Buscar:", X = 1, Y = 1 };
        var campoBusqueda = new TextField { X = Pos.Right(etiquetaBusqueda) + 1, Y = 1, Width = 30 };
        var encabezadoLista = new Label { Text = $"{"Código",-12} {"Nombre",-25} {"Precio",9}  Stock", X = 1, Y = 2 };
        var listaProductos = new ListView { X = 1, Y = 3, Width = Dim.Percent(55), Height = Dim.Fill() - 1 };

        // panel derecho: movimientos del producto seleccionado
        var etiquetaMovs = new Label { Text = "Historial de movimientos:", X = Pos.Percent(57), Y = 1 };
        var encabezadoMovs = new Label { Text = $"{"Fecha",-18} {"Tipo",-8} Cantidad", X = Pos.Percent(57), Y = 2 };
        var listaMovs = new ListView { X = Pos.Percent(57), Y = 3, Width = Dim.Fill() - 1, Height = Dim.Fill() - 1 };

        ventana.Add(etiquetaBusqueda, campoBusqueda,
                    encabezadoLista, listaProductos,
                    etiquetaMovs, encabezadoMovs, listaMovs);

        List<Producto> Filtrados() =>
            string.IsNullOrWhiteSpace(busqueda)
                ? productos
                : productos.Where(p =>
                    p.Codigo.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).ToList();

        void RefrescarLista()
        {
            listaProductos.SetSource(new ObservableCollection<string>(Filtrados().Select(p => p.ToString())));
        }

        Producto? ProductoActual()
        {
            var lista = Filtrados();
            int i = listaProductos.SelectedItem;
            return (i >= 0 && i < lista.Count) ? lista[i] : null;
        }

        void Mensaje(string titulo, string texto) =>
            MessageBox.Query(titulo, texto, "OK");

        void ActualizarTodo() =>
            Application.MainLoop.Invoke(async () =>
            {
                productos = await Api.ObtenerProductos();
                RefrescarLista();
                listaMovs.SetSource(new ObservableCollection<string>());
            });

        // actulizar movimientos al cambiar de producto seleccionado
        listaProductos.SelectedItemChanged += _ =>
        {
            var p = ProductoActual();
            if (p is not null)
                Application.MainLoop.Invoke(async () =>
                {
                    var movs = await Api.ObtenerMovimientos(p.Id);
                    listaMovs.SetSource(new ObservableCollection<string>(movs.Select(m => m.ToString())));
                });
        };

        campoBusqueda.TextChanged += _ =>
        {
            busqueda = campoBusqueda.Text?.ToString() ?? "";
            RefrescarLista();
        };

        void AbrirDialogoAgregar()
        {
            var dialogo = new Dialog { Title = "Agregar producto", Width = 50, Height = 14 };

            var campoCodigo = new TextField { X = 14, Y = 1, Width = 28 };
            var campoNombre = new TextField { X = 14, Y = 3, Width = 28 };
            var campoPrecio = new TextField { X = 14, Y = 5, Width = 28, Text = "0.00" };
            var campoStock = new TextField { X = 14, Y = 7, Width = 28, Text = "0" };

            dialogo.Add(new Label { Text = "Codigo:", X = 2, Y = 1 }, campoCodigo,
                        new Label { Text = "Nombre:", X = 2, Y = 3 }, campoNombre,
                        new Label { Text = "Precio:", X = 2, Y = 5 }, campoPrecio,
                        new Label { Text = "Stock:", X = 2, Y = 7 }, campoStock);

            var btnAceptar = new Button { Text = "Aceptar", X = Pos.Center() - 8, Y = 10, IsDefault = true };
            var btnCancelar = new Button { Text = "Cancelar", X = Pos.Center() + 2, Y = 10 };

            btnAceptar.Clicked += async () =>
            {
                string codigo = campoCodigo.Text?.ToString()?.Trim() ?? "";
                string nombre = campoNombre.Text?.ToString()?.Trim() ?? "";
                decimal.TryParse(campoPrecio.Text?.ToString(), out decimal precio);
                int.TryParse(campoStock.Text?.ToString(), out int stock);

                if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(nombre))
                { Mensaje("Error", "Codigo y nombre son obligatorios."); return; }

                var (ok, msg) = await Api.CrearProducto(new Producto { Codigo = codigo, Nombre = nombre, Precio = precio, Stock = stock });
                Application.RequestStop(dialogo);
                Mensaje(ok ? "OK" : "Error", msg);
                if (ok) ActualizarTodo();
            };

            btnCancelar.Clicked += () => Application.RequestStop(dialogo);
            dialogo.Add(btnAceptar, btnCancelar);
            Application.Run(dialogo);
        }

        void AbrirDialogoModificar()
        {
            var p = ProductoActual();
            if (p is null) { Mensaje("Aviso", "Seleccione un producto."); return; }

            var dialogo = new Dialog { Title = "Modificar producto", Width = 50, Height = 14 };

            var campoCodigo = new TextField { X = 14, Y = 1, Width = 28, Text = p.Codigo };
            var campoNombre = new TextField { X = 14, Y = 3, Width = 28, Text = p.Nombre };
            var campoPrecio = new TextField { X = 14, Y = 5, Width = 28, Text = p.Precio.ToString("F2") };
            var campoStock = new TextField { X = 14, Y = 7, Width = 28, Text = p.Stock.ToString() };

            dialogo.Add(new Label { Text = "Codigo:", X = 2, Y = 1 }, campoCodigo,
                        new Label { Text = "Nombre:", X = 2, Y = 3 }, campoNombre,
                        new Label { Text = "Precio:", X = 2, Y = 5 }, campoPrecio,
                        new Label { Text = "Stock:", X = 2, Y = 7 }, campoStock);

            var btnAceptar = new Button { Text = "Aceptar", X = Pos.Center() - 8, Y = 10, IsDefault = true };
            var btnCancelar = new Button { Text = "Cancelar", X = Pos.Center() + 2, Y = 10 };

            btnAceptar.Clicked += async () =>
            {
                string codigo = campoCodigo.Text?.ToString()?.Trim() ?? "";
                string nombre = campoNombre.Text?.ToString()?.Trim() ?? "";
                decimal.TryParse(campoPrecio.Text?.ToString(), out decimal precio);
                int.TryParse(campoStock.Text?.ToString(), out int stock);

                if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(nombre))
                { Mensaje("Error", "Codigo y nombre son obligatorios."); return; }

                var (ok, msg) = await Api.ModificarProducto(new Producto { Id = p.Id, Codigo = codigo, Nombre = nombre, Precio = precio, Stock = stock });
                Application.RequestStop(dialogo);
                Mensaje(ok ? "OK" : "Error", msg);
                if (ok) ActualizarTodo();
            };

            btnCancelar.Clicked += () => Application.RequestStop(dialogo);
            dialogo.Add(btnAceptar, btnCancelar);
            Application.Run(dialogo);
        }

        void EliminarSeleccionado()
        {
            var p = ProductoActual();
            if (p is null) { Mensaje("Aviso", "Seleccione un producto."); return; }

            if (MessageBox.Query("Confirmar", $"¿Eliminar '{p.Nombre}'?", "Sí", "No") != 0) return;

            Application.MainLoop.Invoke(async () =>
            {
                var (ok, msg) = await Api.EliminarProducto(p.Id);
                Mensaje(ok ? "OK" : "Error", msg);
                if (ok) ActualizarTodo();
            });
        }

        void AbrirDialogoMovimiento(TipoMovimiento tipo)
        {
            var p = ProductoActual();
            if (p is null) { Mensaje("Aviso", "Seleccione un producto."); return; }

            var dialogo = new Dialog { Title = $"Registrar {tipo} — {p.Nombre}", Width = 45, Height = 10 };

            var campoCantidad = new TextField { X = 13, Y = 1, Width = 12, Text = "1" };
            var btnAceptar = new Button { Text = "Aceptar", X = Pos.Center() - 8, Y = 5, IsDefault = true };
            var btnCancelar = new Button { Text = "Cancelar", X = Pos.Center() + 2, Y = 5 };

            btnAceptar.Clicked += async () =>
            {
                if (!int.TryParse(campoCantidad.Text?.ToString(), out int cantidad) || cantidad <= 0)
                { Mensaje("Error", "Ingrese una cantidad válida mayor a cero."); return; }

                var (ok, msg) = await Api.RegistrarMovimiento(p.Id, new Movimiento { Tipo = tipo, Cantidad = cantidad });
                Application.RequestStop(dialogo);
                Mensaje(ok ? "OK" : "Error", msg);
                if (ok) ActualizarTodo();
            };

            btnCancelar.Clicked += () => Application.RequestStop(dialogo);
            dialogo.Add(new Label { Text = "Cantidad:", X = 2, Y = 1 }, campoCantidad, btnAceptar, btnCancelar);
            Application.Run(dialogo);
        }

        var barraEstado = new StatusBar([
            new StatusItem(Key.F5, "F5 Actualizar", () => ActualizarTodo()),
            new StatusItem((Key)'a', "A Agregar", () => AbrirDialogoAgregar()),
            new StatusItem((Key)'m', "M Modificar", () => AbrirDialogoModificar()),
            new StatusItem(Key.DeleteChar, "Del Eliminar", () => EliminarSeleccionado()),
            new StatusItem((Key)'c', "C Compra", () => AbrirDialogoMovimiento(TipoMovimiento.Compra)),
            new StatusItem((Key)'v', "V Venta", () => AbrirDialogoMovimiento(TipoMovimiento.Venta)),
            new StatusItem((Key)'j', "J Ajuste", () => AbrirDialogoMovimiento(TipoMovimiento.Ajuste)),
            new StatusItem((Key)'q', "Q Salir", () => Application.RequestStop()),
        ]);

        var menuBar = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_Productos", [
                    new MenuItem("_Agregar   [A]", "", () => AbrirDialogoAgregar()),
                    new MenuItem("_Modificar [M]", "", () => AbrirDialogoModificar()),
                    new MenuItem("_Eliminar  [Del]", "", () => EliminarSeleccionado()),
                    new MenuItem("_Actualizar [F5]", "", () => ActualizarTodo()),
                ]),
                new MenuBarItem("_Movimientos", [
                    new MenuItem("_Compra [C]", "", () => AbrirDialogoMovimiento(TipoMovimiento.Compra)),
                    new MenuItem("_Venta  [V]", "", () => AbrirDialogoMovimiento(TipoMovimiento.Venta)),
                    new MenuItem("_Ajuste [J]", "", () => AbrirDialogoMovimiento(TipoMovimiento.Ajuste)),
                ]),
                new MenuBarItem("_Salir", [
                    new MenuItem("_Salir [Q]", "", () => Application.RequestStop()),
                ]),
            ]
        };

        Application.Top.Add(menuBar, ventana, barraEstado);

        ActualizarTodo();
        Application.Run();
        Application.Shutdown();
    }
}
