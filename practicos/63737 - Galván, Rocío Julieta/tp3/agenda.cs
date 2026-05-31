#!/usr/bin/env dotnet
#:property LangVersion=preview
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*


using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using SQLitePCL;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;


string databasePath = args.Length > 0 ? args[0] : "agenda.db";

try {
    var store = new SqliteAgendaStore(databasePath);

    using var app = Application.Create();
    app.Init();

    using var window = new AgendaWindow(store);
    app.Run(window);
}
catch (Exception ex) {
    Console.Error.WriteLine($"Error: {ex.Message}");
}

public class AgendaWindow : Window {
    private readonly SqliteAgendaStore _store;
    private readonly List<Contacto> _contacts;
    private List<Contacto> _filteredContacts;
    private bool _soloFavoritos;

    private MenuBar _menuBar = null!;
    private TextField _searchField = null!;
    private ListView _listView = null!;
    private TextView _detailView = null!;
    private StatusBar _statusBar = null!;
    private MenuItem _toggleFavoritosItem = null!;

public AgendaWindow(SqliteAgendaStore store) {
    _store = store;
    _contacts = store.GetAll();
    _filteredContacts = new List<Contacto>(_contacts);

    Title = "AGENDA DE CONTACTO";
    X = 0;
    Y = 0;
    Width = Dim.Fill();
    Height = Dim.Fill();

    BuildMenu();
    BuildLayout();
    BuildStatusBar();
    RefreshList();

   
}

    private void BuildMenu() {
        _toggleFavoritosItem = new MenuItem("_Solo favoritos", null!, () => ToggleFavoritos());

        _menuBar = new MenuBar {
            Menus =
            [
                new MenuBarItem("_Archivo",
            [
                new MenuItem("_Importar JSON", "Ctrl+I",  () => ImportJson()),
                new MenuItem("_Exportar JSON", "Ctrl+E",  () => ExportJson()),
                null!,
                new MenuItem("_Salir",   "Ctrl+Q",  () => App!.RequestStop()),
            ]),

            new MenuBarItem("_Contactos",
            [
                new MenuItem("_Nuevo",    "F2 / Ctrl+N",  NuevoContacto),
                new MenuItem("_Editar",   "F3 / Enter", EditarContacto),
                new MenuItem("_Eliminar", "Del / Ctrl+D",  EliminarContacto)
            ]),

             new MenuBarItem("_Ver",
           [
             _toggleFavoritosItem,
            new MenuItem("_Todos los contactos", null!, () => MostrarTodos())
           ]),

            new MenuBarItem("_Ayuda",
            [
                new MenuItem("_Acerca de", null!, () => MostrarAcercaDe())
            ])
            ]
        };
        Add(_menuBar);
    }

    private void BuildLayout() {
        _searchField = new TextField {
            X = 10,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 1
        };
        var labelSearch = new Label {
            Text = "Buscar:",
            X = 1,
            Y = 1
        };
        _listView = new ListView {
            X = 1,
            Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(2)
        };
        _detailView = new TextView {
            X = Pos.Right(_listView) + 1,
            Y = 3,
            Width = Dim.Fill(1),
            Height = Dim.Fill(2),
            ReadOnly = true
        };

        _searchField.TextChanged += (_, _) => ApplyFilter();
        _listView.ValueChanged += (_, _) => ShowDetail();

        Add(labelSearch, _searchField, _listView, _detailView);
    }


 private void BuildStatusBar() {
     _statusBar = new StatusBar([
    new Shortcut(Key.F2, "Nuevo", NuevoContacto),
    new Shortcut(Key.F3, "Editar", EditarContacto),
    new Shortcut(Key.Delete, "Eliminar", EliminarContacto),
    new Shortcut(Key.F4, "Buscar", () => _searchField.SetFocus()),
    new Shortcut(Key.I.WithCtrl, "Importar", ImportJson),  
    new Shortcut(Key.E.WithCtrl, "Exportar", ExportJson),  
    new Shortcut(Key.Q.WithCtrl, "Salir", () => App!.RequestStop())
     ]);
     Add(_statusBar); 
}



    private void RefreshList() {
        _listView.SetSource(new ObservableCollection<string>(
            _filteredContacts.Select(c => (c.Favorito ? "♥ " : "  ") + c.Nombre)));
    }

