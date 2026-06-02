#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var dbPath = args.Length > 0 ? args[0] : "agenda.db";

try
{
    using var store = new SqliteAgendaStore(dbPath);
    using IApplication app = Application.Create().Init();
    app.Run(new AgendaWindow(store));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al iniciar AgendaT: {ex.Message}");
}

public sealed class AgendaWindow : Runnable
{
    private readonly SqliteAgendaStore store;
    private readonly List<Contacto> contacts;
    private readonly List<Contacto> filteredContacts = new();
    private readonly TextField searchField = new();
    private readonly ListView contactsList = new();
    private readonly TextView detailView = new();
    private readonly Label statusBar = new();
    private bool onlyFavorites;

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;
        contacts = store.GetAll().ToList();

        Title = "AgendaT";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var menu = BuildMenu();
        Add(menu);

        var searchLabel = new Label { X = 1, Y = 1, Text = "Buscar:" };
        searchField.X = Pos.Right(searchLabel) + 1;
        searchField.Y = 1;
        searchField.Width = Dim.Fill(1);
        searchField.TextChanged += (_, _) => RefreshFilteredContacts();

        contactsList.X = 1;
        contactsList.Y = 3;
        contactsList.Width = Dim.Percent(40);
        contactsList.Height = Dim.Fill(2);
        contactsList.Accepting += (_, e) =>
        {
            EditSelectedContact();
            e.Handled = true;
        };
        contactsList.ValueChanged += (_, _) => RefreshDetail();

        detailView.X = Pos.Right(contactsList) + 1;
        detailView.Y = 3;
        detailView.Width = Dim.Fill(1);
        detailView.Height = Dim.Fill(2);
        detailView.ReadOnly = true;
        detailView.WordWrap = true;

        statusBar.X = 0;
        statusBar.Y = Pos.AnchorEnd(1);
        statusBar.Width = Dim.Fill();
        statusBar.Height = 1;
        statusBar.Text = "";

