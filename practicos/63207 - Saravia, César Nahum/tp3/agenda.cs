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
        Add(menu);
        Label searchLabel = new() {
            Text = "Buscar:",
            X = 1,
            Y = 1
        };
        searchField = new TextField() {
            X = 10,
            Y = 1,
            Width = 40,
            Text = ""
        };
        searchField.TextChanged += (_, _) => ApplyFilters();
        contactsList = new ListView() {
            X = 1,
            Y = 3,
            Width = 30,
            Height = Dim.Fill()
        };
        FrameView contactosFrame = new() {
            Title = "Contactos",
            X = 1,
            Y = 3,
            Width = 30,
            Height = Dim.Fill(1)
        };
        contactsList = new ListView() {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        contactsList.RowRender += (_, _) => UpdateDetail();
        contactosFrame.Add(contactsList);

        FrameView detalleFrame = new() {
            Title = "Detalle",
            X = 32,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        detailView = new TextView() {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true
        };
        detalleFrame.Add(detailView);

        Add(menu, searchLabel, searchField, contactosFrame, detalleFrame);
         statusLabel = new Label() {
            Text = "F2 Nuevo | F3 Editar | Del Eliminar | F4 Buscar | Ctrl+I Importar | Ctrl+E Exportar | Ctrl+Q Salir",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
         };
        Add(statusLabel);
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
        ObservableCollection<string> items = new(filteredContacts.Select(c => c.Favorito ? $"★ {c.Nombre}" : c.Nombre));
        contactsList.SetSource<string>(items);
        UpdateDetail();
    }

    private void UpdateDetail() {
        if (filteredContacts.Count == 0) {
            detailView.Text = "No hay contactos para mostrar.";
            return;
        }
        int index = contactsList.SelectedItem.HasValue ? contactsList.SelectedItem.Value : 0;
        if (index < 0 || index >= filteredContacts.Count)
            return;
            Contacto c = filteredContacts[index];
            detailView.Text = $"Nombre: {c.Nombre}\n" +
                              $"Teléfonos: {c.Telefonos}\n" +
                              $"Email: {c.Email}\n" +
                              $"Favorito: {(c.Favorito ? "Sí" : "No")}\n"+
                              $"Notas: {c.Notas}\n";                   
    }

    private Contacto? GetSelected() {
        
        if (filteredContacts.Count ==0)
        return null;
        int index = contactsList.SelectedItem.HasValue? contactsList.SelectedItem.Value : 0;
        if (index < 0 || index >= filteredContacts.Count)
            return null;
        return filteredContacts[index];
    }

    private void NuevoContacto() {
        ContactDialog dialog = new(new Contacto());
        App!.Run(dialog);
        if (!dialog.Accepted)
            return;
            store.Insert(dialog.Contacto);
            LoadContacts();
            SetStatus("Contacto agregado");
    }

    private void EditarContacto() {
        Contacto? selected = GetSelected();
        if (selected == null)
            return;
        ContactDialog dialog = new(selected.Clone());
        App!.Run(dialog);
        if (!dialog.Accepted)
            return;
            store.Update(dialog.Contacto);
            LoadContacts();
            SetStatus("Contacto actualizado");
    }

    private void EliminarContacto() {
        Contacto? selected = GetSelected();
        if (selected == null)
            return;
        int result = MessageBox.Query(App!, "Confirmar",
            $"¿Eliminar contacto '{selected.Nombre}'?", 
            "Sí", 
            "No"
            ) ?? 0;
            if (result !=0)
            return;
            store.Delete(selected);
            LoadContacts();
            SetStatus("Contacto eliminado");
    }

    private void ToggleFavorites() {
        onlyFavorites = !onlyFavorites;
        ApplyFilters();
    }

    private string? PedirNombreArchivo(string titulo, string valorDefault) {
        string? resultado = null;
        Dialog dialog = new() {
            Title = titulo,
            Width = 50,
            Height = 8
        };
        Label label = new() {Text = "Archivo:", X = 1, Y = 1};
        TextField field = new() {
            X= 11,
            Y= 1,
            Width = 30,
            Text = valorDefault
        };
        Button okButton = new() {Text = "_OK", X = 11, Y = 3};
        okButton.Accepting +=(_, e) => {
            resultado = field.Text.ToString()?.Trim();
            dialog.App!.RequestStop();
            e.Handled = true;
        };
        Button cancelButton = new() {Text = "_Cancelar"};
        cancelButton.Accepting += (_, e) => {
            dialog.App!.RequestStop();
            e.Handled = true;
        };
        dialog.Add(label, field);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);
        App!.Run(dialog);
        return resultado;
    }

    private void ImportJson() {
        string? archivo = PedirNombreArchivo("Importar desde JSON", "contactos.json");
        if(string.IsNullOrWhiteSpace(archivo))
            return;
        try {
            List<Contacto> imported = JsonAgendaIO.Import(archivo);
            int result = MessageBox.Query(
                App!,
                "Importar",
                $"Agregar {imported.Count} contactos?",
                "Sí",
                "No"
            )?? 0;
            if (result != 0)
                return;
            foreach (Contacto c in imported) {
                c.Id = 0;
                store.Insert(c);
            }
            LoadContacts();
            SetStatus($"{imported.Count} contactos importados");
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, 
            "Error", 
            ex.Message,
            "OK");
        }
    }

    private void ExportJson() {
        string? archivo = PedirNombreArchivo("Exportar a JSON", "contactos.json");
        if(string.IsNullOrWhiteSpace(archivo))
            return;
        try {
            JsonAgendaIO.Export(archivo, contacts);
            MessageBox.Query(
                App!,
                "Exportar",
                "Archivo exportado",
                "OK"
            );
        } catch (Exception ex) {
            MessageBox.ErrorQuery(
            App!, 
            "Error", 
            ex.Message,
            "OK");
        }
    }

    private void AcercaDe() {
        MessageBox.Query(App!, 
            "Acerca de", 
            "AgendaT - TP3",
            "OK");
    }

    private void FocoBusqueda() {
        if (searchField != null) {
            searchField.SetFocus();
        }
    }

    private void SetStatus(string mensaje) {
        statusLabel.Text = $"{mensaje}  | F2 Nuevo | F3 Editar | F4 Buscar | Del Eliminar | Ctrl+Q Salir | Ctrl+I Importar | Ctrl+E Exportar";
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            SolicitarSalir();
            return true;
        }
        if(key == Key.F2) {
            NuevoContacto();
            return true;
        }
        if(key == Key.N.WithCtrl) {
            NuevoContacto();
            return true;
        }
        if(key == Key.F3) {
            EditarContacto();
            return true;
        }
        if (key == Key.Enter) {
            EditarContacto();
            return true;
        }
        if (key == Key.DeleteChar) {
            EliminarContacto();
            return true;
        }
        if (key == Key.D.WithCtrl) {
            EliminarContacto();
            return true;
        }
        if (key == Key.I.WithCtrl) {
            ImportJson();
            return true;
        }
        if (key == Key.E.WithCtrl) {
            ExportJson();
            return true;
        }
        if(key == Key.F4) {
            FocoBusqueda();
            return true;
        }
        return base.OnKeyDown(key);
    }
}

