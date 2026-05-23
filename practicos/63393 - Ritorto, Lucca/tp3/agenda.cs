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
using System.Collections.ObjectModel;
using System.Text.Json;

// ── Punto de entrada ───────────────────────────────────────────────────────────
Console.OutputEncoding = System.Text.Encoding.UTF8;
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow());

// ── Ventana principal ─────────────────────────────────────────────────────────
public sealed class AgendaWindow : Runnable
{
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _contacts       = new();
    private List<Contacto> _filteredContacts = new();
    private bool _soloFavoritos = false;

    private TextField _searchField = null!;
    private ListView  _listView    = null!;
    private TextView  _detailView  = null!;

    public AgendaWindow()
    {
        Title  = "AgendaT — TP3";
        Width  = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;

        try { _store = new SqliteAgendaStore("agenda.db"); }
        catch (Exception ex)
        {
            // No podemos usar MessageBox aún (la app no corrió), lanzamos directo
            throw new Exception($"No se pudo abrir la BD: {ex.Message}", ex);
        }

        BuildLayout();
        LoadContacts();
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJSON),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJSON),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir),
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo",    "F2",  NuevoContacto),
                    new MenuItem("_Editar",   "F3",  EditarContacto),
                    new MenuItem("_Eliminar", "Del", EliminarContacto),
                ]),
                new MenuBarItem("_Ver",
                [
                    new MenuItem("_Solo favoritos", "", ToggleFavoritos),
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", "", AcercaDe),
                ]),
            ]
        };

        Label searchLabel = new() { Text = "Buscar: ", X = 0, Y = 1 };
        _searchField = new TextField
        {
            X = Pos.Right(searchLabel), Y = 1, Width = Dim.Fill(),
        };
        _searchField.TextChanged += (_, _) => ApplyFilter();

        FrameView listFrame = new()
        {
            Title = "Contactos", X = 0, Y = 3,
            Width = Dim.Percent(40), Height = Dim.Fill(1),
        };

        _listView = new ListView
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(),
        };
        _listView.SelectedItemChanged += (_, _) => UpdateDetail();
        _listView.OpenSelectedItem    += (_, _) => EditarContacto();
        listFrame.Add(_listView);

        FrameView detailFrame = new()
        {
            Title = "Detalle", X = Pos.Right(listFrame), Y = 3,
            Width = Dim.Fill(), Height = Dim.Fill(1),
        };
        _detailView = new TextView
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true,
        };
        detailFrame.Add(_detailView);

        Add(menu, searchLabel, _searchField, listFrame, detailFrame);
    }

    // ── Teclado ───────────────────────────────────────────────────────────────
    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.F4)               { _searchField.SetFocus(); return true; }
        if (key == Key.Q.WithCtrl)       { SolicitarSalir();  return true; }
        if (key == Key.I.WithCtrl)       { ImportarJSON();    return true; }
        if (key == Key.E.WithCtrl)       { ExportarJSON();    return true; }
        if (key == Key.F2)               { NuevoContacto();   return true; }
        if (key == Key.F3)               { EditarContacto();  return true; }
        if (key == Key.Delete)           { EliminarContacto(); return true; }
        return base.OnKeyDown(key);
    }

    // ── Datos ─────────────────────────────────────────────────────────────────
    private void LoadContacts()
    {
        try   { _contacts = _store.GetAll().ToList(); ApplyFilter(); }
        catch (Exception ex)
        { MessageBox.ErrorQuery(App!, "Error", $"No se pudo cargar contactos:\n{ex.Message}", "Aceptar"); }
    }

    private void ApplyFilter()
    {
        var q = _searchField?.Text?.ToString()?.Trim() ?? "";
        _filteredContacts = _contacts
            .Where(c => !_soloFavoritos || c.Favorito)
            .Where(c => string.IsNullOrEmpty(q)
                     || c.Nombre.Contains(q,    StringComparison.OrdinalIgnoreCase)
                     || c.Telefonos.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || c.Email.Contains(q,     StringComparison.OrdinalIgnoreCase))
            .ToList();

        _listView.SetSource(new ObservableCollection<string>(
            _filteredContacts.Select(c => $"{(c.Favorito ? "★ " : "  ")}{c.Nombre}")));
        UpdateDetail();
    }

    private void UpdateDetail()
    {
        var c = SelectedContact();
        if (c == null) { _detailView.Text = ""; return; }
        _detailView.Text =
            $"Nombre   : {c.Nombre}\n"    +
            $"Teléfonos: {c.Telefonos}\n" +
            $"Email    : {c.Email}\n"     +
            $"Favorito : {(c.Favorito ? "Sí" : "No")}\n\nNotas:\n{c.Notas}";
    }

    private Contacto? SelectedContact()
    {
        int idx = _listView.SelectedItem ?? -1;
        return (idx < 0 || idx >= _filteredContacts.Count) ? null : _filteredContacts[idx];
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────
    private void NuevoContacto()
    {
        var dlg = new ContactDialog("Nuevo contacto", new Contacto());
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;
        try
        {
            var c = dlg.ContactResult!;
            _store.Insert(c); _contacts.Add(c); ApplyFilter();
            int idx = _filteredContacts.IndexOf(c);
            if (idx >= 0) _listView.SelectedItem = idx;
        }
        catch (Exception ex)
        { MessageBox.ErrorQuery(App!, "Error", $"No se pudo crear el contacto:\n{ex.Message}", "Aceptar"); }
    }

    private void EditarContacto()
    {
        var c = SelectedContact(); if (c == null) return;
        var dlg = new ContactDialog("Editar contacto", c.Clone());
        App!.Run(dlg);
        if (!dlg.WasAccepted) return;
        try
        {
            var updated = dlg.ContactResult!; updated.Id = c.Id;
            _store.Update(updated);
            int idx = _contacts.IndexOf(c); if (idx >= 0) _contacts[idx] = updated;
            ApplyFilter();
        }
        catch (Exception ex)
        { MessageBox.ErrorQuery(App!, "Error", $"No se pudo actualizar:\n{ex.Message}", "Aceptar"); }
    }

    private void EliminarContacto()
    {
        var c = SelectedContact(); if (c == null) return;
        if (MessageBox.Query(App!, "Confirmar", $"¿Eliminar a '{c.Nombre}'?", "Sí", "No") != 0) return;
        try   { _store.Delete(c); _contacts.Remove(c); ApplyFilter(); }
        catch (Exception ex)
        { MessageBox.ErrorQuery(App!, "Error", $"No se pudo eliminar:\n{ex.Message}", "Aceptar"); }
    }

    // ── JSON ──────────────────────────────────────────────────────────────────
    private void ImportarJSON()
    {
        string? path = PedirRuta("Importar JSON", "");
        if (path == null) return;
        if (!File.Exists(path))
        { MessageBox.ErrorQuery(App!, "Error", $"El archivo no existe:\n{path}", "Aceptar"); return; }

        List<Contacto> imported;
        try   { imported = JsonAgendaIO.Read(path); }
        catch (Exception ex)
        { MessageBox.ErrorQuery(App!, "Error", $"JSON inválido:\n{ex.Message}", "Aceptar"); return; }

        if (MessageBox.Query(App!, "Confirmar", $"Se agregarán {imported.Count} contacto(s).\n¿Continuar?", "Sí", "No") != 0) return;
        try   { foreach (var c in imported) { c.Id = 0; _store.Insert(c); _contacts.Add(c); } ApplyFilter(); }
        catch (Exception ex) { MessageBox.ErrorQuery(App!, "Error", $"Error al importar:\n{ex.Message}", "Aceptar"); }
    }

    private void ExportarJSON()
    {
        string? path = PedirRuta("Exportar JSON", "contactos.json");
        if (path == null) return;
        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) path += ".json";
        try
        {
            JsonAgendaIO.Write(path, _contacts);
            MessageBox.Query(App!, "Exportar", $"Exportado a:\n{path}", "Aceptar");
        }
        catch (Exception ex) { MessageBox.ErrorQuery(App!, "Error", $"No se pudo exportar:\n{ex.Message}", "Aceptar"); }
    }

    private string? PedirRuta(string titulo, string valorInicial)
    {
        string? resultado = null;
        var dlg    = new Dialog { Title = titulo, Width = 70, Height = 8 };
        var lbl    = new Label     { Text = "Ruta del archivo .json:", X = 1, Y = 1 };
        var txt    = new TextField { Text = valorInicial, X = 1, Y = 2, Width = Dim.Fill() - 2 };
        var ok     = new Button    { Text = "_Aceptar", IsDefault = true };
        var cancel = new Button    { Text = "_Cancelar" };

        ok.Accepting     += (_, e) => { resultado = txt.Text?.ToString()?.Trim(); App!.RequestStop(); e.Handled = true; };
        cancel.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };

        dlg.Add(lbl, txt); dlg.AddButton(ok); dlg.AddButton(cancel);
        App!.Run(dlg);
        return string.IsNullOrWhiteSpace(resultado) ? null : resultado;
    }

    private void ToggleFavoritos() { _soloFavoritos = !_soloFavoritos; ApplyFilter(); }

    private void AcercaDe() =>
        MessageBox.Query(App!, "Acerca de",
            "AgendaT — TP3\nAplicación de agenda TUI\n.NET 10 · Terminal.Gui v2 · SQLite · Dapper",
            "Aceptar");

    private void SolicitarSalir() => App!.RequestStop();
}

