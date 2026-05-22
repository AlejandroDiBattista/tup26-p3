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
using System.Text.Encodings.Web;

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

var store = new SqliteAgendaStore(dbPath);
store.InitDb();

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));

[Table("Contactos")]
public class Contacto {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => (Contacto)MemberwiseClone();
    public override string ToString() => $"{(Favorito ? "★" : " ")} {Nombre}";
}

public class SqliteAgendaStore {
    private readonly string _connStr;

    public SqliteAgendaStore(string dbPath) {
        _connStr = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
    }

    public void InitDb() {
        using var conn = new SqliteConnection(_connStr);
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER
            )");
    }

    public IEnumerable<Contacto> GetAll() {
        using var conn = new SqliteConnection(_connStr);
        return conn.GetAll<Contacto>().OrderByDescending(c => c.Favorito).ThenBy(c => c.Nombre);
    }

    public void Insert(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        c.Id = (int)conn.Insert(c);
    }

    public void Update(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        conn.Update(c);
    }

    public void Delete(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        conn.Delete(c);
    }
}

public class JsonAgendaIO {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static void Export(string path, IEnumerable<Contacto> contactos) {
        var json = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, json);
    }

    public static IEnumerable<Contacto> Import(string path) {
        if (!File.Exists(path)) throw new FileNotFoundException("El archivo JSON indicado no existe.");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Contacto>>(json, Options) ?? [];
    }
}

public sealed class ContactDialog : Dialog {
    private const int MaxPhones = 2;

    private readonly TextField _tfName;
    private readonly TextField[] _tfPhones = new TextField[MaxPhones];
    private readonly TextField _tfEmail;
    private readonly TextView _tvNotes;

    private bool _isFav;
    private readonly Button _btnFav;

    public Contacto? ContactoResult { get; private set; }
    public bool IsCanceled { get; private set; } = true;

    public ContactDialog(Contacto? c = null) {
        Title = c == null ? "Nuevo Contacto" : "Editar Contacto";
        Width = 55;
        Height = 19;

        Add(new Label() { Text = "Nombre:", X = 1, Y = 1 });
        _tfName = new TextField() { Text = c?.Nombre ?? "", X = 12, Y = 1, Width = Dim.Fill(1) };
        Add(_tfName);

        string[] phones = (c?.Telefonos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < MaxPhones; i++) {
            Add(new Label() { Text = $"Teléfono {i + 1}:", X = 1, Y = 3 + i });
            _tfPhones[i] = new TextField() {
                Text = i < phones.Length ? phones[i].Trim() : "",
                X = 12,
                Y = 3 + i,
                Width = Dim.Fill(1)
            };
            Add(_tfPhones[i]);
        }

        Add(new Label() { Text = "Email:", X = 1, Y = 6 });
        _tfEmail = new TextField() { Text = c?.Email ?? "", X = 12, Y = 6, Width = Dim.Fill(1) };
        Add(_tfEmail);

        Add(new Label() { Text = "Notas:", X = 1, Y = 8 });
        _tvNotes = new TextView() { Text = c?.Notas ?? "", X = 12, Y = 8, Width = Dim.Fill(1), Height = 4 };
        Add(_tvNotes);

        _isFav = c?.Favorito ?? false;
        Add(new Label() { Text = "Favorito:", X = 1, Y = 13 });

        _btnFav = new Button() {
            Text = _isFav ? "[★] Sí" : "[ ] No",
            X = 12,
            Y = 13
        };

        _btnFav.Accepting += (_, e) => {
            _isFav = !_isFav;
            _btnFav.Text = _isFav ? "[★] Sí" : "[ ] No";
            e.Handled = true;
        };
        Add(_btnFav);

        Button btnOk = new() { Text = "Guardar", IsDefault = true };
        Button btnCancel = new() { Text = "Cancelar" };

        btnOk.Accepting += (_, e) => {
            if (string.IsNullOrWhiteSpace(_tfName.Text)) {
                MostrarErrorValidacion("El nombre no puede estar vacío.");
                return;
            }

            var email = _tfEmail.Text ?? "";
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@")) {
                MostrarErrorValidacion("El email debe contener un '@'.");
                return;
            }

            var phoneList = _tfPhones
                .Select(p => p.Text?.Trim())
                .Where(p => !string.IsNullOrEmpty(p));

            ContactoResult = new Contacto {
                Id = c?.Id ?? 0,
                Nombre = _tfName.Text.Trim(),
                Telefonos = string.Join(",", phoneList),
                Email = email.Trim(),
                Notas = _tvNotes.Text ?? "",
                Favorito = _isFav
            };

            IsCanceled = false;
            App!.RequestStop();
            e.Handled = true;
        };

