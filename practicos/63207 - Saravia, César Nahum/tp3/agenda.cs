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
using System.Text.Json;
using System.Collections.ObjectModel;

string dbPath = args.Length > 0 ? args[0]
 : "agenda.db";

SqliteAgendaStore store = new(dbPath);

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));

// Ventana principal
public sealed class AgendaWindow : Runnable {
private readonly SqliteAgendaStore store;
private List<Contacto> contacts = [];
private List<Contacto> filteredContacts = [];
private ListView contactsList = null!;
 private TextView detailView = null!;
private TextField searchField = null!;
private bool onlyFavorites = false;
private MenuItem itemFavoritos = null!;

private Label statusLabel = null!;

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        LoadContacts();
    }

    private void BuildLayout() {
         string textoInicial = onlyFavorites ? "_Solo favoritos [x]" : "_Solo favoritos [ ]";
        itemFavoritos = new MenuItem(textoInicial, "", () => {
        ToggleFavorites();
        itemFavoritos.Title = onlyFavorites ? "_Solo favoritos [x]" : "_Solo favoritos [ ]";
    });
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON",  "", ImportJson),
                    new MenuItem("_Exportar JSON",  "", ExportJson),
                    null!, // Separador
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                 new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo", "F2", NuevoContacto),
                    new MenuItem("_Editar", "F3", EditarContacto),
                    new MenuItem("_Eliminar", "Del", EliminarContacto),
            ]),
                new MenuBarItem("_Ver", [
                    itemFavoritos
                ]),
                new MenuBarItem("Ayuda", [
                    new MenuItem("_Acerca de", "", AcercaDe)
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

    private void LoadContacts() {
       contacts = store.GetAll();
       ApplyFilters();
    }

    private void ApplyFilters() {
        string search = searchField.Text.ToString()?.ToLower() ?? "";
        filteredContacts = contacts.Where(c => {
            bool matchesSearch = c.Nombre.ToLower().Contains(search) ||
                                 c.Telefonos.ToLower().Contains(search) ||
                                 c.Email.ToLower().Contains(search);
            bool matchesFavorite = !onlyFavorites || c.Favorito;
            return matchesSearch && matchesFavorite;
        })
        .ToList();
        observableCollection<string> items = new(filteredContacts.Select(c => c.Favorito ? $"★ {c.Nombre}" : c.Nombre));
        contactsList.SetSource<string>(items);
        UpdateDetail();
    }

    private void UpdateDetail() {
        if (filteredContacts.Count == 0) {
            detailView.Text = "No hay contactos para mostrar.";
            return;
        }
        int index = contactsList.SelectedItem ? contactsList.SelectedItem.Value0 : 0;
        if (index < 0 || index >= filteredContacts.Count) {
            return;
            Contacto c = filteredContacts[index];
            detailView.Text = $"Nombre: {c.Nombre}\n" +
                              $"Teléfonos: {c.Telefonos}\n" +
                              $"Email: {c.Email}\n" +
                              $"Favorito: {(c.Favorito ? "Sí" : "No")}"+
                              $"Notas: {c.Notas}\n";                   
        }
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