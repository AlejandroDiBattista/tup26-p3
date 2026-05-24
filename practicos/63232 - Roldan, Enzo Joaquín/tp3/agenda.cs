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
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", Salir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo",    "F2",  NuevoContacto),
                    new MenuItem("_Editar",   "F3",  EditarContacto),
                    new MenuItem("_Eliminar", "Del", EliminarContacto)
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
        contactList.OpenSelectedItem    += (_) => EditarContacto();
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
            new Shortcut(Key.F2,               "Nuevo",    NuevoContacto),
            new Shortcut(Key.F3,               "Editar",   EditarContacto),
            new Shortcut(Key.DeleteChar,       "Eliminar", EliminarContacto),
            new Shortcut(Key.CtrlMask | Key.I, "Importar", ImportJson),
            new Shortcut(Key.CtrlMask | Key.E, "Exportar", ExportJson),
            new Shortcut(Key.F4,               "Buscar",   () => searchField.SetFocus()),
            new Shortcut(Key.CtrlMask | Key.Q, "Salir",    Salir)
        ]);

        Add(menu, searchLabel, searchField, listFrame, detailFrame, statusBar);
        LoadContacts();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == (Key.CtrlMask | Key.N) || key == Key.F2)        { NuevoContacto();        return true; }
        if (key == Key.F3)                                          { EditarContacto();        return true; }
        if (key == (Key.CtrlMask | Key.D) || key == Key.DeleteChar) { EliminarContacto();     return true; }
        if (key == (Key.CtrlMask | Key.I))                         { ImportJson();            return true; }
        if (key == (Key.CtrlMask | Key.E))                         { ExportJson();            return true; }
        if (key == Key.F4)                                          { searchField.SetFocus();  return true; }
        if (key == (Key.CtrlMask | Key.Q))                         { Salir();                 return true; }
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

    private void NuevoContacto() {
        ContactDialog dlg = new();
        Application.Run(dlg);
        if (!dlg.Accepted) return;
        try {
            store.Insert(dlg.Contacto!);
            contacts.Add(dlg.Contacto!);
            ApplyFilters();
        } catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "Aceptar"); }
    }

    private void EditarContacto() {
        Contacto? sel = Selected();
        if (sel is null) return;
        ContactDialog dlg = new(sel.Clone());
        Application.Run(dlg);
        if (!dlg.Accepted) return;
        try {
            store.Update(dlg.Contacto!);
            int idx = contacts.FindIndex(c => c.Id == dlg.Contacto!.Id);
            if (idx >= 0) contacts[idx] = dlg.Contacto!;
            ApplyFilters();
        } catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "Aceptar"); }
    }

    private void EliminarContacto() {
        Contacto? sel = Selected();
        if (sel is null) return;
        if (MessageBox.Query("Confirmar", $"¿Eliminar a {sel.Nombre}?", "Sí", "No") != 0) return;
        try {
            store.Delete(sel.Id);
            contacts.RemoveAll(c => c.Id == sel.Id);
            ApplyFilters();
        } catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "Aceptar"); }
    }

    private void ImportJson() {
        string path = AskPath("Importar JSON");
        if (string.IsNullOrWhiteSpace(path)) return;
        try {
            List<Contacto> imported = JsonAgendaIO.Import(path);
            if (MessageBox.Query("Confirmar", $"Se importarán {imported.Count} contactos.", "Importar", "Cancelar") != 0) return;
            foreach (Contacto c in imported) {
                c.Id = 0;
                store.Insert(c);
                contacts.Add(c);
            }
            ApplyFilters();
        } catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "Aceptar"); }
    }

    private void ExportJson() {
        string path = AskPath("Exportar JSON");
        if (string.IsNullOrWhiteSpace(path)) return;
        try {
            JsonAgendaIO.Export(path, contacts);
            MessageBox.Query("OK", $"Exportados {contacts.Count} contactos.", "Aceptar");
        } catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "Aceptar"); }
    }

    private string AskPath(string title) {
        string result = "";
        Dialog dlg = new() { Title = title, Width = 60, Height = 8 };
        TextField field = new() { X = 1, Y = 1, Width = Dim.Fill(2) };
        Button ok     = new() { Text = "OK",       X = Pos.Center() - 8, Y = 3 };
        Button cancel = new() { Text = "Cancelar", X = Pos.Center() + 2, Y = 3 };
        ok.Accepting     += (_, e) => { result = field.Text?.ToString() ?? ""; Application.RequestStop(); e.Handled = true; };
        cancel.Accepting += (_, e) => { result = ""; Application.RequestStop(); e.Handled = true; };
        dlg.Add(field);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);
        Application.Run(dlg);
        return result;
    }

    private void ToggleFavoritos() { onlyFavorites = !onlyFavorites; ApplyFilters(); }
    private void AcercaDe() { MessageBox.Query("Acerca de", "AgendaT\nTP3 — Terminal.Gui + SQLite + JSON", "Aceptar"); }
    private void Salir()    { Application.RequestStop(); }
}

