using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Dapper;
using Dapper.Contrib.Extensions;

using Microsoft.Data.Sqlite;

using Terminal.Gui;

namespace AgendaTP;

class Program
{
    static void Main(string[] args)
    {
        string dbPath = args.Length > 0
            ? args[0]
            : "agenda.db";

        try
        {
            var store = new SqliteAgendaStore(dbPath);

            Application.Init();

            var top = Application.Top;

            var win = new AgendaWindow(store);

            top.Add(win);

            Application.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            Application.Shutdown();
        }
    }
}

public class AgendaWindow : Window
{
    private readonly SqliteAgendaStore store;

    private List<Contacto> contactos = new();
    private List<Contacto> contactosFiltrados = new();

    private readonly TextField txtBusqueda;
    private readonly ListView lstContactos;
    private readonly TextView txtDetalle;

    private bool soloFavoritos = false;

    public AgendaWindow(SqliteAgendaStore store)
        : base("AgendaT")
    {
        this.store = store;

        Width = Dim.Fill();
        Height = Dim.Fill();

        contactos = store.GetAll();

        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_Archivo", new MenuItem[]
            {
                new MenuItem("_Importar JSON", "", ImportarJson),
                new MenuItem("_Exportar JSON", "", ExportarJson),
                new MenuItem("_Salir", "", Salir)
            }),

            new MenuBarItem("_Contactos", new MenuItem[]
            {
                new MenuItem("_Nuevo", "", NuevoContacto),
                new MenuItem("_Editar", "", EditarContacto),
                new MenuItem("_Eliminar", "", EliminarContacto)
            }),

