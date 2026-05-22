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
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }
}

public sealed class SqliteAgendaStore {
    private readonly string _connectionString;

public SqliteAgendaStore(String dbPath) {
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
    private SqliteConnection Conectar() {
    SqliteConnection connection = new(_connectionString);
    connection.Open();
    return connection;
 }
    public IEnumerable<Contacto> ObtenerTodos() {
    using SqliteConnection connection = Conectar();
    return connection.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
}
public void Insertar(Contacto contacto) {
    using SqliteConnection connection = Conectar();
    long id = connection.Insert(contacto);
    contacto.Id = (int)id;
    }
public void Actualizar(Contacto contacto) {
    using SqliteConnection connection = Conectar();
        connection.Update(contacto);
}
public void Eliminar(int id) {
    using SqliteConnection connection = Conectar();
    connection.Delete(new Contacto { Id = id });}
 
}
public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    public static List<Contacto> Leer(String ruta)
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
public sealed class AgendaWindow : Runnable {
    private readonly SqliteAgendaStore _store;
    private readonly List<Contacto> _contacts = new();
    private readonly List<Contacto> _filteredContacts = new();
    private bool _soloFavoritos;
    private ListView _listView = null!;
    private TextField _searchBox = null!;
    private Label _detailView = null!;
     private Label _statusBar = null!;
    public AgendaWindow(SqliteAgendaStore store) {
       _store = store;
        Title  = "Agenda";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }

    private void BuildLayout() {
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
        X    = 0,
        Y    = 1
    };
    _searchBox = new TextField {
        X    = Pos.Right(searchLabel) + 1,
        Y    = 1,
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
    

    // aqui implemento los stubs q se usaran despues
private void LoadContacts()
{
    _contacts.Clear();
    _contacts.AddRange(_store.ObtenerTodos());
    AplicarFiltro();
}
private void EliminarContacto() => throw new NotImplementedException();
private void ImportarJson() => throw new NotImplementedException();
private void ExportarJson() => throw new NotImplementedException();
private string PedirRuta(string titulo, string etiqueta) => throw new NotImplementedException();
private void ToggleFavoritos() => throw new NotImplementedException();
private void AcercaDe() => throw new NotImplementedException();
private void SolicitarSalir() => throw new NotImplementedException();
protected override bool OnKeyDown(Key key) => base.OnKeyDown(key);

  
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
 private void EditarContacto() {
        Contacto? original = ContactoSeleccionado();
        if (original is null)        {
            SetStatus("No hay contacto seleccionado para editar.");
            return;
        }
        Contactdialog dialog = new("Editar contacto", original.Clone());
    }
}
