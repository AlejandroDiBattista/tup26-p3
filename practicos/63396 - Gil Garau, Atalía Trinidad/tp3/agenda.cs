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

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
var dbPath = args.FirstOrDefault() ?? "agenda.db";
using IApplication app = Application.Create();
app.Run(new AgendaWindow());


// Ventana principal
public sealed class AgendaWindow : Runnable {
    public sealed class AgendaWindow : Runnable
{
    public static string DbPath = "agenda.db";

    readonly SqliteAgendaStore store;
    readonly List<Contacto> contacts = [];
    List<Contacto> filtered = [];
    readonly TextField search = new();
    readonly ListView list = new();
    readonly TextView detail = new();
    readonly Label status = new();
    readonly MenuItem favItem;
    bool onlyFav;

        public AgendaWindow() {
            Title  = "Agenda - Terminal.Gui";
            Width  = Dim.Fill();
            Height = Dim.Fill();
            Menu.DefaultBorderStyle = LineStyle.Single;
            store = new SqliteAgendaStore(DbPath);
            try { store.Init(); contacts.AddRange(store.GetAll()); }
            catch (Exception ex) { MessageBox.ErrorQuery(App!, "Base de datos", ex.Message, "OK"); }

            favItem = new MenuItem("Solo favoritos", null, ToggleFav) { CheckType = MenuItemCheckStyle.Checked };
            BuildLayout();
            RefreshList();
        }

    }

    private void BuildLayout() {
       
        MenuBar menu = new() {
             Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON", null, ImportJson),
                    new MenuItem("_Exportar JSON", null, ExportJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo contacto", "F2 / Ctrl+N", AbrirDialogo),
                    new MenuItem("_Editar contacto", "F3 / Enter", EditSelected),
                    new MenuItem("_Eliminar contacto", "Del / Ctrl+D", DeleteSelected)
                ]),
                new MenuBarItem("_Ver", [favItem]),
                new MenuBarItem("_Ayuda", [new MenuItem("_Acerca de", null, () => MessageBox.Query(App!, "Acerca de", "Agenda TUI simple", "OK"))])
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