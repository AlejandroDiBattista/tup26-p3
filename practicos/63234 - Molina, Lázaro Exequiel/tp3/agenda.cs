#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*


using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data.Common;
using Dapper.Contrib.Extensions;


string destino = args.FirstOrDefault() ?? ":memory:";
using SqliteAgendaStore agendaStore = new(destino);
agendaStore.Inicializar();
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(agendaStore, destino));

public sealed class AgendaWindow : Runnable {
    readonly SqliteAgendaStore datos; readonly string origen;
    readonly List<Contacto> baseCompleta; readonly List<Contacto> baseFiltrada = [];
    readonly System.Collections.ObjectModel.ObservableCollection<string> lineas = [];
    readonly TextField txtFiltro; readonly ListView listado; readonly TextView panel; readonly Label barra;
    bool favoritosActivos;

    public AgendaWindow(SqliteAgendaStore datos, string origen) {
        this.datos = datos; this.origen = origen; baseCompleta = datos.Obtener().ToList();
        Title = "AgendaT"; Width = Dim.Fill(); Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;
        MenuBar menu = MenuPrincipal();
        Label etiqueta = new() { Text = "Filtro:", X = 1, Y = 1 };
        txtFiltro = new() { X = Pos.Right(etiqueta) + 1, Y = 1, Width = Dim.Fill(1) };
        txtFiltro.TextChanged += (_, _) => Redibujar();
        listado = new() { X = 0, Y = 3, Width = Dim.Percent(42), Height = Dim.Fill(1), Title = "Agenda", BorderStyle = LineStyle.Single };
        listado.SetSource(lineas);
        listado.ValueChanged += (_, _) => PintarFicha();
        listado.Accepting += (_, e) => { Modificar(); e.Handled = true; };
        panel = new() { X = Pos.Right(listado) + 1, Y = 3, Width = Dim.Fill(), Height = Dim.Fill(1), Title = "Contacto", BorderStyle = LineStyle.Single, CanFocus = false };
        barra = new() { X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Text = "Preparado." };
        Add(menu, etiqueta, txtFiltro, listado, panel, barra);
        Redibujar();
    }