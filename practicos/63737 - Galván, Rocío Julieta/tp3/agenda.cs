#!/usr/bin/env dotnet
#:property LangVersion=preview

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
using SQLitePCL;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;


string databasePath = args.Length > 0 ? args[0] : "agenda.db";

try {
    var store = new SqliteAgendaStore(databasePath);

    using var app = Application.Create();
    app.Init();

    using var window = new AgendaWindow(store);
    app.Run(window);
}
catch (Exception ex) {
    Console.Error.WriteLine($"Error: {ex.Message}");
}

public class AgendaWindow : Window {
    private readonly SqliteAgendaStore _store;
    private readonly List<Contacto> _contacts;
    private List<Contacto> _filteredContacts;
    private bool _soloFavoritos;

    private MenuBar _menuBar = null!;
    private TextField _searchField = null!;
    private ListView _listView = null!;
    private TextView _detailView = null!;
    private StatusBar _statusBar = null!;

    public AgendaWindow(SqliteAgendaStore store) {
        _store = store;
        _contacts = store.GetAll();
        _filteredContacts = new List<Contacto>(_contacts);

        Title = "AGENDA DE CONTACTO";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BuildMenu();
        BuildLayout();
        BuildStatusBar();
        RefreshList();
    }

    private void BuildMenu() {
        _menuBar = new MenuBar {
            Menus =
            [
                new MenuBarItem("_Archivo",
            [
                new MenuItem("_Importar JSON", "Ctrl+I",  () => ImportJson()),
                new MenuItem("_Exportar JSON", "Ctrl+E",  () => ExportJson()),
                null!,
                new MenuItem("_Salir",         "Ctrl+Q",  () => App.RequestStop())
            ]),
            new MenuBarItem("_Contactos",
            [
                new MenuItem("_Nuevo",    "F2 / Ctrl+N",  () => NuevoContacto()),
                new MenuItem("_Editar",   "F3 / Enter",   () => EditarContacto()),
                new MenuItem("_Eliminar", "Del / Ctrl+D", () => EliminarContacto())
            ]),
            new MenuBarItem("_Ver",
            [
                new MenuItem("_Solo favoritos", null!, () => ToggleFavoritos())
            ]),
            new MenuBarItem("_Ayuda",
            [
                new MenuItem("_Acerca de", null!, () => MostrarAcercaDe())
            ])
            ]
        };
        Add(_menuBar);
    }

    private void BuildLayout() {
        _searchField = new TextField {
            X = 10,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 1
        };
        var labelSearch = new Label {
            Text = "Buscar:",
            X = 1,
            Y = 1
        };
        _listView = new ListView {
            X = 1,
            Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(2)
        };
        _detailView = new TextView {
            X = Pos.Right(_listView) + 1,
            Y = 3,
            Width = Dim.Fill(1),
            Height = Dim.Fill(2),
            ReadOnly = true
        };

        _searchField.TextChanged += (_, _) => ApplyFilter();
        _listView.ValueChanged += (_, _) => ShowDetail();

        Add(labelSearch, _searchField, _listView, _detailView);
    }



    private void BuildStatusBar() {
        _statusBar = new StatusBar(
        [
            new Shortcut(Key.F2,         "Nuevo",    () => NuevoContacto()),
        new Shortcut(Key.F3,         "Editar",   () => EditarContacto()),
        new Shortcut(Key.Delete,     "Eliminar", () => EliminarContacto()),
        new Shortcut(Key.F4,         "Buscar",   () => _searchField.SetFocus()),
        new Shortcut(Key.Q.WithCtrl, "Salir",    () => App.RequestStop())
        ]);
        Add(_statusBar);
    }

    private void RefreshList() {
        _listView.SetSource(new ObservableCollection<string>(
            _filteredContacts.Select(c => (c.Favorito ? "♥ " : "  ") + c.Nombre)));
    }

    private void ApplyFilter() {
        string busqueda = _searchField.Text.ToLower();
        _filteredContacts = _contacts.Where(c =>
            (!_soloFavoritos || c.Favorito) &&
            (c.Nombre.ToLower().Contains(busqueda) ||
             c.Telefonos.ToLower().Contains(busqueda) ||
             c.Email.ToLower().Contains(busqueda))
        ).ToList();

        RefreshList();
    }

    private void ShowDetail() {
        int selectedIndex = SelectedIndex();
        if (selectedIndex < 0 || selectedIndex >= _filteredContacts.Count) {
            _detailView.Text = "";
            return;
        }

        var c = _filteredContacts[selectedIndex];
        _detailView.Text =
            $"Nombre:    {c.Nombre}\n" +
            $"Telefonos: {c.Telefonos}\n" +
            $"Email:     {c.Email}\n" +
            $"Notas:\n{c.Notas}\n\n" +
            $"Favorito:  {(c.Favorito ? "Si" : "No")}";
    }

