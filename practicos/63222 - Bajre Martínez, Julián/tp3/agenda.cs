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
using Terminal.Gui;
using System.Collections;
using System.Collections.ObjectModel;

using Microsoft.Data.Sqlite;

using Dapper;
using Dapper.Contrib.Extensions;

using System.Data.Common;
using System.Linq;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store = new(dbPath);

using IApplication app = Application.Create().Init();

app.Run(new AgendaWindow(store));

public sealed class AgendaWindow :  Window {
private readonly SqliteAgendaStore store;

private List<Contacto> contactos = [];

private ListView listaContactos = null!;

private List<Contacto> contactosFiltrados = [];

private TextField campoBusqueda = null!;

private TextView detalle = null!;

private StatusBar statusBar = null!;

private bool soloFavoritos = false;

public AgendaWindow(SqliteAgendaStore store) {

    this.store = store;

    contactos = store.ObtenerTodos();

    Title  = "Agenda - Terminal.Gui";
    Width  = Dim.Fill();
    Height = Dim.Fill();

    MenuBar menu = new() {
        Menus = [

            new MenuBarItem("_Archivo", [
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
            ]),

            new MenuBarItem("_Ver", [
                new MenuItem(
                    "_Solo favoritos",
                    "",
                    ToggleFavoritos
                )
            ])
        ]
    };

    Label buscarLabel = new() {
        Text = "Buscar:",
        X = 1,
        Y = 1
    };

    campoBusqueda = new TextField() {
        X = 10,
        Y = 1,
        Width = 30
    };

    campoBusqueda.TextChanged += (_, _) => {
        AplicarFiltros();
    };

listaContactos = new ListView()
{
    X = 0,
    Y = 3,
    Width = 30,
    Height = Dim.Fill(1)
};

    FrameView detalleFrame = new() {
        Title  = "Detalle",
        X      = 30,
        Y      = 3,
        Width  = Dim.Fill(),
        Height = Dim.Fill(1)
    };

    detalle = new TextView() {
        Width  = Dim.Fill(),
        Height = Dim.Fill(),
        ReadOnly = true
    };

    detalleFrame.Add(detalle);

    statusBar = new StatusBar([
        new Shortcut(Key.Q.WithCtrl, "Salir", SolicitarSalir)
    ]);

    Add(
        menu,
        buscarLabel,
        campoBusqueda,
        listaContactos,
        detalleFrame,
        statusBar
    );

    AplicarFiltros();
}

private void AplicarFiltros() {

        string texto =
            campoBusqueda.Text?.ToString()?.ToLower()
            ?? "";

        contactosFiltrados = contactos
            .Where(c =>

                (
                    c.Nombre.ToLower().Contains(texto)
                    || c.Telefonos.ToLower().Contains(texto)
                    || c.Email.ToLower().Contains(texto)
                )

                &&

                (
                    !soloFavoritos || c.Favorito
                )

            )
            .ToList();

        ActualizarLista();

        ActualizarDetalle();
    }
private void ActualizarLista()
{
    ObservableCollection<string> items =
    [
        .. contactosFiltrados.Select(c =>
            $"{(c.Favorito ? "★" : " ")} {c.Nombre}"
        )
    ];

    listaContactos.SetSource<string>(items);
}
private void ActualizarDetalle()
{
    int index = listaContactos.SelectedItem ?? -1;

    if (
        index < 0 ||
        index >= contactosFiltrados.Count
    )
    {
        detalle.Text = "";
        return;
    }

    Contacto c = contactosFiltrados[index];

    detalle.Text =
$"""
Nombre:
{c.Nombre}

Teléfonos:
{c.Telefonos}

Email:
{c.Email}

Favorito:
{(c.Favorito ? "Sí" : "No")}

Notas:
{c.Notas}
""";
}

    private void ToggleFavoritos() {

        soloFavoritos = !soloFavoritos;

        statusBar.Title = soloFavoritos
            ? "Filtro: solo favoritos"
            : "Filtro: todos";

        AplicarFiltros();
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {

        if (key == Key.Q.WithCtrl) {

            SolicitarSalir();

            return true;
        }

        if (key == Key.F4) {

            campoBusqueda.SetFocus();

            return true;
        }

        return base.OnKeyDown(key);
    }
}


public sealed class EjemploDialog : Dialog {
    public EjemploDialog() {
        Title  = "Diálogo";
        Width  = 50;
        Height = 8;

        Label message = new() {
            Text = "Ejemplo de Dialogo.",
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

    public SqliteAgendaStore(string dbPath) {
        this.dbPath = dbPath;
        Inicializar();
    }

    private DbConnection GetConnection() {
        return new SqliteConnection($"Data Source={dbPath}");
    }

    private void Inicializar() {

        using DbConnection connection = GetConnection();

        connection.Open();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL,
                Email TEXT NOT NULL,
                Notas TEXT NOT NULL,
                Favorito INTEGER NOT NULL
            );
        """);
    }

    public List<Contacto> ObtenerTodos() {

        using DbConnection connection = GetConnection();

        connection.Open();

        return connection.GetAll<Contacto>().ToList();
    }

    public void Insertar(Contacto contacto) {

        using DbConnection connection = GetConnection();

        connection.Open();

        connection.Insert(contacto);
    }

    public void Actualizar(Contacto contacto) {

        using DbConnection connection = GetConnection();

        connection.Open();

        connection.Update(contacto);
    }

    public void Eliminar(Contacto contacto) {

        using DbConnection connection = GetConnection();

        connection.Open();

        connection.Delete(contacto);
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
}