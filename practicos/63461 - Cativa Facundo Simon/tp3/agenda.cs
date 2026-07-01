#!/usr/bin/env dotnet run
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui;



var dbPath = args.Length > 0 ? args[0] : "agenda.db";
var store = new SqliteAgendaStore(dbPath);
var jsonIo = new JsonAgendaIO();

Application.Init();
var window = new AgendaWindow(store, jsonIo);
Application.Run(window);
Application.Shutdown();

// ============================================================================
// AgendaWindow
// ============================================================================

public sealed class AgendaWindow : Window
{
    private SqliteAgendaStore _store;
    private JsonAgendaIO _jsonIo;
    private List<Contacto> _contacts = new();
    private List<Contacto> _filtered = new();
    private bool _onlyFav = false;
    private Contacto? _selected;

    private TextField _search = null!;
    private ListView _list = null!;
    private Label _details = null!;
    private Label _status = null!;

    public AgendaWindow(SqliteAgendaStore store, JsonAgendaIO jsonIo)
        : base("📋 Agenda")
    {
        _store = store;
        _jsonIo = jsonIo;
        _contacts = _store.GetAll().ToList();
        _filtered = new(_contacts);

        BuildMenu();
        BuildUI();
        RefreshList();
        Msg("Listo");
    }

    private void BuildMenu()
    {
        var menu = new MenuBar(new MenuBarItem[]
        {
            new("_File", new MenuItem[]
            {
                new("_Import JSON", "", () => Import()),
                new("_Export JSON", "", () => Export()),
                null!,
                new("E_xit", "", () => Application.RequestStop())
            }),
            new("_Contacts", new MenuItem[]
            {
                new("_New", "", () => NewContact()),
                new("_Edit", "", () => EditContact()),
                new("_Delete", "", () => DeleteContact())
            }),
            new("_View", new MenuItem[]
            {
                new("_Favorites Only", "", () => ToggleFav())
            }),
        });
        Add(menu);
    }

    private void BuildUI()
    {
        var lbl1 = new Label("Search:") { X = 1, Y = 1 };
        _search = new TextField("") { X = 12, Y = 1, Width = Dim.Fill(2), CanFocus = true };
        _search.TextChanged += _ => RefreshList();

        var lbl2 = new Label("Contacts:") { X = 1, Y = 3 };
        _list = new ListView(new()) { X = 1, Y = 4, Width = 28, Height = Dim.Fill(4), CanFocus = true };
        _list.SelectedItemChanged += _ => ShowDetails();

        var lbl3 = new Label("Details:") { X = 31, Y = 3 };
        _details = new Label("") { X = 31, Y = 4, Width = Dim.Fill(2), Height = Dim.Fill(2) };

        _status = new Label("") { X = 1, Y = Dim.Fill(0), Width = Dim.Fill(1) };

        Add(lbl1, _search, lbl2, _list, lbl3, _details, _status);

        KeyBindings.Add(Key.CtrlMask | Key.N, NewContact);
        KeyBindings.Add(Key.F2, NewContact);
        KeyBindings.Add(Key.CtrlMask | Key.D, DeleteContact);
        KeyBindings.Add(Key.Delete, DeleteContact);
        KeyBindings.Add(Key.F3, EditContact);
        KeyBindings.Add(Key.Enter, EditContact);
        KeyBindings.Add(Key.CtrlMask | Key.I, Import);
        KeyBindings.Add(Key.CtrlMask | Key.E, Export);
        KeyBindings.Add(Key.F4, () => SetFocusTo(_search));
        KeyBindings.Add(Key.CtrlMask | Key.Q, () => Application.RequestStop());
    }

    private void RefreshList()
    {
        var q = (_search.Text.ToString() ?? "").ToLower();
        _filtered = _contacts.Where(c =>
        {
            if (_onlyFav && !c.Favorito) return false;
            return c.Nombre.ToLower().Contains(q) ||
                   c.Telefonos.ToLower().Contains(q) ||
                   c.Email.ToLower().Contains(q);
        }).ToList();

        var items = _filtered.Select(c => c.Favorito ? $"★ {c.Nombre}" : c.Nombre).ToList();
        _list.SetSource(items);

        if (_filtered.Count > 0)
        {
            _list.SelectedItem = 0;
            _selected = _filtered[0];
            ShowDetails();
        }
    }

