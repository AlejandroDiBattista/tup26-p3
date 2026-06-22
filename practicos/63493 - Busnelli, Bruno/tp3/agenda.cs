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
/// Trabajo Práctico 3 - AgendaT
/// Aplicación de agenda TUI con persistencia SQLite e import/export JSON.
/// ====

// Punto de entrada
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow());


// Ventana principal
public sealed class AgendaWindow : Runnable
{
    public AgendaWindow()
    {
        Title = "AgendaT - Terminal.Gui";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Nuevo contacto", "F2 / Ctrl+N", AbrirDialogo),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ])
            ]
        };

        Button openButton = new()
        {
            Text = "_Abrir diálogo",
            X = Pos.Center(),
            Y = Pos.Center()
        };

        openButton.Accepting += (_, e) =>
        {
            AbrirDialogo();
            e.Handled = true;
        };

        Add(menu, openButton);
    }

    private void AbrirDialogo()
    {
        EjemploDialog dialog = new();
        App!.Run(dialog);
    }

    private void SolicitarSalir()
    {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Q.WithCtrl)
        {
            SolicitarSalir();
            return true;
        }

        return base.OnKeyDown(key);
    }
}


// Diálogo de ejemplo
public sealed class EjemploDialog : Dialog
{
    public EjemploDialog()
    {
        Title = "Diálogo de ejemplo";
        Width = 50;
        Height = 8;

        Label message = new()
        {
            Text = "Este es un diálogo modal de ejemplo.",
            X = Pos.Center(),
            Y = 1
        };

        Button closeButton = new()
        {
            Text = "_Cerrar",
            IsDefault = true
        };

        closeButton.Accepting += (_, e) =>
        {
            App!.RequestStop();
            e.Handled = true;
        };

        Add(message);
        AddButton(closeButton);
    }
}


// Persistencia SQLite
public class SqliteAgendaStore
{
}


// Interoperabilidad JSON
public class JsonAgendaIO
{
}


// Modelo de datos
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

    public override string ToString()
    {
        return $"{(Favorito ? "★ " : "")}{Nombre}";
    }
}