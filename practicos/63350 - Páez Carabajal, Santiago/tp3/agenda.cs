#!/usr/bin/env -S dotnet run
#:package Dapper@2.1.79
#:package Dapper.Contrib@2.0.78
#:package Microsoft.Data.Sqlite@10.0.8
#:package Terminal.Gui@1.14.0
#:property PublishAot=false

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui;

// ==========================================
// 1. TOP-LEVEL CODE (Arranque de la App)
// ==========================================

string dbName = "agenda.db";
if (args.Length > 0) {
    if (args[0] == "--" && args.Length > 1)
        dbName = args[1];
    else if (args[0] != "--")
        dbName = args[0];
}

try {
    var store = new SqliteAgendaStore(dbName);
    Application.Init();

    var mainWindow = new AgendaWindow(store);
    Application.Run(mainWindow);
    Application.Shutdown();
} catch (Exception ex) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error fatal: {ex.Message}");
    Console.ResetColor();
}

// ==========================================
// 2. CLASE AgendaWindow (Interfaz Principal)
// ==========================================

public sealed class AgendaWindow : Window {
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _allContacts = new();
    private List<Contacto> _filteredContacts = new();

    private TextField _searchField;
    private ListView _listView;
    private View _detailFrame;
    private TextView _detailTextView;
    private bool _showOnlyFavorites = false;

    public AgendaWindow(SqliteAgendaStore store) {
        _store = store;
        Title = " AgendaT - Gestión de Contactos ";

        InitMenuBar();

        var searchLabel = new Label { Text = "Buscar (F4): ", X = 1, Y = 0 };
        _searchField = new TextField { X = Pos.Right(searchLabel) + 1, Y = 0, Width = Dim.Fill(2) };
        _searchField.TextChanged += (_) => ApplyFilter();
        Add(searchLabel, _searchField);

        var listFrame = new View { X = 1, Y = 2, Width = Dim.Percent(45), Height = Dim.Fill(1) };
        _listView = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        _listView.SelectedItemChanged += (_) => UpdateDetailPanel();
        _listView.OpenSelectedItem += (_) => OnEditarContacto();
        listFrame.Add(_listView);

        _detailFrame = new View { X = Pos.Right(listFrame) + 1, Y = 2, Width = Dim.Fill(1), Height = Dim.Fill(1) };
        _detailTextView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        _detailFrame.Add(_detailTextView);

        Add(listFrame, _detailFrame);

        KeyPress += (e) => {
            if (e.KeyEvent.Key == Key.F2)
                OnNuevoContacto();
            else if (e.KeyEvent.Key == Key.F3)
                OnEditarContacto();
            else if (e.KeyEvent.Key == Key.DeleteChar)
                OnEliminarContacto();
            else if (e.KeyEvent.Key == Key.F4)
                _searchField.SetFocus();
            else if (e.KeyEvent.Key == (Key.CtrlMask | Key.Q))
                Application.RequestStop();
            else if (e.KeyEvent.Key == (Key.CtrlMask | Key.N))
                OnNuevoContacto();
            else if (e.KeyEvent.Key == (Key.CtrlMask | Key.D))
                OnEliminarContacto();
            else if (e.KeyEvent.Key == (Key.CtrlMask | Key.I))
                OnImportarJson();
            else if (e.KeyEvent.Key == (Key.CtrlMask | Key.E))
                OnExportarJson();
        };

        RefreshData();
    }

    private void InitMenuBar() {
        var menuBar = new MenuBar {
            Menus = new MenuBarItem[] {
                new MenuBarItem ("_Archivo", new MenuItem [] {
                    new MenuItem ("_Importar JSON", "Ctrl+I", OnImportarJson),
                    new MenuItem ("_Exportar JSON", "Ctrl+E", OnExportarJson),
                    new MenuItem ("_Salir", "Ctrl+Q", () => Application.RequestStop())
                }),
                new MenuBarItem ("_Contactos", new MenuItem [] {
                    new MenuItem ("_Nuevo", "F2", OnNuevoContacto),
                    new MenuItem ("_Editar", "F3", OnEditarContacto),
                    new MenuItem ("_Eliminar", "Del", OnEliminarContacto)
                }),
                new MenuBarItem ("_Ver", new MenuItem [] {
                    new MenuItem ("_Solo Favoritos", "Toggle", OnToggleFavoritos) { Checked = _showOnlyFavorites, CheckType = MenuItemCheckStyle.Checked }
                }),
                new MenuBarItem ("_Ayuda", new MenuItem [] {
                    new MenuItem ("_Acerca de", "", () => MessageBox.Query("Acerca de", "AgendaT v1.0\n.NET 10.", "OK"))
                })
            }
        };
        Add(menuBar);
    }