            new MenuBarItem("_Ver", new MenuItem[]
            {
                new MenuItem("_Solo favoritos", "", ToggleFavoritos)
            })
        });

        Add(menu);

        Add(new Label("Buscar:")
        {
            X = 1,
            Y = 1
        });

        txtBusqueda = new TextField("")
        {
            X = 10,
            Y = 1,
            Width = 40
        };

        txtBusqueda.TextChanged += (_) => AplicarFiltros();

        Add(txtBusqueda);

        lstContactos = new ListView()
        {
            X = 0,
            Y = 3,
            Width = 30,
            Height = Dim.Fill()
        };

        lstContactos.SelectedItemChanged += (_) =>
        {
            ActualizarDetalle();
        };

        lstContactos.OpenSelectedItem += (_) =>
        {
            EditarContacto();
        };

        Add(lstContactos);

        txtDetalle = new TextView()
        {
            X = Pos.Right(lstContactos) + 1,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true
        };

        Add(txtDetalle);

        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        string filtro =
            txtBusqueda.Text.ToString().ToLower();

        contactosFiltrados = contactos
            .Where(c =>
            {
                bool coincideBusqueda =
                    c.Nombre.ToLower().Contains(filtro)
                    || c.Telefonos.ToLower().Contains(filtro)
                    || c.Email.ToLower().Contains(filtro);

                bool coincideFavoritos =
                    !soloFavoritos || c.Favorito;

                return coincideBusqueda && coincideFavoritos;
            })
            .OrderBy(c => c.Nombre)
            .ToList();

        lstContactos.SetSource(
            contactosFiltrados
                .Select(FormatearContacto)
                .ToList()
        );

        ActualizarDetalle();
    }

    private string FormatearContacto(Contacto c)
    {
        return c.Favorito
            ? $"★ {c.Nombre}"
            : c.Nombre;
    }

    private void ActualizarDetalle()
    {
        if (contactosFiltrados.Count == 0)
        {
            txtDetalle.Text = "";
            return;
        }

        int index = lstContactos.SelectedItem;

        if (index < 0 || index >= contactosFiltrados.Count)
        {
            txtDetalle.Text = "";
            return;
        }

        var c = contactosFiltrados[index];

        txtDetalle.Text =
$@"Nombre:
{c.Nombre}

Telefonos:
{c.Telefonos}

Email:
{c.Email}

Favorito:
{(c.Favorito ? "Si" : "No")}

Notas:
{c.Notas}";
    }

    private Contacto ObtenerSeleccionado()
    {
        int index = lstContactos.SelectedItem;

        if (index < 0 || index >= contactosFiltrados.Count)
            return null;

        return contactosFiltrados[index];
    }

    private void NuevoContacto()
    {
        var dialog = new ContactDialog(new Contacto());

        Application.Run(dialog);

        if (dialog.Resultado == null)
            return;

        var nuevo = store.Insert(dialog.Resultado);

        contactos.Add(nuevo);

        AplicarFiltros();
    }

    private void EditarContacto()
    {
        var seleccionado = ObtenerSeleccionado();

        if (seleccionado == null)
            return;

        var dialog = new ContactDialog(seleccionado);

        Application.Run(dialog);

        if (dialog.Resultado == null)
            return;

        store.Update(dialog.Resultado);

        int index = contactos.FindIndex(c => c.Id == seleccionado.Id);

        if (index >= 0)
        {
            contactos[index] = dialog.Resultado;
        }

        AplicarFiltros();
    }

    private void EliminarContacto()
    {
        var seleccionado = ObtenerSeleccionado();

        if (seleccionado == null)
            return;

        int result = MessageBox.Query(
            "Confirmar",
            $"Eliminar a {seleccionado.Nombre}?",
            "Si",
            "No"
        );

        if (result != 0)
            return;

        store.Delete(seleccionado);

        contactos.RemoveAll(c => c.Id == seleccionado.Id);

        AplicarFiltros();
    }

    private void ToggleFavoritos()
    {
        soloFavoritos = !soloFavoritos;

        AplicarFiltros();
    }

    private void ImportarJson()
    {
        try
        {
            var dialog = new OpenDialog(
                "Importar JSON",
                "Seleccione un archivo"
            );

            Application.Run(dialog);

            if (dialog.Canceled)
                return;

            string path = dialog.FilePath.ToString();

            var importados = JsonAgendaIO.Import(path);

            int confirm = MessageBox.Query(
                "Importar",
                $"Se importaran {importados.Count} contactos",
                "Aceptar",
                "Cancelar"
            );

            if (confirm != 0)
                return;

            foreach (var c in importados)
            {
                c.Id = 0;

                var nuevo = store.Insert(c);

                contactos.Add(nuevo);
            }

            AplicarFiltros();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void ExportarJson()
    {
        try
        {
            var dialog = new SaveDialog(
                "Exportar JSON",
                "Guardar archivo"
            );

            dialog.FileName = "contactos.json";

            Application.Run(dialog);

            if (dialog.Canceled)
                return;

            string path = dialog.FilePath.ToString();

            JsonAgendaIO.Export(path, contactos);

            MessageBox.Query(
                "Exportacion",
                "Contactos exportados",
                "OK"
            );
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void Salir()
    {
        Application.RequestStop();
    }
}

public class ContactDialog : Dialog
{
    private readonly TextField txtNombre;
    private readonly TextField txtTelefonos;
    private readonly TextField txtEmail;
    private readonly TextView txtNotas;
    private readonly CheckBox chkFavorito;

    public Contacto Resultado { get; private set; }

    public ContactDialog(Contacto contacto)
        : base("Contacto", 60, 20)
    {
        var clone = contacto.Clone();

        Add(new Label("Nombre:")
        {
            X = 1,
            Y = 1
        });

        txtNombre = new TextField(clone.Nombre)
        {
            X = 15,
            Y = 1,
            Width = 30
        };

        Add(txtNombre);

        Add(new Label("Telefonos:")
        {
            X = 1,
            Y = 3
        });

        txtTelefonos = new TextField(clone.Telefonos)
        {
            X = 15,
            Y = 3,
            Width = 30
        };

        Add(txtTelefonos);

        Add(new Label("Email:")
        {
            X = 1,
            Y = 5
        });

        txtEmail = new TextField(clone.Email)
        {
            X = 15,
            Y = 5,
            Width = 30
        };

        Add(txtEmail);

        chkFavorito = new CheckBox("Favorito")
        {
            X = 15,
            Y = 7,
            Checked = clone.Favorito
        };

        Add(chkFavorito);

        Add(new Label("Notas:")
        {
            X = 1,
            Y = 9
        });

        txtNotas = new TextView()
        {
            X = 15,
            Y = 9,
            Width = 30,
            Height = 4,
            Text = clone.Notas
        };

        Add(txtNotas);

        var btnGuardar = new Button("Guardar")
        {
            X = 15,
            Y = 15,
            IsDefault = true
        };

        btnGuardar.Clicked += Guardar;

        Add(btnGuardar);

        var btnCancelar = new Button("Cancelar")
        {
            X = 30,
            Y = 15
        };

        btnCancelar.Clicked += () =>
        {
            Application.RequestStop();
        };

        Add(btnCancelar);
    }

    private void Guardar()
    {
        string nombre = txtNombre.Text.ToString();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.ErrorQuery(
                "Error",
                "El nombre es obligatorio",
                "OK"
            );

            return;
        }

        string email = txtEmail.Text.ToString();

        if (!string.IsNullOrWhiteSpace(email)
            && !email.Contains("@"))
        {
            MessageBox.ErrorQuery(
                "Error",
                "Email invalido",
                "OK"
            );

            return;
        }

        Resultado = new Contacto()
        {
            Nombre = nombre,
            Telefonos = txtTelefonos.Text.ToString(),
            Email = email,
            Notas = txtNotas.Text.ToString(),
            Favorito = chkFavorito.Checked
        };

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
                Favorito INTEGER NOT NULL DEFAULT 0
            )
        ");
    }

    public List<Contacto> GetAll()
    {
        using var connection = new SqliteConnection(connectionString);

        return connection.GetAll<Contacto>().ToList();
    }

    public Contacto Insert(Contacto contacto)
    {
        using var connection = new SqliteConnection(connectionString);

        long id = connection.Insert(contacto);

        contacto.Id = (int)id;

        return contacto;
    }

    public void Update(Contacto contacto)
    {
        using var connection = new SqliteConnection(connectionString);

        connection.Update(contacto);
    }

    public void Delete(Contacto contacto)
    {
        using var connection = new SqliteConnection(connectionString);

        connection.Delete(contacto);
    }
}

public static class JsonAgendaIO
{
    public static List<Contacto> Import(string path)
    {
        string json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<List<Contacto>>(json)
            ?? new List<Contacto>();
    }

    public static void Export(
        string path,
        List<Contacto> contactos
    )
    {
        var options = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

        string json =
            JsonSerializer.Serialize(contactos, options);

        File.WriteAllText(path, json);
    }
}

[Table("Contactos")]
public class Contacto
{
    [ExplicitKey]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Telefonos { get; set; } = "";

    public string Email { get; set; } = "";

    public string Notas { get; set; } = "";

    public bool Favorito { get; set; }

    public Contacto Clone()
    {
        return new Contacto()
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
