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
using Dapper.Contrib.Extensions;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Collections.ObjectModel;


string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store;
try {
    store = new SqliteAgendaStore(dbPath);
}
catch (Exception ex) {
    Console.Error.WriteLine($"Error al abrir la base de datos '{dbPath}': {ex.Message}");
    return 1;
}

using (store) {
    using IApplication app = Application.Create().Init();
    app.Run(new AgendaWindow(store));
}
return 0;



public sealed class AgendaWindow : Runnable {
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _contacts = new();
    private List<Contacto> _filteredContacts = new();
    private bool _soloFavoritos = false;

    private TextField _searchField = null!;
    private ListView _listView = null!;
    private TextView _detailView = null!;
    private Label _statusLabel = null!;
    private MenuItem _soloFavoritosMenuItem = null!;

    public AgendaWindow(SqliteAgendaStore store) {
        _store = store;
        Title = "AgendaT";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        LoadContacts();
    }

    private void BuildLayout() {
        _soloFavoritosMenuItem = new MenuItem("_Solo favoritos", "", MenuToggleFavoritos);

        MenuBar menu = new() {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", MenuImportar),
                    new MenuItem("_Exportar JSON", "Ctrl+E", MenuExportar),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", MenuSalir),
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo",    "F2/Ctrl+N",  MenuNuevo),
                    new MenuItem("_Editar",   "F3/Enter",   MenuEditar),
                    new MenuItem("_Eliminar", "Del/Ctrl+D", MenuEliminar),
                ]),
                new MenuBarItem("_Ver",
                [
                    _soloFavoritosMenuItem,
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", "", MenuAcercaDe),
                ]),
            ]
        };

        Label searchLabel = new() {
            Text = "Buscar: ",
            X = 0,
            Y = 1,
        };

        _searchField = new TextField() {
            X = Pos.Right(searchLabel),
            Y = 1,
            Width = Dim.Fill(),
        };
        _searchField.TextChanged += (_, _) => ApplyFilter();

        FrameView listFrame = new() {
            Title = "Contactos",
            X = 0,
            Y = 2,
            Width = Dim.Percent(40),
            Height = Dim.Fill(2),
        };

        _listView = new ListView() {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _listView.ValueChanged += OnSelectedItemChanged;
        _listView.Accepting += (_, e) => {
            MenuEditar();
            e.Handled = true;
        };
        listFrame.Add(_listView);

        FrameView detailFrame = new() {
            Title = "Detalle",
            X = Pos.Right(listFrame),
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };

        _detailView = new TextView() {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
        };
        detailFrame.Add(_detailView);

        _statusLabel = new Label() {
            Text = "F2/Ctrl+N:Nuevo  F3/Enter:Editar  Del/Ctrl+D:Eliminar  Ctrl+I/E:JSON  F4:Buscar  Ctrl+Q:Salir",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };

        Add(menu, searchLabel, _searchField, listFrame, detailFrame, _statusLabel);
    }

    private void LoadContacts() {
        try {
            _contacts = _store.GetAll();
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudieron cargar los contactos:\n{ex.Message}", "Aceptar");
            _contacts = new List<Contacto>();
        }
        ApplyFilter();
    }

    private void ApplyFilter() {
        string search = _searchField?.Text?.Trim() ?? "";
        var query = _contacts.AsEnumerable();

        if (_soloFavoritos)
            query = query.Where(c => c.Favorito);

        if (!string.IsNullOrEmpty(search)) {
            string lower = search.ToLowerInvariant();
            query = query.Where(c =>
                c.Nombre.ToLowerInvariant().Contains(lower) ||
                c.Telefonos.ToLowerInvariant().Contains(lower) ||
                c.Email.ToLowerInvariant().Contains(lower));
        }

        _filteredContacts = query.ToList();

        var items = new ObservableCollection<string>(
            _filteredContacts.Select(c => (c.Favorito ? "★ " : "  ") + c.Nombre));

        _listView.SetSource(items);

        int? sel = _listView.SelectedItem;
        if (_filteredContacts.Count > 0 && (sel == null || sel.Value >= _filteredContacts.Count))
            _listView.SelectedItem = 0;
        else if (_filteredContacts.Count == 0)
            _listView.SelectedItem = null;

        UpdateDetail();
    }

    private void OnSelectedItemChanged(object? sender, ValueChangedEventArgs<int?> e)
        => UpdateDetail();

    private void UpdateDetail() {
        int? selNullable = _listView.SelectedItem;
        if (selNullable == null) {
            _detailView.Text = "";
            return;
        }

        int idx = selNullable.Value;
        if (idx < 0 || idx >= _filteredContacts.Count) {
            _detailView.Text = "";
            return;
        }

        var c = _filteredContacts[idx];
        _detailView.Text =
            $"Nombre:    {c.Nombre}\n" +
            $"Teléfonos: {c.Telefonos}\n" +
            $"Email:     {c.Email}\n" +
            $"Favorito:  {(c.Favorito ? "Sí" : "No")}\n\n" +
            $"Notas:\n{c.Notas}";
    }

    private Contacto? GetSelected() {
        int? selNullable = _listView.SelectedItem;
        if (selNullable == null) return null;
        int idx = selNullable.Value;
        return (idx >= 0 && idx < _filteredContacts.Count) ? _filteredContacts[idx] : null;
    }


    private void SetStatus(string msg) => _statusLabel.Text = msg;
    protected override bool OnKeyDown(Key key) {
        if (key == Key.F2) { MenuNuevo(); return true; }
        if (key == Key.F3) { MenuEditar(); return true; }
        if (key == Key.F4) { FocusSearch(); return true; }
        if (key == Key.Delete) { MenuEliminar(); return true; }
        if (key == Key.N.WithCtrl) { MenuNuevo(); return true; }
        if (key == Key.D.WithCtrl) { MenuEliminar(); return true; }
        if (key == Key.I.WithCtrl) { MenuImportar(); return true; }
        if (key == Key.E.WithCtrl) { MenuExportar(); return true; }
        if (key == Key.Q.WithCtrl) { MenuSalir(); return true; }
        return base.OnKeyDown(key);
    }

    private void MenuSalir() => App!.RequestStop();

    private void MenuAcercaDe()
        => MessageBox.Query(App!, "Acerca de AgendaT", "AgendaT — Aplicación de agenda en terminal\n" + "TP3 — .NET 10 + Terminal.Gui v2 + SQLite\n", "Cerrar");

    private void FocusSearch() => _searchField.SetFocus();
    private void MenuToggleFavoritos() {
        _soloFavoritos = !_soloFavoritos;
        _soloFavoritosMenuItem.Title = _soloFavoritos ? "_Solo favoritos ✓" : "_Solo favoritos";
        SetStatus(_soloFavoritos ? "Mostrando solo favoritos." : "Mostrando todos los contactos.");
        ApplyFilter();
    }


    private void MenuNuevo() {
        var dlg = new ContactDialog("Nuevo contacto", new Contacto());
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;

        var nuevo = dlg.ContactResult!;
        try {
            _store.Insert(nuevo);
            _contacts.Add(nuevo);
            ApplyFilter();
            int idx = _filteredContacts.FindIndex(c => c.Id == nuevo.Id);
            if (idx >= 0) _listView.SelectedItem = idx;
            SetStatus($"Contacto '{nuevo.Nombre}' creado.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo guardar el contacto:\n{ex.Message}", "Aceptar");
        }
    }


    private void MenuEditar() {
        var sel = GetSelected();
        if (sel == null) {
            MessageBox.ErrorQuery(App!, "Aviso", "No hay contacto seleccionado.", "Aceptar");
            return;
        }

        var dlg = new ContactDialog("Editar contacto", sel.Clone());
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;

        var editado = dlg.ContactResult!;
        editado.Id = sel.Id;
        try {
            _store.Update(editado);
            int memIdx = _contacts.FindIndex(c => c.Id == sel.Id);
            if (memIdx >= 0) _contacts[memIdx] = editado;
            ApplyFilter();
            int idx = _filteredContacts.FindIndex(c => c.Id == editado.Id);
            if (idx >= 0) _listView.SelectedItem = idx;
            SetStatus($"Contacto '{editado.Nombre}' actualizado.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo actualizar el contacto:\n{ex.Message}", "Aceptar");
        }
    }

    private void MenuEliminar() {
        var sel = GetSelected();
        if (sel == null) {
            MessageBox.ErrorQuery(App!, "Aviso", "No hay contacto seleccionado.", "Aceptar");
            return;
        }

        int? resp = MessageBox.Query(App!, "Confirmar", $"¿Eliminar el contacto '{sel.Nombre}'?", "Sí", "No");
        if (resp != 0) return;

        try {
            _store.Delete(sel);
            _contacts.RemoveAll(c => c.Id == sel.Id);
            ApplyFilter();
            SetStatus($"Contacto '{sel.Nombre}' eliminado.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo eliminar el contacto:\n{ex.Message}", "Aceptar");
        }
    }


    private void MenuImportar() {
        var dlgPath = new InputDialog("Importar JSON", "Ruta del archivo JSON a importar:", "contactos.json");
        App!.Run(dlgPath);
        if (!dlgPath.WasAccepted || string.IsNullOrWhiteSpace(dlgPath.InputValue)) return;

        string path = dlgPath.InputValue.Trim();
        List<Contacto> importados;
        try {
            importados = JsonAgendaIO.Read(path);
            ValidateImportedContacts(importados);
        }
        catch (FileNotFoundException) {
            MessageBox.ErrorQuery(App!, "Error", $"Archivo no encontrado:\n{path}", "Aceptar");
            return;
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo leer el archivo JSON:\n{ex.Message}", "Aceptar");
            return;
        }

        int? resp = MessageBox.Query(App!, "Confirmar importación", $"Se agregarán {importados.Count} contacto(s).\n¿Continuar?", "Sí", "No");
        if (resp != 0) return;

        try {
            int agregados = 0;
            foreach (var c in importados) {
                c.Id = 0;
                NormalizeContact(c);
                _store.Insert(c);
                _contacts.Add(c);
                agregados++;
            }
            ApplyFilter();
            SetStatus($"{agregados} contacto(s) importados desde '{path}'.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"Error durante la importación:\n{ex.Message}", "Aceptar");
        }
    }

    private void MenuExportar() {
        var dlgPath = new InputDialog("Exportar JSON", "Ruta del archivo JSON de salida:", "contactos.json");
        App!.Run(dlgPath);
        if (!dlgPath.WasAccepted || string.IsNullOrWhiteSpace(dlgPath.InputValue)) return;

        string path = dlgPath.InputValue.Trim();
        try {
            JsonAgendaIO.Write(_contacts, path);
            SetStatus($"{_contacts.Count} contacto(s) exportados a '{path}'.");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo exportar:\n{ex.Message}", "Aceptar");
        }
    }

    private static void ValidateImportedContacts(IEnumerable<Contacto> contactos) {
        int row = 1;
        foreach (var contacto in contactos) {
            if (string.IsNullOrEmpty(contacto.Nombre?.Trim() ?? ""))
                throw new JsonException($"El contacto #{row} no tiene nombre.");
            if (!string.IsNullOrEmpty(contacto.Email?.Trim() ?? "") && !contacto.Email.Contains('@'))
                throw new JsonException($"El contacto #{row} tiene un email inválido.");
            row++;
        }
    }

    private static void NormalizeContact(Contacto contacto) {
        contacto.Nombre = contacto.Nombre?.Trim() ?? "";
        contacto.Telefonos = string.Join(", ", (contacto.Telefonos ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Take(5));
        contacto.Email = contacto.Email?.Trim() ?? "";
        contacto.Notas ??= "";
    }

}

public sealed class ContactDialog : Dialog {
    public bool WasAccepted { get; private set; } = false;
    public Contacto? ContactResult { get; private set; }

    private readonly TextField _nombreField;
    private readonly TextField[] _telefonoFields = new TextField[5];
    private readonly TextField _emailField;
    private readonly TextView _notasField;
    private readonly CheckBox _favoritoCheck;

    public ContactDialog(string title, Contacto contacto) {
        Title = title;
        Width = 62;
        Height = 27;

        int row = 0;

        Add(new Label() { Text = "Nombre (*):", X = 1, Y = row });
        row++;
        _nombreField = new TextField() {
            Text = contacto.Nombre,
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
        };
        Add(_nombreField);
        row++;

        Add(new Label() { Text = "Teléfonos (hasta 5, uno por campo):", X = 1, Y = row });
        row++;
        string[] teleparts = contacto.Telefonos.Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++) {
            Add(new Label() { Text = $"  Tel {i + 1}:", X = 1, Y = row });
            string val = i < teleparts.Length ? teleparts[i] : "";
            _telefonoFields[i] = new TextField() {
                Text = val,
                X = 10,
                Y = row,
                Width = Dim.Fill(1),
            };
            Add(_telefonoFields[i]);
            row++;
        }

        Add(new Label() { Text = "Email:", X = 1, Y = row });
        row++;
        _emailField = new TextField() {
            Text = contacto.Email,
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
        };
        Add(_emailField);
        row++;

        _favoritoCheck = new CheckBox() {
            Text = "Favorito",
            X = 1,
            Y = row,
            Value = contacto.Favorito
                ? CheckState.Checked
                : CheckState.UnChecked,
        };
        Add(_favoritoCheck);
        row++;

        Add(new Label() { Text = "Notas:", X = 1, Y = row });
        row++;
        _notasField = new TextView() {
            Text = contacto.Notas,
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
            Height = 3,
        };
        Add(_notasField);

        Button btnGuardar = new() { Text = "_Guardar", IsDefault = true };
        btnGuardar.Accepting += (_, e) => { OnGuardar(); e.Handled = true; };

        Button btnCancelar = new() { Text = "_Cancelar" };
        btnCancelar.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };

        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }

    private void OnGuardar() {
        string nombre = _nombreField.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(nombre)) {
            MessageBox.ErrorQuery(App!, "Validación", "El nombre no puede estar vacío.", "Aceptar");
            return;
        }

        string email = _emailField.Text?.Trim() ?? "";
        if (!string.IsNullOrEmpty(email) && !email.Contains('@')) {
            MessageBox.ErrorQuery(App!, "Validación", "El email debe contener '@'.", "Aceptar");
            return;
        }

        var telefonos = _telefonoFields
            .Select(f => f.Text?.Trim() ?? "")
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        ContactResult = new Contacto {
            Nombre = nombre,
            Telefonos = string.Join(", ", telefonos),
            Email = email,
            Notas = _notasField.Text?.ToString() ?? "",
            Favorito = _favoritoCheck.Value == CheckState.Checked,
        };

        WasAccepted = true;
        App!.RequestStop();
    }
}

