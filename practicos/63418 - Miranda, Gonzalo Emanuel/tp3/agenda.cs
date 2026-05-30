#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data.Common;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;



// Punto de entrada
string dbPath = args.Length > 0 ? args[0] : "agenda.db";
var store = new SqliteAgendaStore(dbPath);

// Inicializamos la app y se la pasamos a la ventana (necesario en v2)
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(app, store));


// Ventana principal
public sealed class AgendaWindow : Runnable 
{
    private readonly IApplication _app;
    private readonly SqliteAgendaStore _store;

    // Variables de estado
    private List<Contacto> _todosLosContactos = new();
    private List<Contacto> _contactosFiltrados = new();
    private bool _soloFavoritos = false;

    // Controles de UI
    private TextField _txtBuscar;
    private ListView _listaContactos;
    private TextView _txtDetalles;
    private Label _statusBar; // BYPASS: Usamos un Label simple para evitar el error de StatusItem

    public AgendaWindow(IApplication app, SqliteAgendaStore store) 
    {
        _app = app;
        _store = store;
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        CargarDatos();
    }

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("Importar _JSON", "Ctrl+I", ImportarJson),
                    new MenuItem("Exportar _JSON", "Ctrl+E", ExportarJson),
                    null!, 
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo", "F2", NuevoContacto),
                    new MenuItem("_Editar", "F3", EditarContacto),
                    new MenuItem("_Eliminar", "Del", EliminarContacto)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("Solo _favoritos", "", ToggleFavoritos)
                ]),
                new MenuBarItem("A_yuda", [
                    new MenuItem("_Acerca de", "", MostrarAcercaDe)
                ])
            ]
        };

        var lblBuscar = new Label { Text = "Buscar:", X = 0, Y = 1 };
        _txtBuscar = new TextField { X = 8, Y = 1, Width = Dim.Fill() };
        _txtBuscar.TextChanged += (_, _) => AplicarFiltros();

        var marcoIzquierdo = new FrameView { 
            Title = "Contactos", 
            X = 0, Y = 2, 
            Width = Dim.Percent(50), Height = Dim.Fill(1) 
        };
        _listaContactos = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        // BYPASS: Eliminamos SelectedItemChanged, actualizaremos el detalle en el evento OnKeyDown
        marcoIzquierdo.Add(_listaContactos);

        var marcoDerecho = new FrameView { 
            Title = "Detalle", 
            X = Pos.Percent(50), Y = 2, 
            Width = Dim.Fill(), Height = Dim.Fill(1) 
        };
        _txtDetalles = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        marcoDerecho.Add(_txtDetalles);

        _statusBar = new Label { Text = "Listo", X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill() };

        Add(menu, lblBuscar, _txtBuscar, marcoIzquierdo, marcoDerecho, _statusBar);
    }

    private void CargarDatos() 
    {
        _todosLosContactos = _store.GetAll().ToList();
        AplicarFiltros();
    }

    private void AplicarFiltros() 
    {
        var texto = _txtBuscar.Text?.ToString()?.ToLower() ?? "";
        
        _contactosFiltrados = _todosLosContactos.Where(c => 
            (!_soloFavoritos || c.Favorito) &&
            (string.IsNullOrEmpty(texto) || 
             (c.Nombre != null && c.Nombre.ToLower().Contains(texto)) ||
             (c.Email != null && c.Email.ToLower().Contains(texto)) ||
             (c.Telefonos != null && c.Telefonos.ToLower().Contains(texto)))
        ).ToList();

        var source = new ObservableCollection<string>(
        _contactosFiltrados.Select(c => $"{(c.Favorito ? "[*]" : "[ ]")} {c.Nombre}"));
        _listaContactos.SetSource(source);
        
        MostrarDetalle();
    }

    private void MostrarDetalle() 
    {
        if (_contactosFiltrados.Count == 0 || _listaContactos.SelectedItem == null || _listaContactos.SelectedItem < 0) 
        {
            _txtDetalles.Text = "No hay contactos para mostrar.";
            return;
        }

        var c = _contactosFiltrados[_listaContactos.SelectedItem.Value];
        _txtDetalles.Text = $"Nombre: {c.Nombre}\n" +
                            $"Email: {c.Email}\n" +
                            $"Teléfonos: {c.Telefonos}\n" +
                            $"Favorito: {(c.Favorito ? "Sí" : "No")}\n\n" +
                            $"Notas:\n{c.Notas}";
    }

    private void NuevoContacto() 
    {
        var dialog = new ContactDialog(_app);
        _app.Run(dialog);
        if (!dialog.Canceled) 
        {
            _store.Insert(dialog.ContactoResult);
            CargarDatos();
            MostrarMensaje("Contacto creado con éxito.");
        }
    }

    private void EditarContacto() 
    {
        if (_contactosFiltrados.Count == 0 || _listaContactos.SelectedItem == null || _listaContactos.SelectedItem < 0) return;
        
        var contactoSeleccionado = _contactosFiltrados[_listaContactos.SelectedItem.Value];
        var dialog = new ContactDialog(_app, contactoSeleccionado.Clone());
        
        _app.Run(dialog);
        if (!dialog.Canceled) 
        {
            _store.Update(dialog.ContactoResult);
            CargarDatos();
            MostrarMensaje("Contacto actualizado.");
        }
    }

   private void EliminarContacto() 
    {
        if (_contactosFiltrados.Count == 0 || _listaContactos.SelectedItem == null || _listaContactos.SelectedItem < 0) return;
        
        var c = _contactosFiltrados[_listaContactos.SelectedItem.Value];
        var r = MessageBox.Query(_app, "Eliminar", $"¿Seguro que querés eliminar a {c.Nombre}?", "Sí", "No");
        if (r == 0)
        {
            _store.Delete(c);
            CargarDatos();
            MostrarMensaje("Contacto eliminado.");
        }
    }

    private void ToggleFavoritos() 
    {
        _soloFavoritos = !_soloFavoritos;
        AplicarFiltros();
    }

    private string PedirRutaArchivo(string titulo, string valorPorDefecto) 
    {
        string rutaResult = null;
        var dialog = new Dialog { Title = titulo, Width = 50, Height = 6 };
        var txtRuta = new TextField { X = 1, Y = 1, Width = Dim.Fill(), Text = valorPorDefecto };
        var btnOk = new Button { Text = "Ok", IsDefault = true };
        var btnCancelar = new Button { Text = "Cancelar" };
        
        btnOk.Accepting += (_, e) => { rutaResult = txtRuta.Text?.ToString(); _app.RequestStop(); e.Handled = true; };
        btnCancelar.Accepting += (_, e) => { _app.RequestStop(); e.Handled = true; };
        
        dialog.Add(new Label { Text = "Ruta del archivo:", X = 1, Y = 0 });
        dialog.Add(txtRuta);
        dialog.AddButton(btnOk);
        dialog.AddButton(btnCancelar);
        
        _app.Run(dialog);
        return rutaResult;
    }

    private void ExportarJson() 
    {
        string ruta = PedirRutaArchivo("Exportar JSON", "contactos.json");
        if (!string.IsNullOrWhiteSpace(ruta)) 
        {
            JsonAgendaIO.Exportar(_todosLosContactos, ruta);
            MostrarMensaje($"Exportación exitosa en {ruta}.");
        }
    }

    private void ImportarJson() 
    {
        string ruta = PedirRutaArchivo("Importar JSON", "contactos.json");
        if (!string.IsNullOrWhiteSpace(ruta)) 
        {
            try 
            {
                var importados = JsonAgendaIO.Importar(ruta);
                // BYPASS: Le pasamos '_app' como primer parámetro
                var r = MessageBox.Query(_app, "Importar", $"Se van a importar {importados.Count} contactos.\n¿Continuar?", "Sí", "No");
                
                if (r == 0) 
                {
                    foreach (var c in importados) 
                    {
                        c.Id = 0; 
                        _store.Insert(c);
                    }
                    CargarDatos();
                    MostrarMensaje("Importación completada.");
                }
            }
            catch (Exception ex) 
            {
                // BYPASS: Le pasamos '_app' como primer parámetro
                MessageBox.ErrorQuery(_app, "Error", $"No se pudo importar:\n{ex.Message}", "OK");
            }
        }
    }

    private void MostrarAcercaDe() {
        MessageBox.Query(_app, "Acerca de", "AgendaT - Trabajo Práctico 3\nDesarrollado con Terminal.Gui", "OK");
    }

    public void MostrarMensaje(string mensaje) {
        _statusBar.Text = mensaje;
    }

    private void SolicitarSalir() {
        _app.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        bool handled = false;
        if (key == Key.Q.WithCtrl) { SolicitarSalir(); handled = true; }
        else if (key == Key.N.WithCtrl || key == Key.F2) { NuevoContacto(); handled = true; }
        else if (key == Key.F3 || key == Key.Enter) { EditarContacto(); handled = true; }
        else if (key == Key.DeleteChar || key == Key.D.WithCtrl) { EliminarContacto(); handled = true; }
        else if (key == Key.I.WithCtrl) { ImportarJson(); handled = true; }
        else if (key == Key.E.WithCtrl) { ExportarJson(); handled = true; }
        else if (key == Key.F4) { _txtBuscar.SetFocus(); handled = true; }
        else {
            handled = base.OnKeyDown(key);
        }
        
        // Al tocar cualquier tecla (ej: flechas de navegación), forzamos la actualización del detalle visual
        MostrarDetalle();
        
        return handled;
    }
}

