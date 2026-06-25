#r "nuget: Terminal.Gui, 2.0.0-beta.31"
#r "nuget: Microsoft.Data.Sqlite, 9.0.0"
#r "nuget: Dapper, 2.1.35"
#r "nuget: Dapper.Contrib, 2.0.78"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui;

#region 1. Top-Level Code (Procesar Argumentos y Arranque)

string dbFile = "agenda.db";
if (args.Length > 0)
{
    // Soporta tanto 'agenda.cs -- otra.db' como 'agenda.cs otra.db'
    dbFile = args[^1] == "--" ? "agenda.db" : args[^1];
}

var store = new SqliteAgendaStore(dbFile);
try
{
    store.Init();
}
catch (Exception ex)
{
    Console.WriteLine($"Error crítico al inicializar la base de datos: {ex.Message}");
    return;
}

// Inicializar Terminal.Gui
Application.Init();

var mainWindow = new AgendaWindow(store);
Application.Run(mainWindow);

Application.Shutdown();

#endregion

#region 2. Ventana Principal (AgendaWindow)

public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _contacts = new();
    private List<Contacto> _filteredContacts = new();

    // Componentes visuales
    private TextField _searchField;
    private ListView _listView;
    private TextView _detailView;
    private StatusBar _statusBar;
    private bool _showOnlyFavorites = false;

    public AgendaWindow(SqliteAgendaStore store)
    {
        _store = store;
        Title = $" AgendaT 2026 — [DB: {Path.GetFileName(store.DbPath)}] ";
        ColorScheme = Colors.ColorSchemes["Base"];

        InitMenu();
        InitControls();
        InitStatusBar();
        
        LoadData();

        // Atajos globales requeridos por consigna
        KeyBindings.Add(Key.F2, () => { OnNuevo(); return true; });
        KeyBindings.Add(Key.CtrlSpace | Key.N, () => { OnNuevo(); return true; }); // Ctrl+N equivalente
        KeyBindings.Add(Key.F3, () => { OnEditar(); return true; });
        KeyBindings.Add(Key.F4, () => { _searchField.SetFocus(); return true; });
        KeyBindings.Add(Key.Delete, () => { OnEliminar(); return true; });
        KeyBindings.Add(Key.CtrlSpace | Key.D, () => { OnEliminar(); return true; }); // Ctrl+D equivalente
        KeyBindings.Add(Key.CtrlSpace | Key.I, () => { OnImportar(); return true; });
        KeyBindings.Add(Key.CtrlSpace | Key.E, () => { OnExportar(); return true; });
        KeyBindings.Add(Key.CtrlSpace | Key.Q, () => { OnSalir(); return true; });
    }

    private void InitMenu()
    {
        var menuBar = new MenuBar(new MenuBarItem[] {
            new MenuBarItem ("_Archivo", new MenuItem [] {
                new MenuItem ("_Importar JSON", "Ctrl+I", OnImportar),
                new MenuItem ("_Exportar JSON", "Ctrl+E", OnExportar),
                null,
                new MenuItem ("_Salir", "Ctrl+Q", OnSalir)
            }),
            new MenuBarItem ("_Contactos", new MenuItem [] {
                new MenuItem ("_Nuevo", "F2", OnNuevo),
                new MenuItem ("_Editar", "F3 / Enter", OnEditar),
                new MenuItem ("_Eliminar", "Del", OnEliminar)
            }),
            new MenuBarItem ("_Ver", new MenuItem [] {
                new MenuItem ("_Solo favoritos", "Toggle", OnToggleFavoritos, () => _showOnlyFavorites)
            }),
            new MenuBarItem ("A_yuda", new MenuItem [] {
                new MenuItem ("_Acerca de", "", OnAcercaDe)
            })
        });
        Add(menuBar);
    }

    private void InitControls()
    {
        // Etiqueta y Campo de búsqueda activa
        var searchLabel = new Label("Buscar:") { X = 1, Y = 1 };
        _searchField = new TextField("") { X = 9, Y = 1, Width = Dim.Fill(2) };
        _searchField.TextChanged += (sender, e) => ApplyFilter();

        // Panel de lista (Izquierda)
        var listFrame = new FrameView(" Contactos ") {
            X = 1, Y = 3, Width = Dim.Percent(45), Height = Dim.Fill(1)
        };
        _listView = new ListView(_filteredContacts) {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), AllowMarking = false
        };
        _listView.SelectedItemChanged += (sender, e) => UpdateDetailPanel();
        _listView.OpenSelectedItem += (sender, e) => OnEditar();
        listFrame.Add(_listView);

        // Panel de detalle (Derecha)
        var detailFrame = new FrameView(" Detalle del Contacto ") {
            X = Pos.Right(listFrame) + 1, Y = 3, Width = Dim.Fill(1), Height = Dim.Fill(1)
        };
        _detailView = new TextView() {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true
        };
        detailFrame.Add(_detailView);

        Add(searchLabel, _searchField, listFrame, detailFrame);
    }

    private void InitStatusBar()
    {
        _statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.Empty, "Listo", null)
        });
        Add(_statusBar);
    }

    private void UpdateStatus(string message)
    {
        _statusBar.Subviews[0].Title = message;
        _statusBar.SetNeedsDisplay();
    }

    private void LoadData()
    {
        try
        {
            _contacts = _store.GetAll().OrderBy(c => c.Nombre).ToList();
            ApplyFilter();
            UpdateStatus("Contactos cargados desde la base de datos.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"No se pudieron cargar los datos:\n{ex.Message}", "Aceptar");
        }
    }

    private void ApplyFilter()
    {
        string query = _searchField.Text.ToString()?.Trim().ToLower() ?? "";
        
        var queryable = _contacts.AsEnumerable();

        if (_showOnlyFavorites)
        {
            queryable = queryable.Where(c => c.Favorito);
        }

        if (!string.IsNullOrEmpty(query))
        {
            queryable = queryable.Where(c => 
                (c.Nombre?.ToLower().Contains(query) ?? false) ||
                (c.Telefonos?.ToLower().Contains(query) ?? false) ||
                (c.Email?.ToLower().Contains(query) ?? false)
            );
        }

        _filteredContacts = queryable.ToList();
        
        // Mapear strings custom para la lista visualizando favoritos con un (*)
        _listView.SetSource(_filteredContacts.Select(c => $"{(c.Favorito ? "[★] " : "    ")} {c.Nombre}").ToList());
        
        UpdateDetailPanel();
    }

    private void UpdateDetailPanel()
    {
        if (_listView.SelectedItem >= 0 && _listView.SelectedItem < _filteredContacts.Count)
        {
            var c = _filteredContacts[_listView.SelectedItem];
            _detailView.Text = $"Nombre: {c.Nombre}\n" +
                               $"Favorito: {(c.Favorito ? "Sí" : "No")}\n" +
                               $"Email: {c.Email}\n" +
                               $"Teléfonos:\n{string.Join("\n", c.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => $"  - {t.Trim()}"))}\n\n" +
                               $"Notas:\n{c.Notas}";
        }
        else
        {
            _detailView.Text = "No hay ningún contacto seleccionado.";
        }
    }

    // --- MANEJO DE ACCIONES ---

    private void OnNuevo()
    {
        var nuevo = new Contacto();
        var dialog = new ContactDialog(nuevo, "Nuevo Contacto");
        Application.Run(dialog);

        if (dialog.SaveConfirmed)
        {
            try
            {
                _store.Insert(nuevo);
                LoadData();
                UpdateStatus($"Contacto '{nuevo.Nombre}' creado con éxito.");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"No se pudo guardar en la DB:\n{ex.Message}", "Aceptar");
            }
        }
    }

    private void OnEditar()
    {
        if (_listView.SelectedItem < 0 || _listView.SelectedItem >= _filteredContacts.Count) return;
        
        var original = _filteredContacts[_listView.SelectedItem];
        var clon = original.Clone();

        var dialog = new ContactDialog(clon, "Editar Contacto");
        Application.Run(dialog);

        if (dialog.SaveConfirmed)
        {
            try
            {
                _store.Update(clon);
                LoadData();
                UpdateStatus($"Contacto '{clon.Nombre}' actualizado.");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"No se pudo actualizar en la DB:\n{ex.Message}", "Aceptar");
            }
        }
    }

    private void OnEliminar()
    {
        if (_listView.SelectedItem < 0 || _listView.SelectedItem >= _filteredContacts.Count) return;
        var seleccionado = _filteredContacts[_listView.SelectedItem];

        int result = MessageBox.Query("Confirmar eliminación", $"¿Está seguro que desea eliminar a {seleccionado.Nombre}?", "Sí", "No");
        if (result == 0)
        {
            try
            {
                _store.Delete(seleccionado.Id);
                LoadData();
                UpdateStatus($"Contacto '{seleccionado.Nombre}' eliminado.");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"No se pudo eliminar de la DB:\n{ex.Message}", "Aceptar");
            }
        }
    }

    private void OnToggleFavoritos()
    {
        _showOnlyFavorites = !_showOnlyFavorites;
        ApplyFilter();
    }

    private void OnImportar()
    {
        // Diálogo simple para ingresar la ruta del JSON
        var d = new Dialog("Importar desde JSON", 60, 7);
        var lbl = new Label("Ruta del archivo:") { X = 1, Y = 1 };
        var tf = new TextField("contactos.json") { X = 19, Y = 1, Width = Dim.Fill(2) };
        var btnOk = new Button("Importar") { X = Pos.Center() - 10, Y = 3 };
        var btnCancel = new Button("Cancelar") { X = Pos.Center() + 2, Y = 3 };

        bool procesar = false;
        btnOk.Clicked += (s, e) => { procesar = true; Application.RequestStop(); };
        btnCancel.Clicked += (s, e) => { Application.RequestStop(); };
        d.Add(lbl, tf, btnOk, btnCancel);
        Application.Run(d);

        if (!procesar) return;

        string path = tf.Text.ToString() ?? "";
        if (!File.Exists(path))
        {
            MessageBox.ErrorQuery("Error", "El archivo JSON especificado no existe.", "Aceptar");
            return;
        }

        try
        {
            var importados = JsonAgendaIO.Import(path);
            int conf = MessageBox.Query("Confirmar Importación", $"Se encontraron {importados.Count} contactos.\n¿Desea agregarlos como registros nuevos?", "Sí", "No");
            if (conf == 0)
            {
                foreach (var c in importados)
                {
                    _store.Insert(c);
                }
                LoadData();
                UpdateStatus($"Se importaron {importados.Count} contactos exitosamente.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error de Importación", $"El JSON posee un formato inválido o está corrupto:\n{ex.Message}", "Aceptar");
        }
    }

    private void OnExportar()
    {
        var d = new Dialog("Exportar a JSON", 60, 7);
        var lbl = new Label("Ruta de destino:") { X = 1, Y = 1 };
        var tf = new TextField("export_agenda.json") { X = 18, Y = 1, Width = Dim.Fill(2) };
        var btnOk = new Button("Exportar") { X = Pos.Center() - 10, Y = 3 };
        var btnCancel = new Button("Cancelar") { X = Pos.Center() + 2, Y = 3 };

        bool procesar = false;
        btnOk.Clicked += (s, e) => { procesar = true; Application.RequestStop(); };
        btnCancel.Clicked += (s, e) => { Application.RequestStop(); };
        d.Add(lbl, tf, btnOk, btnCancel);
        Application.Run(d);

        if (!procesar) return;

        string path = tf.Text.ToString() ?? "";
        try
        {
            JsonAgendaIO.Export(path, _contacts);
            UpdateStatus($"Agenda exportada correctamente a '{path}'.");
            MessageBox.Query("Éxito", "Exportación completada con éxito.", "Aceptar");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error al Exportar", $"No se pudo escribir el archivo:\n{ex.Message}", "Aceptar");
        }
    }

    private void OnAcercaDe()
    {
        MessageBox.Query("Acerca de", "AgendaT — Trabajo Práctico 3\nDesarrollado en C# (.NET 10 TUI)\nPersistencia: SQLite + Dapper\nAño: 2026", "Cerrar");
    }

    private void OnSalir()
    {
        Application.RequestStop();
    }
}