    private void ShowDetails()
    {
        if (_list.SelectedItem < 0 || _list.SelectedItem >= _filtered.Count)
        {
            _details.Text = "";
            return;
        }
        _selected = _filtered[_list.SelectedItem];
        var info = $"Name:   {_selected.Nombre}\n" +
                   $"Phones: {_selected.Telefonos}\n" +
                   $"Email:  {_selected.Email}\n" +
                   $"Fav:    {(_selected.Favorito ? "Yes" : "No")}\n" +
                   $"Notes:  {(_selected.Notas.Length > 30 ? _selected.Notas[..30] + "..." : _selected.Notas)}";
        _details.Text = info;
    }

    private void NewContact()
    {
        var dlg = new ContactDialog(new Contacto());
        if (Application.Run(dlg) == true)
        {
            var c = dlg.Get();
            var id = _store.Insert(c);
            c.Id = (int)id;
            _contacts.Add(c);
            RefreshList();
            Msg($"Added '{c.Nombre}'");
        }
    }

    private void EditContact()
    {
        if (_selected == null)
        {
            MessageBox.ErrorQuery("Oops", "Select a contact first");
            return;
        }
        var copy = _selected.Clone();
        var dlg = new ContactDialog(copy);
        if (Application.Run(dlg) == true)
        {
            var c = dlg.Get();
            _store.Update(c);
            var idx = _contacts.FindIndex(x => x.Id == c.Id);
            if (idx >= 0) _contacts[idx] = c;
            _selected = c;
            RefreshList();
            Msg($"Updated '{c.Nombre}'");
        }
    }

    private void DeleteContact()
    {
        if (_selected == null)
        {
            MessageBox.ErrorQuery("Oops", "Select a contact first");
            return;
        }
        if (MessageBox.Query("Confirm", $"Delete '{_selected.Nombre}'?", "Yes", "No") == 0)
        {
            _store.Delete(_selected.Id);
            _contacts.RemoveAll(x => x.Id == _selected.Id);
            _selected = null;
            RefreshList();
            Msg("Contact deleted");
        }
    }

    private void Import()
    {
        var dlg = new SaveDialog { Title = "Open JSON", AllowedFileTypes = new[] { ".json" } };
        if (Application.Run(dlg) != true) return;

        var path = dlg.FilePath.ToString();
        if (!File.Exists(path))
        {
            MessageBox.ErrorQuery("Error", $"File not found: {path}");
            return;
        }

        try
        {
            var imported = _jsonIo.Import(path);
            var cnt = imported.Count();
            if (MessageBox.Query("Import", $"Add {cnt} contact(s)?", "Yes", "No") == 0)
            {
                foreach (var c in imported)
                {
                    var id = _store.Insert(c);
                    c.Id = (int)id;
                    _contacts.Add(c);
                }
                RefreshList();
                Msg($"Imported {cnt} contacts");
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Import failed: {ex.Message}");
        }
    }

    private void Export()
    {
        var dlg = new SaveDialog { Title = "Save JSON", AllowedFileTypes = new[] { ".json" } };
        if (Application.Run(dlg) != true) return;

        var path = dlg.FilePath.ToString();
        try
        {
            _jsonIo.Export(_contacts, path);
            Msg($"Exported to {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Export failed: {ex.Message}");
        }
    }

    private void ToggleFav()
    {
        _onlyFav = !_onlyFav;
        RefreshList();
        Msg(_onlyFav ? "Favorites only" : "All contacts");
    }

    private void Msg(string text) => _status.Text = $" {text}";
}

// ============================================================================
// ContactDialog
// ============================================================================

public sealed class ContactDialog : Dialog
{
    private Contacto _c;
    private TextField _nombre = null!;
    private TextField[] _phones = null!;
    private TextField _email = null!;
    private TextView _notas = null!;
    private CheckBox _fav = null!;

    public ContactDialog(Contacto c) : base("Edit")
    {
        _c = c;
        Build();
    }

