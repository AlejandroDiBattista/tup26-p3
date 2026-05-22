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

    private void RefreshFilteredContacts() {
        string query = searchField?.Text?.ToString() ?? "";
        int currentId = SelectedContact()?.Id ?? 0;

        filteredContacts.Clear();
        filteredContacts.AddRange(contacts
            .Where(c => (!onlyFavorites || c.Favorito) && MatchesSearch(c, query))
            .OrderByDescending(c => c.Favorito)
            .ThenBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase));

        listView?.SetSource(new ObservableCollection<string>(filteredContacts.Select(FormatContactListItem).ToList()));

        selectedIndex = 0;
        if (currentId != 0) {
            int found = filteredContacts.FindIndex(c => c.Id == currentId);
            selectedIndex = found >= 0 ? found : 0;
        }

        if (listView is not null && filteredContacts.Count > 0) {
            listView.SelectedItem = Math.Min(selectedIndex, filteredContacts.Count - 1);
        }

        UpdateDetails();
    }

    private static bool MatchesSearch(Contacto contact, string query) {
        if (string.IsNullOrWhiteSpace(query)) {
            return true;
        }

        return contact.Nombre.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || contact.Telefonos.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || contact.Email.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private static string FormatContactListItem(Contacto contact) {
        string favorite = contact.Favorito ? "* " : "  ";
        string email = string.IsNullOrWhiteSpace(contact.Email) ? "" : $" <{contact.Email}>";
        return $"{favorite}{contact.Nombre}{email}";
    }

    private Contacto? SelectedContact() {
        if (filteredContacts.Count == 0) {
            return null;
        }

        int index = listView is null ? selectedIndex : listView.SelectedItem ?? selectedIndex;
        if (index < 0 || index >= filteredContacts.Count) {
            index = 0;
        }

        return filteredContacts[index];
    }

    private void UpdateDetails() {
        Contacto? contact = SelectedContact();
        if (detailLabel is null) {
            return;
        }

        detailLabel.Text = contact is null
            ? "No hay contactos para mostrar."
            : BuildDetails(contact);
    }

    private static string BuildDetails(Contacto contact) {
        string favorite = contact.Favorito ? "Si" : "No";
        return
            $"Id: {contact.Id}\n" +
            $"Nombre: {contact.Nombre}\n" +
            $"Telefonos: {contact.Telefonos}\n" +
            $"Email: {contact.Email}\n" +
            $"Favorito: {favorite}\n\n" +
            $"Notas:\n{contact.Notas}";
    }

    private void NewContact() {
        ContactDialog dialog = new();
        App!.Run(dialog);

        if (!dialog.Accepted || dialog.Contact is null) {
            SetStatus("Alta cancelada.");
            return;
        }

        try {
            Contacto saved = dialog.Contact;
            int id = store.Insert(saved);
            saved.Id = id;
            contacts.Add(saved);
            RefreshFilteredContacts();
            SelectContact(id);
            SetStatus($"Contacto agregado: {saved.Nombre}.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al guardar", ex.Message, "Aceptar");
        }
    }

    private void EditSelectedContact() {
        Contacto? selected = SelectedContact();
        if (selected is null) {
            SetStatus("No hay contacto seleccionado para editar.");
            return;
        }

        ContactDialog dialog = new(selected);
        App!.Run(dialog);

        if (!dialog.Accepted || dialog.Contact is null) {
            SetStatus("Edicion cancelada.");
            return;
        }

        try {
            Contacto updated = dialog.Contact;
            store.Update(updated);

            int index = contacts.FindIndex(c => c.Id == updated.Id);
            if (index >= 0) {
                contacts[index] = updated;
            }

            RefreshFilteredContacts();
            SelectContact(updated.Id);
            SetStatus($"Contacto actualizado: {updated.Nombre}.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al actualizar", ex.Message, "Aceptar");
        }
    }

    private void DeleteSelectedContact() {
        Contacto? selected = SelectedContact();
        if (selected is null) {
            SetStatus("No hay contacto seleccionado para eliminar.");
            return;
        }

        int answer = MessageBox.Query(
            App!,
            "Confirmar eliminacion",
            $"Eliminar el contacto \"{selected.Nombre}\"?",
            "Eliminar",
            "Cancelar") ?? 1;

        if (answer != 0) {
            SetStatus("Eliminacion cancelada.");
            return;
        }

        try {
            store.Delete(selected);
            contacts.RemoveAll(c => c.Id == selected.Id);
            RefreshFilteredContacts();
            SetStatus($"Contacto eliminado: {selected.Nombre}.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al eliminar", ex.Message, "Aceptar");
        }
    }

    private void ImportJson() {
        string? path = AskForPath(App!, "Importar JSON", "Ruta del archivo JSON:", "Importar");
        if (string.IsNullOrWhiteSpace(path)) {
            SetStatus("Importacion cancelada.");
            return;
        }

        try {
            List<Contacto> imported = JsonAgendaIO.Read(path).ToList();
            int answer = MessageBox.Query(
                App!,
                "Confirmar importacion",
                $"Se agregaran {imported.Count} contacto(s). Continuar?",
                "Importar",
                "Cancelar") ?? 1;

            if (answer != 0) {
                SetStatus("Importacion cancelada.");
                return;
            }

            foreach (Contacto contact in imported) {
                contact.Id = 0;
                int id = store.Insert(contact);
                contact.Id = id;
                contacts.Add(contact);
            }

            RefreshFilteredContacts();
            SetStatus($"Importados {imported.Count} contacto(s) desde {path}.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al importar", ex.Message, "Aceptar");
        }
    }

    private void ExportJson() {
        string? path = AskForPath(App!, "Exportar JSON", "Ruta de salida:", "Exportar");
        if (string.IsNullOrWhiteSpace(path)) {
            SetStatus("Exportacion cancelada.");
            return;
        }

        try {
            JsonAgendaIO.Write(path, contacts);
            SetStatus($"Exportados {contacts.Count} contacto(s) a {path}.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al exportar", ex.Message, "Aceptar");
        }
    }

    private static string? AskForPath(IApplication app, string title, string prompt, string actionText) {
        Dialog dialog = new() {
            Title = title,
            Width = 72,
            Height = 8
        };

        Label label = new() {
            Text = prompt,
            X = 1,
            Y = 1,
            Width = 20
        };

        TextField pathField = new() {
            X = Pos.Right(label) + 1,
            Y = 1,
            Width = Dim.Fill(1)
        };

        string? result = null;

        Button accept = new() {
            Text = $"_{actionText}",
            IsDefault = true
        };
        accept.Accepting += (_, e) => {
            result = pathField.Text?.ToString();
            app.RequestStop();
            e.Handled = true;
        };

        Button cancel = new() {
            Text = "_Cancelar"
        };
        cancel.Accepting += (_, e) => {
            result = null;
            app.RequestStop();
            e.Handled = true;
        };

        dialog.Add(label, pathField);
        dialog.AddButton(accept);
        dialog.AddButton(cancel);
        app.Run(dialog);

        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }

    private void ToggleOnlyFavorites() {
        onlyFavorites = !onlyFavorites;
        RefreshFilteredContacts();
        SetStatus(onlyFavorites ? "Filtro activo: solo favoritos." : "Filtro de favoritos desactivado.");
    }

    private void ShowAbout() {
        MessageBox.Query(
            App!,
            "Acerca de",
            "Agenda de contactos\nTerminal.Gui v2 + SQLite + JSON",
            "Aceptar");
    }

    private void FocusSearch() {
        searchField.SetFocus();
        SetStatus("Busqueda activa.");
    }

    private void RequestExit() {
        App!.RequestStop();
    }

    private void SelectContact(int id) {
        int index = filteredContacts.FindIndex(c => c.Id == id);
        if (index >= 0) {
            listView.SelectedItem = index;
            selectedIndex = index;
            UpdateDetails();
        }
    }

    private void SetStatus(string message) {
        if (statusBar is not null) {
            statusBar.Text = message;
        }
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.N.WithCtrl || key == Key.F2) {
            NewContact();
            return true;
        }

        if (key == Key.F3 || key == Key.Enter) {
            EditSelectedContact();
            return true;
        }

        if (key == Key.D.WithCtrl || key == Key.Delete) {
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

        if (key == Key.Q.WithCtrl) {
            RequestExit();
            return true;
        }

        bool handled = base.OnKeyDown(key);
        selectedIndex = listView?.SelectedItem ?? selectedIndex;
        UpdateDetails();
        return handled;
    }
}

