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
                    new MenuItem("_Salir", "Ctrl+Q", RequestExit)
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

    private void RefreshFilteredContacts() {}
    private void NewContact() {}
    private void EditSelectedContact() {}
    private void DeleteSelectedContact() {}
    private void ImportJson() {}
    private void ExportJson() {}
    private void ToggleOnlyFavorites() {}
    private void ShowAbout() {}
    private void FocusSearch() {}
    private void RequestExit() => App!.RequestStop();

    private void SetStatus(string message) {
        if (statusBar is not null) {
            statusBar.Text = message;
        }
    }
}

public sealed class SqliteAgendaStore : IDisposable {
    private readonly SqliteConnection connection;

    public string DatabasePath { get; }

    public SqliteAgendaStore(string databasePath) {
        DatabasePath = databasePath;

        SqliteConnectionStringBuilder builder = new() {
            DataSource = databasePath
        };

        connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        EnsureSchema();
    }

    public IEnumerable<Contacto> GetAll() {
        return connection.GetAll<Contacto>();
    }

    public int Insert(Contacto contact) {
        Validate(contact);
        long id = connection.Insert(contact);
        return checked((int)id);
    }

    public void Update(Contacto contact) {
        Validate(contact);
        connection.Update(contact);
    }

    public void Delete(Contacto contact) {
        connection.Delete(contact);
    }

    public void Dispose() {
        connection.Dispose();
    }

    private void EnsureSchema() {
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Notas TEXT NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            );
            """);
    }

    private static void Validate(Contacto contact) {
        if (string.IsNullOrWhiteSpace(contact.Nombre)) {
            throw new InvalidOperationException("El nombre no puede estar vacio.");
        }

        if (!string.IsNullOrWhiteSpace(contact.Email) && !contact.Email.Contains('@')) {
            throw new InvalidOperationException("El email debe contener @.");
        }
    }
}

public static class JsonAgendaIO {
}

[Table("Contactos")]
public sealed class Contacto {
    [Key]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Telefonos { get; set; } = "";

    public string Email { get; set; } = "";

    public string Notas { get; set; } = "";

    public bool Favorito { get; set; }

    public Contacto Clone() {
        return new Contacto {
            Id = Id,
            Nombre = Nombre,
            Telefonos = Telefonos,
            Email = Email,
            Notas = Notas,
            Favorito = Favorito
        };
    }
}