    private void ApplyFilter() {
        string busqueda = _searchField.Text.ToLower();
        _filteredContacts = _contacts.Where(c =>
            (!_soloFavoritos || c.Favorito) &&
            (c.Nombre.ToLower().Contains(busqueda) ||
             c.Telefonos.ToLower().Contains(busqueda) ||
             c.Email.ToLower().Contains(busqueda))
        ).ToList();

        RefreshList();
    }

    private void ShowDetail() {
        int selectedIndex = SelectedIndex();
        if (selectedIndex < 0 || selectedIndex >= _filteredContacts.Count) {
            _detailView.Text = "";
            return;
        }

        var c = _filteredContacts[selectedIndex];
        _detailView.Text =
            $"Nombre:    {c.Nombre}\n" +
            $"Telefonos: {c.Telefonos}\n" +
            $"Email:     {c.Email}\n" +
            $"Notas:\n{c.Notas}\n\n" +
            $"Favorito:  {(c.Favorito ? "Si" : "No")}";
    }

    private void NuevoContacto() {
        var dialog = new ContactDialog(new Contacto());
        App!.Run(dialog);
        if (dialog.Result == null) return;
        _store.Insert(dialog.Result);
        _contacts.Add(dialog.Result);
        ApplyFilter();
        SetStatus("Se agregó el contacto correctamente");

    }

private void EditarContacto() {
    int selectedIndex = SelectedIndex();
    if (selectedIndex < 0 || selectedIndex >= _filteredContacts.Count) {
        return;
    }

    var original = _filteredContacts[selectedIndex];

    var dialog = new ContactDialog(original.Clone());
    App!.Run(dialog);

    if (dialog.Result is null) {
        return;
    }

    dialog.Result.Id = original.Id;
    _store.Update(dialog.Result);

    int index = _contacts.FindIndex(c => c.Id == original.Id);
    if (index >= 0) {
        _contacts[index] = dialog.Result;
    }
    ApplyFilter();
    SetStatus("Se actualizó el contacto correctamente");
}

    private void EliminarContacto() {
    int selectedIndex = SelectedIndex();
    if (selectedIndex < 0 || selectedIndex >= _filteredContacts.Count) {
        return;
    }

    var contacto = _filteredContacts[selectedIndex];

    int? confirm = MessageBox.Query(
        App!,
        "Eliminar",
        $"Eliminar a {contacto.Nombre}?",
        "Si",
        "No");

    if (confirm != 0) {
        return;
    }

    _store.Delete(contacto);
    _contacts.RemoveAll(c => c.Id == contacto.Id);
    ApplyFilter();
    SetStatus("CONTACTO ELIMINADO");
}

private void ToggleFavoritos() {
    _soloFavoritos = !_soloFavoritos;
    _toggleFavoritosItem.Title = _soloFavoritos ? "✓ Solo favoritos" : "_Solo favoritos";
    _menuBar.SetNeedsDraw();
    ApplyFilter();
    SetStatus(_soloFavoritos ? "Mostrando solo favoritos" : "Mostrando todos");
}

private void MostrarTodos() {
    _soloFavoritos = false;
    _toggleFavoritosItem.Title = "_Solo favoritos";
    _menuBar.SetNeedsDraw();
    ApplyFilter();
    SetStatus("Mostrando todos los contactos");
}

    private void ImportJson() {
        var dialog = new OpenDialog {
            Title = "Importar JSON",
            Path = Directory.GetCurrentDirectory()
        };
        App!.Run(dialog);

        string? path = dialog.FilePaths.FirstOrDefault() ?? dialog.Path;
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            var io = new JsonAgendaIO();
            var nuevos = io.Import(path);
            int cantidad = nuevos.Count;
            int? confirm = MessageBox.Query(
                App!,
                "Importar",
                $"Se agregaran {cantidad} contactos. Continuar?",
                "Si",
                "No");

            if (confirm != 0) {
                return;
            }
            foreach (var contacto in nuevos) {
                contacto.Id = 0;
                _store.Insert(contacto);
                _contacts.Add(contacto);
            }

            ApplyFilter();

           MessageBox.Query(App!, "Importar", $"{cantidad} contacto(s) importado(s) correctamente", "Ok");
          SetStatus($"{cantidad} contacto(s) importado(s)");
        }
        catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error", ex.Message, "Ok");
        }
        
    }
    
