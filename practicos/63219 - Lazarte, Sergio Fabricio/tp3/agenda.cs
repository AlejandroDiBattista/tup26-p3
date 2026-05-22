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
using System.Text.Json;
using System.Collections.ObjectModel;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store;
try
{
    store = new SqliteAgendaStore(dbPath);
    store.EnsureSchema();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al abrir la base de datos '{dbPath}': {ex.Message}");
    Environment.Exit(1);
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));

[Table("Contactos")]
public sealed class Contacto
{
    [Key] public int    Id        { get; set; }
         public string  Nombre    { get; set; } = "";
         public string  Telefonos { get; set; } = "";
         public string  Email     { get; set; } = "";
         public string  Notas     { get; set; } = "";
         public bool    Favorito  { get; set; }

    public Contacto Clone() => new()
    {
        Id = Id, Nombre = Nombre, Telefonos = Telefonos,
        Email = Email, Notas = Notas, Favorito = Favorito
    };
}

public sealed class SqliteAgendaStore
{
    public string DbPath { get; }
    public SqliteAgendaStore(string dbPath) => DbPath = dbPath;

    private SqliteConnection Open() => new($"Data Source={DbPath}");

    public void EnsureSchema()
    {
        using var cn = Open();
        cn.Open();
        cn.Execute(@"CREATE TABLE IF NOT EXISTS Contactos (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre    TEXT    NOT NULL DEFAULT '',
            Telefonos TEXT    NOT NULL DEFAULT '',
            Email     TEXT    NOT NULL DEFAULT '',
            Notas     TEXT    NOT NULL DEFAULT '',
            Favorito  INTEGER NOT NULL DEFAULT 0
        );");
    }

    public IEnumerable<Contacto> GetAll()  { using var cn = Open(); return cn.GetAll<Contacto>().ToList(); }
    public void Insert(Contacto c)         { using var cn = Open(); c.Id = (int)cn.Insert(c); }
    public void Update(Contacto c)         { using var cn = Open(); cn.Update(c); }
    public void Delete(Contacto c)         { using var cn = Open(); cn.Delete(c); }
}