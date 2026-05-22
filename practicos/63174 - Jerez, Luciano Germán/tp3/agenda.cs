#:package Dapper --version 2.1.35
#:package Dapper.Contrib --version 2.0.78
#:package Microsoft.Data.Sqlite --version 9.0.0
#:package Terminal.Gui --version 2.0.0-v2-develop.93

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui;

#region 1. TOP-LEVEL CODE (Procesar argumentos e inicialización)

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

try
{
    var store = new SqliteAgendaStore(dbPath);
    
    Application.Init();
    
    var mainWindow = new AgendaWindow(store);
    Application.Run(mainWindow);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error crítico de inicialización: {ex.Message}");
    Console.ResetColor();
}
finally
{
    Application.Shutdown();
}

#endregion

#region 2. VENTANA PRINCIPAL (AgendaWindow)

public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _allContacts = new();
    private List<Contacto> _filteredContacts = new();

    // Componentes de la interfaz
    private TextField _searchField = null!;
    private ListView _listView = null!;
    private TextView _detailsTextView = null!;
    private StatusBar _statusBar = null!;
    private bool _showOnlyFavorites;

    public AgendaWindow(SqliteAgendaStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Title = $" Agenda Telefónica SQL/JSON - [{_store.DbPath}] ";
        ColorScheme = Colors.ColorSchemes["Base"];

        InitMenus();
        InitLayout();
        LoadContacts();
        
        // Atajos globales requeridos por especificación
        KeyBindings.Add(Key.F2, () => { OnNuevoContacto(); return true; });
        KeyBindings.Add(Key.CtrlMask | Key.N, () => { OnNuevoContacto(); return true; });
        KeyBindings.Add(Key.F3, () => { OnEditarContacto(); return true; });
        KeyBindings.Add(Key.Enter, () => { OnEditarContacto(); return true; });
        KeyBindings.Add(Key.DeleteChar, () => { OnEliminarContacto(); return true; });
        KeyBindings.Add(Key.CtrlMask | Key.D, () => { OnEliminarContacto(); return true; });
        KeyBindings.Add(Key.CtrlMask | Key.I, () => { OnImportarJson(); return true; });
        KeyBindings.Add(Key.CtrlMask | Key.E, () => { OnExportarJson(); return true; });
        KeyBindings.Add(Key.F4, () => { _searchField.SetFocus(); return true; });
        KeyBindings.Add(Key.CtrlMask | Key.Q, () => { OnSalir(); return true; });
    }

    private void InitMenus()
    {
        var menuBar = new MenuBar
        {
            Menus = new MenuBarItem[]
            {
                new MenuBarItem("_Archivo", new MenuItem[]
                {
                    new MenuItem("_Importar JSON", "Ctrl+I", OnImportarJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", OnExportarJson),
                    null!, // Separador
                    new MenuItem("_Salir", "Ctrl+Q", OnSalir)
                }),
                new MenuBarItem("_Contactos", new MenuItem[]
                {
                    new MenuItem("_Nuevo", "F2 / Ctrl+N", OnNuevoContacto),
                    new MenuItem("_Editar", "F3 / Enter", OnEditarContacto),
                    new MenuItem("_Eliminar", "Del / Ctrl+D", OnEliminarContacto)
                }),
                new MenuBarItem("_Ver", new MenuItem[]
                {
                    new MenuItem("_Solo Favoritos", "Toggle", OnToggleFavoritos, () => _showOnlyFavorites) { Checked = _showOnlyFavorites }
                }),
                new MenuBarItem("A_yuda", new MenuItem[]
                {
                    new MenuItem("_Acerca de", "", OnAcercaDe)
                })
            }
        };
        Add(menuBar);
    }

    private void InitLayout()
    {
        // Panel de Búsqueda Activa
        var searchLabel = new Label { Text = "Buscar:", X = 1, Y = 1 };
        _searchField = new TextField { X = Pos.Right(searchLabel) + 1, Y = 1, Width = Dim.Fill(2) };
        _searchField.TextChanged += (sender, e) => ApplyFilter();

        Add(searchLabel, _searchField);

        // Contenedor de división izquierda (Lista) y derecha (Detalles)
        var mainContainer = new View { X = 0, Y = 3, Width = Dim.Fill(), Height = Dim.Fill(1) };

        var leftFrame = new FrameView(" Contactos ")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(45),
            Height = Dim.Fill()
        };

        _listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            AllowMarking = false
        };
        _listView.SelectedItemChanged += (sender, e) => UpdateDetailsPanel();
        leftFrame.Add(_listView);

        var rightFrame = new FrameView(" Detalles del Contacto ")
        {
            X = Pos.Right(leftFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _detailsTextView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true
        };
        rightFrame.Add(_detailsTextView);

        mainContainer.Add(leftFrame, rightFrame);
        Add(mainContainer);

        // Barra de estado inferior
        _statusBar = new StatusBar(new StatusItem[]
        {
            new(Key.Empty, "Listo", null)
        });
        Add(_statusBar);
    }

    private void LoadContacts()
    {
        try
        {
            _allContacts = _store.GetAll().OrderBy(c => c.Nombre).ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error de Base de Datos", $"No se pudieron cargar los datos: {ex.Message}", "Aceptar");
        }
    }

    private void ApplyFilter()
    {
        string query = _searchField.Text.Trim().ToLower();

        _filteredContacts = _allContacts.Where(c =>
        {
            bool matchesFav = !_showOnlyFavorites || c.Favorito;
            bool matchesSearch = string.IsNullOrEmpty(query) ||
                                 c.Nombre.ToLower().Contains(query) ||
                                 c.Telefonos.ToLower().Contains(query) ||
                                 c.Email.ToLower().Contains(query);
            return matchesFav && matchesSearch;
        }).ToList();

        _listView.SetSource(_filteredContacts.Select(c => $"{(c.Favorito ? "[★]" : "   ")} {c.Nombre}").ToList());
        UpdateDetailsPanel();
    }

    private void UpdateDetailsPanel()
    {
        if (_listView.SelectedItem >= 0 && _listView.SelectedItem < _filteredContacts.Count)
        {
            var c = _filteredContacts[_listView.SelectedItem];
            var phones = string.IsNullOrEmpty(c.Telefonos) 
                ? "Ninguno" 
                : string.Join("\n    - ", c.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

            _detailsTextView.Text = $"Nombre:    {c.Nombre}\n" +
                                    $"Favorito:  {(c.Favorito ? "Sí" : "No")}\n" +
                                    $"Email:     {(string.IsNullOrEmpty(c.Email) ? "No asignado" : c.Email)}\n" +
                                    $"Teléfonos:\n    - {phones}\n\n" +
                                    $"Notas:\n{c.Notas}";
        }
        else
        {
            _detailsTextView.Text = "Ningún contacto seleccionado.";
        }
    }

    private void UpdateStatus(string message)
    {
        _statusBar.Subviews[0].Text = message;
        _statusBar.SetNeedsDisplay();
    }

    // Handlers de Eventos de Menú y Atajos
    private void OnNuevoContacto()
    {
        var nuevo = new Contacto();
        if (ContactDialog.ShowDialog("Nuevo Contacto", nuevo))
        {
            try
            {
                _store.Insert(nuevo);
                LoadContacts();
                UpdateStatus($"Contacto '{nuevo.Nombre}' creado exitosamente.");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error al guardar", ex.Message, "Aceptar");
            }
        }
    }

    private void OnEditarContacto()
    {
        if (_listView.SelectedItem < 0 || _listView.SelectedItem >= _filteredContacts.Count) return;

        var original = _filteredContacts[_listView.SelectedItem];
        var clon = original.Clone();

        if (ContactDialog.ShowDialog("Editar Contacto", clon))
        {
            try
            {
                _store.Update(clon);
                LoadContacts();
                UpdateStatus($"Contacto '{clon.Nombre}' modificado.");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error al actualizar", ex.Message, "Aceptar");
            }
        }
    }

    private void OnEliminarContacto()
    {
        if (_listView.SelectedItem < 0 || _listView.SelectedItem >= _filteredContacts.Count) return;

        var seleccionado = _filteredContacts[_listView.SelectedItem];
        int resultado = MessageBox.Query("Confirmar Eliminación", $"¿Está seguro de eliminar a '{seleccionado.Nombre}'?", "Sí", "No");
        
        if (resultado == 0) // "Sí"
        {
            try
            {
                _store.Delete(seleccionado);
                LoadContacts();
                UpdateStatus($"Contacto '{seleccionado.Nombre}' eliminado.");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error al eliminar", ex.Message, "Aceptar");
            }
        }
    }

    private void OnToggleFavoritos()
    {
        _showOnlyFavorites = !_showOnlyFavorites;
        // Forzar actualización del check en el menú visual
        if (Application.Top.Subviews.FirstOrDefault(v => v is MenuBar) is MenuBar menuBar)
        {
            var verMenu = menuBar.Menus.FirstOrDefault(m => m.Title == "_Ver");
            if (verMenu?.Children[0] is MenuItem item) item.Checked = _showOnlyFavorites;
        }
        ApplyFilter();
    }

    private void OnImportarJson()
    {
        var d = new FileDialog { Title = "Importar Agenda JSON", AllowedTypes = new() { ".json" }, OpenMode = FileDialog.OpenModes.File };
        Application.Run(d);

        if (d.Canceled) return;
        string ruta = d.Path;

        if (!File.Exists(ruta))
        {
            MessageBox.ErrorQuery("Archivo No Encontrado", $"El archivo '{ruta}' no existe.", "Aceptar");
            return;
        }

        try
        {
            var importados = JsonAgendaIO.Importar(ruta);
            int confirmacion = MessageBox.Query("Confirmar Importación", $"Se encontraron {importados.Count} contactos.\n¿Desea agregarlos a la base activa?", "Sí", "No");
            
            if (confirmacion == 0)
            {
                _store.InsertRange(importados);
                LoadContacts();
                UpdateStatus($"Se importaron {importados.Count} contactos desde JSON.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error de Importación", $"Estructura o parseo JSON inválido:\n{ex.Message}", "Aceptar");
        }
    }

    private void OnExportarJson()
    {
        var d = new FileDialog { Title = "Exportar Agenda a JSON", AllowedTypes = new() { ".json" }, OpenMode = FileDialog.OpenModes.File };
        Application.Run(d);

        if (d.Canceled) return;
        string ruta = d.Path;

        try
        {
            JsonAgendaIO.Exportar(ruta, _allContacts);
            UpdateStatus($"Archivo exportado con éxito a: {Path.GetFileName(ruta)}");
            MessageBox.Query("Exportación Exitosa", "Los datos se guardaron correctamente de forma legible.", "Aceptar");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error de Exportación", $"No se pudo escribir el archivo:\n{ex.Message}", "Aceptar");
        }
    }

    private void OnAcercaDe()
    {
        MessageBox.Query("Acerca de Agenda TUI", "Agenda de Contactos Corporativa Avanzada\nPersistencia: SQLite (Dapper)\nInterfaz: Terminal.Gui v2\nAño: 2026", "Cerrar");
    }

    private void OnSalir()
    {
        Application.RequestStop();
    }
}

#endregion

#region 3. DIÁLOGO DE EDICIÓN (ContactDialog)

public static class ContactDialog
{
    public static bool ShowDialog(string title, Contacto contacto)
    {
        bool guardado = false;

        // Creación del diálogo adaptativo centralizado
        var dialog = new Dialog
        {
            Title = $" {title} ",
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Percent(75),
            Height = Dim.Percent(80),
            ColorScheme = Colors.ColorSchemes["Dialog"]
        };

        // Layout de campos utilizando coordenadas calculadas consecutivas
        var lblNombre = new Label { Text = "Nombre (*):", X = 2, Y = 1 };
        var txtNombre = new TextField { Text = contacto.Nombre, X = 16, Y = 1, Width = Dim.Fill(2) };

        var lblEmail = new Label { Text = "Email:", X = 2, Y = 3 };
        var txtEmail = new TextField { Text = contacto.Email, X = 16, Y = 3, Width = Dim.Fill(2) };

        var lblFav = new Label { Text = "Favorito:", X = 2, Y = 5 };
        var chkFav = new CheckBox { Checked = contacto.Favorito, X = 16, Y = 5 };

        // Sección de Teléfonos Desglosados en 5 campos independientes de entrada limpia
        var lblTels = new Label { Text = "Telefonos (max 5):", X = 2, Y = 7 };
        var phoneFields = new TextField[5];
        
        string[] currentPhones = contacto.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < 5; i++)
        {
            phoneFields[i] = new TextField
            {
                Text = i < currentPhones.Length ? currentPhones[i] : "",
                X = 22,
                Y = 7 + i,
                Width = Dim.Fill(2)
            };
            dialog.Add(phoneFields[i]);
        }

        var lblNotas = new Label { Text = "Notas:", X = 2, Y = 13 };
        var txtNotas = new TextView
        {
            Text = contacto.Notas,
            X = 16,
            Y = 13,
            Width = Dim.Fill(2),
            Height = Dim.Fill(4) // Espacio para botones inferiores
        };

        dialog.Add(lblNombre, txtNombre, lblEmail, txtEmail, lblFav, chkFav, lblTels, lblNotas, txtNotas);

        // Botones de acción coordinada
        var btnGuardar = new Button { Text = "Guardar", IsDefault = true };
        var btnCancelar = new Button { Text = "Cancelar" };

        btnGuardar.Accept += (sender, e) =>
        {
            // Validaciones obligatorias de negocio
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.ErrorQuery("Validación fallida", "El campo 'Nombre' es obligatorio.", "Modificar");
                txtNombre.SetFocus();
                return;
            }

            string email = txtEmail.Text.Trim();
            if (!string.IsNullOrEmpty(email) && !email.Contains('@'))
            {
                MessageBox.ErrorQuery("Validación fallida", "El formato del Email es inválido (Debe contener '@').", "Modificar");
                txtEmail.SetFocus();
                return;
            }

            // Procesar los 5 inputs sanitizando comas internas residuales
            var listaTels = phoneFields
                .Select(tf => tf.Text.Trim().Replace(",", ""))
                .Where(t => !string.IsNullOrEmpty(t));

            // Seteo del objeto mutado en memoria
            contacto.Nombre = nombre;
            contacto.Email = email;
            contacto.Favorito = chkFav.Checked;
            contacto.Telefonos = string.Join(",", listaTels);
            contacto.Notas = txtNotas.Text;

            guardado = true;
            Application.RequestStop();
        };

        btnCancelar.Accept += (sender, e) =>
        {
            Application.RequestStop();
        };

        dialog.AddButton(btnGuardar);
        dialog.AddButton(btnCancelar);

        Application.Run(dialog);
        return guardado;
    }
}

