#:package Terminal.Gui@2.*
#:property PublishAot=false

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ── Interfaz TUI ──────────────────────────────────────────────────────────

Console.OutputEncoding = Encoding.UTF8;
Menu.DefaultBorderStyle = LineStyle.Single;

using IApplication app = Application.Create().Init();
using CatalogoApi api = new("http://localhost:5050");

app.Run(new CatalogoWindow(api));

// ── Ventana principal ─────────────────────────────────────────────────────

public sealed class CatalogoWindow : Runnable {
    private readonly CatalogoApi _api;
    private readonly ListView _productList = new();
    private readonly TextField _searchField = new();
    private readonly TextView _detailView = new();
    private readonly Label _statusLabel = new();

    private List<ProductoDto> _products = new();
    private List<ProductoDto> _filteredProducts = new();

    public CatalogoWindow(CatalogoApi api) {
        _api = api;

        Title = "CatalogoREST - TP4";
        Width = Dim.Fill();
        Height = Dim.Fill();

        BuildLayout();
        LoadProducts();
        _searchField.SetFocus();
    }

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Actualizar", "F5", () => LoadProducts()),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", () => App!.RequestStop())
                ]),
                new MenuBarItem("_Productos", [
                    new MenuItem("_Nuevo", "F2", NewProduct),
                    new MenuItem("_Editar", "F3", EditProduct),
                    new MenuItem("E_liminar", "Del", DeleteProduct)
                ]),
                new MenuBarItem("_Movimientos", [
                    new MenuItem("_Registrar movimiento", "F6", RegisterMovement)
                ]),
                new MenuBarItem("A_yuda", [
                    new MenuItem("Acerca _de", "", About)
                ])
            ]
        };

        Label searchLabel = new() { Text = "Buscar:", X = 1, Y = 1 };

        _searchField.X = 10;
        _searchField.Y = 1;
        _searchField.Width = Dim.Fill(2);
        _searchField.CanFocus = true;
        _searchField.ValueChanged += (_, _) => RefreshProductList();
        _searchField.KeyDown += (_, key) => {
            if (key == Key.Enter || key == Key.Tab) {
                key.Handled = true;
                _productList.SetFocus();
            }
        };

        FrameView masterPanel = new() {
            Title = "Productos",
            X = 0,
            Y = 3,
            Width = Dim.Percent(45),
            Height = Dim.Fill(2)
        };
        masterPanel.BorderStyle = LineStyle.Single;

        _productList.X = 0;
        _productList.Y = 0;
        _productList.Width = Dim.Fill();
        _productList.Height = Dim.Fill();
        _productList.CanFocus = true;
        _productList.ValueChanged += (_, _) => UpdateDetail();
        _productList.Activated += (_, _) => EditProduct();
        _productList.KeyDown += (_, key) => {
            if (key == Key.Enter) {
                key.Handled = true;
                EditProduct();
            } else if (key == Key.Delete) {
                key.Handled = true;
                DeleteProduct();
            }
        };
        masterPanel.Add(_productList);

        FrameView detailPanel = new() {
            Title = "Detalle e historial",
            X = Pos.Right(masterPanel),
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        detailPanel.BorderStyle = LineStyle.Single;

        _detailView.X = 0;
        _detailView.Y = 0;
        _detailView.Width = Dim.Fill();
        _detailView.Height = Dim.Fill();
        _detailView.ReadOnly = true;
        detailPanel.Add(_detailView);

        _statusLabel.X = 1;
        _statusLabel.Y = Pos.AnchorEnd(1);
        _statusLabel.Width = Dim.Fill();
        _statusLabel.Text = "Listo.";

        Add(menu, searchLabel, _searchField, masterPanel, detailPanel, _statusLabel);
    }

    private void LoadProducts(int? selectedId = null) {
        try {
            _products = _api.GetProducts();
            RefreshProductList(selectedId);
            SetStatus($"{_products.Count} producto(s) cargado(s) desde el servidor.");
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Servidor", $"No se pudo conectar con el servidor: {ex.Message}", "Ok");
            SetStatus("Servidor no disponible. Ejecutá servidor.cs en http://localhost:5050");
        }
    }

    private void RefreshProductList(int? selectedId = null) {
        string search = _searchField.Text?.ToString()?.Trim() ?? "";

        _filteredProducts = _products
            .Where(p => MatchesFilter(p, search))
            .OrderBy(p => p.Codigo)
            .ToList();

        List<string> rows = _filteredProducts
            .Select(p => $"{p.Codigo,-8} {TrimTo(p.Nombre, 24),-24} ${p.Precio,9:N2}  Stock: {p.Stock,4}")
            .ToList();

        _productList.SetSource(new ObservableCollection<string>(rows));

        if (selectedId.HasValue) {
            int index = _filteredProducts.FindIndex(p => p.Id == selectedId.Value);
            if (index >= 0) {
                _productList.SelectedItem = index;
            }
        } else if (_filteredProducts.Count > 0) {
            _productList.SelectedItem = 0;
        }

        UpdateDetail();
    }

    private static bool MatchesFilter(ProductoDto product, string search) {
        if (string.IsNullOrWhiteSpace(search)) return true;

        return product.Codigo.Contains(search, StringComparison.OrdinalIgnoreCase)
            || product.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateDetail() {
        ProductoDto? selected = GetSelectedProduct();
        if (selected is null) {
            _detailView.Text = _filteredProducts.Count == 0
                ? "No hay productos para mostrar."
                : "Ningún producto seleccionado.";
            return;
        }

        try {
            List<MovimientoDto> movements = _api.GetMovements(selected.Id);
            string movementRows = movements.Count == 0
                ? "Sin movimientos registrados."
                : string.Join(Environment.NewLine, movements.Select(FormatMovement));

            _detailView.Text = $"""
                Producto seleccionado
                ---------------------
                Id:      {selected.Id}
                Código:  {selected.Codigo}
                Nombre:  {selected.Nombre}
                Precio:  ${selected.Precio:N2}
                Stock:   {selected.Stock}

                Historial de movimientos
                ------------------------
                {movementRows}
                """;
        } catch (Exception ex) {
            _detailView.Text = $"No se pudo cargar el historial: {ex.Message}";
        }
    }

    private ProductoDto? GetSelectedProduct() {
        int index = _productList.SelectedItem ?? -1;
        if (index >= 0 && index < _filteredProducts.Count) {
            return _filteredProducts[index];
        }

        return null;
    }

    private void NewProduct() {
        ProductDialog dialog = new("Nuevo producto", new ProductoRequest("", "", 0m, 0));
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Product is null) return;

        try {
            ProductoDto created = _api.CreateProduct(dialog.Product);
            LoadProducts(created.Id);
            SetStatus($"Producto '{created.Nombre}' creado.");
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Crear producto", ex.Message, "Ok");
        }
    }

    private void EditProduct() {
        ProductoDto? selected = GetSelectedProduct();
        if (selected is null) {
            MessageBox.Query(App!, "Editar", "Seleccioná un producto.", "Ok");
            return;
        }

        ProductDialog dialog = new("Editar producto", new ProductoRequest(selected.Codigo, selected.Nombre, selected.Precio, selected.Stock));
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Product is null) return;

        try {
            ProductoDto updated = _api.UpdateProduct(selected.Id, dialog.Product);
            LoadProducts(updated.Id);
            SetStatus($"Producto '{updated.Nombre}' actualizado.");
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Editar producto", ex.Message, "Ok");
        }
    }

    private void DeleteProduct() {
        ProductoDto? selected = GetSelectedProduct();
        if (selected is null) {
            MessageBox.Query(App!, "Eliminar", "Seleccioná un producto.", "Ok");
            return;
        }

        int answer = MessageBox.Query(
            App!,
            "Eliminar producto",
            $"¿Eliminar el producto '{selected.Nombre}'?",
            "No", "Sí") ?? 0;

        if (answer != 1) return;

        try {
            _api.DeleteProduct(selected.Id);
            LoadProducts();
            SetStatus($"Producto '{selected.Nombre}' eliminado.");
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Eliminar producto", ex.Message, "Ok");
        }
    }

    private void RegisterMovement() {
        ProductoDto? selected = GetSelectedProduct();
        if (selected is null) {
            MessageBox.Query(App!, "Movimiento", "Seleccioná un producto.", "Ok");
            return;
        }

        MovementDialog dialog = new(selected);
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Movement is null) return;

        try {
            _api.CreateMovement(selected.Id, dialog.Movement);
            LoadProducts(selected.Id);
            SetStatus($"Movimiento registrado para '{selected.Nombre}'.");
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Registrar movimiento", ex.Message, "Ok");
        }
    }

    private void About() {
        MessageBox.Query(
            App!,
            "Acerca de",
            """
            CatalogoREST - Trabajo Práctico 4
            TUI con API REST, SQLite y EF Core.

            Paz, Naim Federico - Legajo 61581
            """,
            "Ok");
    }

    private void SetStatus(string message) {
        _statusLabel.Text = message;
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            App!.RequestStop();
            return true;
        }
        if (key == Key.N.WithCtrl || key == Key.F2) {
            NewProduct();
            return true;
        }
        if (key == Key.F3) {
            EditProduct();
            return true;
        }
        if (key == Key.Delete) {
            DeleteProduct();
            return true;
        }
        if (key == Key.F5) {
            LoadProducts(GetSelectedProduct()?.Id);
            return true;
        }
        if (key == Key.F6) {
            RegisterMovement();
            return true;
        }

        return base.OnKeyDown(key);
    }

    private static string FormatMovement(MovimientoDto movement) =>
        $"{movement.Fecha:dd/MM/yyyy HH:mm}  {movement.Tipo,-7}  Cantidad: {movement.Cantidad,5}";

    private static string TrimTo(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}

