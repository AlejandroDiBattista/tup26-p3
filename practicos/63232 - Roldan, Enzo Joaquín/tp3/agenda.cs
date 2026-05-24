#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store;
try {
    store = new SqliteAgendaStore(dbPath);
} catch (Exception ex) {
    Console.WriteLine($"Error al abrir la base: {ex.Message}");
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));

public sealed class AgendaWindow : Window {

    private readonly SqliteAgendaStore store;
    private List<Contacto> contacts = [];
    private List<Contacto> filteredContacts = [];
    private bool onlyFavorites = false;

    private readonly TextField searchField;
    private readonly ListView contactList;
    private readonly TextView detailView;

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;
        Title  = "AgendaT";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON", "Ctrl+I", () => MessageBox.Query("Info", "Importar — próximamente", "OK")),
                    new MenuItem("_Exportar JSON", "Ctrl+E", () => MessageBox.Query("Info", "Exportar — próximamente", "OK")),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", Salir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo",    "F2",  () => MessageBox.Query("Info", "Nuevo — próximamente", "OK")),
                    new MenuItem("_Editar",   "F3",  () => MessageBox.Query("Info", "Editar — próximamente", "OK")),
                    new MenuItem("_Eliminar", "Del", () => MessageBox.Query("Info", "Eliminar — próximamente", "OK"))
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("_Solo favoritos", "", ToggleFavoritos)
                ]),
                new MenuBarItem("_Ayuda", [
                    new MenuItem("_Acerca de", "", AcercaDe)
                ])
            ]
        };

        Label searchLabel = new() { Text = "Buscar:", X = 1, Y = 1 };
        searchField = new TextField("") { X = 10, Y = 1, Width = Dim.Fill(2) };
        searchField.TextChanged += (_) => ApplyFilters();

        FrameView listFrame = new() {
            Title = "Contactos",
            X = 0, Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1)
        };
        contactList = new ListView() { Width = Dim.Fill(), Height = Dim.Fill() };
        contactList.SelectedItemChanged += (_) => UpdateDetail();
        listFrame.Add(contactList);

        FrameView detailFrame = new() {
            Title = "Detalle",
            X = Pos.Right(listFrame), Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };
        detailView = new TextView() { ReadOnly = true, Width = Dim.Fill(), Height = Dim.Fill() };
        detailFrame.Add(detailView);

        StatusBar statusBar = new([
            new Shortcut(Key.F2,               "Nuevo",    null),
            new Shortcut(Key.F3,               "Editar",   null),
            new Shortcut(Key.DeleteChar,       "Eliminar", null),
            new Shortcut(Key.CtrlMask | Key.I, "Importar", null),
            new Shortcut(Key.CtrlMask | Key.E, "Exportar", null),
            new Shortcut(Key.F4,               "Buscar",   () => searchField.SetFocus()),
            new Shortcut(Key.CtrlMask | Key.Q, "Salir",    Salir)
        ]);

        Add(menu, searchLabel, searchField, listFrame, detailFrame, statusBar);
        LoadContacts();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == (Key.CtrlMask | Key.Q)) { Salir();           return true; }
        if (key == Key.F4)                 { searchField.SetFocus(); return true; }
        return base.OnKeyDown(key);
    }

    private void LoadContacts() {
        contacts = store.GetAll();
        ApplyFilters();
    }

    private void ApplyFilters() {
        string q = searchField.Text?.ToString()?.ToLower() ?? "";
        filteredContacts = contacts
            .Where(c =>
                (!onlyFavorites || c.Favorito) &&
                (c.Nombre.ToLower().Contains(q) ||
                 c.Telefonos.ToLower().Contains(q) ||
                 c.Email.ToLower().Contains(q)))
            .ToList();
        contactList.SetSource(filteredContacts.Select(c => $"{(c.Favorito ? "★" : " ")} {c.Nombre}").ToList());
        UpdateDetail();
    }

    private Contacto? Selected() {
        int i = contactList.SelectedItem;
        return (i >= 0 && i < filteredContacts.Count) ? filteredContacts[i] : null;
    }

    private void UpdateDetail() {
        Contacto? c = Selected();
        detailView.Text = c is null ? "" :
$"""
Nombre:    {c.Nombre}

Teléfonos:
{c.Telefonos}

Email:
{c.Email}

Favorito:  {(c.Favorito ? "Sí" : "No")}

Notas:
{c.Notas}
""";
    }

    private void ToggleFavoritos() { onlyFavorites = !onlyFavorites; ApplyFilters(); }
    private void AcercaDe() { MessageBox.Query("Acerca de", "AgendaT\nTP3 — Terminal.Gui + SQLite + JSON", "Aceptar"); }
    private void Salir()    { Application.RequestStop(); }
}

public sealed class SqliteAgendaStore {

    private readonly string cs;

    public SqliteAgendaStore(string path) {
        cs = $"Data Source={path}";
        using SqliteConnection db = Open();
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT    NOT NULL,
                Telefonos TEXT    NOT NULL DEFAULT '',
                Email     TEXT    NOT NULL DEFAULT '',
                Notas     TEXT    NOT NULL DEFAULT '',
                Favorito  INTEGER NOT NULL DEFAULT 0
            );
        """);
    }

    private SqliteConnection Open() {
        SqliteConnection db = new(cs);
        db.Open();
        return db;
    }

    public List<Contacto> GetAll() { using SqliteConnection db = Open(); return db.GetAll<Contacto>().ToList(); }
    public void Insert(Contacto c) { using SqliteConnection db = Open(); c.Id = (int)db.Insert(c); }
    public void Update(Contacto c) { using SqliteConnection db = Open(); db.Update(c); }
    public void Delete(int id)     { using SqliteConnection db = Open(); db.Delete(new Contacto { Id = id }); }
}

public static class JsonAgendaIO {

    private static readonly JsonSerializerOptions Opts = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static List<Contacto> Import(string path) {
        if (!File.Exists(path)) throw new FileNotFoundException($"Archivo no encontrado: {path}");
        return JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(path), Opts) ?? [];
    }

    public static void Export(string path, List<Contacto> contactos) {
        File.WriteAllText(path, JsonSerializer.Serialize(contactos, Opts));
    }
}

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int    Id        { get; set; }
         public string Nombre    { get; set; } = "";
         public string Telefonos { get; set; } = "";
         public string Email     { get; set; } = "";
         public string Notas     { get; set; } = "";
         public bool   Favorito  { get; set; }

    public Contacto Clone() => new() {
        Id = Id, Nombre = Nombre, Telefonos = Telefonos,
        Email = Email, Notas = Notas, Favorito = Favorito
    };
}
