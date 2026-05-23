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
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

try {
    using SqliteAgendaStore store = new(dbPath);
    using IApplication app = Application.Create().Init();
    app.Run(new AgendaWindow(store));
}
catch (Exception ex) {
    Console.Error.WriteLine($"Error al iniciar la agenda: {ex.Message}");
}

public sealed class AgendaWindow : Window {
    private readonly SqliteAgendaStore store;
    private readonly List<Contacto> contacts = [];
    private readonly List<Contacto> filteredContacts = [];

    private TextField searchField = null!;
    private ListView listView = null!;
    private TextView detailView = null!;
    private StatusBar statusBar = null!;
    private bool onlyFavorites;
    private int selectedIndex;

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;

        Title = "Agenda - Terminal.Gui";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        LoadContacts();
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
                new MenuBarItem("A_yuda", [
                    new MenuItem("_Acerca de", null!, ShowAbout)
                ])
            ]
        };

        Label searchLabel = new() {
            Text = "Buscar:",
            X = 1,
            Y = 2,
            Width = 8
        };

        searchField = new TextField {
            X = Pos.Right(searchLabel) + 1,
            Y = 2,
            Width = Dim.Fill(1)
        };
        searchField.TextChanged += (_, _) => ApplyFilters();

        FrameView listFrame = new() {
            Title = "Contactos",
            X = 0,
            Y = 4,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1)
        };

        listView = new ListView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        listView.ValueChanged += (_, _) => {
            selectedIndex = listView.SelectedItem ?? 0;
            UpdateDetail();
        };
        listView.Accepting += (_, e) => {
            EditSelectedContact();
            e.Handled = true;
        };
        listFrame.Add(listView);

        FrameView detailFrame = new() {
            Title = "Detalle",
            X = Pos.Right(listFrame),
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        detailView = new TextView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        detailFrame.Add(detailView);

        statusBar = new StatusBar([
            new Shortcut(Key.F2, "Nuevo", NewContact),
            new Shortcut(Key.F3, "Editar", EditSelectedContact),
            new Shortcut(Key.DeleteChar, "Eliminar", DeleteSelectedContact),
            new Shortcut(Key.F4, "Buscar", FocusSearch),
            new Shortcut(Key.Q.WithCtrl, "Salir", RequestExit)
        ]);

        Add(menu, searchLabel, searchField, listFrame, detailFrame, statusBar);
    }

    private void LoadContacts() {
        contacts.Clear();
        contacts.AddRange(store.GetAll());
        ApplyFilters();
        SetStatus($"Base abierta: {store.DatabasePath}");
    }

    private void ApplyFilters() {
        string query = searchField.Text.ToString() ?? "";
        query = query.Trim();

        filteredContacts.Clear();
        IEnumerable<Contacto> source = contacts;

        if (onlyFavorites) {
            source = source.Where(c => c.Favorito);
        }

        if (!string.IsNullOrWhiteSpace(query)) {
            source = source.Where(c =>
                Contains(c.Nombre, query) ||
                Contains(c.Telefonos, query) ||
                Contains(c.Email, query));
        }

        filteredContacts.AddRange(source.OrderByDescending(c => c.Favorito).ThenBy(c => c.Nombre));
        listView.SetSource(new ObservableCollection<string>(filteredContacts.Select(FormatContactListItem)));

        UpdateSelectionAfterFilter();
        UpdateDetail();
    }

    private void UpdateSelectionAfterFilter() {
        if (filteredContacts.Count == 0) {
            selectedIndex = 0;
            listView.SelectedItem = null;
            return;
        }

        if (selectedIndex >= filteredContacts.Count) {
            selectedIndex = filteredContacts.Count - 1;
        }

        if (selectedIndex < 0) {
            selectedIndex = 0;
        }

        listView.SelectedItem = selectedIndex;
    }

    private static bool Contains(string value, string query) {
        return value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private static string FormatContactListItem(Contacto contact) {
        string favorite = contact.Favorito ? "* " : "  ";
        return $"{favorite}{contact.Nombre}";
    }

    private Contacto? GetSelectedContact() {
        if (selectedIndex < 0 || selectedIndex >= filteredContacts.Count) {
            return null;
        }

        return filteredContacts[selectedIndex];
    }

    private void UpdateDetail() {
        Contacto? contact = GetSelectedContact();

        if (contact is null) {
            detailView.Text = "Sin contactos para mostrar.";
            return;
        }

        detailView.Text =
            $"Id: {contact.Id}\n" +
            $"Nombre: {contact.Nombre}\n" +
            $"Telefonos: {contact.Telefonos}\n" +
            $"Email: {contact.Email}\n" +
            $"Favorito: {(contact.Favorito ? "Si" : "No")}\n\n" +
            $"Notas:\n{contact.Notas}";
    }

    private void NewContact() {
        ContactDialog dialog = new("Nuevo contacto", new Contacto());
        App!.Run(dialog);

        if (!dialog.WasAccepted || dialog.Contact is null) {
            SetStatus("Alta cancelada.");
            return;
        }

        Contacto saved = dialog.Contact;
        store.Insert(saved);
        contacts.Add(saved);
        ApplyFilters();
        SetStatus($"Contacto creado: {saved.Nombre}");
    }

    private void EditSelectedContact() {
        Contacto? selected = GetSelectedContact();
        if (selected is null) {
            SetStatus("No hay contacto seleccionado.");
            return;
        }

        ContactDialog dialog = new("Editar contacto", selected.Clone());
        App!.Run(dialog);

        if (!dialog.WasAccepted || dialog.Contact is null) {
            SetStatus("Edicion cancelada.");
            return;
        }

        store.Update(dialog.Contact);
        int index = contacts.FindIndex(c => c.Id == dialog.Contact.Id);
        if (index >= 0) {
            contacts[index] = dialog.Contact;
        }

        ApplyFilters();
        SetStatus($"Contacto actualizado: {dialog.Contact.Nombre}");
    }

    private void DeleteSelectedContact() {
        Contacto? selected = GetSelectedContact();
        if (selected is null) {
            SetStatus("No hay contacto seleccionado.");
            return;
        }

        int? answer = MessageBox.Query(App!, "Confirmar eliminacion", $"Eliminar a {selected.Nombre}?", "Si", "No");
        if (answer != 0) {
            SetStatus("Eliminacion cancelada.");
            return;
        }

        store.Delete(selected);
        contacts.RemoveAll(c => c.Id == selected.Id);
        ApplyFilters();
        SetStatus($"Contacto eliminado: {selected.Nombre}");
    }

    private void ImportJson() {
        string? path = AskPath("Importar JSON", "Ruta del archivo JSON:");
        if (string.IsNullOrWhiteSpace(path)) {
            SetStatus("Importacion cancelada.");
            return;
        }

        try {
            List<Contacto> imported = JsonAgendaIO.Read(path);
            int? answer = MessageBox.Query(
                App!,
                "Confirmar importacion",
                $"Se agregaran {imported.Count} contactos. Continuar?",
                "Si",
                "No");

            if (answer != 0) {
                SetStatus("Importacion cancelada.");
                return;
            }

            foreach (Contacto contact in imported) {
                contact.Id = 0;
                store.Insert(contact);
                contacts.Add(contact);
            }

            ApplyFilters();
            SetStatus($"Contactos importados: {imported.Count}");
        }
        catch (Exception ex) {
            ShowError("Error al importar", ex.Message);
            SetStatus("No se pudo importar el archivo.");
        }
    }

    private void ExportJson() {
        string? path = AskPath("Exportar JSON", "Ruta de salida:");
        if (string.IsNullOrWhiteSpace(path)) {
            SetStatus("Exportacion cancelada.");
            return;
        }

        try {
            JsonAgendaIO.Write(path, contacts);
            SetStatus($"Contactos exportados: {contacts.Count}");
        }
        catch (Exception ex) {
            ShowError("Error al exportar", ex.Message);
            SetStatus("No se pudo exportar el archivo.");
        }
    }

    private string? AskPath(string title, string labelText) {
        Dialog dialog = new() {
            Title = title,
            Width = 70,
            Height = 8
        };

        Label label = new() {
            Text = labelText,
            X = 1,
            Y = 1,
            Width = 20
        };

        TextField pathField = new() {
            X = Pos.Right(label) + 1,
            Y = 1,
            Width = Dim.Fill(2)
        };

        string? result = null;
        Button okButton = new() {
            Text = "_Aceptar",
            IsDefault = true
        };
        okButton.Accepting += (_, e) => {
            result = pathField.Text.ToString()?.Trim();
            dialog.RequestStop();
            e.Handled = true;
        };

        Button cancelButton = new() {
            Text = "_Cancelar"
        };
        cancelButton.Accepting += (_, e) => {
            result = null;
            dialog.RequestStop();
            e.Handled = true;
        };

        dialog.Add(label, pathField);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);
        App!.Run(dialog);
        return result;
    }

    private void ToggleOnlyFavorites() {
        onlyFavorites = !onlyFavorites;
        ApplyFilters();
        SetStatus(onlyFavorites ? "Mostrando solo favoritos." : "Mostrando todos los contactos.");
    }

    private void ShowAbout() {
        MessageBox.Query(App!, "Acerca de", "AgendaT - Trabajo Practico 3\nTerminal.Gui + SQLite + JSON", "Aceptar");
    }

    private void FocusSearch() {
        searchField.SetFocus();
        SetStatus("Busqueda activa.");
    }

    private void RequestExit() {
        App!.RequestStop();
    }

    private void SetStatus(string message) {
        statusBar.Text = message;
    }

    private void ShowError(string title, string message) {
        MessageBox.ErrorQuery(App!, title, message, "Aceptar");
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            RequestExit();
            return true;
        }

        if (key == Key.N.WithCtrl || key == Key.F2) {
            NewContact();
            return true;
        }

        if (key == Key.F3 || key == Key.Enter) {
            EditSelectedContact();
            return true;
        }

        if (key == Key.DeleteChar || key == Key.D.WithCtrl) {
            DeleteSelectedContact();
            return true;
        }

        if (key == Key.I.WithCtrl) {
            ImportJson();
            return true;
        }

        if (key == Key.E.WithCtrl) {
            ExportJson();
            return true;
        }

        if (key == Key.F4) {
            FocusSearch();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class ContactDialog : Dialog {
    private readonly TextField nameField;
    private readonly TextField phone1Field;
    private readonly TextField phone2Field;
    private readonly TextField phone3Field;
    private readonly TextField phone4Field;
    private readonly TextField phone5Field;
    private readonly TextField emailField;
    private readonly TextView notesField;
    private readonly CheckBox favoriteField;
    private readonly Contacto original;

    public bool WasAccepted { get; private set; }
    public Contacto? Contact { get; private set; }

    public ContactDialog(string title, Contacto contact) {
        original = contact.Clone();
        Title = title;
        Width = 76;
        Height = 23;

        Label nameLabel = NewLabel("Nombre:", 1);
        nameField = NewTextField(12, 1, 58, original.Nombre);

        Label emailLabel = NewLabel("Email:", 3);
        emailField = NewTextField(12, 3, 58, original.Email);

        Label phonesLabel = NewLabel("Telefonos:", 5);
        string[] phones = original.Telefonos
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(5)
            .ToArray();

        phone1Field = NewTextField(12, 5, 25, phones.ElementAtOrDefault(0) ?? "");
        phone2Field = NewTextField(39, 5, 25, phones.ElementAtOrDefault(1) ?? "");
        phone3Field = NewTextField(12, 7, 25, phones.ElementAtOrDefault(2) ?? "");
        phone4Field = NewTextField(39, 7, 25, phones.ElementAtOrDefault(3) ?? "");
        phone5Field = NewTextField(12, 9, 25, phones.ElementAtOrDefault(4) ?? "");

        favoriteField = new CheckBox {
            Text = "Favorito",
            X = 39,
            Y = 9,
            Value = original.Favorito ? CheckState.Checked : CheckState.UnChecked
        };

        Label notesLabel = NewLabel("Notas:", 11);
        notesField = new TextView {
            X = 12,
            Y = 11,
            Width = 58,
            Height = 5,
            Text = original.Notas,
            WordWrap = true
        };

        Button saveButton = new() {
            Text = "_Guardar",
            IsDefault = true
        };
        saveButton.Accepting += (_, e) => {
            TryAccept();
            e.Handled = true;
        };

        Button cancelButton = new() {
            Text = "_Cancelar"
        };
        cancelButton.Accepting += (_, e) => {
            WasAccepted = false;
            RequestStop();
            e.Handled = true;
        };

        Add(nameLabel, nameField, emailLabel, emailField, phonesLabel);
        Add(phone1Field, phone2Field, phone3Field, phone4Field, phone5Field);
        Add(favoriteField, notesLabel, notesField);
        AddButton(saveButton);
        AddButton(cancelButton);
    }

    private static Label NewLabel(string text, int y) {
        return new Label {
            Text = text,
            X = 1,
            Y = y,
            Width = 10
        };
    }

    private static TextField NewTextField(int x, int y, int width, string text) {
        return new TextField {
            X = x,
            Y = y,
            Width = width,
            Text = text
        };
    }

    private void TryAccept() {
        string name = (nameField.Text.ToString() ?? "").Trim();
        string email = (emailField.Text.ToString() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name)) {
            ShowValidationError("El nombre no puede estar vacio.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) {
            ShowValidationError("El email debe contener @.");
            return;
        }

        string phones = string.Join(", ", new[] {
            phone1Field.Text.ToString(),
            phone2Field.Text.ToString(),
            phone3Field.Text.ToString(),
            phone4Field.Text.ToString(),
            phone5Field.Text.ToString()
        }.Select(p => (p ?? "").Trim()).Where(p => p.Length > 0));

        Contact = new Contacto {
            Id = original.Id,
            Nombre = name,
            Telefonos = phones,
            Email = email,
            Notas = notesField.Text.ToString() ?? "",
            Favorito = favoriteField.Value == CheckState.Checked
        };

        WasAccepted = true;
        RequestStop();
    }

    private void ShowValidationError(string message) {
        MessageBox.ErrorQuery(App!, "Error de validacion", message, "Aceptar");
    }
}

public sealed class SqliteAgendaStore : IDisposable {
    private readonly SqliteConnection connection;

    public string DatabasePath { get; }

    public SqliteAgendaStore(string databasePath) {
        DatabasePath = databasePath;
        connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        EnsureSchema();
    }

    public List<Contacto> GetAll() {
        return connection.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
    }

    public void Insert(Contacto contact) {
        long id = connection.Insert(contact);
        contact.Id = (int)id;
    }

    public void Update(Contacto contact) {
        connection.Update(contact);
    }

    public void Delete(Contacto contact) {
        connection.Delete(contact);
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

    public void Dispose() {
        connection.Dispose();
    }
}

public static class JsonAgendaIO {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static List<Contacto> Read(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException("El archivo JSON no existe.", path);
        }

        string json = File.ReadAllText(path, Encoding.UTF8);
        List<Contacto>? contacts = JsonSerializer.Deserialize<List<Contacto>>(json, Options);

        if (contacts is null) {
            throw new InvalidDataException("El JSON no contiene una lista de contactos valida.");
        }

        foreach (Contacto contact in contacts) {
            contact.Id = 0;
            contact.Nombre ??= "";
            contact.Telefonos ??= "";
            contact.Email ??= "";
            contact.Notas ??= "";
            ValidateImportedContact(contact);
        }

        return contacts;
    }

    private static void ValidateImportedContact(Contacto contact) {
        if (string.IsNullOrWhiteSpace(contact.Nombre)) {
            throw new InvalidDataException("El JSON contiene un contacto con nombre vacio.");
        }

        if (!string.IsNullOrWhiteSpace(contact.Email) && !contact.Email.Contains('@')) {
            throw new InvalidDataException($"El JSON contiene un email invalido: {contact.Email}");
        }
    }

    public static void Write(string path, IEnumerable<Contacto> contacts) {
        string json = JsonSerializer.Serialize(contacts, Options);
        File.WriteAllText(path, json, Encoding.UTF8);
    }
}

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int Id { get; set; }
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
