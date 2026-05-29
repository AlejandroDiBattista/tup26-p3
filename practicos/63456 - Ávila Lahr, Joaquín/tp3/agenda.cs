#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Text.Json;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string archivoBase = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore baseDatos = new($"Data Source={archivoBase}");

try { baseDatos.Inicializar(); }
catch (Exception ex) {
    Console.WriteLine("No se pudo abrir la base");
    Console.WriteLine(ex.Message);
    return;
}

using IApplication app = Application.Create().Init();

app.Run(new AgendaWindow(baseDatos));

public sealed class AgendaWindow : Runnable {
    readonly SqliteAgendaStore baseDatos;
    readonly ListView lista = new();
    readonly TextField buscar = new();
    readonly TextView detalle = new();
    readonly Label estado = new();
    List<Contacto> contactos = [];
    List<Contacto> visibles = [];
    bool soloFavoritos = false;
    public AgendaWindow(SqliteAgendaStore baseDatos) {
        this.baseDatos = baseDatos;
        Title = "Agenda TUI";
        Width = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;
        CrearInterfaz();
        Recargar();
        buscar.SetFocus();
    }
    void CrearInterfaz() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem(" _Importar JSON ", "", ImportarJson),
                    new MenuItem(" _Exportar JSON ", "", ExportarJson),
                    null!,
                    new MenuItem(" _Salir ", "Ctrl+Q", Salir)
                ]),
                new MenuBarItem("_Contactos", [
                  new MenuItem("_Nuevo", "F2", Nuevo),
                    new MenuItem("_Editar", "Enter", Editar),
                    new MenuItem("_Eliminar", "Del", Eliminar)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("_Solo favoritos", "", ToggleFavoritos)
                ]),
                new MenuBarItem("_Ayuda", [
                    new MenuItem("_Acerca de", "", AcercaDe)
                ])
            ]
        };
        Add(menu);
        Add(new Label() {
            Text = "Buscar:",
            X = 1,
            Y = 2
        });
        buscar.X = 10;
        buscar.Y = 2;
        buscar.Width = 30;
        buscar.TextChanged += (_, _) => ActualizarLista();
        Add(buscar);
        FrameView panelLista = new() {
            Title = "Contactos",
            X = 0,
            Y = 4,
            Width = 30,
            Height = Dim.Fill(2)
        };
        lista.Width = Dim.Fill();
        lista.Height = Dim.Fill();
        lista.ValueChanged += (_, _) => ActualizarDetalle();
        lista.KeyDown += (_, tecla) => {
            if (tecla == Key.Enter) {
                Editar();
                tecla.Handled = true;
            }
            if (tecla == Key.Delete) {
                Eliminar();
                tecla.Handled = true;
            }
        };
        panelLista.Add(lista);
        Add(panelLista);
        FrameView panelDetalle = new() {
            Title = "Detalle",
            X = Pos.Right(panelLista),
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        detalle.X = 1;
        detalle.Y = 0;
        detalle.Width = Dim.Fill(1);
        detalle.Height = Dim.Fill();
        detalle.ReadOnly = true;
        panelDetalle.Add(detalle);
        Add(panelDetalle);
        estado.X = 1;
        estado.Y = Pos.AnchorEnd(1);
        estado.Width = Dim.Fill();
        estado.Text = "Agenda iniciada";
        Add(estado);
    }
    void Recargar() {
        contactos = baseDatos.Listar();
        ActualizarLista();
    }
    void ActualizarLista() {
        string texto =
            (buscar.Text?.ToString() ?? "")
            .ToLower()
            .Trim();
        visibles = contactos
            .Where(c =>
                (!soloFavoritos || c.Favorito) &&
                (
                    c.Nombre.ToLower().Contains(texto) ||
                    c.Telefonos.ToLower().Contains(texto) ||
                    c.Email.ToLower().Contains(texto)
                )
            )
            .ToList();
        lista.SetSource<string>(
            new ObservableCollection<string>(
                visibles.Select(c =>
                    $"{(c.Favorito ? "+" : "")}{c.Nombre}"
                ).ToList()
            )
        );
        ActualizarDetalle();
    }
    void ActualizarDetalle() {
        Contacto? c = ObtenerSeleccionado();
        detalle.Text = c == null ? "" :
            $"Nombre:\n{c.Nombre}\n\n" +
            $"Telefonos:\n{c.Telefonos}\n\n" +
            $"Email:\n{c.Email}\n\n" +
            $"Favorito:\n{(c.Favorito ? "Si" : "No")}\n\n" +
            $"Notas:\n{c.Notas}";
    }
    Contacto? ObtenerSeleccionado() {
        int pos = lista.SelectedItem.HasValue
            ? lista.SelectedItem.Value
            : -1;
        return pos < 0 || pos >= visibles.Count
            ? null
            : visibles[pos];
    }
    void Nuevo() {
        ContactDialog dialogo =
            new("Nuevo contacto", new Contacto());
        App!.Run(dialogo);
        if (!dialogo.Guardado) return;
        baseDatos.Insertar(dialogo.ContactoEditado);
        Recargar();
        estado.Text = "Contacto agregado";
    }
    void Editar() {
        Contacto? c = ObtenerSeleccionado();
        if (c == null) {
            Mensaje("Selecciona un contacto");
            return;
        }
        ContactDialog dialogo =
            new("Editar contacto", c.Clone());
        App!.Run(dialogo);
        if (!dialogo.Guardado) return;
        baseDatos.Actualizar(dialogo.ContactoEditado);
        Recargar();
        estado.Text = "Contacto actualizado";
    }
    void Eliminar() {
        Contacto? c = ObtenerSeleccionado();
        if (c == null) {
            Mensaje("Selecciona un contacto");
            return;
        }
        int r = MessageBox.Query(
            App!,
            "Eliminar",
            $"Deseas eliminar a {c.Nombre}?",
            "Cancelar",
            "Eliminar"
        ) ?? 0;
        if (r != 1) return;
        baseDatos.Eliminar(c);
        Recargar();
        estado.Text = "Contacto eliminado";
    }
    void ToggleFavoritos() {

        soloFavoritos = !soloFavoritos;

        estado.Text = soloFavoritos
            ? "Mostrando favoritos"
            : "Mostrando todos";

        ActualizarLista();
    }
    void ExportarJson() {

        try {
            JsonAgendaIO.Exportar(
                "agenda_exportada.json",
                contactos
            );
            estado.Text = "JSON exportado";
        }
        catch (Exception ex) {
            Mensaje(ex.Message);
        }
    }

    void ImportarJson() {
        try {
            List<Contacto> nuevos =
                JsonAgendaIO.Importar(
                    "agenda_exportada.json"
                );
            int r = MessageBox.Query(
                App!,
                "Importar",
                $"Se agregaran {nuevos.Count} contactos",
                "Cancelar",
                "Importar"
            ) ?? 0;
            if (r != 1) return;
            foreach (Contacto c in nuevos) {
                c.Id = 0;
                baseDatos.Insertar(c);
            }
            Recargar();
            estado.Text = "Importacion completada";
        }
        catch (Exception ex) {
            Mensaje(ex.Message);
        }
    }

    void Mensaje(string texto) {

        MessageBox.ErrorQuery(
            App!,
            "Agenda",
            texto,
            "OK"
        );
    }
    void AcercaDe() {
        MessageBox.Query(
            App!,
            "Acerca de",
            "Agenda TUI\nSQLite + JSON\nTP Programacion",
            "OK"
        );
    }
    void Salir() => App!.RequestStop();
    protected override bool OnKeyDown(Key tecla) {

        if (tecla == Key.Q.WithCtrl) {
            Salir();
            return true;
        }

        if (tecla == Key.F2) {
            Nuevo();
            return true;
        }

        if (tecla == Key.F4) {
            buscar.SetFocus();
            return true;
        }

        return base.OnKeyDown(tecla);
    }
}

