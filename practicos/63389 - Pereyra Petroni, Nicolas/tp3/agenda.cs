#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*


using Terminal.Gui;
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
using Terminal.Gui.App;
using System.Text.Json;




string dbPath = args.Length > 0
    ? args[0]
    : "agenda.db";

SqliteAgendaStore store = new(dbPath);
Application.Init();
Application.Run(new AgendaWindow(store));

Application.Shutdown();

// Ventana principal
public sealed class AgendaWindow : Window {

private readonly SqliteAgendaStore store;
private List<Contacto> contactos = [];
private List<Contacto> contactosFiltrados= [];
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

    new MenuItem("_Editar contacto", "", EditarContacto),

    new MenuItem("_Eliminar contacto", "", EliminarContacto),

    new MenuItem("_Importar JSON", "", ImportarJson),

    new MenuItem("_Exportar JSON", "", ExportarJson),

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

   buscador.TextChanged += (_, _) => {
    ActualizarLista();
};
        listaContactos = new ListView(){
            X = 1,
            Y = 3,
            Width = 30,
            Height = Dim.Fill() - 1
        };
        listaContactos.SetSource(
        new ObservableCollection<string>()
);

        detalle = new Label() {
           Text = "Seleccione un contacto",
            X = 35,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

      

listaContactos.Accepting += (_, e) => {

    if (!listaContactos.SelectedItem.HasValue ||
        listaContactos.SelectedItem.Value < 0 ||
        listaContactos.SelectedItem.Value >= contactosFiltrados.Count) {

        return;
    }

    Contacto c =
        contactosFiltrados[
            listaContactos.SelectedItem.Value
        ];

    detalle.Text =
        $"Nombre: {c.Nombre}\n" +
        $"Telefonos: {c.Telefonos}\n" +
        $"Email: {c.Email}\n" +
        $"Notas: {c.Notas}\n" +
        $"Favorito: {(c.Favorito ? "Sí" : "No")}";

    e.Handled = true;
};
        Add(
            menu,
            buscarLabel,
            buscador,
            listaContactos,
            detalle
        );
        ActualizarLista();
    }
   
   private void EliminarContacto() {

     if (!listaContactos.SelectedItem.HasValue ||
        listaContactos.SelectedItem.Value < 0 ||
        listaContactos.SelectedItem.Value >= contactosFiltrados.Count) {

        return;
    }
    Contacto contacto =
        contactosFiltrados[listaContactos.SelectedItem.Value];

    int? respuesta = MessageBox.Query
    (
    App!,
    "Confirmar",
    $"¿Eliminar a {contacto.Nombre}?",
    "Si",
    "No"
    );

    if (respuesta.HasValue && respuesta .Value== 0) {

        store.Delete(contacto);

        contactos = store.GetAll();

       ActualizarLista();
    
    detalle.Text = "Contacto eliminado";
    }
}
private void EditarContacto() {

    if (!listaContactos.SelectedItem.HasValue ||
        listaContactos.SelectedItem.Value < 0 ||
        listaContactos.SelectedItem.Value >= contactosFiltrados.Count) {

        return;
    }

    Contacto seleccionado =
        contactosFiltrados[
            listaContactos.SelectedItem.Value
        ];

    ContactDialog dialog =
        new(seleccionado);

    Application.Run(dialog);

    if (dialog.Guardado) {

        store.Update(dialog.Contacto);

        contactos = store.GetAll();

        ActualizarLista();

        detalle.Text =
            "Contacto actualizado";
    }
}

private void ActualizarLista() 
{

    string texto =
        buscador.Text.ToString()?.ToLower() ?? "";

    contactosFiltrados = contactos
        .Where(c =>
            c.Nombre.ToLower().Contains(texto) ||
            c.Telefonos.ToLower().Contains(texto) ||
            c.Email.ToLower().Contains(texto)
        )
        .ToList();

   listaContactos.SetSource(
    new ObservableCollection<string>
    (
        contactosFiltrados
            .Select(c => c.Nombre)
            .ToList()
    )
);
}
private void ExportarJson() {

    try {

        JsonAgendaIO.Exportar(
            "contactos.json",
            contactos
        );

        detalle.Text =
            "Contactos exportados a contactos.json";
    }
    catch (Exception ex) {

        detalle.Text =
            $"Error: {ex.Message}";
    }
}

private void ImportarJson() {

    try {

        List<Contacto> importados =
            JsonAgendaIO.Importar(
                "contactos.json"
            );

        foreach (Contacto c in importados) {

            c.Id = 0;

            store.Insert(c);
        }

        contactos = store.GetAll();

        ActualizarLista();

        detalle.Text =
            "Contactos importados correctamente";
    }
    catch (Exception ex) {

        detalle.Text =
            $"Error: {ex.Message}";
    }
}
                
    

    private void AbrirDialogo() {
        

    ContactDialog dialog = new();

    Application.Run(dialog);

    if (dialog.Guardado) {

        store.Insert(dialog.Contacto);

        contactos = store.GetAll();
     
     ActualizarLista();
     }
    }

    private void SolicitarSalir() {
        Application.RequestStop();
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

   public ContactDialog(Contacto? contacto = null)
   {

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
       
     if (contacto != null) 
     {

    nombreField.Text = contacto.Nombre;

    telefonoField.Text = contacto.Telefonos;

    emailField.Text = contacto.Email;

    notasField.Text = contacto.Notas;
     }

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

            Contacto = new Contacto
             {
                Id = contacto?.Id ?? 0,
                Nombre = nombre,
                Telefonos = telefonoField.Text.ToString() ?? "",
                Email = email,
                Notas = notasField.Text.ToString() ?? "",
                Favorito = false
                     
            };

            Guardado = true;

            Application.RequestStop();

            e.Handled = true;
        };

        cancelarButton.Accepting += (_, e) => {

            Application.RequestStop();

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
public class JsonAgendaIO {
    

    public static void Exportar(
        string ruta,
        List<Contacto> contactos
    ) {

        JsonSerializerOptions options = new() {
            WriteIndented = true
        };

        string json =
            JsonSerializer.Serialize(
                contactos,
                options
            );

        File.WriteAllText(ruta, json);
    }

    public static List<Contacto> Importar(string ruta) {

        string json = File.ReadAllText(ruta);

        return JsonSerializer.Deserialize<List<Contacto>>(json)
               ?? [];
    }
}


[Table("Contactos")]
public class Contacto
{
    [Key] public int    Id        { get; set; }

          public string Nombre    { get; set; } = "";

          public string Telefonos { get; set; } = "";

          public string Email     { get; set; } = "";

          public string Notas     { get; set; } = "";

          public bool   Favorito  { get; set; }
 }