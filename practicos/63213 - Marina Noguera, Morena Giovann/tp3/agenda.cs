#:package Terminal.Gui@2.0.0
#:package Microsoft.Data.Sqlite@9.0.0
#:package Dapper@2.1.35
#:package Dapper.Contrib@2.0.78
#:property PublishAot=false
#:property PublishTrimmed=false

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; 
using System.IO;
using System.Linq;
using System.Text.Json;
using Terminal.Gui;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

var dbPath = args.Length > 0 ? args[0] : "agenda.db";
var store = new SqliteAgendaStore(dbPath);

Application.Init();

Application.Run(new AgendaWindow(store));
Application.Shutdown();

public class AgendaWindow : Window
{
    private readonly SqliteAgendaStore _store;
    private List<Contacto> _contactosEnMemoria = new();
    
    private ListView _listView;
    private TextField _txtSearch;
    private TextView _txtDetail;
    private Label _lblStatus;
    private bool _soloFavoritos = false;

    public AgendaWindow(SqliteAgendaStore store)
    {
        _store = store;
        Title = "AgendaT - Gestión de Contactos";

        var menu = new MenuBar();
        // PASO 1: Agregamos las descripciones de los atajos en el menú
        menu.Menus = new MenuBarItem[] {
            new MenuBarItem("_Archivo", new MenuItem[] { 
                new MenuItem("_Importar JSON (Ctrl+I)", "", Importar), 
                new MenuItem("E_xportar JSON (Ctrl+E)", "", Exportar), 
                new MenuItem("_Salir (Ctrl+Q)", "", () => Application.RequestStop()) 
            }),
            new MenuBarItem("_Contactos", new MenuItem[] { 
                new MenuItem("_Nuevo (F2 / Ctrl+N)", "", NuevoContacto), 
                new MenuItem("E_ditar (F3 / Enter)", "", EditarContacto), 
                new MenuItem("E_liminar (Del / Ctrl+D)", "", EliminarContacto) 
            }),
            new MenuBarItem("_Ver", new MenuItem[] { 
                new MenuItem("Solo _Favoritos", "", AlternarFavoritos) 
            })
        };
        Add(menu);

        KeyDown += (s, e) => {
            if (e.KeyCode == KeyCode.F2 || e.KeyCode == (KeyCode.N | KeyCode.CtrlMask)) { NuevoContacto(); e.Handled = true; }
            if (e.KeyCode == KeyCode.F3 || e.KeyCode == KeyCode.Enter) { EditarContacto(); e.Handled = true; }
            if (e.KeyCode == KeyCode.Delete || e.KeyCode == (KeyCode.D | KeyCode.CtrlMask)) { EliminarContacto(); e.Handled = true; }
            if (e.KeyCode == (KeyCode.I | KeyCode.CtrlMask)) { Importar(); e.Handled = true; }
            if (e.KeyCode == (KeyCode.E | KeyCode.CtrlMask)) { Exportar(); e.Handled = true; }
            if (e.KeyCode == KeyCode.F4) { _txtSearch.SetFocus(); e.Handled = true; }
            if (e.KeyCode == (KeyCode.Q | KeyCode.CtrlMask)) { Application.RequestStop(); e.Handled = true; }
        };

        var lblSearch = new Label { Text = "Buscar (F4):", X = 1, Y = 1 };
        _txtSearch = new TextField { Text = "", X = Pos.Right(lblSearch) + 1, Y = 1, Width = Dim.Fill() };
        
        _txtSearch.TextChanged += (s, e) => ActualizarLista();
        Add(lblSearch, _txtSearch);

        var panelLista = new FrameView { Title = "Contactos", X = 0, Y = 3, Width = Dim.Percent(40), Height = Dim.Fill(1) };
        _listView = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        
        _listView.SelectedItemChanged += (s, e) => MostrarDetalle();
        _listView.OpenSelectedItem += (s, e) => EditarContacto();
        panelLista.Add(_listView);

        var panelDetalle = new FrameView { Title = "Detalles", X = Pos.Right(panelLista), Y = 3, Width = Dim.Fill(), Height = Dim.Fill(1) };
        _txtDetail = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        panelDetalle.Add(_txtDetail);

        Add(panelLista, panelDetalle);

        _lblStatus = new Label { Text = "Listo", X = 1, Y = Pos.AnchorEnd(1) };
        Add(_lblStatus);

        ActualizarLista();
    }

