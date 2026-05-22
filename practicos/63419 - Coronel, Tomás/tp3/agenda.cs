#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Data.Common;

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;



string archivoDb = args.Length > 0 ? args[0] : "agenda.db";

var repositorio = new SqliteAgendaStore(archivoDb);
repositorio.InitDb();

using IApplication app = Application.Create().Init();
app.Run(new VentanaPrincipal(repositorio));
public sealed class VentanaPrincipal : Runnable
{
    private readonly SqliteAgendaStore _repositorio;
    private List<Contacto> _listaCompleta = [];
    private List<Contacto> _listaFiltrada = [];

    private ListView    _vistaLista    = null!;
    private TextField   _campoBusqueda = null!;
    private TextView    _vistaDetalle  = null!;
    private Label       _etiquetaEstado = null!;
    private MenuItem    _itemFavoritos  = null!;

    private bool _filtrarFavoritos = false;

    public VentanaPrincipal(SqliteAgendaStore repositorio)
    {
        _repositorio = repositorio;
        Title  = "AgendaT — TP3";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        ArmarVentana();
        CargarContactos();
    }

    private void ArmarVentana()
    {
        _itemFavoritos = new MenuItem("_Solo favoritos", null!, AlternarFavoritos);

        MenuBar barraMenu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarDesdeJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarAJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", PedirSalida)
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo",    "F2",  AgregarContacto),
                    new MenuItem("_Editar",   "F3",  ModificarContacto),
                    new MenuItem("_Eliminar", "Del", BorrarContacto)
                ]),
                new MenuBarItem("_Ver",
                [
                    _itemFavoritos
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", null!, VerAcercaDe)
                ])
            ]
        };

        Label lblBuscar = new() { Text = "Buscar [F4]:", X = 0, Y = 1 };
        _campoBusqueda = new TextField()
        {
            Text = "",
            X = Pos.Right(lblBuscar) + 1,
            Y = 1,
            Width = Dim.Fill()
        };
        _campoBusqueda.TextChanged += (_, _) => AplicarFiltro();

        FrameView panelIzquierdo = new()
        {
            Title = "Contactos",
            X = 0, Y = 2,
            Width = Dim.Percent(50),
            Height = Dim.Fill(1)
        };
        _vistaLista = new ListView()
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _vistaLista.Accepting += (_, e) => { ModificarContacto(); e.Handled = true; };
        _vistaLista.KeyDown   += (_, _) => RefrescarDetalle();
        _vistaLista.KeyUp     += (_, _) => RefrescarDetalle();
        panelIzquierdo.Add(_vistaLista);

        FrameView panelDerecho = new()
        {
            Title = "Detalle",
            X = Pos.Right(panelIzquierdo), Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };
        _vistaDetalle = new TextView()
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        panelDerecho.Add(_vistaDetalle);

        _etiquetaEstado = new Label()
        {
            Text = "Listo.",
            X = 0,
            Y = Pos.Bottom(panelIzquierdo),
            Width = Dim.Fill()
        };

        Add(barraMenu, lblBuscar, _campoBusqueda, panelIzquierdo, panelDerecho, _etiquetaEstado);
    }

    