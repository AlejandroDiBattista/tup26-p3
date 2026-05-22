#!/usr/bin/dotnet run

#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property PublishAot=false
#:property PublishTrimmed=false
#:property TrimMode=copyused
#:property EnableTrimAnalyzer=false

#:package Terminal.Gui@2.0.0-v2-develop.400
#:package Microsoft.Data.Sqlite@9.0.0
#:package Dapper@2.1.35
#:package Dapper.Contrib@2.0.78


using System;
using Terminal.Gui;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;


// ==========================================================
// TOP LEVEL CODE
// ==========================================================

SqlMapper.AddTypeHandler(new BooleanTypeHandler());

string archivoBaseDatos = args.Length > 0 ? args[0] : "agenda.db";

try
{
    SqliteAgendaStore store = new SqliteAgendaStore(archivoBaseDatos);

    Application.Init();

    AgendaWindow ventana = new AgendaWindow(store);
    Application.Run(ventana);
    Application.Shutdown();
}
catch (Exception ex)
{
    Console.WriteLine("Error al iniciar la aplicación:");
    Console.WriteLine(ex.Message);
}

// ==========================================================
// VENTANA PRINCIPAL
// ==========================================================

public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore store;
    private List<Contacto> contactos = new();
    
    // Componentes principales
    private readonly ListView listaContactos;
    private readonly TextView detalleContacto;

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;
        Title = "Agenda de Contactos TUI";
        Width = Dim.Fill();
        Height = Dim.Fill();

        contactos = store.ObtenerTodos();

        // LISTA
        listaContactos = new ListView()
        {
            X = 0, Y = 0, Width = 30, Height = Dim.Fill()
        };
        Add(listaContactos);

        // DETALLE
        detalleContacto = new TextView()
        {
            X = 31, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(),
            ReadOnly = true
        };
        Add(detalleContacto);
        
        // Cargar nombres en la lista
        listaContactos.SetSource(contactos.Select(c => c.Nombre).ToList());
    }
}
// ==========================================================
// SQLITE STORE
// ==========================================================

public sealed class SqliteAgendaStore
{
    private readonly string connectionString;

    public SqliteAgendaStore(string archivo)
    {
        connectionString = $"Data Source={archivo}";
        CrearTabla();
    }

    private void CrearTabla()
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        string sql = """
        CREATE TABLE IF NOT EXISTS Contactos(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Telefonos TEXT,
            Email TEXT,
            Notas TEXT,
            Favorito INTEGER NOT NULL
        );
        """;
        conexion.Execute(sql);
    }

    public List<Contacto> ObtenerTodos()
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        string sql = """
        SELECT Id, Nombre, Telefonos, Email, Notas, (Favorito = 1) AS Favorito 
        FROM Contactos 
        ORDER BY Nombre
        """;
        return conexion.Query<Contacto>(sql).ToList();
    }

    public void Insertar(Contacto contacto)
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        conexion.Insert(contacto);
    }

    public void Actualizar(Contacto contacto)
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        conexion.Update(contacto);
    }

    public void Eliminar(Contacto contacto)
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        conexion.Delete(contacto);
    }
}

// ==========================================================
// MODELO
// ==========================================================

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

    public Contacto Clone() => new Contacto()
    {
        Id = this.Id,
        Nombre = this.Nombre,
        Telefonos = this.Telefonos,
        Email = this.Email,
        Notas = this.Notas,
        Favorito = this.Favorito
    };
}

// ==========================================================
// SOPORTE INTERNO: BooleanTypeHandler
// ==========================================================

internal class BooleanTypeHandler : SqlMapper.TypeHandler<bool>
{
    public override void SetValue(System.Data.IDbDataParameter parameter, bool value)
    {
        parameter.Value = value ? 1 : 0;
    }

    public override bool Parse(object value)
    {
        if (value is long l) return l == 1;
        if (value is int i) return i == 1;
        return Convert.ToBoolean(value);
    }
}