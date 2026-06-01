#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

string serverUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

try {
    using CatalogoApiClient api = new(serverUrl);
    using IApplication app = Application.Create().Init();
    app.Run(new CatalogoWindow(api));
}
catch (Exception ex) {
    Console.Error.WriteLine($"No se pudo iniciar el catalogo: {ex.Message}");
    Environment.ExitCode = 1;
}

public sealed class CatalogoWindow : Window {
    private readonly CatalogoApiClient api;
    private readonly List<Producto> products = [];
    private readonly List<Producto> filteredProducts = [];
    private readonly List<MovimientoDeProducto> movements = [];

    private TextField searchField = null!;
    private ListView productList = null!;
    private ListView movementList = null!;
    private Label productDetail = null!;
    private StatusBar statusBar = null!;
    private int selectedIndex;

    public CatalogoWindow(CatalogoApiClient api) {
        this.api = api;

        Title = $"Catalogo de productos - {api.BaseUrl}";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        ReloadProducts("Catalogo cargado.");
    }

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Refrescar", "F5", RefreshAll),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", RequestExit)
                ]),
                new MenuBarItem("_Productos", [
                    new MenuItem("_Agregar", "F2 / Ctrl+N", AddProduct),
                    new MenuItem("_Modificar", "F3 / Enter", EditProduct),
                    new MenuItem("_Eliminar", "Del / Ctrl+D", DeleteProduct)
                ]),
                new MenuBarItem("_Movimientos", [
                    new MenuItem("_Compra", "F6", RegisterPurchase),
                    new MenuItem("_Venta", "F7", RegisterSale),
                    new MenuItem("_Ajuste", "F8", RegisterAdjustment)
                ]),
                new MenuBarItem("_Ayuda", [
                    new MenuItem("_Acerca de", null!, ShowAbout)
                ])
            ]
        };

        Label searchLabel = new() {
            Text = "Buscar:",
            X = 1,
            Y = 1,
            Width = 8
        };

        searchField = new TextField {
            X = Pos.Right(searchLabel) + 1,
            Y = 1,
            Width = Dim.Fill(1)
        };
        searchField.TextChanged += (_, _) => RefreshFilteredProducts();

        FrameView productFrame = new() {
            Title = "Productos",
            X = 1,
            Y = 3,
            Width = Dim.Percent(52),
            Height = Dim.Fill(1)
        };

        productList = new ListView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        productFrame.Add(productList);

        FrameView detailFrame = new() {
            Title = "Detalle / movimientos",
            X = Pos.Right(productFrame) + 1,
            Y = 3,
            Width = Dim.Fill(1),
            Height = Dim.Fill(1)
        };

        productDetail = new Label {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = 5
        };

        movementList = new ListView {
            X = 0,
            Y = Pos.Bottom(productDetail) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        detailFrame.Add(productDetail, movementList);

        statusBar = new StatusBar([
            new Shortcut(Key.F2, "Agregar", AddProduct),
            new Shortcut(Key.F3, "Editar", EditProduct),
            new Shortcut(Key.Delete, "Eliminar", DeleteProduct),
            new Shortcut(Key.F5, "Refrescar", RefreshAll),
            new Shortcut(Key.F6, "Compra", RegisterPurchase),
            new Shortcut(Key.F7, "Venta", RegisterSale),
            new Shortcut(Key.F8, "Ajuste", RegisterAdjustment),
            new Shortcut(Key.Q.WithCtrl, "Salir", RequestExit)
        ]);

        Add(menu, searchLabel, searchField, productFrame, detailFrame, statusBar);
        searchField.SetFocus();
    }

     private void ReloadProducts(string status) {
        try {
            int selectedId = SelectedProduct()?.Id ?? 0;
            products.Clear();
            products.AddRange(api.GetProductsAsync().GetAwaiter().GetResult());
            RefreshFilteredProducts();

            if (selectedId != 0) {
                SelectProduct(selectedId);
            }

            LoadSelectedMovements();
            SetStatus($"{status} {products.Count} producto(s).");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error de conexion", ex.Message, "Aceptar");
            SetStatus("No se pudo cargar el catalogo.");
        }
    }

  private void RefreshFilteredProducts() {
        string query = (searchField?.Text?.ToString() ?? "").Trim();
        int currentId = SelectedProduct()?.Id ?? 0;

        filteredProducts.Clear();
        filteredProducts.AddRange(products
            .Where(p => MatchesSearch(p, query))
            .OrderBy(p => p.Codigo, StringComparer.CurrentCultureIgnoreCase));

        productList?.SetSource(new ObservableCollection<string>(filteredProducts.Select(FormatProductListItem).ToList()));

        selectedIndex = 0;
        if (currentId != 0) {
            int found = filteredProducts.FindIndex(p => p.Id == currentId);
            selectedIndex = found >= 0 ? found : 0;
        }

        if (productList is not null && filteredProducts.Count > 0) {
            productList.SelectedItem = Math.Min(selectedIndex, filteredProducts.Count - 1);
        }

        LoadSelectedMovements();
        productList?.SetNeedsDraw();
    }

      private static bool MatchesSearch(Producto product, string query) {
        return string.IsNullOrWhiteSpace(query)
            || product.Codigo.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || product.Nombre.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private static string FormatProductListItem(Producto product) {
        return $"{product.Codigo,-12} {TrimTo(product.Nombre, 28),-28} {product.Precio,10:C2}  Stock: {product.Stock,5}";
    }

     private Producto? SelectedProduct() {
        if (filteredProducts.Count == 0) {
            return null;
        }

        int index = productList is null ? selectedIndex : productList.SelectedItem ?? selectedIndex;
        if (index < 0 || index >= filteredProducts.Count) {
            index = 0;
        }

        return filteredProducts[index];
    }

    private void LoadSelectedMovements() {
        Producto? selected = SelectedProduct();
        movements.Clear();

        if (selected is null) {
            productDetail.Text = "No hay productos para mostrar.";
            movementList.SetSource(new ObservableCollection<string>(["Sin movimientos."]));
            return;
        }

        productDetail.Text =
            $"Id: {selected.Id}\n" +
            $"Codigo: {selected.Codigo}\n" +
            $"Nombre: {selected.Nombre}\n" +
            $"Precio: {selected.Precio:C2}    Stock actual: {selected.Stock}";

        try {
            movements.AddRange(api.GetMovementsAsync(selected.Id).GetAwaiter().GetResult());
            movementList.SetSource(new ObservableCollection<string>(
                movements.Count == 0
                    ? ["Sin movimientos registrados."]
                    : movements.Select(FormatMovementListItem).ToList()));
        }
        catch (Exception ex) {
            movementList.SetSource(new ObservableCollection<string>([$"Error: {ex.Message}"]));
        }

        productDetail.SetNeedsDraw();
        movementList.SetNeedsDraw();
    }

    private static string FormatMovementListItem(MovimientoDeProducto movement) {
        return $"{movement.Fecha:yyyy-MM-dd HH:mm}  {movement.Tipo,-7}  {movement.Cantidad,6}";
    }

 private void AddProduct() {
        ProductDialog dialog = new();
        App!.Run(dialog);

        if (!dialog.Accepted || dialog.Product is null) {
            SetStatus("Alta cancelada.");
            return;
        }

        try {
            Producto saved = api.CreateProductAsync(dialog.Product).GetAwaiter().GetResult();
            ReloadProducts($"Producto agregado: {saved.Codigo}.");
            SelectProduct(saved.Id);
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al guardar", ex.Message, "Aceptar");
        }
    }

     private void EditProduct() {
        Producto? selected = SelectedProduct();
        if (selected is null) {
            SetStatus("No hay producto seleccionado para modificar.");
            return;
        }

        ProductDialog dialog = new(selected);
        App!.Run(dialog);

        if (!dialog.Accepted || dialog.Product is null) {
            SetStatus("Edicion cancelada.");
            return;
        }

        try {
            Producto updated = api.UpdateProductAsync(selected.Id, dialog.Product).GetAwaiter().GetResult();
            ReloadProducts($"Producto modificado: {updated.Codigo}.");
            SelectProduct(updated.Id);
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al modificar", ex.Message, "Aceptar");
        }
    }

       private void DeleteProduct() {
        Producto? selected = SelectedProduct();
        if (selected is null) {
            SetStatus("No hay producto seleccionado para eliminar.");
            return;
        }

        int answer = MessageBox.Query(
            App!,
            "Confirmar eliminacion",
            $"Eliminar el producto \"{selected.Codigo} - {selected.Nombre}\"?",
            "Eliminar",
            "Cancelar") ?? 1;

        if (answer != 0) {
            SetStatus("Eliminacion cancelada.");
            return;
        }

        try {
            api.DeleteProductAsync(selected.Id).GetAwaiter().GetResult();
            ReloadProducts($"Producto eliminado: {selected.Codigo}.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al eliminar", ex.Message, "Aceptar");
        }
    }

private void RegisterPurchase() {
        RegisterMovement(TipoMovimiento.Compra);
    }

private void RegisterSale() {
        RegisterMovement(TipoMovimiento.Venta); 
    }

private void RegisterAdjustment() {
        RegisterMovement(TipoMovimiento.Ajuste);
    }

private void RegisterMovement(TipoMovimiento type) {
        Producto? selected = SelectedProduct();
        if (selected is null) {
            SetStatus($"No hay producto seleccionado para registrar movimientos.");
            return;
        }

        MovementDialog dialog = new(type, selected);
        App!.Run(dialog);

        if (!dialog.Accepted || dialog.Request is null) {
            SetStatus($"{tipo} cancelado.");
            return;
        }

        try {
            MovimientoResponse response = api.CreateMovementAsync(selected.Id, dialog.Request).GetAwaiter().GetResult();
            ReloadProducts($"Movimiento registrado. Stock actual: {response.StockActual}.");
           
           SelectProduct(selected.Id);
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, $"Error al registrar", ex.Message, "Aceptar");
        }
    }

    private void RefreshAll() {
        ReloadProducts("Catalogo actualizado.");
    }

    private void SelectProduct(int id) {
        int index = filteredProducts.FindIndex(p => p.Id == id);
        if (index >= 0) {
                productList.SelectedItem = selectedIndex;
                selectedIndex = index;
                LoadSelectedMovements();   
        }
    }

    private void ShowAbout() {
        MessageBox.Query(
            App!,
            "Acerca de",
            "Catalogo de productos\nTerminal.Gui v2 + ASP.NET Core Minimal API + EF Core SQLite", 
            "Aceptar");
    }

    private void RequestExit() {
        App!.RequestStop();
    }

    private void SetStatus(string message) {
       if (statusBar is not null) {
            statusBar.Text = message;
        }
 }   

 