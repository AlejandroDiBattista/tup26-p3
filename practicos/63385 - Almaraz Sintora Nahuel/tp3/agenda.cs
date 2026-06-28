#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@10.0.8
#:package Dapper@2.1.79
#:package Dapper.Contrib@2.0.78

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;


string dbPath = args.Length > 0 ? args[0] : "agenda.db";
SqliteAgendaStore store;

try
{
    store = new SqliteAgendaStore(dbPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al abrir la base de datos: {ex.Message}");
    return;
}

IApplication app = Application.Create();
app.Init();
app.Run(new AgendaWindow(store, app));
app.Dispose();



public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore _store;
    private readonly IApplication _app;
    private readonly List<Contacto> _contacts = new();
    private readonly List<Contacto> _filtered = new();
    private bool _soloFavs = false;

    private ListView _listView = null!;
    private TextField _searchBox = null!;
    private Label _detailView = null!;
    private Label _statusBar = null!;

    public AgendaWindow(SqliteAgendaStore store, IApplication app)
    {
        _store = store; _app = app;
        Title = "Agenda"; Width = Dim.Fill(); Height = Dim.Fill();
        BuildLayout(); LoadContacts();
    }

    private void BuildLayout()
    {
        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem("_Archivo", new MenuItem[] {
                new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson),
                new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson),
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
            }),
            new MenuBarItem("_Contactos", new MenuItem[] {
                new MenuItem("_Nuevo", "F2/Ctrl+N", NuevoContacto),
                new MenuItem("_Editar", "F3/Enter", EditarContacto),
                new MenuItem("_Eliminar", "Del/Ctrl+D", EliminarContacto)
            }),
            new MenuBarItem("_Ver", new MenuItem[] { new MenuItem("_Solo favoritos", "", ToggleFavoritos) }),
            new MenuBarItem("_Ayuda", new MenuItem[] { new MenuItem("_Acerca de", "", AcercaDe) })
        });
        Add(menu);

        var searchLabel = new Label { Text = "Buscar:", X = 0, Y = 1 }; Add(searchLabel);
        _searchBox = new TextField { X = Pos.Right(searchLabel) + 1, Y = 1, Width = Dim.Fill() };
        _searchBox.TextChanged += (s, e) => AplicarFiltro(); Add(_searchBox);

        _listView = new ListView() { X = 0, Y = 3, Width = Dim.Percent(40), Height = Dim.Fill(2) };
        _listView.ValueChanged += (s, e) => MostrarDetalle();
        _listView.Accepting += (s, e) => { e.Handled = true; EditarContacto(); }; Add(_listView);

        _detailView = new Label { X = Pos.Right(_listView) + 1, Y = 3, Width = Dim.Fill(), Height = Dim.Fill(2) }; Add(_detailView);

        _statusBar = new Label {
            Text = "F2/Ctrl+N=Nuevo  F3/Enter=Editar  Del/Ctrl+D=Eliminar  Ctrl+I/E=JSON  F4=Buscar  Ctrl+Q=Salir",
            X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill()
        }; Add(_statusBar);
    }

    private void LoadContacts() { _contacts.Clear(); _contacts.AddRange(_store.ObtenerTodos()); AplicarFiltro(); }

    private void AplicarFiltro()
    {
        string texto = (_searchBox.Text?.ToString() ?? "").Trim().ToLowerInvariant();
        _filtered.Clear();
        foreach (var c in _contacts) {
            if (_soloFavs && !c.Favorito) continue;
            if (texto.Length > 0 && !c.Nombre.ToLowerInvariant().Contains(texto) && !c.Telefonos.ToLowerInvariant().Contains(texto) && !c.Email.ToLowerInvariant().Contains(texto)) continue;
            _filtered.Add(c);
        }
        _listView.SetSource(new ObservableCollection<string>(_filtered.Select(c => (c.Favorito ? "★ " : "  ") + c.Nombre).ToList()));
        MostrarDetalle();
    }

    private void MostrarDetalle()
    {
        int idx = _listView.SelectedItem.GetValueOrDefault(-1);
        if (idx < 0 || idx >= _filtered.Count) { _detailView.Text = ""; return; }
        var c = _filtered[idx];
        _detailView.Text = $"Nombre:    {c.Nombre}\nTelefono:  {c.Telefonos}\nEmail:     {c.Email}\nFavorito:  {(c.Favorito ? "Sí" : "No")}\n\nNotas:\n{c.Notas}";
    }

    private Contacto? ContactoSeleccionado() { int idx = _listView.SelectedItem.GetValueOrDefault(-1); return (idx < 0 || idx >= _filtered.Count) ? null : _filtered[idx]; }
    private void SetStatus(string txt) { _statusBar.Text = txt; }

    private void NuevoContacto() {
        var dlg = new ContactDialog(_app, "Nuevo contacto", new Contacto()); _app.Run(dlg);
        if (dlg.Aceptado) { _store.Insertar(dlg.Resultado); _contacts.Add(dlg.Resultado); AplicarFiltro(); SetStatus($"Contacto '{dlg.Resultado.Nombre}' creado."); }
    }

    private void EditarContacto() {
        var orig = ContactoSeleccionado(); if (orig is null) { SetStatus("Seleccione un contacto."); return; }
        var dlg = new ContactDialog(_app, "Editar contacto", orig.Clone()); _app.Run(dlg);
        if (dlg.Aceptado) { dlg.Resultado.Id = orig.Id; _store.Actualizar(dlg.Resultado); int idx = _contacts.IndexOf(orig); if (idx >= 0) _contacts[idx] = dlg.Resultado; AplicarFiltro(); SetStatus($"Contacto '{dlg.Resultado.Nombre}' actualizado."); }
    }

    private void EliminarContacto() {
        var c = ContactoSeleccionado(); if (c is null) { SetStatus("Seleccione un contacto."); return; }
        if (MessageBox.Query(_app, "Confirmar", $"¿Eliminar a '{c.Nombre}'?", "Sí", "No") == 0) { _store.Eliminar(c.Id); _contacts.Remove(c); AplicarFiltro(); SetStatus($"Contacto '{c.Nombre}' eliminado."); }
    }

    private void ImportarJson() {
        string? ruta = SolicitarRuta("Importar JSON", "Archivo a importar:", "contactos.json"); if (ruta is null) return;
        try {
            var contactos = JsonAgendaIO.Leer(ruta);
            if (MessageBox.Query(_app, "Confirmar importacion", $"Se agregaran {contactos.Count} contactos nuevos. ¿Continuar?", "Si", "No") != 0) return;
            foreach (var con in contactos) { con.Id = 0; _store.Insertar(con); _contacts.Add(con); }
            AplicarFiltro(); SetStatus($"Importados {contactos.Count} contactos.");
        }
        catch (FileNotFoundException) { MostrarError("Importar", "El archivo no existe."); }
        catch (Exception ex) { MostrarError("Importar", ex.Message); }
    }

    private void ExportarJson() {
        string? ruta = SolicitarRuta("Exportar JSON", "Archivo de salida:", "salida.json"); if (ruta is null) return;
        try { JsonAgendaIO.Escribir(ruta, _contacts); SetStatus($"Exportados {_contacts.Count} contactos."); }
        catch (Exception ex) { MostrarError("Exportar", ex.Message); }
    }

    private string? SolicitarRuta(string t, string e, string s) { var dlg = new PathDialog(_app, t, e, s); _app.Run(dlg); return dlg.Aceptado ? dlg.Ruta : null; }
    private void MostrarError(string t, string m) { MessageBox.ErrorQuery(_app, t, m, "OK"); }
    private void ToggleFavoritos() { _soloFavs = !_soloFavs; AplicarFiltro(); SetStatus(_soloFavs ? "Solo favoritos." : "Todos los contactos."); }
    private void AcercaDe() { MessageBox.Query(_app, "Acerca de", "Agenda TUI\nTerminal.Gui + SQLite", "OK"); }
    private void SolicitarSalir() { _app.RequestStop(); }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.F2 || key == Key.N.WithCtrl) { NuevoContacto(); return true; }
        if (key == Key.F3 || key == Key.Enter) { EditarContacto(); return true; }
        if (key == Key.DeleteChar || key == Key.Delete || key == Key.D.WithCtrl) { EliminarContacto(); return true; }
        if (key == Key.I.WithCtrl) { ImportarJson(); return true; }
        if (key == Key.E.WithCtrl) { ExportarJson(); return true; }
        if (key == Key.F4) { _searchBox.SetFocus(); return true; }
        if (key == Key.Q.WithCtrl) { SolicitarSalir(); return true; }
        return base.OnKeyDown(key);
    }
}


