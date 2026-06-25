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
using System.Linq;
using System.Data.Common;
using System.Collections.ObjectModel;
using Dapper.Contrib.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
string dbFile = Environment.GetCommandLineArgs().Length > 1 ? Environment.GetCommandLineArgs()[1] : "agenda.db";
SqliteAgendaStore store = new(dbFile);
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));


// Ventana principal
public sealed class AgendaWindow : Runnable {

    private readonly SqliteAgendaStore store;

    private List<Contacto> contacts = new List<Contacto>();
    private List<Contacto> filteredContacts = new List<Contacto>();
    private bool soloFavoritos = false;

    private ListView contactsList = null!;
    private TextField searchField = null!;
    private TextView detailView = null!;
    private Label statusBar = null!;

    private void ExportarJson(){
        JsonAgendaIO.Exportar("contactos.json", contacts);
        statusBar.Text = "Contactos exportados a contactos.json";
    }

    private void ImportarJson(){
        try{
            List<Contacto> importados = JsonAgendaIO.Importar("contactos.json");
            
            MessageBox.Query(
                App!,
                "Importar",
                $"Se importarán {importados.Count} contactos",
                "OK"
            );

            foreach (Contacto c in importados){
                c.Id = 0;
                store.Insert(c);
            }
            contacts = store.ObtenerContactos();

            ApplyFilters();

            statusBar.Text = $"{importados.Count} contactos importados";
        }
        catch (Exception ex){
            MessageBox.ErrorQuery(
            App!,
            "Error",
            ex.Message,
            "OK"
            );
        }
    }

    private void MostrarAcercaDe(){
        MessageBox.Query(
            App!,
            "Acerca de",
            "AgendaT\nTP3 Programacion III\nSQLite + JSON + Terminal.Gui",
            "OK"
        );
    }

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();
        
        Menu.DefaultBorderStyle = LineStyle.Single;
        contacts = store.ObtenerContactos();
        filteredContacts = contacts.ToList();
        
