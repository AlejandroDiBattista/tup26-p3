#!/usr/bin/env dotnet
// #:package Terminal.Gui@2.0.0-rc.4
// #:package Microsoft.Data.Sqlite@10.0.8
// #:package Dapper@2.1.35
// #:package Dapper.Contrib@2.0.78

#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8625, IL2026, IL3050, 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text.Json;
using Terminal.Gui;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

string dbPath = args.Length > 0 && !args[0].EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? args[0] : "agenda.db";
try {
    Application.Init();
    var store = new SqliteAgendaStore(dbPath);
    Application.Run(new AgendaWindow(store));
    Application.Shutdown();
}
catch (Exception ex) {
    Console.WriteLine($"Error crítico: {ex.Message}");
}

public class SqliteAgendaStore {
    private string _cs;

    public SqliteAgendaStore(string path) {
        _cs = $"Data Source={path}";
        using var cn = new SqliteConnection(_cs);
        cn.Execute(@"CREATE TABLE IF NOT EXISTS Contactos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, Nombre TEXT NOT NULL,
            Telefonos TEXT, Email TEXT, Notas TEXT, Favorito INTEGER)");
    }

    public List<Contacto> GetAll() {
        using var cn = new SqliteConnection(_cs);
        return cn.GetAll<Contacto>().ToList();
    }

    public void Insert(Contacto c) {
        using var cn = new SqliteConnection(_cs);
        c.Id = (int)cn.Insert(c);
    }

    public void Update(Contacto c) {
        using var cn = new SqliteConnection(_cs);
        cn.Update(c);
    }

    public void Delete(Contacto c) {
        using var cn = new SqliteConnection(_cs);
        cn.Delete(c);
    }
}

public class JsonAgendaIO {
    private static readonly JsonSerializerOptions opts = new() {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    public static void Export(string ruta, IEnumerable<Contacto> c) => File.WriteAllText(ruta, JsonSerializer.Serialize(c, opts));
    public static List<Contacto> Import(string ruta) => File.Exists(ruta) ? JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(ruta), opts) ?? [] : throw new FileNotFoundException("El archivo no existe.");
}

public class ContactDialog : Dialog {
    public Contacto Result { get; private set; }
    public bool IsSaved { get; private set; }

    private Contacto _editable;
    private TextField _txtName, _txtEmail;
    private TextField[] _txtPhones = new TextField[5];
    private TextView _txtNotes;
    private bool _isFav;

    public ContactDialog(string title, Contacto c) {
        Title = title; Width = 60; Height = 22;
        _editable = c;
        _isFav = c.Favorito;

        Add(new Label { Text = "Nombre (*):", X = 1, Y = 1 });
        _txtName = new TextField { X = 15, Y = 1, Width = Dim.Fill(1), Text = c.Nombre ?? "" };

        Add(new Label { Text = "Email:", X = 1, Y = 3 });
        _txtEmail = new TextField { X = 15, Y = 3, Width = Dim.Fill(1), Text = c.Email ?? "" };

        var phones = (c.Telefonos ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Add(new Label { Text = "Teléfonos:", X = 1, Y = 5 });
        for (int i = 0; i < 5; i++) {
            _txtPhones[i] = new TextField { X = 15, Y = 5 + i, Width = Dim.Fill(1), Text = i < phones.Length ? phones[i] : "" };
            Add(new Label { Text = $"{i + 1}:", X = 11, Y = 5 + i }, _txtPhones[i]);
        }

        Add(new Label { Text = "Notas:", X = 1, Y = 11 });
        _txtNotes = new TextView { X = 15, Y = 11, Width = Dim.Fill(1), Height = 3, Text = c.Notas ?? "" };

        var btnFav = new Button { Text = _isFav ? "[★] Es Favorito" : "[ ] Es Favorito", X = 15, Y = 15 };
        btnFav.Accepting += (_, _) => {
            _isFav = !_isFav;
            btnFav.Text = _isFav ? "[★] Es Favorito" : "[ ] Es Favorito";
        };

        var btnSave = new Button { Text = "Guardar", IsDefault = true, X = Pos.Center() - 10, Y = Pos.AnchorEnd(2) };
        var btnCancel = new Button { Text = "Cancelar", X = Pos.Center() + 2, Y = Pos.AnchorEnd(2) };

        btnSave.Accepting += (_, _) => {
            string n = _txtName.Text?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(n)) { MessageBox.ErrorQuery("Error", "El nombre es obligatorio.", "OK"); return; }
            string e = _txtEmail.Text?.ToString()?.Trim() ?? "";
            if (e != "" && !e.Contains("@")) { MessageBox.ErrorQuery("Error", "El email debe tener '@'.", "OK"); return; }

            _editable.Nombre = n; _editable.Email = e;
            _editable.Notas = _txtNotes.Text?.ToString() ?? "";
            _editable.Favorito = _isFav;
            _editable.Telefonos = string.Join(", ", _txtPhones.Select(x => x.Text?.ToString()?.Trim()).Where(x => !string.IsNullOrEmpty(x)));

            Result = _editable; IsSaved = true; Application.RequestStop();
        };
        btnCancel.Accepting += (_, _) => { IsSaved = false; Application.RequestStop(); };

        Add(_txtName, _txtEmail, _txtNotes, btnFav, btnSave, btnCancel);
    }
}

