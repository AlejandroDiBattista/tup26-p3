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
using System.Data.Common;
using Dapper.Contrib.Extensions;

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store;
try
{
    store = new SqliteAgendaStore(dbPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al abrir la base de datos: {ex.Message}");
    return 1;
}
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));
return 0;


// Ventana principal
public sealed class AgendaWindow : Runnable {
    private readonly SqliteAgendaStore _store;

    private List<Contacto> _contacts = [];
    private List<Contacto> _filteredContacts = [];

    private ListView _listView = null!;
    private Label _detailName = null!;
    private Label _detailPhone = null!;
    private Label _detailEmail = null!;
    private Label _detailNotes = null!;
    private Label _statusBar = null!;
    


    public AgendaWindow(SqliteAgendaStore store) {

        _store = store;
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        _listView.ValueChanged += (_, _) =>
    MostrarDetalle(ContactoSeleccionado());
    }

    private void BuildLayout() {
         MenuBar menu = new() {
        Menus = [
            new MenuBarItem("_Archivo", [
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
            ])
        ]
    };

    FrameView listFrame = new() {
        Title = "Contactos",
        X = 0,
        Y = 1,
        Width = Dim.Percent(40),
        Height = Dim.Fill(1)
    };

    _listView = new ListView() {
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };

    listFrame.Add(_listView);

    FrameView detailFrame = new() {
        Title = "Detalle",
        X = Pos.Right(listFrame),
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill(1)
    };

    _detailName = new Label() { X = 0, Y = 0 };
    _detailPhone = new Label() { X = 0, Y = 1 };
    _detailEmail = new Label() { X = 0, Y = 2 };
    _detailNotes = new Label() { X = 0, Y = 3 };

    detailFrame.Add(
        _detailName,
        _detailPhone,
        _detailEmail,
        _detailNotes
    );

    _statusBar = new Label() {
        Text = "Listo.",
        X = 0,
        Y = Pos.AnchorEnd(1),
        Width = Dim.Fill()
    };

    Add(menu, listFrame, detailFrame, _statusBar);

    CargarContactos();
    }
    private void CargarContactos()
{
    _contacts = _store.GetAll().ToList();

    _filteredContacts = _contacts;

    _listView.SetSource<string>(
        new System.Collections.ObjectModel.ObservableCollection<string>(
            _filteredContacts.Select(c => c.Nombre)
        )
    );
    
}
private Contacto? ContactoSeleccionado()
{
    int idx = _listView.SelectedItem ?? -1;

    return idx >= 0 && idx < _filteredContacts.Count
        ? _filteredContacts[idx]
        : null;
}

private void MostrarDetalle(Contacto? c)
{
    if (c is null)
    {
        _detailName.Text = "";
        _detailPhone.Text = "";
        _detailEmail.Text = "";
        _detailNotes.Text = "";
        return;
    }

    _detailName.Text = $"Nombre: {c.Nombre}";
    _detailPhone.Text = $"Tel: {c.Telefonos}";
    _detailEmail.Text = $"Email: {c.Email}";
    _detailNotes.Text = $"Notas: {c.Notas}";
}


    private void AbrirDialogo() {
        ContactDialog dialog = new(new Contacto());
        App!.Run(dialog);
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            SolicitarSalir();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class ContactDialog : Dialog {
       public bool Confirmed { get; private set; }

    public Contacto? ContactResult { get; private set; }

    private readonly TextField _nameField;
    private readonly TextField _emailField;

    public ContactDialog(Contacto initial)
    {
        Title = "Contacto";

        Width = 50;
        Height = 10;

        Add(new Label { Text = "Nombre:", X = 1, Y = 1 });

        _nameField = new TextField
        {
            Text = initial.Nombre,
            X = 12,
            Y = 1,
            Width = 30
        };

        Add(_nameField);

        Add(new Label { Text = "Email:", X = 1, Y = 3 });

        _emailField = new TextField
        {
            Text = initial.Email,
            X = 12,
            Y = 3,
            Width = 30
        };

        Add(_emailField);

        Button btnGuardar = new()
        {
            Text = "_Guardar",
            IsDefault = true
        };

        btnGuardar.Accepting += (_, e) =>
        {
            Guardar();
            e.Handled = true;
        };

        AddButton(btnGuardar);
    }

    private void Guardar()
    {
        string nombre = _nameField.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrEmpty(nombre))
        {
            MessageBox.ErrorQuery(
                App!,
                "Validación",
                "El nombre no puede estar vacío.",
                "OK"
            );
            return;
        }

        ContactResult = new Contacto
        {
            Nombre = nombre,
            Email = _emailField.Text?.ToString() ?? ""
        };

        Confirmed = true;

        App!.RequestStop();
    }

}


public class SqliteAgendaStore 
{
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";

        using SqliteConnection con = Abrir();

        con.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT    NOT NULL DEFAULT '',
                Telefonos TEXT    NOT NULL DEFAULT '',
                Email     TEXT    NOT NULL DEFAULT '',
                Notas     TEXT    NOT NULL DEFAULT '',
                Favorito  INTEGER NOT NULL DEFAULT 0
            )
        ");
    }
     private SqliteConnection Abrir()
    {
        SqliteConnection con = new(_connectionString);
        con.Open();
        return con;
    }
    public IEnumerable<Contacto> GetAll()
    {
        using SqliteConnection con = Abrir();
        return con.GetAll<Contacto>().ToList();
    }

    public void Insert(Contacto c)
    {
        using SqliteConnection con = Abrir();
        c.Id = (int)con.Insert(c);
    }

    public void Update(Contacto c)
    {
        using SqliteConnection con = Abrir();
        con.Update(c);
    }

    public void Delete(Contacto c)
    {
        using SqliteConnection con = Abrir();
        con.Delete(c);
    }
}
public class JsonAgendaIO {}

[Table("Contactos")]
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }
}
