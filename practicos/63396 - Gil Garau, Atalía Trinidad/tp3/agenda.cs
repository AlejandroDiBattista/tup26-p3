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
using System.Collections.ObjectModel;
using System.Data.Common;
using Dapper.Contrib.Extensions;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;


/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
var dbPath = args.FirstOrDefault() ?? "agenda.db";
AgendaWindow.DbPath = dbPath;
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow());


// Ventana principal
public sealed class AgendaWindow : Runnable {
    public static string DbPath = "agenda.db";

    readonly SqliteAgendaStore store;
    readonly List<Contacto> contacts = [];
    List<Contacto> filtered = [];
    readonly TextField search = new();
    readonly ListView list = new();
    readonly TextView detail = new();
    readonly Label status = new();
    readonly MenuItem favItem;
    bool onlyFav;

    public AgendaWindow() {
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        store = new SqliteAgendaStore(DbPath);
        try { store.Init(); contacts.AddRange(store.GetAll()); }
        catch (Exception ex) { MessageBox.ErrorQuery(App!, "Base de datos", ex.Message, "OK"); }

        favItem = new MenuItem("Solo favoritos", null, ToggleFav);
        BuildLayout();
        RefreshList();
    }

    private void BuildLayout() {
       MenuBar menu = new()
        {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON", null, ImportJson),
                    null!,
                    new MenuItem("_Exportar JSON", null, ExportJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo contacto", null, AbrirDialogo),
                    null!,
                    new MenuItem("_Editar contacto", null, EditSelected),
                    null!,
                    new MenuItem("_Eliminar contacto", null, DeleteSelected)
                ]),
                new MenuBarItem("_Ver", [favItem]),
                new MenuBarItem("_Ayuda", [new MenuItem("_Acerca de", null, () => MessageBox.Query(App!, "Acerca de", "Agenda TUI simple", "OK"))])
            ]
        };

        Label searchLabel = new() { Text = "Buscar:", X = 1, Y = 2 };
        search.X = 10;
        search.Y = 2;
        search.Width = Dim.Fill(2);
        search.TextChanged += (_, _) => RefreshList();

        FrameView left = new() { Title = "Contactos", X = 0, Y = 4, Width = Dim.Percent(40), Height = Dim.Fill(2) };
        list.Width = Dim.Fill();
        list.Height = Dim.Fill();
        list.ValueChanged += (_, _) => ShowDetail();
        left.Add(list);

        FrameView right = new() { Title = "Detalle", X = Pos.Right(left), Y = 4, Width = Dim.Fill(), Height = Dim.Fill(2) };
        detail.ReadOnly = true;
        detail.WordWrap = true;
        detail.X = 1;
        detail.Y = 1;
        detail.Width = Dim.Fill(2);
        detail.Height = Dim.Fill(2);
        right.Add(detail);

        Button openButton = new() { Text = "_Abrir diálogo", X = Pos.Center(), Y = Pos.Center() };
        openButton.Accepting += (_, e) => { AbrirDialogo(); e.Handled = true; };

        status.Text = "F2 nuevo | F3 editar | Del borrar | Ctrl+I importar | Ctrl+E exportar | Ctrl+Q salir";
        status.X = 1;
        status.Y = Pos.AnchorEnd(1);
        status.Width = Dim.Fill();

        Add(menu, searchLabel, search, left, right, status, openButton);
    }

    private void AbrirDialogo() {
        EjemploDialog dialog = new();
        App!.Run(dialog);
        if (!dialog.Ok) return;
        contacts.Add(store.Insert(dialog.Result));
        status.Text = "Contacto creado.";
        RefreshList();
    }
    private void EditSelected()
    {
        var c = Current();
        if (c is null) { MessageBox.Query(App!, "Agenda", "Selecciona un contacto.", "OK"); return; }
        EjemploDialog dialog = new("Editar contacto", c.Clone());
        App!.Run(dialog);
        if (!dialog.Ok) return;
        store.Update(dialog.Result);
        contacts[contacts.FindIndex(x => x.Id == c.Id)] = dialog.Result;
        status.Text = "Contacto actualizado.";
        RefreshList();
    }
    private void DeleteSelected()
    {
        var c = Current();
        if (c is null) { MessageBox.Query(App!, "Agenda", "Selecciona un contacto.", "OK"); return; }
        if (MessageBox.Query(App!, "Eliminar", $"Eliminar a {c.Nombre}?", "Cancelar", "Eliminar") != 1) return;
        store.Delete(c.Id);
        contacts.RemoveAll(x => x.Id == c.Id);
        status.Text = "Contacto eliminado.";
        RefreshList();
    }
    private void ToggleFav() { onlyFav = !onlyFav; favItem.Title = onlyFav ? "Solo favoritos ✓" : "Solo favoritos"; RefreshList(); }
    private void ImportJson()
    {
        var path = AskPath("Importar JSON", "agenda.json");
        if (path is null) return;
        try
        {
            var listToAdd = JsonAgendaIO.Read(path);
            if (MessageBox.Query(App!, "Importar", $"Se agregaran {listToAdd.Count} contactos.\nContinuar?", "Cancelar", "Importar") != 1) return;
            foreach (var c in listToAdd)
            {
                var x = c.Clone();
                x.Id = 0;
                contacts.Add(store.Insert(x));
            }
            status.Text = $"Importados {listToAdd.Count} contactos.";
            RefreshList();
        }
        catch (Exception ex) { MessageBox.ErrorQuery(App!, "Importar JSON", ex.Message, "OK"); }
    }
    private void ExportJson()
    {
        var path = AskPath("Exportar JSON", "agenda.json");
        if (path is null) return;
        try { JsonAgendaIO.Write(path, store.GetAll()); status.Text = $"Exportado a {path}."; }
        catch (Exception ex) { MessageBox.ErrorQuery(App!, "Exportar JSON", ex.Message, "OK"); }
    }

    private string? AskPath(string title, string initial)
    {
        Dialog d = new() { Title = title, Width = 58, Height = 7 };
        TextField input = new() { Text = initial, X = 2, Y = 1, Width = Dim.Fill(2) };
        string result = "";
        d.Add(new Label { Text = "Archivo:", X = 2, Y = 0 }, input);
        Button ok = new() { Text = "OK" };
        ok.Accepting += (_, e) => { e.Handled = true; result = input.Text?.ToString()?.Trim() ?? ""; if (result.Length > 0) App!.RequestStop(); };
        Button cancel = new() { Text = "Cancelar" };
        cancel.Accepting += (_, e) => { e.Handled = true; App!.RequestStop(); };
        d.AddButton(ok);
        d.AddButton(cancel);
        App!.Run(d);
        return result.Length == 0 ? null : result;
    }
    private void RefreshList()
    {
        var q = search.Text?.ToString()?.Trim() ?? "";
        filtered = contacts.Where(c => (!onlyFav || c.Favorito) && (q.Length == 0 || c.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Telefonos.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Email.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
        list.SetSource(new ObservableCollection<string>(filtered.Select(c => (c.Favorito ? "★ " : "  ") + c.Nombre).ToList()));
        if (filtered.Count > 0) list.SelectedItem = 0;
        ShowDetail();
    }
    private Contacto? Current()
    {
        
        var i = list.SelectedItem ?? -1;
        return i >= 0 && i < filtered.Count ? filtered[i] : null;
    }
    private void ShowDetail()
    {
        var c = Current();
        detail.Text = c is null ? "Sin contacto seleccionado." : $"Nombre: {c.Nombre}\nTelefonos: {c.Telefonos}\nEmail: {c.Email}\nFavorito: {(c.Favorito ? "Si" : "No")}\nNotas:\n{c.Notas}";
    }
    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Q.WithCtrl) { SolicitarSalir(); return true; }
        if (key == Key.F4) { search.SetFocus(); return true; }
        if (key == Key.F2 || key == Key.N.WithCtrl) { AbrirDialogo(); return true; }
        if (key == Key.F3 || key == Key.Enter) { EditSelected(); return true; }
        if (key == Key.Delete || key == Key.D.WithCtrl) { DeleteSelected(); return true; }
        if (key == Key.I.WithCtrl) { ImportJson(); return true; }
        if (key == Key.E.WithCtrl) { ExportJson(); return true; }
        return base.OnKeyDown(key);
    }
    private void SolicitarSalir() {
        App!.RequestStop();
    }
}

