#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui;

var dbFile = args.Length > 0 ? args[0] : "agenda.db";
var store = new SqliteAgendaStore(dbFile);
store.Initialize();

Console.WriteLine($"Cimientos listos. Base de datos conectada en: {dbFile}");
Console.WriteLine("Motor JSON en linea.");
return;

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
            Id = this.Id,
            Nombre = this.Nombre,
            Telefonos = this.Telefonos,
            Email = this.Email,
            Notas = this.Notas,
            Favorito = this.Favorito
        };
    }
}

public sealed class SqliteAgendaStore
{
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbFile)
    {
        _connectionString = $"Data Source={dbFile}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            );");
    }

    public IEnumerable<Contacto> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.GetAll<Contacto>();
    }

    public long Insert(Contacto contacto)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Insert(contacto);
    }

    public bool Update(Contacto contacto)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Update(contacto);
    }

    public bool Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Delete(new Contacto { Id = id });
    }
}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<Contacto> Import(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var records = JsonSerializer.Deserialize<List<Contacto>>(json, Options);
        if (records == null) return new List<Contacto>();
        
        foreach (var r in records)
        {
            r.Id = 0;
        }
        return records;
    }

    public static void Export(string path, List<Contacto> contactos)
    {
        var json = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, json, Encoding.UTF8);
    }
}