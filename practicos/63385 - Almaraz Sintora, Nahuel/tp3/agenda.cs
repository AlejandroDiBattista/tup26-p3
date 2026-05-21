#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@10.0.8
#:package Dapper@2.1.79
#:package Dapper.Contrib@2.0.78

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;


string dbPath = args.Length > 0 ? args[0] : "agenda.db";
SqliteAgendaStore store;

try
{
    store = new SqliteAgendaStore(dbPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al abrir la base de datos: {ex.Message}");
    return;
}

IApplication app = Application.Create();
app.Init();
app.Run(new AgendaWindow(store, app));
app.Dispose();



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


public sealed class ContactDialog : Dialog
{
    private readonly IApplication _app;
    public bool Aceptado { get; private set; }
    public Contacto Resultado { get; private set; } = new();

    private readonly TextField _nombre = new();
    private readonly TextField[] _telefonos = new TextField[5];
    private readonly TextField _email = new();
    private readonly TextView _notas = new();
    private readonly CheckBox _favorito = new();

    public ContactDialog(IApplication app, string titulo, Contacto c)
    {
        _app = app; Title = titulo; Width = 60; Height = 22;
        int y = 1;

        Add(new Label { Text = "Nombre (*):", X = 1, Y = y });
        _nombre.X = 20; _nombre.Y = y; _nombre.Width = Dim.Fill(1); _nombre.Text = c.Nombre;
        Add(_nombre); y++;

        string[] tels = c.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++)
        {
            Add(new Label { Text = $"Telefono {i + 1}:", X = 1, Y = y });
            _telefonos[i] = new TextField { X = 20, Y = y, Width = Dim.Fill(1), Text = i < tels.Length ? tels[i].Trim() : "" };
            Add(_telefonos[i]); y++;
        }

        Add(new Label { Text = "Email:", X = 1, Y = y });
        _email.X = 20; _email.Y = y; _email.Width = Dim.Fill(1); _email.Text = c.Email;
        Add(_email); y++;

        _favorito.X = 1; _favorito.Y = y; _favorito.Text = "Favorito";
        _favorito.Value = c.Favorito ? CheckState.Checked : CheckState.UnChecked;
        Add(_favorito); y++;

        Add(new Label { Text = "Notas:", X = 1, Y = y }); y++;
        _notas.X = 1; _notas.Y = y; _notas.Width = Dim.Fill(1); _notas.Height = 3; _notas.Text = c.Notas;
        Add(_notas);

        var guardar = new Button { Text = "_Guardar" };
        guardar.Accepting += (s, e) => { if (Validar()) { e.Handled = true; _app.RequestStop(); } };

        var cancelar = new Button { Text = "_Cancelar" };
        cancelar.Accepting += (s, e) => { Aceptado = false; e.Handled = true; _app.RequestStop(); };

        AddButton(guardar); AddButton(cancelar);
    }

    private bool Validar()
    {
        string nombre = (_nombre.Text?.ToString() ?? "").Trim();
        if (nombre.Length == 0) { MessageBox.ErrorQuery(_app, "Validación", "El nombre no puede estar vacío.", "OK"); return false; }
        string email = (_email.Text?.ToString() ?? "").Trim();
        if (email.Length > 0 && !email.Contains('@')) { MessageBox.ErrorQuery(_app, "Validación", "El email debe contener '@'.", "OK"); return false; }

        string telefonos = string.Join(", ", _telefonos.Select(t => (t.Text?.ToString() ?? "").Trim()).Where(t => t.Length > 0));
        Aceptado = true;
        Resultado = new Contacto { Nombre = nombre, Telefonos = telefonos, Email = email, Notas = _notas.Text?.ToString() ?? "", Favorito = _favorito.Value == CheckState.Checked };
        return true;
    }
}

public sealed class PathDialog : Dialog
{
    private readonly IApplication _app;
    private readonly TextField _ruta = new();
    public bool Aceptado { get; private set; }
    public string Ruta { get; private set; } = "";

    public PathDialog(IApplication app, string titulo, string etiqueta, string sugerida)
    {
        _app = app;
        Title = titulo; Width = 70; Height = 8;

        Add(new Label { Text = etiqueta, X = 1, Y = 1 });
        _ruta.X = 1; _ruta.Y = 2; _ruta.Width = Dim.Fill(1); _ruta.Text = sugerida;
        Add(_ruta);

        var aceptar = new Button { Text = "_Aceptar" };
        aceptar.Accepting += (s, e) => {
            string ruta = (_ruta.Text?.ToString() ?? "").Trim();
            if (ruta.Length == 0) { MessageBox.ErrorQuery(_app, "Validacion", "La ruta no puede estar vacia.", "OK"); return; }
            Ruta = ruta; Aceptado = true; e.Handled = true; _app.RequestStop();
        };

        var cancelar = new Button { Text = "_Cancelar" };
        cancelar.Accepting += (s, e) => { Aceptado = false; e.Handled = true; _app.RequestStop(); };

        AddButton(aceptar);
        AddButton(cancelar);
    }
}

public class SqliteAgendaStore
{
    private readonly string _cs;

    public SqliteAgendaStore(string dbPath)
    {
        _cs = $"Data Source={dbPath}";

        using var conn = Abrir();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL,
                Email TEXT NOT NULL,
                Notas TEXT NOT NULL,
                Favorito INTEGER NOT NULL
            )
        ");
    }

    private SqliteConnection Abrir()
    {
        var conn = new SqliteConnection(_cs);
        conn.Open();
        return conn;
    }

    public IEnumerable<Contacto> ObtenerTodos()
    {
        using var conn = Abrir();
        return conn.GetAll<Contacto>().ToList();
    }

    public void Insertar(Contacto c)
    {
        using var conn = Abrir();
        long id = conn.Insert(c);
        c.Id = (int)id;
    }

    public void Actualizar(Contacto c)
    {
        using var conn = Abrir();
        conn.Update(c);
    }

    public void Eliminar(int id)
    {
        using var conn = Abrir();
        conn.Delete(new Contacto { Id = id });
    }
}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<Contacto> Leer(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("No existe el archivo JSON.", path);

        string json = File.ReadAllText(path, Encoding.UTF8);
        var contactos = JsonSerializer.Deserialize<List<Contacto>>(json, Options);

        if (contactos is null)
            throw new JsonException("El archivo no contiene una lista de contactos.");

        foreach (var contacto in contactos)
        {
            contacto.Id = 0;
            contacto.Nombre ??= "";
            contacto.Telefonos ??= "";
            contacto.Email ??= "";
            contacto.Notas ??= "";
        }

        return contactos;
    }

    public static void Escribir(string path, IEnumerable<Contacto> contactos)
    {
        string json = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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
            Id = Id,
            Nombre = Nombre,
            Telefonos = Telefonos,
            Email = Email,
            Notas = Notas,
            Favorito = Favorito
        };
    }
}