    private void ActualizarLista()
    {
        var termino = _txtSearch.Text.ToString().ToLower();
        var todos = _store.GetAll().ToList();
        
        _contactosEnMemoria = todos.Where(c => 
            (!_soloFavoritos || c.Favorito) &&
            (c.Nombre.ToLower().Contains(termino) || c.Telefonos.ToLower().Contains(termino) || c.Email.ToLower().Contains(termino))
        ).ToList();

        var displayList = _contactosEnMemoria.Select(c => $"{(c.Favorito ? "★" : " ")} {c.Nombre}").ToList();
        
        _listView.SetSource(new ObservableCollection<string>(displayList));
        MostrarDetalle();
    }

    private void MostrarDetalle()
    {
        if (_listView.SelectedItem >= 0 && _listView.SelectedItem < _contactosEnMemoria.Count)
        {
            var c = _contactosEnMemoria[_listView.SelectedItem];
            _txtDetail.Text = $"Nombre: {c.Nombre}\n" +
                              $"Teléfonos: {c.Telefonos}\n" +
                              $"Email: {c.Email}\n" +
                              $"ID Interno: {c.Id}\n\n" +
                              $"Notas:\n{c.Notas}";
        }
        else
        {
            _txtDetail.Text = "";
        }
    }

    private void NuevoContacto()
    {
        var dialog = new ContactDialog();
        Application.Run(dialog);
        if (!dialog.Cancelado)
        {
            _store.Insert(dialog.Result);
            _lblStatus.Text = "Contacto guardado con éxito.";
            ActualizarLista();
        }
    }

    private void EditarContacto()
    {
        if (_listView.SelectedItem >= 0 && _listView.SelectedItem < _contactosEnMemoria.Count)
        {
            var c = _contactosEnMemoria[_listView.SelectedItem];
            var dialog = new ContactDialog(c);
            Application.Run(dialog);
            if (!dialog.Cancelado)
            {
                _store.Update(dialog.Result);
                _lblStatus.Text = "Contacto actualizado.";
                ActualizarLista();
            }
        }
    }

    private void EliminarContacto()
    {
        if (_listView.SelectedItem >= 0 && _listView.SelectedItem < _contactosEnMemoria.Count)
        {
            var c = _contactosEnMemoria[_listView.SelectedItem];
            if (MessageBox.Query("Confirmar", $"¿Seguro de eliminar a {c.Nombre}?", "Sí", "No") == 0)
            {
                _store.Delete(c);
                _lblStatus.Text = "Eliminado y lista reordenada.";
                ActualizarLista();
            }
        }
    }

    private void AlternarFavoritos()
    {
        _soloFavoritos = !_soloFavoritos;
        _lblStatus.Text = _soloFavoritos ? "Mostrando solo favoritos." : "Mostrando todos.";
        ActualizarLista();
    }

    private void Importar()
    {
        var d = new Dialog { Title = "Importar JSON" };
        var txtPath = new TextField { Text = "datos.json", X = 1, Y = 2, Width = 30 };
        d.Add(new Label { Text = "Ruta del archivo:", X = 1, Y = 1 }, txtPath);

        var btnOk = new Button { Text = "Importar" };
        btnOk.Accepting += (s, e) => {
            Application.RequestStop();
            try {
                var nuevos = JsonAgendaIO.Import(txtPath.Text.ToString());
                if (MessageBox.Query("Importar", $"Se hallaron {nuevos.Count()} contactos. ¿Importar?", "Sí", "No") == 0)
                {
                    foreach (var c in nuevos) { c.Id = 0; _store.Insert(c); }
                    _lblStatus.Text = "Importación completada.";
                    ActualizarLista();
                }
            } catch (Exception ex) { MessageBox.ErrorQuery("Error", $"Al importar: {ex.Message}", "Ok"); }
        };
        var btnCancel = new Button { Text = "Cancelar" };
        btnCancel.Accepting += (s, e) => Application.RequestStop();
        
        d.AddButton(btnOk);
        d.AddButton(btnCancel);
        Application.Run(d);
    }