// Diálogo de ejemplo
public sealed class ContactDialog : Dialog {
    public new bool Accepted { get; private set; }
    public Contacto Contacto { get; private set; }
    private readonly TextField nombreField;
    private readonly TextField[] telefonosFields = new TextField[5];
    private readonly TextField emailField;
    private readonly TextView notasField;
    private CheckBox favoritoCheck;
    public ContactDialog(Contacto contacto) {
        Contacto = contacto;
        Title  = "Contacto";
        Width  = 60;
        Height = 32;

        Add (new Label() {
            Text = "Nombre",
            X = 1,
            Y = 1
        });

        nombreField = new TextField() {
            X = 15,
            Y = 1,
            Width = 40,
            Text = contacto.Nombre
        };
        Add(nombreField);
        string[] telefonosGuardados = contacto.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for(int i = 0; i < 5; i++) {
            Add(new Label() {
                Text = $"Teléfono {i+1}:",
                X = 1,
                Y = 3 + i*2
            });
            telefonosFields[i] = new TextField() {
                X = 15,
                Y = 3 + i*2,
                Width = 40,
                Text = i < telefonosGuardados.Length ? telefonosGuardados[i].Trim() : ""
            };
            Add(telefonosFields[i]);
        }
        Add(new Label() {
            Text = "Email:",
            X = 1,
            Y = 13
        });
        emailField = new TextField() {
            X = 15,
            Y = 13,
            Width = 40,
            Text = contacto.Email
        };
        Add(emailField);
        
        string textoInicial = contacto.Favorito? "Favorito (si)" : "Favorito (No)";
        favoritoCheck = new CheckBox() {
            Text = "Favorito",
            X = 15,
            Y = 15,
        };
        favoritoCheck.Value = contacto.Favorito ? CheckState.Checked : CheckState.UnChecked;
        Add(favoritoCheck);
        Add(new Label() {
            Text = "Notas:",
            X = 1,
            Y = 17
        });
        notasField = new TextView() {
            X = 15,
            Y = 17,
            Width = 40,
            Height = 4,
            Text = contacto.Notas            
        };
        Add(notasField);
        Button saveButton = new() {
            Text = "_Guardar",
            X = 15,
            Y = 22
        };
        saveButton.Accepting += (_, e) => {
            Save();
            e.Handled = true;
        };
        AddButton(saveButton);
        Button cancelButton = new() {
            Text = "_Cancelar"
        };
        cancelButton.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };
        AddButton(cancelButton);
}

    private void Save() {
        string nombre = nombreField.Text.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre)) {
            MessageBox.ErrorQuery(App!, "Error", "El nombre no puede estar vacío.", "OK");
            return;
        }
        string email = emailField.Text.ToString() ?? "";
        if (
            email.Length > 0 &&
            !email.Contains("@")
        ) {
            MessageBox.ErrorQuery(App!, "Error", "El email debe contener @", "OK");
            return;
        }
        Contacto.Nombre = nombre;
        string primero = telefonosFields[0].Text.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(primero)) {
            MessageBox.ErrorQuery(App!, "Error", "Al menos un teléfono es requerido", "OK");
            return;
        }
        Contacto.Telefonos = string.Join(",", telefonosFields.Select(f => f.Text.ToString()?.Trim() ?? "")
        .Where(t => !string.IsNullOrWhiteSpace(t))
        );
        Contacto.Email = email;
        Contacto.Notas = notasField.Text.ToString() ?? "";
        Contacto.Favorito = (favoritoCheck.Value == CheckState.Checked);
        Accepted = true;
        App!.RequestStop();
    }
}

