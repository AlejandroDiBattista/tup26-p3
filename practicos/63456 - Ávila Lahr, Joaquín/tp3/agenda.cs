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
using System.Collections.ObjectModel;
using System.Linq;



Console.OutputEncoding = System.Text.Encoding.UTF8;
String archivoBase = args.Length > 0 ? args[0] :  "agenda.db";
SqliteAgendaStore baseDatos = new ($"Data Source={archivoBase}");

try {
    baseDatos.Inicializar();
}
catch (Exception ex) {
    Console.WriteLine(ex.Message);
    return;
}
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(baseDatos));


// Ventana principal
public sealed class AgendaWindow : Runnable {
    readonly SqliteAgendaStore baseDatos;
    readonly ListView lista = new();
    readonly TextView detalle = new();
    readonly Label estado = new();

List<Contacto> contactos = [];

    public AgendaWindow(SqliteAgendaStore baseDatos) {
        this.baseDatos = baseDatos;
        Title  = "Agenda TUI";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        CrearInterfaz();
        Recargar();
    }

    private void CrearInterfaz() {
       MenuBar menu = new() {
        Menus = [
            new MenuBarItem("_Archivo", [
                new MenuItem("_Nuevo contacto", "F2", AbrirDialogo),
                null!,
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
            ])
        ]
    };

    Add(menu);

    FrameView panelLista = new() {
        Title = "Contactos",
        X = 0,
        Y = 1,
        Width = 30,
        Height = Dim.Fill(2)
    };

    lista.Width = Dim.Fill();
    lista.Height = Dim.Fill();

    lista.ValueChanged += (_, _) => {
        MostrarDetalle();
    };

    panelLista.Add(lista);

    Add(panelLista);

    FrameView panelDetalle = new() {
        Title = "Detalle",
        X = Pos.Right(panelLista),
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill(2)
    };

    detalle.X = 1;
    detalle.Y = 0;
    detalle.Width = Dim.Fill(1);
    detalle.Height = Dim.Fill();
    detalle.ReadOnly = true;

    panelDetalle.Add(detalle);

    Add(panelDetalle);

    estado.X = 1;
    estado.Y = Pos.AnchorEnd(1);
    estado.Width = Dim.Fill();
    estado.Text = "Agenda iniciada";

    Add(estado);
    }
void Recargar() {

    contactos = baseDatos.Listar();

    lista.SetSource(
        new ObservableCollection<string>(
            contactos.Select(c => c.Nombre).ToList()
        )
    );

    MostrarDetalle();
}
void MostrarDetalle() {

    Contacto? c = ObtenerSeleccionado();

    detalle.Text = c == null
        ? ""
        :
        $"Nombre:\n{c.Nombre}\n\n" +
        $"Telefonos:\n{c.Telefonos}\n\n" +
        $"Email:\n{c.Email}\n\n" +
        $"Notas:\n{c.Notas}";
}
Contacto? ObtenerSeleccionado() {

    int pos =
        lista.SelectedItem.HasValue
        ? lista.SelectedItem.Value
        : -1;

    return pos < 0 || pos >= contactos.Count
        ? null
        : contactos[pos];
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
    readonly TextField nombre = new();
readonly TextField telefono = new();
readonly TextField email = new();
    public ContactDialog() {
        Title  = "Nuevo contacto";
        Width  = 60;
        Height = 20;
        Add(new Label() {
            Text= "Nombre:",
            X=1,
            Y=1
        });
        nombre = new() {
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
        telefono = new() {
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
        email = new() {
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
nombre.SetFocus();
    }
}

public class SqliteAgendaStore {
    readonly string conexion;
    public SqliteAgendaStore(string conexion) {
        this.conexion = conexion;
    }
    SqliteConnection Abrir() {
        SqliteConnection db = new(conexion);
        db.Open();
        return db;
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
        """);}
        public List<Contacto> Listar() {
    using SqliteConnection db = Abrir();
    return db
        .GetAll<Contacto>()
        .OrderBy(x => x.Nombre)
        .ToList();
    }
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


          public Contacto Clone() => new() {
              Id = Id,
               Nombre = Nombre,
    Telefonos = Telefonos,
    Email = Email,
    Notas = Notas,
    Favorito = Favorito
          };
}