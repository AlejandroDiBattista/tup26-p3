#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@*
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data.Common;
using Dapper.Contrib.Extensions;


using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow());


// Ventana principal
public sealed class AgendaWindow : Runnable {

    public AgendaWindow() {
        Title  = "Agenda TUI";
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

        FrameView panel = new() {
            Title = "agenda",
            X = 1,
            Y=2,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        Label info = new() {
            Text= "No hay contactos cargados",
            X = Pos.Center(),
            Y= Pos.Center
        };
            panel.Add(info);
            Add(menu, panel); 
    }

    private void AbrirDialogo() {
        ContactDialog dialog = new();
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
public sealed class ContactDialog : Dialog {
    public ContactDialog() {
        Title  = "Nuevo contacto";
        Width  = 60;
        Height = 20;
        Add(new Label() {
            Text= "Nombre:",
            X=1,
            Y=1
        });
        TextField nombre = new() {
            X = 12,
            Y = 1,
            Width = 40
            };
        Add(nombre);
        Add(new Label() {
            Text = "Telefono:",
            X = 1,
            Y = 3
            });
        TextField telefono = new() {
            X = 12,
            Y = 3,
            Width = 40
            };
        Add(telefono);
        Add(new Label() {
            Text = "Email:",
            X = 1,
            Y = 5
            });
        TextField email = new() {
            X = 12,
            Y = 5,
            Width = 40
            };
            Add(email);
        Button guardar = new() {
            Text = "_Guardar"
            };
        
        guardar.Accepting += (_, e) => {App!.RequestStop();
        e.Handled = true;
};

Button cancelar = new() {
    Text = "_Cancelar"
};

cancelar.Accepting += (_, e) => {
    App!.RequestStop();
    e.Handled = true;
};

AddButton(guardar);
AddButton(cancelar);
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