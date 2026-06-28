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
using System.Data;
using System.Text.Json;
using IApplication app = Application.Create().Init();
// top-level code
var store = new SqliteAgendaStore("agenda.db");
store.Initialize();
app.Run(new AgendaWindow(store));
// clase AgendaWindow
public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore store;

    private List<Contacto> contacts = [];
    private List<Contacto> filteredContacts = [];

    private readonly ListView listView;
    private readonly TextField txtBuscar;

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;

        Title = "Agenda";
        Width = Dim.Fill();
        Height = Dim.Fill();

        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("Archivo",
                [
                    new MenuItem("Nuevo", "", NuevoContacto),
                    new MenuItem("Editar", "", EditarContacto),
                    new MenuItem("Eliminar", "", EliminarContacto),
                    null!,
                    new MenuItem("Importar JSON", "", ImportarJson),
                    new MenuItem("Exportar JSON", "", ExportarJson),
                    null!,
                    new MenuItem("Salir", "", Salir)
                ])
            ]
        };

        Add(menu);

        Label lblBuscar = new()
        {
            Text = "Buscar:",
            X = 1,
            Y = 2
        };

        txtBuscar = new()
        {
            X = 10,
            Y = 2,
            Width = Dim.Fill() - 2
        };

        txtBuscar.TextChanged += (_, _) => AplicarFiltro();

        listView = new()
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 5
        };

        Add(lblBuscar, txtBuscar, listView);

        CargarContactos();
    }

    private void CargarContactos()
    {
        contacts = store.GetAll();
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string filtro = txtBuscar.Text?.ToString() ?? "";

        filteredContacts =
            contacts
            .Where(c =>
                c.Nombre.Contains(
                    filtro,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        listView.SetSource(
            new System.Collections.ObjectModel.ObservableCollection<string>(
                filteredContacts
                    .Select(c => $"{c.Nombre} - {c.Telefonos}")
                    .ToList()));
    }

    private Contacto? Seleccionado()
    {
        int? selectedIndex = listView.SelectedItem;

        if (!selectedIndex.HasValue ||
            selectedIndex.Value < 0 ||
            selectedIndex.Value >= filteredContacts.Count)
            return null;

        return filteredContacts[selectedIndex.Value];
    }

    private void NuevoContacto()
    {
        ContactDialog dlg = new();

        App!.Run(dlg);

        if (dlg.Resultado is null)
            return;

        Contacto resultado = dlg.Resultado;
        long id = store.Insert(resultado);
        resultado.Id = (int)id;

        contacts.Add(resultado);

        AplicarFiltro();
    }

    private void EditarContacto()
    {
        Contacto? seleccionado = Seleccionado();

        if (seleccionado is null)
            return;

        ContactDialog dlg =
            new(seleccionado.Clone());

        App!.Run(dlg);

        if (dlg.Resultado is null)
            return;

        Contacto resultado = dlg.Resultado;
        store.Update(resultado);

        int index =
            contacts.FindIndex(c => c.Id == resultado.Id);

        if (index >= 0)
            contacts[index] = resultado;

        AplicarFiltro();
    }

    private void EliminarContacto()
    {
        Contacto? seleccionado = Seleccionado();

        if (seleccionado is null)
            return;

        store.Delete(seleccionado);

        contacts.RemoveAll(c => c.Id == seleccionado.Id);

        AplicarFiltro();
    }

    private void ImportarJson()
    {
        var contactos =
            JsonAgendaIO.Import("agenda.json");

        foreach (var c in contactos)
        {
            c.Id = 0;
            store.Insert(c);
        }

        CargarContactos();
    }

    private void ExportarJson()
    {
        JsonAgendaIO.Export(
            "agenda.json",
            contacts);
    }

    private void Salir()
    {
        App!.RequestStop();
    }
}
// clase ContactDialog
public sealed class ContactDialog : Dialog
{
    private readonly TextField txtNombre;
    private readonly TextField txtTelefono;
    private readonly TextField txtEmail;
    private readonly TextField txtNotas;
    private readonly CheckBox chkFavorito;

    public Contacto? Resultado { get; private set; }

    public ContactDialog(Contacto? contacto = null)
    {
        contacto ??= new Contacto();

        Title = "Contacto";
        Width = 60;
        Height = 18;

        txtNombre = new()
        {
            X = 15,
            Y = 1,
            Width = 30,
            Text = contacto.Nombre
        };

        txtTelefono = new()
        {
            X = 15,
            Y = 3,
            Width = 30,
            Text = contacto.Telefonos
        };

        txtEmail = new()
        {
            X = 15,
            Y = 5,
            Width = 30,
            Text = contacto.Email
        };

        txtNotas = new()
        {
            X = 15,
            Y = 7,
            Width = 30,
            Text = contacto.Notas
        };

        chkFavorito = new()
        {
            X = 15,
            Y = 9,   
           Value = contacto.Favorito? CheckState.Checked: CheckState.UnChecked
        };

        Add(
            new Label() { Text = "Nombre", X = 1, Y = 1 },
            txtNombre,

            new Label() { Text = "Telefono", X = 1, Y = 3 },
            txtTelefono,

            new Label() { Text = "Email", X = 1, Y = 5 },
            txtEmail,

            new Label() { Text = "Notas", X = 1, Y = 7 },
            txtNotas,

            new Label() { Text = "Favorito", X = 1, Y = 9 },
            chkFavorito
        );

        Button guardar = new()
        {
            Text = "_Guardar"
        };

        guardar.Accepting += (_, e) =>
        {
            string nombre = txtNombre.Text?.ToString() ?? "";
            string email = txtEmail.Text?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
              MessageBox.ErrorQuery( App!,"Error","El nombre es obligatorio","OK");
               e.Handled = true;
               return;
            }

            if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            {
               MessageBox.ErrorQuery( App!,"Error","El email debe contener '@'","OK");
               e.Handled = true;
               return;
            }

            Resultado = new Contacto
            {
                Id = contacto.Id,
                Nombre = txtNombre.Text?.ToString() ?? "",
                Telefonos = txtTelefono.Text?.ToString() ?? "",
                Email = txtEmail.Text?.ToString() ?? "",
                Notas = txtNotas.Text?.ToString() ?? "",
                Favorito = chkFavorito.Value == CheckState.Checked
            };

            App!.RequestStop();
            e.Handled = true;
        };

        Button cancelar = new()
        {
            Text = "_Cancelar"
        };

        cancelar.Accepting += (_, e) =>
        {
            Resultado = null;
            App!.RequestStop();
            e.Handled = true;
        };

        AddButton(guardar);
        AddButton(cancelar);
    }
}
// clase SqliteAgendaStore
public sealed class SqliteAgendaStore
{
    private readonly string connectionString;

