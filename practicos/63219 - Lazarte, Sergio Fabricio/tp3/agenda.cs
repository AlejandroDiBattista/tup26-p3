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
using System.Collections.ObjectModel;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store;
try
{
    store = new SqliteAgendaStore(dbPath);
    store.EnsureSchema();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al abrir la base de datos '{dbPath}': {ex.Message}");
    Environment.Exit(1);
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));

public sealed class AgendaWindow : Runnable
{
    private readonly SqliteAgendaStore _store;
    private readonly List<Contacto>    _contacts = new();
    private readonly List<Contacto>    _filtered = new();
    private bool _soloFavoritos = false;

    private TextField _searchField  = null!;
    private ListView  _listView     = null!;
    private Label     _detailFav    = null!;
    private Label     _detailNombre = null!;
    private Label     _detailTels   = null!;
    private Label     _detailEmail  = null!;
    private Label     _detailNotas  = null!;

    public AgendaWindow(SqliteAgendaStore store)
    {
        _store = store;
        Title  = $"AgendaT — {Path.GetFileName(store.DbPath)}";
        Width  = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        ReloadFromDb();
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo",    "F2",  NuevoContacto),
                    new MenuItem("_Editar",   "F3",  EditarContacto),
                    new MenuItem("E_liminar", "Del", EliminarContacto)
                ]),
                new MenuBarItem("_Ver",
                [
                    new MenuItem("_Solo favoritos", "", ToggleSoloFavoritos)
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", null!, AcercaDe)
                ])
            ]
        };

        Label searchLabel = new() { Text = "Buscar:", X = 1, Y = 1 };
        _searchField = new TextField { X = Pos.Right(searchLabel) + 1, Y = 1, Width = 35 };
        _searchField.TextChanged += (_, _) => AplicarFiltros();

        FrameView listFrame = new()
        {
            Title = "Contactos", X = 0, Y = 3,
            Width = Dim.Percent(45), Height = Dim.Fill(1)
        };
        _listView = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        listFrame.Add(_listView);

        FrameView detailFrame = new()
        {
            Title = "Detalle", X = Pos.Right(listFrame), Y = 3,
            Width = Dim.Fill(), Height = Dim.Fill(1)
        };
        _detailFav    = new Label { X = 1, Y = 0, Width = Dim.Fill() };
        _detailNombre = new Label { X = 1, Y = 1, Width = Dim.Fill() };
        _detailTels   = new Label { X = 1, Y = 2, Width = Dim.Fill() };
        _detailEmail  = new Label { X = 1, Y = 3, Width = Dim.Fill() };
        Label lblNotas = new() { Text = "Notas:", X = 1, Y = 4 };
        _detailNotas  = new Label { X = 1, Y = 5, Width = Dim.Fill(), Height = Dim.Fill() };
        detailFrame.Add(_detailFav, _detailNombre, _detailTels,
                        _detailEmail, lblNotas, _detailNotas);

        StatusBar statusBar = new();
        statusBar.Add(
            new Shortcut { Key = Key.F2,         Title = "F2 Nuevo",     Action = NuevoContacto },
            new Shortcut { Key = Key.F3,         Title = "F3 Editar",    Action = EditarContacto },
            new Shortcut { Key = Key.Delete,     Title = "Del Eliminar", Action = EliminarContacto },
            new Shortcut { Key = Key.F4,         Title = "F4 Buscar",    Action = () => _searchField.SetFocus() },
            new Shortcut { Key = Key.F5,         Title = "F5 Importar", Action = ImportarJson },
            new Shortcut { Key = Key.Q.WithCtrl, Title = "^Q Salir",     Action = SolicitarSalir }
        );

        Add(menu, searchLabel, _searchField, listFrame, detailFrame, statusBar);
    }

    private void ReloadFromDb()
    {
        _contacts.Clear();
        _contacts.AddRange(_store.GetAll());
        AplicarFiltros();
        SetStatus($"{_contacts.Count} contacto(s) cargados.");
    }

    private void AplicarFiltros()
    {
        string query = (_searchField?.Text ?? "").Trim().ToLowerInvariant();
        _filtered.Clear();
        foreach (var c in _contacts)
        {
            if (_soloFavoritos && !c.Favorito) continue;
            if (query.Length > 0)
            {
                bool match = c.Nombre.Contains(query,    StringComparison.OrdinalIgnoreCase)
                          || c.Telefonos.Contains(query, StringComparison.OrdinalIgnoreCase)
                          || c.Email.Contains(query,     StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
            }
            _filtered.Add(c);
        }
        _listView.SetSource<string>(new ObservableCollection<string>(_filtered.Select(FormatItem)));
        MostrarDetalle();
    }

    private static string FormatItem(Contacto c)
        => (c.Favorito ? "★ " : "  ") + c.Nombre;

    private void MostrarDetalle()
    {
        int idx = _listView.SelectedItem.GetValueOrDefault(-1);
        Contacto? c = (idx >= 0 && idx < _filtered.Count) ? _filtered[idx] : null;
        if (c is null)
        {
            _detailFav.Text = _detailNombre.Text =
            _detailTels.Text = _detailEmail.Text = _detailNotas.Text = "";
            return;
        }
        _detailFav.Text    = c.Favorito ? "★  Favorito" : "";
        _detailNombre.Text = $"Nombre:    {c.Nombre}";
        _detailTels.Text   = $"Teléfonos: {c.Telefonos}";
        _detailEmail.Text  = $"Email:     {c.Email}";
        _detailNotas.Text  = c.Notas;
    }

    private Contacto? ContactoSeleccionado()
    {
        int idx = _listView.SelectedItem.GetValueOrDefault(-1);
        return (idx >= 0 && idx < _filtered.Count) ? _filtered[idx] : null;
    }

    private void SetStatus(string msg)
        => Title = $"AgendaT — {Path.GetFileName(_store.DbPath)}   [{msg}]";

    private void MostrarError(string titulo, string msg)
        => MessageBox.ErrorQuery(App!, titulo, msg, "Aceptar");

    private void MostrarInfo(string titulo, string msg)
        => MessageBox.Query(App!, titulo, msg, "Cerrar");

    private int Preguntar(string titulo, string msg, params string[] botones)
    => (int)(MessageBox.Query(App!, titulo, msg, botones) ?? -1);

    private void AcercaDe()
        => MostrarInfo("Acerca de AgendaT", "AgendaT v1.0\nTP3 — TUI con Terminal.Gui + SQLite");

    private void SolicitarSalir() => App!.RequestStop();

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Q.WithCtrl)                      { SolicitarSalir();        return true; }
        if (key == Key.N.WithCtrl || key == Key.F2)     { NuevoContacto();         return true; }
        if (key == Key.F3)                              { EditarContacto();        return true; }
        if (key == Key.D.WithCtrl || key == Key.Delete) { EliminarContacto();      return true; }
        if (key == Key.I.WithCtrl || key == Key.F5)     { ImportarJson();          return true; }
        if (key == Key.E.WithCtrl)                      { ExportarJson();          return true; }
        if (key == Key.F4)                              { _searchField.SetFocus(); return true; }
        MostrarDetalle();
        return base.OnKeyDown(key);
    }

    // Acciones CRUD — se agregan en el siguiente commit
    
    private void NuevoContacto()
    {
        var dlg = new ContactDialog(App!, "Nuevo contacto", new Contacto());
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;
        try { _store.Insert(dlg.ResultContact!); ReloadFromDb(); SetStatus($"'{dlg.ResultContact!.Nombre}' creado."); }
        catch (Exception ex) { MostrarError("Error al guardar", ex.Message); }
    }

    private void EditarContacto()
    {
        var sel = ContactoSeleccionado();
        if (sel is null) return;
        var dlg = new ContactDialog(App!, "Editar contacto", sel.Clone());
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;
        try { _store.Update(dlg.ResultContact!); ReloadFromDb(); SetStatus($"'{dlg.ResultContact!.Nombre}' actualizado."); }
        catch (Exception ex) { MostrarError("Error al actualizar", ex.Message); }
    }

    private void EliminarContacto()
    {
        var sel = ContactoSeleccionado();
        if (sel is null) return;
        int r = Preguntar("Confirmar", $"¿Eliminar a '{sel.Nombre}'?", "Sí", "No");
        if (r != 0) return;
        try { _store.Delete(sel); ReloadFromDb(); SetStatus($"'{sel.Nombre}' eliminado."); }
        catch (Exception ex) { MostrarError("Error al eliminar", ex.Message); }
    }

    private void ToggleSoloFavoritos()
    {
        _soloFavoritos = !_soloFavoritos;
        AplicarFiltros();
        SetStatus(_soloFavoritos ? "Solo favoritos." : "Todos los contactos.");
    }

    private void ImportarJson()
    {
        string path = PedirRuta("Importar JSON", "Ruta del archivo JSON:", "");
        if (string.IsNullOrEmpty(path)) return;
        List<Contacto> lista;
        try { lista = JsonAgendaIO.Leer(path); }
        catch (Exception ex) { MostrarError("Error al importar", ex.Message); return; }

        int r = Preguntar("Confirmar", $"Se agregarán {lista.Count} contacto(s). ¿Continuar?", "Sí", "No");
        if (r != 0) return;
        try
        {
            foreach (var c in lista) { c.Id = 0; _store.Insert(c); }
            ReloadFromDb(); SetStatus($"{lista.Count} contacto(s) importados.");
        }
        catch (Exception ex) { MostrarError("Error al importar", ex.Message); }
    }

    private void ExportarJson()
    {
        string path = PedirRuta("Exportar JSON", "Ruta de salida:", "salida.json");
        if (string.IsNullOrEmpty(path)) return;
        try { JsonAgendaIO.Escribir(path, _contacts); SetStatus($"Exportados {_contacts.Count} contacto(s) a '{path}'."); }
        catch (Exception ex) { MostrarError("Error al exportar", ex.Message); }
    }

    private string PedirRuta(string title, string label, string defaultVal)
    {
    string result = "";
    bool confirmed = false;

    var dlg = new Dialog()
    {
        Title = title,
        Width = 60,
        Height = 10
    };

    var lbl = new Label()
    {
        Text = label,
        X = 1,
        Y = 1
    };

    var fld = new TextField()
    {
        X = 1,
        Y = 3,
        Width = Dim.Fill(2),
        Text = defaultVal
    };

    var ok = new Button()
    {
        Text = "_Aceptar",
        X = Pos.Center() - 10,
        Y = 5,
        IsDefault = true
    };

    var cancel = new Button()
    {
        Text = "_Cancelar",
        X = Pos.Right(ok) + 2,
        Y = 5
    };

    ok.Accepting += (_, e) =>
    {
        result = fld.Text.ToString() ?? "";
        confirmed = true;

        dlg.App!.RequestStop();
        e.Handled = true;
    };

    cancel.Accepting += (_, e) =>
    {
        dlg.App!.RequestStop();
        e.Handled = true;
    };

    dlg.Add(lbl);
    dlg.Add(fld);
    dlg.Add(ok);
    dlg.Add(cancel);

    fld.SetFocus();

    App!.Run(dlg);

    return confirmed ? result : "";
    }
}