public sealed class InputDialog : Dialog {
    public bool WasAccepted { get; private set; } = false;
    public string InputValue => _field.Text?.Trim() ?? "";
    private readonly TextField _field;

    public InputDialog(string title, string prompt, string defaultValue = "") {
        Title = title; Width = 62; Height = 9;
        Add(new Label() { Text = prompt, X = 1, Y = 0 });
        _field = new TextField() { Text = defaultValue, X = 1, Y = 1, Width = Dim.Fill(1) };
        Add(_field);

        Button btnOk = new() { Text = "_Aceptar", IsDefault = true };
        btnOk.Accepting += (_, e) => { WasAccepted = true; App!.RequestStop(); e.Handled = true; };
        Button btnCancel = new() { Text = "_Cancelar" };
        btnCancel.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        AddButton(btnOk); AddButton(btnCancel);
    }
}
public sealed class SqliteAgendaStore : IDisposable {
    private readonly SqliteConnection _conn;

    public SqliteAgendaStore(string dbPath) {
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        EnsureSchema();
    }

    private void EnsureSchema() {
        _conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT    NOT NULL DEFAULT '',
                Telefonos TEXT    NOT NULL DEFAULT '',
                Email     TEXT    NOT NULL DEFAULT '',
                Notas     TEXT    NOT NULL DEFAULT '',
                Favorito  INTEGER NOT NULL DEFAULT 0
            )");
    }

    public List<Contacto> GetAll()
        => _conn.GetAll<Contacto>().ToList();

    public void Insert(Contacto c) {
        long id = _conn.Insert(c);
        c.Id = (int)id;
    }

    public void Update(Contacto c)
        => _conn.Update(c);

    public void Delete(Contacto c)
        => _conn.Delete(c);

    public void Dispose()
        => _conn.Dispose();
}

public static class JsonAgendaIO {
    private static readonly JsonSerializerOptions _opts = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public static List<Contacto> Read(string path) {
        if (!File.Exists(path))
            throw new FileNotFoundException("Archivo no encontrado.", path);

        string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        var list = JsonSerializer.Deserialize<List<Contacto>>(json, _opts);
        if (list == null)
            throw new JsonException("El archivo JSON no contiene un arreglo de contactos válido.");
        return list;
    }

    public static void Write(IEnumerable<Contacto> contactos, string path) {
        string json = JsonSerializer.Serialize(contactos.ToList(), _opts);
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
    }
}

[Table("Contactos")]
public class Contacto {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => new() {
        Id = Id,
        Nombre = Nombre,
        Telefonos = Telefonos,
        Email = Email,
        Notas = Notas,
        Favorito = Favorito,
    };
}