    private void RefreshData() {
        try {
            _allContacts = _store.GetAll().OrderBy(c => c.Nombre).ToList();
            ApplyFilter();
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void ApplyFilter() {
        string text = _searchField.Text?.ToString() ?? "";
        var query = _allContacts.AsEnumerable();

        if (_showOnlyFavorites) query = query.Where(c => c.Favorito);

        if (!string.IsNullOrWhiteSpace(text)) {
            query = query.Where(c => c.Nombre.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                                     c.Telefonos.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                                     c.Email.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        _filteredContacts = query.ToList();
        var displayList = _filteredContacts.Select(c => $"{(c.Favorito ? "★" : " ")} {c.Nombre}").ToList();
        _listView.SetSource(displayList);
        UpdateDetailPanel();
    }

    private void UpdateDetailPanel() {
        if (_listView.SelectedItem >= 0 && _listView.SelectedItem < _filteredContacts.Count) {
            var c = _filteredContacts[_listView.SelectedItem];
            var multiLinePhones = string.Join("\n", c.Telefonos.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => $"  - {p.Trim()}"));

            _detailTextView.Text = $"Nombre: {c.Nombre}\n" +
                                   $"Favorito: {(c.Favorito ? "Sí" : "No")}\n" +
                                   $"Email: {c.Email}\n" +
                                   $"Teléfonos:\n{multiLinePhones}\n\n" +
                                   $"Notas:\n{c.Notas}";
        } else {
            _detailTextView.Text = "Ningún contacto seleccionado.";
        }
    }

    private void OnToggleFavoritos() {
        _showOnlyFavorites = !_showOnlyFavorites;
        ApplyFilter();
    }

    private void OnNuevoContacto() {
        var dialog = new ContactDialog(new Contacto());
        Application.Run(dialog);
        if (dialog.SaveConfirmed) {
            _store.Insert(dialog.ContactResult);
            RefreshData();
        }
    }

    private void OnEditarContacto() {
        if (_listView.SelectedItem < 0 || _listView.SelectedItem >= _filteredContacts.Count) return;
        var original = _filteredContacts[_listView.SelectedItem];
        var dialog = new ContactDialog(original.Clone());
        Application.Run(dialog);
        if (dialog.SaveConfirmed) {
            _store.Update(dialog.ContactResult);
            RefreshData();
        }
    }

    private void OnEliminarContacto() {
        if (_listView.SelectedItem < 0 || _listView.SelectedItem >= _filteredContacts.Count) return;
        var c = _filteredContacts[_listView.SelectedItem];

        int result = MessageBox.Query("Eliminar", $"¿Borrar a {c.Nombre}?", "Sí", "No");
        if (result == 0) {
            _store.Delete(c.Id);
            RefreshData();
        }
    }

    private void OnImportarJson() {
        var d = new Dialog { Title = "Importar JSON", Width = 50, Height = 10 };
        var lbl = new Label { Text = "Ruta del archivo:", X = 2, Y = 1 };
        var txt = new TextField { Text = "contactos.json", X = 2, Y = 2, Width = Dim.Fill(2) };
        var btnOk = new Button { Text = "Ok", X = 15, Y = 5 };

        btnOk.Clicked += () => { Application.RequestStop(); };
        d.Add(lbl, txt, btnOk);
        Application.Run(d);

        string path = txt.Text?.ToString() ?? "";
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            try {
                var imported = JsonAgendaIO.Import(path);
                foreach (var c in imported) _store.Insert(c);
                RefreshData();
            } catch (Exception ex) {
                MessageBox.ErrorQuery("Error", ex.Message, "OK");
            }
        }
    }

    private void OnExportarJson() {
        var d = new Dialog { Title = "Exportar JSON", Width = 50, Height = 10 };
        var lbl = new Label { Text = "Ruta de salida:", X = 2, Y = 1 };
        var txt = new TextField { Text = "salida.json", X = 2, Y = 2, Width = Dim.Fill(2) };
        var btnOk = new Button { Text = "Ok", X = 15, Y = 5 };

        btnOk.Clicked += () => { Application.RequestStop(); };
        d.Add(lbl, txt, btnOk);
        Application.Run(d);

        string path = txt.Text?.ToString() ?? "";
        if (!string.IsNullOrEmpty(path)) {
            try {
                JsonAgendaIO.Export(path, _allContacts);
            } catch (Exception ex) {
                MessageBox.ErrorQuery("Error", ex.Message, "OK");
            }
        }
    }
}

// ==========================================
// 3. CLASE ContactDialog (Formulario)
// ==========================================

public sealed class ContactDialog : Dialog {
    public Contacto ContactResult { get; private set; }
    public bool SaveConfirmed { get; private set; } = false;

    private TextField _txtNombre;
    private CheckBox _chkFavorito;
    private TextField _txtEmail;
    private TextView _txtNotas;
    private TextField[] _txtTelefonos = new TextField[5];

    public ContactDialog(Contacto contacto) {
        ContactResult = contacto;
        Title = contacto.Id == 0 ? " Nuevo Contacto " : " Editar Contacto ";
        Width = 65;
        Height = 16;

        var lblNombre = new Label { Text = "Nombre (*):", X = 2, Y = 1 };
        _txtNombre = new TextField { Text = contacto.Nombre, X = 15, Y = 1, Width = 25 };
        _chkFavorito = new CheckBox { Text = "Favorito", X = 45, Y = 1, Checked = contacto.Favorito };

        var lblEmail = new Label { Text = "Email:", X = 2, Y = 3 };
        _txtEmail = new TextField { Text = contacto.Email, X = 15, Y = 3, Width = 40 };

        var lblTels = new Label { Text = "Teléfonos:", X = 2, Y = 5 };
        string[] currentPhones = contacto.Telefonos.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < 5; i++) {
            string val = i < currentPhones.Length ? currentPhones[i].Trim() : "";
            _txtTelefonos[i] = new TextField { Text = val, X = 15 + (i * 9), Y = 5, Width = 8 };
        }

        var lblNotas = new Label { Text = "Notas:", X = 2, Y = 7 };
        _txtNotas = new TextView { Text = contacto.Notas, X = 15, Y = 7, Width = 40, Height = 4 };

        var btnGuardar = new Button { Text = "Guardar", X = 15, Y = 12 };
        var btnCancelar = new Button { Text = "Cancelar", X = 35, Y = 12 };

        btnGuardar.Clicked += () => OnGuardar();
        btnCancelar.Clicked += () => { Application.RequestStop(); };

        Add(lblNombre, _txtNombre, _chkFavorito, lblEmail, _txtEmail, lblTels);
        for (int i = 0; i < 5; i++) Add(_txtTelefonos[i]);
        Add(lblNotas, _txtNotas, btnGuardar, btnCancelar);
    }

    private void OnGuardar() {
        string nombre = _txtNombre.Text?.ToString()?.Trim() ?? "";
        string email = _txtEmail.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrEmpty(nombre)) {
            MessageBox.ErrorQuery("Validación", "El Nombre no puede estar vacío.", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(email) && !email.Contains("@")) {
            MessageBox.ErrorQuery("Validación", "El Email debe contener @.", "OK");
            return;
        }

        var phoneList = new List<string>();
        for (int i = 0; i < 5; i++) {
            string p = _txtTelefonos[i].Text?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(p)) phoneList.Add(p);
        }

        ContactResult.Nombre = nombre;
        ContactResult.Favorito = _chkFavorito.Checked;
        ContactResult.Email = email;
        ContactResult.Telefonos = string.Join(",", phoneList);
        ContactResult.Notas = _txtNotas.Text?.ToString()?.Trim() ?? "";

        SaveConfirmed = true;
        Application.RequestStop();
    }
}