// Diálogo de edición de contactos
public sealed class ContactDialog : Dialog 
{
    private readonly IApplication _app;
    public Contacto ContactoResult { get; private set; }
    public bool Canceled { get; private set; } = true;

    private TextField _txtNombre;
    private TextField _txtEmail;
    private List<TextField> _txtTelefonos = new();
    private TextView _txtNotas;
    
    // BYPASS DEFINITIVO: Usamos un botón y un bool en lugar del CheckBox problemático
    private bool _esFavorito = false;
    private Button _btnFavorito;

    public ContactDialog(IApplication app, Contacto contactoExistente = null) 
    {
        _app = app;
        Title  = contactoExistente == null ? "Nuevo contacto" : "Editar contacto";
        Width  = 60;
        Height = 22;

        int yPos = 1;

        Add(new Label() { Text = "Nombre:", X = 1, Y = yPos });
        _txtNombre = new TextField() { X = 15, Y = yPos++, Width = Dim.Fill(1) };
        Add(_txtNombre);

        Add(new Label() { Text = "Email:", X = 1, Y = yPos });
        _txtEmail = new TextField() { X = 15, Y = yPos++, Width = Dim.Fill(1) };
        Add(_txtEmail);

        Add(new Label() { Text = "Teléfonos:", X = 1, Y = yPos });
        for(int i = 0; i < 5; i++) 
        {
            var txtTel = new TextField() { X = 15, Y = yPos++, Width = Dim.Fill(1) };
            _txtTelefonos.Add(txtTel);
            Add(txtTel);
        }

        Add(new Label() { Text = "Notas:", X = 1, Y = yPos });
        _txtNotas = new TextView() { X = 15, Y = yPos, Width = Dim.Fill(1), Height = 4 };
        yPos += 4;
        Add(_txtNotas);

        // Configuramos nuestro botón simulador de CheckBox
        _esFavorito = contactoExistente?.Favorito ?? false;
        _btnFavorito = new Button() { Text = _esFavorito ? "Favorito: SI [X]" : "Favorito: NO [ ]", X = 15, Y = yPos++ };
        _btnFavorito.Accepting += (_, e) => {
            _esFavorito = !_esFavorito; // Cambiamos el estado
            _btnFavorito.Text = _esFavorito ? "Favorito: SI [X]" : "Favorito: NO [ ]"; // Actualizamos el texto
            e.Handled = true;
        };
        Add(_btnFavorito);

        if (contactoExistente != null)
        {
            _txtNombre.Text = contactoExistente.Nombre ?? "";
            _txtEmail.Text = contactoExistente.Email ?? "";
            _txtNotas.Text = contactoExistente.Notas ?? "";

            if (!string.IsNullOrEmpty(contactoExistente.Telefonos))
            {
                var tels = contactoExistente.Telefonos.Split(',');
                for (int i = 0; i < tels.Length && i < 5; i++)
                {
                    _txtTelefonos[i].Text = tels[i].Trim();
                }
            }
        }

        Button btnGuardar = new() { Text = "_Guardar", IsDefault = true };
        Button btnCancelar = new() { Text = "_Cancelar" };

        btnGuardar.Accepting += (_, e) => 
        {
            string nombre = _txtNombre.Text?.ToString() ?? "";
            string email = _txtEmail.Text?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.ErrorQuery(_app, "Error", "El nombre no puede estar vacío.", "OK");
                e.Handled = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
            {
                MessageBox.ErrorQuery(_app, "Error", "El email debe contener '@'.", "OK");
                e.Handled = true;
                return;
            }

            var telefonosIngresados = _txtTelefonos
                .Select(t => t.Text?.ToString())
                .Where(t => !string.IsNullOrWhiteSpace(t));

            ContactoResult = new Contacto
            {
                Id = contactoExistente?.Id ?? 0,
                Nombre = nombre,
                Email = email,
                Telefonos = string.Join(",", telefonosIngresados),
                Notas = _txtNotas.Text?.ToString() ?? "",
                Favorito = _esFavorito // Usamos nuestra variable bool
            };

            Canceled = false;
            _app.RequestStop();
            e.Handled = true;
        };

        btnCancelar.Accepting += (_, e) => 
        {
            _app.RequestStop();
            e.Handled = true;
        };

        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }
}

