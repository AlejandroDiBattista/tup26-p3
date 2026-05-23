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
using System.Text.Encodings.Web;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

Console.OutputEncoding = System.Text.Encoding.UTF8;
string archivo = args.Length > 0 ? args[0] : "agenda.db";
SqliteAgendaStore store = new($"Data Source={archivo}");

try {
    store.Inicializar();
}
catch (Exception ex) {
    Console.WriteLine("Error al abrir la base de datos:");
    Console.WriteLine(ex.Message);
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));
public sealed class AgendaWindow : Runnable {
    readonly SqliteAgendaStore store;
    readonly ListView lista = new();
    readonly TextField buscar = new();
    readonly TextView detalle = new();
    readonly Label estado = new();

    List<Contacto> contactos = [];
    List<Contacto> filtrados = [];
    bool soloFavoritos;

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;

        Title  = "Agenda";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();

        contactos = store.Listar();
        Refrescar();
        buscar.SetFocus();
    }

    void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson, Key.I.WithCtrl),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson, Key.E.WithCtrl),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", Salir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo", "Ctrl+N", Nuevo, Key.N.WithCtrl),
                    new MenuItem("_Editar", "F3", EditarSeleccionado, Key.F3),
                    new MenuItem("E_liminar", "Ctrl+D", EliminarSeleccionado, Key.D.WithCtrl)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("_Solo favoritos", "", ToggleFavoritos)
                ]),
                new MenuBarItem("Ay_uda", [
                    new MenuItem("_Acerca de", "", AcercaDe)
                ])
            ]
        };

        Label etiquetaBuscar = new() { Text = "Buscar:", X = 1, Y = 2 };
        buscar.X = 10;
        buscar.Y = 2;
        buscar.Width = Dim.Fill(2);
        buscar.TextChanged += (_, _) => Refrescar();
        buscar.KeyDown += (_, key) => {
            if (key == Key.Enter || key == Key.Tab) {
                key.Handled = true;
                lista.SetFocus();
            }
        };

        FrameView panelLista = new() {
            Title  = "Contactos",
            X = 0, Y = 4,
            Width  = Dim.Percent(40),
            Height = Dim.Fill(1)
        };
        lista.Width  = Dim.Fill();
        lista.Height = Dim.Fill();
        lista.ValueChanged += (_, _) => MostrarDetalle();
        lista.KeyDown += (_, key) => {
            if (key == Key.Enter) {
                key.Handled = true;
                EditarSeleccionado();
            } else if (key == Key.Delete) {
                key.Handled = true;
                EliminarSeleccionado();
            }
        };
        panelLista.Add(lista);

        FrameView panelDetalle = new() {
            Title  = "Detalle",
            X = Pos.Right(panelLista), Y = 4,
            Width  = Dim.Fill(),
            Height = Dim.Fill(1)
        };
        detalle.X = 0;
        detalle.Y = 0;
        detalle.Width  = Dim.Fill();
        detalle.Height = Dim.Fill();
        detalle.ReadOnly = true;
        panelDetalle.Add(detalle);

        estado.X = 1;
        estado.Y = Pos.AnchorEnd(1);
        estado.Width = Dim.Fill();
        estado.Text = "Agenda Lista.";

        Add(menu, etiquetaBuscar, buscar, panelLista, panelDetalle, estado);
    }
void Refrescar() {
        string texto = (buscar.Text?.ToString() ?? "").Trim();

        filtrados = contactos
            .Where(c => !soloFavoritos || c.Favorito)
            .Where(c => texto == ""
                || c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || c.Telefonos.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || c.Email.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var lineas = filtrados.Select(c => (c.Favorito ? "* " : "  ") + c.Nombre).ToList();
        lista.SetSource(new ObservableCollection<string>(lineas));
        MostrarDetalle();
    }

    void MostrarDetalle() {
        Contacto? c = ContactoSeleccionado();
        if (c is null) {
            detalle.Text = "";
            return;
        }

        detalle.Text =
            "Nombre:    " + c.Nombre + "\n" +
            "Telefonos: " + c.Telefonos + "\n" +
            "Email:     " + c.Email + "\n" +
            "Favorito:  " + (c.Favorito ? "Si" : "No") + "\n\n" +
            "Notas:\n" + c.Notas;
    }

    Contacto? ContactoSeleccionado() {
        int i = lista.SelectedItem ?? -1;
        if (i < 0 || i >= filtrados.Count) return null;
        return filtrados[i];
    }

    void Nuevo() {
        ContactDialog dialog = new("Nuevo contacto", new Contacto());
        App!.Run(dialog);
        if (!dialog.Aceptado) return;

        store.Insertar(dialog.Contacto);
        contactos = store.Listar();
        estado.Text = "CNuevo contacto agregado.";
        Refrescar();
    }

    void EditarSeleccionado() {
        Contacto? c = ContactoSeleccionado();
        if (c is null) { Aviso("Selecciona un contacto."); return; }

        ContactDialog dialog = new("Editar contacto", c.Clone());
        App!.Run(dialog);
        if (!dialog.Aceptado) return;

        store.Actualizar(dialog.Contacto);
        contactos = store.Listar();
        estado.Text = "Contacto modificado.";
        Refrescar();
    }

    void EliminarSeleccionado() {
        Contacto? c = ContactoSeleccionado();
        if (c is null) { Aviso("Selecciona un contacto."); return; }

        int r = MessageBox.Query(App!, "Eliminar", $"Eliminar a {c.Nombre}?", "No", "Si") ?? 0;
        if (r != 1) return;

        store.Eliminar(c);
        contactos = store.Listar();
        estado.Text = "Contacto eliminado.";
        Refrescar();
    }


public sealed class SqliteAgendaStore {
    readonly string connectionString;

    public SqliteAgendaStore(string connectionString) {
        this.connectionString = connectionString;
    }

    SqliteConnection Abrir() {
        SqliteConnection conexion = new(connectionString);
        conexion.Open();
        return conexion;
    }

    public void Inicializar() {
        using SqliteConnection db = Abrir();
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL,
                Email TEXT NOT NULL,
                Notas TEXT NOT NULL,
                Favorito INTEGER NOT NULL
            );
            """);
    }

    public List<Contacto> Listar() {
        using SqliteConnection db = Abrir();
        return db.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
    }

    public void Insertar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Insert(c);
    }

    public void Actualizar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Update(c);
    }

    public void Eliminar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Delete(c);
    }
}
public sealed class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

    public Contacto Clone() => new() {
        Id        = Id,
        Nombre    = Nombre,
        Telefonos = Telefonos,
        Email     = Email,
        Notas     = Notas,
        Favorito  = Favorito
    };
}