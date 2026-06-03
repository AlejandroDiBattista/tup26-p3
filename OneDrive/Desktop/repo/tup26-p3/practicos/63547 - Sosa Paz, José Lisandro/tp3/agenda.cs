#:package Terminal.Gui@2.4.1
#:package Microsoft.Data.Sqlite@10.0.8
#:package Dapper@2.1.79
#:package Dapper.Contrib@2.0.78
#:property LangVersion=preview
#nullable enable
#pragma warning disable CS0618
#pragma warning disable IL2026
#pragma warning disable IL3050

using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var dbPath = args.Length > 0 ? args[0] : "agenda.db";

try
{
    using IApplication app = Application.Create();
    app.Init();

    using var store = new SqliteAgendaStore(dbPath);
    using var window = new AgendaWindow(app, store);

    app.Run(window);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al iniciar la agenda: {ex.Message}");
}

public sealed class AgendaWindow : Window
{
    private readonly IApplication app;
    private readonly SqliteAgendaStore store;
    private readonly List<Contacto> contacts;
    private readonly List<Contacto> filteredContacts = [];
    private readonly TextField searchField;
    private readonly ListView contactList;
    private readonly TextView detailView;
    private readonly Label statusLabel;
    private bool onlyFavorites;

    public AgendaWindow(IApplication app, SqliteAgendaStore store)
    {
        this.app = app;
        this.store = store;
        contacts = store.GetAll().ToList();

        Title = "AgendaT";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var menu = BuildMenu();
        searchField = new TextField
        {
            X = 10,
            Y = 2,
            Width = Dim.Fill(1),
            Text = ""
        };
        searchField.TextChanged += (_, _) => ApplyFilters();

        var searchLabel = new Label
        {
            X = 1,
            Y = 2,
            Text = "Buscar:"
        };

        contactList = new ListView
        {
            X = 0,
            Y = 4,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1),
            CanFocus = true
        };
        contactList.ValueChanged += (_, _) => ShowSelectedContact();
        contactList.Accepted += (_, _) => EditSelectedContact();

        detailView = new TextView
        {
            X = Pos.Right(contactList),
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ReadOnly = true,
            Text = ""
        };

        statusLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom(this) - 1,
            Width = Dim.Fill(),
            Height = 1,
            Text = "Listo"
        };

        Add(menu, searchLabel, searchField, contactList, detailView, statusLabel);
        ApplyFilters();
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.F2 || key == Key.N.WithCtrl)
        {
            NewContact();
            return true;
        }

        if (key == Key.F3 || key == Key.Enter)
        {
            EditSelectedContact();
            return true;
        }

        if (key == Key.Delete || key == Key.D.WithCtrl)
        {
            DeleteSelectedContact();
            return true;
        }

        if (key == Key.I.WithCtrl)
        {
            ImportJson();
            return true;
        }

        if (key == Key.E.WithCtrl)
        {
            ExportJson();
            return true;
        }

        if (key == Key.F4)
        {
            searchField.SetFocus();
            return true;
        }

        if (key == Key.Q.WithCtrl)
        {
            app.RequestStop();
            return true;
        }

        return base.OnKeyDown(key);
    }

    private MenuBar BuildMenu()
    {
        return new MenuBar(
        [
            new MenuBarItem("_Archivo",
            [
                new MenuItem("_Importar JSON", "Ctrl+I", ImportJson, Key.I.WithCtrl),
                new MenuItem("_Exportar JSON", "Ctrl+E", ExportJson, Key.E.WithCtrl),
                new MenuItem("_Salir", "Ctrl+Q", () => app.RequestStop(), Key.Q.WithCtrl)
            ]),
            new MenuBarItem("_Contactos",
            [
                new MenuItem("_Nuevo", "F2 / Ctrl+N", NewContact, Key.F2),
                new MenuItem("_Editar", "F3 / Enter", EditSelectedContact, Key.F3),
                new MenuItem("_Eliminar", "Del / Ctrl+D", DeleteSelectedContact, Key.Delete)
            ]),
            new MenuBarItem("_Ver",
            [
                new MenuItem("_Solo favoritos", "toggle", ToggleOnlyFavorites, Key.Empty)
            ]),
            new MenuBarItem("_Ayuda",
            [
                new MenuItem("_Acerca de", "", ShowAbout, Key.Empty)
            ])
        ]);
    }

    private void NewContact()
    {
        var dialog = new ContactDialog(app, new Contacto());
        app.Run(dialog);

        if (dialog.Result is null)
        {
            SetStatus("Operacion cancelada");
            return;
        }

        try
        {
            var saved = store.Insert(dialog.Result);
            contacts.Add(saved);
            ApplyFilters(saved.Id);
            SetStatus($"Contacto creado: {saved.Nombre}");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo crear el contacto", ex.Message);
        }
    }

    private void EditSelectedContact()
    {
        var selected = GetSelectedContact();
        if (selected is null)
        {
            SetStatus("No hay contacto seleccionado");
            return;
        }

        var dialog = new ContactDialog(app, selected.Clone());
        app.Run(dialog);

        if (dialog.Result is null)
        {
            SetStatus("Operacion cancelada");
            return;
        }

        try
        {
            store.Update(dialog.Result);
            var index = contacts.FindIndex(c => c.Id == dialog.Result.Id);
            if (index >= 0)
            {
                contacts[index] = dialog.Result;
            }

            ApplyFilters(dialog.Result.Id);
            SetStatus($"Contacto actualizado: {dialog.Result.Nombre}");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo actualizar el contacto", ex.Message);
        }
    }

    private void DeleteSelectedContact()
    {
        var selected = GetSelectedContact();
        if (selected is null)
        {
            SetStatus("No hay contacto seleccionado");
            return;
        }

        var answer = MessageBox.Query(app, "Confirmar", $"Eliminar a {selected.Nombre}?", "No", "Si");
        if (answer != 1)
        {
            SetStatus("Eliminacion cancelada");
            return;
        }

        try
        {
            store.Delete(selected);
            contacts.RemoveAll(c => c.Id == selected.Id);
            ApplyFilters();
            SetStatus($"Contacto eliminado: {selected.Nombre}");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar el contacto", ex.Message);
        }
    }

    private void ImportJson()
    {
        var path = AskPath("Importar JSON", "Ruta del archivo JSON:");
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Importacion cancelada");
            return;
        }

        try
        {
            var imported = JsonAgendaIO.Read(path).ToList();
            var answer = MessageBox.Query(app, "Confirmar importacion", $"Se agregaran {imported.Count} contactos.", "No", "Si");
            if (answer != 1)
            {
                SetStatus("Importacion cancelada");
                return;
            }

            foreach (var contact in imported)
            {
                contact.Id = 0;
                var saved = store.Insert(contact);
                contacts.Add(saved);
            }

            ApplyFilters();
            SetStatus($"Importados: {imported.Count}");
        }
        catch (FileNotFoundException)
        {
            ShowError("Archivo JSON inexistente", "No se encontro el archivo indicado.");
        }
        catch (JsonException ex)
        {
            ShowError("JSON con formato invalido", ex.Message);
        }
        catch (Exception ex)
        {
            ShowError("No se pudo importar", ex.Message);
        }
    }

    private void ExportJson()
    {
        var path = AskPath("Exportar JSON", "Ruta de salida:");
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Exportacion cancelada");
            return;
        }

        try
        {
            JsonAgendaIO.Write(path, contacts);
            SetStatus($"Exportados: {contacts.Count}");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo exportar", ex.Message);
        }
    }

    private void ToggleOnlyFavorites()
    {
        onlyFavorites = !onlyFavorites;
        ApplyFilters();
        SetStatus(onlyFavorites ? "Vista: solo favoritos" : "Vista: todos los contactos");
    }

    private void ShowAbout()
    {
        MessageBox.Query(app, "Acerca de", "AgendaT - Aplicacion de agenda TUI con SQLite y JSON.", "Aceptar");
    }

    private string? AskPath(string title, string prompt)
    {
        string? result = null;
        var dialog = new Dialog
        {
            Title = title,
            Width = 70,
            Height = 8
        };

        var label = new Label
        {
            X = 1,
            Y = 1,
            Text = prompt
        };
        var field = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2),
            Text = ""
        };
        var ok = new Button
        {
            X = Pos.Center() - 12,
            Y = 4,
            Text = "Aceptar"
        };
        var cancel = new Button
        {
            X = Pos.Right(ok) + 2,
            Y = 4,
            Text = "Cancelar"
        };

        ok.Accepted += (_, _) =>
        {
            result = field.Text?.ToString();
            dialog.RequestStop();
        };
        cancel.Accepted += (_, _) => dialog.RequestStop();

        dialog.Add(label, field, ok, cancel);
        app.Run(dialog);
        return result;
    }

    private void ApplyFilters(int selectedId = 0)
    {
        var query = (searchField.Text?.ToString() ?? "").Trim();
        filteredContacts.Clear();
        filteredContacts.AddRange(contacts.Where(c =>
            (!onlyFavorites || c.Favorito) &&
            (query.Length == 0 ||
             c.Nombre.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             c.Telefonos.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             c.Email.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
            .OrderByDescending(c => c.Favorito)
            .ThenBy(c => c.Nombre));

        contactList.SetSource(new ObservableCollection<string>(filteredContacts.Select(ToListText)));

        if (filteredContacts.Count > 0)
        {
            var selectedIndex = selectedId == 0 ? 0 : filteredContacts.FindIndex(c => c.Id == selectedId);
            contactList.SelectedItem = selectedIndex >= 0 ? selectedIndex : 0;
        }

        ShowSelectedContact();
        contactList.SetNeedsDraw();
    }

    private void ShowSelectedContact()
    {
        var selected = GetSelectedContact();
        detailView.Text = selected is null
            ? ""
            : $"Id: {selected.Id}\nNombre: {selected.Nombre}\nTelefonos: {selected.Telefonos}\nEmail: {selected.Email}\nFavorito: {(selected.Favorito ? "Si" : "No")}\n\nNotas:\n{selected.Notas}";
        detailView.SetNeedsDraw();
    }

    private Contacto? GetSelectedContact()
    {
        var index = contactList.SelectedItem;
        return index is >= 0 && index < filteredContacts.Count ? filteredContacts[index.Value] : null;
    }

    private static string ToListText(Contacto contact)
    {
        var marker = contact.Favorito ? "* " : "  ";
        return $"{marker}{contact.Nombre} - {contact.Telefonos} - {contact.Email}";
    }

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
        statusLabel.SetNeedsDraw();
    }

    private void ShowError(string title, string message)
    {
        MessageBox.ErrorQuery(app, title, message, "Aceptar");
        SetStatus(title);
    }
}

