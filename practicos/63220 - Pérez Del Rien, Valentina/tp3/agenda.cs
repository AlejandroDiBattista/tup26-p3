#:property PublishAot=false
#:property PublishTrimmed=false
#:property EnableAotAnalyzer=false
#:property EnableTrimAnalyzer=false

#:package Terminal.Gui@2.0.0
#:package Microsoft.Data.Sqlite@10.0.8
#:package Dapper@2.1.79
#:package Dapper.Contrib@2.0.78

using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui;

var dbPath = args.Length > 0 ? args[0] : "agenda.db";

var store = new SqliteAgendaStore(dbPath);

Application.Init();

Application.Run(new AgendaWindow(store));

Application.Shutdown();

public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore store;

    private readonly AgendaListView contactList;
    private readonly AgendaTextField searchField;
    private readonly Label detailLabel;
    private readonly Label statusLabel;

    private List<Contacto> contacts = new();
    private List<Contacto> filteredContacts = new();

    private bool soloFavoritos = false;

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;

        Title = "Agenda TUI";
        Width = Dim.Fill();
        Height = Dim.Fill();

        contacts = store.GetAll();
        filteredContacts = contacts.ToList();

        var menu = new MenuBar
        {
            Menus = new MenuBarItem[]
            {
                new MenuBarItem("_Archivo", new MenuItem[]
                {
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson),
                    new MenuItem("_Salir", "Ctrl+Q", Salir)
                }),

                new MenuBarItem("_Contactos", new MenuItem[]
                {
                    new MenuItem("_Nuevo", "F2 / Ctrl+N", NuevoContacto),
                    new MenuItem("_Editar", "F3 / Enter", EditarContacto),
                    new MenuItem("_Eliminar", "Delete / Ctrl+D", EliminarContacto)
                }),

                new MenuBarItem("_Ver", new MenuItem[]
                {
                    new MenuItem("_Solo favoritos", "Toggle", CambiarSoloFavoritos)
                }),

                new MenuBarItem("_Ayuda", new MenuItem[]
                {
                    new MenuItem("_Acerca de", "", MostrarAcercaDe)
                })
            }
        };

        Add(menu);

        Add(new Label { Text = "Buscar:", X = 1, Y = 2 });

        searchField = new AgendaTextField(ManejarAtajo)
        {
            Text = "",
            X = 10,
            Y = 2,
            Width = 45
        };

        searchField.TextChanged += (sender, e) => Filtrar();

        Add(searchField);

        contactList = new AgendaListView(ManejarAtajo)
        {
            X = 1,
            Y = 4,
            Width = 45,
            Height = Dim.Fill(3)
        };

        contactList.SelectedItemChanged += (sender, e) => MostrarDetalle();

        Add(contactList);

        detailLabel = new Label
        {
            Text = "Seleccione un contacto",
            X = 50,
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };

        Add(detailLabel);

        statusLabel = new Label
        {
            Text = "Agenda iniciada correctamente",
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill()
        };

        Add(statusLabel);

        ActualizarLista();
    }

    protected override bool OnKeyDown(Key key)
    {
        return ManejarAtajo(key) || base.OnKeyDown(key);
    }

    private bool ManejarAtajo(Key key)
    {
        if (key == Key.F2 || key == Key.N.WithCtrl)
        {
            NuevoContacto();
            return true;
        }

        if (key == Key.F3 || key == Key.Enter)
        {
            EditarContacto();
            return true;
        }

        if (key == Key.DeleteChar || key == Key.D.WithCtrl)
        {
            EliminarContacto();
            return true;
        }

        if (key == Key.I.WithCtrl)
        {
            ImportarJson();
            return true;
        }

        if (key == Key.E.WithCtrl)
        {
            ExportarJson();
            return true;
        }

        if (key == Key.F4)
        {
            searchField.SetFocus();
            statusLabel.Text = "Foco en búsqueda";
            return true;
        }

        if (key == Key.Q.WithCtrl)
        {
            Salir();
            return true;
        }

        return false;
    }

    private void ActualizarLista()
    {
        contactList.SetSource<string>(
            new ObservableCollection<string>(
                filteredContacts.Select(c => MostrarNombre(c)).ToList()
            )
        );
    }

    private void Filtrar()
    {
        var texto = searchField.Text?.ToString() ?? "";

        filteredContacts = contacts
            .Where(c =>
                (
                    c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    c.Telefonos.Contains(texto, StringComparison.OrdinalIgnoreCase)
                )
                &&
                (!soloFavoritos || c.Favorito)
            )
            .ToList();

        ActualizarLista();
        MostrarDetalle();
    }

    private string MostrarNombre(Contacto contacto)
    {
        return contacto.Favorito ? $"★ {contacto.Nombre}" : contacto.Nombre;
    }

    private Contacto? ObtenerSeleccionado()
    {
        if (filteredContacts.Count == 0)
        {
            return null;
        }

        var index = contactList.SelectedItem;

        if (index < 0 || index >= filteredContacts.Count)
        {
            return null;
        }

        return filteredContacts[index];
    }

    private void MostrarDetalle()
    {
        var contacto = ObtenerSeleccionado();

        if (contacto == null)
        {
            detailLabel.Text = "Seleccione un contacto";
            return;
        }

        detailLabel.Text =
            $"Id: {contacto.Id}\n" +
            $"Nombre: {contacto.Nombre}\n" +
            $"Teléfonos: {contacto.Telefonos}\n" +
            $"Email: {contacto.Email}\n" +
            $"Favorito: {(contacto.Favorito ? "Sí" : "No")}\n\n" +
            $"Notas:\n{contacto.Notas}";
    }

    private void NuevoContacto()
    {
        var contacto = ContactDialog.CrearNuevo();

        if (contacto == null)
        {
            statusLabel.Text = "Alta cancelada";
            return;
        }

        store.Insert(contacto);
        Recargar();

        statusLabel.Text = "Contacto agregado correctamente";
    }

    private void EditarContacto()
    {
        var seleccionado = ObtenerSeleccionado();

        if (seleccionado == null)
        {
            MessageBox.ErrorQuery("Error", "Debe seleccionar un contacto.", "Aceptar");
            return;
        }

        var editado = ContactDialog.Editar(seleccionado);

        if (editado == null)
        {
            statusLabel.Text = "Edición cancelada";
            return;
        }

        store.Update(editado);
        Recargar();

        statusLabel.Text = "Contacto editado correctamente";
    }

    private void EliminarContacto()
    {
        var contacto = ObtenerSeleccionado();

        if (contacto == null)
        {
            MessageBox.ErrorQuery("Error", "Debe seleccionar un contacto.", "Aceptar");
            return;
        }

        var respuesta = MessageBox.Query(
            "Confirmar eliminación",
            $"¿Seguro que desea eliminar a {contacto.Nombre}?",
            "Sí",
            "No"
        );

        if (respuesta == 0)
        {
            store.Delete(contacto);
            Recargar();

            statusLabel.Text = "Contacto eliminado correctamente";
        }
    }

    private void CambiarSoloFavoritos()
    {
        soloFavoritos = !soloFavoritos;
        Filtrar();

        statusLabel.Text = soloFavoritos
            ? "Filtro activo: solo favoritos"
            : "Filtro desactivado: todos los contactos";
    }

    private void ExportarJson()
    {
        try
        {
            var ruta = "salida.json";

            JsonAgendaIO.Exportar(ruta, contacts);

            statusLabel.Text = $"Contactos exportados en {ruta}";

            MessageBox.Query("Exportación JSON", $"Se exportó correctamente:\n{ruta}", "Aceptar");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "Aceptar");
        }
    }

    private void ImportarJson()
    {
        try
        {
            var ruta = "salida.json";

            var importados = JsonAgendaIO.Importar(ruta);

            if (importados.Count == 0)
            {
                MessageBox.ErrorQuery("Importar JSON", "El archivo no tiene contactos.", "Aceptar");
                return;
            }

            var respuesta = MessageBox.Query(
                "Confirmar importación",
                $"Se van a importar {importados.Count} contactos nuevos.\n¿Desea continuar?",
                "Sí",
                "No"
            );

            if (respuesta != 0)
            {
                statusLabel.Text = "Importación cancelada";
                return;
            }

            foreach (var contacto in importados)
            {
                contacto.Id = 0;
                store.Insert(contacto);
            }

            Recargar();

            statusLabel.Text = $"Se importaron {importados.Count} contactos";
        }
        catch (FileNotFoundException)
        {
            MessageBox.ErrorQuery("Error", "No existe el archivo salida.json.", "Aceptar");
        }
        catch (JsonException)
        {
            MessageBox.ErrorQuery("Error", "El JSON tiene formato inválido.", "Aceptar");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "Aceptar");
        }
    }

    private void Recargar()
    {
        contacts = store.GetAll();
        Filtrar();
    }

    private void MostrarAcercaDe()
    {
        MessageBox.Query(
            "Acerca de",
            "Agenda TUI - Trabajo Práctico 3\nSQLite + Terminal.Gui + JSON",
            "Aceptar"
        );
    }

    private void Salir()
    {
        Application.RequestStop();
    }
}

