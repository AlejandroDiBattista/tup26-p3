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

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
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

// Ventana principal
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
public class JsonAgendaIO {}

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