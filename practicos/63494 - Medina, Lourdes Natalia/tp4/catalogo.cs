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

try{
    using CatalogoApiClient api = new(serverUrl);
    using IApplication app = Application.Create().Init();
    app.Run(new CatalogoWindow(api));
}
catch (Exception ex) {
    Console.WriteLine($"No se pudo iniciar el catalogo: {ex.Message} ");
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

        Label searchLabel = new(){
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

        productList = new ListView{
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
            Height = Dim.Fill(1)
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
