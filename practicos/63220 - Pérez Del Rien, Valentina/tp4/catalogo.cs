#!/usr/bin/env -S dotnet run
#:sdk Microsoft.NET.Sdk
#:package Terminal.Gui@2.4.3

#pragma warning disable IL2026, IL3050

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using System.Text.Json;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization.Metadata;

var http = new HttpClient() { BaseAddress = new Uri("http://localhost:5000") };

var jsonOpts = new JsonSerializerOptions 
{ 
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver() 
};

using IApplication app = Application.Create().Init();
app.Run(new CatalogoWindow(http, jsonOpts));

class CatalogoWindow : Window
{
    private readonly HttpClient http;
    private readonly JsonSerializerOptions jsonOpts;

    private List<Producto> productos          = new();
    private List<Producto> productosFiltrados = new();
    private Producto?      productoSeleccionado = null;

    private readonly ListView  listaProductos;
    private readonly ListView  listaMovimientos;
    private readonly TextField campoBusqueda;

    public CatalogoWindow(HttpClient http, JsonSerializerOptions jsonOpts)
    {
        this.http     = http;
        this.jsonOpts = jsonOpts;

        Title  = "Catálogo de Productos";
        Width  = Dim.Fill();
        Height = Dim.Fill();


        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Productos",
                [
                    new MenuItem("_Agregar",  "Ctrl+A", () => {}),
                    new MenuItem("_Editar",   "Ctrl+E", () => {}),
                    new MenuItem("_Eliminar", "Ctrl+D", () => {}),
                ]),
                new MenuBarItem("_Movimientos",
                [
                    new MenuItem("_Compra", "Alt+C", () => {}),
                    new MenuItem("_Venta",  "Alt+X", () => {}),
                    new MenuItem("_Ajuste", "Alt+J", () => {}),
                ]),
                new MenuBarItem("_Salir",
                [
                    new MenuItem("_Salir", "Alt+Q", () => App!.RequestStop()),
                ]),
            ]
        };


        FrameView panelIzq = new()
        {
            Title  = "Productos",
            X      = 0,
            Y      = 1,
            Width  = Dim.Percent(50),
            Height = Dim.Fill(1) 
        };

        Label lblBuscar = new() { Text = "Buscar: ", X = 0, Y = 0 };

        campoBusqueda = new TextField()
        {
            X     = Pos.Right(lblBuscar),
            Y     = 0,
            Width = Dim.Fill()
        };

        listaProductos = new ListView()
        {
            X = 0, Y = 2,
            Width  = Dim.Fill(),
            Height = Dim.Fill()
        };

        panelIzq.Add(lblBuscar, campoBusqueda, listaProductos);


        FrameView panelDer = new()
        {
            Title  = "Movimientos",
            X      = Pos.Right(panelIzq),
            Y      = 1,
            Width  = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        listaMovimientos = new ListView()
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill()
        };

        panelDer.Add(listaMovimientos);

        Label barraAtajos = new()
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = " Atajos: [Ctrl+A] Agregar │ [Ctrl+E] Editar │ [Ctrl+D] Eliminar │ [Alt+C] Compra │ [Alt+X] Venta │ [Alt+J] Ajuste │ [Alt+Q] Salir"
        };

        Add(menu, panelIzq, panelDer, barraAtajos);
    }
}

class Producto
{
    public int     Id     { get; set; }
    public string  Codigo { get; set; } = "";
    public string  Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int     Stock  { get; set; }
}

class MovimientoDeProducto
{
    public int      Id         { get; set; }
    public int      ProductoId { get; set; }
    public int      Tipo       { get; set; }
    public int      Cantidad   { get; set; }
    public DateTime Fecha      { get; set; }
}