public sealed class ContactDialog : Dialog
{
    private readonly IApplication _app;
    public bool Aceptado { get; private set; }
    public Contacto Resultado { get; private set; } = new();

    private readonly TextField _nombre = new();
    private readonly TextField[] _telefonos = new TextField[5];
    private readonly TextField _email = new();
    private readonly TextView _notas = new();
    private readonly CheckBox _favorito = new();

    public ContactDialog(IApplication app, string titulo, Contacto c)
    {
        _app = app; Title = titulo; Width = 60; Height = 22;
        int y = 1;

        Add(new Label { Text = "Nombre (*):", X = 1, Y = y });
        _nombre.X = 20; _nombre.Y = y; _nombre.Width = Dim.Fill(1); _nombre.Text = c.Nombre;
        Add(_nombre); y++;

        string[] tels = c.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++)
        {
            Add(new Label { Text = $"Telefono {i + 1}:", X = 1, Y = y });
            _telefonos[i] = new TextField { X = 20, Y = y, Width = Dim.Fill(1), Text = i < tels.Length ? tels[i].Trim() : "" };
            Add(_telefonos[i]); y++;
        }

        Add(new Label { Text = "Email:", X = 1, Y = y });
        _email.X = 20; _email.Y = y; _email.Width = Dim.Fill(1); _email.Text = c.Email;
        Add(_email); y++;