        btnCancel.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        AddButton(btnOk);
        AddButton(btnCancel);
    }

    private void MostrarErrorValidacion(string mensaje) {
        Dialog d = new() { Title = "Validación", Width = 40, Height = 7 };
        d.Add(new Label() { Text = mensaje, X = Pos.Center(), Y = 1 });

        Button btn = new() { Text = "OK", IsDefault = true };
        btn.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        d.AddButton(btn);
        App!.Run(d);
    }
}

public sealed class AgendaWindow : Runnable {
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _contacts = [];
    private List<Contacto> _filteredContacts = [];

    private ListView _listView = null!;
    private TextField _searchField = null!;
    private TextView _detailView = null!;
    private Label _statusLabel = null!;
    private Button _favButton = null!;

    private bool _showOnlyFavs = false;

    public AgendaWindow(SqliteAgendaStore store) {
        _store = store;
        Title = "AgendaT - Trabajo Práctico 3";
        Width = Dim.Fill();
        Height = Dim.Fill();

        BuildLayout();
        LoadData();
    }

    private void BuildLayout() {
        FrameView actionPane = new() {
            Title = "Acciones",
            X = 0,
            Y = 0,
            Width = 21,
            Height = Dim.Fill(1)
        };

        Button btnNew = CreateActionButton("Nuevo (F2)", 1, NuevoContacto);
        Button btnEdit = CreateActionButton("Editar (F3)", 3, EditarContacto);
        Button btnDelete = CreateActionButton("Eliminar (Del)", 5, EliminarContacto);
        Button btnImport = CreateActionButton("Importar", 8, ImportarJson);
        Button btnExport = CreateActionButton("Exportar", 10, ExportarJson);

        _favButton = CreateActionButton("Solo favoritos", 13, ToggleFavorites);
        Button btnAbout = CreateActionButton("Acerca de", 16, MostrarAcercaDe);
        Button btnExit = CreateActionButton("Salir (Ctrl+Q)", 18, SolicitarSalir);

        actionPane.Add(btnNew, btnEdit, btnDelete, btnImport, btnExport, _favButton, btnAbout, btnExit);

        Label searchLabel = new() {
            Text = "Buscar [F4]:",
            X = Pos.Right(actionPane) + 1,
            Y = 0
        };

        _searchField = new TextField() {
            Text = "",
            X = Pos.Right(searchLabel) + 1,
            Y = 0,
            Width = Dim.Fill()
        };
        _searchField.TextChanged += (_, _) => ApplyFilter();

        FrameView leftPane = new() {
            Title = "Contactos",
            X = Pos.Right(actionPane) + 1,
            Y = 2,
            Width = Dim.Percent(43),
            Height = Dim.Fill(1)
        };

        _listView = new ListView() {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _listView.Accepting += (_, e) => {
            EditarContacto();
            e.Handled = true;
        };
        _listView.KeyDown += (_, _) => UpdateDetailView();
        _listView.KeyUp += (_, _) => UpdateDetailView();

        leftPane.Add(_listView);

        FrameView rightPane = new() {
            Title = "Detalle",
            X = Pos.Right(leftPane),
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        _detailView = new TextView() {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        rightPane.Add(_detailView);

        _statusLabel = new Label() {
            Text = "Listo",
            X = 0,
            Y = Pos.Bottom(actionPane),
            Width = Dim.Fill()
        };

        Add(actionPane, searchLabel, _searchField, leftPane, rightPane, _statusLabel);
    }

    private static Button CreateActionButton(string text, int y, Action action) {
        Button button = new() {
            Text = text,
            X = 1,
            Y = y,
            Width = Dim.Fill(1)
        };

        button.Accepting += (_, e) => {
            action();
            e.Handled = true;
        };

        return button;
    }

    private void UpdateStatus(string message) => _statusLabel.Text = message;

    private void LoadData() {
        try {
            _contacts = _store.GetAll().ToList();
            ApplyFilter();
            UpdateStatus($"Base cargada. {_contacts.Count} contactos.");
        } catch (Exception ex) {
            MostrarError("Error DB", ex.Message);
        }
    }

    private void ApplyFilter() {
        var term = _searchField.Text?.ToLower() ?? "";
        _filteredContacts = _contacts
            .Where(c => {
                if (_showOnlyFavs && !c.Favorito) return false;
                if (string.IsNullOrWhiteSpace(term)) return true;

                return (c.Nombre?.ToLower().Contains(term) == true) ||
                       (c.Telefonos?.ToLower().Contains(term) == true) ||
                       (c.Email?.ToLower().Contains(term) == true);
            })
            .OrderByDescending(c => c.Favorito)
            .ThenBy(c => c.Nombre)
            .ToList();

        _listView.SetSource(new ObservableCollection<Contacto>(_filteredContacts));
        UpdateDetailView();
    }

    private void UpdateDetailView() {
        int? idx = _listView.SelectedItem;
        if (idx.HasValue && idx.Value >= 0 && idx.Value < _filteredContacts.Count) {
            var c = _filteredContacts[idx.Value];
            _detailView.Text = $"Nombre: {c.Nombre}\n" +
                               $"Favorito: {(c.Favorito ? "Sí" : "No")}\n" +
                               $"Email: {c.Email}\n" +
                               $"Teléfonos: {c.Telefonos}\n\n" +
                               $"Notas:\n{c.Notas}";
        } else {
            _detailView.Text = "";
        }
    }

    private void NuevoContacto() {
        ContactDialog dialog = new();
        App!.Run(dialog);

        if (!dialog.IsCanceled && dialog.ContactoResult != null) {
            try {
                _store.Insert(dialog.ContactoResult);
                _contacts.Add(dialog.ContactoResult);
                ApplyFilter();
                UpdateStatus($"Contacto '{dialog.ContactoResult.Nombre}' creado.");
            } catch (Exception ex) {
                MostrarError("Error DB", ex.Message);
            }
        }
    }

    private void EditarContacto() {
        int? idx = _listView.SelectedItem;
        if (!idx.HasValue || idx.Value < 0 || idx.Value >= _filteredContacts.Count) return;

        var c = _filteredContacts[idx.Value];
        ContactDialog dialog = new(c);
        App!.Run(dialog);

        if (!dialog.IsCanceled && dialog.ContactoResult != null) {
            try {
                _store.Update(dialog.ContactoResult);
                var realIdx = _contacts.FindIndex(x => x.Id == dialog.ContactoResult.Id);
                if (realIdx >= 0) _contacts[realIdx] = dialog.ContactoResult;

                ApplyFilter();
                UpdateStatus($"Contacto '{dialog.ContactoResult.Nombre}' actualizado.");
            } catch (Exception ex) {
                MostrarError("Error DB", ex.Message);
            }
        }
    }

    private void EliminarContacto() {
        int? idx = _listView.SelectedItem;
        if (!idx.HasValue || idx.Value < 0 || idx.Value >= _filteredContacts.Count) return;

        var c = _filteredContacts[idx.Value];
        int res = MostrarConfirmacion("Eliminar", $"¿Seguro que desea eliminar a {c.Nombre}?");
        if (res == 1) {
            try {
                _store.Delete(c);
                _contacts.RemoveAll(x => x.Id == c.Id);
                ApplyFilter();
                UpdateStatus($"Contacto '{c.Nombre}' eliminado.");
            } catch (Exception ex) {
                MostrarError("Error DB", ex.Message);
            }
        }
    }

    private void ToggleFavorites() {
        _showOnlyFavs = !_showOnlyFavs;
        _favButton.Text = _showOnlyFavs ? "Ver todos" : "Solo favoritos";
        ApplyFilter();
        UpdateStatus(_showOnlyFavs ? "Mostrando solo favoritos." : "Mostrando todos.");
    }

    private void MostrarError(string titulo, string mensaje) {
        Dialog d = new() { Title = titulo, Width = 50, Height = 8 };
        d.Add(new Label() { Text = mensaje, X = Pos.Center(), Y = 1 });

        Button btn = new() { Text = "OK", IsDefault = true };
        btn.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        d.AddButton(btn);
        App!.Run(d);
    }

    private int MostrarConfirmacion(string titulo, string mensaje) {
        Dialog d = new() { Title = titulo, Width = 50, Height = 8 };
        d.Add(new Label() { Text = mensaje, X = Pos.Center(), Y = 1 });

        Button btnSi = new() { Text = "Sí", IsDefault = true };
        Button btnNo = new() { Text = "No" };
        int resultado = 0;

        btnSi.Accepting += (_, e) => {
            resultado = 1;
            App!.RequestStop();
            e.Handled = true;
        };
        btnNo.Accepting += (_, e) => {
            resultado = 0;
            App!.RequestStop();
            e.Handled = true;
        };

        d.AddButton(btnNo);
        d.AddButton(btnSi);
        App!.Run(d);

        return resultado;
    }

    private void MostrarAcercaDe() {
        Dialog d = new() { Title = "Acerca de", Width = 40, Height = 8 };
        d.Add(new Label() { Text = "AgendaT\nTrabajo Práctico 3\nTerminal.Gui v2", X = Pos.Center(), Y = 1 });

        Button btn = new() { Text = "OK", IsDefault = true };
        btn.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        d.AddButton(btn);
        App!.Run(d);
    }

    private string? PromptForPath(string title, string label, string defaultPath = "") {
        Dialog d = new() { Title = title, Width = 50, Height = 8 };
        Label lbl = new() { Text = label, X = 1, Y = 1 };
        TextField tf = new() { Text = defaultPath, X = 1, Y = 2, Width = Dim.Fill(1) };

        Button btnOk = new() { Text = "Aceptar", IsDefault = true };
        Button btnCancel = new() { Text = "Cancelar" };

        string? result = null;

        btnOk.Accepting += (_, e) => {
            result = tf.Text;
            App!.RequestStop();
            e.Handled = true;
        };
        btnCancel.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        d.Add(lbl, tf);
        d.AddButton(btnOk);
        d.AddButton(btnCancel);
        App!.Run(d);

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private void ImportarJson() {
        string? path = PromptForPath("Importar JSON", "Ruta del archivo JSON:");
        if (path == null) return;

        try {
            var imported = JsonAgendaIO.Import(path).ToList();
            int res = MostrarConfirmacion("Importación", $"Se encontraron {imported.Count} contactos.\n¿Desea agregarlos?");
            if (res == 1) {
                foreach (var c in imported) {
                    c.Id = 0;
                    _store.Insert(c);
                    _contacts.Add(c);
                }

                ApplyFilter();
                UpdateStatus($"Se importaron {imported.Count} contactos.");
            }
        } catch (Exception ex) {
            MostrarError("Error Importación", ex.Message);
        }
    }

    private void ExportarJson() {
        string? path = PromptForPath("Exportar JSON", "Ruta para guardar:", "agenda.json");
        if (path == null) return;

        try {
            JsonAgendaIO.Export(path, _contacts);
            UpdateStatus($"Exportación exitosa a {path}");
        } catch (Exception ex) {
            MostrarError("Error Exportación", ex.Message);
        }
    }

    private void SolicitarSalir() => App!.RequestStop();

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) { SolicitarSalir(); return true; }
        if (key == Key.I.WithCtrl) { ImportarJson(); return true; }
        if (key == Key.E.WithCtrl) { ExportarJson(); return true; }
        if (key == Key.N.WithCtrl || key == Key.F2) { NuevoContacto(); return true; }
        if (key == Key.F3) { EditarContacto(); return true; }
        if (key == Key.D.WithCtrl || key == Key.Delete) { EliminarContacto(); return true; }
        if (key == Key.F4) { _searchField.SetFocus(); return true; }

        return base.OnKeyDown(key);
    }
}
