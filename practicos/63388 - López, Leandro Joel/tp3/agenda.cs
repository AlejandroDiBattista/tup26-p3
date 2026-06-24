#!/usr/bin/env dotnet
#:property PublishAot=false
#:property NuGetAudit=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@10.0.8
#:package Dapper@2.1.79
#:package Dapper.Contrib@2.0.78

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data.Common;
using Dapper.Contrib.Extensions;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada

try {
    string databasePath = args.Length switch {
        
        0=> "agenda.db",
        1=> args[0],
        _=> throw new ArgumentException("uso: agenda [archivo.db]")
    };

    using IApplication app = Application.Create().Init();
    var store = new SqliteAgendaStore(databasePath);
    var agenda = new AgendaWindow(store);
    app.Run(agenda);
    return 0;

}
 catch (Exception ex) {

    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

// Ventana principal
public sealed class AgendaWindow : Runnable {

    private readonly SqliteAgendaStore store;
    private readonly List <Contacto> contactos = new();
    private readonly List<Contacto> contactosFiltrados = new();
    private readonly ObservableCollection<string> filas = new();
    private MenuItem soloFavoritosMenuItem = null!;
    private bool soloFavoritos;
    private string filtro = string.Empty;
    private ListView listView = null!;
    private TextField searchField = null!;
    private TextView detailsView = null!;
    private Label statusLabel = null!;
    private string ultimaOperacion = "Listo.";
    public AgendaWindow(SqliteAgendaStore store) : base(){

        this.store = store;
        Title  = "Agenda - AgendaT";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        CargarContactos();
    }

    private void BuildLayout() {

        soloFavoritosMenuItem = new MenuItem("_Solo favoritos", string.Empty, ToggleSoloFavoritos);
        
        
        var menu = new MenuBar(new MenuBarItem[]  {
            
                new ("_Archivo", new MenuItem [] {
                    new ("_Importar Json", "Ctrl+I", ImportarJson),
                    new ("_Exportar Json", "Ctrl+E", ExportarJson),                    
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                }),

                new ("_Contacto", new MenuItem [] {
                    new ("_Nuevo contacto", "Ctrl+N", NuevoContacto),
                    new ("_Editar contacto", "Ctrl+E", EditarSeleccionado),
                    new ("_Eliminar contacto", "Ctrl+D", EliminarSeleccionado),
                    new ("_Alternar favorito", string.Empty, AlternarFavorito)
                }),

                new ("_Ver", new MenuItem [] {

                    soloFavoritosMenuItem,
                }),
                new ("_Ayuda", new MenuItem [] {

                    new ("_Acerca de", string.Empty, MostrarAcercaDe)
                })
        });

        var searchLabel = new Label() {
            Text = "Buscar:",
            X    = 1,
            Y    = 1
        };

        searchField = new TextField() {
            Text  = string.Empty,
            X     = Pos.Right(searchLabel) + 1,
            Y     = Pos.Top(searchLabel),
            Width = Dim.Fill(1)
        };

        searchField.TextChanged += (_, _) => {
            filtro = searchField.Text ?? string.Empty;
            ActualizarListaVisible();
        };

        listView = new ListView() {
            X      = 1,
            Y      = 3,
            Width  = Dim.Percent(55),
            Height = Dim.Fill(4)
        };

        listView.Source = new ListWrapper<string>(filas);
        listView.ValueChanged += (_, _) => MostrarDetalle();
        listView.Accepted += (_, _) => EditarSeleccionado();

        detailsView = new TextView() {
            X      = Pos.Right(listView) + 2,
            Y      = Pos.Top(listView),
            Width  = Dim.Fill(1),
            Height = Dim.Fill(4),
            ReadOnly = true,
            WordWrap = true,
            Text = "Sin contactos."
        };

        statusLabel = new Label() {
            Text = ultimaOperacion,
            X    = 1,
            Y    = Pos.AnchorEnd(1),
            Width = Dim.Fill(1)
        };

        Add(menu, searchLabel, searchField, listView, detailsView, statusLabel);
    }
    
    private void CargarContactos() {
        contactos.Clear();
        contactos.AddRange(store.ListarTodos());
        ActualizarListaVisible();
    }

    private void ActualizarListaVisible() {

        contactosFiltrados.Clear();
        filas.Clear();

        string texto = filtro.Trim();
        foreach (Contacto contacto in contactos) {

            if (soloFavoritos && !contacto.Favorito) {

                continue;
            }

            if (!string.IsNullOrWhiteSpace(texto) &&
               !contacto.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) &&
               !contacto.Telefonos.Contains(texto, StringComparison.OrdinalIgnoreCase) &&
               !contacto.Email.Contains(texto, StringComparison.OrdinalIgnoreCase))

                {
                continue;
                }
        
            contactosFiltrados.Add(contacto);
            filas.Add($"{(contacto.Favorito ? "*" : " ")}{contacto.Nombre} - {contacto.Telefonos}");
    }
    listView.Source = new ListWrapper<string>(filas);
    MostrarDetalle();
    }

    private Contacto? Seleccionado() {

        int selected = listView.SelectedItem ?? -1;
        if (selected < 0 || selected >= contactosFiltrados.Count) {

            return null;
        }
        return contactosFiltrados[selected];
    }

    private void MostrarDetalle() {

        Contacto? contacto = Seleccionado();
    detailsView.Text = contacto is null ? " Sin contactos para mostrar." : $"Nombre: {contacto.Nombre}\nTelefonos: {contacto.Telefonos}\nEmail: {contacto.Email}\nFavorito: {(contacto.Favorito ? "Sí" : "No")}\n\nNotas:\n{contacto.Notas}";
    }

    private void NuevoContacto() {

        var dialog = new ContactoDialog("Nuevo contacto", new Contacto());
        App!.Run(dialog);
        if (dialog.Guardado) {
            
            store.Guardar(dialog.Contacto);
            CargarContactos();
            ActualizarEstado("Contacto creado.");
        }
    }

    private void EditarSeleccionado() {

        Contacto? actual = Seleccionado();
        if (actual is null) {

            MessageBox.ErrorQuery(App!, "Agenda", "Selecciona un contacto para editar.", "OK");
            return;
        }

        var dialog = new ContactoDialog("Editar contacto", actual.Clonar());
        App!.Run(dialog);
        if (dialog.Guardado) {

            store.Guardar(dialog.Contacto);
            CargarContactos();
            ActualizarEstado($"Contacto '{dialog.Contacto.Nombre}' actualizado.");
        }
    }

    private void EliminarSeleccionado() {

        Contacto? actual = Seleccionado();
        if (actual is null) {

            MessageBox.ErrorQuery(App!, "Agenda", "Selecciona un contacto para eliminar.", "OK");
            return;
        }

        int response = MessageBox.Query(App!, "Eliminar", $"Eliminar a {actual.Nombre}?", "Sí", "No") ??-1;
        if (response == 1) {

            store.Eliminar(actual.Id);
            CargarContactos();
            ActualizarEstado($"Contacto '{actual.Nombre}' eliminado.");
        }
    }

    private void AlternarFavorito() {

        Contacto? actual = Seleccionado();
        if (actual is null) {

            MessageBox.ErrorQuery(App!, "Agenda", "Selecciona un contacto para alternar favorito.", "OK");
            return;
        }

        actual.Favorito = !actual.Favorito;
        store.Guardar(actual);
        CargarContactos();
        ActualizarEstado(actual.Favorito ? "marcado como favorito" : "desmarcado como favorito");
    }

    private void ToggleSoloFavoritos() {

        soloFavoritos = !soloFavoritos;
        soloFavoritosMenuItem.Title = soloFavoritos ? "_Solo favoritos [x]" : "_Solo favoritos";
        ActualizarListaVisible();
        ActualizarEstado(soloFavoritos ? "Filtrando solo favoritos" : "Mostrando todos los contactos");
    }

    private void ImportarJson() {

        var dialog = new OpenDialog() {
            
            Title = "Importar contactos",

        };

        App!.Run(dialog);

        if (dialog.Canceled) {

            return;

        }

        var pathEntry = dialog.FilePaths.FirstOrDefault();
        if (pathEntry is null) {

            return;
        }

        string path = pathEntry.ToString();
        try {
            
            var importados = JsonAgendaIO.Leer(path);
            if (importados.Count == 0) {

                MessageBox.Query(App!, "Importar", "No se encontraron contactos en el archivo.", "OK");
                return;
            }

            int response = MessageBox.Query(App!, "Importar", $"Se encontraron {importados.Count} contactos. ¿Deseas importarlos?", "Sí", "No") ?? -1;
            if (response != 1) {

                return;
            }

            foreach (var contacto in importados) {

                contacto.Id = 0;
                store.Guardar(contacto);
            }

            CargarContactos();
            ActualizarEstado($"Importados {importados.Count} contactos.");
        }

        catch (Exception ex) {

            MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK");
        }
    }

    private void ExportarJson() {

        var dialog = new SaveDialog() {
            
            Title = "Exportar contactos",

        };

        App!.Run(dialog);

        if (dialog.Canceled || string.IsNullOrWhiteSpace(dialog.FileName?.ToString()) ) {

            return;

        }

        try {
            
            string path = dialog.FileName.ToString();

            JsonAgendaIO.Escribir(path, StoreLocation.ListarTodos());
            MessageBox.Query(App!, "Exportar", $"Contactos exportados correctamente", "OK");
            ActualizarEstado($"Exportados {store.ListarTodos().Count} contactos a {Path.GetFileName(path)}.");
        }
        
        catch (Exception ex) {

            MessageBox.ErrorQuery(App!, "Error", ex.Message, "OK");
        }
    }

    private void MostrarAcercaDe() {

        MessageBox.Query(App!, "Acerca de", "AgendaT - Trabajo Practico 3\nTerminal GUI con Persistencia SQLite e import/export JSON.\n\nAtajos: F2, F3, Del, Ctrl+N, CtrlD, Ctrl+I, Ctrl+E, F4,Ctrl+Q", "OK");
    }

    private void ActualizarEstado(string mensaje) {

        ultimaOperacion = mensaje;
        statusLabel.Text = ultimaOperacion;
    }
    
    private void SolicitarSalir() {

        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {

        if (IsBaseKey(key, KeyCode.F2) || (key.IsCtrl && IsBaseKey(key, KeyCode.N))) {
            
            NuevoContacto();
            return true;
        }

        if (IsBaseKey(key, KeyCode.F3) || IsBaseKey(key, KeyCode.Enter)) {
            
            EditarSeleccionado();
            return true;
        }

        if (IsBaseKey(key, KeyCode.Delete) || (key.IsCtrl && IsBaseKey(key, KeyCode.D))) {
            
            EliminarSeleccionado();
            return true;
        }

        if (key.IsCtrl && IsBaseKey(key, KeyCode.I)) {
            
            ImportarJson();
            return true;
        }

        if (key.IsCtrl && IsBaseKey(key, KeyCode.E)) {
            
            ExportarJson();
            return true;
        }

        if (IsBaseKey(key, KeyCode.F4)) {
            
            searchField.SetFocus();
            return true;
        }
        
        return base.OnKeyDown(key);
    }

    private static bool IsBaseKey(Key key, KeyCode keyCode) {

        return (key.NoCtrl.NoAlt.NoShift.KeyCode == keyCode);
    }
}

// Diálogo de ejemplo
public sealed class ContactoDialog : Dialog {
        
        private readonly TextField nombre = new();
        private readonly TextField[] telefonos = new TextField[3];
        private readonly TextField email = new();
        private readonly CheckBox notas = new() { Height = 4, Width = Dim.Fill(1) };
        private readonly CheckBox favorito = new() { Text = "Favorito  " };
        
        public Contacto Contacto { get; }
        public bool Guardado { get; private set; }

        public ContactoDialog(string title, Contacto contacto): base() {
        
        Title = title;
        Width = 64;
        Height = 20;
        Contacto = contacto;

        int row = 1;
        AddRow("Nombre:", nombre, row++);
        for (int index = 0; index < telefonos.Length; index++) {

            telefonos[index] = new TextField();
            AddRow($"Telefono {index + 1}:", telefonos[index], row++);
        }

        AddRow("Email:", email, row++);

        var notasLabel = new Label() { Text = "Notas:", X = 1, Y = row };
        notas.X = 13;
        notas.Y = row;
        notas.Width = Dim.Fill(2);
        notas.Height = 4;

        favorito.X = 13;
        favorito.Y = row + 5;

        Button guardar = new Button() {

            Text = "_Guardar",
            X = 13,
            Y = row + 7,
            IsDefault = true  
        };
        guardar.Accepted += (_, _) => Guardar();

        Button cancelar = new Button() {

            Text = "_Cancelar",
            X = 25,
            Y = row + 7
        };
        cancelar.Accepted += (_, _) => App!.RequestStop();

        Add(notasLabel, notas, favorito, guardar, cancelar);

        InicializarCampos();
    }

    private void AddRow(string label, TextField field, int y) {
        
        var viewLabel = new Label() { Text = label, X = 1, Y = y };
        field.X = 13;
        field.Y = y;
        field.Width = Dim.Fill(2);
        Add(viewLabel, field);
    }

    private void InicializarCampos() {

        nombre.Text = Contacto.Nombre;
        email.Text = Contacto.Email;
        notas.Text = Contacto.Notas;
        favorito.Value = Contacto.Favorito ? CheckState.Checked : CheckState.UnChecked;

        string[] partes = Contacto.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < telefonos.Length; i++) {

            telefonos[i].Text = i < partes.Length ? partes[i] : string.Empty;
        }
    }

    private void Guardar() {
        
        string nombreTexto = nombre.Text?.ToString().Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreTexto)) {

            MessageBox.ErrorQuery(App!, "Validacion", "El nombre es obligatorio.", "OK");
            return;
        }

        string emailTexto = email.Text?.ToString().Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(emailTexto) && !emailTexto.Contains('@')) {

            MessageBox.ErrorQuery(App!, "Validacion", "El email debe contener un @.", "OK");
            return;
        }

        Contacto.Nombre = nombreTexto;
        Contacto.Email = emailTexto;
        Contacto.Notas = notas.Text?.ToString().Trim() ?? string.Empty;
        Contacto.Favorito = favorito.Value == CheckState.Checked;
        Contacto.Telefonos = string.Join(", ", telefonos.Select(t => t.Text?.ToString().Trim()).Where(t => !string.IsNullOrWhiteSpace(t)));

        Guardado = true;
        App!.RequestStop();
    }
}

