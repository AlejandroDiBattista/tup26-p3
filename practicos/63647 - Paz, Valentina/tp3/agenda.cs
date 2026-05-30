#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@*
#:package Dapper@*
#:package Dapper.Contrib@*
#:package Microsoft.Data.Sqlite@*
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

try
{
    var store = new SqliteAgendaStore(dbPath);

    Application.Init();

    var window = new AgendaWindow(store);

    Application.Run(window);

    Application.Shutdown();
}
catch (Exception ex)
{
    Console.WriteLine($"Error al iniciar la aplicación: {ex.Message}");
}

public class AgendaWindow : Window
{
    private readonly SqliteAgendaStore store;

    private List<Contacto> contacts = new();
    private List<Contacto> filteredContacts = new();

    private ListView contactList;
    private TextField searchField;
    private TextView detailView;
    private StatusBar statusBar;

    private bool onlyFavorites = false;

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;

        Title = "Agenda TUI";

        Width = Dim.Fill();
        Height = Dim.Fill();

        CreateMenu();
        CreateLayout();

        LoadContacts();
    }

    private void CreateMenu()
    {
        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_Archivo", new MenuItem[]
            {
                new MenuItem("_Importar JSON", "", ImportJson),
                new MenuItem("_Exportar JSON", "", ExportJson),
                new MenuItem("_Salir", "", Quit)
            }),

            new MenuBarItem("_Contactos", new MenuItem[]
            {
                new MenuItem("_Nuevo", "", NewContact),
                new MenuItem("_Editar", "", EditContact),
                new MenuItem("_Eliminar", "", DeleteContact)
            }),

            new MenuBarItem("_Ver", new MenuItem[]
            {
                new MenuItem("_Solo favoritos", "", ToggleFavorites)
            }),

            new MenuBarItem("_Ayuda", new MenuItem[]
            {
                new MenuItem("_Acerca de", "", ShowAbout)
            })
        });

        Add(menu);
    }

    private void CreateLayout()
    {
        var searchLabel = new Label("Buscar:")
        {
            X = 1,
            Y = 1
        };

        searchField = new TextField("")
        {
            X = 10,
            Y = 1,
            Width = 40
        };

        searchField.TextChanged += (_) =>
        {
            ApplyFilters();
        };

        contactList = new ListView()
        {
            X = 1,
            Y = 3,
            Width = 40,
            Height = Dim.Fill() - 2
        };

        contactList.SelectedItemChanged += (_) =>
        {
            ShowDetails();
        };

        contactList.OpenSelectedItem += (_) =>
        {
            EditContact();
        };

        detailView = new TextView()
        {
            X = 42,
            Y = 3,
            Width = Dim.Fill() - 1,
            Height = Dim.Fill() - 2,
            ReadOnly = true
        };

        statusBar = new StatusBar(new StatusItem[]
        {
            new StatusItem(Key.F2, "~F2~ Nuevo", NewContact),
            new StatusItem(Key.F3, "~F3~ Editar", EditContact),
            new StatusItem(Key.CtrlMask | Key.Q, "~Ctrl+Q~ Salir", Quit)
        });

        Add(searchLabel);
        Add(searchField);
        Add(contactList);
        Add(detailView);
        Add(statusBar);
    }

    private void LoadContacts()
    {
        contacts = store.GetAll();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        string search = searchField.Text.ToString()?.ToLower() ?? "";

        filteredContacts = contacts
            .Where(c =>
                (!onlyFavorites || c.Favorito) &&
                (
                    c.Nombre.ToLower().Contains(search) ||
                    c.Telefonos.ToLower().Contains(search) ||
                    c.Email.ToLower().Contains(search)
                )
            )
            .ToList();

        contactList.SetSource(filteredContacts
            .Select(c => c.Favorito ? $"★ {c.Nombre}" : c.Nombre)
            .ToList());

        ShowDetails();
    }

    private void ShowDetails()
    {
        if (filteredContacts.Count == 0 || contactList.SelectedItem < 0)
        {
            detailView.Text = "";
            return;
        }

        var c = filteredContacts[contactList.SelectedItem];

        detailView.Text =
$@"Nombre: {c.Nombre}

Telefonos:
{c.Telefonos}

Email:
{c.Email}

Notas:
{c.Notas}

Favorito:
{(c.Favorito ? "Sí" : "No")}";
    }

    private void NewContact()
    {
        var dialog = new ContactDialog(new Contacto());

        Application.Run(dialog);

        if (!dialog.Accepted)
            return;

        try
        {
            store.Insert(dialog.Contact);

            LoadContacts();

            MessageBox.Query("Éxito", "Contacto agregado", "OK");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void EditContact()
    {
        if (filteredContacts.Count == 0)
            return;

        var selected = filteredContacts[contactList.SelectedItem];

        var dialog = new ContactDialog(selected.Clone());

        Application.Run(dialog);

        if (!dialog.Accepted)
            return;

        try
        {
            store.Update(dialog.Contact);

            LoadContacts();

            MessageBox.Query("Éxito", "Contacto actualizado", "OK");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void DeleteContact()
    {
        if (filteredContacts.Count == 0)
            return;

        var selected = filteredContacts[contactList.SelectedItem];

        int result = MessageBox.Query(
            "Confirmar",
            $"¿Eliminar a {selected.Nombre}?",
            "Sí",
            "No"
        );

        if (result != 0)
            return;

        store.Delete(selected.Id);

        LoadContacts();

        MessageBox.Query("Éxito", "Contacto eliminado", "OK");
    }

    private void ToggleFavorites()
    {
        onlyFavorites = !onlyFavorites;
        ApplyFilters();
    }

    private void ImportJson()
    {
        var dialog = new OpenDialog("Importar JSON", "Seleccione un archivo")
        {
            AllowsMultipleSelection = false
        };

        Application.Run(dialog);

        if (dialog.Canceled || dialog.FilePaths.Count == 0)
            return;

        try
        {
            var imported = JsonAgendaIO.Import(dialog.FilePaths[0].ToString());

            int confirm = MessageBox.Query(
                "Importar",
                $"Se agregarán {imported.Count} contactos. ¿Continuar?",
                "Sí",
                "No"
            );

            if (confirm != 0)
                return;

            foreach (var c in imported)
            {
                c.Id = 0;
                store.Insert(c);
            }

            LoadContacts();

            MessageBox.Query("Éxito", "Importación completada", "OK");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void ExportJson()
    {
        var dialog = new SaveDialog("Exportar JSON", "Guardar archivo")
        {
            FileName = "contactos.json"
        };

        Application.Run(dialog);

        if (dialog.Canceled)
            return;

        try
        {
            JsonAgendaIO.Export(dialog.FilePath.ToString(), contacts);

            MessageBox.Query("Éxito", "Exportación completada", "OK");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void ShowAbout()
    {
        MessageBox.Query(
            "Acerca de",
            "Agenda TUI\nTrabajo Práctico 3\nProgramación I",
            "OK"
        );
    }

    private void Quit()
    {
        Application.RequestStop();
    }
}

public class ContactDialog : Dialog
{
    public Contacto Contact { get; private set; }

    public bool Accepted { get; private set; }

    private TextField txtNombre;
    private TextField txtTelefono1;
    private TextField txtTelefono2;
    private TextField txtTelefono3;
    private TextField txtTelefono4;
    private TextField txtTelefono5;
    private TextField txtEmail;
    private TextView txtNotas;
    private CheckBox chkFavorito;

    public ContactDialog(Contacto contacto)
    {
        Contact = contacto;

        Title = "Contacto";

        Width = 60;
        Height = 25;

        Add(new Label("Nombre:") { X = 1, Y = 1 });

        txtNombre = new TextField(contacto.Nombre)
        {
            X = 15,
            Y = 1,
            Width = 35
        };

        Add(txtNombre);

        string[] phones = contacto.Telefonos.Split(',');

        Add(new Label("Teléfono 1:") { X = 1, Y = 3 });
        txtTelefono1 = new TextField(phones.ElementAtOrDefault(0)?.Trim() ?? "")
        {
            X = 15,
            Y = 3,
            Width = 35
        };
        Add(txtTelefono1);

        Add(new Label("Teléfono 2:") { X = 1, Y = 4 });
        txtTelefono2 = new TextField(phones.ElementAtOrDefault(1)?.Trim() ?? "")
        {
            X = 15,
            Y = 4,
            Width = 35
        };
        Add(txtTelefono2);

        Add(new Label("Teléfono 3:") { X = 1, Y = 5 });
        txtTelefono3 = new TextField(phones.ElementAtOrDefault(2)?.Trim() ?? "")
        {
            X = 15,
            Y = 5,
            Width = 35
        };
        Add(txtTelefono3);

        Add(new Label("Teléfono 4:") { X = 1, Y = 6 });
        txtTelefono4 = new TextField(phones.ElementAtOrDefault(3)?.Trim() ?? "")
        {
            X = 15,
            Y = 6,
            Width = 35
        };
        Add(txtTelefono4);

        Add(new Label("Teléfono 5:") { X = 1, Y = 7 });
        txtTelefono5 = new TextField(phones.ElementAtOrDefault(4)?.Trim() ?? "")
        {
            X = 15,
            Y = 7,
            Width = 35
        };
        Add(txtTelefono5);

        Add(new Label("Email:") { X = 1, Y = 9 });

        txtEmail = new TextField(contacto.Email)
        {
            X = 15,
            Y = 9,
            Width = 35
        };

        Add(txtEmail);

        Add(new Label("Notas:") { X = 1, Y = 11 });

        txtNotas = new TextView()
        {
            X = 15,
            Y = 11,
            Width = 35,
            Height = 5,
            Text = contacto.Notas
        };

        Add(txtNotas);

        chkFavorito = new CheckBox("Favorito")
        {
            X = 15,
            Y = 17,
            CheckedState = contacto.Favorito
                ? CheckState.Checked
                : CheckState.UnChecked
        };

        Add(chkFavorito);

        var btnGuardar = new Button("Guardar")
        {
            X = 15,
            Y = 20
        };

        btnGuardar.Accepting += (_) =>
        {
            Save();
        };

        var btnCancelar = new Button("Cancelar")
        {
            X = 30,
            Y = 20
        };

        btnCancelar.Accepting += (_) =>
        {
            Application.RequestStop();
        };

        Add(btnGuardar);
        Add(btnCancelar);
    }

    private void Save()
    {
        string nombre = txtNombre.Text.ToString()?.Trim() ?? "";
        string email = txtEmail.Text.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.ErrorQuery(
                "Error",
                "El nombre no puede estar vacío",
                "OK"
            );
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
        {
            MessageBox.ErrorQuery(
                "Error",
                "El email debe contener @",
                "OK"
            );
            return;
        }

        List<string> phones = new()
        {
            txtTelefono1.Text.ToString(),
            txtTelefono2.Text.ToString(),
            txtTelefono3.Text.ToString(),
            txtTelefono4.Text.ToString(),
            txtTelefono5.Text.ToString()
        };

        Contact.Nombre = nombre;
        Contact.Telefonos = string.Join(", ",
            phones.Where(p => !string.IsNullOrWhiteSpace(p)));

        Contact.Email = email;
        Contact.Notas = txtNotas.Text.ToString();
        Contact.Favorito =
            chkFavorito.CheckedState == CheckState.Checked;

        Accepted = true;

        Application.RequestStop();
    }
}

public class SqliteAgendaStore
{
    private readonly string connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        connectionString = $"Data Source={dbPath}";

        using var connection = new SqliteConnection(connectionString);

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER
            )
        ");
    }

    private SqliteConnection GetConnection()
    {
        return new SqliteConnection(connectionString);
    }

    public List<Contacto> GetAll()
    {
        using var connection = GetConnection();

        return connection.GetAll<Contacto>().ToList();
    }

    public void Insert(Contacto contacto)
    {
        using var connection = GetConnection();

        connection.Insert(contacto);
    }

    public void Update(Contacto contacto)
    {
        using var connection = GetConnection();

        connection.Update(contacto);
    }

    public void Delete(int id)
    {
        using var connection = GetConnection();

        var contacto = connection.Get<Contacto>(id);

        if (contacto != null)
        {
            connection.Delete(contacto);
        }
    }
}

public static class JsonAgendaIO
{
    public static void Export(string path, List<Contacto> contactos)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(contactos, options);

        File.WriteAllText(path, json);
    }

    public static List<Contacto> Import(string path)
    {
        if (!File.Exists(path))
        {
            throw new Exception("El archivo JSON no existe");
        }

        string json = File.ReadAllText(path);

        var contactos = JsonSerializer.Deserialize<List<Contacto>>(json);

        return contactos ?? new List<Contacto>();
    }
}

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
}