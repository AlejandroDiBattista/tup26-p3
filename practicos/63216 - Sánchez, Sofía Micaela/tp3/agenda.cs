#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.ObjectModel;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

string archivoBase = args.Length > 0 ? args[0] : "agenda.db";

try
{
    using SqliteAgendaStore almacenAgenda = new(archivoBase);
    using IApplication aplicacion = Application.Create().Init();
    aplicacion.Run(new AgendaWindow(almacenAgenda));
}
catch (Exception error)
{
    Console.Error.WriteLine($"No se pudo abrir la agenda: {error.Message}");
    Environment.ExitCode = 1;
}

public sealed class AgendaWindow : Window
{
    private const string AppScheme = "AgendaTP3";

    private readonly SqliteAgendaStore repositorio;
    private readonly List<Contacto> agendaCompleta;
    private readonly List<Contacto> vistaActual = [];

    private TextField buscador = null!;
    private ListView grillaContactos = null!;
    private Label fichaContacto = null!;
    private StatusBar barraInferior = null!;

    private bool verSoloFavoritos;
    private int indiceRecordado;

    public AgendaWindow(SqliteAgendaStore store)
    {
        repositorio = store;
        agendaCompleta = store.GetAll().ToList();

        RegistrarPaleta();

        Title = $"Agenda TP3 - {store.DatabasePath}";
        Width = Dim.Fill();
        Height = Dim.Fill();
        SchemeName = AppScheme;

        Menu.DefaultBorderStyle = LineStyle.Single;

        ArmarPantalla();
        RefrescarListado();
        Informar($"Base abierta. {agendaCompleta.Count} contacto(s) disponible(s).");
    }

    private static void RegistrarPaleta()
    {
        SchemeManager.AddScheme(AppScheme, new Scheme
        {
            Normal = new Terminal.Gui.Drawing.Attribute(Color.BrightCyan, Color.Black),
            Focus = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.BrightCyan),
            Active = new Terminal.Gui.Drawing.Attribute(Color.White, Color.Blue),
            HotNormal = new Terminal.Gui.Drawing.Attribute(Color.BrightYellow, Color.Black),
            HotFocus = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.BrightYellow),
            Editable = new Terminal.Gui.Drawing.Attribute(Color.White, Color.DarkGray),
            Highlight = new Terminal.Gui.Drawing.Attribute(Color.BrightYellow, Color.Blue)
        });
    }

    private void ArmarPantalla()
    {
        MenuBar menuPrincipal = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", CerrarAgenda)
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo", "F2 / Ctrl+N", CrearContacto),
                    new MenuItem("_Editar", "F3 / Enter", ModificarContacto),
                    new MenuItem("_Eliminar", "Del / Ctrl+D", BorrarContacto)
                ]),
                new MenuBarItem("_Ver",
                [
                    new MenuItem("_Solo favoritos", null!, AlternarFavoritos)
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", null!, ShowAbout)
                ])
            ]
        };

        Label etiquetaBusqueda = new()
        {
            Text = "Buscar:",
            X = 1,
            Y = 1,
            Width = 8
        };

        buscador = new TextField
        {
            X = Pos.Right(etiquetaBusqueda) + 1,
            Y = 1,
            Width = Dim.Fill(1)
        };
        buscador.TextChanged += (_, _) => RefrescarListado();

        FrameView panelAgenda = new()
        {
            Title = "Agenda",
            X = 1,
            Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1)
        };
        panelAgenda.SchemeName = AppScheme;

        grillaContactos = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        grillaContactos.ValueChanged += (_, _) =>
        {
            indiceRecordado = grillaContactos.SelectedItem ?? 0;
            DibujarFicha();
        };
        panelAgenda.Add(grillaContactos);

        FrameView panelFicha = new()
        {
            Title = "Ficha",
            X = Pos.Right(panelAgenda) + 1,
            Y = 3,
            Width = Dim.Fill(1),
            Height = Dim.Fill(1)
        };
        panelFicha.SchemeName = AppScheme;

        fichaContacto = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = Dim.Fill()
        };
        panelFicha.Add(fichaContacto);

        barraInferior = new StatusBar(
        [
            new Shortcut(Key.F2, "Nuevo", CrearContacto),
            new Shortcut(Key.F3, "Editar", ModificarContacto),
            new Shortcut(Key.Delete, "Borrar", BorrarContacto),
            new Shortcut(Key.F4, "Buscar", ActivarBusqueda),
            new Shortcut(Key.Q.WithCtrl, "Salir", CerrarAgenda)
        ]);

        Add(menuPrincipal, etiquetaBusqueda, buscador, panelAgenda, panelFicha, barraInferior);
    }