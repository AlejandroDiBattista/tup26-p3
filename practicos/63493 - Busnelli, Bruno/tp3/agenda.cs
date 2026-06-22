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

/// ====
/// Trabajo Práctico 3 - AgendaT
/// Aplicación de agenda TUI con persistencia SQLite e import/export JSON.
/// ====

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store = new(dbPath);
store.Initialize();

// Punto de entrada
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));


// Ventana principal
public sealed class AgendaWindow : Runnable
{
    private readonly SqliteAgendaStore store;

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;

        Title = "AgendaT - Terminal.Gui";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Nuevo contacto", "F2 / Ctrl+N", AbrirDialogo),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ])
            ]
        };

        int cantidad = store.GetAll().Count;

        Label message = new()
        {
            Text = $"Base cargada correctamente. Contactos guardados: {cantidad}",
            X = Pos.Center(),
            Y = Pos.Center()
        };

        Add(menu, message);
    }

    private void AbrirDialogo()
    {
        EjemploDialog dialog = new();
        App!.Run(dialog);
    }

    private void SolicitarSalir()
    {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Q.WithCtrl)
        {
            SolicitarSalir();
            return true;
        }

        return base.OnKeyDown(key);
    }
}


// Diálogo de ejemplo
public sealed class EjemploDialog : Dialog
{
    public EjemploDialog()
    {
        Title = "Diálogo de ejemplo";
        Width = 50;
        Height = 8;

        Label message = new()
        {
            Text = "Este es un diálogo modal de ejemplo.",
            X = Pos.Center(),
            Y = 1
        };

        Button closeButton = new()
        {
            Text = "_Cerrar",
            IsDefault = true
        };

        closeButton.Accepting += (_, e) =>
        {
            App!.RequestStop();
            e.Handled = true;
        };

        Add(message);
        AddButton(closeButton);
    }
}


// Persistencia SQLite
public sealed class SqliteAgendaStore
{
    private readonly string connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using SqliteConnection connection = OpenConnection();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Notas TEXT NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            );
        """);
    }

    public List<Contacto> GetAll()
    {
        using SqliteConnection connection = OpenConnection();

        return connection
            .GetAll<Contacto>()
            .OrderByDescending(contacto => contacto.Favorito)
            .ThenBy(contacto => contacto.Nombre)
            .ToList();
    }

    public int Insert(Contacto contacto)
    {
        using SqliteConnection connection = OpenConnection();

        long id = connection.Insert(contacto);
        return (int)id;
    }

    public bool Update(Contacto contacto)
    {
        using SqliteConnection connection = OpenConnection();

        return connection.Update(contacto);
    }

    public bool Delete(Contacto contacto)
    {
        using SqliteConnection connection = OpenConnection();

        return connection.Delete(contacto);
    }

    private SqliteConnection OpenConnection()
    {
        SqliteConnection connection = new(connectionString);
        connection.Open();
        return connection;
    }
}


// Interoperabilidad JSON
public class JsonAgendaIO
{
}


// Modelo de datos
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

    public override string ToString()
    {
        return $"{(Favorito ? "★ " : "")}{Nombre}";
    }
}