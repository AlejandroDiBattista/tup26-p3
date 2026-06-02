#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

Console.OutputEncoding = System.Text.Encoding.UTF8;
string archivoDb = args.Length > 0 ? args[0] : "miscontactos.db";
SqliteAgendaStore store = new($"Data Source={archivoDb}");
store.Inicializar();
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));
public sealed class AgendaWindow : Runnable {
    readonly SqliteAgendaStore store;
    readonly ListView lista = new();
    readonly TextField buscar = new();
    readonly TextView detalle = new();
    readonly Label estado = new();
    List<Contacto> contactos = [];
    List<Contacto> visibles = [];
    bool verFavs = false;
    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;
        Title = "Agenda TUI";
        Width = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;
        CrearLayout();
        contactos = store.Listar();
        ActualizarLista();
        buscar.SetFocus();
    }
    void CrearLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson, Key.I.WithCtrl),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson, Key.E.WithCtrl),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", Salir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo", "Ctrl+N", NuevoContacto, Key.N.WithCtrl),
                    new MenuItem("_Editar", "F3", EditarContacto, Key.F3),
                    new MenuItem("_Eliminar", "Ctrl+D", EliminarContacto, Key.D.WithCtrl)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("_Solo favoritos", "", CambiarFavoritos)
                ]),
                new MenuBarItem("_Ayuda", [
                    new MenuItem("_Acerca de", "", MostrarInfo)
                ])
            ]
        };
        Label textoBuscar = new() {
            Text = "Buscar:",
            X = 1,
            Y = 2
        };
        buscar.X = 10;
        buscar.Y = 2;
        buscar.Width = Dim.Fill(2);
        buscar.TextChanged += (_, _) => {
            ActualizarLista();
        };
        buscar.KeyDown += (_, k) => {
            if (k == Key.Enter || k == Key.Tab) {
                k.Handled = true;
                lista.SetFocus();
            }
        };
        FrameView cajaLista = new() {
            Title = "Contactos",
            X = 0,
            Y = 4,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1)
        };
        lista.Width = Dim.Fill();
        lista.Height = Dim.Fill();
        lista.ValueChanged += (_, _) => VerDetalle();
        lista.KeyDown += (_, k) => {
            if (k == Key.Enter) {
                k.Handled = true;
                EditarContacto();
            }
            else if (k == Key.Delete) {
                k.Handled = true;
                EliminarContacto();
            }
        };
        cajaLista.Add(lista);
        FrameView cajaDetalle = new() {
            Title = "Detalle",
            X = Pos.Right(cajaLista),
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };
        detalle.X = 0;
        detalle.Y = 0;
        detalle.Width = Dim.Fill();
        detalle.Height = Dim.Fill();
        detalle.ReadOnly = true;
        cajaDetalle.Add(detalle);
        estado.X = 1;
        estado.Y = Pos.AnchorEnd(1);
        estado.Width = Dim.Fill();
        estado.Text = "Agenda iniciada.";
        Add(menu, textoBuscar, buscar, cajaLista, cajaDetalle, estado);
    }

    void ActualizarLista() {
        string txt = (buscar.Text?.ToString() ?? "").Trim();
        visibles = contactos
            .Where(x => !verFavs || x.Favorito)
            .Where(x =>
                txt == ""
                || x.Nombre.Contains(txt, StringComparison.OrdinalIgnoreCase)
                || x.Telefonos.Contains(txt, StringComparison.OrdinalIgnoreCase)
                || x.Email.Contains(txt, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();
        var nombres = visibles
            .Select(x => (x.Favorito ? "* " : "") + x.Nombre)
            .ToList();
        lista.SetSource(new ObservableCollection<string>(nombres));
        VerDetalle();
    }
    void VerDetalle() {
        Contacto? c = ObtenerSeleccionado();
        if (c == null) {
            detalle.Text = "";
            return;
        }
        detalle.Text =
            "Nombre: " + c.Nombre + "\n\n" +
            "Telefonos: " + c.Telefonos + "\n\n" +
            "Email: " + c.Email + "\n\n" +
            "Favorito: " + (c.Favorito ? "Si" : "No") + "\n\n" +
            "Notas:\n" + c.Notas;
    }
    Contacto? ObtenerSeleccionado() {
        int i = lista.SelectedItem ?? -1;
        if (i < 0 || i >= visibles.Count)
            return null;
        return visibles[i];
    }
    void NuevoContacto() {
        ContactDialog d = new("Nuevo", new Contacto());
        App!.Run(d);
        if (!d.Aceptado)
            return;
        store.Insertar(d.Contacto);
        contactos = store.Listar();
        estado.Text = "Contacto agregado.";
        ActualizarLista();
    }
    void EditarContacto() {
        Contacto? c = ObtenerSeleccionado();
        if (c == null) {
            Error("Selecciona un contacto.");
            return;
        }
        ContactDialog d = new("Editar", c.Clone());
        App!.Run(d);
        if (!d.Aceptado)
            return;
        store.Actualizar(d.Contacto);
        contactos = store.Listar();
        estado.Text = "Contacto editado.";
        ActualizarLista();
    }
    void EliminarContacto() {
        Contacto? c = ObtenerSeleccionado();
        if (c == null) {
            Error("No seleccionaste nada.");
            return;
        }
        int r = MessageBox.Query(App!, "Eliminar", $"Borrar a {c.Nombre}?", "No", "Si") ?? 0;
        if (r != 1)
            return;
        store.Eliminar(c);
        contactos = store.Listar();
        estado.Text = "Contacto eliminado.";
        ActualizarLista();
    }
    void CambiarFavoritos() {
        verFavs = !verFavs;
        estado.Text = verFavs
            ? "Solo favoritos."
            : "Mostrando todos.";
        ActualizarLista();
    }
    void ExportarJson() {
        string? ruta = PedirRuta("Exportar", "contactos.json");
        if (ruta == null)
            return;
        try {
            JsonAgendaIO.Guardar(ruta, contactos);
            estado.Text = "JSON exportado.";
        }
        catch (Exception ex) {
            Error(ex.Message);
        }
    }
    void ImportarJson() {
        string? ruta = PedirRuta("Importar", "contactos.json");
        if (ruta == null)
            return;
        List<Contacto> nuevos;
        try {
            nuevos = JsonAgendaIO.Cargar(ruta);
        }
        catch (Exception ex) {
            Error(ex.Message);
            return;
        }
        int r = MessageBox.Query(
            App!,
            "Importar",
            $"Agregar {nuevos.Count} contactos?",
            "No",
            "Si"
        ) ?? 0;
        if (r != 1)
            return;
        foreach (Contacto c in nuevos) {
            c.Id = 0;
            store.Insertar(c);
        }
        contactos = store.Listar();
        estado.Text = "Importacion completa.";
        ActualizarLista();
    }
    string? PedirRuta(string titulo, string inicio) {
        string? resultado = null;
        Dialog d = new() {
            Title = titulo,
            Width = 70,
            Height = 8
        };
        TextField campo = new() {
            Text = inicio,
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };
        Button ok = new() {
            Text = "_Aceptar"
        };
        ok.Accepting += (_, e) => {
            string r = (campo.Text?.ToString() ?? "").Trim();
            if (r != "") {
                resultado = r;
                App!.RequestStop();
            }
            e.Handled = true;
        };
        Button cancelar = new() {
            Text = "_Cancelar"
        };
        cancelar.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };
        d.Add(campo);
        d.AddButton(ok);
        d.AddButton(cancelar);
        campo.SetFocus();
        App!.Run(d);
        return resultado;
    }
    void Error(string txt) {
        MessageBox.Query(App!, "Agenda", txt, "OK");
    }
    void MostrarInfo() {
        MessageBox.Query(
            App!,
            "Info",
            "TP Agenda\nSQLite + JSON",
            "Cerrar"
        );
    }
    void Salir() {
        App!.RequestStop();
    }
    protected override bool OnKeyDown(Key k) {
        if (k == Key.Q.WithCtrl) {
            Salir();
            return true;
        }
        if (k == Key.F2) {
            NuevoContacto();
            return true;
        }
        if (k == Key.F4) {
            buscar.SetFocus();
            return true;
        }
        return base.OnKeyDown(k);
    }
}
public sealed class ContactDialog : Dialog {
    readonly TextField nombre;
    readonly TextField[] tels = new TextField[5];
    readonly TextField email;
    readonly TextView notas;
    readonly Button favorito;
    bool fav;
    readonly Contacto original;
    public bool Aceptado { get; private set; }
    public Contacto Contacto { get; private set; }
    public ContactDialog(string titulo, Contacto contacto) {
        original = contacto;
        Contacto = contacto;
        Title = titulo;
        Width = 64;
        Height = 20;
        nombre = new TextField {
            Text = contacto.Nombre,
            X = 12,
            Y = 1,
            Width = Dim.Fill(2)
        };
        Add(new Label {
            Text = "Nombre:",
            X = 1,
            Y = 1
        }, nombre);
        string[] p = contacto.Telefonos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++) {
            tels[i] = new TextField {
                Text = i < p.Length ? p[i] : "",
                X = 12,
                Y = 3 + i,
                Width = Dim.Fill(2)
            };
            Add(new Label {
                Text = $"Telefono {i + 1}:",
                X = 1,
                Y = 3 + i
            }, tels[i]);
        }
        email = new TextField {
            Text = contacto.Email,
            X = 12,
            Y = 9,
            Width = Dim.Fill(2)
        };
        Add(new Label {
            Text = "Email:",
            X = 1,
            Y = 9
        }, email);
        fav = contacto.Favorito;
        favorito = new Button {
            Text = TextoFav(),
            X = 1,
            Y = 11
        };
        favorito.Accepting += (_, e) => {
            e.Handled = true;
            fav = !fav;
            favorito.Text = TextoFav();
        };
        Add(favorito);
        Add(new Label {
            Text = "Notas:",
            X = 1,
            Y = 12
        });
        notas = new TextView {
            X = 1,
            Y = 13,
            Width = Dim.Fill(2),
            Height = 3,
            Text = contacto.Notas
        };
        Add(notas);
        Button aceptar = new() {
            Text = "_Aceptar"
        };
        aceptar.Accepting += (_, e) => {
            e.Handled = true;
            Guardar();
        };
        Button cancelar = new() {
            Text = "_Cancelar"
        };
        cancelar.Accepting += (_, e) => {
            e.Handled = true;
            App!.RequestStop();
        };
        AddButton(aceptar);
        AddButton(cancelar);
        nombre.SetFocus();
    }
    void Guardar() {
        string nom = (nombre.Text?.ToString() ?? "").Trim();
        if (nom == "") {
            MessageBox.Query(App!, "Error", "Nombre vacio.", "OK");
            return;
        }
        string mail = (email.Text?.ToString() ?? "").Trim();
        if (mail != "" && !mail.Contains('@')) {
            MessageBox.Query(App!, "Error", "Email invalido.", "OK");
            return;
        }
        string tel = string.Join(", ",
            tels
            .Select(x => (x.Text?.ToString() ?? "").Trim())
            .Where(x => x != "")
        );
        Contacto = new Contacto {
            Id = original.Id,
            Nombre = nom,
            Telefonos = tel,
            Email = mail,
            Notas = notas.Text?.ToString() ?? "",
            Favorito = fav
        };
        Aceptado = true;
        App!.RequestStop();
    }
    string TextoFav() {
        return fav
            ? "[X] Favorito"
            : "[ ] Favorito";
    }
}
public static class JsonAgendaIO {
    static readonly JsonSerializerOptions op = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    public static void Guardar(string ruta, List<Contacto> contactos) {
        File.WriteAllText(
            ruta,
            JsonSerializer.Serialize(contactos, op)
        );
    }
    public static List<Contacto> Cargar(string ruta) {
        if (!File.Exists(ruta)) {
            throw new Exception("No existe el json.");
        }
        return JsonSerializer.Deserialize<List<Contacto>>(
            File.ReadAllText(ruta)
        ) ?? [];
    }
}
public sealed class SqliteAgendaStore {
    readonly string conexion;
    public SqliteAgendaStore(string conexion) {
        this.conexion = conexion;
    }
    SqliteConnection Abrir() {
        SqliteConnection c = new(conexion);
        c.Open();
        return c;
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
        return db.GetAll<Contacto>()
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
[Table("Contactos")]
public sealed class Contacto {
    [Key]
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