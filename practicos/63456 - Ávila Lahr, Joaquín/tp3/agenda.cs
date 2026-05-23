#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

Console.OutputEncoding = System.Text.Encoding.UTF8;
string archivo = args.Length > 0 ? args[0] : "agenda.db";
SqliteAgendaStore store = new($"Data Source={archivo}");

try {
    store.Inicializar();
}
catch (Exception ex) {
    Console.WriteLine("Error al abrir la base de datos:");
    Console.WriteLine(ex.Message);
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));

public sealed class SqliteAgendaStore {
    readonly string connectionString;

    public SqliteAgendaStore(string connectionString) {
        this.connectionString = connectionString;
    }

    SqliteConnection Abrir() {
        SqliteConnection conexion = new(connectionString);
        conexion.Open();
        return conexion;
    }

    public void Inicializar() {
        using SqliteConnection db = Abrir();
        db.Execute("""
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

    public List<Contacto> Listar() {
        using SqliteConnection db = Abrir();
        return db.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
    }

    public void Insertar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Insert(c);
    }

    public void Actualizar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Update(c);
    }

    public void Eliminar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Delete(c);
    }
}
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