// ── Diálogos ──────────────────────────────────────────────────────────────

public sealed class ProductDialog : Dialog {
    private readonly TextField _codeField;
    private readonly TextField _nameField;
    private readonly TextField _priceField;
    private readonly TextField _stockField;

    public bool Saved { get; private set; }
    public ProductoRequest? Product { get; private set; }

    public ProductDialog(string title, ProductoRequest product) {
        Title = title;
        Width = 60;
        Height = 14;

        Label codeLabel = new() { Text = "Código *:", X = 2, Y = 1 };
        _codeField = new() {
            Text = product.Codigo,
            X = 15,
            Y = 1,
            Width = Dim.Fill(4),
            CanFocus = true
        };

        Label nameLabel = new() { Text = "Nombre *:", X = 2, Y = 3 };
        _nameField = new() {
            Text = product.Nombre,
            X = 15,
            Y = 3,
            Width = Dim.Fill(4),
            CanFocus = true
        };

        Label priceLabel = new() { Text = "Precio *:", X = 2, Y = 5 };
        _priceField = new() {
            Text = product.Precio.ToString("0.##", CultureInfo.CurrentCulture),
            X = 15,
            Y = 5,
            Width = 15,
            CanFocus = true
        };

        Label stockLabel = new() { Text = "Stock *:", X = 2, Y = 7 };
        _stockField = new() {
            Text = product.Stock.ToString(CultureInfo.CurrentCulture),
            X = 15,
            Y = 7,
            Width = 15,
            CanFocus = true
        };

        Add(codeLabel, _codeField, nameLabel, _nameField, priceLabel, _priceField, stockLabel, _stockField);

        Button saveButton = new() { Text = "Guardar", IsDefault = true };
        saveButton.Accepting += (_, e) => {
            e.Handled = true;
            if (TrySave()) App!.RequestStop();
        };

        Button cancelButton = new() { Text = "Cancelar" };
        cancelButton.Accepting += (_, e) => {
            Saved = false;
            e.Handled = true;
            App!.RequestStop();
        };

        AddButton(saveButton);
        AddButton(cancelButton);
        _codeField.SetFocus();
    }