#endregion

#region 3. Diálogo de Edición (ContactDialog)

public sealed class ContactDialog : Dialog
{
    public bool SaveConfirmed { get; private set; } = false;
    private readonly Contacto _contacto;

    // Componentes del Formulario
    private TextField _txtNombre;
    private CheckBox _chkFavorito;
    private TextField _txtEmail;
    private List<TextField> _txtTelefonos = new();
    private TextView _txtNotas;

    public ContactDialog(Contacto contacto, string titulo)
    {
        _contacto = contacto;
        Title = titulo;
        Width = 68;
        Height = 20;

        InitForm();
    }

    private void InitForm()
    {
        var lblNombre = new Label("Nombre (*):") { X = 2, Y = 1 };
        _txtNombre = new TextField(_contacto.Nombre) { X = 16, Y = 1, Width = 46 };

        _chkFavorito = new CheckBox("Marcar como Favorito", _contacto.Favorito) { X = 16, Y = 2 };

        var lblEmail = new Label("Email:") { X = 2, Y = 4 };
        _txtEmail = new TextField(_contacto.Email) { X = 16, Y = 4, Width = 46 };

        // Dividir teléfonos individuales guardados en formato CSV (hasta 5)
        var telfsExistentes = _contacto.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(t => t.Trim()).ToList();
        
        var lblTels = new Label("Teléfonos:") { X = 2, Y = 6 };
        
        for (int i = 0; i < 5; i++)
        {
            string val = i < telfsExistentes.Count ? telfsExistentes[i] : "";
            var tf = new TextField(val) {
                X = 16 + (i * 9), Y = 6, Width = 8
            };
            _txtTelefonos.Add(tf);
            Add(tf);
        }

        var lblNotas = new Label("Notas:") { X = 2, Y = 8 };
        _txtNotas = new TextView() {
            X = 16, Y = 8, Width = 46, Height = 4,
            Text = _contacto.Notas
        };

        // Botones de acción del diálogo
        var btnGuardar = new Button("Guardar");
        btnGuardar.Clicked += (sender, e) => OnValidarYGuardar();

        var btnCancelar = new Button("Cancelar");
        btnCancelar.Clicked += (sender, e) => { Application.RequestStop(); };

        AddButton(btnGuardar);
        AddButton(btnCancelar);

        Add(lblNombre, _txtNombre, _chkFavorito, lblEmail, lblTels, lblNotas, _txtNotas);
    }

