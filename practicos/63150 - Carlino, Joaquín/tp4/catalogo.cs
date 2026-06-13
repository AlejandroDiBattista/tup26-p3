#:package Terminal.Gui@1.17.1
#:property PublishAot=false

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using Terminal.Gui;

class Program
{
    static async Task Main()
    {
        Application.Init();

        var articulos = new List<Articulo>();
        string filtro = "";

        var panel = new Window
        {
            Title = "Catalogo de productos",
            X = 0, Y = 1,
            Width = Dim.Fill(), Height = Dim.Fill()
        };

        //lista de productos
        var vistaArticulos = new ListView { X = 0, Y = 0, Width = Dim.Percent(55), Height = Dim.Fill() - 1 };
        var vistaOperaciones = new ListView { X = Pos.Percent(56), Y = 0, Width = Dim.Fill(), Height = Dim.Fill() - 1 };

        // buscador
        var lblFiltro = new Label { Text = "Buscar:", X = 0, Y = Pos.AnchorEnd(1) };
        var txtFiltro = new TextField { X = Pos.Right(lblFiltro) + 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill() };

        panel.Add(vistaArticulos, vistaOperaciones, lblFiltro, txtFiltro);

        List<Articulo> Coincidencias() =>
            string.IsNullOrWhiteSpace(filtro)
                ? articulos
                : articulos.Where(a =>
                    a.Clave.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    a.Descripcion.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

        void PintarArticulos()
        {
            vistaArticulos.SetSource(new ObservableCollection<string>(Coincidencias().Select(a => a.ToString())));
        }

        Articulo? Seleccionado()
        {
            var vigentes = Coincidencias();
            int idx = vistaArticulos.SelectedItem;
            return (idx >= 0 && idx < vigentes.Count) ? vigentes[idx] : null;
        }

        void Avisar(string titulo, string texto) =>
            MessageBox.Query(titulo, texto, "OK");

        void RecargarTodo() =>
            Application.MainLoop.Invoke(async () =>
            {
                articulos = await Servicio.ListarProductos();
                PintarArticulos();
                vistaOperaciones.SetSource(new ObservableCollection<string>());
            });

        void DialogoAlta()
        {
            var dlg = new Dialog { Title = "Agregar producto", Width = 50, Height = 14 };

            var inClave = new TextField { X = 14, Y = 1, Width = 28 };
            var inDescripcion = new TextField { X = 14, Y = 3, Width = 28 };
            var inImporte = new TextField { X = 14, Y = 5, Width = 28, Text = "0.00" };
            var inExistencia = new TextField { X = 14, Y = 7, Width = 28, Text = "0" };

            dlg.Add(new Label { Text = "Codigo:", X = 2, Y = 1 }, inClave,
                    new Label { Text = "Nombre:", X = 2, Y = 3 }, inDescripcion,
                    new Label { Text = "Precio:", X = 2, Y = 5 }, inImporte,
                    new Label { Text = "Stock:", X = 2, Y = 7 }, inExistencia);

            var btnOk = new Button { Text = "Aceptar", X = Pos.Center() - 8, Y = 10, IsDefault = true };
            var btnNo = new Button { Text = "Cancelar", X = Pos.Center() + 2, Y = 10 };

            btnOk.Clicked += async () =>
            {
                string clave = inClave.Text?.ToString()?.Trim() ?? "";
                string descripcion = inDescripcion.Text?.ToString()?.Trim() ?? "";
                decimal.TryParse(inImporte.Text?.ToString(), out decimal importe);
                int.TryParse(inExistencia.Text?.ToString(), out int existencia);

                if (string.IsNullOrEmpty(clave) || string.IsNullOrEmpty(descripcion))
                { Avisar("Error", "Codigo y nombre son obligatorios."); return; }

                    var (exito, detalle) = await Servicio.AltaProducto(new Articulo { Clave = clave, Descripcion = descripcion, Importe = importe, Existencia = existencia });
                Application.RequestStop(dlg);
                Avisar(exito ? "OK" : "Error", detalle);
                if (exito) RecargarTodo();
            };

            btnNo.Clicked += () => Application.RequestStop(dlg);
            dlg.Add(btnOk, btnNo);
            Application.Run(dlg);
        }

        void DialogoEdicion()
        {
            var sel = Seleccionado();
            if (sel is null) { Avisar("Aviso", "Seleccione un producto."); return; }

            var dlg = new Dialog { Title = "Modificar producto", Width = 50, Height = 14 };

            var inClave = new TextField { X = 14, Y = 1, Width = 28, Text = sel.Clave };
            var inDescripcion = new TextField { X = 14, Y = 3, Width = 28, Text = sel.Descripcion };
            var inImporte = new TextField { X = 14, Y = 5, Width = 28, Text = sel.Importe.ToString("F2") };
            var inExistencia = new TextField { X = 14, Y = 7, Width = 28, Text = sel.Existencia.ToString() };

            dlg.Add(new Label { Text = "Codigo:", X = 2, Y = 1 }, inClave,
                    new Label { Text = "Nombre:", X = 2, Y = 3 }, inDescripcion,
                    new Label { Text = "Precio:", X = 2, Y = 5 }, inImporte,
                    new Label { Text = "Stock:", X = 2, Y = 7 }, inExistencia);

            var btnOk = new Button { Text = "Aceptar", X = Pos.Center() - 8, Y = 10, IsDefault = true };
            var btnNo = new Button { Text = "Cancelar", X = Pos.Center() + 2, Y = 10 };

            btnOk.Clicked += async () =>
            {
                string clave = inClave.Text?.ToString()?.Trim() ?? "";
                string descripcion = inDescripcion.Text?.ToString()?.Trim() ?? "";
                decimal.TryParse(inImporte.Text?.ToString(), out decimal importe);
                int.TryParse(inExistencia.Text?.ToString(), out int existencia);

                if (string.IsNullOrEmpty(clave) || string.IsNullOrEmpty(descripcion))
                { Avisar("Error", "Codigo y nombre son obligatorios."); return; }

                var (exito, detalle) = await Servicio.EditarProducto(new Articulo { Id = sel.Id, Clave = clave, Descripcion = descripcion, Importe = importe, Existencia = existencia });
                Application.RequestStop(dlg);
                Avisar(exito ? "OK" : "Error", detalle);
                if (exito) RecargarTodo();
            };

            btnNo.Clicked += () => Application.RequestStop(dlg);
            dlg.Add(btnOk, btnNo);
            Application.Run(dlg);
        }

        void BorrarSeleccion()
        {
            var sel = Seleccionado();
            if (sel is null) { Avisar("Aviso", "Seleccione un producto."); return; }

            if (MessageBox.Query("Confirmar", $"¿Eliminar '{sel.Descripcion}'?", "Sí", "No") != 0) return;

            Application.MainLoop.Invoke(async () =>
            {
                var (exito, detalle) = await Servicio.BajaProducto(sel.Id);
                Avisar(exito ? "OK" : "Error", detalle);
                if (exito) RecargarTodo();
            });
        }

        void DialogoOperacion(ClaseOperacion clase)
        {
            var sel = Seleccionado();
            if (sel is null) { Avisar("Aviso", "Seleccione un producto."); return; }

            var dlg = new Dialog { Title = $"Registrar {clase} — {sel.Descripcion}", Width = 45, Height = 10 };

            var inUnidades = new TextField { X = 13, Y = 1, Width = 12, Text = "1" };
            var btnOk = new Button { Text = "Aceptar", X = Pos.Center() - 8, Y = 5, IsDefault = true };
            var btnNo = new Button { Text = "Cancelar", X = Pos.Center() + 2, Y = 5 };

            btnOk.Clicked += async () =>
            {
                if (!int.TryParse(inUnidades.Text?.ToString(), out int unidades) || unidades <= 0)
                { Avisar("Error", "Ingrese una cantidad válida mayor a cero."); return; }

                var (exito, detalle) = await Servicio.RegistrarMovimiento(sel.Id, new Operacion { Clase = clase, Unidades = unidades });
                Application.RequestStop(dlg);
                Avisar(exito ? "OK" : "Error", detalle);
                if (exito) RecargarTodo();
            };

            btnNo.Clicked += () => Application.RequestStop(dlg);
            dlg.Add(new Label { Text = "Cantidad:", X = 2, Y = 1 }, inUnidades, btnOk, btnNo);
            Application.Run(dlg);
        }

        // actualizar movimientos
        vistaArticulos.SelectedItemChanged += _ =>
        {
            var sel = Seleccionado();
            if (sel is not null)
                Application.MainLoop.Invoke(async () =>
                {
                    var ops = await Servicio.ListarMovimientos(sel.Id);
                    vistaOperaciones.SetSource(new ObservableCollection<string>(ops.Select(o => o.ToString())));
                });
        };

        txtFiltro.TextChanged += _ =>
        {
            filtro = txtFiltro.Text?.ToString() ?? "";
            PintarArticulos();
        };

        var barra = new StatusBar([
            new StatusItem(Key.F5, "F5 Actualizar", () => RecargarTodo()),
            new StatusItem((Key)'a', "A Agregar", () => DialogoAlta()),
            new StatusItem((Key)'m', "M Modificar", () => DialogoEdicion()),
            new StatusItem(Key.DeleteChar, "Del Eliminar", () => BorrarSeleccion()),
            new StatusItem((Key)'e', "E Entrada", () => DialogoOperacion(ClaseOperacion.Entrada)),
            new StatusItem((Key)'s', "S Salida", () => DialogoOperacion(ClaseOperacion.Salida)),
            new StatusItem((Key)'r', "R Recargar", () => DialogoOperacion(ClaseOperacion.Regularizacion)),
            new StatusItem((Key)'q', "Q Salir", () => Application.RequestStop()),
        ]);

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_Productos", [
                    new MenuItem("_Agregar   [A]", "", () => DialogoAlta()),
                    new MenuItem("_Modificar [M]", "", () => DialogoEdicion()),
                    new MenuItem("_Eliminar  [Del]", "", () => BorrarSeleccion()),
                    new MenuItem("_Actualizar [F5]", "", () => RecargarTodo()),
                ]),
                new MenuBarItem("_Movimientos", [
                    new MenuItem("_Entrada [E]", "", () => DialogoOperacion(ClaseOperacion.Entrada)),
                    new MenuItem("_Salida  [S]", "", () => DialogoOperacion(ClaseOperacion.Salida)),
                    new MenuItem("_Regularizar [R]", "", () => DialogoOperacion(ClaseOperacion.Regularizacion)),
                ]),
                new MenuBarItem("_Salir", [
                    new MenuItem("_Salir [Q]", "", () => Application.RequestStop()),
                ]),
            ]
        };

        Application.Top.Add(menu, panel, barra);

        RecargarTodo();
        Application.Run();
        Application.Shutdown();
    }
}

