#!/usr/bin/env dotnet
#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using ApiClient api = new("http://localhost:5050");

try {
    await api.ObtenerProductosAsync();
}
catch (Exception ex) {
    Console.Error.WriteLine($"No se pudo conectar con el servidor: {ex.Message}");
    Console.Error.WriteLine("Primero ejecuta: dotnet run servidor.cs");
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(api));

public sealed class CatalogoWindow : Window {
    private readonly ApiClient api;
    private readonly List<ProductoDto> productos = [];
    private readonly List<ProductoDto> productosFiltrados = [];

    private TextField buscador = null!;
    private ListView listaProductos = null!;
    private Label detalleProducto = null!;
    private ListView listaMovimientos = null!;
    private StatusBar barraEstado = null!;
    private int indiceSeleccionado;

    public CatalogoWindow(ApiClient api) {
        this.api = api;
        Title = "Catalogo REST - Ctrl+Q para salir";
        Width = Dim.Fill();
        Height = Dim.Fill();

        CrearInterfaz();
        CargarProductos();
    }

    private void CrearInterfaz() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Productos", [
                    new MenuItem("_Agregar", "F2", AgregarProducto),
                    new MenuItem("_Editar", "F3", EditarProducto),
                    new MenuItem("_Eliminar", "Supr", EliminarProducto)
                ]),
                new MenuBarItem("_Stock", [
                    new MenuItem("_Registrar movimiento", "F5", RegistrarMovimiento)
                ]),
                new MenuBarItem("_Aplicacion", [
                    new MenuItem("_Actualizar", "F6", CargarProductos),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", Salir)
                ]),
                new MenuBarItem("A_yuda", [
                    new MenuItem("_Acerca de", null!, MostrarAyuda)
                ])
            ]
        };

        Label etiquetaBuscar = new() {
            Text = "Buscar:",
            X = 1,
            Y = 2,
            Width = 8
        };

        buscador = new TextField {
            X = Pos.Right(etiquetaBuscar) + 1,
            Y = 2,
            Width = Dim.Fill(1)
        };
        buscador.TextChanged += (_, _) => AplicarFiltro();

        FrameView panelProductos = new() {
            Title = "Productos",
            X = 0,
            Y = 4,
            Width = Dim.Percent(52),
            Height = Dim.Fill(1)
        };

        listaProductos = new ListView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        listaProductos.ValueChanged += (_, _) => {
            indiceSeleccionado = listaProductos.SelectedItem ?? 0;
            MostrarDetalle();
        };
        listaProductos.Accepting += (_, e) => {
            EditarProducto();
            e.Handled = true;
        };
        panelProductos.Add(listaProductos);

        FrameView panelDetalle = new() {
            Title = "Detalle e historial de movimientos",
            X = Pos.Right(panelProductos),
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        detalleProducto = new Label {
            X = 1,
            Y = 1,
            Width = Dim.Fill(1),
            Height = 6
        };

        Label etiquetaMovimientos = new() {
            Text = "Movimientos:",
            X = 1,
            Y = 7
        };

        listaMovimientos = new ListView {
            X = 1,
            Y = 8,
            Width = Dim.Fill(1),
            Height = Dim.Fill(1)
        };

        panelDetalle.Add(detalleProducto, etiquetaMovimientos, listaMovimientos);

        barraEstado = new StatusBar([
            new Shortcut(Key.F2, "Agregar", AgregarProducto),
            new Shortcut(Key.F3, "Editar", EditarProducto),
            new Shortcut(Key.DeleteChar, "Eliminar", EliminarProducto),
            new Shortcut(Key.F5, "Movimiento", RegistrarMovimiento),
            new Shortcut(Key.F6, "Actualizar", CargarProductos),
            new Shortcut(Key.Q.WithCtrl, "Salir", Salir)
        ]);

        Add(menu, etiquetaBuscar, buscador, panelProductos, panelDetalle, barraEstado);
    }

    private void CargarProductos() {
        try {
            int? idAnterior = ProductoSeleccionado()?.Id;
            productos.Clear();
            productos.AddRange(api.ObtenerProductosAsync().GetAwaiter().GetResult());

            AplicarFiltro();

            if (idAnterior is not null) {
                int nuevoIndice = productosFiltrados.FindIndex(p => p.Id == idAnterior);
                if (nuevoIndice >= 0) {
                    indiceSeleccionado = nuevoIndice;
                    listaProductos.SelectedItem = nuevoIndice;
                }
            }

            MostrarDetalle();
            CambiarEstado($"{productos.Count} productos cargados.");
        }
        catch (Exception ex) {
            MostrarError(ex.Message);
        }
    }

    private void AplicarFiltro() {
        string texto = buscador.Text.ToString()?.Trim() ?? "";
        ProductoDto? anterior = ProductoSeleccionado();

        productosFiltrados.Clear();
        productosFiltrados.AddRange(productos
            .Where(p => texto.Length == 0 ||
                p.Codigo.Contains(texto, StringComparison.CurrentCultureIgnoreCase) ||
                p.Nombre.Contains(texto, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(p => p.Nombre));

        listaProductos.SetSource(new ObservableCollection<string>(
            productosFiltrados.Select(FormatearProducto)));

        if (productosFiltrados.Count == 0) {
            indiceSeleccionado = 0;
            listaProductos.SelectedItem = null;
        }
        else {
            int indiceAnterior = anterior is null
                ? -1
                : productosFiltrados.FindIndex(p => p.Id == anterior.Id);
            indiceSeleccionado = indiceAnterior >= 0
                ? indiceAnterior
                : Math.Clamp(indiceSeleccionado, 0, productosFiltrados.Count - 1);
            listaProductos.SelectedItem = indiceSeleccionado;
        }

        MostrarDetalle();
    }

    private static string FormatearProducto(ProductoDto producto) =>
        $"{producto.Codigo,-8} {producto.Nombre,-25} ${producto.Precio,9:N2}  Stock: {producto.Stock}";

    private ProductoDto? ProductoSeleccionado() {
        if (indiceSeleccionado < 0 || indiceSeleccionado >= productosFiltrados.Count) {
            return null;
        }

        return productosFiltrados[indiceSeleccionado];
    }

    private void MostrarDetalle() {
        ProductoDto? producto = ProductoSeleccionado();
        if (producto is null) {
            detalleProducto.Text = "No hay productos para mostrar.";
            listaMovimientos.SetSource(new ObservableCollection<string>());
            return;
        }

        detalleProducto.Text =
            $"Codigo: {producto.Codigo}\n" +
            $"Nombre: {producto.Nombre}\n" +
            $"Precio: ${producto.Precio:N2}\n" +
            $"Stock actual: {producto.Stock}";

        try {
            List<MovimientoDto> movimientos = api
                .ObtenerMovimientosAsync(producto.Id)
                .GetAwaiter()
                .GetResult();

            IEnumerable<string> filas = movimientos.Count == 0
                ? ["Todavia no hay movimientos."]
                : movimientos.Select(m =>
                    $"{m.Fecha:dd/MM/yyyy HH:mm}  {m.Tipo,-7}  Cantidad: {m.Cantidad}");

            listaMovimientos.SetSource(new ObservableCollection<string>(filas));
        }
        catch (Exception ex) {
            listaMovimientos.SetSource(new ObservableCollection<string>(
                [$"No se pudo cargar el historial: {ex.Message}"]));
        }
    }

    // CREATE: abre un formulario y envia un POST /productos.
    private void AgregarProducto() {
        ProductoDialog dialogo = new("Agregar producto");
        App!.Run(dialogo);

        if (!dialogo.Aceptado || dialogo.Datos is null) {
            CambiarEstado("Alta cancelada.");
            return;
        }

        try {
            ProductoDto creado = api
                .CrearProductoAsync(dialogo.Datos)
                .GetAwaiter()
                .GetResult();
            CargarProductos();
            SeleccionarProducto(creado.Id);
            CambiarEstado($"Producto agregado: {creado.Nombre}");
        }
        catch (Exception ex) {
            MostrarError(ex.Message);
        }
    }

    // UPDATE: carga los datos seleccionados y envia un PUT /productos/{id}.
    private void EditarProducto() {
        ProductoDto? seleccionado = ProductoSeleccionado();
        if (seleccionado is null) {
            CambiarEstado("No hay un producto seleccionado.");
            return;
        }

        ProductoDialog dialogo = new("Editar producto", seleccionado);
        App!.Run(dialogo);

        if (!dialogo.Aceptado || dialogo.Datos is null) {
            CambiarEstado("Edicion cancelada.");
            return;
        }

        try {
            ProductoDto editado = api
                .EditarProductoAsync(seleccionado.Id, dialogo.Datos)
                .GetAwaiter()
                .GetResult();
            CargarProductos();
            SeleccionarProducto(editado.Id);
            CambiarEstado($"Producto actualizado: {editado.Nombre}");
        }
        catch (Exception ex) {
            MostrarError(ex.Message);
        }
    }

    // DELETE: pide confirmacion y envia un DELETE /productos/{id}.
    private void EliminarProducto() {
        ProductoDto? seleccionado = ProductoSeleccionado();
        if (seleccionado is null) {
            CambiarEstado("No hay un producto seleccionado.");
            return;
        }

        int? respuesta = MessageBox.Query(
            App!,
            "Confirmar eliminacion",
            $"Eliminar {seleccionado.Codigo} - {seleccionado.Nombre}?",
            "Si",
            "No");

        if (respuesta != 0) {
            CambiarEstado("Eliminacion cancelada.");
            return;
        }

        try {
            api.EliminarProductoAsync(seleccionado.Id).GetAwaiter().GetResult();
            CargarProductos();
            CambiarEstado($"Producto eliminado: {seleccionado.Nombre}");
        }
        catch (Exception ex) {
            MostrarError(ex.Message);
        }
    }

    private void RegistrarMovimiento() {
        ProductoDto? seleccionado = ProductoSeleccionado();
        if (seleccionado is null) {
            CambiarEstado("No hay un producto seleccionado.");
            return;
        }

        MovimientoDialog dialogo = new(seleccionado);
        App!.Run(dialogo);

        if (!dialogo.Aceptado || dialogo.Datos is null) {
            CambiarEstado("Movimiento cancelado.");
            return;
        }

        try {
            api.RegistrarMovimientoAsync(seleccionado.Id, dialogo.Datos)
                .GetAwaiter()
                .GetResult();
            CargarProductos();
            SeleccionarProducto(seleccionado.Id);
            CambiarEstado($"Movimiento registrado para {seleccionado.Nombre}.");
        }
        catch (Exception ex) {
            MostrarError(ex.Message);
        }
    }

    private void SeleccionarProducto(int id) {
        int indice = productosFiltrados.FindIndex(p => p.Id == id);
        if (indice < 0) {
            return;
        }

        indiceSeleccionado = indice;
        listaProductos.SelectedItem = indice;
        MostrarDetalle();
    }

    private void MostrarAyuda() {
        MessageBox.Query(
            App!,
            "Acerca de",
            "CatalogoREST - Trabajo Practico 4\n" +
            "F2: agregar | F3: editar | Supr: eliminar\n" +
            "F5: movimiento | F6: actualizar | Ctrl+Q: salir",
            "Aceptar");
    }

    private void CambiarEstado(string mensaje) {
        barraEstado.Text = mensaje;
    }

    private void MostrarError(string mensaje) {
        MessageBox.ErrorQuery(App!, "Error", mensaje, "Aceptar");
        CambiarEstado("La operacion no pudo completarse.");
    }

    private void Salir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            Salir();
            return true;
        }

        if (key == Key.F2) {
            AgregarProducto();
            return true;
        }

        if (key == Key.F3 || key == Key.Enter) {
            EditarProducto();
            return true;
        }

        if (key == Key.DeleteChar) {
            EliminarProducto();
            return true;
        }

        if (key == Key.F5) {
            RegistrarMovimiento();
            return true;
        }

        if (key == Key.F6) {
            CargarProductos();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class ProductoDialog : Dialog {
    private readonly TextField campoCodigo;
    private readonly TextField campoNombre;
    private readonly TextField campoPrecio;
    private readonly TextField campoStock;

    public bool Aceptado { get; private set; }
    public ProductoDatos? Datos { get; private set; }

    public ProductoDialog(string titulo, ProductoDto? producto = null) {
        Title = titulo;
        Width = 65;
        Height = 16;

        campoCodigo = AgregarCampo("Codigo:", 1, producto?.Codigo ?? "");
        campoNombre = AgregarCampo("Nombre:", 3, producto?.Nombre ?? "");
        campoPrecio = AgregarCampo("Precio:", 5, producto?.Precio.ToString(CultureInfo.CurrentCulture) ?? "");
        campoStock = AgregarCampo("Stock:", 7, producto?.Stock.ToString() ?? "0");

        Label ayuda = new() {
            Text = "Use numeros sin simbolo $ para precio y stock.",
            X = 2,
            Y = 9,
            Width = Dim.Fill(2)
        };
        Add(ayuda);

        Button guardar = new() {
            Text = "_Guardar",
            IsDefault = true
        };
        guardar.Accepting += (_, e) => {
            if (!Validar()) {
                e.Handled = true;
                return;
            }

            Aceptado = true;
            RequestStop();
            e.Handled = true;
        };

        Button cancelar = new() { Text = "_Cancelar" };
        cancelar.Accepting += (_, e) => {
            RequestStop();
            e.Handled = true;
        };

        AddButton(guardar);
        AddButton(cancelar);
    }

    private TextField AgregarCampo(string etiqueta, int y, string valor) {
        Label label = new() {
            Text = etiqueta,
            X = 2,
            Y = y,
            Width = 10
        };

        TextField campo = new() {
            Text = valor,
            X = 13,
            Y = y,
            Width = Dim.Fill(2)
        };

        Add(label, campo);
        return campo;
    }

    private bool Validar() {
        string codigo = campoCodigo.Text.ToString()?.Trim() ?? "";
        string nombre = campoNombre.Text.ToString()?.Trim() ?? "";
        string precioTexto = campoPrecio.Text.ToString()?.Trim() ?? "";
        string stockTexto = campoStock.Text.ToString()?.Trim() ?? "";

        if (codigo.Length == 0) {
            return Error("El codigo es obligatorio.", campoCodigo);
        }

        if (nombre.Length == 0) {
            return Error("El nombre es obligatorio.", campoNombre);
        }

        if (!IntentarDecimal(precioTexto, out decimal precio) || precio < 0) {
            return Error("El precio debe ser un numero mayor o igual a cero.", campoPrecio);
        }

        if (!int.TryParse(stockTexto, out int stock) || stock < 0) {
            return Error("El stock debe ser un entero mayor o igual a cero.", campoStock);
        }

        Datos = new ProductoDatos(codigo, nombre, precio, stock);
        return true;
    }

    private static bool IntentarDecimal(string texto, out decimal valor) {
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out valor) ||
               decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    private bool Error(string mensaje, View campo) {
        MessageBox.ErrorQuery(App!, "Error de validacion", mensaje, "Aceptar");
        campo.SetFocus();
        return false;
    }
}

public sealed class MovimientoDialog : Dialog {
    private static readonly string[] Tipos = ["Compra", "Venta", "Ajuste"];
    private readonly ListView selectorTipo;
    private readonly TextField campoCantidad;

    public bool Aceptado { get; private set; }
    public MovimientoDatos? Datos { get; private set; }

    public MovimientoDialog(ProductoDto producto) {
        Title = $"Movimiento - {producto.Nombre}";
        Width = 58;
        Height = 15;

        Label stockActual = new() {
            Text = $"Stock actual: {producto.Stock}",
            X = 2,
            Y = 1
        };

        Label etiquetaTipo = new() {
            Text = "Tipo:",
            X = 2,
            Y = 3
        };

        selectorTipo = new ListView {
            X = 12,
            Y = 3,
            Width = 18,
            Height = 3
        };
        selectorTipo.SetSource(new ObservableCollection<string>(Tipos));
        selectorTipo.SelectedItem = 0;

        Label etiquetaCantidad = new() {
            Text = "Cantidad:",
            X = 2,
            Y = 7
        };

        campoCantidad = new TextField {
            Text = "1",
            X = 12,
            Y = 7,
            Width = 12
        };

        Label explicacion = new() {
            Text = "Compra suma, venta resta y ajuste fija el stock.",
            X = 2,
            Y = 9,
            Width = Dim.Fill(2)
        };

        Add(stockActual, etiquetaTipo, selectorTipo, etiquetaCantidad, campoCantidad, explicacion);

        Button registrar = new() {
            Text = "_Registrar",
            IsDefault = true
        };
        registrar.Accepting += (_, e) => {
            if (!int.TryParse(campoCantidad.Text.ToString(), out int cantidad) || cantidad <= 0) {
                MessageBox.ErrorQuery(
                    App!,
                    "Error de validacion",
                    "La cantidad debe ser un entero mayor que cero.",
                    "Aceptar");
                campoCantidad.SetFocus();
                e.Handled = true;
                return;
            }

            int tipoSeleccionado = selectorTipo.SelectedItem ?? 0;
            Datos = new MovimientoDatos(Tipos[tipoSeleccionado], cantidad);
            Aceptado = true;
            RequestStop();
            e.Handled = true;
        };

        Button cancelar = new() { Text = "_Cancelar" };
        cancelar.Accepting += (_, e) => {
            RequestStop();
            e.Handled = true;
        };

        AddButton(registrar);
        AddButton(cancelar);
    }
}

public sealed class ApiClient : IDisposable {
    private readonly HttpClient http;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(string urlBase) {
        http = new HttpClient {
            BaseAddress = new Uri(urlBase),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<List<ProductoDto>> ObtenerProductosAsync() =>
        await http.GetFromJsonAsync<List<ProductoDto>>("/productos", jsonOptions) ?? [];

    public async Task<List<MovimientoDto>> ObtenerMovimientosAsync(int productoId) =>
        await http.GetFromJsonAsync<List<MovimientoDto>>(
            $"/productos/{productoId}/movimientos",
            jsonOptions) ?? [];

    public async Task<ProductoDto> CrearProductoAsync(ProductoDatos datos) {
        HttpResponseMessage respuesta = await http.PostAsJsonAsync("/productos", datos, jsonOptions);
        return await LeerRespuestaAsync<ProductoDto>(respuesta);
    }

    public async Task<ProductoDto> EditarProductoAsync(int id, ProductoDatos datos) {
        HttpResponseMessage respuesta = await http.PutAsJsonAsync($"/productos/{id}", datos, jsonOptions);
        return await LeerRespuestaAsync<ProductoDto>(respuesta);
    }

    public async Task EliminarProductoAsync(int id) {
        HttpResponseMessage respuesta = await http.DeleteAsync($"/productos/{id}");
        await VerificarRespuestaAsync(respuesta);
    }

    public async Task RegistrarMovimientoAsync(int productoId, MovimientoDatos datos) {
        HttpResponseMessage respuesta = await http.PostAsJsonAsync(
            $"/productos/{productoId}/movimientos",
            datos,
            jsonOptions);
        await VerificarRespuestaAsync(respuesta);
    }

    private async Task<T> LeerRespuestaAsync<T>(HttpResponseMessage respuesta) {
        await VerificarRespuestaAsync(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>(jsonOptions)
            ?? throw new ApiException("El servidor devolvio una respuesta vacia.");
    }

    private static async Task VerificarRespuestaAsync(HttpResponseMessage respuesta) {
        if (respuesta.IsSuccessStatusCode) {
            return;
        }

        string contenido = await respuesta.Content.ReadAsStringAsync();
        string mensaje = contenido;

        try {
            ErrorDto? error = JsonSerializer.Deserialize<ErrorDto>(
                contenido,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (!string.IsNullOrWhiteSpace(error?.Error)) {
                mensaje = error.Error;
            }
        }
        catch (JsonException) {
            // Si el servidor no devuelve JSON, se muestra el contenido original.
        }

        if (respuesta.StatusCode == HttpStatusCode.Conflict) {
            mensaje = $"Dato repetido: {mensaje}";
        }

        throw new ApiException(mensaje);
    }

    public void Dispose() {
        http.Dispose();
    }
}

public sealed class ApiException(string message) : Exception(message);

public sealed record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public sealed record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, DateTime Fecha);
public sealed record ProductoDatos(string Codigo, string Nombre, decimal Precio, int Stock);
public sealed record MovimientoDatos(string Tipo, int Cantidad);
public sealed record ErrorDto(string Error);