    private void OnValidarYGuardar()
    {
        string nombre = _txtNombre.Text.ToString()?.Trim() ?? "";
        string email = _txtEmail.Text.ToString()?.Trim() ?? "";

        // Reglas de Validación Requeridas por la Consigna
        if (string.IsNullOrEmpty(nombre))
        {
            MessageBox.ErrorQuery("Validación", "El Nombre no puede estar vacío.", "Aceptar");
            return;
        }

        if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
        {
            MessageBox.ErrorQuery("Validación", "El Email es inválido (debe contener '@').", "Aceptar");
            return;
        }

        // Construcción de la cadena de teléfonos separados por comas
        var listaTels = _txtTelefonos.Select(t => t.Text.ToString()?.Trim())
                                     .Where(t => !string.IsNullOrEmpty(t));
        
        // Mapear de regreso al objeto de transferencia de datos de la UI
        _contacto.Nombre = nombre;
        _contacto.Favorito = _chkFavorito.Checked;
        _contacto.Email = email;
        _contacto.Telefonos = string.Join(",", listaTels);
        _contacto.Notas = _txtNotas.Text.ToString() ?? "";

        SaveConfirmed = true;
        Application.RequestStop();
    }
}

#endregion

#region 4. Persistencia (SqliteAgendaStore)