    private bool TrySave() {
        string code = _codeField.Text?.ToString()?.Trim() ?? "";
        string name = _nameField.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(code)) {
            MessageBox.ErrorQuery(App!, "Validación", "El código es obligatorio.", "Ok");
            _codeField.SetFocus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(name)) {
            MessageBox.ErrorQuery(App!, "Validación", "El nombre es obligatorio.", "Ok");
            _nameField.SetFocus();
            return false;
        }

        if (!TryReadDecimal(_priceField.Text?.ToString(), out decimal price) || price < 0) {
            MessageBox.ErrorQuery(App!, "Validación", "El precio debe ser un número mayor o igual a cero.", "Ok");
            _priceField.SetFocus();
            return false;
        }

        if (!int.TryParse(_stockField.Text?.ToString(), out int stock) || stock < 0) {
            MessageBox.ErrorQuery(App!, "Validación", "El stock debe ser un entero mayor o igual a cero.", "Ok");
            _stockField.SetFocus();
            return false;
        }

        Product = new ProductoRequest(code, name, price, stock);
        Saved = true;
        return true;
    }

    private static bool TryReadDecimal(string? text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
        || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}

public sealed class MovementDialog : Dialog {
    private readonly TextField _typeField;
    private readonly TextField _quantityField;

    public bool Saved { get; private set; }
    public MovimientoRequest? Movement { get; private set; }

