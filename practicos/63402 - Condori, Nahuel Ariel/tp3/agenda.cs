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
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Data.Common;

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

[Table("Contactos")]
public class Contacto {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => (Contacto)MemberwiseClone();
    public override string ToString() => $"{(Favorito ? "★" : " ")} {Nombre}";
}

public class SqliteAgendaStore {
    private readonly string _connStr;

    public SqliteAgendaStore(string dbPath) {
        _connStr = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
    }

    public void InitDb() {
        using var conn = new SqliteConnection(_connStr);
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER
            )");
    }

    public IEnumerable<Contacto> GetAll() {
        using var conn = new SqliteConnection(_connStr);
        return conn.GetAll<Contacto>();
    }

    public void Insert(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        c.Id = (int)conn.Insert(c);
    }

    public void Update(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        conn.Update(c);
    }

    public void Delete(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        conn.Delete(c);
    }
}

public class JsonAgendaIO {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static void Export(string path, IEnumerable<Contacto> contactos) {
        var json = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, json);
    }

    public static IEnumerable<Contacto> Import(string path) {
        if (!File.Exists(path)) throw new FileNotFoundException("El archivo JSON indicado no existe.");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Contacto>>(json, Options) ?? [];
    }
}