public sealed class ContactDialog : Dialog {
    private readonly TextField nameField;
    private readonly TextField[] phoneFields;
    private readonly TextField emailField;
    private readonly TextView notesField;
    private readonly CheckBox favoriteField;

    public new bool Accepted { get; private set; }
    public Contacto? Contact { get; private set; }

    public ContactDialog(Contacto? contact = null) {
        Contacto editing = contact?.Clone() ?? new Contacto();

        Title = contact is null ? "Nuevo contacto" : "Editar contacto";
        Width = 74;
        Height = 22;

        Label nameLabel = LabelAt("Nombre:", 1, 1);
        nameField = FieldAt(Pos.Right(nameLabel) + 1, 1, editing.Nombre);

        phoneFields = new TextField[5];
        List<Label> phoneLabels = [];
        string[] phones = editing.Telefonos
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(5)
            .ToArray();

        for (int i = 0; i < phoneFields.Length; i++) {
            Label phoneLabel = LabelAt($"Telefono {i + 1}:", 1, 3 + i);
            phoneLabels.Add(phoneLabel);
            phoneFields[i] = FieldAt(Pos.Right(phoneLabel) + 1, 3 + i, i < phones.Length ? phones[i] : "");
        }

        Label emailLabel = LabelAt("Email:", 1, 9);
        emailField = FieldAt(Pos.Right(emailLabel) + 1, 9, editing.Email);

        favoriteField = new CheckBox {
            Text = "Favorito",
            X = 13,
            Y = 11,
            Value = editing.Favorito ? CheckState.Checked : CheckState.UnChecked
        };

        Label notesLabel = LabelAt("Notas:", 1, 13);
        notesField = new TextView {
            X = 13,
            Y = 13,
            Width = Dim.Fill(1),
            Height = 4,
            Text = editing.Notas
        };

        Button saveButton = new() {
            Text = "_Guardar",
            IsDefault = true
        };
        saveButton.Accepting += (_, e) => {
            if (TryBuildContact(editing.Id, out Contacto? result)) {
                Contact = result;
                Accepted = true;
                App!.RequestStop();
            }

            e.Handled = true;
        };

        Button cancelButton = new() {
            Text = "_Cancelar"
        };
        cancelButton.Accepting += (_, e) => {
            Accepted = false;
            App!.RequestStop();
            e.Handled = true;
        };

        Add(nameLabel, nameField, emailLabel, emailField, favoriteField, notesLabel, notesField);
        for (int i = 0; i < phoneFields.Length; i++) {
            Add(phoneLabels[i], phoneFields[i]);
        }

        AddButton(saveButton);
        AddButton(cancelButton);
    }

