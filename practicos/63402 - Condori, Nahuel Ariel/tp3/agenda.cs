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
// ==============================================================================
// 3. Diálogo de edición (ContactDialog)
// ==============================================================================
public sealed class ContactDialog : Dialog {
    private TextField _tfName;
    private TextField[] _tfPhones = new TextField[5];
    private TextField _tfEmail;
    private TextView _tvNotes;
    
    private bool _isFav;
    private Button _btnFav = null!;

    public Contacto? ContactoResult { get; private set; }
    public bool IsCanceled { get; private set; } = true;

    public ContactDialog(Contacto? c = null) {
        Title  = c == null ? "Nuevo Contacto" : "Editar Contacto";
        Width  = 55;
        Height = 22;

        Add(new Label() { Text = "Nombre:", X = 1, Y = 1 });
        _tfName = new TextField() { Text = c?.Nombre ?? "", X = 12, Y = 1, Width = Dim.Fill(1) };
        Add(_tfName);

        string[] phones = (c?.Telefonos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++) {
            Add(new Label() { Text = $"Teléfono {i + 1}:", X = 1, Y = 3 + i });
            _tfPhones[i] = new TextField() {
                Text = i < phones.Length ? phones[i].Trim() : "",
                X = 12, Y = 3 + i, Width = Dim.Fill(1)
            };
            Add(_tfPhones[i]);
        }

        Add(new Label() { Text = "Email:", X = 1, Y = 9 });
        _tfEmail = new TextField() { Text = c?.Email ?? "", X = 12, Y = 9, Width = Dim.Fill(1) };
        Add(_tfEmail);

        Add(new Label() { Text = "Notas:", X = 1, Y = 11 });
        _tvNotes = new TextView() { Text = c?.Notas ?? "", X = 12, Y = 11, Width = Dim.Fill(1), Height = 4 };
        Add(_tvNotes);

        _isFav = c?.Favorito ?? false;
        Add(new Label() { Text = "Favorito:", X = 1, Y = 16 });
        
        _btnFav = new Button() { 
            Text = _isFav ? "[★] Sí" : "[ ] No", 
            X = 12, Y = 16 
        };
        
        _btnFav.Accepting += (_, e) => {
            _isFav = !_isFav; 
            _btnFav.Text = _isFav ? "[★] Sí" : "[ ] No"; 
            e.Handled = true;
        };
        Add(_btnFav);

        Button btnOk = new() { Text = "Guardar", IsDefault = true };
        Button btnCancel = new() { Text = "Cancelar" };

        btnOk.Accepting += (_, e) => {
            if (string.IsNullOrWhiteSpace(_tfName.Text)) {
                MostrarErrorValidacion("El nombre no puede estar vacío.");
                return; 
            }

            var email = _tfEmail.Text ?? "";
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@")) {
                MostrarErrorValidacion("El email debe contener un '@'.");
                return;
            }

            var phoneList = _tfPhones.Select(p => p.Text?.Trim()).Where(p => !string.IsNullOrEmpty(p));

            ContactoResult = new Contacto {
                Id = c?.Id ?? 0,
                Nombre = _tfName.Text.Trim(),
                Telefonos = string.Join(",", phoneList),
                Email = email.Trim(),
                Notas = _tvNotes.Text ?? "",
                Favorito = _isFav
            };
            
            IsCanceled = false;
            App!.RequestStop();
            e.Handled = true;
        };

        btnCancel.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        AddButton(btnOk);
        AddButton(btnCancel);
    }

    private void MostrarErrorValidacion(string mensaje) {
        Dialog d = new() { Title = "Validación", Width = 40, Height = 7 };
        d.Add(new Label() { Text = mensaje, X = Pos.Center(), Y = 1 });
        Button btn = new() { Text = "OK", IsDefault = true };
        btn.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        d.AddButton(btn);
        App!.Run(d);
    }
}