    public MovementDialog(ProductoDto product) {
        Title = "Registrar movimiento";
        Width = 70;
        Height = 13;

        Label productLabel = new() {
            Text = $"Producto: {product.Codigo} - {product.Nombre} | Stock actual: {product.Stock}",
            X = 2,
            Y = 1
        };

        Label typeLabel = new() { Text = "Tipo *:", X = 2, Y = 3 };
        _typeField = new() {
            Text = "Compra",
            X = 15,
            Y = 3,
            Width = 18,
            CanFocus = true
        };

        Label helpLabel = new() { Text = "Valores: Compra, Venta o Ajuste", X = 36, Y = 3 };

        Label quantityLabel = new() { Text = "Cantidad *:", X = 2, Y = 5 };
        _quantityField = new() {
            Text = "1",
            X = 15,
            Y = 5,
            Width = 18,
            CanFocus = true
        };

        Label noteLabel = new() {
            Text = "En ajuste, la cantidad pasa a ser el stock final del producto.",
            X = 2,
            Y = 7
        };

        Add(productLabel, typeLabel, _typeField, helpLabel, quantityLabel, _quantityField, noteLabel);

        Button saveButton = new() { Text = "Registrar", IsDefault = true };
        saveButton.Accepting += (_, e) => {
            e.Handled = true;
            if (TrySave()) App!.RequestStop();
        };

        Button cancelButton = new() { Text = "Cancelar" };
        cancelButton.Accepting += (_, e) => {
            Saved = false;
            e.Handled = true;
            App!.RequestStop();
        };

        AddButton(saveButton);
        AddButton(cancelButton);
        _typeField.SetFocus();
    }

    private bool TrySave() {
        string typeText = _typeField.Text?.ToString()?.Trim() ?? "";
        if (!Enum.TryParse(typeText, ignoreCase: true, out TipoMovimiento type)) {
            MessageBox.ErrorQuery(App!, "Validación", "El tipo debe ser Compra, Venta o Ajuste.", "Ok");
            _typeField.SetFocus();
            return false;
        }

        if (!int.TryParse(_quantityField.Text?.ToString(), out int quantity) || quantity <= 0) {
            MessageBox.ErrorQuery(App!, "Validación", "La cantidad debe ser un entero positivo.", "Ok");
            _quantityField.SetFocus();
            return false;
        }

        Movement = new MovimientoRequest(type, quantity);
        Saved = true;
        return true;
    }
}

// ── Cliente REST ──────────────────────────────────────────────────────────

public sealed class CatalogoApi : IDisposable {
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;

    public CatalogoApi(string baseUrl) {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public List<ProductoDto> GetProducts() =>
        _http.GetFromJsonAsync<List<ProductoDto>>("productos", _jsonOptions).GetAwaiter().GetResult() ?? [];

    public ProductoDto CreateProduct(ProductoRequest product) {
        using HttpResponseMessage response = _http.PostAsJsonAsync("productos", product, _jsonOptions).GetAwaiter().GetResult();
        EnsureSuccess(response);
        return response.Content.ReadFromJsonAsync<ProductoDto>(_jsonOptions).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("El servidor no devolvió el producto creado.");
    }

    public ProductoDto UpdateProduct(int id, ProductoRequest product) {
        using HttpResponseMessage response = _http.PutAsJsonAsync($"productos/{id}", product, _jsonOptions).GetAwaiter().GetResult();
        EnsureSuccess(response);
        return response.Content.ReadFromJsonAsync<ProductoDto>(_jsonOptions).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("El servidor no devolvió el producto actualizado.");
    }

    public void DeleteProduct(int id) {
        using HttpResponseMessage response = _http.DeleteAsync($"productos/{id}").GetAwaiter().GetResult();
        EnsureSuccess(response);
    }

    public List<MovimientoDto> GetMovements(int productId) =>
        _http.GetFromJsonAsync<List<MovimientoDto>>($"productos/{productId}/movimientos", _jsonOptions).GetAwaiter().GetResult() ?? [];

    public MovimientoDto CreateMovement(int productId, MovimientoRequest movement) {
        using HttpResponseMessage response = _http.PostAsJsonAsync($"productos/{productId}/movimientos", movement, _jsonOptions).GetAwaiter().GetResult();
        EnsureSuccess(response);
        return response.Content.ReadFromJsonAsync<MovimientoDto>(_jsonOptions).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("El servidor no devolvió el movimiento creado.");
    }

    public void Dispose() => _http.Dispose();

    private static void EnsureSuccess(HttpResponseMessage response) {
        if (response.IsSuccessStatusCode) return;

        string message = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(message)) {
            message = $"Error HTTP {(int)response.StatusCode}";
        }

        throw new InvalidOperationException(message.Trim('"'));
    }
}

// ── DTO ───────────────────────────────────────────────────────────────────

public record ProductoDto(int Id, string Codigo, string Nombre, decimal Precio, int Stock);
public record MovimientoDto(int Id, int ProductoId, TipoMovimiento Tipo, int Cantidad, DateTime Fecha);
public record ProductoRequest(string Codigo, string Nombre, decimal Precio, int Stock);
public record MovimientoRequest(TipoMovimiento Tipo, int Cantidad);

public enum TipoMovimiento {
    Compra,
    Venta,
    Ajuste
}