    private void Exportar()
    {
        var d = new Dialog { Title = "Exportar JSON" };
        var txtPath = new TextField { Text = "salida.json", X = 1, Y = 2, Width = 30 };
        d.Add(new Label { Text = "Ruta para guardar:", X = 1, Y = 1 }, txtPath);

        var btnOk = new Button { Text = "Exportar" };
        btnOk.Accepting += (s, e) => {
            Application.RequestStop();
            try {
                JsonAgendaIO.Export(txtPath.Text.ToString(), _store.GetAll());
                _lblStatus.Text = "Exportación completada con éxito.";
            } catch (Exception ex) { MessageBox.ErrorQuery("Error", $"Al exportar: {ex.Message}", "Ok"); }
        };
        var btnCancel = new Button { Text = "Cancelar" };
        btnCancel.Accepting += (s, e) => Application.RequestStop();
        
        d.AddButton(btnOk);
        d.AddButton(btnCancel);
        Application.Run(d);
    }
}

public class ContactDialog : Dialog
{
    public Contacto Result { get; private set; }
    public bool Cancelado { get; private set; } = true;

    private TextField _txtNombre, _txtEmail, _txtTelefonos;
    private TextView _txtNotas;
    private Button _btnFavorito;

    public ContactDialog(Contacto c = null)
    {
        Title = c == null ? "Nuevo Contacto" : "Editar Contacto";
        // Ajustamos las medidas de la ventana para que entre el texto nuevo
        Width = 70;
        Height = 22;

        Result = c != null ? new Contacto { Id = c.Id, Nombre = c.Nombre, Telefonos = c.Telefonos, Email = c.Email, Notas = c.Notas, Favorito = c.Favorito } : new Contacto();

        var lblNombre = new Label { Text = "Nombre:", X = 1, Y = 1 };
        _txtNombre = new TextField { Text = Result.Nombre, X = Pos.Right(lblNombre) + 1, Y = 1, Width = 30 };

        var lblEmail = new Label { Text = "Email:", X = 1, Y = 3 };
        _txtEmail = new TextField { Text = Result.Email, X = Pos.Right(lblEmail) + 2, Y = 3, Width = 30 };

        // PASO 2: El botón del Arroba y su descripción
        var btnArroba = new Button { Text = "@", X = Pos.Right(_txtEmail) + 1, Y = 3 };
        btnArroba.Accepting += (s, e) => {
            _txtEmail.Text = _txtEmail.Text.ToString() + "@";
            _txtEmail.CursorPosition = _txtEmail.Text.Length; // Mueve el cursor al final
            _txtEmail.SetFocus();
            
            e.Cancel = true; // <--- ESTO EVITA QUE SE CIERRE LA VENTANA
        };

        var lblArrobaAyuda = new Label { Text = "(Usa el botón '@' si no puedes escribirlo en tu teclado)", X = Pos.Left(_txtEmail), Y = 4 };

        // Como sumamos el texto en Y = 4, empujamos los teléfonos a Y = 6 (antes estaban en 5)
        var lblTel = new Label { Text = "Teléfonos:", X = 1, Y = 6 };
        _txtTelefonos = new TextField { Text = Result.Telefonos, X = Pos.Right(lblTel) + 1, Y = 6, Width = 30 };

        _btnFavorito = new Button { Text = Result.Favorito ? "[★] Es Favorito" : "[ ] No Favorito", X = 1, Y = 8 };
        _btnFavorito.Accepting += (s, e) => {
            Result.Favorito = !Result.Favorito;
            _btnFavorito.Text = Result.Favorito ? "[★] Es Favorito" : "[ ] No Favorito";
            e.Cancel = true;
        };

        var lblNotas = new Label { Text = "Notas:", X = 1, Y = 10 };
        _txtNotas = new TextView { Text = Result.Notas, X = 1, Y = 11, Width = Dim.Fill(1), Height = Dim.Fill(3) };

        // Agregamos todos los elementos (ahora sumando el btnArroba y el lblArrobaAyuda)
        Add(lblNombre, _txtNombre, lblEmail, _txtEmail, btnArroba, lblArrobaAyuda, lblTel, _txtTelefonos, _btnFavorito, lblNotas, _txtNotas);

        var btnGuardar = new Button { Text = "Guardar", IsDefault = true }; // Hacemos que sea el botón por defecto con Enter
        btnGuardar.Accepting += (s, e) => 
        {
            if (string.IsNullOrWhiteSpace(_txtNombre.Text.ToString())) {
                MessageBox.ErrorQuery("Validación", "El campo Nombre es obligatorio.", "Ok");
                return;
            }
            if (!string.IsNullOrWhiteSpace(_txtEmail.Text.ToString()) && !_txtEmail.Text.ToString().Contains("@")) {
                MessageBox.ErrorQuery("Validación", "El Email debe contener '@'.", "Ok");
                return;
            }

            Result.Nombre = _txtNombre.Text.ToString().Trim();
            Result.Email = _txtEmail.Text.ToString().Trim();
            Result.Telefonos = _txtTelefonos.Text.ToString().Trim();
            Result.Notas = _txtNotas.Text.ToString().Trim();
            
            Cancelado = false;
            Application.RequestStop();
        };

        var btnCancelar = new Button { Text = "Cancelar" };
        btnCancelar.Accepting += (s, e) => Application.RequestStop();

        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }
}

