#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow());

public sealed class AgendaWindow : Runnable {

    private SqliteAgendaStore _store;

    private List<Contacto> _contactos = new();
    private List<Contacto> _contactosFiltrados = new();

    private ListView _listView = null!;
    private Label _lblDetalles = null!;

    private TextField _txtBusqueda = null!;
    private bool _soloFavoritos = false;

    public AgendaWindow() {

        Title = "AGENDA DE CONTACTOS";
        BorderStyle = LineStyle.Single;
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;

        var args = Environment.GetCommandLineArgs();
        string dbPath = args.Length > 2 ? args.Last() : "agenda.db";

        try {

    _store = new SqliteAgendaStore(dbPath);

    BuildLayout();
    LoadData();
}
catch (Exception ex) {

    MessageBox.Query(
        App!,
        "Error de Base de Datos",
        $"No se pudo abrir la base de datos.\n\n{ex.Message}",
        "OK"
    );

    Environment.Exit(1);
}
        
    }

private void BuildLayout() {

    MenuBar menu = new() {
    Menus = [

        new MenuBarItem("_Archivo", [

            new MenuItem(
                "_Importar JSON",
                "Ctrl+I",
                () => Importar()
            ),

            new MenuItem(
                "_Exportar JSON",
                "Ctrl+E",
                () => Exportar()
            ),

            null!,

            new MenuItem(
                "_Salir",
                "Ctrl+X",
                () => SolicitarSalir()
            )
        ]),


        new MenuBarItem("_Contactos", [

            new MenuItem(
                "_Nuevo",
                "F2",
                () => NuevoContacto()
            ),

            new MenuItem(
                "_Editar",
                "F3",
                () => EditarContacto()
            ),

            new MenuItem(
                "_Eliminar",
                "Del",
                () => EliminarContacto()
            )
        ]),

      
        new MenuBarItem("_Ver", [

            new MenuItem(
                "_Solo favoritos",
                "",
                () => {

                    _soloFavoritos = !_soloFavoritos;

                    AplicarFiltros();
                }
            )
        ]),

        new MenuBarItem("_Ayuda", [

            new MenuItem(
                "_Acerca de",
                "",
                () => {

                    MessageBox.Query(
                        App!,
                        "Acerca de",
                        "Agenda de Contactos",
                        "OK"
                    );
                }
            )
        ])
    ]
};


    Label lblBuscar = new() {
        Text = "Buscar:",
        X = 1,
        Y = 2
    };

    _txtBusqueda = new TextField() {
        X = 10,
        Y = 2,
        Width = 30,
        CanFocus = true
    };

    _txtBusqueda.TextChanged += (_, _) => {
        AplicarFiltros();
    };
    FrameView panelLista = new() {

        Title = "Contactos",

        X = 0,
        Y = 4,

        Width = Dim.Percent(40),
        Height = Dim.Fill()
    };

    _listView = new ListView() {

        X = 0,
        Y = 0,

        Width = Dim.Fill(),
        Height = Dim.Fill(),

        CanFocus = true
    };

    _listView.ValueChanged += (_, _) => {
        MostrarDetalles();
    };

    panelLista.Add(_listView);

    FrameView panelDetalle = new() {

        Title = "Detalles",

        X = Pos.Right(panelLista),
        Y = 4,

        Width = Dim.Fill(),
        Height = Dim.Fill()
    };

    _lblDetalles = new Label() {

        X = 1,
        Y = 1,

        Width = Dim.Fill(1),
        Height = Dim.Fill(1)
    };

    panelDetalle.Add(_lblDetalles);

    Add(
    menu,
    lblBuscar,
    _txtBusqueda,
    panelLista,
    panelDetalle
    );
}

    private void LoadData() {

        _contactos = _store.GetAll();
        AplicarFiltros();
    }

    private void AplicarFiltros() {

        string filtro = _txtBusqueda.Text?.ToString()?.Trim().ToLower() ?? "";

        _contactosFiltrados = _contactos
            .Where(c => {

                bool coincideBusqueda =
                    string.IsNullOrWhiteSpace(filtro)
                    || c.Nombre.ToLower().Contains(filtro)
                    || c.Telefonos.ToLower().Contains(filtro)
                    || c.Email.ToLower().Contains(filtro);

                bool coincideFavorito =
                !_soloFavoritos
                || c.Favorito;

                return coincideBusqueda && coincideFavorito;
            })
            .ToList();

        var items = _contactosFiltrados
            .Select(c => $"{(c.Favorito ? "★ " : "  ")}{c.Nombre}")
            .ToList();

        _listView.SetSource<string>(
            new ObservableCollection<string>(items)
        );

        MostrarDetalles();
    }

    private void MostrarDetalles() {

        int idx = _listView.SelectedItem ?? -1;

        if (idx >= 0 && idx < _contactosFiltrados.Count) {

            var c = _contactosFiltrados[idx];

            _lblDetalles.Text =
                $"Nombre: {c.Nombre}\n" +
                $"Email: {c.Email}\n" +
                $"Teléfonos: {c.Telefonos}\n" +
                $"Favorito: {(c.Favorito ? "Sí" : "No")}\n\n" +
                $"Notas:\n{c.Notas}";
        }
        else {

            _lblDetalles.Text = "(Ningún contacto seleccionado)";
        }
    }

