#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.Generic;
using System.Linq;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

// Punto de entrada: procesar argumentos, abrir la base y arrancar la app
string dbPath = args.Length > 0 ? args[0] : "agenda.db";

using IApplication app = Application.Create().Init();

SqliteAgendaStore store;
try {
    store = new SqliteAgendaStore(dbPath);
} catch (Exception ex) {
    MessageBox.ErrorQuery(app, "Error", $"No se pudo abrir la base de datos: {ex.Message}", "Ok");
    return;
}

List<Contacto> contacts = store.GetAll();
app.Run(new AgendaWindow(store, contacts, dbPath));


// Ventana principal
public sealed class AgendaWindow : Runnable {

    private readonly SqliteAgendaStore _store;
    private readonly List<Contacto> _contacts;
    private readonly string _dbPath;

    public AgendaWindow(SqliteAgendaStore store, List<Contacto> contacts, string dbPath) {
        _store    = store;
        _contacts = contacts;
        _dbPath   = dbPath;

        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Nuevo contacto", null!, AbrirDialogo),
                    null!, // Separador
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ])
            ]
        };

        Label infoLabel = new() {
            Text = $"Base: {_dbPath} ÔÇö {_contacts.Count} contacto(s) cargado(s)",
            X    = Pos.Center(),
            Y    = Pos.Center()
        };

        Button openButton = new() {
            Text = "_Abrir di├ílogo",
            X    = Pos.Center(),
            Y    = Pos.Bottom(infoLabel) + 1
        };

        openButton.Accepting += (_, e) => {
            AbrirDialogo();
            e.Handled = true;
        };

        Add(menu, infoLabel, openButton);
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

// Di├ílogo de ejemplo (se reemplazar├í por ContactDialog en una parte posterior)
public sealed class EjemploDialog : Dialog {
    public EjemploDialog() {
        Title  = "Di├ílogo de ejemplo";
        Width  = 50;
        Height = 8;

        Label message = new() {
            Text = "Este es un di├ílogo modal de ejemplo.",
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

    private readonly string _connectionString;

    public SqliteAgendaStore(string dbPath) {
        _connectionString = $"Data Source={dbPath}";
        EnsureSchema();
    }

    private SqliteConnection Open() {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureSchema() {
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

    public List<Contacto> GetAll() {
        using SqliteConnection db = Open();
        return db.GetAll<Contacto>().ToList();
    }

    public int Insert(Contacto contact) {
        using SqliteConnection db = Open();
        return (int)db.Insert(contact);
    }

    public bool Update(Contacto contact) {
        using SqliteConnection db = Open();
        return db.Update(contact);
    }

    public bool Delete(Contacto contact) {
        using SqliteConnection db = Open();
        return db.Delete(contact);
    }
}

public class JsonAgendaIO {}

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

    public Contacto Clone() => new() {
        Id        = Id,
        Nombre    = Nombre,
        Telefonos = Telefonos,
        Email     = Email,
        Notas     = Notas,
        Favorito  = Favorito
    };
}