public sealed class AgendaTextField : TextField
{
    private readonly Func<Key, bool> manejarAtajo;

    public AgendaTextField(Func<Key, bool> manejarAtajo)
    {
        this.manejarAtajo = manejarAtajo;
    }

    protected override bool OnKeyDown(Key key)
    {
        if (manejarAtajo(key))
        {
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class AgendaListView : ListView
{
    private readonly Func<Key, bool> manejarAtajo;

    public AgendaListView(Func<Key, bool> manejarAtajo)
    {
        this.manejarAtajo = manejarAtajo;
    }

    protected override bool OnKeyDown(Key key)
    {
        if (manejarAtajo(key))
        {
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class DialogTextField : TextField
{
    private readonly Func<Key, bool> manejarAtajo;

    public DialogTextField(Func<Key, bool> manejarAtajo)
    {
        this.manejarAtajo = manejarAtajo;
    }

    protected override bool OnKeyDown(Key key)
    {
        if (manejarAtajo(key))
        {
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class ContactDialog : Window
{
    private readonly DialogTextField nombreField;
    private readonly DialogTextField telefono1Field;
    private readonly DialogTextField telefono2Field;
    private readonly DialogTextField telefono3Field;
    private readonly DialogTextField telefono4Field;
    private readonly DialogTextField telefono5Field;
    private readonly DialogTextField emailField;
    private readonly DialogTextField notasField;
    private readonly DialogTextField favoritoField;

    private Contacto? resultado;
    private readonly Contacto contactoBase;

    private ContactDialog(string titulo, Contacto contacto)
    {
        contactoBase = contacto.Clone();

        Title = titulo;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var menu = new MenuBar
        {
            Menus = new MenuBarItem[]
            {
                new MenuBarItem("_Opciones", new MenuItem[]
                {
                    new MenuItem("_Guardar", "Ctrl+S", Guardar),
                    new MenuItem("_Insertar arroba", "@", InsertarArroba),
                    new MenuItem("_Cancelar", "Esc", Cancelar)
                })
            }
        };

        Add(menu);

        Add(new Label { Text = "Nombre:", X = 2, Y = 3 });
        nombreField = new DialogTextField(ManejarAtajoDialogo) { Text = contacto.Nombre, X = 15, Y = 3, Width = 50 };
        Add(nombreField);

        var telefonos = contacto.Telefonos
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(5)
            .ToList();

        while (telefonos.Count < 5)
        {
            telefonos.Add("");
        }

        Add(new Label { Text = "Telefono 1:", X = 2, Y = 5 });
        telefono1Field = new DialogTextField(ManejarAtajoDialogo) { Text = telefonos[0], X = 15, Y = 5, Width = 30 };
        Add(telefono1Field);

        Add(new Label { Text = "Telefono 2:", X = 2, Y = 6 });
        telefono2Field = new DialogTextField(ManejarAtajoDialogo) { Text = telefonos[1], X = 15, Y = 6, Width = 30 };
        Add(telefono2Field);

        Add(new Label { Text = "Telefono 3:", X = 2, Y = 7 });
        telefono3Field = new DialogTextField(ManejarAtajoDialogo) { Text = telefonos[2], X = 15, Y = 7, Width = 30 };
        Add(telefono3Field);

        Add(new Label { Text = "Telefono 4:", X = 2, Y = 8 });
        telefono4Field = new DialogTextField(ManejarAtajoDialogo) { Text = telefonos[3], X = 15, Y = 8, Width = 30 };
        Add(telefono4Field);

        Add(new Label { Text = "Telefono 5:", X = 2, Y = 9 });
        telefono5Field = new DialogTextField(ManejarAtajoDialogo) { Text = telefonos[4], X = 15, Y = 9, Width = 30 };
        Add(telefono5Field);

        Add(new Label { Text = "Email:", X = 2, Y = 11 });
        emailField = new DialogTextField(ManejarAtajoDialogo) { Text = contacto.Email, X = 15, Y = 11, Width = 50 };
        Add(emailField);

        Add(new Label { Text = "Notas:", X = 2, Y = 13 });
        notasField = new DialogTextField(ManejarAtajoDialogo) { Text = contacto.Notas, X = 15, Y = 13, Width = 60 };
        Add(notasField);

        Add(new Label { Text = "Favorito:", X = 2, Y = 15 });
        favoritoField = new DialogTextField(ManejarAtajoDialogo)
        {
            Text = contacto.Favorito ? "s" : "n",
            X = 15,
            Y = 15,
            Width = 5
        };
        Add(favoritoField);

        Add(new Label
        {
            Text = "Favorito: escriba s para si o n para no.",
            X = 2,
            Y = 18,
            Width = Dim.Fill()
        });

        Add(new Label
        {
            Text = "Si el teclado no escribe @, use Opciones > Insertar arroba.",
            X = 2,
            Y = 19,
            Width = Dim.Fill()
        });
    }

    protected override bool OnKeyDown(Key key)
    {
        return ManejarAtajoDialogo(key) || base.OnKeyDown(key);
    }

    private bool ManejarAtajoDialogo(Key key)
    {
        if (key == Key.S.WithCtrl)
        {
            Guardar();
            return true;
        }

        if (key == Key.Esc)
        {
            Cancelar();
            return true;
        }

        return false;
    }

    public static Contacto? CrearNuevo()
    {
        var dialogo = new ContactDialog("Nuevo contacto", new Contacto());

        Application.Run(dialogo);

        return dialogo.resultado;
    }

    public static Contacto? Editar(Contacto original)
    {
        var dialogo = new ContactDialog("Editar contacto", original);

        Application.Run(dialogo);

        return dialogo.resultado;
    }

    private void InsertarArroba()
    {
        var actual = emailField.Text?.ToString() ?? "";

        emailField.Text = actual + "@";

        emailField.SetFocus();
    }

    private void Guardar()
    {
        var nombre = nombreField.Text?.ToString()?.Trim() ?? "";
        var email = emailField.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.ErrorQuery("Error", "El nombre no puede estar vacío.", "Aceptar");
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
        {
            MessageBox.ErrorQuery("Error", "El email debe contener @.", "Aceptar");
            return;
        }

        var telefonos = new List<string>
        {
            telefono1Field.Text?.ToString()?.Trim() ?? "",
            telefono2Field.Text?.ToString()?.Trim() ?? "",
            telefono3Field.Text?.ToString()?.Trim() ?? "",
            telefono4Field.Text?.ToString()?.Trim() ?? "",
            telefono5Field.Text?.ToString()?.Trim() ?? ""
        }
        .Where(t => !string.IsNullOrWhiteSpace(t))
        .Take(5)
        .ToList();

        var favoritoTexto = favoritoField.Text?.ToString()?.Trim().ToLower() ?? "n";

        resultado = new Contacto
        {
            Id = contactoBase.Id,
            Nombre = nombre,
            Telefonos = string.Join(", ", telefonos),
            Email = email,
            Notas = notasField.Text?.ToString() ?? "",
            Favorito = favoritoTexto == "s" || favoritoTexto == "si" || favoritoTexto == "sí"
        };

        Application.RequestStop();
    }

    private void Cancelar()
    {
        resultado = null;
        Application.RequestStop();
    }
}

public sealed class SqliteAgendaStore
{
    private readonly string connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        connectionString = $"Data Source={dbPath}";

        using var connection = new SqliteConnection(connectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Contactos
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT,
                Email TEXT,
                Notas TEXT,
                Favorito INTEGER
            )
        ";

        command.ExecuteNonQuery();
    }

    public List<Contacto> GetAll()
    {
        var contactos = new List<Contacto>();

        using var connection = new SqliteConnection(connectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT Id, Nombre, Telefonos, Email, Notas, Favorito
            FROM Contactos
            ORDER BY Nombre
        ";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            contactos.Add(new Contacto
            {
                Id = reader.GetInt32(0),
                Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Telefonos = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Notas = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Favorito = !reader.IsDBNull(5) && reader.GetInt32(5) == 1
            });
        }

        return contactos;
    }

    public void Insert(Contacto contacto)
    {
        using var connection = new SqliteConnection(connectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Contactos (Nombre, Telefonos, Email, Notas, Favorito)
            VALUES ($nombre, $telefonos, $email, $notas, $favorito)
        ";

        command.Parameters.AddWithValue("$nombre", contacto.Nombre);
        command.Parameters.AddWithValue("$telefonos", contacto.Telefonos);
        command.Parameters.AddWithValue("$email", contacto.Email);
        command.Parameters.AddWithValue("$notas", contacto.Notas);
        command.Parameters.AddWithValue("$favorito", contacto.Favorito ? 1 : 0);

        command.ExecuteNonQuery();
    }

    public void Update(Contacto contacto)
    {
        using var connection = new SqliteConnection(connectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE Contactos
            SET Nombre = $nombre,
                Telefonos = $telefonos,
                Email = $email,
                Notas = $notas,
                Favorito = $favorito
            WHERE Id = $id
        ";

        command.Parameters.AddWithValue("$id", contacto.Id);
        command.Parameters.AddWithValue("$nombre", contacto.Nombre);
        command.Parameters.AddWithValue("$telefonos", contacto.Telefonos);
        command.Parameters.AddWithValue("$email", contacto.Email);
        command.Parameters.AddWithValue("$notas", contacto.Notas);
        command.Parameters.AddWithValue("$favorito", contacto.Favorito ? 1 : 0);

        command.ExecuteNonQuery();
    }

    public void Delete(Contacto contacto)
    {
        using var connection = new SqliteConnection(connectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM Contactos WHERE Id = $id";

        command.Parameters.AddWithValue("$id", contacto.Id);

        command.ExecuteNonQuery();
    }
}

public sealed class JsonAgendaIO
{
    public static void Exportar(string ruta, List<Contacto> contactos)
    {
        var opciones = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(contactos, opciones);

        File.WriteAllText(ruta, json);
    }

    public static List<Contacto> Importar(string ruta)
    {
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException();
        }

        var json = File.ReadAllText(ruta);

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