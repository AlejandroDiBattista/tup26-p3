#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

// Punto de entrada: procesar argumentos, abrir la base y arrancar la app
Console.OutputEncoding = Encoding.UTF8;
Menu.DefaultBorderStyle = LineStyle.Single;

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
JsonAgendaIO jsonIO = new();
app.Run(new AgendaWindow(store, contacts, dbPath, jsonIO));


// Ventana principal
public sealed class AgendaWindow : Runnable {

    private readonly SqliteAgendaStore _store;
    private readonly JsonAgendaIO _jsonIO;
    private readonly List<Contacto> _contacts;
    private List<Contacto> _filteredContacts = new();

    private readonly ListView _contactList = new();
    private readonly TextField _searchField = new();
    private readonly TextView _detailView = new();
    private readonly Label _statusLabel = new();

    private bool _soloFavoritos;

    public AgendaWindow(SqliteAgendaStore store, List<Contacto> contacts, string dbPath, JsonAgendaIO jsonIO) {
        _store    = store;
        _jsonIO   = jsonIO;
        _contacts = contacts;

        Title  = "AgendaT";
        Width  = Dim.Fill();
        Height = Dim.Fill();

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

        return (contact.Nombre ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
            || (contact.Telefonos ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
            || (contact.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase);
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
        ContactDialog dialog = new("Nuevo contacto", new Contacto());
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Contact is null) {
            return;
        }

        try {
            int newId = _store.Insert(dialog.Contact);
            dialog.Contact.Id = newId;
            _contacts.Add(dialog.Contact);
            SetStatus($"Contacto '{dialog.Contact.Nombre}' creado.");
            RefreshContacts(newId);
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo crear el contacto: {ex.Message}", "Ok");
        }
    }

    private void EditarContacto() {
        Contacto? selected = GetSelectedContact();
        if (selected is null) {
            MessageBox.Query(App!, "Editar", "Seleccion├í un contacto.", "Ok");
            return;
        }

        ContactDialog dialog = new("Editar contacto", selected.Clone());
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Contact is null) {
            return;
        }

        try {
            _store.Update(dialog.Contact);
            int index = _contacts.FindIndex(c => c.Id == selected.Id);
            if (index >= 0) {
                _contacts[index] = dialog.Contact;
            }
            SetStatus($"Contacto '{dialog.Contact.Nombre}' actualizado.");
            RefreshContacts(dialog.Contact.Id);
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo actualizar el contacto: {ex.Message}", "Ok");
        }
    }

    private void EliminarContacto() {
        Contacto? selected = GetSelectedContact();
        if (selected is null) {
            MessageBox.Query(App!, "Eliminar", "Seleccion├í un contacto.", "Ok");
            return;
        }

        int answer = MessageBox.Query(
            App!,
            "Eliminar contacto",
            $"┬┐Eliminar el contacto '{selected.Nombre}'?",
            "No", "S├¡") ?? 0;

        if (answer != 1) {
            return;
        }

        try {
            _store.Delete(selected);
            _contacts.RemoveAll(c => c.Id == selected.Id);
            SetStatus($"Contacto '{selected.Nombre}' eliminado.");
            RefreshContacts();
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo eliminar el contacto: {ex.Message}", "Ok");
        }
    }

    private void ImportarJson() {
        FilePathDialog dialog = new("Importar JSON", "agenda.json");
        App!.Run(dialog);

        if (!dialog.Confirmed) {
            return;
        }

        try {
            List<Contacto> imported = _jsonIO.Read(dialog.Path);

            int answer = MessageBox.Query(
                App!,
                "Importar JSON",
                $"Se agregar├ín {imported.Count} contacto(s) del archivo. ┬┐Continuar?",
                "No", "S├¡") ?? 0;

            if (answer != 1) {
                return;
            }

            foreach (Contacto contact in imported) {
                contact.Id = 0;
                int newId = _store.Insert(contact);
                contact.Id = newId;
                _contacts.Add(contact);
            }

            SetStatus($"{imported.Count} contacto(s) importado(s) desde '{dialog.Path}'.");
            RefreshContacts();
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Importar JSON", ex.Message, "Ok");
        }
    }

