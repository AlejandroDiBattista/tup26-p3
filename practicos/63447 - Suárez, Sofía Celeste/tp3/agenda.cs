#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow());

public sealed class AgendaWindow : Runnable {

    private SqliteAgendaStore _store;

    private List<Contacto> _contactos = new();
    private List<Contacto> _contactosFiltrados = new();

    private ListView _listView = null!;
    private Label _lblDetalles = null!;

    private TextField _txtBusqueda = null!;
    private bool _soloFavoritos = false;

    public AgendaWindow() {

        Title = "AGENDA DE CONTACTOS";
        BorderStyle = LineStyle.Single;
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;

        var args = Environment.GetCommandLineArgs();
        string dbPath = args.Length > 2 ? args.Last() : "agenda.db";

        try {
           _store = new SqliteAgendaStore(dbPath);
            BuildLayout();
            LoadData();
        }
        catch (Exception ex) {
           MessageBox.Query(
               App!,
               "Error de Base de Datos",
               $"No se pudo abrir la base de datos.\n\n{ex.Message}",
               "OK"
            );

           Environment.Exit(1);
        }
        
    }
    private void BuildLayout() {
        MenuBar menu = new() {
        Menus = [
           new MenuBarItem ("_Archivo", [
              new MenuItem(
                   "_Importar JSON",
                   "Ctrl+I",
                    () => Importar()
                ),
              new MenuItem(
                   "_Exportar JSON",
                   "Ctrl+E",
                   () => Exportar()
                ),
               null!,
             new MenuItem(
                   "_Salir",
                   "Ctrl+X",
                   () => SolicitarSalir()
                )
            ]),

            new MenuBarItem("_Contactos", [
                new MenuItem(
                 "_Nuevo",
                  "F2",
                  () => NuevoContacto()
                ),
                new MenuItem(
                   "_Editar",
                   "F3",
                    () => EditarContacto()
                ),
                new MenuItem(
                  "_Eliminar",
                  "Del",
                   () => EliminarContacto()
                )
            ]),

            new MenuBarItem("_Ver", [
              new MenuItem(
                  "_Solo favoritos",
                   "",
                   () => {
                      _soloFavoritos = !_soloFavoritos;
                       AplicarFiltros();
                    }
                )
            ]),

            new MenuBarItem("_Ayuda", [
                new MenuItem(
                   "_Acerca de",
                   "",
                   () => {
                       MessageBox.Query(
                           App!,
                           "Acerca de",
                           "Agenda de Contactos",
                           "OK"
                       );
                   }
                )
            ])
        ]};
    }
    Label lblBuscar = new() {
        Text = "Buscar:",
        X = 1,
        Y = 2
    };

    _txtBusqueda = new TextField() {
        X = 10,
        Y = 2,
        Width = 30,
        CanFocus = true
    };

    _txtBusqueda.TextChanged += (_, _) => {
        AplicarFiltros();
    };
    
}