    private bool TryBuildContact(int id, out Contacto? contact) {
        contact = null;
        string name = nameField.Text?.ToString()?.Trim() ?? "";
        string email = emailField.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name)) {
            MessageBox.ErrorQuery(App!, "Validacion", "El nombre no puede estar vacio.", "Aceptar");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) {
            MessageBox.ErrorQuery(App!, "Validacion", "El email debe contener @.", "Aceptar");
            return false;
        }

        string phones = string.Join(", ",
            phoneFields
                .Select(field => field.Text?.ToString()?.Trim() ?? "")
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        contact = new Contacto {
            Id = id,
            Nombre = name,
            Telefonos = phones,
            Email = email,
            Notas = notesField.Text?.ToString() ?? "",
            Favorito = favoriteField.Value == CheckState.Checked
        };

        return true;
    }

    private static Label LabelAt(string text, int x, int y) {
        return new Label {
            Text = text,
            X = x,
            Y = y,
            Width = 11
        };
    }

    private static TextField FieldAt(Pos x, int y, string text) {
        return new TextField {
            Text = text,
            X = x,
            Y = y,
            Width = Dim.Fill(1)
        };
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
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static IReadOnlyList<Contacto> Read(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException("El archivo JSON no existe.", path);
        }

        try {
            string json = File.ReadAllText(path, Encoding.UTF8);
            List<Contacto>? contacts = JsonSerializer.Deserialize<List<Contacto>>(json, Options);
            return contacts?.Select(c => {
                c.Id = 0;
                c.Nombre = c.Nombre?.Trim() ?? "";
                c.Telefonos ??= "";
                c.Email ??= "";
                c.Notas ??= "";
                return c;
            }).ToList() ?? [];
        }
        catch (JsonException ex) {
            throw new InvalidOperationException($"JSON con formato invalido: {ex.Message}", ex);
        }
    }

    public static void Write(string path, IEnumerable<Contacto> contacts) {
        string json = JsonSerializer.Serialize(contacts, Options);
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
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