public class SqliteAgendaStore
{
    private string _cs;
    public SqliteAgendaStore(string path) { 
        _cs = $"Data Source={path}"; 
        using var cn = new SqliteConnection(_cs);
        cn.Execute("CREATE TABLE IF NOT EXISTS Contactos (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nombre TEXT NOT NULL, Telefonos TEXT, Email TEXT, Notas TEXT, Favorito INTEGER)"); 
    }

    public IEnumerable<Contacto> GetAll() {
        using var cn = new SqliteConnection(_cs);
        return cn.GetAll<Contacto>().OrderBy(c => c.Id).ToList();
    }

    public void Insert(Contacto c) {
        using var cn = new SqliteConnection(_cs);
        c.Id = (int)cn.Insert(c);
    }

    public void Update(Contacto c) {
        using var cn = new SqliteConnection(_cs);
        cn.Update(c);
    }

    public void Delete(Contacto c) {
        using var cn = new SqliteConnection(_cs);
        cn.Delete(c);
        ReordenarIds(); 
    }

    private void ReordenarIds() {
        using var cn = new SqliteConnection(_cs); 
        var restantes = cn.GetAll<Contacto>().OrderBy(c => c.Id).ToList();
        int nuevoId = 1;
        foreach (var c in restantes) {
            if (c.Id != nuevoId) cn.Execute("UPDATE Contactos SET Id=@nId WHERE Id=@vId", new { nId = nuevoId, vId = c.Id });
            nuevoId++;
        }
        try { cn.Execute("UPDATE sqlite_sequence SET seq = @c WHERE name = 'Contactos'", new { c = restantes.Count }); } catch {}
    }
}

public class JsonAgendaIO {
    public static void Export(string ruta, IEnumerable<Contacto> c) => File.WriteAllText(ruta, JsonSerializer.Serialize(c, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    public static IEnumerable<Contacto> Import(string ruta) => File.Exists(ruta) ? JsonSerializer.Deserialize<IEnumerable<Contacto>>(File.ReadAllText(ruta)) ?? new List<Contacto>() : throw new Exception("El archivo no existe.");
}

[System.ComponentModel.DataAnnotations.Schema.Table("Contactos")]
public class Contacto {
    [System.ComponentModel.DataAnnotations.Key] 
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }
}