private void ExportJson() {
    

var label = new Label {    
    Text = "Nombre del archivo:",
    X = 1,
    Y = 1
};
    var input = new TextField {
        X = 1,
        Y = 2,
        Width = Dim.Fill(1),
        Text = "contactos.json"
    };

    var dialog = new Dialog {
        Title = "Exportar JSON",
        Width = 50,
        Height = 10
    };

    var btnOk = new Button {
        Text = "Exportar",
        X = Pos.Center(),
        Y = 4,
        IsDefault = true
    };
    btnOk.Accepting += (_, e) => {
        dialog.RequestStop();
        e.Handled = true;
    };

    var btnCancelar = new Button {
        Text = "Cancelar",
        X = Pos.Right(btnOk) + 2,
        Y = 4
    };
    btnCancelar.Accepting += (_, e) => {
        input.Text = "";
        dialog.RequestStop();
        e.Handled = true;
    };

   dialog.Add(label, input, btnOk, btnCancelar); 
    App!.Run(dialog);

    if (string.IsNullOrWhiteSpace(input.Text)) return;

    try {
        var io = new JsonAgendaIO();
        io.Export(_contacts, input.Text);
        MessageBox.Query(App!, "Exportar", $"Contactos exportados correctamente a:\n{input.Text}", "Ok");
        SetStatus($"Exportados {_contacts.Count} contacto(s) a {input.Text}");
    }
    catch (Exception ex) {
        MessageBox.ErrorQuery(App!, "Error", ex.Message, "Ok");
    }
}


   private void MostrarAcercaDe() {
    MessageBox.Query(       
        App!,
        "Acerca de",
        "Agenda de Contactos\nTrabajo Practico 3\nTerminal.Gui + SQLite + JSON",
        "Ok");

}
    private void SetStatus(string mensaje) {
        _statusBar.Title = mensaje;
        _statusBar.SetNeedsDraw();
    }
    private int SelectedIndex() {
        return _listView.SelectedItem ?? -1;
    }


protected override bool OnKeyDown(Key key) {
    string teclaStr = key.ToString();

    if (teclaStr == "F2" || key.IsCtrl && teclaStr == "N") {
        NuevoContacto(); 
        return true;
    }

    if (teclaStr == "F3" || teclaStr == "Enter") {
        EditarContacto();
        return true;
    }

    if (teclaStr == "Delete" || key.IsCtrl && teclaStr == "D") {
        EliminarContacto(); 
        return true;
    }

    if (key.IsCtrl && teclaStr == "I") {
        ImportJson();
        return true;
    }

    if (key.IsCtrl && teclaStr == "E") {
        ExportJson();
        return true;
    }

    if (teclaStr == "F4") {
        _searchField.SetFocus(); 
        return true;
    }

    if (key.IsCtrl && teclaStr == "Q") {
        App!.RequestStop();
        return true;
    }

    bool handled = base.OnKeyDown(key);
    

    return handled;
}

}



public class ContactDialog : Dialog
{
    private readonly TextField _nameField;
    private readonly TextField[] _phoneFields = new TextField[5];
    private readonly TextField _emailField;
    private readonly TextView _notesField;
    private readonly CheckBox _favoriteField;

    public new Contacto? Result { get; private set; }

