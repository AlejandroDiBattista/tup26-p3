#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

// Punto de entrada: procesar argumentos, abrir la base y arrancar la app
string dbPath = args.Length > 0 ? args[0] : "agenda.db";

using IApplication app = Application.Create().Init();

SqliteAgendaStore store;
try {
    store = new SqliteAgendaStore(dbPath);
} catch (Exception ex) {
    MessageBox.ErrorQuery(app, "Error", $"No se pudo abrir la base de datos: {ex.Message}", "Ok");
    return;
}

List<Contacto> contacts = store.GetAll();
app.Run(new AgendaWindow(store, contacts, dbPath));


// Ventana principal
public sealed class AgendaWindow : Runnable {

    private readonly SqliteAgendaStore _store;
    private readonly List<Contacto> _contacts;
    private List<Contacto> _filteredContacts = new();

    private readonly ListView _contactList = new();
    private readonly TextField _searchField = new();
    private readonly TextView _detailView = new();
    private readonly Label _statusLabel = new();

    private bool _soloFavoritos;

    public AgendaWindow(SqliteAgendaStore store, List<Contacto> contacts, string dbPath) {
        _store    = store;
        _contacts = contacts;

        Title  = "AgendaT";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        RefreshContacts();
        SetStatus($"{_contacts.Count} contacto(s) cargado(s) desde '{dbPath}'.");
        _searchField.SetFocus();
    }

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON...", "Ctrl+I", ImportarJson),
                    new MenuItem("_Exportar JSON...", "Ctrl+E", ExportarJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo", "F2", NuevoContacto),
                    new MenuItem("_Editar", "F3", EditarContacto),
                    new MenuItem("E_liminar", "Del", EliminarContacto)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("Solo _favoritos", "", ToggleSoloFavoritos)
                ]),
                new MenuBarItem("A_yuda", [
                    new MenuItem("Acerca _de", "", AcercaDe)
                ])
            ]
        };

        Label searchLabel = new() { Text = "Buscar:", X = 1, Y = 1 };

        _searchField.X = 10;
        _searchField.Y = 1;
        _searchField.Width = Dim.Fill(2);
        _searchField.CanFocus = true;
        _searchField.ValueChanged += (_, _) => RefreshContacts();
        _searchField.KeyDown += (_, key) => {
            if (key == Key.Enter || key == Key.Tab) {
                key.Handled = true;
                _contactList.SetFocus();
            }
        };

        FrameView listPanel = new() {
            Title  = "Contactos",
            X      = 0,
            Y      = 3,
            Width  = Dim.Percent(40),
            Height = Dim.Fill(2)
        };
        listPanel.BorderStyle = LineStyle.Single;

        _contactList.X = 0;
        _contactList.Y = 0;
        _contactList.Width = Dim.Fill();
        _contactList.Height = Dim.Fill();
        _contactList.CanFocus = true;
        _contactList.ValueChanged += (_, _) => UpdateDetail();
        _contactList.Activated += (_, _) => EditarContacto();
        _contactList.KeyDown += (_, key) => {
            if (key == Key.Enter) {
                key.Handled = true;
                EditarContacto();
            } else if (key == Key.Delete) {
                key.Handled = true;
                EliminarContacto();
            }
        };
        listPanel.Add(_contactList);

        FrameView detailPanel = new() {
            Title  = "Detalle",
            X      = Pos.Right(listPanel),
            Y      = 3,
            Width  = Dim.Fill(),
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

        Add(menu, searchLabel, _searchField, listPanel, detailPanel, _statusLabel);
    }

    private void RefreshContacts(int? selectedId = null) {
        string search = _searchField.Text?.ToString()?.Trim() ?? "";

        _filteredContacts = _contacts
            .Where(c => MatchesFilter(c, search, _soloFavoritos))
            .OrderByDescending(c => c.Favorito)
            .ThenBy(c => c.Nombre)
            .ToList();

        List<string> rows = _filteredContacts
            .Select(c => $"{(c.Favorito ? "Ôÿà" : " ")} {c.Nombre}")
            .ToList();

        _contactList.SetSource(new ObservableCollection<string>(rows));

        if (selectedId.HasValue) {
            int index = _filteredContacts.FindIndex(c => c.Id == selectedId.Value);
            if (index >= 0) {
                _contactList.SelectedItem = index;
            }
        } else if (_filteredContacts.Count > 0) {
            _contactList.SelectedItem = 0;
        }

        UpdateDetail();
    }

    private static bool MatchesFilter(Contacto contact, string search, bool soloFavoritos) {
        if (soloFavoritos && !contact.Favorito) {
            return false;
        }

        if (string.IsNullOrEmpty(search)) {
            return true;
        }

        return contact.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase)
            || contact.Telefonos.Contains(search, StringComparison.OrdinalIgnoreCase)
            || contact.Email.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateDetail() {
        Contacto? selected = GetSelectedContact();
        if (selected is null) {
            _detailView.Text = _filteredContacts.Count == 0
                ? "No hay contactos para mostrar."
                : "Ning├║n contacto seleccionado.";
            return;
        }

        _detailView.Text = $"""
            Nombre:    {selected.Nombre}
            Email:     {selected.Email}
            Favorito:  {(selected.Favorito ? "S├¡ Ôÿà" : "No")}
            Tel├®fonos: {selected.Telefonos}

            Notas:
            {selected.Notas}
            """;
    }

    private Contacto? GetSelectedContact() {
        int index = _contactList.SelectedItem ?? -1;
        if (index >= 0 && index < _filteredContacts.Count) {
            return _filteredContacts[index];
        }
        return null;
    }

    private void SetStatus(string message) {
        _statusLabel.Text = message;
    }

    private void NuevoContacto() {
        SetStatus("Nuevo contacto: pendiente de implementar (Parte 3/4).");
    }

    private void EditarContacto() {
        if (GetSelectedContact() is null) {
            MessageBox.Query(App!, "Editar", "Seleccion├í un contacto.", "Ok");
            return;
        }
        SetStatus("Editar contacto: pendiente de implementar (Parte 3/4).");
    }

    private void EliminarContacto() {
        if (GetSelectedContact() is null) {
            MessageBox.Query(App!, "Eliminar", "Seleccion├í un contacto.", "Ok");
            return;
        }
        SetStatus("Eliminar contacto: pendiente de implementar (Parte 4).");
    }

    private void ImportarJson() {
        SetStatus("Importar JSON: pendiente de implementar (Parte 6).");
    }

    private void ExportarJson() {
        SetStatus("Exportar JSON: pendiente de implementar (Parte 6).");
    }

    private void ToggleSoloFavoritos() {
        _soloFavoritos = !_soloFavoritos;
        RefreshContacts();
        SetStatus(_soloFavoritos
            ? "Mostrando solo favoritos."
            : "Mostrando todos los contactos.");
    }

    private void AcercaDe() {
        MessageBox.Query(
            App!,
            "Acerca de",
            "AgendaT ÔÇö Trabajo Pr├íctico 3\nGesti├│n de contactos con SQLite y JSON.",
            "Ok");
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            SolicitarSalir();
            return true;
        }
        if (key == Key.N.WithCtrl || key == Key.F2) {
            NuevoContacto();
            return true;
        }
        if (key == Key.F3) {
            EditarContacto();
            return true;
        }
        if (key == Key.F4) {
            _searchField.SetFocus();
            return true;
        }
        if (key == Key.D.WithCtrl || key == Key.Delete) {
            EliminarContacto();
            return true;
        }
        if (key == Key.I.WithCtrl) {
            ImportarJson();
            return true;
        }
        if (key == Key.E.WithCtrl) {
            ExportarJson();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class SqliteAgendaStore {

    private readonly string _connectionString;

    public SqliteAgendaStore(string dbPath) {
        _connectionString = $"Data Source={dbPath}";
        EnsureSchema();
    }

    private SqliteConnection Open() {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureSchema() {
        using SqliteConnection db = Open();
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT    NOT NULL,
                Telefonos TEXT    NOT NULL DEFAULT '',
                Email     TEXT    NOT NULL DEFAULT '',
                Notas     TEXT    NOT NULL DEFAULT '',
                Favorito  INTEGER NOT NULL DEFAULT 0
            );
            """);
    }

    public List<Contacto> GetAll() {
        using SqliteConnection db = Open();
        return db.GetAll<Contacto>().ToList();
    }

    public int Insert(Contacto contact) {
        using SqliteConnection db = Open();
        return (int)db.Insert(contact);
    }

    public bool Update(Contacto contact) {
        using SqliteConnection db = Open();
        return db.Update(contact);
    }

    public bool Delete(Contacto contact) {
        using SqliteConnection db = Open();
        return db.Delete(contact);
    }
}

public class JsonAgendaIO {}

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

    public Contacto Clone() => new() {
        Id        = Id,
        Nombre    = Nombre,
        Telefonos = Telefonos,
        Email     = Email,
        Notas     = Notas,
        Favorito  = Favorito
    };
}
