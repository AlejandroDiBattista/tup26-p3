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
using System.Collections.ObjectModel;

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
var dbPath = "agenda.db";
var store = new SqliteAgendaStore(dbPath);

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));

// Ventana principal
public sealed class AgendaWindow : Runnable {

    private readonly SqliteAgendaStore store;
    private List<Contacto> contacts = [];

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;

        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;

        CargarContactos();
        BuildLayout();
    }
    private void CargarContactos() {
    contacts = store.GetAll();
}
   private void BuildLayout() {
    MenuBar menu = new() {
        Menus = [
            new MenuBarItem("_Archivo", [
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
            ])
        ]
    };

    var listView = new ListView() {
        X = 0,
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };

    listView.SetSource(
        new ObservableCollection<string>(
            contacts.Select(c => c.Nombre)
        )
    );

    Add(menu, listView);
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


public class SqliteAgendaStore {
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbPath = "agenda.db") {
        _connectionString = $"Data Source={dbPath}";
        CrearTablaSiNoExiste();
    }

    private DbConnection GetConnection() {
        return new SqliteConnection(_connectionString);
    }

    private void CrearTablaSiNoExiste() {
        using var conn = GetConnection();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL
            );
        ");
    }

    public List<Contacto> GetAll() {
        using var conn = GetConnection();
        return conn.GetAll<Contacto>().ToList();
    }

    public long Insert(Contacto c) {
        using var conn = GetConnection();
        return conn.Insert(c);
    }

    public bool Update(Contacto c) {
        using var conn = GetConnection();
        return conn.Update(c);
    }

    public bool Delete(Contacto c) {
        using var conn = GetConnection();
        return conn.Delete(c);
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
        return (Contacto)this.MemberwiseClone();
    }
}