    private void AlternarFavorito() {

        int idx = _listView.SelectedItem ?? -1;

        if (idx < 0 || idx >= _contactosFiltrados.Count)
            return;

        var contacto = _contactosFiltrados[idx];

        contacto.Favorito = !contacto.Favorito;

        _store.Update(contacto);

        LoadData();
    }

    private void NuevoContacto() {

        var dialog = new ContactoDialog();

        App!.Run(dialog);

        if (dialog.Resultado != null) {

            _store.Insert(dialog.Resultado);

            LoadData();
        }
    }

    private void EditarContacto() {

        int idx = _listView.SelectedItem ?? -1;

        if (idx < 0 || idx >= _contactosFiltrados.Count)
            return;

        var original = _contactosFiltrados[idx];

        var dialog = new ContactoDialog(original);

        App!.Run(dialog);

        if (dialog.Resultado != null) {

            var confirmar =
                new ConfirmarDialog($"¿Guardar los cambios en {original.Nombre}?");

            App!.Run(confirmar);

            if (confirmar.Confirmado) {

                _store.Update(dialog.Resultado);

                LoadData();
            }
        }
    }

    private void EliminarContacto() {

        int idx = _listView.SelectedItem ?? -1;

        if (idx < 0 || idx >= _contactosFiltrados.Count)
            return;

        var contacto = _contactosFiltrados[idx];

        var dialog =
            new ConfirmarDialog($"¿Eliminar a {contacto.Nombre}?");

        App!.Run(dialog);

        if (dialog.Confirmado) {

            _store.Delete(contacto);

            LoadData();
        }
    }

    private void Importar() {

        var confirmar =
            new ConfirmarDialog("¿Desea importar los contactos desde el archivo JSON?");

        App!.Run(confirmar);

        if (confirmar.Confirmado) {

            
List<Contacto> importados;

try {

    importados = JsonAgendaIO.Importar("contactos.json");
}
catch (FileNotFoundException) {

    MessageBox.Query(
        App!,
        "Error",
        "El archivo JSON no existe.",
        "OK"
    );

    return;
}
catch (JsonException) {

    MessageBox.Query(
        App!,
        "Error",
        "El archivo JSON tiene un formato inválido.",
        "OK"
    );

    return;
}
catch (Exception ex) {

    MessageBox.Query(
        App!,
        "Error",
        $"No se pudo importar el archivo.\n\n{ex.Message}",
        "OK"
    );

    return;
}
            foreach (var c in importados) {

                c.Id = 0;
                _store.Insert(c);
            }

            LoadData();
        }
    }