public sealed class ContactDialog : Dialog
{
    private readonly IApplication app;
    private readonly int id;
    private readonly TextField nameField = new();
    private readonly TextField[] phoneFields = [new(), new(), new(), new(), new()];
    private readonly TextField emailField = new();
    private readonly TextView notesField = new();
    private readonly CheckBox favoriteField = new();

    public new Contacto? Result { get; private set; }

    public ContactDialog(IApplication app, Contacto contact)
    {
        this.app = app;
        id = contact.Id;
        Title = contact.Id == 0 ? "Nuevo contacto" : "Editar contacto";
        Width = 72;
        Height = 22;

        AddField("Nombre:", nameField, 1, contact.Nombre);

        var phones = contact.Telefonos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < phoneFields.Length; i++)
        {
            AddField($"Telefono {i + 1}:", phoneFields[i], 3 + i, i < phones.Length ? phones[i] : "");
        }

        AddField("Email:", emailField, 9, contact.Email);

        var notesLabel = new Label
        {
            X = 2,
            Y = 11,
            Text = "Notas:"
        };
        notesField.X = 14;
        notesField.Y = 11;
        notesField.Width = Dim.Fill(2);
        notesField.Height = 5;
        notesField.Text = contact.Notas;

        favoriteField.X = 14;
        favoriteField.Y = 17;
        favoriteField.Text = "Favorito";
        favoriteField.Value = contact.Favorito ? CheckState.Checked : CheckState.UnChecked;