public class AgendaWindow : Window {
    private SqliteAgendaStore _store;
    private List<Contacto> _contacts, _filtered = new();
    private ListView _list;
    private TextField _search;
    private bool _onlyFavs;
    private Label _status;
    private Label _lblNombre, _lblTels, _lblEmail, _lblFav;
    private TextView _lblNotas;
    private MenuItem _mnuFavs;

    public AgendaWindow(SqliteAgendaStore store) {
        _store = store; Title = "AgendaT - TUI"; Width = Dim.Fill(); Height = Dim.Fill();
        _contacts = _store.GetAll();

        _mnuFavs = new MenuItem("Solo _favoritos", "", ToggleFavs) { CheckType = MenuItemCheckStyle.Checked };
        var menu = new MenuBar {
            Menus = new MenuBarItem[] {
            new MenuBarItem("_Archivo", new MenuItem[] {
                new MenuItem("_Importar JSON", "Ctrl+I", Importar),
                new MenuItem("_Exportar JSON", "Ctrl+E", Exportar),
                new MenuItem("_Salir", "Ctrl+Q", Salir)
            }),
            new MenuBarItem("_Contactos", new MenuItem[] {
                new MenuItem("_Nuevo", "F2 / Ctrl+N", Nuevo),
                new MenuItem("E_ditar", "F3 / Enter", Editar),
                new MenuItem("_Eliminar", "Del / Ctrl+D", Eliminar)
            }),
            new MenuBarItem("_Ver", new MenuItem[] { _mnuFavs }),
            new MenuBarItem("A_yuda", new MenuItem[] { new MenuItem("_Acerca de", "", AcercaDe) })
            }
        };

        var lblBuscar = new Label { Text = "Buscar (F4):", X = 0, Y = Pos.Bottom(menu) };
        _search = new TextField { X = Pos.Right(lblBuscar) + 1, Y = Pos.Top(lblBuscar), Width = Dim.Fill() };
        _search.TextChanged += (_, _) => RefreshList();

        var frmList = new FrameView { Title = "Lista", X = 0, Y = Pos.Bottom(_search), Width = Dim.Percent(40), Height = Dim.Fill(1) };
        _list = new ListView { Width = Dim.Fill(), Height = Dim.Fill(), AllowsMarking = false };
        _list.SelectedItemChanged += (_, _) => UpdateDetails();
        _list.OpenSelectedItem += (_, _) => Editar();
        frmList.Add(_list);

        var frmDet = new FrameView { Title = "Detalle", X = Pos.Right(frmList), Y = Pos.Top(frmList), Width = Dim.Fill(), Height = Dim.Fill(1) };
        _lblNombre = new Label { X = 1, Y = 1, Width = Dim.Fill() };
        _lblTels = new Label { X = 1, Y = 3, Width = Dim.Fill() };
        _lblEmail = new Label { X = 1, Y = 5, Width = Dim.Fill() };
        _lblFav = new Label { X = 1, Y = 7, Width = Dim.Fill() };
        _lblNotas = new TextView { X = 1, Y = 9, Width = Dim.Fill(1), Height = Dim.Fill(), ReadOnly = true };
        frmDet.Add(_lblNombre, _lblTels, _lblEmail, _lblFav, new Label { Text = "Notas:", X = 1, Y = 8 }, _lblNotas);

        _status = new Label { Text = " Listo", X = 0, Y = Pos.Bottom(frmList), Width = Dim.Fill() };

        Add(menu, lblBuscar, _search, frmList, frmDet, _status);
        RefreshList();
    }

    private void ToggleFavs() { _onlyFavs = !_onlyFavs; _mnuFavs.Checked = _onlyFavs; RefreshList(); }

    private void RefreshList() {
        var q = _search.Text?.ToString()?.ToLower() ?? "";
        _filtered = _contacts.Where(c => (!_onlyFavs || c.Favorito) && ((c.Nombre ?? "").ToLower().Contains(q) || (c.Telefonos ?? "").ToLower().Contains(q) || (c.Email ?? "").ToLower().Contains(q))).OrderBy(c => c.Nombre).ToList();
        _list.SetSource(new ObservableCollection<Contacto>(_filtered));
        UpdateDetails();
    }

