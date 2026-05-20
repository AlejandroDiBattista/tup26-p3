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

// --- PUNTO DE ENTRADA (Top-level code temporal para probar) ---
var dbFile = args.Length > 0 ? args[0] : "agenda.db";
var store = new SqliteAgendaStore(dbFile);
store.Initialize();

Console.WriteLine($"Cimientos listos. Base de datos conectada en: {dbFile}");
return; // Frenamos la ejecución acá por ahora

// --- MODELO DE DATOS ---
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

// --- PERSISTENCIA (Base de Datos SQLite) ---
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