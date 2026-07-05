#:package Terminal.Gui --version 2.0.0-beta.31
#:package Microsoft.Data.Sqlite
#:package Dapper
#:package Dapper.Contrib

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui;

// ============================================================================
// 1. TOP-LEVEL CODE (Procesar argumentos y arrancar la App)
// ============================================================================

string dbPath = "agenda.db";
if (args.Length > 0)
{
    dbPath = args[0];
}

SqliteAgendaStore store;
try
{
    store = new SqliteAgendaStore(dbPath);
}
catch (Exception ex)
{
    Console.WriteLine($"Error crítico al inicializar la base de datos: {ex.Message}");
    return;
}

Application.Init();
var win = new AgendaWindow(store);
Application.Run(win);
Application.Shutdown();

// ============================================================================
// 2. CLASE AgendaWindow (Layout principal, menús y eventos)
// ============================================================================
public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _allContacts = new();
    private List<Contacto> _filteredContacts = new();

    private TextField _searchField;
    private ListView _listView;
    private TextView _detailView;
    private StatusBar _statusBar;
    
    private bool _filterOnlyFavorites = false;

    public AgendaWindow(SqliteAgendaStore store)
    {
        _store = store;
        Title = " AgendaT — Gestor de Contactos TUI ";
        ColorScheme = Colors.ColorSchemes["Base"];

        InitMenus();
        InitLayout();
        LoadContacts();

        // Configurar Atajos Globales requeridos
        AddKeyBinding(Key.F2, Command.HotKey);
        AddKeyBinding(Key.F3, Command.HotKey);
        AddKeyBinding(Key.F4, Command.HotKey);
    }

    private void InitMenus()
    {
        var menuBar = new MenuBar(new MenuBarItem[] {
            new MenuBarItem ("_Archivo", new MenuItem [] {
                new MenuItem ("_Importar JSON", "Ctrl+I", ActionImport, null, null, Key.I.WithCtrl),
                new MenuItem ("_Exportar JSON", "Ctrl+E", ActionExport, null, null, Key.E.WithCtrl),
                new MenuItem ("_Salir", "Ctrl+Q", () => Application.RequestStop(), null, null, Key.Q.WithCtrl)
            }),
            new MenuBarItem ("_Contactos", new MenuItem [] {
                new MenuItem ("_Nuevo", "F2", ActionNuevo, null, null, Key.F2),
                new MenuItem ("_Editar", "Enter", ActionEditar),
                new MenuItem ("_Eliminar", "Del", ActionEliminar, null, null, Key.DeleteChar)
            }),
            new MenuBarItem ("_Ver", new MenuItem [] {
                new MenuItem ("_Solo Favoritos", "Toggle", ActionToggleFavoritos, () => _filterOnlyFavorites)
            }),
            new MenuBarItem ("A_yuda", new MenuItem [] {
                new MenuItem ("_Acerca de", "", () => MessageBox.Query("Acerca de", "AgendaT v1.0\nTrabajo Práctico 3\nDesarrollado en .NET 10 con Terminal.Gui.", "OK"))
            })
        });
        Add(menuBar);
    }

    private void InitLayout()
    {
        // Panel de Búsqueda
        var searchLabel = new Label("Buscar:") { X = 1, Y = 1 };
        _searchField = new TextField("") { X = 9, Y = 1, Width = Dim.Fill(1) };
        _searchField.TextChanged += (sender, e) => UpdateFilter();
        Add(searchLabel, _searchField);

        // Contenedor para división Izquierda (Lista) e Derecha (Detalle)
        var leftPane = new FrameView(" Contactos ") {
            X = 0, Y = 3, Width = Dim.Percent(45), Height = Dim.Fill(1)
        };
        _listView = new ListView(_filteredContacts) {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
        };
        _listView.OpenSelectedItem += (sender, e) => ActionEditar();
        _listView.SelectedItemChanged += (sender, e) => UpdateDetail();
        leftPane.Add(_listView);

        var rightPane = new FrameView(" Vista de Detalle ") {
            X = Pos.Right(leftPane), Y = 3, Width = Dim.Fill(), Height = Dim.Fill(1)
        };
        _detailView = new TextView() {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true
        };
        rightPane.Add(_detailView);

        Add(leftPane, rightPane);

        // Barra de estado
        _statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.F2, "~F2~ Nuevo", ActionNuevo),
            new StatusItem(Key.F3, "~Enter~ Editar", ActionEditar),
            new StatusItem(Key.DeleteChar, "~Del~ Eliminar", ActionEliminar),
            new StatusItem(Key.F4, "~F4~ Buscar", () => _searchField.SetFocus())
        });
        Add(_statusBar);
    }

    private void LoadContacts()
    {
        try
        {
            _allContacts = _store.GetAll().ToList();
            UpdateFilter();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"No se pudieron cargar los contactos: {ex.Message}", "OK");
        }
    }

    private void UpdateFilter()
    {
        string text = _searchField.Text?.ToString().ToLower() ?? "";
        var query = _allContacts.AsEnumerable();

        if (_filterOnlyFavorites)
        {
            query = query.Where(c => c.Favorito);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(c => 
                (c.Nombre != null && c.Nombre.ToLower().Contains(text)) ||
                (c.Telefonos != null && c.Telefonos.ToLower().Contains(text)) ||
                (c.Email != null && c.Email.ToLower().Contains(text))
            );
        }

        _filteredContacts = query.ToList();
        
        // Mapear strings limpios para renderizado de fila
        _listView.SetSource(_filteredContacts.Select(c => $"{(c.Favorito ? "★" : " ")} {c.Nombre}").ToList());
        UpdateDetail();
    }

    private void UpdateDetail()
    {
        if (_listView.SelectedItem >= 0 && _listView.SelectedItem < _filteredContacts.Count)
        {
            var c = _filteredContacts[_listView.SelectedItem];
            _detailView.Text = $"ID: {c.Id}\n" +
                               $"Nombre: {c.Nombre}\n" +
                               $"Teléfonos: {c.Telefonos}\n" +
                               $"Email: {c.Email}\n" +
                               $"Favorito: {(c.Favorito ? "Sí" : "No")}\n" +
                               $"----------------------------------------\n" +
                               $"Notas:\n{c.Notas}";
        }
        else
        {
            _detailView.Text = "Ningún contacto seleccionado.";
        }
    }

    private void LogStatus(string message)
    {
        // Terminal.Gui administra dinámicamente la barra de estado. Actualizamos el título de contexto.
        _statusBar.Subviews[0].Text = $"[ {message} ]";
    }

    // --- ACCIONES ---

    private void ActionNuevo()
    {
        var nuevo = new Contacto();
        var dialog = new ContactDialog(nuevo);
        Application.Run(dialog);

        if (dialog.IsSaved)
        {
            try
            {
                _store.Insert(nuevo);
                LogStatus("Contacto guardado correctamente.");
                LoadContacts();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"No se pudo persistir el contacto: {ex.Message}", "OK");
            }
        }
    }

    private void ActionEditar()
    {
        if (_listView.SelectedItem < 0 || _listView.SelectedItem >= _filteredContacts.Count) return;

        var original = _filteredContacts[_listView.SelectedItem];
        var clon = original.Clone();

        var dialog = new ContactDialog(clon);
        Application.Run(dialog);

        if (dialog.IsSaved)
        {
            try
            {
                _store.Update(clon);
                LogStatus("Contacto modificado.");
                LoadContacts();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"No se pudo actualizar el contacto: {ex.Message}", "OK");
            }
        }
    }

    private void ActionEliminar()
    {
        if (_listView.SelectedItem < 0 || _listView.SelectedItem >= _filteredContacts.Count) return;
        var seleccionado = _filteredContacts[_listView.SelectedItem];

        int result = MessageBox.Query("Confirmar eliminación", $"¿Está seguro que desea eliminar a {seleccionado.Nombre}?", "Sí", "No");
        if (result == 0)
        {
            try
            {
                _store.Delete(seleccionado);
                LogStatus("Contacto eliminado.");
                LoadContacts();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"No se pudo eliminar el contacto: {ex.Message}", "OK");
            }
        }
    }

    private void ActionToggleFavoritos()
    {
        _filterOnlyFavorites = !_filterOnlyFavorites;
        UpdateFilter();
    }

    private void ActionImport()
    {
        var d = new Dialog("Importar JSON", 50, 10);
        var lbl = new Label("Ruta de archivo:") { X = 1, Y = 1 };
        var txt = new TextField("agenda.json") { X = 1, Y = 2, Width = Dim.Fill(1) };
        var btnOk = new Button("Importar") { X = Pos.Center() - 10, Y = 5 };
        var btnCancel = new Button("Cancelar") { X = Pos.Center() + 2, Y = 5 };

        btnCancel.Clicked += (s, e) => Application.RequestStop(d);
        btnOk.Clicked += (s, e) => {
            Application.RequestStop(d);
            string path = txt.Text?.ToString() ?? "";
            if (!File.Exists(path))
            {
                MessageBox.ErrorQuery("Error", "El archivo JSON especificado no existe.", "OK");
                return;
            }

            try
            {
                var list = JsonAgendaIO.Import(path);
                int confirm = MessageBox.Query("Confirmar Importación", $"Se encontraron {list.Count} contactos. ¿Desea agregarlos?", "Sí", "No");
                if (confirm == 0)
                {
                    foreach (var c in list)
                    {
                        _store.Insert(c);
                    }
                    LogStatus($"Se importaron {list.Count} contactos.");
                    LoadContacts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error de formato", $"El archivo JSON posee un formato inválido:\n{ex.Message}", "OK");
            }
        };

        d.Add(lbl, txt, btnOk, btnCancel);
        Application.Run(d);
    }

    private void ActionExport()
    {
        var d = new Dialog("Exportar JSON", 50, 10);
        var lbl = new Label("Ruta de salida:") { X = 1, Y = 1 };
        var txt = new TextField("agenda_export.json") { X = 1, Y = 2, Width = Dim.Fill(1) };
        var btnOk = new Button("Exportar") { X = Pos.Center() - 10, Y = 5 };
        var btnCancel = new Button("Cancelar") { X = Pos.Center() + 2, Y = 5 };

        btnCancel.Clicked += (s, e) => Application.RequestStop(d);
        btnOk.Clicked += (s, e) => {
            Application.RequestStop(d);
            string path = txt.Text?.ToString() ?? "";
            try
            {
                JsonAgendaIO.Export(path, _allContacts);
                MessageBox.Query("Éxito", "Exportación completada correctamente.", "OK");
                LogStatus("Agenda exportada con éxito.");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"No se pudo exportar el archivo:\n{ex.Message}", "OK");
            }
        };

        d.Add(lbl, txt, btnOk, btnCancel);
        Application.Run(d);
    }
}