    private void Build()
    {
        Width = 60;
        Height = 20;

        Add(new Label("Name:") { X = 1, Y = 1 });
        _nombre = new TextField(_c.Nombre) { X = 15, Y = 1, Width = 40, CanFocus = true };

        Add(new Label("Phones:") { X = 1, Y = 3 });
        var phones = _c.Telefonos.Split(',', StringSplitOptions.TrimEntries);
        _phones = new TextField[5];
        for (int i = 0; i < 5; i++)
        {
            var y = 3 + (i / 2);
            var x = (i % 2) == 0 ? 15 : 32;
            _phones[i] = new TextField(i < phones.Length ? phones[i] : "")
            {
                X = x,
                Y = y,
                Width = 15,
                CanFocus = true
            };
            Add(_phones[i]);
        }

        Add(new Label("Email:") { X = 1, Y = 6 });
        _email = new TextField(_c.Email) { X = 15, Y = 6, Width = 40, CanFocus = true };

        Add(new Label("Notes:") { X = 1, Y = 8 });
        _notas = new TextView { X = 1, Y = 9, Width = 54, Height = 5, CanFocus = true, Text = _c.Notas };

        _fav = new CheckBox("Favorite") { X = 1, Y = 14, Checked = _c.Favorito, CanFocus = true };

        var btnOk = new Button("Save") { X = 15, Y = 14, IsDefault = true, CanFocus = true };
        btnOk.Clicked += Validate;

        var btnCancel = new Button("Cancel") { X = 27, Y = 14, CanFocus = true };
        btnCancel.Clicked += () => Close(false);

        Add(_nombre, _email, _notas, _fav, btnOk, btnCancel);
    }

    private void Validate()
    {
        var name = _nombre.Text.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.ErrorQuery("Error", "Name required");
            return;
        }

        var email = _email.Text.ToString() ?? "";
        if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
        {
            MessageBox.ErrorQuery("Error", "Invalid email");
            return;
        }

        _c.Nombre = name;
        _c.Email = email;
        _c.Favorito = _fav.Checked ?? false;
        _c.Notas = _notas.Text.ToString() ?? "";

        var phonesList = new List<string>();
        foreach (var p in _phones)
        {
            var phone = (p.Text.ToString() ?? "").Trim();
            if (!string.IsNullOrEmpty(phone))
                phonesList.Add(phone);
        }
        _c.Telefonos = string.Join(", ", phonesList);

        Close(true);
    }

    public Contacto Get() => _c;
}

// ============================================================================
// SqliteAgendaStore
// ============================================================================

public sealed class SqliteAgendaStore
{
    private string _conn;

    public SqliteAgendaStore(string dbPath)
    {
        _conn = $"Data Source={dbPath}";
        Init();
    }

    private void Init()
    {
        using (var c = new SqliteConnection(_conn))
        {
            c.Open();
            c.Execute(@"CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT DEFAULT '',
                Email TEXT DEFAULT '',
                Notas TEXT DEFAULT '',
                Favorito INTEGER DEFAULT 0)");
        }
    }

    public IEnumerable<Contacto> GetAll()
    {
        using (var c = new SqliteConnection(_conn))
        {
            c.Open();
            return c.GetAll<Contacto>().ToList();
        }
    }

    public long Insert(Contacto contacto)
    {
        using (var c = new SqliteConnection(_conn))
        {
            c.Open();
            return c.Insert(contacto);
        }
    }

    public void Update(Contacto contacto)
    {
        using (var c = new SqliteConnection(_conn))
        {
            c.Open();
            c.Update(contacto);
        }
    }

    public void Delete(int id)
    {
        using (var c = new SqliteConnection(_conn))
        {
            c.Open();
            c.Delete(new Contacto { Id = id });
        }
    }
}

// ============================================================================
// JsonAgendaIO
// ============================================================================

public sealed class JsonAgendaIO
{
    public IEnumerable<Contacto> Import(string path)
    {
        var json = File.ReadAllText(path);
        var list = JsonSerializer.Deserialize<List<Contacto>>(json,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ?? new List<Contacto>();

        foreach (var c in list)
            c.Id = 0; // Fuerza nuevos IDs
        return list;
    }

    public void Export(IEnumerable<Contacto> contactos, string path)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(contactos, opts);
        File.WriteAllText(path, json);
    }
}

// ============================================================================
// Contacto
// ============================================================================

[Table("Contactos")]
public sealed class Contacto
{
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => new()
    {
        Id = Id,
        Nombre = Nombre,
        Telefonos = Telefonos,
        Email = Email,
        Notas = Notas,
        Favorito = Favorito
    };
}