[Table("Contactos")]
public sealed class Contacto
{
    [Key] public int    Id        { get; set; }
         public string  Nombre    { get; set; } = "";
         public string  Telefonos { get; set; } = "";
         public string  Email     { get; set; } = "";
         public string  Notas     { get; set; } = "";
         public bool    Favorito  { get; set; }

    public Contacto Clone() => new()
    {
        Id = Id, Nombre = Nombre, Telefonos = Telefonos,
        Email = Email, Notas = Notas, Favorito = Favorito
    };
}

public sealed class ContactDialog : Dialog
{
    public bool      WasAccepted   { get; private set; }
    public Contacto? ResultContact { get; private set; }

    private readonly IApplication _app;
    private readonly Contacto     _orig;
    private TextField             _nombre = null!;
    private TextField[]           _tels   = null!;
    private TextField             _email  = null!;
    private TextView              _notas  = null!;
    private bool                  _favorito;
    private Button                _favBtn = null!;

    public ContactDialog(IApplication app, string title, Contacto contacto)
    {
        _app      = app;
        _orig     = contacto;
        _favorito = contacto.Favorito;
        Title     = title;
        Width     = 62;
        Height    = 26;
        BuildLayout();
        CargarDatos();
    }

    private void BuildLayout()
    {
        int y = 1;

        Add(new Label { Text = "Nombre (*):", X = 1, Y = y });
        _nombre = new TextField { X = 20, Y = y, Width = Dim.Fill(2) };
        Add(_nombre);
        y += 2;

        Add(new Label { Text = "Teléfonos:", X = 1, Y = y });
        y++;
        _tels = new TextField[5];
        for (int i = 0; i < 5; i++)
        {
            Add(new Label { Text = $"  Tel {i + 1}:", X = 1, Y = y });
            _tels[i] = new TextField { X = 12, Y = y, Width = 32 };
            Add(_tels[i]);
            y++;
        }
        y++;

        Add(new Label { Text = "Email:", X = 1, Y = y });
        _email = new TextField { X = 20, Y = y, Width = Dim.Fill(2) };
        Add(_email);
        y += 2;

        _favBtn = new Button { X = 1, Y = y };
        ActualizarTextoFav();
        _favBtn.Accepting += (_, e) =>
        {
            _favorito = !_favorito;
            ActualizarTextoFav();
            e.Handled = true;
        };
        Add(_favBtn);
        y += 2;

        Add(new Label { Text = "Notas:", X = 1, Y = y });
        y++;
        _notas = new TextView { X = 1, Y = y, Width = Dim.Fill(2), Height = 3 };
        Add(_notas);

        Button guardar  = new() { Text = "_Guardar",  IsDefault = true };
        Button cancelar = new() { Text = "_Cancelar" };
        guardar.Accepting  += (_, e) => { Guardar();  e.Handled = true; };
        cancelar.Accepting += (_, e) => { Cancelar(); e.Handled = true; };
        AddButton(guardar);
        AddButton(cancelar);
    }

