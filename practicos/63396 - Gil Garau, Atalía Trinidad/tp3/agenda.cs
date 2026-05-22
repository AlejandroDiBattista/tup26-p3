#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.ObjectModel;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(args.FirstOrDefault() ?? "agenda.db"));

public sealed class AgendaWindow : Runnable
{
    readonly SqliteAgendaStore store;
    readonly List<Contacto> contacts = [];
    List<Contacto> filtered = [];
    readonly TextField search = new();
    readonly ListView list = new();
    readonly TextView detail = new();
    readonly Label status = new();
    readonly MenuItem favItem;
    bool onlyFav;

    public AgendaWindow(string dbPath)
    {
        Title = "Agenda";
        Width = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;

        store = new SqliteAgendaStore(dbPath);
        try { store.Init(); contacts.AddRange(store.GetAll()); }
        catch (Exception ex) { MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK"); }

        favItem = new MenuItem("Solo favoritos", "", ToggleFav);
        Build();
        Refresh();
        SetStatus("Listo.");
    }

    void Build()
    {
        Add(new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_Archivo", [new MenuItem("_Importar JSON", "", ImportJson, Key.I.WithCtrl), new MenuItem("_Exportar JSON", "", ExportJson, Key.E.WithCtrl), null!, new MenuItem("_Salir", "Ctrl+Q", Exit, Key.Q.WithCtrl)]),
                new MenuBarItem("_Contactos", [new MenuItem("_Nuevo", "Ctrl+N", New, Key.F2), new MenuItem("_Editar", "Enter", Edit, Key.F3), new MenuItem("_Eliminar", "Ctrl+D", Delete, Key.Delete)]),
                new MenuBarItem("_Ver", [favItem]),
                new MenuBarItem("_Ayuda", [new MenuItem("_Acerca de", "", About)])
            ]
        });

        Add(new Label { Text = "Buscar:", X = 1, Y = 2 }, search);
        search.X = 10; search.Y = 2; search.Width = Dim.Fill(2); search.TextChanged += (_, _) => Refresh();

        var left = new FrameView { Title = "Contactos", X = 0, Y = 4, Width = Dim.Percent(40), Height = Dim.Fill(2) };
        list.Width = Dim.Fill(); list.Height = Dim.Fill(); list.ValueChanged += (_, _) => ShowDetail(); left.Add(list);

        var right = new FrameView { Title = "Detalle", X = Pos.Right(left), Y = 4, Width = Dim.Fill(), Height = Dim.Fill(2) };
        detail.ReadOnly = true; detail.WordWrap = true; detail.X = 1; detail.Y = 1; detail.Width = Dim.Fill(2); detail.Height = Dim.Fill(2); right.Add(detail);