        _favorito.X = 1; _favorito.Y = y; _favorito.Text = "Favorito";
        _favorito.Value = c.Favorito ? CheckState.Checked : CheckState.UnChecked;
        Add(_favorito); y++;

        Add(new Label { Text = "Notas:", X = 1, Y = y }); y++;
        _notas.X = 1; _notas.Y = y; _notas.Width = Dim.Fill(1); _notas.Height = 3; _notas.Text = c.Notas;
        Add(_notas);

        var guardar = new Button { Text = "_Guardar" };
        guardar.Accepting += (s, e) => { if (Validar()) { e.Handled = true; _app.RequestStop(); } };

        var cancelar = new Button { Text = "_Cancelar" };
        cancelar.Accepting += (s, e) => { Aceptado = false; e.Handled = true; _app.RequestStop(); };

        AddButton(guardar); AddButton(cancelar);
    }

    private bool Validar()
    {
        string nombre = (_nombre.Text?.ToString() ?? "").Trim();
        if (nombre.Length == 0) { MessageBox.ErrorQuery(_app, "Validación", "El nombre no puede estar vacío.", "OK"); return false; }
        string email = (_email.Text?.ToString() ?? "").Trim();
        if (email.Length > 0 && !email.Contains('@')) { MessageBox.ErrorQuery(_app, "Validación", "El email debe contener '@'.", "OK"); return false; }

        string telefonos = string.Join(", ", _telefonos.Select(t => (t.Text?.ToString() ?? "").Trim()).Where(t => t.Length > 0));
        Aceptado = true;
        Resultado = new Contacto { Nombre = nombre, Telefonos = telefonos, Email = email, Notas = _notas.Text?.ToString() ?? "", Favorito = _favorito.Value == CheckState.Checked };
        return true;
    }
}

public sealed class PathDialog : Dialog
{
    private readonly IApplication _app;
    private readonly TextField _ruta = new();
    public bool Aceptado { get; private set; }
    public string Ruta { get; private set; } = "";

    public PathDialog(IApplication app, string titulo, string etiqueta, string sugerida)
    {
        _app = app;
        Title = titulo; Width = 70; Height = 8;

        Add(new Label { Text = etiqueta, X = 1, Y = 1 });
        _ruta.X = 1; _ruta.Y = 2; _ruta.Width = Dim.Fill(1); _ruta.Text = sugerida;
        Add(_ruta);

        var aceptar = new Button { Text = "_Aceptar" };
        aceptar.Accepting += (s, e) => {
            string ruta = (_ruta.Text?.ToString() ?? "").Trim();
            if (ruta.Length == 0) { MessageBox.ErrorQuery(_app, "Validacion", "La ruta no puede estar vacia.", "OK"); return; }
            Ruta = ruta; Aceptado = true; e.Handled = true; _app.RequestStop();
        };

        var cancelar = new Button { Text = "_Cancelar" };
        cancelar.Accepting += (s, e) => { Aceptado = false; e.Handled = true; _app.RequestStop(); };

        AddButton(aceptar);
        AddButton(cancelar);
    }
}

public class SqliteAgendaStore
{
    private readonly string _cs;

    public SqliteAgendaStore(string dbPath)
    {
        _cs = $"Data Source={dbPath}";

        using var conn = Abrir();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL,
                Email TEXT NOT NULL,
                Notas TEXT NOT NULL,
                Favorito INTEGER NOT NULL
            )
        ");
    }

    private SqliteConnection Abrir()
    {
        var conn = new SqliteConnection(_cs);
        conn.Open();
        return conn;
    }

    public IEnumerable<Contacto> ObtenerTodos()
    {
        using var conn = Abrir();
        return conn.GetAll<Contacto>().ToList();
    }

    public void Insertar(Contacto c)
    {
        using var conn = Abrir();
        long id = conn.Insert(c);
        c.Id = (int)id;
    }

    public void Actualizar(Contacto c)
    {
        using var conn = Abrir();
        conn.Update(c);
    }

    public void Eliminar(int id)
    {
        using var conn = Abrir();
        conn.Delete(new Contacto { Id = id });
    }
}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<Contacto> Leer(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("No existe el archivo JSON.", path);

        string json = File.ReadAllText(path, Encoding.UTF8);
        var contactos = JsonSerializer.Deserialize<List<Contacto>>(json, Options);

        if (contactos is null)
            throw new JsonException("El archivo no contiene una lista de contactos.");

        foreach (var contacto in contactos)
        {
            contacto.Id = 0;
            contacto.Nombre ??= "";
            contacto.Telefonos ??= "";
            contacto.Email ??= "";
            contacto.Notas ??= "";
        }

        return contactos;
    }

    public static void Escribir(string path, IEnumerable<Contacto> contactos)
    {
        string json = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}

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
            Id = Id,
            Nombre = Nombre,
            Telefonos = Telefonos,
            Email = Email,
            Notas = Notas,
            Favorito = Favorito
        };
    }
}