    private void ActualizarTextoFav()
        => _favBtn.Text = _favorito ? "★ Favorito [ON] " : "☆ Favorito [OFF]";

    private void CargarDatos()
    {
        _nombre.Text = _orig.Nombre;
        _email.Text  = _orig.Email;
        _notas.Text  = _orig.Notas;

        string[] partes = _orig.Telefonos
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < _tels.Length; i++)
            _tels[i].Text = i < partes.Length ? partes[i] : "";
    }

    private void Guardar()
    {
        string nombre = _nombre.Text.Trim();
        string email  = _email.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.ErrorQuery(_app, "Validación", "El nombre no puede estar vacío.", "Aceptar");
            return;
        }
        if (!string.IsNullOrEmpty(email) && !email.Contains('@'))
        {
            MessageBox.ErrorQuery(_app, "Validación", "El email debe contener '@'.", "Aceptar");
            return;
        }

        string tels = string.Join(", ",
            _tels.Select(t => t.Text.Trim()).Where(t => !string.IsNullOrEmpty(t)));

        ResultContact = new Contacto
        {
            Id        = _orig.Id,
            Nombre    = nombre,
            Telefonos = tels,
            Email     = email,
            Notas     = _notas.Text,
            Favorito  = _favorito
        };
        WasAccepted = true;
        App!.RequestStop();
    }

    private void Cancelar() { WasAccepted = false; App!.RequestStop(); }
}