        var save = new Button
        {
            X = Pos.Center() - 14,
            Y = 19,
            Text = "Guardar"
        };
        var cancel = new Button
        {
            X = Pos.Right(save) + 2,
            Y = 19,
            Text = "Cancelar"
        };

        save.Accepted += (_, _) => Save();
        cancel.Accepted += (_, _) => RequestStop();

        Add(notesLabel, notesField, favoriteField, save, cancel);
    }

    private void AddField(string label, TextField field, int y, string value)
    {
        Add(new Label
        {
            X = 2,
            Y = y,
            Text = label
        });

        field.X = 14;
        field.Y = y;
        field.Width = Dim.Fill(2);
        field.Text = value;
        Add(field);
    }

    private void Save()
    {
        var name = (nameField.Text?.ToString() ?? "").Trim();
        if (name.Length == 0)
        {
            MessageBox.ErrorQuery(app, "Nombre vacio", "El nombre no puede estar vacio.", "Aceptar");
            return;
        }

        var email = (emailField.Text?.ToString() ?? "").Trim();
        if (email.Length > 0 && !email.Contains('@'))
        {
            MessageBox.ErrorQuery(app, "Email invalido", "El email debe contener @.", "Aceptar");
            return;
        }

        var phones = phoneFields
            .Select(f => (f.Text?.ToString() ?? "").Trim())
            .Where(p => p.Length > 0)
            .Take(5);

        Result = new Contacto
        {
            Id = id,
            Nombre = name,
            Telefonos = string.Join(", ", phones),
            Email = email,
            Notas = notesField.Text?.ToString() ?? "",
            Favorito = favoriteField.Value == CheckState.Checked
        };

        RequestStop();
    }
}

public sealed class SqliteAgendaStore : IDisposable
{
    private readonly SqliteConnection connection;

    public SqliteAgendaStore(string path)
    {
        connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        EnsureSchema();
    }

    public IEnumerable<Contacto> GetAll()
    {
        return connection.GetAll<Contacto>().OrderBy(c => c.Nombre);
    }

    public Contacto Insert(Contacto contact)
    {
        contact.Id = 0;
        contact.Id = (int)connection.Insert(contact);
        return contact;
    }

    public void Update(Contacto contact)
    {
        connection.Update(contact);
    }

    public void Delete(Contacto contact)
    {
        connection.Delete(contact);
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    private void EnsureSchema()
    {
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
}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IEnumerable<Contacto> Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }

        var json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<Contacto>>(json, Options) ?? [];
    }

    public static void Write(string path, IEnumerable<Contacto> contacts)
    {
        var json = JsonSerializer.Serialize(contacts, Options);
        File.WriteAllText(path, json, Encoding.UTF8);
    }
}

[Table("Contactos")]
public sealed class Contacto
{
    [Key]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Telefonos { get; set; } = "";

    public string Email { get; set; } = "";

    public string Notas { get; set; } = "";

    public bool Favorito { get; set; }

    public Contacto Clone()
    {
        return new Contacto
        {
            Id = Id,
            Nombre = Nombre,
            Telefonos = Telefonos,
            Email = Email,
            Notas = Notas,
            Favorito = Favorito
        };
    }
}