    public ContactDialog(Contacto contacto)
    {
        Title = contacto.Id == 0 ? "Nuevo contacto" : "Editar contacto";

        Width = 60;
        Height = 25;

        
        var nameLabel = new Label {
            Text = "Nombre:",
            X = 1,
            Y = 1
        };

        var phoneLabel = new Label {
        Text = "Teléfonos:",
        X = 1,
        Y = 3
    };

        string[] tels = contacto.Telefonos.Split(',');
      for (int i = 0; i < 5; i++)
    {
    _phoneFields[i] = new TextField {
        X = 12,
        Y = 3 + i,
        Width = 40,
        Text = i < tels.Length ? tels[i].Trim() : ""
    };
}

        var emailLabel = new Label {
            Text = "Email:",
            X = 1,
            Y = 9
        };

        var notesLabel = new Label {
            Text = "Notas:",
            X = 1,
            Y = 11
        };

        
        _nameField = new TextField {
            X = 12,
            Y = 1,
            Width = 40,
            Text = contacto.Nombre
        };


        _emailField = new TextField {
            X = 12,
            Y = 9,
            Width = 40,
            Text = contacto.Email
        };

        _notesField = new TextView {
            X = 12,
            Y = 11,
            Width = 40,
            Height = 5,
            Text = contacto.Notas
        };

        _favoriteField = new CheckBox {
            Text = "Favorito",
            X = 12,
            Y = 17,
            Value= contacto.Favorito
                ? CheckState.Checked
                : CheckState.UnChecked
        };

        
        var saveButton = new Button {
            Text = "_Guardar",
            X = Pos.Center() - 10,
            Y = Pos.AnchorEnd(2),
            IsDefault = true
        };

        saveButton.Accepting += (_, e) =>
{
    string nombre = _nameField.Text.ToString()?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(nombre))
    {
        MessageBox.ErrorQuery(
            App!,
            "Validación",
            "El nombre es obligatorio",
            "OK");
        _nameField.SetFocus();
        e.Handled = true;
        return;
    }

    string email = _emailField.Text.ToString()?.Trim() ?? "";
    if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
    {
        MessageBox.ErrorQuery(
            App!,
            "Validación",
            "El email debe contener @",
            "OK");
        _emailField.SetFocus();
        e.Handled = true;
        return;
    }

    Result = new Contacto {
        Id        = contacto.Id,
        Nombre    = nombre,
        Telefonos = string.Join(",", _phoneFields
        .Select(t => t.Text.ToString()?.Trim() ?? "")
        .Where(t => t != "")),
        Email     = email,
        Notas     = _notesField.Text.ToString() ?? "",
        Favorito  = _favoriteField.Value == CheckState.Checked
    };

    RequestStop();
    e.Handled = true;
};
    
        var cancelButton = new Button {
            Text = "_Cancelar",
            X = Pos.Center() + 2,
            Y = Pos.AnchorEnd(2)
        };

        cancelButton.Accepting += (_, e) =>
        {
            Result = null;
            RequestStop();
            e.Handled = true;
        };

        Add(
            nameLabel,
            phoneLabel,
            emailLabel,
            notesLabel,

            _nameField,
            _phoneFields[0],
            _phoneFields[1],
            _phoneFields[2],
            _phoneFields[3],
            _phoneFields[4],
            _emailField,
            _notesField,
            _favoriteField,

            saveButton,
            cancelButton
        );
    }
}

public class SqliteAgendaStore
{
    private readonly string _connectionString;

    public SqliteAgendaStore(string databasePath)  {
        _connectionString = $"Data Source={databasePath}";
        EnsureSchema();
    }

    private SqliteConnection Open() {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureSchema() {
        using var db = Open();

        db.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre      TEXT NOT NULL,
                Telefonos   TEXT NOT NULL DEFAULT '',
                Email       TEXT NOT NULL DEFAULT '',
                Notas       TEXT NOT NULL DEFAULT '',
                Favorito    INTEGER NOT NULL DEFAULT 0
            );
        """);
    }

    public List<Contacto> GetAll(){
        using var db = Open();

        return db.GetAll<Contacto>().ToList();
    }

    public void Insert(Contacto contacto) {
        using var db = Open();

        int newId = (int)db.Insert(contacto);
        contacto.Id = newId;
    }

    public void Update(Contacto contacto) {
        using var db = Open();

        db.Update(contacto);
    }

    public void Delete(Contacto contacto) {
        using var db = Open();

        db.Delete(contacto);
    }
}

public class JsonAgendaIO {
    private readonly JsonSerializerOptions _options = new() {
        WriteIndented = true
    };

    public List<Contacto> Import(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"No existe el archivo '{path}'");
        }

        string json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<List<Contacto>>(json, _options)
               ?? new List<Contacto>();
    }

    public void Export(List<Contacto> contactos, string path) {
        string json = JsonSerializer.Serialize(contactos, _options);

        File.WriteAllText(path, json);
    }
}

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => (Contacto)MemberwiseClone();

}