static class Servicio
{
    static readonly HttpClient cliente = new() { BaseAddress = new Uri("http://localhost:5050") };

    static readonly JsonSerializerOptions formato = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    static async Task<(bool exito, string detalle)> EjecutarRequest(Func<Task<HttpResponseMessage>> accion)
    {
        try
        {
            var resp = await accion();
            if (resp.IsSuccessStatusCode) return (true, "Operación exitosa.");
            var cuerpoError = await resp.Content.ReadAsStringAsync();
            return (false, cuerpoError);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public static async Task<List<Articulo>> ListarProductos()
    {
        try { return await cliente.GetFromJsonAsync<List<Articulo>>("/productos", formato) ?? []; }
        catch { return []; }
    }

    public static async Task<List<Operacion>> ListarMovimientos(int articuloId)
    {
        try { return await cliente.GetFromJsonAsync<List<Operacion>>($"/productos/{articuloId}/movimientos", formato) ?? []; }
        catch { return []; }
    }

    public static async Task<(bool exito, string detalle)> AltaProducto(Articulo a)
        => await EjecutarRequest(() => cliente.PostAsJsonAsync("/productos", a));

    public static async Task<(bool exito, string detalle)> EditarProducto(Articulo a)
        => await EjecutarRequest(() => cliente.PutAsJsonAsync($"/productos/{a.Id}", a));

    public static async Task<(bool exito, string detalle)> BajaProducto(int id)
        => await EjecutarRequest(() => cliente.DeleteAsync($"/productos/{id}"));

    public static async Task<(bool exito, string detalle)> RegistrarMovimiento(int articuloId, Operacion o)
        => await EjecutarRequest(() => cliente.PostAsJsonAsync($"/productos/{articuloId}/movimientos", o, formato));
}

class Operacion
{
    public int Id { get; set; }
    public int ArticuloId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaseOperacion Clase { get; set; }

    public int Unidades { get; set; }
    public DateTime Momento { get; set; }

    public override string ToString() =>
        $"{Momento:dd/MM/yyyy HH:mm}  {Clase,-14}  Cant: {Unidades}";
}

class Articulo
{
    public int Id { get; set; }
    public string Clave { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Importe { get; set; }
    public int Existencia { get; set; }

    // mostrar lista
    public override string ToString() =>
        $"{Clave,-12} {Descripcion,-25} ${Importe,8:F2}  Exist: {Existencia}";
}

enum ClaseOperacion { Entrada, Salida, Regularizacion }