#endregion

#region 4. PERSISTENCIA (SqliteAgendaStore)

public sealed class SqliteAgendaStore
{
    public string DbPath { get; }
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath)) throw new ArgumentException("Ruta inválida.", nameof(dbPath));
        DbPath = dbPath;
        _connectionString = $"Data Source={dbPath}";
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        const string query = @"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NULL,
                Email TEXT NULL,
                Notas TEXT NULL,
                Favorito INTEGER NOT NULL DEFAULT 0
            );";
        
        connection.Execute(query);
    }

    private SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public IEnumerable<Contacto> GetAll()
    {
        using var conn = CreateConnection();
        return conn.GetAll<Contacto>();
    }

    public void Insert(Contacto contacto)
    {
        using var conn = CreateConnection();
        int id = (int)conn.Insert(contacto);
        contacto.Id = id;
    }

    public void InsertRange(IEnumerable<Contacto> contactos)
    {
        using var conn = CreateConnection();
        using var transaction = conn.BeginTransaction();
        try
        {
            foreach (var c in contactos)
            {
                c.Id = 0; // Garantiza autogeneración limpia y descarte de IDs del JSON
                c.Id = (int)conn.Insert(c, transaction);
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Update(Contacto contacto)
    {
        using var conn = CreateConnection();
        conn.Update(contacto);
    }

    public void Delete(Contacto contacto)
    {
        using var conn = CreateConnection();
        conn.Delete(contacto);
    }
}

#endregion

#region 5. INTEROPERABILIDAD JSON (JsonAgendaIO)

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        // Codificador no destructivo para la preservación nativa de eñes, tildes y símbolos en español
        Encoder = MyJavaScriptEncoder.CreateUnsafeRelaxedJsonEscaping()
    };

    public static List<Contacto> Importar(string path)
    {
        string rawJson = File.ReadAllText(path);
        var lista = JsonSerializer.Deserialize<List<Contacto>>(rawJson, Options);
        return lista ?? new List<Contacto>();
    }

    public static void Exportar(string path, List<Contacto> contactos)
    {
        // Se exporta la lista limpia omitiendo la dependencia del ID secuencial previo si es necesario
        string rawJson = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, rawJson);
    }

    // Adaptador de codificación personalizado heredado seguro
    private class MyJavaScriptEncoder : JavaScriptEncoder
    {
        public static JavaScriptEncoder CreateUnsafeRelaxedJsonEscaping()
        {
            return JavaScriptEncoder.Create(UnicodeRanges.All);
        }
        public override int MaxOutputCharsPerInputChar => throw new NotImplementedException();
        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten) => throw new NotImplementedException();
        public override bool WillEncode(int unicodeScalar) => throw new NotImplementedException();
    }
}

#endregion

#region 6. MODELO DE DATOS (Contacto)

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