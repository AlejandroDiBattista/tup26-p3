#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

#pragma warning disable CS0618
#pragma warning disable CS8618

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data.Common;
using Dapper.Contrib.Extensions;

var dbFile = args.Length > 0 ? args[0] : "agenda.db";
var store = new SqliteAgendaStore(dbFile);
store.Initialize();

Application.Init();
var mainWindow = new AgendaWindow(store);
Application.Run(mainWindow);
Application.Shutdown();
return;

public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _allContacts = new();
    private List<Contacto> _filteredContacts = new();

    private TextField _searchField;
    private ListView _listView;
    private TextView _detailsView;
    private bool _filterFavoritesOnly = false;

    public AgendaWindow(SqliteAgendaStore store)
    {
        _store = store;
        Title = " AgendaT - Centro de Mando ";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        InitMenus();
        InitControls();
        LoadData();
    }

    private void InitMenus()
    {
        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem("_Archivo", new MenuItem[] {
                new MenuItem("_Importar JSON", "", OnImportJson),
                new MenuItem("_Exportar JSON", "", OnExportJson),
                new MenuItem("_Salir", "", () => Application.RequestStop())
            }),
            new MenuBarItem("_Contactos", new MenuItem[] {
                new MenuItem("_Nuevo", "", OnNewContact),
                new MenuItem("_Editar", "", OnEditContact),
                new MenuItem("_Eliminar", "", OnDeleteContact)
            }),
            new MenuBarItem("_Ver", new MenuItem[] {
                new MenuItem("_Alternar Favoritos", "", OnToggleFavorites)
            }),
            new MenuBarItem("A_yuda", new MenuItem[] {
                new MenuItem("_Acerca de", "", () => MessageBox.Query((IApplication)null!, "Acerca de", "AgendaT V1.0\nDesarrollado por Jeremías Sosa Paz", "OK"))
            })
        });
        Add(menu);
    }

    private void InitControls()
    {
        Add(new Label { Text = "Buscar:", X = 1, Y = 2 });
        _searchField = new TextField { Text = "", X = 9, Y = 2, Width = Dim.Fill(2) };
        _searchField.TextChanged += (s, e) => ApplyFilters();
        Add(_searchField);

        var listFrame = new FrameView { Title = " Contactos ", X = 1, Y = 4, Width = Dim.Percent(40), Height = Dim.Fill(1) };
        _listView = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        _listView.Accepting += (s, e) => { OnEditContact(); e.Handled = true; };
        listFrame.Add(_listView);

        var detailsFrame = new FrameView { Title = " Detalle de Contacto ", X = Pos.Right(listFrame) + 1, Y = 4, Width = Dim.Fill(1), Height = Dim.Fill(1) };
        _detailsView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        detailsFrame.Add(_detailsView);

        Add(listFrame, detailsFrame);
    }

    private void LoadData()
    {
        try
        {
            _allContacts = _store.GetAll().ToList();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery((IApplication)null!, "Error", $"Base de datos: {ex.Message}", "OK");
        }
    }

    private void ApplyFilters()
    {
        var query = _searchField.Text?.ToString()?.Trim().ToLower() ?? "";
        var records = _allContacts.AsEnumerable();

        if (_filterFavoritesOnly) records = records.Where(c => c.Favorito);

        if (!string.IsNullOrEmpty(query))
        {
            records = records.Where(c => 
                (c.Nombre != null && c.Nombre.ToLower().Contains(query)) || 
                (c.Telefonos != null && c.Telefonos.ToLower().Contains(query)) || 
                (c.Email != null && c.Email.ToLower().Contains(query)));
        }

        _filteredContacts = records.ToList();
        var listItems = _filteredContacts.Select(c => $"{(c.Favorito ? "[X]" : "[ ]")} {c.Nombre}").ToList();
        _listView.SetSource(new ObservableCollection<string>(listItems));
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        int selectedIndex = _listView.SelectedItem ?? -1;
        
        if (selectedIndex >= 0 && selectedIndex < _filteredContacts.Count)
        {
            var c = _filteredContacts[selectedIndex];
            var sb = new StringBuilder();
            sb.AppendLine($"Nombre: {c.Nombre}");
            sb.AppendLine($"Email: {c.Email}");
            sb.AppendLine($"Favorito: {(c.Favorito ? "Sí" : "No")}");
            sb.AppendLine();
            sb.AppendLine("Teléfonos:");
            if (c.Telefonos != null)
            {
                foreach (var t in c.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    sb.AppendLine($"  - {t.Trim()}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("Notas:");
            sb.AppendLine(c.Notas);
            _detailsView.Text = sb.ToString();
        }
        else
        {
            _detailsView.Text = "";
        }
    }

    private void OnToggleFavorites()
    {
        _filterFavoritesOnly = !_filterFavoritesOnly;
        ApplyFilters();
    }

    private void OnNewContact()
    {
        var dialog = new ContactDialog(new Contacto());
        Application.Run(dialog);
        if (dialog.SaveConfirmed)
        {
            _store.Insert(dialog.ContactResult);
            LoadData();
        }
    }

    private void OnEditContact()
    {
        int selectedIndex = _listView.SelectedItem ?? -1;
        if (selectedIndex < 0 || selectedIndex >= _filteredContacts.Count) return;
        
        var selected = _filteredContacts[selectedIndex];
        var dialog = new ContactDialog(selected.Clone());
        Application.Run(dialog);
        if (dialog.SaveConfirmed)
        {
            _store.Update(dialog.ContactResult);
            LoadData();
        }
    }

    private void OnDeleteContact()
    {
        int selectedIndex = _listView.SelectedItem ?? -1;
        if (selectedIndex < 0 || selectedIndex >= _filteredContacts.Count) return;
        
        var selected = _filteredContacts[selectedIndex];
        var n = MessageBox.Query((IApplication)null!, "Eliminar", $"¿Eliminar a {selected.Nombre}?", "Sí", "No");
        if (n == 0)
        {
            _store.Delete(selected.Id);
            LoadData();
        }
    }

    private void OnImportJson()
    {
        var d = new Dialog { Title = "Importar JSON", Width = 50, Height = 10 };
        var lbl = new Label { Text = "Ruta del archivo:", X = 1, Y = 1 };
        var txt = new TextField { Text = "agenda.json", X = 1, Y = 2, Width = Dim.Fill(1) };
        var btnOk = new Button { Text = "Importar", X = 10, Y = 4 };
        var btnCancel = new Button { Text = "Cancelar", X = 25, Y = 4 };

        btnCancel.Accepting += (s, e) => Application.RequestStop();
        btnOk.Accepting += (s, e) => {
            var path = txt.Text?.ToString() ?? "agenda.json";
            if (File.Exists(path))
            {
                var imported = JsonAgendaIO.Import(path);
                foreach (var c in imported) _store.Insert(c);
                LoadData();
                MessageBox.Query((IApplication)null!, "Éxito", $"Se importaron {imported.Count} contactos.", "OK");
            }
            else
            {
                MessageBox.ErrorQuery((IApplication)null!, "Error", "Archivo no encontrado.", "OK");
            }
            e.Handled = true;
            Application.RequestStop();
        };

        d.Add(lbl, txt, btnOk, btnCancel);
        Application.Run(d);
    }

    private void OnExportJson()
    {
        var d = new Dialog { Title = "Exportar JSON", Width = 50, Height = 10 };
        var lbl = new Label { Text = "Ruta de destino:", X = 1, Y = 1 };
        var txt = new TextField { Text = "exportacion.json", X = 1, Y = 2, Width = Dim.Fill(1) };
        var btnOk = new Button { Text = "Exportar", X = 10, Y = 4 };
        var btnCancel = new Button { Text = "Cancelar", X = 25, Y = 4 };

        btnCancel.Accepting += (s, e) => Application.RequestStop();
        btnOk.Accepting += (s, e) => {
            var path = txt.Text?.ToString() ?? "exportacion.json";
            JsonAgendaIO.Export(path, _allContacts);
            e.Handled = true;
            Application.RequestStop();
            MessageBox.Query((IApplication)null!, "Éxito", $"Datos exportados a {path}.", "OK");
        };

        d.Add(lbl, txt, btnOk, btnCancel);
        Application.Run(d);
    }
}

public sealed class ContactDialog : Dialog
{
    public Contacto ContactResult { get; private set; }
    public bool SaveConfirmed { get; private set; } = false;

    private TextField _txtNombre;
    private TextField _txtEmail;
    private Button _btnFavorito;
    private bool _isFavorito = false;
    private TextView _txtNotas;
    private readonly TextField[] _txtTelefonos = new TextField[5];

    public ContactDialog(Contacto contacto)
    {
        ContactResult = contacto;
        Title = contacto.Id == 0 ? "Nuevo Contacto" : "Editar Contacto";
        Width = 65;
        Height = 20;

        InitControls();
        LoadContact();
    }

    private void InitControls()
    {
        Add(new Label { Text = "Nombre:", X = 2, Y = 1 });
        _txtNombre = new TextField { Text = "", X = 12, Y = 1, Width = 48 };
        Add(_txtNombre);

        Add(new Label { Text = "Email:", X = 2, Y = 3 });
        _txtEmail = new TextField { Text = "", X = 12, Y = 3, Width = 48 };
        Add(_txtEmail);

        _btnFavorito = new Button { Text = "[ ] Marcar como Favorito", X = 12, Y = 5 };
        _btnFavorito.Accepting += (s, e) => {
            _isFavorito = !_isFavorito;
            _btnFavorito.Text = _isFavorito ? "[X] Marcar como Favorito" : "[ ] Marcar como Favorito";
            e.Handled = true; 
        };
        Add(_btnFavorito);

        Add(new Label { Text = "Teléfonos:", X = 2, Y = 7 });
        for (int i = 0; i < 5; i++)
        {
            _txtTelefonos[i] = new TextField { Text = "", X = 12 + (i * 9), Y = 7, Width = 8 };
            Add(_txtTelefonos[i]);
        }

        Add(new Label { Text = "Notas:", X = 2, Y = 9 });
        _txtNotas = new TextView { X = 12, Y = 9, Width = 48, Height = 4 };
        Add(_txtNotas);

        var btnSave = new Button { Text = "Guardar", X = 18, Y = 15 };
        var btnCancel = new Button { Text = "Cancelar", X = 35, Y = 15 };

        btnCancel.Accepting += (s, e) => Application.RequestStop();
        btnSave.Accepting += (s, e) => OnValidateAndSave();

        Add(btnSave, btnCancel);
    }

    private void LoadContact()
    {
        _txtNombre.Text = ContactResult.Nombre ?? "";
        _txtEmail.Text = ContactResult.Email ?? "";
        
        _isFavorito = ContactResult.Favorito;
        _btnFavorito.Text = _isFavorito ? "[X] Marcar como Favorito" : "[ ] Marcar como Favorito";
        
        _txtNotas.Text = ContactResult.Notas ?? "";

        var parts = ContactResult.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++)
        {
            _txtTelefonos[i].Text = i < parts.Length ? parts[i].Trim() : "";
        }
    }

    private void OnValidateAndSave()
    {
        var nombre = _txtNombre.Text?.ToString()?.Trim() ?? "";
        var email = _txtEmail.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrEmpty(nombre))
        {
            MessageBox.ErrorQuery((IApplication)null!, "Error", "El Nombre no puede estar vacío.", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
        {
            MessageBox.ErrorQuery((IApplication)null!, "Error", "El Email debe contener un carácter '@'.", "OK");
            return;
        }

        ContactResult.Nombre = nombre;
        ContactResult.Email = email;
        ContactResult.Favorito = _isFavorito;
        ContactResult.Notas = _txtNotas.Text?.ToString()?.Trim() ?? "";

        var phoneList = _txtTelefonos
            .Select(t => t.Text?.ToString()?.Trim() ?? "")
            .Where(s => !string.IsNullOrEmpty(s));
        
        ContactResult.Telefonos = string.Join(",", phoneList);

        SaveConfirmed = true;
        Application.RequestStop();
    }
}

public sealed class SqliteAgendaStore
{
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbFile)
    {
        _connectionString = $"Data Source={dbFile}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            );");
    }

    public IEnumerable<Contacto> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.GetAll<Contacto>();
    }

    public long Insert(Contacto contacto)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Insert(contacto);
    }

    public bool Update(Contacto contacto)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Update(contacto);
    }

    public bool Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Delete(new Contacto { Id = id });
    }
}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<Contacto> Import(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var records = JsonSerializer.Deserialize<List<Contacto>>(json, Options);
        if (records == null) return new List<Contacto>();
        
        foreach (var r in records)
        {
            r.Id = 0;
        }
        return records;
    }

    public static void Export(string path, List<Contacto> contactos)
    {
        var json = JsonSerializer.Serialize(contactos, Options);
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
            Id = this.Id,
            Nombre = this.Nombre,
            Telefonos = this.Telefonos,
            Email = this.Email,
            Notas = this.Notas,
            Favorito = this.Favorito
        };
    }
}