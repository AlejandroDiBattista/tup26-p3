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
using System.Linq;


string dbPath = args.Length > 0
    ? args[0]
    : "agenda.db";

SqliteAgendaStore store = new(dbPath);
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));


// Ventana principal
public sealed class AgendaWindow : Runnable {

private readonly SqliteAgendaStore store;
private List<Contacto> contactos = [];
private ListView listaContactos = null!;
private TextField buscador = null!;
private Label detalle = null!;

        
     public AgendaWindow(SqliteAgendaStore store) {
        
        this.store = store ;
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }
    
    private void BuildLayout() {
       contactos = store.GetAll();
        
        MenuBar menu = new() 
        {
        Menus = 
        [
            new MenuBarItem("_Archivo", 
            [
                new MenuItem("_Nuevo contacto", null!, AbrirDialogo),
                null!,
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
            ])
        ]
    };
        Label buscarLabel = new() 
        {
          Text = "Buscar:", 
          X = 1,
          Y = 1 
        }; 
        
    buscador = new TextField("")
     {
        X = 10,
        Y = 1,
        Width = 40    
     }; 

    listaContactos = new ListView(contactos.Select(c => c.Nombre).ToList()) 
    {
        X = 1,
        Y = 3,
        Width = 30,
        Height = Dim.Fill() - 1
    };
      
    detalle = new Label("Seleccione un contacto") 
    {
        X = 35,
        Y = 3,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    };
    listaContactos.SelectedItemChanged += e => 
    {

        if (e.Item >= 0 && e.Item < contactos.Count) 
        {

            Contacto c = contactos[e.Item];

            detalle.Text =
                $"Nombre: {c.Nombre}\n" +
                $"Telefonos: {c.Telefonos}\n" +
                $"Email: {c.Email}\n" +
                $"Notas: {c.Notas}\n" +
                $"Favorito: {(c.Favorito ? "Sí" : "No")}";
        }
    };
     
     Add(
        menu,
        buscarLabel,
        buscador,
        listaContactos,
        detalle
    );
    
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


public class SqliteAgendaStore {
    private readonly string connectionString;

    public SqliteAgendaStore(string dbPath) {
        connectionString = $"Data Source={dbPath}";
        Inicializar();
    }

    private DbConnection GetConnection() {
        return new SqliteConnection(connectionString);
    }

    private void Inicializar() {
        using DbConnection db = GetConnection();
        
            db.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            )
        ");
    }

    public List<Contacto> GetAll() {
        using DbConnection db = GetConnection();

        return db.Query<Contacto>(
            "SELECT * FROM Contactos ORDER BY Nombre"
        ).ToList();
    }
    public void Insert (Contacto contacto) {
        using DbConnection db = GetConnection();
        db.Insert(contacto);
    }

    public void Update(Contacto contacto) {
        using DbConnection db = GetConnection();

        db.Update(contacto);
    }

    public void Delete(Contacto contacto) {
        using DbConnection db = GetConnection();

        db.Delete(contacto);
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
}