// ── Diálogo de contacto ───────────────────────────────────────────────────────
public sealed class ContactDialog : Dialog
{
    public bool      WasAccepted   { get; private set; }
    public Contacto? ContactResult { get; private set; }

    private readonly TextField _nombre;
    private readonly TextField _tel1, _tel2, _tel3, _tel4, _tel5;
    private readonly TextField _email;
    private readonly TextView  _notas;
    private readonly CheckBox  _favorito;

    public ContactDialog(string title, Contacto c)
    {
        Title = title; Width = 75; Height = 28;

        var tels = c.Telefonos.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string T(int i) => i < tels.Length ? tels[i] : "";

        int y = 1;
        Add(Lbl("Nombre *:", 1, y)); _nombre = Fld(c.Nombre, 14, y, 55); Add(_nombre); y += 2;

        Add(Lbl("Tel 1:", 1,  y)); _tel1 = Fld(T(0), 10, y, 18); Add(_tel1);
        Add(Lbl("Tel 2:", 31, y)); _tel2 = Fld(T(1), 40, y, 18); Add(_tel2); y += 2;
        Add(Lbl("Tel 3:", 1,  y)); _tel3 = Fld(T(2), 10, y, 18); Add(_tel3);
        Add(Lbl("Tel 4:", 31, y)); _tel4 = Fld(T(3), 40, y, 18); Add(_tel4); y += 2;
        Add(Lbl("Tel 5:", 1,  y)); _tel5 = Fld(T(4), 10, y, 18); Add(_tel5); y += 2;

        Add(Lbl("Email:", 1, y)); _email = Fld(c.Email, 14, y, 55); Add(_email); y += 2;

        Add(Lbl("Notas:", 1, y)); y++;
        _notas = new TextView { X = 1, Y = y, Width = 70, Height = 3, Text = c.Notas };
        Add(_notas); y += 4;

        _favorito = new CheckBox
        {
            Text       = "★ Favorito",
            CheckState = c.Favorito ? CheckState.Checked : CheckState.UnChecked,
            X = 1, Y = y,
        };
        Add(_favorito);

        var btnOk     = new Button { Text = "_Aceptar", IsDefault = true };
        var btnCancel = new Button { Text = "_Cancelar" };

        btnOk.Accepting     += (_, e) => { if (TryBuildResult()) App!.RequestStop(); e.Handled = true; };
        btnCancel.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };

        AddButton(btnOk);
        AddButton(btnCancel);
    }

    private static Label     Lbl(string t, int x, int y) => new() { Text = t, X = x, Y = y };
    private static TextField Fld(string v, int x, int y, int w) => new() { Text = v, X = x, Y = y, Width = w };

    private bool TryBuildResult()
    {
        var nombre = _nombre.Text?.ToString()?.Trim() ?? "";
        if (string.IsNullOrEmpty(nombre))
        {
            MessageBox.ErrorQuery(App!, "Validación", "El nombre no puede estar vacío.", "Aceptar");
            _nombre.SetFocus(); return false;
        }
        var email = _email.Text?.ToString()?.Trim() ?? "";
        if (!string.IsNullOrEmpty(email) && !email.Contains('@'))
        {
            MessageBox.ErrorQuery(App!, "Validación", "El email debe contener '@'.", "Aceptar");
            _email.SetFocus(); return false;
        }
        var tels = new[]
        {
            _tel1.Text?.ToString()?.Trim() ?? "", _tel2.Text?.ToString()?.Trim() ?? "",
            _tel3.Text?.ToString()?.Trim() ?? "", _tel4.Text?.ToString()?.Trim() ?? "",
            _tel5.Text?.ToString()?.Trim() ?? "",
        };
        ContactResult = new Contacto
        {
            Nombre    = nombre,
            Telefonos = string.Join(", ", tels.Where(t => !string.IsNullOrEmpty(t))),
            Email     = email,
            Notas     = _notas.Text?.ToString() ?? "",
            Favorito  = _favorito.CheckState == CheckState.Checked,
        };
        WasAccepted = true; return true;
    }
}

// ── Persistencia ──────────────────────────────────────────────────────────────
public class SqliteAgendaStore
{
    private readonly string _connString;
    public SqliteAgendaStore(string dbPath) { _connString = $"Data Source={dbPath}"; EnsureSchema(); }

    private SqliteConnection Open() { var c = new SqliteConnection(_connString); c.Open(); return c; }

    private void EnsureSchema()
    {
        using var conn = Open();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS Contactos (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre    TEXT NOT NULL DEFAULT '',
            Telefonos TEXT NOT NULL DEFAULT '',
            Email     TEXT NOT NULL DEFAULT '',
            Notas     TEXT NOT NULL DEFAULT '',
            Favorito  INTEGER NOT NULL DEFAULT 0);");
    }

    public IEnumerable<Contacto> GetAll() { using var c = Open(); return c.GetAll<Contacto>().ToList(); }
    public void Insert(Contacto x) { using var c = Open(); x.Id = (int)c.Insert(x); }
    public void Update(Contacto x) { using var c = Open(); c.Update(x); }
    public void Delete(Contacto x) { using var c = Open(); c.Delete(x); }
}

// ── JSON ──────────────────────────────────────────────────────────────────────
public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    public static List<Contacto> Read(string path) =>
        JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(path), Opts) ?? new();
    public static void Write(string path, IEnumerable<Contacto> contacts) =>
        File.WriteAllText(path, JsonSerializer.Serialize(contacts.ToList(), Opts), System.Text.Encoding.UTF8);
}

// ── Modelo ────────────────────────────────────────────────────────────────────
[Table("Contactos")]
public sealed class Contacto
{
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

    public Contacto Clone() => new()
        { Id = Id, Nombre = Nombre, Telefonos = Telefonos, Email = Email, Notas = Notas, Favorito = Favorito };
}