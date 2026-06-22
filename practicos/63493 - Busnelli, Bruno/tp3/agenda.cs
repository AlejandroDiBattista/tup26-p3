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
using Dapper.Contrib.Extensions;
using System.Text.Json;
using System.Text;

/// ====
/// Trabajo Práctico 3 - AgendaT
/// Aplicación de agenda TUI con persistencia SQLite e import/export JSON.
/// ====

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store = new(dbPath);
store.Initialize();

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));


// Ventana principal
public sealed class AgendaWindow : Runnable
{
    private readonly SqliteAgendaStore store;

    private readonly List<Contacto> contacts = [];
    private readonly List<Contacto> filteredContacts = [];

    private TextField searchField = null!;
    private TextView listView = null!;
    private TextView detailView = null!;
    private Label statusLabel = null!;

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;

        Title = "AgendaT - Terminal.Gui";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;

        LoadContacts();
        BuildLayout();
        RefreshViews("Base cargada correctamente.");
    }

    private void LoadContacts()
    {
        contacts.Clear();
        contacts.AddRange(store.GetAll());

        filteredContacts.Clear();
        filteredContacts.AddRange(contacts);
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", MostrarPendiente),
                    new MenuItem("_Exportar JSON", "Ctrl+E", MostrarPendiente),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),

                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo", "F2 / Ctrl+N", AbrirDialogo),
                    new MenuItem("_Editar", "F3 / Enter", MostrarPendiente),
                    new MenuItem("_Eliminar", "Del / Ctrl+D", MostrarPendiente)
                ]),

                new MenuBarItem("_Ver",
                [
                    new MenuItem("_Solo favoritos", null!, MostrarPendiente)
                ]),

                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", null!, MostrarAcercaDe)
                ])
            ]
        };

        Add(menu);

        Add(new Label()
        {
            Text = "Buscar:",
            X = 1,
            Y = 1
        });

        searchField = new TextField()
        {
            X = 10,
            Y = 1,
            Width = Dim.Fill(1)
        };

        Add(searchField);

        FrameView listPanel = new()
        {
            Title = "Contactos",
            X = 0,
            Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1)
        };

        listView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        listPanel.Add(listView);

        FrameView detailPanel = new()
        {
            Title = "Detalle",
            X = Pos.Right(listPanel),
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        detailView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        detailPanel.Add(detailView);

        statusLabel = new Label()
        {
            Text = "Listo.",
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill()
        };

        Add(listPanel, detailPanel, statusLabel);
    }

    private void RefreshViews(string status)
    {
        listView.Text = BuildContactListText();
        detailView.Text = BuildDetailText();
        statusLabel.Text = status;
    }

    private string BuildContactListText()
    {
        if (filteredContacts.Count == 0)
        {
            return "No hay contactos cargados.";
        }

        StringBuilder builder = new();

        for (int i = 0; i < filteredContacts.Count; i++)
        {
            Contacto contacto = filteredContacts[i];

            builder.AppendLine(
                $"{i + 1}. {(contacto.Favorito ? "★ " : "")}{contacto.Nombre}"
            );
        }

        return builder.ToString();
    }

    private string BuildDetailText()
    {
        if (filteredContacts.Count == 0)
        {
            return "Seleccione o cree un contacto para ver su detalle.";
        }

        Contacto contacto = filteredContacts[0];

        return $"""
        Nombre: {contacto.Nombre}
        Telefonos: {contacto.Telefonos}
        Email: {contacto.Email}
        Favorito: {(contacto.Favorito ? "Si" : "No")}

        Notas:
        {contacto.Notas}
        """;
    }

    private void AbrirDialogo()
    {
        ContactDialog dialog = new();
        App!.Run(dialog);

        if (dialog.Contacto is null)
        {
            RefreshViews("Operacion cancelada.");
            return;
        }

        Contacto nuevo = dialog.Contacto;
        nuevo.Id = store.Insert(nuevo);

        contacts.Add(nuevo);

        filteredContacts.Clear();
        filteredContacts.AddRange(
            contacts
                .OrderByDescending(contacto => contacto.Favorito)
                .ThenBy(contacto => contacto.Nombre)
        );

        RefreshViews($"Contacto '{nuevo.Nombre}' agregado correctamente.");
    }

    private void MostrarPendiente()
    {
        MessageBox.Query(
            App!,
            "Pendiente",
            "Esta opcion se completara en el proximo commit.",
            "OK"
        );
    }

    private void MostrarAcercaDe()
    {
        MessageBox.Query(
            App!,
            "Acerca de",
            "AgendaT - Trabajo Practico 3\nAgenda TUI con SQLite y JSON.",
            "OK"
        );
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

        if (key == Key.F2 || key == Key.N.WithCtrl)
        {
            AbrirDialogo();
            return true;
        }

        return base.OnKeyDown(key);
    }
}


// Diálogo de edición
public sealed class ContactDialog : Dialog
{
    public Contacto? Contacto { get; private set; }