// Lógica de Persistencia SQLite
public class SqliteAgendaStore 
{
    private readonly string _connectionString;

    public SqliteAgendaStore(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var createTableQuery = @"
        CREATE TABLE IF NOT EXISTS Contactos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Telefonos TEXT,
            Email TEXT,
            Notas TEXT,
            Favorito INTEGER NOT NULL
        );";
        connection.Execute(createTableQuery);
    }

    public IEnumerable<Contacto> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.GetAll<Contacto>();
    }

    public void Insert(Contacto contacto)
    {
        using var connection = new SqliteConnection(_connectionString);
        contacto.Id = (int)connection.Insert(contacto);
    }

    public void Update(Contacto contacto)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Update(contacto);
    }

    public void Delete(Contacto contacto)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Delete(contacto);
    }
}

// Lógica de importación/exportación JSON
public class JsonAgendaIO 
{
    public static void Exportar(IEnumerable<Contacto> contactos, string rutaArchivo)
    {
        var opciones = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        string json = JsonSerializer.Serialize(contactos, opciones);
        File.WriteAllText(rutaArchivo, json);
    }

    public static List<Contacto> Importar(string rutaArchivo)
    {
        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException("El archivo JSON no existe.");
        }

        string json = File.ReadAllText(rutaArchivo);
        var contactos = JsonSerializer.Deserialize<List<Contacto>>(json);
        
        return contactos ?? new List<Contacto>();
    }
}

// Modelo de datos
[Table("Contactos")]
public sealed class Contacto 
{
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

    public Contacto Clone()
    {
        return new Contacto
        {
            Id = this.Id,
            Nombre = this.Nombre,
            Telefonos = this.Telefonos,
            Email = this.Email,
            Notas = this.Notas,
            Favorito = this.Favorito
        };
    }
}