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


string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store;
try
{
    store = new SqliteAgendaStore(dbPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al abrir la base de datos '{dbPath}': {ex.Message}");
    return 1;
}

using (store)
{
    using IApplication app = Application.Create().Init();
    app.Run(new AgendaWindow(store)); 
}
return 0;



public sealed class AgendaWindow : Runnable 
{
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _contacts = new();
    private List<Contacto> _filteredContacts = new();
    private bool _soloFavoritos = false;

    private TextField _searchField = null!;
    private ListView _listView = null!;
    private TextView _detailView = null!;
    private Label _statusLabel = null!;
    private MenuItem _soloFavoritosMenuItem = null!;

    public AgendaWindow(SqliteAgendaStore store)
    {
        _store = store;
        Title = "AgendaT";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }

    private void BuildLayout()
    {
        _soloFavoritosMenuItem = new MenuItem("_Solo favoritos", "", MenuToggleFavoritos);

        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", MenuImportar),
                    new MenuItem("_Exportar JSON", "Ctrl+E", MenuExportar),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", MenuSalir),
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo",    "F2/Ctrl+N",  MenuNuevo),
                    new MenuItem("_Editar",   "F3/Enter",   MenuEditar),
                    new MenuItem("_Eliminar", "Del/Ctrl+D", MenuEliminar),
                ]),
                new MenuBarItem("_Ver",
                [
                    _soloFavoritosMenuItem,
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", "", MenuAcercaDe),
                ]),
            ]
        };

        Label searchLabel = new()
        {
            Text = "Buscar: ",
            X = 0,
            Y = 1,
        };

        _searchField = new TextField()
        {
            X = Pos.Right(searchLabel),
            Y = 1,
            Width = Dim.Fill(),
        };

        FrameView listFrame = new()
        {
            Title = "Contactos",
            X = 0,
            Y = 2,
            Width = Dim.Percent(40),
            Height = Dim.Fill(2),
        };

        _listView = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        listFrame.Add(_listView);

        FrameView detailFrame = new()
        {
            Title = "Detalle",
            X = Pos.Right(listFrame),
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };

        _detailView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
        };
        detailFrame.Add(_detailView);

        _statusLabel = new Label()
        {
            Text = "F2/Ctrl+N:Nuevo  F3/Enter:Editar  Del/Ctrl+D:Eliminar  Ctrl+I/E:JSON  F4:Buscar  Ctrl+Q:Salir",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };

        Add(menu, searchLabel, _searchField, listFrame, detailFrame, _statusLabel);
    }
    
    private void MenuToggleFavoritos() {}
    private void MenuImportar() {}
    private void MenuExportar() {}
    private void MenuSalir() {}
    private void MenuNuevo() {}
    private void MenuEditar() {}
    private void MenuEliminar() {}
    private void MenuAcercaDe() {}
}


public sealed class ContactDialog : Dialog
{
    public bool WasAccepted { get; private set; } = false;
    public Contacto? ContactResult { get; private set; }

    private readonly TextField _nombreField;
    private readonly TextField[] _telefonoFields = new TextField[5];
    private readonly TextField _emailField;
    private readonly TextView _notasField;
    private readonly CheckBox _favoritoCheck;

    public ContactDialog(string title, Contacto contacto)
    {
        Title = title;
        Width = 62;
        Height = 27;

        int row = 0;

        Add(new Label() { Text = "Nombre (*):", X = 1, Y = row });
        row++;
        _nombreField = new TextField()
        {
            Text = contacto.Nombre,
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
        };
        Add(_nombreField);
        row++;

        Add(new Label() { Text = "Teléfonos (hasta 5, uno por campo):", X = 1, Y = row });
        row++;
        string[] teleparts = contacto.Telefonos.Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++)
        {
            Add(new Label() { Text = $"  Tel {i + 1}:", X = 1, Y = row });
            string val = i < teleparts.Length ? teleparts[i] : "";
            _telefonoFields[i] = new TextField()
            {
                Text = val,
                X = 10,
                Y = row,
                Width = Dim.Fill(1),
            };
            Add(_telefonoFields[i]);
            row++;
        }

        Add(new Label() { Text = "Email:", X = 1, Y = row });
        row++;
        _emailField = new TextField()
        {
            Text = contacto.Email,
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
        };
        Add(_emailField);
        row++;

        _favoritoCheck = new CheckBox()
        {
            Text = "Favorito",
            X = 1,
            Y = row,
            Value = contacto.Favorito
                ? CheckState.Checked
                : CheckState.UnChecked,
        };
        Add(_favoritoCheck);
        row++;

        Add(new Label() { Text = "Notas:", X = 1, Y = row });
        row++;
        _notasField = new TextView()
        {
            Text = contacto.Notas,
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
            Height = 3,
        };
        Add(_notasField);

        Button btnGuardar = new() { Text = "_Guardar", IsDefault = true };
        btnGuardar.Accepting += (_, e) => { OnGuardar(); e.Handled = true; };

        Button btnCancelar = new() { Text = "_Cancelar" };
        btnCancelar.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };

        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }

    private void OnGuardar()
    {
        string nombre = _nombreField.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(nombre))
        {
            MessageBox.ErrorQuery(App!, "Validación", "El nombre no puede estar vacío.", "Aceptar");
            return;
        }

        string email = _emailField.Text?.Trim() ?? "";
        if (!string.IsNullOrEmpty(email) && !email.Contains('@'))
        {
            MessageBox.ErrorQuery(App!, "Validación", "El email debe contener '@'.", "Aceptar");
            return;
        }

        var telefonos = _telefonoFields
            .Select(f => f.Text?.Trim() ?? "")
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        ContactResult = new Contacto
        {
            Nombre = nombre,
            Telefonos = string.Join(", ", telefonos),
            Email = email,
            Notas = _notasField.Text?.ToString() ?? "",
            Favorito = _favoritoCheck.Value == CheckState.Checked,
        };

        WasAccepted = true;
        App!.RequestStop();
    }
}


public sealed class SqliteAgendaStore : IDisposable
{
    private readonly SqliteConnection _conn;

    public SqliteAgendaStore(string dbPath)
    {
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        _conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT    NOT NULL DEFAULT '',
                Telefonos TEXT    NOT NULL DEFAULT '',
                Email     TEXT    NOT NULL DEFAULT '',
                Notas     TEXT    NOT NULL DEFAULT '',
                Favorito  INTEGER NOT NULL DEFAULT 0
            )");
    }

    public List<Contacto> GetAll()
        => _conn.GetAll<Contacto>().ToList();

    public void Insert(Contacto c)
    {
        long id = _conn.Insert(c);
        c.Id = (int)id;
    }

    public void Update(Contacto c)
        => _conn.Update(c);

    public void Delete(Contacto c)
        => _conn.Delete(c);

    public void Dispose()
        => _conn.Dispose();
}

public class JsonAgendaIO { }

[Table("Contactos")]
public class Contacto {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }
}