        status.X = 1; status.Y = Pos.Bottom(left) + 1; status.Width = Dim.Fill(2);
        Add(left, right, status);
    }

    void New() { var d = new ContactDialog(); App!.Run(d); if (d.Ok && d.Contact is not null) { var c = store.Insert(d.Contact); contacts.Add(c); Refresh(); SetStatus($"Agregado: {c.Nombre}"); } }

    void Edit()
    {
        var c = Current(); if (c is null) { MessageBox.Query(App!, "Agenda", "Selecciona un contacto.", "OK"); return; }
        var d = new ContactDialog(c); App!.Run(d); if (!d.Ok || d.Contact is null) return;
        store.Update(d.Contact); var i = contacts.FindIndex(x => x.Id == d.Contact.Id); if (i >= 0) contacts[i] = d.Contact;
        Refresh(); SetStatus($"Actualizado: {d.Contact.Nombre}");
    }

    void Delete()
    {
        var c = Current(); if (c is null) { MessageBox.Query(App!, "Agenda", "Selecciona un contacto.", "OK"); return; }
        if (MessageBox.Query(App!, "Eliminar", $"Eliminar a {c.Nombre}?", "Cancelar", "Eliminar") != 1) return;
        store.Delete(c.Id); contacts.RemoveAll(x => x.Id == c.Id); Refresh(); SetStatus($"Eliminado: {c.Nombre}");
    }

    void ImportJson()
    {
        var path = AskPath("Importar JSON", "agenda.json"); if (path is null) return;
        try
        {
            var items = JsonAgendaIO.Read(path);
            if (MessageBox.Query(App!, "Importar", $"Se agregaran {items.Count} contactos.\n¿Continuar?", "Cancelar", "Importar") != 1) return;
            foreach (var c in items) { var x = c.Clone(); x.Id = 0; contacts.Add(store.Insert(x)); }
            Refresh(); SetStatus($"Importados: {items.Count}");
        }
        catch (Exception ex) { MessageBox.ErrorQuery(App!, "Importar JSON", ex.Message, "OK"); }
    }

    void ExportJson()
    {
        var path = AskPath("Exportar JSON", "salida.json"); if (path is null) return;
        try { JsonAgendaIO.Write(path, store.GetAll()); SetStatus("Exportado."); }
        catch (Exception ex) { MessageBox.ErrorQuery(App!, "Exportar JSON", ex.Message, "OK"); }
    }

    void ToggleFav() { onlyFav = !onlyFav; favItem.Title = onlyFav ? "Solo favoritos ✓" : "Solo favoritos"; Refresh(); }
    void About() => MessageBox.Query(App!, "Acerca de", "Agenda TUI básica con SQLite, Dapper y JSON.", "OK");
    void Exit() => App!.RequestStop();

    string? AskPath(string title, string initial)
    {
        var d = new Dialog { Title = title, Width = 58, Height = 8 };
        var input = new TextField { Text = initial, X = 2, Y = 1, Width = Dim.Fill(2) };
        d.Add(new Label { Text = "Archivo:", X = 2, Y = 0 }, input);
        string result = "";
        var ok = new Button { Text = "_OK", IsDefault = true };
        ok.Accepting += (_, e) => { result = input.Text?.ToString()?.Trim() ?? ""; e.Handled = true; d.RequestStop(); };
        var cancel = new Button { Text = "_Cancelar" };
        cancel.Accepting += (_, e) => { e.Handled = true; d.RequestStop(); };
        d.AddButton(ok); d.AddButton(cancel); App!.Run(d);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    void Refresh()
    {
        var q = search.Text?.ToString()?.Trim() ?? "";
        filtered = contacts.Where(c => (!onlyFav || c.Favorito) && (q == "" || c.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Telefonos.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Email.Contains(q, StringComparison.OrdinalIgnoreCase))).OrderBy(c => c.Nombre, StringComparer.OrdinalIgnoreCase).ToList();
        list.SetSource(new ObservableCollection<string>(filtered.Select(c => $"{(c.Favorito ? "★" : " ")} {c.Nombre}")));
        if (filtered.Count > 0) list.SelectedItem = 0;
        ShowDetail();
    }

    Contacto? Current() => (list.SelectedItem is int i && i >= 0 && i < filtered.Count) ? filtered[i] : null;

    void ShowDetail()
    {
        var c = Current();
        detail.Text = c is null ? "Sin contacto seleccionado." : $"Nombre: {c.Nombre}\nTelefono: {c.Telefonos}\nEmail: {c.Email}\nNotas:\n{c.Notas}\nFavorito: {(c.Favorito ? "Si" : "No")}";
    }

    void SetStatus(string text) => status.Text = text;

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Q.WithCtrl) { Exit(); return true; }
        if (key == Key.F4) { search.SetFocus(); return true; }
        if (key == Key.F2 || key == Key.N.WithCtrl) { New(); return true; }
        if (key == Key.F3 || key == Key.Enter) { Edit(); return true; }
        if (key == Key.Delete || key == Key.D.WithCtrl) { Delete(); return true; }
        if (key == Key.I.WithCtrl) { ImportJson(); return true; }
        if (key == Key.E.WithCtrl) { ExportJson(); return true; }
        return base.OnKeyDown(key);
    }
}

public sealed class ContactDialog : Dialog
{
    readonly TextField nombre = new(), email = new();
    readonly TextField tel = new();
    readonly TextView notas = new();
    readonly CheckBox fav = new();
    public bool Ok { get; private set; }
    public Contacto? Contact { get; private set; }