public sealed class SqliteAgendaStore
{
    public string DbPath { get; }
    public SqliteAgendaStore(string dbPath) => DbPath = dbPath;

    private SqliteConnection Open() => new($"Data Source={DbPath}");

    public void EnsureSchema()
    {
        using var cn = Open();
        cn.Open();
        cn.Execute(@"CREATE TABLE IF NOT EXISTS Contactos (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre    TEXT    NOT NULL DEFAULT '',
            Telefonos TEXT    NOT NULL DEFAULT '',
            Email     TEXT    NOT NULL DEFAULT '',
            Notas     TEXT    NOT NULL DEFAULT '',
            Favorito  INTEGER NOT NULL DEFAULT 0
        );");
    }

    public IEnumerable<Contacto> GetAll()  { using var cn = Open(); return cn.GetAll<Contacto>().ToList(); }
    public void Insert(Contacto c)         { using var cn = Open(); c.Id = (int)cn.Insert(c); }
    public void Update(Contacto c)         { using var cn = Open(); cn.Update(c); }
    public void Delete(Contacto c)         { using var cn = Open(); cn.Delete(c); }
}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<Contacto> Leer(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Archivo no encontrado: '{path}'");
        string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        List<Contacto>? lista;
        try { lista = JsonSerializer.Deserialize<List<Contacto>>(json, Opts); }
        catch (JsonException ex) { throw new FormatException($"JSON inválido: {ex.Message}"); }
        return lista ?? throw new FormatException("El JSON no contiene una lista válida.");
    }

    public static void Escribir(string path, IEnumerable<Contacto> contactos)
        => File.WriteAllText(path, JsonSerializer.Serialize(contactos.ToList(), Opts),
                             System.Text.Encoding.UTF8);
}