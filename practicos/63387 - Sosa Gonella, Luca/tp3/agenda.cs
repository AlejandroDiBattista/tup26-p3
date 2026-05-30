#!/usr/bin/env -S dotnet run
#:package Dapper@2.1.35
#:package Dapper.Contrib@2.0.78
#:package Microsoft.Data.Sqlite@9.0.5
#:package Terminal.Gui@1.14.0
#:property PublishAot=false

using System.Text.Json;
using Terminal.Gui;
using Microsoft.Data.Sqlite;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

var store = new SqliteAgendaStore(dbPath);

Application.Init();

var top = Application.Top;

var window = new AgendaWindow(store);

top.Add(window);

Application.Run();

Application.Shutdown();

public class AgendaWindow : Window {
    private readonly SqliteAgendaStore store;

    private List<Contacto> contactos = new();
    private List<Contacto> filtrados = new();

    private readonly ListView lista;
    private readonly TextField buscador;
    private readonly Label detalle;

    private bool soloFavoritos = false;

    public AgendaWindow(SqliteAgendaStore store) : base("Agenda") {
        this.store = store;

        X = 0;
        Y = 1;
        Width = Dim.Fill();
        Height = Dim.Fill();

        contactos = store.GetAll();

        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_Archivo", new MenuItem[]
            {
                new MenuItem("_Importar JSON", "", Importar),
                new MenuItem("_Exportar JSON", "", Exportar),
                new MenuItem("_Salir", "", () => Application.RequestStop())
            }),

            new MenuBarItem("_Contactos", new MenuItem[]
            {
                new MenuItem("_Nuevo", "", Nuevo),
                new MenuItem("_Editar", "", Editar),
                new MenuItem("_Eliminar", "", Eliminar)
            }),

            new MenuBarItem("_Ver", new MenuItem[]
            {
                new MenuItem("_Solo favoritos", "", ToggleFavoritos)
            }),

            new MenuBarItem("_Ayuda", new MenuItem[]
            {
                new MenuItem("_Acerca de", "", AcercaDe)
            })
        });

        Add(menu);

        var lblBuscar = new Label("Buscar:") {
            X = 1,
            Y = 1
        };

        Add(lblBuscar);

        buscador = new TextField("") {
            X = 10,
            Y = 1,
            Width = 40
        };

        buscador.TextChanged += (_) => ActualizarFiltro();

        Add(buscador);

        lista = new ListView() {
            X = 1,
            Y = 3,
            Width = 40,
            Height = Dim.Fill() - 2
        };

        lista.SelectedItemChanged += (_) => MostrarDetalle();

        lista.OpenSelectedItem += (_) => Editar();

        Add(lista);

        var frame = new FrameView("Detalle") {
            X = 42,
            Y = 3,
            Width = Dim.Fill() - 1,
            Height = Dim.Fill() - 2
        };

        detalle = new Label("") {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        frame.Add(detalle);

        Add(frame);

        KeyPress += (e) => {
            if (e.KeyEvent.Key == Key.F2)
                Nuevo();

            if (e.KeyEvent.Key == Key.F3 || e.KeyEvent.Key == Key.Enter)
                Editar();

            if (e.KeyEvent.Key == Key.DeleteChar)
                Eliminar();

            if (e.KeyEvent.Key == (Key.CtrlMask | Key.Q))
                Application.RequestStop();
        };

        ActualizarFiltro();
    }

    private void ActualizarFiltro() {
        string texto = buscador.Text.ToString()?.ToLower() ?? "";

        filtrados = contactos
            .Where(c =>
                (
                    c.Nombre.ToLower().Contains(texto) ||
                    c.Telefonos.ToLower().Contains(texto) ||
                    c.Email.ToLower().Contains(texto)
                )
                &&
                (!soloFavoritos || c.Favorito)
            )
            .ToList();

        lista.SetSource(
            filtrados.Select(c =>
                $"{(c.Favorito ? "★" : " ")} {c.Nombre}"
            ).ToList()
        );

        MostrarDetalle();
    }

    private void MostrarDetalle() {
        if (lista.SelectedItem < 0 || lista.SelectedItem >= filtrados.Count) {
            detalle.Text = "";
            return;
        }

        var c = filtrados[lista.SelectedItem];

        detalle.Text =
$@"Nombre:
{c.Nombre}

Telefonos:
{c.Telefonos}

Email:
{c.Email}

Favorito:
{(c.Favorito ? "Sí" : "No")}

Notas:
{c.Notas}";
    }

    private Contacto? ContactoSeleccionado() {
        if (lista.SelectedItem < 0 || lista.SelectedItem >= filtrados.Count)
            return null;

        return filtrados[lista.SelectedItem];
    }

    private void Nuevo() {
        var dialog = new ContactDialog(new Contacto());

        Application.Run(dialog);

        if (!dialog.Aceptado)
            return;

        store.Insert(dialog.Contacto);

        contactos = store.GetAll();

        ActualizarFiltro();
    }

    private void Editar() {
        var seleccionado = ContactoSeleccionado();

        if (seleccionado == null)
            return;

        var dialog = new ContactDialog(seleccionado.Clone());

        Application.Run(dialog);

        if (!dialog.Aceptado)
            return;

        store.Update(dialog.Contacto);

        contactos = store.GetAll();

        ActualizarFiltro();
    }

    private void Eliminar() {
        var seleccionado = ContactoSeleccionado();

        if (seleccionado == null)
            return;

        var r = MessageBox.Query(
            "Confirmar",
            $"¿Eliminar a {seleccionado.Nombre}?",
            "Sí",
            "No"
        );

        if (r != 0)
            return;

        store.Delete(seleccionado);

        contactos = store.GetAll();

        ActualizarFiltro();
    }

    private void ToggleFavoritos() {
        soloFavoritos = !soloFavoritos;

        ActualizarFiltro();
    }

    private void AcercaDe() {
        MessageBox.Query(
            "Acerca de",
            "Agenda TUI TP3",
            "OK"
        );
    }

    private void Exportar() {
        try {
            JsonAgendaIO.Exportar("salida.json", contactos);

            MessageBox.Query(
                "OK",
                "Exportado a salida.json",
                "OK"
            );
        } catch (Exception ex) {
            MessageBox.ErrorQuery(
                "Error",
                ex.Message,
                "OK"
            );
        }
    }

    private void Importar() {
        try {
            var nuevos = JsonAgendaIO.Importar("salida.json");

            foreach (var c in nuevos) {
                c.Id = 0;
                store.Insert(c);
            }

            contactos = store.GetAll();

            ActualizarFiltro();
        } catch (Exception ex) {
            MessageBox.ErrorQuery(
                "Error",
                ex.Message,
                "OK"
            );
        }
    }
}