    private void Exportar() {
        try {

        SaveDialog saveDialog = new() {

            Title = "Exportar JSON"
        };

        App!.Run(saveDialog);

        if (saveDialog.Canceled)
            return;

        string path = saveDialog.FileName?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!path.EndsWith(".json"))
            path += ".json";

        JsonAgendaIO.Exportar(path, _contactos);

        MessageBox.Query(
            App!,
            "Exportación",
            "Contactos exportados correctamente.",
            "OK"
        );
    }
    catch (Exception ex) {

        MessageBox.Query(
            App!,
            "Error",
            $"No se pudo exportar el JSON.\n\n{ex.Message}",
            "OK"
        );
    }
       
    }

    private void SolicitarSalir() {

        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {

        if (key == Key.CursorUp || key == Key.CursorDown) {

            MostrarDetalles();
        }

        if (key == Key.X.WithCtrl) {

            SolicitarSalir();
            return true;
        }

        if (key == Key.F2) {

            NuevoContacto();
            return true;
        }

        if (key == Key.F3) {

            EditarContacto();
            return true;
        }

        if (key == Key.F4) {

            _txtBusqueda.SetFocus();
            return true;
        }

        if (key == Key.DeleteChar) {

            EliminarContacto();
            return true;
        }

        if (key == Key.I.WithCtrl) {

            Importar();
            return true;
        }

        if (key == Key.E.WithCtrl) {

            Exportar();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class ConfirmarDialog : Dialog {
    public bool Confirmado { get; private set; } = false;
    public ConfirmarDialog(string mensaje) {
        Title = "Confirmar";
        Width = 40;
        Height = 8;
        Add(new Label() {
            Text = mensaje,
            X = Pos.Center(),
            Y = 1
        });
        Button btnSi = new() {
            Text = "_Sí",
            IsDefault = true
        };
        btnSi.Accepting += (_, e) => {

            Confirmado = true;

            App!.RequestStop();

            e.Handled = true;
        };
        Button btnNo = new() {
            Text = "_No"
        };
        btnNo.Accepting += (_, e) => {

            App!.RequestStop();

            e.Handled = true;
        };
        AddButton(btnSi);
        AddButton(btnNo);
    }
}

public sealed class ContactoDialog : Dialog {
    public Contacto? Resultado { get; private set; }
    private TextField _txtNombre;
    private TextField _txtEmail;
    private TextField _txtTels;
    private TextField _txtNotas;
    public ContactoDialog(Contacto? original = null) {
        Title = original == null
            ? "Nuevo Contacto"
            : "Editar Contacto";

        Width = 50;
        Height = 14;
        Add(new Label() {
            Text = "Nombre:",
            X = 1,
            Y = 1
        });
        _txtNombre = new TextField() {
            X = 12,
            Y = 1,
            Width = Dim.Fill() - 1,
            Text = original?.Nombre ?? ""
        };
        Add(new Label() {
            Text = "Teléfonos:",
            X = 1,
            Y = 3
        });
        _txtTels = new TextField() {
            X = 12,
            Y = 3,
            Width = Dim.Fill() - 1,
            Text = original?.Telefonos ?? ""
        };
        Add(new Label() {
            Text = "Email:",
            X = 1,
            Y = 5
        });
        _txtEmail = new TextField() {
            X = 12,
            Y = 5,
            Width = Dim.Fill() - 1,
            Text = original?.Email ?? ""
        };
        Add(new Label() {
            Text = "Notas:",
            X = 1,
            Y = 7
        });
        _txtNotas = new TextField() {
            X = 12,
            Y = 7,
            Width = Dim.Fill() - 1,
            Text = original?.Notas ?? ""
        };
        Button btnGuardar = new() {
            Text = "_Guardar",
            IsDefault = true
        };
        btnGuardar.Accepting += (_, e) => {
            string nombreTxt = _txtNombre.Text.ToString().Trim();
            string telsTxt = _txtTels.Text.ToString().Trim();
            string emailTxt = _txtEmail.Text.ToString().Trim();
            if (string.IsNullOrWhiteSpace(nombreTxt)) {

                DialogErrorInterno("El nombre no puede estar vacío.");

                e.Handled = true;
                return;
            }
            if (emailTxt.Length > 0 && !emailTxt.Contains('@')) {

                DialogErrorInterno("El email debe contener un '@'.");

                e.Handled = true;
                return;
            }
            int cantidadNumeros = telsTxt.Count(char.IsDigit);
            if (cantidadNumeros < 5) {

                DialogErrorInterno(
                    "El teléfono debe tener al menos 5 números."
                );

                e.Handled = true;
                return;
            }
            Resultado = new Contacto {

                Id = original?.Id ?? 0,
                Nombre = nombreTxt,
                Telefonos = telsTxt,
                Email = emailTxt,
                Notas = _txtNotas.Text?.ToString() ?? "",
                Favorito = original?.Favorito ?? false
            };
            App!.RequestStop();
            e.Handled = true;
        };

        Button btnCancelar = new() {
            Text = "_Cancelar"
        };

        btnCancelar.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        Add(_txtNombre, _txtTels, _txtEmail, _txtNotas);
        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }
    private void DialogErrorInterno(string mensaje) {

        Dialog errorDlg = new() {
            Title = "Error de Validación",
            Width = 46,
            Height = 8,
            BorderStyle = LineStyle.Single
        };

        errorDlg.Add(new Label() {
            Text = mensaje,
            X = Pos.Center(),
            Y = 1
        });

        Button btnAceptar = new() {
            Text = "_Aceptar",
            IsDefault = true
        };

        btnAceptar.Accepting += (_, ev) => {
            App!.RequestStop();
            ev.Handled = true;
        };
        errorDlg.AddButton(btnAceptar);
        App!.Run(errorDlg);
    }
}

public class SqliteAgendaStore {
    private string _connStr;
    public SqliteAgendaStore(string dbPath) {
    _connStr = $"Data Source={dbPath}";
    using var conn = new SqliteConnection(_connStr);
    conn.Open();
    conn.Execute(@"
        CREATE TABLE IF NOT EXISTS Contactos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Telefonos TEXT,
            Email TEXT,
            Notas TEXT,
            Favorito INTEGER NOT NULL DEFAULT 0
        )");

    }

    public List<Contacto> GetAll() {
        using var conn = new SqliteConnection(_connStr);
        return conn.GetAll<Contacto>().ToList();
    }

    public void Insert(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        conn.Insert(c);
    }

    public void Update(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        conn.Update(c);
    }

    public void Delete(Contacto c) {
        using var conn = new SqliteConnection(_connStr);
        conn.Delete(c);
    }
}

public class JsonAgendaIO {
    public static void Exportar(
        string path,
        List<Contacto> lista
       ) {
        var json = JsonSerializer.Serialize(
            lista,
            new JsonSerializerOptions {
                WriteIndented = true
            }
        );
        File.WriteAllText(path, json);
    }
    public static List<Contacto> Importar(string path) {
        if (!File.Exists(path))
            throw new FileNotFoundException();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Contacto>>(json)
        ?? new();
    }
}

[Table("Contactos")]
public class Contacto {

    [Key]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Telefonos { get; set; } = "";

    public string Email { get; set; } = "";

    public string Notas { get; set; } = "";

    public bool Favorito { get; set; }
}