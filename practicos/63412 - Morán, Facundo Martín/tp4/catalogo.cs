    #:package Terminal.Gui@2.*
    #:property PublishAot=false

    using System.Collections.ObjectModel;
    using System.Net.Http.Json;
    using Terminal.Gui.App;
    using Terminal.Gui.Views;

    // ── Consulta inicial al servidor ──────────────────────────────────────────

    var api = new CatalogoApi();

    List<ProductoDto> productos;
    try {
        productos = await api.ListarProductosAsync();
    } catch (HttpRequestException ex) {
        Console.WriteLine($"Error al conectar con el servidor: {ex.Message}");
        return;
    }
    // ── Interfaz TUI ──────────────────────────────────────────────────────────

    using IApplication app = Application.Create().Init();
    var ventana = new CatalogoWindow(productos);
    app.Run(ventana);
    sealed class CatalogoWindow : Window
    {
        public CatalogoWindow(List<ProductoDto> productos)
        {
            Title = $" Catalogo REST — {productos.Count} productos cargados ";

            var api = new CatalogoApi();
            var filtrados = productos;

            var menu = new Label
    {
        X = 1,
        Y = 0,
        Text = "Acciones"
    };

    Add(menu);

            Add(new Label { X = 1, Y = 2, Text = "Buscar:" });
            var buscar = new TextField { X = 9, Y = 2, Width = 35 };
            Add(buscar);

            var listaProductos = new ListView
            {
                X = 1,
                Y = 6,
                Width = 46,
                Height = 7
            };
            var detalle = new Label
    {
        X = 50,
        Y = 14,
        Text = "Seleccione un producto"
    };
            var informacion = new Label
            {
                X = 1,
                Y = 14,
                Text = ""
            };
            var estado = new Label
            {
                X = 1,
                Y = 15,
                Text = ""
            };
            var agregar = new Button    
    {
        X = 1,
        Y = 3,
        Text = "Agregar"
    };
    agregar.Accepting += async (_, _) =>
    {
        var producto = ProductoDialog.Mostrar(App!, null);

        if (producto is not null)
        {
            await api.CrearProductoAsync(producto);
            productos = await api.ListarProductosAsync();
            RefrescarLista();
        }
    };

    var modificar = new Button
    {
        X = 13,
        Y = 3,
        Text = "Modificar"
    };

    var eliminar = new Button
    {
        X = 29,
        Y = 3,
        Text = "Eliminar"
    };
    modificar.Accepting += async (_, _) =>
    {
        if (listaProductos.SelectedItem is not int indice || indice < 0 || indice >= filtrados.Count)
        {
            estado.Text = "Seleccione un producto para modificar.";
            return;
        }

        var producto = filtrados[indice];
        var request = ProductoDialog.Mostrar(App!, producto);
        if (request is null)
            return;

        await api.ModificarProductoAsync(producto.Id, request);
        productos = await api.ListarProductosAsync();
        estado.Text = "Producto modificado.";
        RefrescarLista();
    };

    eliminar.Accepting += async (_, _) =>
    {
        if (listaProductos.SelectedItem is not int indice || indice < 0 || indice >= filtrados.Count)
        {
            estado.Text = "Seleccione un producto para eliminar.";
            return;
        }

        var producto = filtrados[indice];
        if ((MessageBox.Query(App!, "Eliminar", $"Eliminar {producto.Nombre}?", "No", "Si") ?? 0) != 1)
            return;

        await api.EliminarProductoAsync(producto.Id);
        productos = await api.ListarProductosAsync();
        estado.Text = "Producto eliminado.";
        RefrescarLista();
    };

    var compra = new Button
    {
        X = 1,
        Y = 4,
        Text = "Compra"
    };

    var venta = new Button
    {
        X = 13,
        Y = 4,
        Text = "Venta"
    };

    var ajuste = new Button
    {
        X = 25,
        Y = 4,
        Text = "Ajuste"
    };
    compra.Accepting += async (_, _) =>
    {
        if (listaProductos.SelectedItem is not int indice)
        {
            estado.Text = "Seleccione un producto para registrar compra.";
            return;
        }

        if (indice < 0 || indice >= filtrados.Count)
        {
            estado.Text = "Seleccione un producto para registrar compra.";
            return;
        }

        var producto = filtrados[indice];
        var cantidad = CantidadDialog.Mostrar(App!, TipoMovimiento.Compra);
        if (cantidad is null)
            return;

        await api
            .RegistrarMovimientoAsync(
                producto.Id,
                TipoMovimiento.Compra,
                cantidad.Value);

        productos = await api.ListarProductosAsync();
        estado.Text = $"Compra registrada para {producto.Nombre}.";
        RefrescarLista();

        Console.WriteLine(
            $"Compra registrada para {producto.Nombre}");
    };

    venta.Accepting += async (_, _) =>
    {
        if (listaProductos.SelectedItem is not int indice)
        {
            estado.Text = "Seleccione un producto para registrar venta.";
            return;
        }

        if (indice < 0 || indice >= filtrados.Count)
        {
            estado.Text = "Seleccione un producto para registrar venta.";
            return;
        }

        var producto = filtrados[indice];
        var cantidad = CantidadDialog.Mostrar(App!, TipoMovimiento.Venta);
        if (cantidad is null)
            return;

        await api
            .RegistrarMovimientoAsync(
                producto.Id,
                TipoMovimiento.Venta,
                cantidad.Value);

        productos = await api.ListarProductosAsync();
        estado.Text = $"Venta registrada para {producto.Nombre}.";
        RefrescarLista();

        Console.WriteLine(
            $"Venta registrada para {producto.Nombre}");
    };

    ajuste.Accepting += async (_, _) =>
    {
        if (listaProductos.SelectedItem is not int indice)
        {
            estado.Text = "Seleccione un producto para registrar ajuste.";
            return;
        }

        if (indice < 0 || indice >= filtrados.Count)
        {
            estado.Text = "Seleccione un producto para registrar ajuste.";
            return;
        }

        var producto = filtrados[indice];
        var cantidad = CantidadDialog.Mostrar(App!, TipoMovimiento.Ajuste);
        if (cantidad is null)
            return;

        await api
            .RegistrarMovimientoAsync(
                producto.Id,
                TipoMovimiento.Ajuste,
                cantidad.Value);

        productos = await api.ListarProductosAsync();
        estado.Text = $"Ajuste registrado para {producto.Nombre}.";
        RefrescarLista();

        Console.WriteLine(
            $"Ajuste registrado para {producto.Nombre}");
    };

            void RefrescarLista()
            {
                var texto = buscar.Text?.ToString()?.Trim() ?? "";
                filtrados = string.IsNullOrWhiteSpace(texto)
                    ? productos
                    : productos.Where(p =>
                        p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                        p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();

                listaProductos.SetSource(
                    new ObservableCollection<string>(
                        filtrados.Select(p =>
                            $"{p.Codigo,-10} {p.Nombre,-25} ${p.Precio,10:N2} Stock:{p.Stock}")
                        .ToList()));
                informacion.Text = $"Productos encontrados: {filtrados.Count}";
            }

            buscar.TextChanged += (_, _) => RefrescarLista();
            RefrescarLista();

            var historial = new ListView
    {
        X = 50,
        Y = 6,
        Width = 38,
        Height = 7
    };

    historial.SetSource(
        new ObservableCollection<string>(
            new List<string>
            {
                "Sin movimientos cargados"
            }));
            listaProductos.Accepting += async (_, _) =>
    {
        if (listaProductos.SelectedItem is not int indice)
            return;

        if (indice < 0 || indice >= filtrados.Count)
            return;

        var p = filtrados[indice];

        detalle.Text =
            $"Código: {p.Codigo}\n" +
            $"Nombre: {p.Nombre}\n" +
            $"Precio: ${p.Precio:N2}\n" +
            $"Stock: {p.Stock}";

        var movimientos = await api.ListarMovimientosAsync(p.Id);
        historial.SetSource(new ObservableCollection<string>(
            movimientos.Count == 0
                ? ["Sin movimientos cargados"]
                : movimientos.Select(m =>
                    $"{m.Fecha:dd/MM HH:mm} {m.Tipo,-7} {m.Cantidad,5}")
                .ToList()));
    };

            


            Add(listaProductos);
            Add(historial);
            Add(detalle);
            Add(informacion);
            Add(estado);
            Add(agregar);
            Add(modificar);
            Add(eliminar);
            Add(compra);
            Add(venta);
            Add(ajuste);
        }
        
    }
    static class ProductoDialog
    {
        public static ProductoRequest? Mostrar(IApplication app, ProductoDto? producto)
        {
            ProductoRequest? resultado = null;
            var d = new Dialog { Title = producto is null ? "Agregar producto" : "Modificar producto", Width = 55, Height = 13 };
            var codigo = Campo(d, "Codigo:", producto?.Codigo ?? "", 1);
            var nombre = Campo(d, "Nombre:", producto?.Nombre ?? "", 3);
            var precio = Campo(d, "Precio:", producto?.Precio.ToString() ?? "", 5);
            var stock = Campo(d, "Stock:", producto?.Stock.ToString() ?? "", 7);

            var aceptar = new Button { Text = "Aceptar" };
            aceptar.Accepting += (_, _) =>
            {
                if (!decimal.TryParse(precio.Text.ToString(), out var precioValor))
                    return;
                if (!int.TryParse(stock.Text.ToString(), out var stockValor))
                    return;

                resultado = new ProductoRequest(
                    codigo.Text.ToString() ?? "",
                    nombre.Text.ToString() ?? "",
                    precioValor,
                    stockValor);
                d.App!.RequestStop();
            };

            var cancelar = new Button { Text = "Cancelar" };
            cancelar.Accepting += (_, _) => d.App!.RequestStop();

            d.AddButton(aceptar);
            d.AddButton(cancelar);
            app.Run(d);
            return resultado;
        }

        static TextField Campo(Dialog d, string etiqueta, string valor, int y)
        {
            var campo = new TextField { X = 12, Y = y, Width = 35, Text = valor };
            d.Add(new Label { X = 2, Y = y, Text = etiqueta }, campo);
            return campo;
        }
    }

    static class CantidadDialog
    {
        public static int? Mostrar(IApplication app, TipoMovimiento tipo)
        {
            int? resultado = null;
            var d = new Dialog { Title = $"Movimiento: {tipo}", Width = 45, Height = 8 };
            var cantidad = new TextField { X = 12, Y = 1, Width = 20 };
            d.Add(new Label { X = 2, Y = 1, Text = "Cantidad:" }, cantidad);

            var aceptar = new Button { Text = "Aceptar" };
            aceptar.Accepting += (_, _) =>
            {
                if (!int.TryParse(cantidad.Text.ToString(), out var valor) || valor <= 0)
                    return;

                resultado = valor;
                d.App!.RequestStop();
            };

            var cancelar = new Button { Text = "Cancelar" };
            cancelar.Accepting += (_, _) => d.App!.RequestStop();

            d.AddButton(aceptar);
            d.AddButton(cancelar);
            app.Run(d);
            return resultado;
        }
    }
    sealed class CatalogoApi
    {
        private readonly HttpClient http = new()
        {
            BaseAddress = new Uri("http://localhost:5050")
        };

        public async Task<List<ProductoDto>> ListarProductosAsync()
        {
            return await http.GetFromJsonAsync<List<ProductoDto>>("/productos")
                ?? [];
        }
        public async Task CrearProductoAsync(ProductoRequest producto)
    {
        await http.PostAsJsonAsync("/productos", producto);
    }

    public async Task ModificarProductoAsync(int id, ProductoRequest producto)
    {
        await http.PutAsJsonAsync($"/productos/{id}", producto);
    }

    public async Task EliminarProductoAsync(int id)
    {
        await http.DeleteAsync($"/productos/{id}");
    }
    public async Task<List<MovimientoDto>> ListarMovimientosAsync(int productoId)
    {
        return await http.GetFromJsonAsync<List<MovimientoDto>>(
            $"/productos/{productoId}/movimientos")
            ?? [];
            
    }
    public async Task RegistrarMovimientoAsync(
        int productoId,
        TipoMovimiento tipo,
        int cantidad)
    {
        var movimiento = new
        {
            Tipo = tipo,
            Cantidad = cantidad
        };

        await http.PostAsJsonAsync(
            $"/productos/{productoId}/movimientos",
            movimiento);
    }
    }
    // ── DTO ───────────────────────────────────────────────────────────────────

    record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
    sealed record ProductoRequest(
        string Codigo,
        string Nombre,
        decimal Precio,
        int Stock);

            enum TipoMovimiento
    {
        Compra,
        Venta,
        Ajuste
    }

    record MovimientoDto(
        int ProductoId,
        TipoMovimiento Tipo,
        int Cantidad,
        DateTime Fecha);