public class ContactDialog : Dialog {
    public Contacto Contacto { get; private set; }

    public bool Aceptado { get; private set; }

    public ContactDialog(Contacto contacto) : base("Contacto", 60, 20) {
        Contacto = contacto;

        var lblNombre = new Label("Nombre:") {
            X = 1,
            Y = 1
        };

        Add(lblNombre);

        var txtNombre = new TextField(contacto.Nombre) {
            X = 15,
            Y = 1,
            Width = 40
        };

        Add(txtNombre);

        var lblTelefonos = new Label("Telefonos:") {
            X = 1,
            Y = 3
        };

        Add(lblTelefonos);

        var txtTelefonos = new TextField(contacto.Telefonos) {
            X = 15,
            Y = 3,
            Width = 40
        };

        Add(txtTelefonos);

        var lblEmail = new Label("Email:") {
            X = 1,
            Y = 5
        };

        Add(lblEmail);

        var txtEmail = new TextField(contacto.Email) {
            X = 15,
            Y = 5,
            Width = 40
        };

        Add(txtEmail);

        var lblNotas = new Label("Notas:") {
            X = 1,
            Y = 7
        };

        Add(lblNotas);

        var txtNotas = new TextView() {
            X = 15,
            Y = 7,
            Width = 40,
            Height = 5,
            Text = contacto.Notas
        };

        Add(txtNotas);

        var chkFavorito = new CheckBox("Favorito", contacto.Favorito) {
            X = 15,
            Y = 13
        };

        Add(chkFavorito);

        var btnGuardar = new Button("Guardar") {
            X = 15,
            Y = 15
        };

        btnGuardar.Clicked += () => {
            string nombre = txtNombre.Text.ToString() ?? "";
            string email = txtEmail.Text.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(nombre)) {
                MessageBox.ErrorQuery(
                    "Error",
                    "El nombre es obligatorio",
                    "OK"
                );

                return;
            }

            if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@")) {
                MessageBox.ErrorQuery(
                    "Error",
                    "El email debe contener @",
                    "OK"
                );

                return;
            }

            Contacto.Nombre = nombre;
            Contacto.Telefonos = txtTelefonos.Text.ToString() ?? "";
            Contacto.Email = email;
            Contacto.Notas = txtNotas.Text.ToString() ?? "";
            Contacto.Favorito = chkFavorito.Checked;