public sealed class ContactDialog : Dialog {

    public Contacto? Contacto { get; private set; }
    public bool Accepted { get; private set; }

    private readonly TextField nombreField;
    private readonly TextField emailField;
    private readonly TextField[] telFields = new TextField[5];
    private readonly CheckBox favCheck;
    private readonly TextView notasField;

    public ContactDialog(Contacto? c = null) {
        c ??= new Contacto();
        Title  = c.Id == 0 ? "Nuevo Contacto" : "Editar Contacto";
        Width  = 70;
        Height = 24;

        Add(new Label("Nombre:") { X = 1, Y = 1 });
        nombreField = new(c.Nombre) { X = 15, Y = 1, Width = 40 };
        Add(nombreField);

        string[] tels = c.Telefonos.Split(',');
        for (int i = 0; i < 5; i++) {
            Add(new Label($"Tel {i + 1}:") { X = 1, Y = 3 + i });
            string val = i < tels.Length ? tels[i].Trim() : "";
            telFields[i] = new TextField(val) { X = 15, Y = 3 + i, Width = 30 };
            Add(telFields[i]);
        }

        Add(new Label("Email:") { X = 1, Y = 9 });
        emailField = new(c.Email) { X = 15, Y = 9, Width = 40 };
        Add(emailField);

        favCheck = new() {
            Text = "Favorito", X = 15, Y = 11,
            CheckedState = c.Favorito ? CheckState.Checked : CheckState.UnChecked
        };
        Add(favCheck);

        Add(new Label("Notas:") { X = 1, Y = 13 });
        notasField = new() { X = 15, Y = 13, Width = 40, Height = 4, Text = c.Notas };
        Add(notasField);

        Button save   = new() { Text = "Guardar" };
        Button cancel = new() { Text = "Cancelar" };

        save.Accepting += (_, e) => {
            string nombre = nombreField.Text?.ToString()?.Trim() ?? "";
            string email  = emailField.Text?.ToString()?.Trim()  ?? "";
            if (string.IsNullOrWhiteSpace(nombre)) {
                MessageBox.ErrorQuery("Error", "El nombre no puede estar vacío.", "Aceptar");
                return;
            }
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) {
                MessageBox.ErrorQuery("Error", "El email debe contener @.", "Aceptar");
                return;
            }
            string telsJoined = string.Join(", ",
                telFields.Select(t => t.Text?.ToString()?.Trim() ?? "").Where(t => t != ""));
            Contacto = new Contacto {
                Id        = c.Id,
                Nombre    = nombre,
                Telefonos = telsJoined,
                Email     = email,
                Notas     = notasField.Text?.ToString() ?? "",
                Favorito  = favCheck.CheckedState == CheckState.Checked
            };
            Accepted = true;
            Application.RequestStop();
            e.Handled = true;
        };

        cancel.Accepting += (_, e) => { Accepted = false; Application.RequestStop(); e.Handled = true; };

        AddButton(save);
        AddButton(cancel);
    }
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
