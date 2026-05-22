#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

string databasePath = args.Length > 0 ? args[0] : "agenda.db";

try {
    using SqliteAgendaStore store = new(databasePath);
    using IApplication app = Application.Create().Init();
    app.Run(new AgendaWindow(store));
}
catch (Exception ex) {
    Console.Error.WriteLine($"No se pudo iniciar la agenda: {ex.Message}");
    Environment.ExitCode = 1;
}

public sealed class AgendaWindow : Window {
    private readonly SqliteAgendaStore store;
    private readonly List<Contacto> contacts;
    private readonly List<Contacto> filteredContacts = [];

    private TextField searchField = null!;
    private ListView listView = null!;
    private Label detailLabel = null!;
    private StatusBar statusBar = null!;
    private bool onlyFavorites;
    private int selectedIndex;

     public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;
        contacts = store.GetAll().ToList();

        Title = $"Agenda - {store.DatabasePath}";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        RefreshFilteredContacts();
        SetStatus($"Agenda abierta. {contacts.Count} contacto(s).");
    }

      private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", RequestExit
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo", "F2 / Ctrl+N", NewContact),
                    new MenuItem("_Editar", "F3 / Enter", EditSelectedContact),
                    new MenuItem("_Eliminar", "Del / Ctrl+D", DeleteSelectedContact)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("_Solo favoritos", null!, ToggleOnlyFavorites)
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
        searchField.TextChanged += (_, _) => RefreshFilteredContacts();

        FrameView listFrame = new() {
            Title = "Contactos",
            X = 1,
            Y = 3,
            Width = Dim.Percent(38),
            Height = Dim.Fill(1)
        };

        listView = new ListView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        listFrame.Add(listView);

        FrameView detailFrame = new() {
            Title = "Detalle",
            X = Pos.Right(listFrame) + 1,
            Y = 3,
            Width = Dim.Fill(1),
            Height = Dim.Fill(1)
        };

        detailLabel = new Label {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = Dim.Fill()
        };
        detailFrame.Add(detailLabel);

        statusBar = new StatusBar([
            new Shortcut(Key.F2, "Nuevo", NewContact),
            new Shortcut(Key.F3, "Editar", EditSelectedContact),
            new Shortcut(Key.Delete, "Eliminar", DeleteSelectedContact),
            new Shortcut(Key.F4, "Buscar", FocusSearch),
            new Shortcut(Key.Q.WithCtrl, "Salir", RequestExit)
        ]);

        Add(menu, searchLabel, searchField, listFrame, detailFrame, statusBar);
    }
