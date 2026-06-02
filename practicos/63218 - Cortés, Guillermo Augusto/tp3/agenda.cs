#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@*
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
using System.Linq;
using System.Data.Common;
using System.Collections.ObjectModel;
using Dapper.Contrib.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
string dbFile = Environment.GetCommandLineArgs().Length > 1 ? Environment.GetCommandLineArgs()[1] : "agenda.db";
SqliteAgendaStore store = new(dbFile);
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));


// Ventana principal
public sealed class AgendaWindow : Runnable {

    private readonly SqliteAgendaStore store;

    private List<Contacto> contacts = new List<Contacto>();
    private List<Contacto> filteredContacts = new List<Contacto>();

    private ListView contactsList = null!;
    private TextField searchField = null!;
    private TextView detailView = null!;
    private Label statusBar = null!;

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();
        
        Menu.DefaultBorderStyle = LineStyle.Single;
        contacts = store.ObtenerContactos();
        filteredContacts = contacts.ToList();
        
        BuildLayout();
        RefreshList();
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus = [
                new MenuBarItem("_Archivo", [
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ])
            ]
        };

        Label lblBuscar = new()
        {
            X = 1,
            Y = 2,
            Text = "Buscar:"
        };

        searchField = new TextField()
        {
            X = 10,
            Y = 2,
            Width = 30
        };

        searchField.TextChanged += (_, _) => ApplyFilters();

        contactsList = new ListView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        FrameView listaFrame = new()
        {
            Title = " Contactos ",
            X = 1,
            Y = 4,
            Width = 35,
            Height = Dim.Fill() - 2
        };

        listaFrame.Add(contactsList);

        detailView = new TextView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true
        };

        FrameView detalleFrame = new()
        {
            Title = " Detalle ",
            X = Pos.Right(listaFrame),
            Y = 4,
            Width = Dim.Fill() - 1,
            Height = Dim.Fill() - 2
        };

        detalleFrame.Add(detailView);

        statusBar = new Label()
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "Agenda iniciada correctamente"
        };

        Add(menu, lblBuscar, searchField, listaFrame, detalleFrame, statusBar);
}
    private void RefreshList()
    {
        contactsList.SetSource<string>(new ObservableCollection<string>(filteredContacts.Select(c => c.ToString()).ToList()));

        UpdateDetail();
    }

    private void ApplyFilters()
    {
        string filtro = searchField.Text?.ToString()?.ToLower() ?? "";

        filteredContacts = contacts.Where(c => c.Nombre.ToLower().Contains(filtro) || c.Telefonos.ToLower().Contains(filtro) || c.Email.ToLower().Contains(filtro)).ToList();

        RefreshList();
    }
    private void UpdateDetail()
    {
        if (filteredContacts.Count == 0)
        {
            detailView.Text = "";
            return;
        }

        if (contactsList.SelectedItem == null || contactsList.SelectedItem < 0)
        {
            detailView.Text = "";
            return;
        }

        int idx = contactsList.SelectedItem!.Value;
        if (idx < 0 || idx >= filteredContacts.Count) {
            detailView.Text = "";
            return;
        }

        Contacto c = filteredContacts[idx];

        detailView.Text = $"""
                            Nombre: {c.Nombre},
                            Teléfonos:{c.Telefonos},
                            Email:{c.Email},
                            Favorito:{(c.Favorito ? "Sí" : "No")},
                            Notas:{c.Notas}
                            """;
    }
    private void AbrirDialogo() {
        EjemploDialog dialog = new();
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

// Diálogo de ejemplo
public sealed class EjemploDialog : Dialog {
    public EjemploDialog() {
        Title  = "Diálogo de ejemplo";
        Width  = 50;
        Height = 8;

        Label message = new() {
            Text = "Este es un diálogo modal de ejemplo.",
            X    = Pos.Center(),
            Y    = 1
        };

        Button closeButton = new() {
            Text      = "_Cerrar",
            IsDefault = true
        };

        closeButton.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        Add(message);
        AddButton(closeButton);
    }
}


public sealed class SqliteAgendaStore {
    private readonly string dbPath;
    private readonly string connectionString;

    public SqliteAgendaStore(string dbPath) {
        this.dbPath = dbPath;
        connectionString = $"Data Source={dbPath}";
        Iniciar();
    } 
    private void Iniciar() {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            )
        """);
    }
    private SqliteConnection GetConnection() {
        return new SqliteConnection(connectionString);
    }
    public List<Contacto> ObtenerContactos() {
        using var cn = GetConnection();
        cn.Open();
        return cn.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
    }
    public long Insert(Contacto contacto) {
        using var cn = GetConnection();
        return cn.Insert(contacto);
    }
    public bool Update(Contacto contacto) {
        using var cn = GetConnection();
        return cn.Update(contacto);
    }
    public bool Delete(Contacto contacto) {
        using var cn = GetConnection();
        return cn.Delete(contacto);
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

          public Contacto Clone() {
            return new Contacto {
                Id = Id,
                Nombre = Nombre,
                Telefonos = Telefonos,
                Email = Email,
                Notas = Notas,
                Favorito = Favorito
            };
        }
        public override string ToString()
        {
            return Favorito ? $"★ {Nombre}" : $"  {Nombre}";
        }      
}