// Diálogo de ejemplo
public sealed class EjemploDialog : Dialog {
    readonly TextField nombre = new(), email = new(), favorito = new();
    readonly TextField[] phones = new TextField[5];
    readonly TextView notas = new();
    public bool Ok { get; private set; }
    public new Contacto Result { get; private set; }

    public EjemploDialog() : this("Nuevo contacto", new Contacto()) { }
    public EjemploDialog(string title, Contacto c) {
        Title  = "Diálogo de ejemplo";
        Width  = 50;
        Height = 8;
        Result = c;

        AddRow("Nombre:", 0, nombre, c.Nombre);
        AddRow("Email:", 1, email, c.Email);
        AddRow("Favorito (s/n):", 2, favorito, c.Favorito ? "s" : "n");

        var parts = c.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < 5; i++) AddRow($"Tel {i + 1}:", 3 + i, phones[i] = new TextField(), i < parts.Length ? parts[i] : "");

        Add(new Label { Text = "Notas:", X = 2, Y = 9 }, notas);
        notas.X = 2;
        notas.Y = 10;
        notas.Width = Dim.Fill(2);
        notas.Height = 4;
        notas.Text = c.Notas;

        Button closeButton = new() { Text = "_Guardar", IsDefault = true };
        closeButton.Accepting += (_, e) => { Save(); e.Handled = true; };