        Add(searchLabel, searchField, contactsList, detailView, statusBar);
        RefreshFilteredContacts();
        SetStatus("Agenda lista.");
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.N.WithCtrl)
        {
            NewContact();
            return true;
        }

        if (key == Key.D.WithCtrl)
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

        if (key == Key.Q.WithCtrl)
        {
            RequestStop();
            return true;
        }

        if (key == Key.F2)
        {
            NewContact();
            return true;
        }

        if (key == Key.F3 || key == Key.Enter)
        {
            EditSelectedContact();
            return true;
        }

        if (key == Key.Delete)
        {
            DeleteSelectedContact();
            return true;
        }

        if (key == Key.F4)
        {
            searchField.SetFocus();
            return true;
        }

        return base.OnKeyDown(key);
    }

    private MenuBar BuildMenu()
    {
        return new MenuBar
        {
            Menus = new[]
            {
                new MenuBarItem("_Archivo", new[]
                {
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportJson),
                    new MenuItem("_Salir", "Ctrl+Q", RequestStop)
                }),
                new MenuBarItem("_Contactos", new[]
                {
                    new MenuItem("_Nuevo", "F2 / Ctrl+N", NewContact),
                    new MenuItem("_Editar", "F3 / Enter", EditSelectedContact),
                    new MenuItem("_Eliminar", "Del / Ctrl+D", DeleteSelectedContact)
                }),
                new MenuBarItem("_Ver", new[]
                {
                    new MenuItem("_Solo favoritos", "", ToggleOnlyFavorites)
                }),
                new MenuBarItem("_Ayuda", new[]
                {
                    new MenuItem("_Acerca de", "", ShowAbout)
                })
            }
        };
    }

    private void NewContact()
    {
        using var dialog = new ContactDialog("Nuevo contacto", new Contacto());
        App!.Run(dialog);
        if (!dialog.Saved || dialog.Contact is null)
        {
            return;
        }

        store.Insert(dialog.Contact);
        contacts.Clear();
        contacts.AddRange(store.GetAll());
        RefreshFilteredContacts();
        SelectContact(dialog.Contact.Id);
        SetStatus("Contacto creado.");
    }

    private void EditSelectedContact()
    {
        var selected = GetSelectedContact();
        if (selected is null)
        {
            SetStatus("No hay contacto seleccionado.");
            return;
        }

        using var dialog = new ContactDialog("Editar contacto", selected.Clone());
        App!.Run(dialog);
        if (!dialog.Saved || dialog.Contact is null)
        {
            return;
        }

        store.Update(dialog.Contact);
        var index = contacts.FindIndex(c => c.Id == dialog.Contact.Id);
        if (index >= 0)
        {
            contacts[index] = dialog.Contact.Clone();
        }
        RefreshFilteredContacts();
        SelectContact(dialog.Contact.Id);
        SetStatus("Contacto actualizado.");
    }

    private void DeleteSelectedContact()
    {
        var selected = GetSelectedContact();
        if (selected is null)
        {
            SetStatus("No hay contacto seleccionado.");
            return;
        }

        var answer = MessageBox.Query(App!, "Confirmar", $"Eliminar a {selected.Nombre}?", "Si", "No");
        if (answer != 0)
        {
            return;
        }

        store.Delete(selected.Id);
        contacts.RemoveAll(c => c.Id == selected.Id);
        RefreshFilteredContacts();
        SetStatus("Contacto eliminado.");
    }

    private void ImportJson()
    {
        var path = AskPath("Importar JSON", "Ruta del archivo JSON:");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var imported = JsonAgendaIO.Read(path).ToList();
            var answer = MessageBox.Query(App!, "Importar", $"Se agregaran {imported.Count} contactos. Continuar?", "Si", "No");
            if (answer != 0)
            {
                return;
            }

            foreach (var contact in imported)
            {
                contact.Id = 0;
                store.Insert(contact);
            }

            contacts.Clear();
            contacts.AddRange(store.GetAll());
            RefreshFilteredContacts();
            SetStatus($"{imported.Count} contactos importados.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error al importar", ex.Message, "Aceptar");
            SetStatus("No se importo el JSON.");
        }
    }

    private void ExportJson()
    {
        var path = AskPath("Exportar JSON", "Ruta de salida:");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            JsonAgendaIO.Write(path, contacts);
            SetStatus($"Contactos exportados a {path}.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error al exportar", ex.Message, "Aceptar");
            SetStatus("No se exporto el JSON.");
        }
    }

    private void ToggleOnlyFavorites()
    {
        onlyFavorites = !onlyFavorites;
        RefreshFilteredContacts();
        SetStatus(onlyFavorites ? "Mostrando solo favoritos." : "Mostrando todos los contactos.");
    }

    private void ShowAbout()
    {
        MessageBox.Query(App!, "Acerca de", "AgendaT - Agenda TUI con SQLite y JSON", "Aceptar");
    }

    private void RefreshFilteredContacts()
    {
        var selectedId = GetSelectedContact()?.Id;
        var query = searchField.Text?.ToString() ?? "";

        filteredContacts.Clear();
        filteredContacts.AddRange(contacts
            .Where(c => MatchesFilters(c, query))
            .OrderByDescending(c => c.Favorito)
            .ThenBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList());

        contactsList.SetSource(new ObservableCollection<string>(filteredContacts.Select(FormatContact).ToList()));
        if (selectedId.HasValue)
        {
            SelectContact(selectedId.Value);
        }
        RefreshDetail();
    }

    private bool MatchesFilters(Contacto contact, string query)
    {
        if (onlyFavorites && !contact.Favorito)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Contains(contact.Nombre, query)
            || Contains(contact.Telefonos, query)
            || Contains(contact.Email, query);
    }

    private static bool Contains(string text, string query)
    {
        return text.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private void RefreshDetail()
    {
        var selected = GetSelectedContact();
        detailView.Text = selected is null
            ? ""
            : $"Id: {selected.Id}\nNombre: {selected.Nombre}\nTelefonos: {selected.Telefonos}\nEmail: {selected.Email}\nFavorito: {(selected.Favorito ? "Si" : "No")}\n\nNotas:\n{selected.Notas}";
    }

    private Contacto? GetSelectedContact()
    {
        var selectedItem = contactsList.SelectedItem;
        if (!selectedItem.HasValue || selectedItem.Value < 0 || selectedItem.Value >= filteredContacts.Count)
        {
            return null;
        }

        return filteredContacts[selectedItem.Value];
    }

    private void SelectContact(int id)
    {
        var index = filteredContacts.FindIndex(c => c.Id == id);
        if (index >= 0)
        {
            contactsList.SelectedItem = index;
        }
    }

    private static string FormatContact(Contacto contact)
    {
        return $"{(contact.Favorito ? "*" : " ")} {contact.Nombre}";
    }

    private void SetStatus(string message)
    {
        statusBar.Text = message;
        SetNeedsDraw();
    }

    private string? AskPath(string title, string label)
    {
        var dialog = new Dialog
        {
            Title = title,
            Width = 70,
            Height = 8
        };
        var pathLabel = new Label { X = 1, Y = 1, Text = label };
        var pathField = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };
        var ok = new Button { Text = "Aceptar", IsDefault = true };
        var cancel = new Button { Text = "Cancelar" };
        string? result = null;

        ok.Accepting += (_, _) =>
        {
            result = pathField.Text?.ToString();
            App!.RequestStop();
        };
        cancel.Accepting += (_, _) => App!.RequestStop();

        dialog.Add(pathLabel, pathField);
        dialog.AddButton(ok);
        dialog.AddButton(cancel);
        App!.Run(dialog);
        return result;
    }
}

public sealed class ContactDialog : Dialog
{
    private readonly TextField nameField = new();
    private readonly TextField phone1Field = new();
    private readonly TextField phone2Field = new();
    private readonly TextField phone3Field = new();
    private readonly TextField phone4Field = new();
    private readonly TextField phone5Field = new();
    private readonly TextField emailField = new();
    private readonly TextView notesField = new();
    private readonly CheckBox favoriteCheck = new() { Text = "Favorito" };
    private readonly int contactId;