    private void ExportarJson() {
        FilePathDialog dialog = new("Exportar JSON", "salida.json");
        App!.Run(dialog);

        if (!dialog.Confirmed) {
            return;
        }

        try {
            _jsonIO.Write(dialog.Path, _contacts);
            SetStatus($"{_contacts.Count} contacto(s) exportado(s) a '{dialog.Path}'.");
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Exportar JSON", ex.Message, "Ok");
        }
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
            """
            AgendaT ÔÇö Trabajo Pr├íctico 3
            Gesti├│n de contactos con SQLite y JSON.

            Paz, Naim Federico ÔÇö Legajo 61581
            """,
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

public sealed class ContactDialog : Dialog {

    private readonly TextField _nameField;
    private readonly TextField _emailField;
    private readonly TextView _notesField;
    private readonly CheckBox _favoriteField;
    private readonly TextField[] _phoneFields = new TextField[5];

    public bool Saved { get; private set; }
    public Contacto Contact { get; private set; }

    public ContactDialog(string title, Contacto contact) {
        Title = title;
        Width = 70;
        Height = 22;

        Contact = contact;

        Label nameLabel = new() { Text = "Nombre *:", X = 2, Y = 1 };
        _nameField = new() {
            Text     = contact.Nombre,
            X        = 15,
            Y        = 1,
            Width    = Dim.Fill(4),
            CanFocus = true
        };

        Label emailLabel = new() { Text = "Email:", X = 2, Y = 3 };
        _emailField = new() {
            Text     = contact.Email,
            X        = 15,
            Y        = 3,
            Width    = Dim.Fill(4),
            CanFocus = true
        };

        Label favoriteLabel = new() { Text = "Favorito:", X = 2, Y = 5 };
        _favoriteField = new() {
            X        = 15,
            Y        = 5,
            Value    = contact.Favorito ? CheckState.Checked : CheckState.UnChecked,
            CanFocus = true
        };

        Label notesLabel = new() { Text = "Notas:", X = 2, Y = 7 };
        _notesField = new() {
            Text     = contact.Notas,
            X        = 15,
            Y        = 7,
            Width    = Dim.Fill(4),
            Height   = 3,
            CanFocus = true
        };

        Label phonesLabel = new() { Text = "Tel├®fonos (hasta 5):", X = 2, Y = 11 };

        string[] phoneParts = (contact.Telefonos ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < 5; i++) {
            _phoneFields[i] = new TextField {
                Text     = phoneParts.Length > i ? phoneParts[i] : "",
                X        = 15 + (i * 10),
                Y        = 11,
                Width    = 9,
                CanFocus = true
            };
        }

        Add(nameLabel, _nameField);
        Add(emailLabel, _emailField);
        Add(favoriteLabel, _favoriteField);
        Add(notesLabel, _notesField);
        Add(phonesLabel);
        foreach (TextField phoneField in _phoneFields) {
            Add(phoneField);
        }

        Button saveButton = new() { Text = "Guardar", IsDefault = true };
        saveButton.Accepting += (_, e) => {
            if (TrySave()) {
                e.Handled = true;
                App!.RequestStop();
            } else {
                e.Handled = true;
            }
        };

        Button cancelButton = new() { Text = "Cancelar" };
        cancelButton.Accepting += (_, e) => {
            Saved = false;
            e.Handled = true;
            App!.RequestStop();
        };

        AddButton(saveButton);
        AddButton(cancelButton);
        _nameField.SetFocus();
    }

    private bool TrySave() {
        string name = _nameField.Text?.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name)) {
            MessageBox.ErrorQuery(App!, "Validaci├│n", "El nombre es obligatorio.", "Ok");
            _nameField.SetFocus();
            return false;
        }

        string email = _emailField.Text?.ToString()?.Trim() ?? "";
        if (!string.IsNullOrEmpty(email) && !email.Contains('@')) {
            MessageBox.ErrorQuery(App!, "Validaci├│n", "El email debe contener '@'.", "Ok");
            _emailField.SetFocus();
            return false;
        }

        List<string> phones = new();
        foreach (TextField phoneField in _phoneFields) {
            string phone = phoneField.Text?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(phone)) {
                phones.Add(phone);
            }
        }

        Contact.Nombre    = name;
        Contact.Email     = email;
        Contact.Favorito  = _favoriteField.Value == CheckState.Checked;
        Contact.Notas     = _notesField.Text?.ToString() ?? "";
        Contact.Telefonos = string.Join(",", phones);

        Saved = true;
        return true;
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

public sealed class JsonAgendaIO {

    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        Encoder       = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public List<Contacto> Read(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"Archivo no encontrado: '{path}'");
        }

        string json = File.ReadAllText(path, Encoding.UTF8);
        try {
            return JsonSerializer.Deserialize<List<Contacto>>(json, JsonOptions) ?? new();
        } catch (JsonException ex) {
            throw new InvalidDataException($"JSON con formato inv├ílido: {ex.Message}", ex);
        }
    }

    public void Write(string path, IEnumerable<Contacto> contacts) {
        string json = JsonSerializer.Serialize(
            contacts.OrderBy(c => c.Id),
            JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }
}

public sealed class FilePathDialog : Dialog {

    private readonly TextField _pathField;

    public bool Confirmed { get; private set; }
    public string Path => _pathField.Text?.ToString()?.Trim() ?? "";

    public FilePathDialog(string title, string initialPath) {
        Title  = title;
        Width  = 60;
        Height = 8;

        Label pathLabel = new() { Text = "Ruta del archivo:", X = 2, Y = 1 };
        _pathField = new() {
            Text     = initialPath,
            X        = 2,
            Y        = 2,
            Width    = Dim.Fill(2),
            CanFocus = true
        };

        Button acceptButton = new() { Text = "Aceptar", IsDefault = true };
        acceptButton.Accepting += (_, e) => {
            if (!string.IsNullOrWhiteSpace(Path)) {
                Confirmed = true;
                e.Handled = true;
                App!.RequestStop();
            }
        };

        Button cancelButton = new() { Text = "Cancelar" };
        cancelButton.Accepting += (_, e) => {
            Confirmed = false;
            e.Handled = true;
            App!.RequestStop();
        };

        Add(pathLabel, _pathField);
        AddButton(acceptButton);
        AddButton(cancelButton);
        _pathField.SetFocus();
    }
}

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