        Button cancelButton = new() { Text = "_Cancelar" };
        cancelButton.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };

        AddButton(closeButton);
        AddButton(cancelButton);
    }
    void AddRow(string label, int row, TextField field, string value)
    {
        Add(new Label { Text = label, X = 2, Y = row + 1 }, field);
        field.X = 18;
        field.Y = row + 1;
        field.Width = Dim.Fill(2);
        field.Text = value;
    }

    void Save()
    {
        var n = nombre.Text?.ToString()?.Trim() ?? "";
        var e = email.Text?.ToString()?.Trim() ?? "";
        if (n.Length == 0) { MessageBox.ErrorQuery(App!, "Error", "El nombre no puede estar vacio.", "OK"); return; }
        if (e.Length > 0 && !e.Contains('@')) { MessageBox.ErrorQuery(App!, "Error", "El email debe contener @.", "OK"); return; }

        Result.Nombre = n;
        Result.Email = e;
        Result.Telefonos = string.Join(", ", phones.Select(p => p.Text?.ToString()?.Trim() ?? "").Where(x => x.Length > 0).Take(5));
        Result.Notas = notas.Text?.ToString() ?? "";
        Result.Favorito = (favorito.Text?.ToString()?.Trim().ToLowerInvariant() ?? "") is "s" or "si" or "y" or "yes";
        Ok = true;
        App!.RequestStop();
    }
}
// Persistencia
public class SqliteAgendaStore
{
    readonly string cs;
    public SqliteAgendaStore(string path) => cs = $"Data Source={path}";
    SqliteConnection Open() { var c = new SqliteConnection(cs); c.Open(); return c; }

    public void Init()
    {
        using var db = Open();
        db.Execute("""
        CREATE TABLE IF NOT EXISTS Contactos(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Telefonos TEXT NOT NULL DEFAULT '',
            Email TEXT NOT NULL DEFAULT '',
            Notas TEXT NOT NULL DEFAULT '',
            Favorito INTEGER NOT NULL DEFAULT 0
        );
        """);
    }

    public List<Contacto> GetAll()
    {
        using var db = Open();
        return db.GetAll<Contacto>().OrderBy(x => x.Nombre, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Contacto Insert(Contacto c) { using var db = Open(); c.Id = (int)db.Insert(c); return c; }
    public void Update(Contacto c) { using var db = Open(); db.Update(c); }
    public void Delete(int id) { using var db = Open(); db.Delete(new Contacto { Id = id }); }
}
// JSON
public class JsonAgendaIO
{
    static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static List<Contacto> Read(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"No existe el archivo {path}.");
        return JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(path, Encoding.UTF8), Opts) ?? [];
    }

    public static void Write(string path, IEnumerable<Contacto> items) =>
        File.WriteAllText(path, JsonSerializer.Serialize(items, Opts), Encoding.UTF8);
}
// Modelo
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