    public ContactDialog() : this(null) { }

    public ContactDialog(Contacto? c)
    {
        Title = c is null ? "Nuevo contacto" : "Editar contacto";
        Width = 74; Height = 18;
        AddRow("Nombre:", 1, nombre); AddRow("Telefono:", 2, tel); AddRow("Email:", 3, email);
        Add(new Label { Text = "Notas:", X = 2, Y = 4 }); notas.X = 12; notas.Y = 4; notas.Width = Dim.Fill(2); notas.Height = 3; Add(notas);
        fav.Text = "Favorito"; fav.X = 12; fav.Y = 8; Add(fav);
        if (c is not null) { Contact = c.Clone(); nombre.Text = c.Nombre; email.Text = c.Email; notas.Text = c.Notas; fav.Value = c.Favorito ? CheckState.Checked : CheckState.UnChecked; tel.Text = c.Telefonos ?? ""; }
        var save = new Button { Text = "_Guardar", IsDefault = true }; save.Accepting += (_, e) => { Save(); e.Handled = true; };
        var cancel = new Button { Text = "_Cancelar" }; cancel.Accepting += (_, e) => { e.Handled = true; RequestStop(); };
        AddButton(save); AddButton(cancel);
        KeyDown += (_, key) => { if (key == Key.Q.WithCtrl) RequestStop(); };
    }

    void AddRow(string label, int row, TextField field) { Add(new Label { Text = label, X = 2, Y = row }, field); field.X = 12; field.Y = row; field.Width = Dim.Fill(2); }

    void Save()
    {
        var n = nombre.Text?.ToString()?.Trim() ?? "";
        var e = email.Text?.ToString()?.Trim() ?? "";
        if (n == "") { MessageBox.ErrorQuery(App!, "Error", "El nombre no puede estar vacío.", "OK"); return; }
        if (e != "" && !e.Contains('@')) { MessageBox.ErrorQuery(App!, "Error", "El email debe contener @.", "OK"); return; }
        Contact ??= new Contacto();
        Contact.Nombre = n;
        Contact.Telefonos = tel.Text?.ToString()?.Trim() ?? "";
        Contact.Email = e;
        Contact.Notas = notas.Text?.ToString() ?? "";
        Contact.Favorito = fav.Value == CheckState.Checked;
        Ok = true;
        RequestStop();
    }
}

public sealed class SqliteAgendaStore
{
    readonly string cs;
    public SqliteAgendaStore(string dbPath) => cs = $"Data Source={dbPath}";
    SqliteConnection Open() { var c = new SqliteConnection(cs); c.Open(); return c; }
    public void Init() { using var db = Open(); db.Execute("""CREATE TABLE IF NOT EXISTS Contactos (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nombre TEXT NOT NULL, Telefonos TEXT NOT NULL DEFAULT '', Email TEXT NOT NULL DEFAULT '', Notas TEXT NOT NULL DEFAULT '', Favorito INTEGER NOT NULL DEFAULT 0);"""); }
    public List<Contacto> GetAll() { using var db = Open(); return db.GetAll<Contacto>().OrderBy(c => c.Nombre, StringComparer.OrdinalIgnoreCase).ToList(); }
    public Contacto Insert(Contacto c) { using var db = Open(); c.Id = (int)db.Insert(c); return c; }
    public void Update(Contacto c) { using var db = Open(); db.Update(c); }
    public void Delete(int id) { using var db = Open(); db.Delete(new Contacto { Id = id }); }
}

public static class JsonAgendaIO
{
    static readonly JsonSerializerOptions Opt = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static List<Contacto> Read(string path) => File.Exists(path) ? JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(path, Encoding.UTF8), Opt) ?? [] : throw new FileNotFoundException($"No existe el archivo: {path}");
    public static void Write(string path, IEnumerable<Contacto> items) => File.WriteAllText(path, JsonSerializer.Serialize(items, Opt), Encoding.UTF8);
}

[Table("Contactos")]
public sealed class Contacto
{
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }
    public Contacto Clone() => (Contacto)MemberwiseClone();
}