// ============================================================================
// 3. CLASE ContactDialog (Formulario dinámico de ingreso y validación)
// ============================================================================
public sealed class ContactDialog : Dialog
{
    public bool IsSaved { get; private set; } = false;
    private readonly Contacto _contacto;

    private TextField _txtName;
    private TextField[] _txtPhones = new TextField[5];
    private TextField _txtEmail;
    private TextView _txtNotes;
    private CheckBox _chkFav;

    public ContactDialog(Contacto contacto)
    {
        _contacto = contacto;
        Title = _contacto.Id == 0 ? "Nuevo Contacto" : "Editar Contacto";
        Width = 65;
        Height = 20;

        InitControls();
    }

    private void InitControls()
    {
        var lblName = new Label("Nombre (*):") { X = 1, Y = 1 };
        _txtName = new TextField(_contacto.Nombre) { X = 14, Y = 1, Width = Dim.Fill(1) };

        // Descomponer teléfonos separados por coma en hasta 5 campos individuales
        string[] existingPhones = (_contacto.Telefonos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        var lblPhones = new Label("Teléfonos:") { X = 1, Y = 3 };
        for (int i = 0; i < 5; i++)
        {
            string val = i < existingPhones.Length ? existingPhones[i].Trim() : "";
            _txtPhones[i] = new TextField(val) {
                X = 14 + (i * 9), Y = 3, Width = 8
            };
            Add(_txtPhones[i]);
        }

        var lblEmail = new Label("Email:") { X = 1, Y = 5 };
        _txtEmail = new TextField(_contacto.Email) { X = 14, Y = 5, Width = Dim.Fill(1) };

        _chkFav = new CheckBox("Marcar como Favorito", _contacto.Favorito) { X = 14, Y = 7 };

        var lblNotes = new Label("Notas:") { X = 1, Y = 9 };
        _txtNotes = new TextView() {
            X = 14, Y = 9, Width = Dim.Fill(1), Height = 4,
            Text = _contacto.Notas
        };

        Add(lblName, _txtName, lblPhones, lblEmail, _chkFav, lblNotes, _txtNotes);

        var btnSave = new Button("Guardar");
        btnSave.Clicked += (s, e) => SaveOrValidate();

        var btnCancel = new Button("Cancelar");
        btnCancel.Clicked += (s, e) => Application.RequestStop(this);

        AddButton(btnSave);
        AddButton(btnCancel);
    }

    private void SaveOrValidate()
    {
        string name = _txtName.Text?.ToString().Trim() ?? "";
        string email = _txtEmail.Text?.ToString().Trim() ?? "";

        // Validaciones requeridas
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.ErrorQuery("Validación", "El Nombre es un campo obligatorio.", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
        {
            MessageBox.ErrorQuery("Validación", "El Email ingresado debe contener un carácter '@'.", "OK");
            return;
        }

        // Recomponer los inputs numéricos a un string csv
        var phoneList = new List<string>();
        foreach (var t in _txtPhones)
        {
            string pStr = t.Text?.ToString().Trim() ?? "";
            if (!string.IsNullOrEmpty(pStr)) phoneList.Add(pStr);
        }

        // Volcar datos al objeto mutado
        _contacto.Nombre = name;
        _contacto.Telefonos = string.Join(",", phoneList);
        _contacto.Email = email;
        _contacto.Favorito = _chkFav.Checked;
        _contacto.Notas = _txtNotes.Text?.ToString() ?? "";

        IsSaved = true;
        Application.RequestStop(this);
    }
}

// ============================================================================
// 4. CLASE SqliteAgendaStore (Infraestructura de persistencia SQLite + Dapper)
// ============================================================================
public sealed class SqliteAgendaStore
{
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitDatabase();
    }

    private void InitDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string ddl = @"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            );";
        conn.Execute(ddl);
    }

