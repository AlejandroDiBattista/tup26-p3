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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====
string databasePath = args.Length > 0 ? args[0] : "agenda.db";
// Punto de entrada
try {
    using SqliteAgendaStore store = new(databasePath);
    using IApplication app = Application.Create().Init();
    app.Run(new AgendaWindow(store));
}
catch (Exception ex) {
    Console.Error.WriteLine($"No se pudo iniciar la agenda: {ex.Message}");
    Environment.ExitCode = 1;
}

// Ventana principal
public sealed class AgendaWindow : Runnable {

    public AgendaWindow() {
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

        Button openButton = new() {
            Text = "_Abrir diálogo",
            X    = Pos.Center(),
            Y    = Pos.Center()
        };

        openButton.Accepting += (_, e) => {
            AbrirDialogo();
            e.Handled = true;
        };

        Add(menu, openButton);
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

/* 6. clase SqliteAgendaStore */
public sealed class SqliteAgendaStore : IDisposable {
    private readonly SqliteConnection connection;

    public string DatabasePath { get; }

    public SqliteAgendaStore(string databasePath) {
        DatabasePath = databasePath;
        SqliteConnectionStringBuilder builder = new() {
            DataSource = databasePath
        };

        connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        EnsureSchema();
    }

    public IEnumerable<Contacto> GetAll() {
        return connection.GetAll<Contacto>();
    }

    public int Insert(Contacto contact) {
        Validate(contact);
        long id = connection.Insert(contact);
        return checked((int)id);
    }

    public void Update(Contacto contact) {
        Validate(contact);
        connection.Update(contact);
    }

    public void Delete(Contacto contact) {
        connection.Delete(contact);
    }

    public void Dispose() {
        connection.Dispose();
    }

    private void EnsureSchema() {
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Notas TEXT NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            );
            """);
    }

    private static void Validate(Contacto contact) {
        if (string.IsNullOrWhiteSpace(contact.Nombre)) {
            throw new InvalidOperationException("El nombre no puede estar vacio.");
        }

        if (!string.IsNullOrWhiteSpace(contact.Email) && !contact.Email.Contains('@')) {
            throw new InvalidOperationException("El email debe contener @.");
        }
    }
}

/* 7. clase JsonAgendaIO */
public class JsonAgendaIO {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static IReadOnlyList<Contacto> Read(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException("El archivo JSON no existe.", path);
        }

        try {
            string json = File.ReadAllText(path, Encoding.UTF8);
            List<Contacto>? contacts = JsonSerializer.Deserialize<List<Contacto>>(json, Options);
            return contacts?.Select(c => {
                c.Id = 0;
                c.Nombre = c.Nombre?.Trim() ?? "";
                c.Telefonos ??= "";
                c.Email ??= "";
                c.Notas ??= "";
                return c;
            }).ToList() ?? [];
        }
        catch (JsonException ex) {
            throw new InvalidOperationException($"JSON con formato invalido: {ex.Message}", ex);
        }
    }

    public static void Write(string path, IEnumerable<Contacto> contacts) {
        string json = JsonSerializer.Serialize(contacts, Options);
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}

[Table("Contactos")]
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }
}