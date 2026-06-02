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
using System.Text.Json;

/// ==== 
/// Este es un archivo de referencia con el esqueleto del proyecto.
/// No es un codigo de ejemplo, sino el punto de partida para el desarrollo del trabajo practico. 
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

    private bool _onlyFavorites;
    private TextField _searchField = null!;


    private ListView _listView = null!;
    private Label _detailName = null!;
    private Label _detailFav = null!;
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
                new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson),
                new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson),
                null!,
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
            ]),

            new MenuBarItem("_Contactos", [
                new MenuItem("_Nuevo", "F2", AbrirNuevo),
                new MenuItem("_Editar", "F3", AbrirEditar),
                new MenuItem("_Eliminar", "Del", EliminarContacto)
            ]),

            new MenuBarItem("_Ver", [
                new MenuItem("_Solo favoritos", "F4", ToggleFavoritos)
            ]),

            new MenuBarItem("_Ayuda", [
                new MenuItem("_Acerca de", "F1", MostrarAcercaDe)
            ])

            
        ]
    };
    Label searchLabel = new()
{
    Text = "Buscar:",
    X = 0,
    Y = 1
};

_searchField = new TextField()
{
    X = Pos.Right(searchLabel),
    Y = 1,
    Width = Dim.Fill()
};

_searchField.TextChanged += (_, _) => AplicarFiltro();

    FrameView listFrame = new() {
        Title = "Contactos",
        X = 0,
        Y = 2,
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
    _detailFav = new Label() { X = 0, Y = 1 };
    _detailPhone = new Label() { X = 0, Y = 2 };
    _detailEmail = new Label() { X = 0, Y = 3 };
    _detailNotes = new Label() { X = 0, Y = 4 };

    detailFrame.Add(
        _detailName,
        _detailFav,
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

    Add(menu, searchLabel, _searchField, listFrame, detailFrame, _statusBar);

    CargarContactos();
    }
    private void CargarContactos()
{
    _contacts = _store.GetAll().ToList();
    AplicarFiltro();

    
}
private void AplicarFiltro()
{
    string q = _searchField.Text?.ToString()?.ToLowerInvariant() ?? "";

    _filteredContacts = _contacts
        .Where(c => !_onlyFavorites || c.Favorito)
        .Where(c =>
            c.Nombre.ToLowerInvariant().Contains(q)
            || c.Email.ToLowerInvariant().Contains(q)
            || c.Telefonos.ToLowerInvariant().Contains(q))
        .ToList();
        _listView.SetSource<string>(new(
    _filteredContacts
        .Select(c =>
            $"{(c.Favorito ? "* " : "")}{c.Nombre}")
        .ToList()
    ));

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
        _detailFav.Text = "";
        _detailPhone.Text = "";
        _detailEmail.Text = "";
        _detailNotes.Text = "";
        return;
    }

    _detailName.Text = $"Nombre: {c.Nombre}";
    _detailFav.Text = $"Favorito: {(c.Favorito ? "Si" : "No")}";
    _detailPhone.Text = $"Tel: {c.Telefonos}";
    _detailEmail.Text = $"Email: {c.Email}";
    _detailNotes.Text = $"Notas: {c.Notas}";
}


    private void AbrirNuevo() {
        ContactDialog dialog = new(new Contacto());
        App!.Run(dialog);
         if (!dialog.Confirmed || dialog.ContactResult is null)
        return;

    Contacto c = dialog.ContactResult;
    _store.Insert(c);
    _contacts.Add(c);
    CargarContactos();
    }   

    private void AbrirEditar() {
        Contacto? seleccionado = ContactoSeleccionado();
        if (seleccionado is null)
            return;

        ContactDialog dialog = new(seleccionado);
        App!.Run(dialog);
        if (!dialog.Confirmed || dialog.ContactResult is null)
            return;

        seleccionado.Nombre = dialog.ContactResult.Nombre;
        seleccionado.Telefonos = dialog.ContactResult.Telefonos;
        seleccionado.Email = dialog.ContactResult.Email;
        seleccionado.Notas = dialog.ContactResult.Notas;
        seleccionado.Favorito = dialog.ContactResult.Favorito;
        _store.Update(seleccionado);
        CargarContactos();
        MostrarDetalle(seleccionado);
    }

    private void EliminarContacto() {
        Contacto? seleccionado = ContactoSeleccionado();
        if (seleccionado is null)
            return;

        _store.Delete(seleccionado);
        CargarContactos();
        MostrarDetalle(ContactoSeleccionado());
    }

    private void ToggleFavoritos()
    {
        _onlyFavorites = !_onlyFavorites;
        AplicarFiltro();
        MostrarDetalle(ContactoSeleccionado());
        _statusBar.Text = _onlyFavorites
            ? "Mostrando solo favoritos."
            : "Mostrando todos los contactos.";
    }

    private void MostrarAcercaDe()
    {
        MessageBox.Query(
            App!,
            "Acerca de",
            "Agenda de contactos - TP3",
            "OK"
        );
    }

    private void ImportarJson()
{
    try
    {
        List<Contacto> imported =
            JsonAgendaIO.Read("contactos.json");

        foreach (Contacto c in imported)
        {
            c.Id = 0;

            _store.Insert(c);

            _contacts.Add(c);
        }

        CargarContactos();
    }
    catch
    {
        MessageBox.ErrorQuery(
            App!,
            "Error",
            "No se pudo importar JSON.",
            "OK"
        );
    }
}

