#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using System.Collections.ObjectModel;
string dbFile = args.Length > 0
    ? args[0]
    : "agenda.db";

try
{
    using SqliteAgendaStore store = new(dbFile);

    using IApplication app = Application.Create().Init();

    app.Run(new AgendaWindow(store));
}
catch (Exception ex)
{
    Console.WriteLine($"Error al iniciar: {ex.Message}");
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

    public Contacto Copiar()
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

public sealed class SqliteAgendaStore : IDisposable
{
    private readonly SqliteConnection connection;

    public string RutaBase { get; }

    public SqliteAgendaStore(string path)
    {
        RutaBase = path;

        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = path
        };

        connection = new SqliteConnection(builder.ConnectionString);

        connection.Open();

        CrearTabla();
    }

    private void CrearTabla()
    {
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Notas TEXT NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            );
        """);
    }

    public IEnumerable<Contacto> ObtenerTodos()
    {
        return connection.GetAll<Contacto>();
    }

    public int Agregar(Contacto contacto)
    {
        Validar(contacto);

        long id = connection.Insert(contacto);

        return (int)id;
    }

    public void Actualizar(Contacto contacto)
    {
        Validar(contacto);

        connection.Update(contacto);
    }

    public void Eliminar(Contacto contacto)
    {
        connection.Delete(contacto);
    }

    private static void Validar(Contacto contacto)
    {
        if (string.IsNullOrWhiteSpace(contacto.Nombre))
        {
            throw new InvalidOperationException(
                "El nombre es obligatorio.");
        }

        if (!string.IsNullOrWhiteSpace(contacto.Email)
            && !contacto.Email.Contains('@'))
        {
            throw new InvalidOperationException(
                "El email debe contener @.");
        }
    }

    public void Dispose()
    {
        connection.Dispose();
    }
}
public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore store;

    private readonly ListView lista;
    private readonly TextField filtro;

    private List<Contacto> contactos = [];

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;

        Title = "Agenda SQLite";

        X = 0;
        Y = 1;
        Width = Dim.Fill();
        Height = Dim.Fill();

        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Exportar JSON", "", ExportarJson),
                    new MenuItem("_Importar JSON", "", ImportarJson),
                    new MenuItem("_Salir", "", Quit)
                ])
            ]
        };

       
        filtro = new TextField()
        {
             Text = "",
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2
        };

        filtro.TextChanged += (s, e) => Refrescar();

        lista = new ListView()
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 3
        };

        Add(
            new Label()
            {
                Text = "buscar:",
                X = 1,
                Y = 0
            },
            filtro,
            lista
        );

        KeyDown += OnKeyDown;

        Refrescar();
    }

    private void Refrescar()
    {
        string texto = filtro.Text.ToString() ?? "";

        contactos = store
            .ObtenerTodos()
            .OrderBy(c => c.Nombre)
            .Where(c =>
                string.IsNullOrWhiteSpace(texto)
                || c.Nombre.Contains(
                    texto,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
            
          var items = new ObservableCollection<string>(
          contactos.Select(c => FormatoFila(c))
         );

          lista.SetSource(items);
          
        
    }

    private static string FormatoFila(Contacto c)
    {
        string fav = c.Favorito ? "★" : " ";

        return $"{fav} {c.Nombre}";
    }



    protected override bool OnKeyDown(Key key)
{
    // Evaluamos los atajos
    switch (key)
    {
        case Key.F2:
            Nuevo();
            return true; 

        case Key.F3:
            Editar();
            return true;

        case Key.F4:
            Eliminar();
            return true;

        case Key.F5:
            Favorito();
            return true;
    }

    return base.OnKeyDown(key);
}

private Contacto? Seleccionado()
{
    int index = lista.SelectedItemIndex;

    if (index < 0 || index >= contactos.Count)
    {
        return null;
    }

    return contactos[index];
}
    private void Nuevo()
    {
        Contacto? c = DialogoContacto(null);

        if (c is null)
            return;

        store.Agregar(c);

        Refrescar();
    }

    private void Editar()
    {
        Contacto? actual = Seleccionado();

        if (actual is null)
            return;

        Contacto? editado =
            DialogoContacto(actual.Copiar());

        if (editado is null)
            return;

        editado.Id = actual.Id;

        store.Actualizar(editado);

        Refrescar();
    }

    private void Eliminar()
    {
        Contacto? actual = Seleccionado();

        if (actual is null)
            return;

        store.Eliminar(actual);

        Refrescar();
    }

    private void Favorito()
    {
        Contacto? actual = Seleccionado();

        if (actual is null)
            return;

        actual.Favorito = !actual.Favorito;

        store.Actualizar(actual);

        Refrescar();
    }

    private void ExportarJson()
    {
        JsonAgendaIO.Exportar(
            store.ObtenerTodos(),
            "agenda.json");
    }

    private void ImportarJson()
    {
        IEnumerable<Contacto> datos =
            JsonAgendaIO.Importar("agenda.json");

        foreach (Contacto c in datos)
        {
            c.Id = 0;
            store.Agregar(c);
        }

        Refrescar();
    }

    private void Quit()
    {
        Application.RequestStop();
    }
private Contacto? DialogoContacto(Contacto? contacto)
{
    contacto ??= new Contacto();

    Dialog dialog = new()
    {
        Title = contacto.Id == 0
            ? "Nuevo contacto"
            : "Editar contacto",
        Width = 60,
        Height = 18
    };

    TextField txtNombre = new()
    {
        Text = contacto.Nombre,
        X = 15,
        Y = 1,
        Width = 35
    };

    TextField txtTelefonos = new()
    {
        Text = contacto.Telefonos,
        X = 15,
        Y = 3,
        Width = 35
    };

    TextField txtEmail = new()
    {
        Text = contacto.Email,
        X = 15,
        Y = 5,
        Width = 35
    };

    TextView txtNotas = new()
    {
        X = 15,
        Y = 7,
        Width = 35,
        Height = 4,
        Text = contacto.Notas
    };

    CheckBox chkFavorito = new()
    {
        X = 15,
        Y = 12,
        Checked = contacto.Favorito
    };

    Button btnAceptar = new()
    {
        Text = "Aceptar",
        X = 12,
        Y = 14
    };

    Button btnCancelar = new()
    {
        Text = "Cancelar",
        X = 30,
        Y = 14
    };

    Contacto? resultado = null;

    btnAceptar.Accepting += (_, e) =>
    {
        try
        {
            resultado = new Contacto
            {
                Id = contacto.Id,
                Nombre = txtNombre.Text.ToString() ?? "",
                Telefonos = txtTelefonos.Text.ToString() ?? "",
                Email = txtEmail.Text.ToString() ?? "",
                Notas = txtNotas.Text.ToString() ?? "",
                Favorito = chkFavorito.CheckedState == Terminal.Gui.CheckState.Checked
            };

            dialog.RequestStop();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(
                "Error",
                ex.Message ?? "Ocurrió un error inesperado",
                 
                  "OK",
                  ex.Message ?? "Mensaje correcto",
                  
                );
        }


    };

    btnCancelar.Accepting += (_, e) =>
    {
        dialog.RequestStop();

    };

    dialog.Add(
        new Label( )
        {
            Text = "Nombre:",
            X = 2,
            Y = 1
        },
        txtNombre,

        new Label( )
        {
            Text = "Teléfonos:",
            X = 2,
            Y = 3
        },
        txtTelefonos,

        new Label( )
        {
            Text = "Email:",
            X = 2,
            Y = 5
        },
        txtEmail,

        new Label()
        {
            Text = "Notas:",
            X = 2,
            Y = 7
        },
        txtNotas,

        new Label( )
        {
            Text = "Favoritos:",
            X = 2,
            Y = 12
        },
        chkFavorito,

        btnAceptar,
        btnCancelar
    );

    Application.Run(dialog);

    return resultado;
}
    
}
public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions options =
        new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull
        };

    public static void Exportar(
        IEnumerable<Contacto> contactos,
        string archivo)
    {
        string json =
            JsonSerializer.Serialize(
                contactos,
                options);

        File.WriteAllText(
            archivo,
            json,
            Encoding.UTF8);
    }

    public static IEnumerable<Contacto> Importar(
        string archivo)
    {
        if (!File.Exists(archivo))
            return [];

        string json =
            File.ReadAllText(
                archivo,
                Encoding.UTF8);

        return JsonSerializer.Deserialize<
            List<Contacto>>(json, options)
            ?? [];
    }
}