        BuildLayout();
        RefreshList();
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus = [
                new MenuBarItem("_Archivo", [
                new MenuItem("_Importar JSON", "", ImportarJson),
                new MenuItem("_Exportar JSON", "Ctrl+X", ExportarJson),
                new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contacto", [
                    new MenuItem("_Nuevo", "Ctrl+N", NuevoContacto),
                    new MenuItem("_Editar", "Ctrl+E", EditarContacto),
                    new MenuItem("_Eliminar", "Ctrl+D", EliminarContacto)
                ]),
                new MenuBarItem("_Ver", [
                new MenuItem("_Solo favoritos", "", ToggleFavoritos)
                ]),
                new MenuBarItem("_Ayuda", [
                new MenuItem("_Acerca de", "", MostrarAcercaDe)
                ])
            ]
        };

        Label lblBuscar = new()
        {
            X = 1,
            Y = 2,
            Text = "Buscar:"
        };

        searchField = new TextField()
        {
            X = 10,
            Y = 2,
            Width = 30
        };

        searchField.TextChanged += (_, _) => ApplyFilters();

        contactsList = new ListView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        contactsList.Accepting += (_, _) => UpdateDetail();

        FrameView listaFrame = new()
        {
            Title = " Contactos ",
            X = 1,
            Y = 4,
            Width = 35,
            Height = Dim.Fill() - 2
        };

        listaFrame.Add(contactsList);

        detailView = new TextView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true
        };

        FrameView detalleFrame = new()
        {
            Title = " Detalle ",
            X = Pos.Right(listaFrame),
            Y = 4,
            Width = Dim.Fill() - 1,
            Height = Dim.Fill() - 2
        };

        detalleFrame.Add(detailView);

        statusBar = new Label()
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "Agenda iniciada correctamente"
        };

        Add(menu, lblBuscar, searchField, listaFrame, detalleFrame, statusBar);
}
    private void RefreshList()
    {
        contactsList.SetSource<string>(new ObservableCollection<string>(filteredContacts.Select(c => c.ToString()).ToList()));

        UpdateDetail();
    }

    private void ApplyFilters()
    {
        string filtro = searchField.Text?.ToString()?.ToLower() ?? "";

        filteredContacts = contacts.Where(c => {
        bool coincideBusqueda =
        c.Nombre.ToLower().Contains(filtro) ||
        c.Telefonos.ToLower().Contains(filtro) ||
        c.Email.ToLower().Contains(filtro);

        bool coincideFavorito =
        !soloFavoritos || c.Favorito;

        return coincideBusqueda && coincideFavorito;
        }).ToList();

        RefreshList();
    }
    private void UpdateDetail()
    {
        if (filteredContacts.Count == 0)
        {
            detailView.Text = "";
            return;
        }

        if (contactsList.SelectedItem == null || contactsList.SelectedItem < 0)
        {
            detailView.Text = "";
            return;
        }

        int idx = contactsList.SelectedItem!.Value;
        if (idx < 0 || idx >= filteredContacts.Count) {
            detailView.Text = "";
            return;
        }

        Contacto c = filteredContacts[idx];

        detailView.Text = $"""
                            Nombre: {c.Nombre},
                            Teléfonos:{c.Telefonos},
                            Email:{c.Email},
                            Favorito:{(c.Favorito ? "Sí" : "No")},
                            Notas:{c.Notas}
                            """;
    }
    private void NuevoContacto() {
        ContactDialog dialog = new();
        App!.Run(dialog);
        if (!dialog.Guardado) return;

        store.Insert(dialog.Contacto);
        contacts = store.ObtenerContactos();
        ApplyFilters();

        statusBar.Text = $"Contacto {dialog.Contacto.Nombre}agregado correctamente";
    }

    private void EditarContacto()
    {
        if (filteredContacts.Count == 0)
            return;

        int idx = contactsList.SelectedItem ?? 0;

        if (idx < 0 || idx >= filteredContacts.Count)
            return;

        Contacto original = filteredContacts[idx];
        ContactDialog dialog = new(original.Clone());

        App!.Run(dialog);

        if (!dialog.Guardado)
        return;

        store.Update(dialog.Contacto);

        contacts = store.ObtenerContactos();

        ApplyFilters();

        statusBar.Text = $"Contacto '{dialog.Contacto.Nombre}' actualizado";
    }

    private void EliminarContacto()
    {
        if (filteredContacts.Count == 0)
        return;

        int idx = contactsList.SelectedItem ?? 0;

        if (idx < 0 || idx >= filteredContacts.Count)
        return;

        Contacto contacto = filteredContacts[idx];

         MessageBox.Query(
            App!,
            "Confirmar",
            $"¿Eliminar contacto '{contacto.Nombre}'?",
            "Sí",
            "No"
        );

        store.Delete(contacto);

        contacts = store.ObtenerContactos();

        ApplyFilters();

        statusBar.Text = $"Contacto '{contacto.Nombre}' eliminado";
    }

    private void ToggleFavoritos(){
        soloFavoritos = !soloFavoritos;
        ApplyFilters();

        statusBar.Text = soloFavoritos ? "Mostrando solo favoritos" : "Mostrandos todos los contactos";
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            SolicitarSalir();
            return true;
        }

        if (key == Key.N.WithCtrl) {
            NuevoContacto();
            return true;
        }

        if (key == Key.Enter) {
            EditarContacto();
            return true;
        }

        if (key == Key.F4)
        {
            searchField.SetFocus();

            return true;
        }

        if (key == Key.D.WithCtrl)
        {
            EliminarContacto();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

// Diálogo de ejemplo
public sealed class ContactDialog : Dialog {
    private readonly TextField txtNombre;
    private readonly TextField txtEmail;
    private readonly CheckBox chkFavorito;
    private readonly TextView txtNotas;

    private readonly List<TextField> telefonos = new();

    public Contacto Contacto { get; private set; } = new();
    public bool Guardado { get; private set; }

    public ContactDialog(Contacto? contacto = null) {

    Title = contacto == null ? "Nuevo contacto" : "Editar contacto";
    Width = 70;
    Height = 22;

    Add(new Label()
    {
        X = 1,
        Y = 1,
        Text = "Nombre:"
    });

    txtNombre = new TextField()
    {
        X = 15,
        Y = 1,
        Width = 40
    };

    Add(txtNombre);

    Add(new Label()
    {
        X = 1,
        Y = 3,
        Text = "Email:"
    });

    txtEmail = new TextField()
    {
        X = 15,
        Y = 3,
        Width = 40
    };

    Add(txtEmail);

    for (int i = 0; i < 5; i++)
    {
        Add(new Label()
        {
            X = 1,
            Y = 5 + i,
            Text = $"Teléfono {i + 1}:"
        });

        TextField txtTelefono = new()
        {
            X = 15,
            Y = 5 + i,
            Width = 25
        };

        telefonos.Add(txtTelefono);

        Add(txtTelefono);
    }

    chkFavorito = new CheckBox()
    {
        X = 15,
        Y = 11,
        Text = "Favorito"
    };

    Add(chkFavorito);
    
    Add(new Label()
    {
        X = 1,
        Y = 13,
        Text = "Notas:"
    });

    txtNotas = new TextView()
    {
        X = 15,
        Y = 13,
        Width = 40,
        Height = 3
    };

    Add(txtNotas);

    Button btnGuardar = new()
    {
        Text = "_Guardar"
    };

    btnGuardar.Accepting += GuardarContacto;

    Button btnCancelar = new()
    {
        Text = "_Cancelar"
    };

    btnCancelar.Accepting += (_, e) =>
    {
        App!.RequestStop();
        e.Handled = true;
    };

    if (contacto != null){
        Contacto = contacto.Clone();
        txtNombre.Text = contacto.Nombre;
        txtEmail.Text = contacto.Email;
        chkFavorito.Value = contacto.Favorito ? CheckState.Checked : CheckState.UnChecked;

        string[] numeros = contacto.Telefonos.Split(',');
        for (int i = 0; i < numeros.Length && i < telefonos.Count; i++) telefonos[i].Text = numeros[i];

        txtNotas.Text = contacto.Notas;
    }

    AddButton(btnGuardar);
    AddButton(btnCancelar);
    }
    private void GuardarContacto(object? sender, CommandEventArgs e)
    {
    string nombre = txtNombre.Text?.ToString()?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(nombre))
    {
        MessageBox.ErrorQuery(
            App!,
            "Error",
            "El nombre es obligatorio",
            "OK"
        );

        e.Handled = true;
        return;
    }

    string email = txtEmail.Text?.ToString()?.Trim() ?? "";

    if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
    {
        MessageBox.ErrorQuery(
            App!,
            "Error",
            "El email debe contener @",
            "OK"
        );

        e.Handled = true;
        return;
    }

    List<string> numeros = telefonos
        .Select(t => t.Text?.ToString()?.Trim())
        .Where(t => !string.IsNullOrWhiteSpace(t))
        .Cast<string>()
        .ToList();

    Contacto = new Contacto
    {
        Id = Contacto.Id,
        Nombre = nombre,
        Email = email,
        Telefonos = string.Join(",", numeros),
        Favorito = chkFavorito.Value == CheckState.Checked,
        Notas = txtNotas.Text?.ToString() ?? ""
    };
    
    Guardado = true;

    App!.RequestStop();

    e.Handled = true;
    }
}