    private void UpdateDetails() {
        if (_list.SelectedItem >= 0 && _list.SelectedItem < _filtered.Count) {
            var c = _filtered[_list.SelectedItem];
            _lblNombre.Text = $"Nombre: {c.Nombre}"; _lblTels.Text = $"Tel: {c.Telefonos}"; _lblEmail.Text = $"Email: {c.Email}"; _lblFav.Text = c.Favorito ? "★ Favorito" : ""; _lblNotas.Text = c.Notas ?? "";
        }
        else {
            _lblNombre.Text = _lblTels.Text = _lblEmail.Text = _lblFav.Text = _lblNotas.Text = "";
        }
    }

    private void Nuevo() {
        var dlg = new ContactDialog("Nuevo Contacto", new Contacto());
        Application.Run(dlg);
        if (dlg.IsSaved) { _store.Insert(dlg.Result); _contacts.Add(dlg.Result); RefreshList(); _status.Text = " Creado."; }
    }

    private void Editar() {
        if (_list.SelectedItem < 0 || _list.SelectedItem >= _filtered.Count) return;
        var dlg = new ContactDialog("Editar Contacto", _filtered[_list.SelectedItem].Clone());
        Application.Run(dlg);
        if (dlg.IsSaved) { _store.Update(dlg.Result); var i = _contacts.FindIndex(x => x.Id == dlg.Result.Id); if (i >= 0) _contacts[i] = dlg.Result; RefreshList(); _status.Text = " Modificado."; }
    }

    private void Eliminar() {
        if (_list.SelectedItem < 0 || _list.SelectedItem >= _filtered.Count) return;
        var c = _filtered[_list.SelectedItem];
        if (MessageBox.Query("Eliminar", $"¿Borrar a {c.Nombre}?", "Sí", "No") == 0) { _store.Delete(c); _contacts.RemoveAll(x => x.Id == c.Id); RefreshList(); _status.Text = " Eliminado."; }
    }

    private void Importar() {
        var r = InputBox("Importar", "Ruta del JSON:"); if (string.IsNullOrEmpty(r)) return;
        try {
            var l = JsonAgendaIO.Import(r);
            if (MessageBox.Query("Importar", $"¿Importar {l.Count} contactos?", "Sí", "No") == 0) {
                foreach (var c in l) { c.Id = 0; _store.Insert(c); _contacts.Add(c); }
                RefreshList(); _status.Text = " Importados.";
            }
        }
        catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "OK"); }
    }

    private void Exportar() {
        var r = InputBox("Exportar", "Ruta a guardar:"); if (string.IsNullOrEmpty(r)) return;
        try { JsonAgendaIO.Export(r, _contacts); MessageBox.Query("Exportado", "Exitoso.", "OK"); _status.Text = " Exportado."; } catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "OK"); }
    }

    private string InputBox(string t, string p) {
        string r = ""; var d = new Dialog { Title = t, Width = 50, Height = 10 };
        var txt = new TextField { X = 1, Y = 3, Width = Dim.Fill(1) };
        var bOk = new Button { Text = "Ok", IsDefault = true, X = Pos.Center() - 8, Y = 6 };
        var bCa = new Button { Text = "Cancelar", X = Pos.Center() + 2, Y = 6 };
        bOk.Accepting += (_, _) => { r = txt.Text?.ToString() ?? ""; Application.RequestStop(); };
        bCa.Accepting += (_, _) => { r = ""; Application.RequestStop(); };
        d.Add(new Label { Text = p, X = 1, Y = 1 }, txt, bOk, bCa); Application.Run(d); return r;
    }

    private void Salir() => Application.RequestStop();
    private void AcercaDe() => MessageBox.Query("AgendaT", "App TUI\n.NET 10", "OK");

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) { Application.RequestStop(); return true; }
        if (key == Key.N.WithCtrl || key == Key.F2) { Nuevo(); return true; }
        if (key == Key.Enter || key == Key.F3) { if (!_search.HasFocus) { Editar(); return true; } }
        if (key == Key.D.WithCtrl || (key == Key.Delete && !_search.HasFocus)) { Eliminar(); return true; }
        if (key == Key.I.WithCtrl) { Importar(); return true; }
        if (key == Key.E.WithCtrl) { Exportar(); return true; }
        if (key == Key.F4) { _search.SetFocus(); return true; }
        return base.OnKeyDown(key);
    }
}

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => (Contacto)this.MemberwiseClone();
    public override string ToString() => $"{(Favorito ? "[★]" : "[ ]")} {Nombre}";
}