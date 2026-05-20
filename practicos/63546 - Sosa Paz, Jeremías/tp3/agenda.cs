#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

#pragma warning disable CS0618
#pragma warning disable CS8618

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data.Common;
using Dapper.Contrib.Extensions;

var dbFile = args.Length > 0 ? args[0] : "agenda.db";
var store = new SqliteAgendaStore(dbFile);
store.Initialize();

Application.Init();
var testDialog = new ContactDialog(new Contacto());
Application.Run(testDialog);
Application.Shutdown();
return;

public sealed class ContactDialog : Dialog
{
    public Contacto ContactResult { get; private set; }
    public bool SaveConfirmed { get; private set; } = false;

    private TextField _txtNombre;
    private TextField _txtEmail;
    private Button _btnFavorito;
    private bool _isFavorito = false;
    private TextView _txtNotas;
    private readonly TextField[] _txtTelefonos = new TextField[5];

    public ContactDialog(Contacto contacto)
    {
        ContactResult = contacto;
        Title = contacto.Id == 0 ? "Nuevo Contacto" : "Editar Contacto";
        Width = 65;
        Height = 20;

        InitControls();
        LoadContact();
    }

    private void InitControls()
    {
        Add(new Label { Text = "Nombre:", X = 2, Y = 1 });
        _txtNombre = new TextField { Text = "", X = 12, Y = 1, Width = 48 };
        Add(_txtNombre);

        Add(new Label { Text = "Email:", X = 2, Y = 3 });
        _txtEmail = new TextField { Text = "", X = 12, Y = 3, Width = 48 };
        Add(_txtEmail);

        _btnFavorito = new Button { Text = "[ ] Marcar como Favorito", X = 12, Y = 5 };
        _btnFavorito.Accepting += (s, e) => {
            _isFavorito = !_isFavorito;
            _btnFavorito.Text = _isFavorito ? "[X] Marcar como Favorito" : "[ ] Marcar como Favorito";
            e.Handled = true; 
        };
        Add(_btnFavorito);

        Add(new Label { Text = "Teléfonos:", X = 2, Y = 7 });
        for (int i = 0; i < 5; i++)
        {
            _txtTelefonos[i] = new TextField { Text = "", X = 12 + (i * 9), Y = 7, Width = 8 };
            Add(_txtTelefonos[i]);
        }

        Add(new Label { Text = "Notas:", X = 2, Y = 9 });
        _txtNotas = new TextView { X = 12, Y = 9, Width = 48, Height = 4 };
        Add(_txtNotas);

        var btnSave = new Button { Text = "Guardar", X = 18, Y = 15 };
        var btnCancel = new Button { Text = "Cancelar", X = 35, Y = 15 };

        btnCancel.Accepting += (s, e) => Application.RequestStop();
        btnSave.Accepting += (s, e) => OnValidateAndSave();

        Add(btnSave, btnCancel);
    }

    private void LoadContact()
    {
        _txtNombre.Text = ContactResult.Nombre ?? "";
        _txtEmail.Text = ContactResult.Email ?? "";
        
        _isFavorito = ContactResult.Favorito;
        _btnFavorito.Text = _isFavorito ? "[X] Marcar como Favorito" : "[ ] Marcar como Favorito";
        
        _txtNotas.Text = ContactResult.Notas ?? "";

        var parts = ContactResult.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++)
        {
            _txtTelefonos[i].Text = i < parts.Length ? parts[i].Trim() : "";
        }
    }

    private void OnValidateAndSave()
    {
        var nombre = _txtNombre.Text?.ToString()?.Trim() ?? "";
        var email = _txtEmail.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrEmpty(nombre))
        {
            MessageBox.ErrorQuery((IApplication)null!, "Error de Validación", "El Nombre no puede estar vacío.", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
        {
            MessageBox.ErrorQuery((IApplication)null!, "Error de Validación", "El Email debe contener un carácter '@'.", "OK");
            return;
        }

        ContactResult.Nombre = nombre;
        ContactResult.Email = email;
        ContactResult.Favorito = _isFavorito;
        ContactResult.Notas = _txtNotas.Text?.ToString()?.Trim() ?? "";

        var phoneList = _txtTelefonos
            .Select(t => t.Text?.ToString()?.Trim() ?? "")
            .Where(s => !string.IsNullOrEmpty(s));
        
        ContactResult.Telefonos = string.Join(",", phoneList);

        SaveConfirmed = true;
        Application.RequestStop();
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