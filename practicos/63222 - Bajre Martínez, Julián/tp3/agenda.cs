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

    public AgendaWindow(SqliteAgendaStore store) 
    {
        this.store = store;

        contactos = store.ObtenerTodos();

        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ])
            ]
        };

        listaContactos = new ListView() {
            X      = 0,
            Y      = 1,
            Width  = 30,
            Height = Dim.Fill()
        };

        ActualizarLista();

        FrameView detalle = new() {
            Title  = "Detalle",
            X      = 30,
            Y      = 1,
            Width  = Dim.Fill(),
            Height = Dim.Fill()
        };

        Add(menu, listaContactos, detalle);
    }

    private void ActualizarLista() {

        listaContactos.SetSource(
            contactos.Select(c =>
                $"{(c.Favorito ? "★" : " ")} {c.Nombre}"
            ).ToList()
        );
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