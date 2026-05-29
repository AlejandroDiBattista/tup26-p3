#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@*
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
using System.Collections.ObjectModel;

// Punto de entrada
string dbPath = args.Length > 0 ? args[0] : "agenda.db";

using (SqliteAgendaStore store = new(dbPath)) {
    Application.Init();
    Application.Run(new AgendaWindow(store));
    Application.Shutdown();
}

// Ventana principal
public sealed class AgendaWindow : Window{

    private readonly SqliteAgendaStore store;
    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        contacts = store.GetAll();
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
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ])
            ]
        };

        listView = new ListView() {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        listView.SetSource(
            new ObservableCollection<string>(
                contacts.Select(c => c.Nombre)
            )
        );
        Add(menu, listView);
    }
    private List<Contacto> contacts = [];
    private ListView listView = null!;
    private void AbrirDialogo() {
    var dialog = new ContactoDialog();
    App!.Run(dialog);

    if (dialog.Resultado != null) {
        store.Insert(dialog.Resultado);

        contacts.Add(dialog.Resultado);

        listView.SetSource(
            new ObservableCollection<string>(
                contacts.Select(c => c.Nombre)
            )
        );
    }
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
public sealed class ContactoDialog : Dialog {

    private TextField nombreField;
    private TextField telefonoField;
    private TextField emailField;
    private TextView notasField;

    public Contacto? Resultado { get; private set; }

    public ContactoDialog() {
        Title  = "Nuevo Contacto";
        Width  = 50;
        Height = 18;

        Add(new Label() { Text = "Nombre:", X = 1, Y = 1 });
        nombreField = new TextField() { Text = "", X = 15, Y = 1, Width = 30 };

        Add(new Label() { Text = "Teléfono:", X = 1, Y = 3 });
        telefonoField = new TextField() { Text = "", X = 15, Y = 3, Width = 30 };

        Add(new Label() { Text = "Email:", X = 1, Y = 5 });
        emailField = new TextField() { Text = "", X = 15, Y = 5, Width = 30 };

        Add(new Label() { Text = "Notas:", X = 1, Y = 7 });
        notasField = new TextView() { X = 15, Y = 7, Width = 30, Height = 4 };

        Button guardar = new() { Text = "_Guardar", IsDefault = true };
        Button cancelar = new() { Text = "_Cancelar" };

        guardar.Accepting += (_, e) => {
            Resultado = new Contacto {
                Nombre = nombreField.Text.ToString() ?? "",
                Telefonos = telefonoField.Text.ToString() ?? "",
                Email = emailField.Text.ToString() ?? "",
                Notas = notasField.Text.ToString() ?? ""
            };

            App!.RequestStop();
            e.Handled = true;
        };

        cancelar.Accepting += (_, e) => {
            Resultado = null;
            App!.RequestStop();
            e.Handled = true;
        };

        Add(nombreField, telefonoField, emailField, notasField);
        AddButton(guardar);
        AddButton(cancelar);
    }
}


public class SqliteAgendaStore : IDisposable {
    private readonly DbConnection connection;
    public SqliteAgendaStore(string path) {
        connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            );
        ");
    }
    public List<Contacto> GetAll() {
        return connection.Query<Contacto>("SELECT * FROM Contactos").ToList();
    }
    public void Insert(Contacto c) {
        var id = connection.Insert(c);
        c.Id = (int)id;
    }
    public void Dispose() => connection.Dispose();
}
public class JsonAgendaIO {}

[Table("Contactos")]
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

      public Contacto Clone() {
        return new Contacto {
            Id = Id,
            Nombre = Nombre,
            Telefonos = Telefonos,
            Email = Email,
            Notas = Notas,
            Favorito = Favorito
        };
    }
}