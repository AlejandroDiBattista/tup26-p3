#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1

#pragma warning disable CS0618
#pragma warning disable CS8618

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Terminal.Gui;

var clienteApi = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

Application.Init();
var win = new CatalogoWindow(clienteApi);
Application.Run(win);
Application.Shutdown();
return;

class CatalogoWindow : Window
{
    HttpClient _api;
    List<Producto> _productos = new();
    ListView _listaProductos;
    TextView _detalleMovimientos;
    TextField _txtBuscar;

    public CatalogoWindow(HttpClient api)
    {
        _api = api;
        Title = " Sistema de Catalogo y Stock ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem("_Archivo", new MenuItem[] {
                new MenuItem("_Salir", "", () => Application.RequestStop())
            }),
            new MenuBarItem("_Productos", new MenuItem[] {
                new MenuItem("_Nuevo", "", () => MessageBox.Query("Info", "Pronto", "OK")),
                new MenuItem("_Editar", "", () => MessageBox.Query("Info", "Pronto", "OK")),
                new MenuItem("_Eliminar", "", () => MessageBox.Query("Info", "Pronto", "OK"))
            }),
            new MenuBarItem("_Stock", new MenuItem[] {
                new MenuItem("_Registrar Movimiento", "", () => MessageBox.Query("Info", "Pronto", "OK"))
            })
        });
        Add(menu);

        Add(new Label { Text = "Buscar:", X = 1, Y = 2 });
        _txtBuscar = new TextField { Text = "", X = 9, Y = 2, Width = 40 };
        Add(_txtBuscar);

        var frameIzq = new FrameView { Title = " Productos (Maestro) ", X = 1, Y = 4, Width = Dim.Percent(50), Height = Dim.Fill(1) };
        _listaProductos = new ListView { Width = Dim.Fill(), Height = Dim.Fill() };
        frameIzq.Add(_listaProductos);

        var frameDer = new FrameView { Title = " Historial Movimientos (Detalle) ", X = Pos.Right(frameIzq) + 1, Y = 4, Width = Dim.Fill(1), Height = Dim.Fill(1) };
        _detalleMovimientos = new TextView { Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        frameDer.Add(_detalleMovimientos);

        Add(frameIzq, frameDer);
    }
}

class Producto 
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

class Movimiento 
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}