    public bool Saved { get; private set; }
    public Contacto? Contact { get; private set; }

    public ContactDialog(string title, Contacto contact)
    {
        Title = title;
        Width = 72;
        Height = 23;
        contactId = contact.Id;

        AddLabelAndField(1, "Nombre:", nameField, contact.Nombre);
        AddLabelAndField(3, "Telefono 1:", phone1Field, GetPhone(contact, 0));
        AddLabelAndField(4, "Telefono 2:", phone2Field, GetPhone(contact, 1));
        AddLabelAndField(5, "Telefono 3:", phone3Field, GetPhone(contact, 2));
        AddLabelAndField(6, "Telefono 4:", phone4Field, GetPhone(contact, 3));
        AddLabelAndField(7, "Telefono 5:", phone5Field, GetPhone(contact, 4));
        AddLabelAndField(9, "Email:", emailField, contact.Email);

        var notesLabel = new Label { X = 1, Y = 11, Text = "Notas:" };
        notesField.X = 14;
        notesField.Y = 11;
        notesField.Width = Dim.Fill(2);
        notesField.Height = 5;
        notesField.Text = contact.Notas;
        notesField.WordWrap = true;

        favoriteCheck.X = 14;
        favoriteCheck.Y = 17;
        favoriteCheck.Value = contact.Favorito ? CheckState.Checked : CheckState.UnChecked;

        Add(notesLabel, notesField, favoriteCheck);

        var save = new Button { Text = "Guardar", IsDefault = true };
        var cancel = new Button { Text = "Cancelar" };
        save.Accepting += (_, _) => Save();
        cancel.Accepting += (_, _) => App!.RequestStop();
        AddButton(save);
        AddButton(cancel);
    }

    private void AddLabelAndField(int y, string label, TextField field, string value)
    {
        Add(new Label { X = 1, Y = y, Text = label });
        field.X = 14;
        field.Y = y;
        field.Width = Dim.Fill(2);
        field.Text = value;
        Add(field);
    }

    private static string GetPhone(Contacto contact, int index)
    {
        var phones = SplitPhones(contact.Telefonos);
        return index < phones.Count ? phones[index] : "";
    }

    private void Save()
    {
        var name = nameField.Text?.ToString().Trim() ?? "";
        var email = emailField.Text?.ToString().Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.ErrorQuery(App!, "Validacion", "El nombre no puede estar vacio.", "Aceptar");
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
        {
            MessageBox.ErrorQuery(App!, "Validacion", "El email debe contener @.", "Aceptar");
            return;
        }

        Contact = new Contacto
        {
            Id = contactId,
            Nombre = name,
            Telefonos = string.Join(", ", new[]
            {
                phone1Field.Text?.ToString().Trim() ?? "",
                phone2Field.Text?.ToString().Trim() ?? "",
                phone3Field.Text?.ToString().Trim() ?? "",
                phone4Field.Text?.ToString().Trim() ?? "",
                phone5Field.Text?.ToString().Trim() ?? ""
            }.Where(p => !string.IsNullOrWhiteSpace(p))),
            Email = email,
            Notas = notesField.Text?.ToString() ?? "",
            Favorito = favoriteCheck.Value == CheckState.Checked
        };

        Saved = true;
        App!.RequestStop();
    }

    private static List<string> SplitPhones(string phones)
    {
        return phones
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(5)
            .ToList();
    }
}

public sealed class SqliteAgendaStore : IDisposable
{
    private readonly SqliteConnection connection;

    public SqliteAgendaStore(string dbPath)
    {
        connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        EnsureSchema();
    }

    public IEnumerable<Contacto> GetAll()
    {
        return connection.GetAll<Contacto>();
    }

    public void Insert(Contacto contact)
    {
        Validate(contact);
        var id = connection.Insert(contact);
        contact.Id = (int)id;
    }

    public void Update(Contacto contact)
    {
        Validate(contact);
        connection.Update(contact);
    }

    public void Delete(int id)
    {
        connection.Delete(new Contacto { Id = id });
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

    private static void Validate(Contacto contact)
    {
        if (string.IsNullOrWhiteSpace(contact.Nombre))
        {
            throw new InvalidOperationException("El nombre no puede estar vacio.");
        }

        if (!string.IsNullOrWhiteSpace(contact.Email) && !contact.Email.Contains('@'))
        {
            throw new InvalidOperationException("El email debe contener @.");
        }
    }
}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static IEnumerable<Contacto> Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("El archivo JSON no existe.", path);
        }

        try
        {
            var json = File.ReadAllText(path);
            var contacts = JsonSerializer.Deserialize<List<Contacto>>(json, JsonOptions);
            return contacts ?? new List<Contacto>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"JSON con formato invalido: {ex.Message}", ex);
        }
    }

    public static void Write(string path, IEnumerable<Contacto> contacts)
    {
        var json = JsonSerializer.Serialize(contacts, JsonOptions);
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
    }
}

[Table("Contactos")]
public sealed class Contacto
{
    [Key] public int Id { get; set; }
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