    public IEnumerable<Contacto> GetAll()
    {
        using var conn = new SqliteConnection(_connectionString);
        return conn.GetAll<Contacto>();
    }

    public long Insert(Contacto c)
    {
        using var conn = new SqliteConnection(_connectionString);
        return conn.Insert(c);
    }

    public bool Update(Contacto c)
    {
        using var conn = new SqliteConnection(_connectionString);
        return conn.Update(c);
    }

    public bool Delete(Contacto c)
    {
        using var conn = new SqliteConnection(_connectionString);
        return conn.Delete(c);
    }
}

// ============================================================================
// 5. CLASE JsonAgendaIO (Serialización / Deserialización)
// ============================================================================
public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Soporte Ñs y tildes
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static List<Contacto> Import(string path)
    {
        string json = File.ReadAllText(path);
        var rawList = JsonSerializer.Deserialize<List<Contacto>>(json, Options) ?? new List<Contacto>();
        
        // Regla: No conservar los ids del archivo viejo para evitar colisiones
        foreach (var c in rawList)
        {
            c.Id = 0;
        }
        return rawList;
    }

    public static void Export(string path, List<Contacto> list)
    {
        string json = JsonSerializer.Serialize(list, Options);
        File.WriteAllText(path, json);
    }
}

// ============================================================================
// 6. CLASE MODELO (Entidad mapeable por ORM)
// ============================================================================
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