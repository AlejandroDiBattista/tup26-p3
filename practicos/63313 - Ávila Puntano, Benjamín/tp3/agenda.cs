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
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));

[Table("Contactos")]
public class Contacto
{
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => new()
    {
        Id = Id,
        Nombre = Nombre,
        Telefonos = Telefonos,
        Email = Email,
        Notas = Notas,
        Favorito = Favorito
    };
}

public sealed class SqliteAgendaStore
{
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InicializarBaseDeDatos();
    }

    private void InicializarBaseDeDatos()
    {
        using SqliteConnection connection = Conectar();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT NOT NULL DEFAULT '',
                Telefonos TEXT NOT NULL DEFAULT '',
                Email     TEXT NOT NULL DEFAULT '',
                Notas     TEXT NOT NULL DEFAULT '',
                Favorito  INTEGER NOT NULL DEFAULT 0
            )
            """);
    }

    private SqliteConnection Conectar()
    {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        return connection;
    }

    public IEnumerable<Contacto> ObtenerTodos()
    {
        using SqliteConnection connection = Conectar();
        return connection.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
    }

    public void Insertar(Contacto contacto)
    {
        using SqliteConnection connection = Conectar();
        long id = connection.Insert(contacto);
        contacto.Id = (int)id;
    }

    public void Actualizar(Contacto contacto)
    {
        using SqliteConnection connection = Conectar();
        connection.Update(contacto);
    }

    public void Eliminar(int id)
    {
        using SqliteConnection connection = Conectar();
        connection.Delete(new Contacto { Id = id });
    }
}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<Contacto> Leer(string ruta)
    {
        string json = File.ReadAllText(ruta, Encoding.UTF8);
        List<Contacto>? resultado = JsonSerializer.Deserialize<List<Contacto>>(json, Options);
        if (resultado is null)
            throw new InvalidDataException("El archivo JSON no contiene una lista valida.");
        return resultado;
    }

    public static void Escribir(string ruta, IEnumerable<Contacto> contactos)
    {
        string json = JsonSerializer.Serialize(contactos.ToList(), Options);
        File.WriteAllText(ruta, json, Encoding.UTF8);
    }
}

public sealed class AgendaWindow : Runnable
{
    private readonly SqliteAgendaStore _store;
    private readonly List<Contacto> _contacts = new();
    private readonly List<Contacto> _filteredContacts = new();
    private bool _soloFavoritos;
    private ListView _listView = null!;
    private TextField _searchBox = null!;
    private Label _detailView = null!;
    private Label _statusBar = null!;

    public AgendaWindow(SqliteAgendaStore store)
    {
        _store = store;
        Title = "Agenda";
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }

    private void BuildLayout()
    {
        MenuBar menu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo", "F2 / Ctrl+N", NuevoContacto),
                    new MenuItem("_Editar", "F3 / Enter", EditarContacto),
                    new MenuItem("E_liminar", "Del / Ctrl+D", EliminarContacto)
                ]),
                new MenuBarItem("_Ver",
                [
                    new MenuItem("_Solo favoritos", null!, ToggleFavoritos)
                ]),
                new MenuBarItem("A_yuda",
                [
                    new MenuItem("_Acerca de", null!, AcercaDe)
                ])
            ]
        };

        Label searchLabel = new()
        {
            Text = "Buscar:",
            X = 0,
            Y = 1
        };

        _searchBox = new TextField
        {
            X = Pos.Right(searchLabel) + 1,
            Y = 1,
            Width = Dim.Fill()
        };
        _searchBox.TextChanged += (_, _) => AplicarFiltro();

        _listView = new ListView
        {
            X = 0,
            Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(2)
        };
        _listView.ValueChanged += (_, _) => MostrarDetalle();

        _detailView = new Label
        {
            X = Pos.Right(_listView) + 1,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Text = ""
        };

        _statusBar = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "Listo. F2=Nuevo  F3=Editar  Del=Eliminar  F4=Buscar  Ctrl+Q=Salir"
        };

        Add(menu, searchLabel, _searchBox, _listView, _detailView, _statusBar);
    }

    private void LoadContacts()
    {
        _contacts.Clear();
        _contacts.AddRange(_store.ObtenerTodos());
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string texto = (_searchBox.Text?.ToString() ?? "").Trim();

        _filteredContacts.Clear();
        foreach (Contacto contacto in _contacts)
        {
            if (_soloFavoritos && !contacto.Favorito)
                continue;

            if (texto.Length > 0 && !CoincideBusqueda(contacto, texto))
                continue;

            _filteredContacts.Add(contacto);
        }

        int selectedBefore = _listView.SelectedItem ?? 0;
        _listView.SetSource(new ObservableCollection<string>(_filteredContacts.Select(FormatearFila)));
        if (_filteredContacts.Count > 0)
            _listView.SelectedItem = Math.Clamp(selectedBefore, 0, _filteredContacts.Count - 1);

        MostrarDetalle();
    }

    private static bool CoincideBusqueda(Contacto contacto, string texto)
    {
        return contacto.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || contacto.Telefonos.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || contacto.Email.Contains(texto, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatearFila(Contacto contacto)
        => (contacto.Favorito ? "* " : "  ") + contacto.Nombre;

    private void MostrarDetalle()
    {
        Contacto? contacto = ContactoSeleccionado();
        if (contacto is null)
        {
            _detailView.Text = "Sin contacto seleccionado.";
            return;
        }

        _detailView.Text =
            $"Nombre:    {contacto.Nombre}\n" +
            $"Telefonos: {contacto.Telefonos}\n" +
            $"Email:     {contacto.Email}\n" +
            $"Favorito:  {(contacto.Favorito ? "Si" : "No")}\n\n" +
            $"Notas:\n{contacto.Notas}";
    }

    private Contacto? ContactoSeleccionado()
    {
        int index = _listView.SelectedItem ?? -1;
        if (index < 0 || index >= _filteredContacts.Count)
            return null;

        return _filteredContacts[index];
    }

    private void SetStatus(string message)
        => _statusBar.Text = message;

    private void NuevoContacto()
    {
        ContactDialog dialog = new("Nuevo contacto", new Contacto());
        App!.Run(dialog);
        if (!dialog.Aceptado)
            return;

        Contacto contacto = dialog.Resultado;
        _store.Insertar(contacto);
        _contacts.Add(contacto);
        AplicarFiltro();
        SetStatus($"Contacto '{contacto.Nombre}' creado.");
    }

    private void EditarContacto()
    {
        Contacto? original = ContactoSeleccionado();
        if (original is null)
        {
            SetStatus("No hay contacto seleccionado para editar.");
            return;
        }

        ContactDialog dialog = new("Editar contacto", original.Clone());
        App!.Run(dialog);
        if (!dialog.Aceptado)
            return;

        Contacto actualizado = dialog.Resultado;
        actualizado.Id = original.Id;
        _store.Actualizar(actualizado);

        int index = _contacts.IndexOf(original);
        if (index >= 0)
            _contacts[index] = actualizado;

        AplicarFiltro();
        SetStatus($"Contacto '{actualizado.Nombre}' actualizado.");
    }

    private void EliminarContacto()
    {
        Contacto? contacto = ContactoSeleccionado();
        if (contacto is null)
        {
            SetStatus("No hay contacto seleccionado para eliminar.");
            return;
        }

        int respuesta = MessageBox.Query(App!, "Confirmar", $"Eliminar a '{contacto.Nombre}'?", "Si", "No") ?? -1;
        if (respuesta != 0)
            return;

        _store.Eliminar(contacto.Id);
        _contacts.Remove(contacto);
        AplicarFiltro();
        SetStatus($"Contacto '{contacto.Nombre}' eliminado.");
    }

    private void ImportarJson()
    {
        string ruta = PedirRuta("Importar JSON", "Ruta del archivo JSON a importar:");
        if (ruta.Length == 0)
            return;

        List<Contacto> importados;
        try
        {
            importados = JsonAgendaIO.Leer(ruta);
        }
        catch (FileNotFoundException ex)
        {
            MessageBox.ErrorQuery(App!, "Error", $"Archivo no encontrado:\n{ex.Message}", "OK");
            return;
        }
        catch (JsonException ex)
        {
            MessageBox.ErrorQuery(App!, "Error", $"JSON con formato invalido:\n{ex.Message}", "OK");
            return;
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo importar:\n{ex.Message}", "OK");
            return;
        }

        int respuesta = MessageBox.Query(App!, "Confirmar importacion", $"Se agregaran {importados.Count} contacto(s). Continuar?", "Si", "No") ?? -1;
        if (respuesta != 0)
            return;

        foreach (Contacto contacto in importados)
        {
            contacto.Id = 0;
            _store.Insertar(contacto);
            _contacts.Add(contacto);
        }

        AplicarFiltro();
        SetStatus($"{importados.Count} contacto(s) importado(s).");
    }

    private void ExportarJson()
    {
        string ruta = PedirRuta("Exportar JSON", "Ruta de destino del archivo JSON:");
        if (ruta.Length == 0)
            return;

        try
        {
            JsonAgendaIO.Escribir(ruta, _contacts);
            SetStatus($"Exportado a '{ruta}'.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error", $"No se pudo exportar:\n{ex.Message}", "OK");
        }
    }

    private string PedirRuta(string titulo, string etiqueta)
    {
        string resultado = "";

        Dialog dialog = new()
        {
            Title = titulo,
            Width = 60,
            Height = 8
        };

        Label label = new() { Text = etiqueta, X = 1, Y = 1 };
        TextField pathField = new() { X = 1, Y = 2, Width = Dim.Fill(1), Text = "" };

        Button okButton = new() { Text = "_OK", IsDefault = true };
        okButton.Accepting += (_, e) =>
        {
            resultado = (pathField.Text?.ToString() ?? "").Trim();
            App!.RequestStop();
            e.Handled = true;
        };

        Button cancelButton = new() { Text = "_Cancelar" };
        cancelButton.Accepting += (_, e) =>
        {
            App!.RequestStop();
            e.Handled = true;
        };

        dialog.Add(label, pathField);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);
        App!.Run(dialog);

        return resultado;
    }

    private void ToggleFavoritos() => throw new NotImplementedException();
    private void AcercaDe() => throw new NotImplementedException();
    private void SolicitarSalir() => throw new NotImplementedException();
    protected override bool OnKeyDown(Key key) => base.OnKeyDown(key);
}

public sealed class ContactDialog : Dialog
{
    public bool Aceptado { get; private set; }
    public Contacto Resultado { get; private set; } = new();

    private readonly TextField _nombre = new();
    private readonly TextField[] _telefonos = new TextField[5];
    private readonly TextField _email = new();
    private readonly TextView _notas = new();
    private readonly CheckBox _favorito = new();

    public ContactDialog(string titulo, Contacto contacto)
    {
        Title = titulo;
        Width = 60;
        Height = 22;

        int y = 1;

        Add(new Label { Text = "Nombre (*):", X = 1, Y = y });
        _nombre.X = 20;
        _nombre.Y = y;
        _nombre.Width = Dim.Fill(1);
        _nombre.Text = contacto.Nombre;
        Add(_nombre);
        y++;

        string[] telefonos = contacto.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < _telefonos.Length; i++)
        {
            Add(new Label { Text = $"Telefono {i + 1}:", X = 1, Y = y });
            _telefonos[i] = new TextField
            {
                X = 20,
                Y = y,
                Width = Dim.Fill(1),
                Text = i < telefonos.Length ? telefonos[i].Trim() : ""
            };
            Add(_telefonos[i]);
            y++;
        }

        Add(new Label { Text = "Email:", X = 1, Y = y });
        _email.X = 20;
        _email.Y = y;
        _email.Width = Dim.Fill(1);
        _email.Text = contacto.Email;
        Add(_email);
        y++;

        _favorito.X = 1;
        _favorito.Y = y;
        _favorito.Text = "Favorito";
        _favorito.Value = contacto.Favorito ? CheckState.Checked : CheckState.UnChecked;
        Add(_favorito);
        y++;

        Add(new Label { Text = "Notas:", X = 1, Y = y });
        y++;
        _notas.X = 1;
        _notas.Y = y;
        _notas.Width = Dim.Fill(1);
        _notas.Height = 3;
        _notas.Text = contacto.Notas;
        Add(_notas);

        Button saveButton = new() { Text = "_Guardar", IsDefault = true };
        saveButton.Accepting += (_, e) =>
        {
            if (Validar())
                App!.RequestStop();
            e.Handled = true;
        };

        Button cancelButton = new() { Text = "_Cancelar" };
        cancelButton.Accepting += (_, e) =>
        {
            App!.RequestStop();
            e.Handled = true;
        };

        AddButton(saveButton);
        AddButton(cancelButton);
    }

    private bool Validar()
    {
        string nombre = (_nombre.Text?.ToString() ?? "").Trim();
        if (nombre.Length == 0)
        {
            MessageBox.ErrorQuery(App!, "Validacion", "El nombre no puede estar vacio.", "OK");
            return false;
        }

        string email = (_email.Text?.ToString() ?? "").Trim();
        if (email.Length > 0 && !email.Contains('@'))
        {
            MessageBox.ErrorQuery(App!, "Validacion", "El email debe contener '@'.", "OK");
            return false;
        }

        string telefonos = string.Join(
            ", ",
            _telefonos
                .Select(t => (t.Text?.ToString() ?? "").Trim())
                .Where(t => t.Length > 0));

        Aceptado = true;
        Resultado = new Contacto
        {
            Nombre = nombre,
            Telefonos = telefonos,
            Email = email,
            Notas = (_notas.Text?.ToString() ?? "").Trim(),
            Favorito = _favorito.Value == CheckState.Checked
        };

        return true;
    }
}