public sealed class SqliteAgendaStore
{
    public string DbPath { get; }

    public SqliteAgendaStore(string dbPath)
    {
        DbPath = dbPath;
    }

    private SqliteConnection GetConnection() => new($"Data Source={DbPath}");

    public void Init()
    {
        using var db = GetConnection();
        db.Open();
        string query = @"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            );";
        db.Execute(query);
    }

    public IEnumerable<Contacto> GetAll()
    {
        using var db = GetConnection();
        return db.GetAll<Contacto>();
    }

    public long Insert(Contacto c)
    {
        using var db = GetConnection();
        return db.Insert(c);
    }

    public bool Update(Contacto c)
    {
        using var db = GetConnection();
        return db.Update(c);
    }

    public bool Delete(int id)
    {
        using var db = GetConnection();
        return db.Delete(new Contacto { Id = id });
    }
}

#endregion

#region 5. Interoperabilidad JSON (JsonAgendaIO)

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // Garantiza que caracteres como 'ñ' o vocales con tilde se guarden legibles de forma nativa
        Encoder = JavaScriptEncoder.Unescaped 
    };

    public static List<Contacto> Import(string path)
    {
        string json = File.ReadAllText(path);
        var lista = JsonSerializer.Deserialize<List<Contacto>>(json, Options);
        return lista ?? new List<Contacto>();
    }

    public static void Export(string path, List<Contacto> contactos)
    {
        string json = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, json);
    }
}

#endregion

#region 6. Modelo de Datos (Contacto)

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

#endregion