public sealed class ContactDialog : Dialog {

    readonly TextField nombre = new();
    readonly TextField email = new();
    readonly TextView notas = new();
    readonly Button favorito = new();

    readonly TextField[] telefonos =
    [
        new(), new(), new(), new(), new()
    ];

    bool favoritoActivo;

    readonly Contacto original;

    public bool Guardado { get; private set; }

    public Contacto ContactoEditado { get; private set; }

    public ContactDialog(string titulo, Contacto contacto) {

        original = contacto;

        ContactoEditado = contacto;

        favoritoActivo = contacto.Favorito;

        Title = titulo;

        Width = 70;
        Height = 22;

        Add(new Label() {
            Text = "Nombre:",
            X = 1,
            Y = 1
        });

        nombre.X = 12;
        nombre.Y = 1;
        nombre.Width = 40;
        nombre.Text = contacto.Nombre;

        Add(nombre);

        string[] datos =
            contacto.Telefonos.Split(",");

        for (int i = 0; i < telefonos.Length; i++) {

            telefonos[i].Text =
                datos.Length > i
                ? datos[i].Trim()
                : "";

            int y = i < 2 ? 3 : i < 4 ? 5 : 7;
            int x = i % 2 == 0 ? 12 : 40;

            Add(new Label() {
                Text = $"Telefono {i + 1}:",
                X = x - 11,
                Y = y
            });

            telefonos[i].X = x;
            telefonos[i].Y = y;
            telefonos[i].Width = 18;

            Add(telefonos[i]);
        }

        Add(new Label() {
            Text = "Email:",
            X = 1,
            Y = 9
        });

        email.X = 12;
        email.Y = 9;
        email.Width = 40;
        email.Text = contacto.Email;

        Add(email);

        favorito.X = 1;
        favorito.Y = 11;

        ActualizarFavorito();

        favorito.Accepting += (_, e) => {

            favoritoActivo = !favoritoActivo;

            ActualizarFavorito();

            e.Handled = true;
        };

        Add(favorito);

        Add(new Label() {
            Text = "Notas:",
            X = 1,
            Y = 13
        });

        notas.X = 1;
        notas.Y = 14;
        notas.Width = Dim.Fill(2);
        notas.Height = 3;
        notas.Text = contacto.Notas;
        Add(notas);
        Button guardar = new() {
            Text = "_Guardar"
        };
        guardar.Accepting += (_, e) => {
            Guardar();
            e.Handled = true;
        };
        Button cancelar = new() {
            Text = "_Cancelar"
        };
        cancelar.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };
        AddButton(guardar);
        AddButton(cancelar);
        nombre.SetFocus();
    }
    void ActualizarFavorito() {
        favorito.Text = favoritoActivo
            ? "[X] Favorito"
            : "[ ] Favorito";
    }
    void Guardar() {
        string nombreTexto =
            (nombre.Text?.ToString() ?? "")
            .Trim();
        if (nombreTexto == "") {
            MessageBox.ErrorQuery(
                App!,
                "Error",
                "El nombre es obligatorio",
                "OK"
            );
            return;
        }
        string emailTexto =
            (email.Text?.ToString() ?? "")
            .Trim();
        if (
            emailTexto != ""
            && !emailTexto.Contains("@")
        ) {
            MessageBox.ErrorQuery(
                App!,
                "Error",
                "Email invalido",
                "OK"
            );
            return;
        }
        List<string> listaTelefonos = [];
        foreach (TextField campo in telefonos) {
            string tel =
                (campo.Text?.ToString() ?? "")
                .Trim();
            if (tel != "") listaTelefonos.Add(tel);
        }
        ContactoEditado = new Contacto {
            Id = original.Id,
            Nombre = nombreTexto,
            Telefonos = string.Join(
                ", ",
                listaTelefonos
            ),
            Email = emailTexto,
            Notas =
               notas.Text?.ToString() ?? "",
            Favorito = favoritoActivo
        };
        Guardado = true;
        App!.RequestStop();
    }
}
public sealed class SqliteAgendaStore {
    readonly string conexion;
    public SqliteAgendaStore(string conexion) {
        this.conexion = conexion;
    }
    SqliteConnection Abrir() {
        SqliteConnection db = new(conexion);
        db.Open();
        return db;
    }
    public void Inicializar() {
        using SqliteConnection db = Abrir();
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL,
                Email TEXT NOT NULL,
                Notas TEXT NOT NULL,
                Favorito INTEGER NOT NULL
            );
        """);
    }
    public List<Contacto> Listar() {
        using SqliteConnection db = Abrir();
        return db
            .GetAll<Contacto>()
            .OrderBy(x => x.Nombre)
            .ToList();
    }
    public void Insertar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Insert(c);
    }
    public void Actualizar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Update(c);
    }
    public void Eliminar(Contacto c) {
        using SqliteConnection db = Abrir();
        db.Delete(c);
    }
}
public static class JsonAgendaIO {
    static readonly JsonSerializerOptions opciones =
        new() { WriteIndented = true };
    public static void Exportar(
        string ruta,
        List<Contacto> contactos
    ) {
        File.WriteAllText(
            ruta,
            JsonSerializer.Serialize(contactos, opciones)
        );
    }
    public static List<Contacto> Importar(string ruta) {
        if (!File.Exists(ruta))
            throw new Exception("No existe el archivo JSON");
        return JsonSerializer.Deserialize<List<Contacto>>(
            File.ReadAllText(ruta)
        ) ?? [];
    }
}
[Table("Contactos")]
public sealed class Contacto {
    [Key]
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }
    public Contacto Clone() => new() {

        Id = Id,
        Nombre = Nombre,
        Telefonos = Telefonos,
        Email = Email,
        Notas = Notas,
        Favorito = Favorito
    };
}