            Aceptado = true;

            Application.RequestStop();
        };

        AddButton(btnGuardar);

        var btnCancelar = new Button("Cancelar") {
            X = 30,
            Y = 15
        };

        btnCancelar.Clicked += () => {
            Application.RequestStop();
        };

        AddButton(btnCancelar);
    }
}

public class SqliteAgendaStore {
    private readonly string connectionString;

    public SqliteAgendaStore(string path) {
        connectionString = $"Data Source={path}";

        CrearTabla();
    }

    private void CrearTabla() {
        using var db = new SqliteConnection(connectionString);

        db.Open();

        var cmd = db.CreateCommand();

        cmd.CommandText =
@"
CREATE TABLE IF NOT EXISTS Contactos(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL,
    Telefonos TEXT,
    Email TEXT,
    Notas TEXT,
    Favorito INTEGER NOT NULL
)
";

        cmd.ExecuteNonQuery();
    }

    public List<Contacto> GetAll() {
        using var db = new SqliteConnection(connectionString);

        db.Open();

        var cmd = db.CreateCommand();

        cmd.CommandText =
@"
SELECT Id, Nombre, Telefonos, Email, Notas, Favorito
FROM Contactos
ORDER BY Nombre
";

        var reader = cmd.ExecuteReader();

        var lista = new List<Contacto>();

        while (reader.Read()) {
            lista.Add(new Contacto {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Telefonos = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Notas = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Favorito = reader.GetBoolean(5)
            });
        }

        return lista;
    }

    public void Insert(Contacto contacto) {
        using var db = new SqliteConnection(connectionString);

        db.Open();

        var cmd = db.CreateCommand();

        cmd.CommandText =
@"
INSERT INTO Contactos
(Nombre, Telefonos, Email, Notas, Favorito)
VALUES
($nombre, $telefonos, $email, $notas, $favorito)
";

        cmd.Parameters.AddWithValue("$nombre", contacto.Nombre);
        cmd.Parameters.AddWithValue("$telefonos", contacto.Telefonos);
        cmd.Parameters.AddWithValue("$email", contacto.Email);
        cmd.Parameters.AddWithValue("$notas", contacto.Notas);
        cmd.Parameters.AddWithValue("$favorito", contacto.Favorito);

        cmd.ExecuteNonQuery();
    }

    public void Update(Contacto contacto) {
        using var db = new SqliteConnection(connectionString);

        db.Open();

        var cmd = db.CreateCommand();

        cmd.CommandText =
@"
UPDATE Contactos
SET
    Nombre = $nombre,
    Telefonos = $telefonos,
    Email = $email,
    Notas = $notas,
    Favorito = $favorito
WHERE Id = $id
";

        cmd.Parameters.AddWithValue("$id", contacto.Id);
        cmd.Parameters.AddWithValue("$nombre", contacto.Nombre);
        cmd.Parameters.AddWithValue("$telefonos", contacto.Telefonos);
        cmd.Parameters.AddWithValue("$email", contacto.Email);
        cmd.Parameters.AddWithValue("$notas", contacto.Notas);
        cmd.Parameters.AddWithValue("$favorito", contacto.Favorito);

        cmd.ExecuteNonQuery();
    }

    public void Delete(Contacto contacto) {
        using var db = new SqliteConnection(connectionString);

        db.Open();

        var cmd = db.CreateCommand();

        cmd.CommandText = "DELETE FROM Contactos WHERE Id = $id";

        cmd.Parameters.AddWithValue("$id", contacto.Id);

        cmd.ExecuteNonQuery();
    }
}

public static class JsonAgendaIO {
    public static void Exportar(string path, List<Contacto> contactos) {
        var options = new JsonSerializerOptions {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(contactos, options);

        File.WriteAllText(path, json);
    }

    public static List<Contacto> Importar(string path) {
        if (!File.Exists(path))
            throw new Exception("El archivo JSON no existe");

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<List<Contacto>>(json)
               ?? new List<Contacto>();
    }
}

public sealed class Contacto {
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Telefonos { get; set; } = "";

    public string Email { get; set; } = "";

    public string Notas { get; set; } = "";

    public bool Favorito { get; set; }

    public Contacto Clone() {

        return new Contacto {
            Id = Id,
            Nombre = Nombre,
            Telefonos = Telefonos,
            Email = Email,
            Notas = Notas,
            Favorito = Favorito
        };
    }
}