public class SqliteAgendaStore {
    private readonly string dbPath;
    
    public SqliteAgendaStore(string dbPath) {
        this.dbPath = dbPath;
        Initialize();
    }

    private DbConnection GetConnection() {
        return new SqliteConnection($"Data Source={dbPath}");
    }
    private void Initialize() {
        using DbConnection connection = GetConnection(); 
        connection.Open();
        connection.Execute("""
         CREATE TABLE IF NOT EXISTS Contactos(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Telefonos TEXT,
            Email TEXT,
            Notas TEXT,
             Favorito INTEGER NOT NULL );
         """);
    }

    public List<Contacto> GetAll() {
        using DbConnection connection = GetConnection();
        connection.Open();
        return connection.GetAll<Contacto>().ToList();
    }

    public void Insert(Contacto contacto) {
        using DbConnection connection = GetConnection();
        connection.Open();
        connection.Insert(contacto);
    }

    public void Update(Contacto contacto) {
        using DbConnection connection = GetConnection();
        connection.Open();
        connection.Update(contacto);
    }

    public void Delete(Contacto contacto) {
        using DbConnection connection = GetConnection();
        connection.Open();
        connection.Delete(contacto);
    }
}
public class JsonAgendaIO {
    
    public static void Export (
        string path, List<Contacto> contactos
    ) {
        JsonSerializerOptions options = new() {
            WriteIndented = true
        };
        string json = JsonSerializer.Serialize(contactos, options);
        File.WriteAllText(path, json);
    }
    public static List<Contacto> Import(
        string path) 
    {
        if (!File.Exists (path))
        throw new Exception("El archivo JSON no existe");
        string json = File.ReadAllText(path);
        List<Contacto>? contactos = JsonSerializer.Deserialize<List<Contacto>>(json);
        if (contactos == null)
        throw new Exception(" JSON inválido");
        return contactos;
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
}