public class SqliteAgendaStore {
    
    private readonly string connectionString;

    public SqliteAgendaStore(string databasePath) {
        
        connectionString = new SqliteConnectionStringBuilder() {
            DataSource = databasePath}.ToString();
            Inicializar();
    }

    public IReadOnlyList<Contacto> ListarTodos() {

        using SqliteConnection connection = Abrir();
        return connection.Query<Contacto>(
            """
            SELECT Id, Nombre, Telefonos, Email, Notas, Favorito
            FROM Contactos
            ORDER BY Favorito DESC, Nombre COLLATE NOCASE
            """).ToList();
    }

    public void Guardar(Contacto contacto) {

        using SqliteConnection connection = Abrir();
        if (contacto.Id == 0) {

            contacto.Id = (int)connection.Insert(contacto);
        }
        else {

            connection.Update(contacto);
        }
    }

    public void Eliminar(int id) {

        using SqliteConnection connection = Abrir();
        connection.Execute("DELETE FROM Contactos WHERE Id = @Id", new { id });
    }

    private SqliteConnection Abrir() {

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private void Inicializar() {

        using SqliteConnection connection = Abrir();
        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Notas TEXT NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            )
            """);
    }
}
public static class JsonAgendaIO {
    
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IReadOnlyList<Contacto> Leer(string path) {

        if (!File.Exists(path)) {

            throw new FileNotFoundException("El archivo JSON no existe.", path);
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Contacto>>(json, Options) ?? new List<Contacto>();
    }

    public static void Escribir(string path, IEnumerable<Contacto> contactos) {

        string json = JsonSerializer.Serialize(contactos, Options);
        File.WriteAllText(path, json);
    }

}

[Table("Contactos")]
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = string.Empty;
          public string Telefonos { get; set; } = string.Empty;
          public string Email     { get; set; } = string.Empty;
          public string Notas     { get; set; } = string.Empty;
          public bool   Favorito  { get; set; }

          public Contacto Clonar() => new() {

                  Id = Id,
                  Nombre = Nombre,
                  Telefonos = Telefonos,
                  Email = Email,
                  Notas = Notas,
                  Favorito = Favorito
          };
}