    private readonly TextField nombreField;
    private readonly TextField[] telefonoFields;
    private readonly TextField emailField;
    private readonly TextView notasField;
    private readonly CheckBox favoritoCheck;

    public ContactDialog(Contacto? contacto = null)
    {
        contacto ??= new Contacto();

        Title = contacto.Id == 0
            ? "Nuevo contacto"
            : "Editar contacto";

        Width = 70;
        Height = 24;

        Add(new Label()
        {
            Text = "Nombre:",
            X = 1,
            Y = 1
        });

        nombreField = new TextField()
        {
            X = 18,
            Y = 1,
            Width = 42,
            Text = contacto.Nombre
        };

        Add(nombreField);

        telefonoFields = new TextField[5];

        string[] telefonosCargados = contacto.Telefonos
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < 5; i++)
        {
            Add(new Label()
            {
                Text = $"Telefono {i + 1}:",
                X = 1,
                Y = 3 + i
            });

            telefonoFields[i] = new TextField()
            {
                X = 18,
                Y = 3 + i,
                Width = 42,
                Text = i < telefonosCargados.Length ? telefonosCargados[i] : ""
            };

            Add(telefonoFields[i]);
        }

        Add(new Label()
        {
            Text = "Email:",
            X = 1,
            Y = 9
        });

        emailField = new TextField()
        {
            X = 18,
            Y = 9,
            Width = 42,
            Text = contacto.Email
        };

        Add(emailField);

        Add(new Label()
        {
            Text = "Notas:",
            X = 1,
            Y = 11
        });

        notasField = new TextView()
        {
            X = 18,
            Y = 11,
            Width = 42,
            Height = 4,
            Text = contacto.Notas
        };

        Add(notasField);

        favoritoCheck = new CheckBox()
        {
            Text = "Favorito",
            X = 18,
            Y = 16,
            Value = contacto.Favorito
                ? CheckState.Checked
                : CheckState.UnChecked
        };

        Add(favoritoCheck);

        Button guardarButton = new()
        {
            Text = "_Guardar",
            IsDefault = true
        };

        guardarButton.Accepting += (_, e) =>
        {
            Guardar(contacto);
            e.Handled = true;
        };

        Button cancelarButton = new()
        {
            Text = "_Cancelar"
        };

        cancelarButton.Accepting += (_, e) =>
        {
            App!.RequestStop();
            e.Handled = true;
        };

        AddButton(guardarButton);
        AddButton(cancelarButton);
    }

    private void Guardar(Contacto original)
    {
        string nombre = nombreField.Text.ToString() ?? "";
        string email = emailField.Text.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.ErrorQuery(
                App!,
                "Error",
                "El nombre no puede estar vacio.",
                "OK"
            );

            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
        {
            MessageBox.ErrorQuery(
                App!,
                "Error",
                "El email debe contener @.",
                "OK"
            );

            return;
        }

        Contacto editado = original.Clone();

        editado.Nombre = nombre.Trim();

        editado.Telefonos = string.Join(", ",
            telefonoFields
                .Select(field => field.Text.ToString() ?? "")
                .Select(text => text.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Take(5)
        );

        editado.Email = email.Trim();
        editado.Notas = notasField.Text.ToString() ?? "";

        editado.Favorito =
            favoritoCheck.Value == CheckState.Checked;

        Contacto = editado;

        App!.RequestStop();
    }
}


// Persistencia SQLite
public sealed class SqliteAgendaStore
{
    private readonly string connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using SqliteConnection connection = OpenConnection();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Notas TEXT NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            );
        """);
    }

    public List<Contacto> GetAll()
    {
        using SqliteConnection connection = OpenConnection();

        return connection
            .GetAll<Contacto>()
            .OrderByDescending(contacto => contacto.Favorito)
            .ThenBy(contacto => contacto.Nombre)
            .ToList();
    }

    public int Insert(Contacto contacto)
    {
        using SqliteConnection connection = OpenConnection();

        long id = connection.Insert(contacto);
        return (int)id;
    }

    public bool Update(Contacto contacto)
    {
        using SqliteConnection connection = OpenConnection();

        return connection.Update(contacto);
    }

    public bool Delete(Contacto contacto)
    {
        using SqliteConnection connection = OpenConnection();

        return connection.Delete(contacto);
    }

    private SqliteConnection OpenConnection()
    {
        SqliteConnection connection = new(connectionString);
        connection.Open();
        return connection;
    }
}


// Interoperabilidad JSON
public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions options =
        new()
        {
            WriteIndented = true
        };

    public static void Export(string filePath, IEnumerable<Contacto> contactos)
    {
        string json = JsonSerializer.Serialize(contactos, options);

        File.WriteAllText(
            filePath,
            json,
            new UTF8Encoding(false)
        );
    }

    public static List<Contacto> Import(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"No existe el archivo: {filePath}"
            );
        }

        string json = File.ReadAllText(
            filePath,
            Encoding.UTF8
        );

        List<Contacto>? contactos =
            JsonSerializer.Deserialize<List<Contacto>>(json);

        return contactos ?? [];
    }
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