private void ExportarJson()
{
    try
    {
        JsonAgendaIO.Write(
            "contactos.json",
            _contacts
        );
    }
    catch
    {
        MessageBox.ErrorQuery(
            App!,
            "Error",
            "No se pudo exportar JSON.",
            "OK"
        );
    }
}

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            SolicitarSalir();
            return true;
        }
        if (key == Key.F2) {
            AbrirNuevo();
            return true;
        }
        if (key == Key.F3) {
            AbrirEditar();
            return true;
        }
        if (key == Key.Delete) {
            EliminarContacto();
            return true;
        }
        if (key == Key.F4) {
            ToggleFavoritos();
            return true;
        }
        if (key == Key.F1) {
            MostrarAcercaDe();
            return true;
        }
        if (key == Key.I.WithCtrl) {
            ImportarJson();
            return true;
        }
        if (key == Key.E.WithCtrl) {
            ExportarJson();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class ContactDialog : Dialog
{
    public bool Confirmed { get; private set; }

    public Contacto? ContactResult { get; private set; }

    private readonly TextField _nameField;
    private readonly TextField[] _phoneFields = new TextField[5];
    private readonly TextField _emailField;
    private readonly TextView _notesField;
    private readonly CheckBox _favCheck;

    public ContactDialog(Contacto initial)
    {
        Title = "Contacto";

        Width = 70;
        Height = 24;

        Add(new Label { Text = "Nombre (*):", X = 1, Y = 1 });

        _nameField = new TextField
        {
            Text = initial.Nombre,
            X = 15,
            Y = 1,
            Width = Dim.Fill(2),
        };

        Add(_nameField);

        string[] phones = initial.Telefonos.Split(',');

        Add(new Label { Text = "Telefonos:", X = 1, Y = 3 });

        for (int i = 0; i < 5; i++)
        {
            Add(new Label {
                Text = $"{i + 1}:",
                X = 1,
                Y = 4 + i
            });

            _phoneFields[i] = new TextField
            {
                Text = i < phones.Length
                    ? phones[i].Trim()
                    : "",

                X = 6,
                Y = 4 + i,
                Width = 30
            };

            Add(_phoneFields[i]);
        }

        Add(new Label { Text = "Email:", X = 1, Y = 10 });

        _emailField = new TextField
        {
            Text = initial.Email,
            X = 15,
            Y = 10,
            Width = Dim.Fill(2),
        };

        Add(_emailField);

        _favCheck = new CheckBox
        {
            Text = "Favorito",
            Value = initial.Favorito ? CheckState.Checked : CheckState.UnChecked,
            X = 1,
            Y = 12
        };

        Add(_favCheck);

        Add(new Label { Text = "Notas:", X = 1, Y = 14 });

        FrameView notesFrame = new()
        {
            X = 1,
            Y = 15,
            Width = Dim.Fill(2),
            Height = 4
        };

        _notesField = new TextView
        {
            Text = initial.Notas,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        notesFrame.Add(_notesField);

        Add(notesFrame);

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

        Button btnCancelar = new()
        {
            Text = "_Cancelar"
        };

        btnCancelar.Accepting += (_, e) =>
        {
            App!.RequestStop();
            e.Handled = true;
        };

        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }

    private void Guardar()
    {
        string nombre =
            _nameField.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.ErrorQuery(
                App!,
                "Validacion",
                "El nombre no puede estar vacio.",
                "OK"
            );

            return;
        }

        string email =
            _emailField.Text?.ToString()?.Trim() ?? "";

        if (!string.IsNullOrEmpty(email)
            && !email.Contains('@'))
        {
            MessageBox.ErrorQuery(
                App!,
                "Validacion",
                "El email debe contener @.",
                "OK"
            );

            return;
        }

        List<string> telefonos = _phoneFields
            .Select(f => f.Text?.ToString()?.Trim() ?? "")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        ContactResult = new Contacto
        {
            Nombre = nombre,
            Telefonos = string.Join(", ", telefonos),
            Email = email,
            Notas = _notesField.Text?.ToString() ?? "",
            Favorito = _favCheck.Value == CheckState.Checked
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
public class JsonAgendaIO 
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true
    };

    public static List<Contacto> Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException();
        }

        string json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<List<Contacto>>(json, Opts)
            ?? [];
    }

    public static void Write(string path, IEnumerable<Contacto> contacts)
    {
        string json = JsonSerializer.Serialize(
            contacts,
            Opts
        );

        File.WriteAllText(path, json);
    }
}


[Table("Contactos")]
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }
}
