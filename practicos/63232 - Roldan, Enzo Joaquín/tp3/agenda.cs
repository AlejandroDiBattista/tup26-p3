#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

Console.WriteLine("AgendaT — infraestructura lista.");

public sealed class SqliteAgendaStore {

    private readonly string cs;

    public SqliteAgendaStore(string path) {
        cs = $"Data Source={path}";
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

    private SqliteConnection Open() {
        SqliteConnection db = new(cs);
        db.Open();
        return db;
    }

    public List<Contacto> GetAll() { using SqliteConnection db = Open(); return db.GetAll<Contacto>().ToList(); }
    public void Insert(Contacto c) { using SqliteConnection db = Open(); c.Id = (int)db.Insert(c); }
    public void Update(Contacto c) { using SqliteConnection db = Open(); db.Update(c); }
    public void Delete(int id)     { using SqliteConnection db = Open(); db.Delete(new Contacto { Id = id }); }
}

public static class JsonAgendaIO {

    private static readonly JsonSerializerOptions Opts = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static List<Contacto> Import(string path) {
        if (!File.Exists(path)) throw new FileNotFoundException($"Archivo no encontrado: {path}");
        return JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(path), Opts) ?? [];
    }

    public static void Export(string path, List<Contacto> contactos) {
        File.WriteAllText(path, JsonSerializer.Serialize(contactos, Opts));
    }
}

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int    Id        { get; set; }
         public string Nombre    { get; set; } = "";
         public string Telefonos { get; set; } = "";
         public string Email     { get; set; } = "";
         public string Notas     { get; set; } = "";
         public bool   Favorito  { get; set; }

    public Contacto Clone() => new() {
        Id = Id, Nombre = Nombre, Telefonos = Telefonos,
        Email = Email, Notas = Notas, Favorito = Favorito
    };
}