    private void NuevoContacto() {
        var dialog = new ContactDialog(new Contacto());
        App.Run(dialog);
        if (dialog.Result == null) return;
        _store.Insert(dialog.Result);
        _contacts.Add(dialog.Result);
        ApplyFilter();
        SetStatus("Se agregó el contacto correctamente");

    }

    private void EditarContacto() {
        int selectedIndex = SelectedIndex();
        if (selectedIndex < 0 || selectedIndex >= _filteredContacts.Count) {
            return;
        }

        var original = _filteredContacts[selectedIndex];
        var dialog = new ContactDialog(original.Clone());
        App.Run(dialog);

        if (dialog.Result is null) {
            return;
        }

        dialog.Result.Id = original.Id;
        _store.Update(dialog.Result);

        int index = _contacts.FindIndex(c => c.Id == original.Id);
        if (index >= 0) {
            _contacts[index] = dialog.Result;
        }
        ApplyFilter();
        SetStatus("Se actualizó el contacto correctamente");
    }

    private void EliminarContacto() {
        int selectedIndex = SelectedIndex();
        if (selectedIndex < 0 || selectedIndex >= _filteredContacts.Count) {
            return;
        }

        var contacto = _filteredContacts[selectedIndex];
        int? confirm = MessageBox.Query(Application.Instance, "Eliminar", $"Eliminar a {contacto.Nombre}?", "Si", "No");

        if (confirm != 0) {
            return;
        }

        _store.Delete(contacto);
        _contacts.RemoveAll(c => c.Id == contacto.Id);
        ApplyFilter();
        SetStatus("CONTACTO ELIMINADO");
    }

    private void ToggleFavoritos() {
        _soloFavoritos = !_soloFavoritos;
        ApplyFilter();
        SetStatus(_soloFavoritos ? "Mostrando solo favoritos" : "Mostrando todos");
    }

    private void ImportJson() {
        var dialog = new OpenDialog {
            Title = "Importar JSON",
            Path = Directory.GetCurrentDirectory()
        };
        App.Run(dialog);

        string? path = dialog.FilePaths.FirstOrDefault() ?? dialog.Path;
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            var io = new JsonAgendaIO();
            var nuevos = io.Import(path);
            int cantidad = nuevos.Count;
            int? confirm = MessageBox.Query(
                Application.Instance,
                "Importar",
                $"Se agregaran {cantidad} contactos. Continuar?",
                "Si",
                "No");

            if (confirm != 0) {
                return;
            }
            foreach (var contacto in nuevos) {
                contacto.Id = 0;
                _store.Insert(contacto);
                _contacts.Add(contacto);
            }

            ApplyFilter();
            SetStatus($"{cantidad} contacto(s) importado(s)");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(Application.Instance, "Error", ex.Message, "Ok");
        }
    }

    private void ExportJson() {
        var dialog = new SaveDialog {
            Title = "Exportar JSON",
            Path = Directory.GetCurrentDirectory()
        };

        App.Run(dialog);
        if (string.IsNullOrWhiteSpace(dialog.FileName)) {
            return;
        }

        try {
            var io = new JsonAgendaIO();
            io.Export(_contacts, dialog.FileName);
            SetStatus("Contactos exportados correctamente");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(Application.Instance, "Error", ex.Message, "Ok");
        }
    }


    private void MostrarAcercaDe() {
        MessageBox.Query(Application.Instance, "Acerca de", "Agenda de Contactos\nTrabajo Practico 3\nTerminal.Gui + SQLite + JSON", "Ok");
    }
    private void SetStatus(string mensaje) {
        _statusBar.Title = mensaje;
        _statusBar.SetNeedsDraw();
    }
    private int SelectedIndex() {
        return _listView.SelectedItem ?? -1;
    }
}


public class ContactDialog : Dialog {
    public Contacto? Result { get; private set; }
    public ContactDialog(Contacto contacto) { }
}

public class SqliteAgendaStore {
    public SqliteAgendaStore(string databasePath) { }
    public List<Contacto> GetAll() => new();
    public void Insert(Contacto c) { }
    public void Update(Contacto c) { }
    public void Delete(Contacto c) { }
}

public class JsonAgendaIO {
    public List<Contacto> Import(string path) => new();
    public void Export(List<Contacto> contactos, string path) { }
}

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => (Contacto)MemberwiseClone();

}