// ==========================================
// 4. CLASE SqliteAgendaStore (Persistencia)
// ==========================================

public sealed class SqliteAgendaStore {
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbName) {
        _connectionString = $"Data Source={dbName}";
        InitDatabase();
    }

    private void InitDatabase() {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        string sql = @"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            );";
        connection.Execute(sql);
    }

    public IEnumerable<Contacto> GetAll() {
        using var connection = new SqliteConnection(_connectionString);
        return connection.GetAll<Contacto>();
    }

    public long Insert(Contacto contacto) {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Insert(contacto);
    }

    public bool Update(Contacto contacto) {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Update(contacto);
    }

    public bool Delete(int id) {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Delete(new Contacto { Id = id });
    }
}

// ==========================================
// 5. CLASE JsonAgendaIO (Import / Export)
// ==========================================

public static class JsonAgendaIO {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<Contacto> Import(string path) {
        string json = File.ReadAllText(path);
        var lista = JsonSerializer.Deserialize<List<Contacto>>(json, Options) ?? new List<Contacto>();
        foreach (var c in lista) c.Id = 0;
        return lista;
    }

    public static void Export(string path, List<Contacto> contactos) {
        string json = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, json);
    }
}

// ==========================================
// 6. MODELO DE DATOS (Contacto)
// ==========================================

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
            Id = this.Id,
            Nombre = this.Nombre,
            Telefonos = this.Telefonos,
            Email = this.Email,
            Notas = this.Notas,
            Favorito = this.Favorito
        };
    }
}
