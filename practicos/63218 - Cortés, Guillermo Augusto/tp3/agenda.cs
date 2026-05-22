#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using Dapper.Contrib.Extensions;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

string databasePath = args.Length > 0 ? args[0] : "agenda.db";

try {
    using SqliteAgendaStore store = new(databasePath);
    using IApplication app = Application.Create().Init();
    app.Run(new AgendaWindow(store));
}
catch (Exception ex) {
    Console.Error.WriteLine($"No se pudo iniciar la agenda: {ex.Message}");
    Environment.ExitCode = 1;
}

public sealed class AgendaWindow : Window {
    private readonly SqliteAgendaStore store;

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;

        Title = $"Agenda - {store.DatabasePath}";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Salir", "Ctrl+Q", RequestExit)
                ]),
                new MenuBarItem("_Ayuda", [
                    new MenuItem("_Acerca de", null!, ShowAbout)
                ])
            ]
        };

        Label title = new() {
            Text = "AgendaT",
            X = Pos.Center(),
            Y = Pos.Center()
        };

        Add(menu, title);
    }

    private void ShowAbout() {
        MessageBox.Query(
            App!,
            "Acerca de",
            "Agenda de contactos en Terminal.Gui",
            "Aceptar");
    }

    private void RequestExit() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            RequestExit();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class SqliteAgendaStore : IDisposable {
    public string DatabasePath { get; }

    public SqliteAgendaStore(string databasePath) {
        DatabasePath = databasePath;
    }

    public IEnumerable<Contacto> GetAll() {
        return [];
    }

    public void Dispose() {
    }
}

public static class JsonAgendaIO {
}

[Table("Contactos")]
public sealed class Contacto {
    [Key]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Telefonos { get; set; } = "";

    public string Email { get; set; } = "";

    public string Notas { get; set; } = "";

    public bool Favorito { get; set; }

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