    public SqliteAgendaStore(string dbFile)
    {
        connectionString =
            $"Data Source={dbFile}";
    }

    private IDbConnection Open()
        => new SqliteConnection(connectionString);

    public void Initialize()
    {
        using var cn = Open();

        cn.Execute(
        """
        CREATE TABLE IF NOT EXISTS Contactos
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT,
            Telefonos TEXT,
            Email TEXT,
            Notas TEXT,
            Favorito INTEGER
        );
        """);
    }

    public List<Contacto> GetAll()
    {
        using var cn = Open();
        return cn.GetAll<Contacto>().ToList();
    }

    public long Insert(Contacto contacto)
    {
        using var cn = Open();
        return cn.Insert(contacto);
    }

    public bool Update(Contacto contacto)
    {
        using var cn = Open();
        return cn.Update(contacto);
    }

    public bool Delete(Contacto contacto)
    {
        using var cn = Open();
        return cn.Delete(contacto);
    }
}
// clase JsonAgendaIO
public static class JsonAgendaIO
{
    public static void Export(
        string path,
        List<Contacto> contactos)
    {
        string json =
            JsonSerializer.Serialize(
                contactos,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(path, json);
    }

    public static List<Contacto> Import(
        string path)
    {
        if (!File.Exists(path))
            return [];

        string json =
            File.ReadAllText(path);

        return JsonSerializer
            .Deserialize<List<Contacto>>(json)
            ?? [];
    }
}
// clase Contacto
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