public sealed class SqliteAgendaStore {
    private readonly string dbPath;
    private readonly string connectionString;

    public SqliteAgendaStore(string dbPath) {
        this.dbPath = dbPath;
        connectionString = $"Data Source={dbPath}";
        Iniciar();
    } 
    private void Iniciar() {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER NOT NULL DEFAULT 0
            )
        """);
    }
    private SqliteConnection GetConnection() {
        return new SqliteConnection(connectionString);
    }
    public List<Contacto> ObtenerContactos() {
        using var cn = GetConnection();
        cn.Open();
        return cn.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
    }
    public long Insert(Contacto contacto) {
        using var cn = GetConnection();
        return cn.Insert(contacto);
    }
    public bool Update(Contacto contacto) {
        using var cn = GetConnection();
        return cn.Update(contacto);
    }
    public bool Delete(Contacto contacto) {
        using var cn = GetConnection();
        return cn.Delete(contacto);
    }
}
public class JsonAgendaIO {
    public static void Exportar(string archivo, List<Contacto> contactos){
        JsonSerializerOptions options = new(){
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(contactos, options);
        File.WriteAllText(archivo, json);
    }

    public static List<Contacto> Importar(string archivo){
        if (!File.Exists(archivo))
        {
            throw new FileNotFoundException(
                $"No existe el archivo '{archivo}'"
            );
        }

        string json = File.ReadAllText(archivo);

        return JsonSerializer.Deserialize<List<Contacto>>(json) ?? new List<Contacto>();
    } 
}

[Table("Contactos")]
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

          public Contacto Clone() {
            return new Contacto {
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
            return Favorito ? $"★ {Nombre}" : $"  {Nombre}";
        }      
}