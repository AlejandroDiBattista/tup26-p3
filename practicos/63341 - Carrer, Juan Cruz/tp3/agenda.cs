#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada

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


public class SqliteAgendaStore {}
public class JsonAgendaIO {}

[Table("Contactos")]
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }
}