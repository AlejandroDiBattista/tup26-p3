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

        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
    new MenuItem("_Nuevo contacto", "", AbrirDialogo),
    new MenuItem("_Eliminar contacto", "", EliminarContacto),
    null!,
    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
])
            ]
        };

        Label buscarLabel = new() {
            Text = "Buscar:",
            X = 1,
            Y = 1
        };

        buscador = new TextField() {
            X = 10,
            Y = 1,
            Width = 40
        };

        listaContactos = new ListView(
            contactos.Select(c => c.Nombre).ToList()
        ) {
            X = 1,
            Y = 3,
            Width = 30,
            Height = Dim.Fill() - 1
        };

        detalle = new Label("Seleccione un contacto") {
            X = 35,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        listaContactos.SelectedItemChanged += e => {

            if (e.Item >= 0 && e.Item < contactos.Count) {

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
   
   private void EliminarContacto() {

    if (listaContactos.SelectedItem < 0 ||
        listaContactos.SelectedItem >= contactos.Count) {

        return;
    }

    Contacto contacto =
        contactos[listaContactos.SelectedItem];

    int respuesta = MessageBox.Query(
        "Confirmar",
        $"¿Eliminar a {contacto.Nombre}?",
        "Si",
        "No"
    );

    if (respuesta == 0) {

        store.Delete(contacto);

        contactos = store.GetAll();

        listaContactos.SetSource(
            contactos.Select(c => c.Nombre).ToList()
        );

        detalle.Text = "Contacto eliminado";
    }
}



                
    

    private void AbrirDialogo() {
        

    ContactDialog dialog = new();

    App!.Run(dialog);

    if (dialog.Guardado) {

        store.Insert(dialog.Contacto);

        contactos = store.GetAll();

        listaContactos.SetSource(
            contactos.Select(c => c.Nombre).ToList()
        );
    }
;
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
public sealed class ContactDialog  : Dialog {
    public Contacto Contacto { get; private set; } = new();

    public bool Guardado { get; private set; }

    public ContactDialog() {

        Title = "Nuevo contacto";

        Width = 60;
        Height = 20;

        Label nombreLabel = new() {
            Text = "Nombre:",
            X = 1,
            Y = 1
        };

        TextField nombreField = new() {
            X = 15,
            Y = 1,
            Width = 40
        };

        Label telefonoLabel = new() {
            Text = "Telefonos:",
            X = 1,
            Y = 3
        };

        TextField telefonoField = new() {
            X = 15,
            Y = 3,
            Width = 40
        };

        Label emailLabel = new() {
            Text = "Email:",
            X = 1,
            Y = 5
        };

        TextField emailField = new() {
            X = 15,
            Y = 5,
            Width = 40
        };

        Label notasLabel = new() {
            Text = "Notas:",
            X = 1,
            Y = 7
        };

        TextView notasField = new() {
            X = 15,
            Y = 7,
            Width = 40,
            Height = 5
        };

        CheckBox favoritoCheck = new() {
            Text = "Favorito",
            X = 15,
            Y = 13
        };

        Button guardarButton = new() {
            Text = "_Guardar",
            X = 15,
            Y = 15,
            IsDefault = true
        };

        Button cancelarButton = new() {
            Text = "_Cancelar",
            X = 30,
            Y = 15
        };

        guardarButton.Accepting += (_, e) => {

            string nombre = nombreField.Text.ToString() ?? "";
            string email = emailField.Text.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(nombre)) {

                MessageBox.Query(
                     App!,
                    "Error",
                    "El nombre es obligatorio",
                    "OK"
                );

                return;
            }

            if (email.Length > 0 && !email.Contains("@")) {

                MessageBox.Query(
                     App!,
                    "Error",
                    "El email debe contener @",
                    "OK"
                );

                return;
            }

            Contacto = new Contacto {

                Nombre = nombre,
                Telefonos = telefonoField.Text.ToString() ?? "",
                Email = email,
                Notas = notasField.Text.ToString() ?? "",
                Favorito = favoritoCheck.CheckedState == CheckState.Checked
            };

            Guardado = true;

            App!.RequestStop();

            e.Handled = true;
        };

        cancelarButton.Accepting += (_, e) => {

            App!.RequestStop();

            e.Handled = true;
        };

        Add(
            nombreLabel,
            nombreField,
            telefonoLabel,
            telefonoField,
            emailLabel,
            emailField,
            notasLabel,
            notasField,
            favoritoCheck
        );

        AddButton(guardarButton);

        AddButton(cancelarButton);
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