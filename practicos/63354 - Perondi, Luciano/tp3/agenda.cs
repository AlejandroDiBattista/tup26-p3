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

string archivo = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store = new($"Data Source={archivo}");
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
    List<Contacto> filtrados = [];
    bool soloFavoritos;

    public AgendaWindow(SqliteAgendaStore store) {
        this.store = store;

        Title  = "Agenda";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();

        contactos = store.Listar();
        Refrescar();
        buscar.SetFocus();
    }

    void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson, Key.I.WithCtrl),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson, Key.E.WithCtrl),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", Salir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo", "Ctrl+N", Nuevo, Key.N.WithCtrl),
                    new MenuItem("_Editar", "F3", EditarSeleccionado, Key.F3),
                    new MenuItem("E_liminar", "Ctrl+D", EliminarSeleccionado, Key.D.WithCtrl)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("_Solo favoritos", "", ToggleFavoritos)
                ]),
                new MenuBarItem("Ay_uda", [
                    new MenuItem("_Acerca de", "", AcercaDe)
                ])
            ]
        };

        Label etiquetaBuscar = new() { Text = "Buscar:", X = 1, Y = 2 };
        buscar.X = 10;
        buscar.Y = 2;
        buscar.Width = Dim.Fill(2);
        buscar.TextChanged += (_, _) => Refrescar();
        buscar.KeyDown += (_, key) => {
            if (key == Key.Enter || key == Key.Tab) {
                key.Handled = true;
                lista.SetFocus();
            }
        };

        FrameView panelLista = new() {
            Title  = "Contactos",
            X = 0, Y = 4,
            Width  = Dim.Percent(40),
            Height = Dim.Fill(1)
        };
        lista.Width  = Dim.Fill();
        lista.Height = Dim.Fill();
        lista.ValueChanged += (_, _) => MostrarDetalle();
        lista.KeyDown += (_, key) => {
            if (key == Key.Enter) {
                key.Handled = true;
                EditarSeleccionado();
            } else if (key == Key.Delete) {
                key.Handled = true;
                EliminarSeleccionado();
            }
        };
        panelLista.Add(lista);

        FrameView panelDetalle = new() {
            Title  = "Detalle",
            X = Pos.Right(panelLista), Y = 4,
            Width  = Dim.Fill(),
            Height = Dim.Fill(1)
        };
        detalle.X = 0;
        detalle.Y = 0;
        detalle.Width  = Dim.Fill();
        detalle.Height = Dim.Fill();
        detalle.ReadOnly = true;
        panelDetalle.Add(detalle);

        estado.X = 1;
        estado.Y = Pos.AnchorEnd(1);
        estado.Width = Dim.Fill();
        estado.Text = "Listo.";

        Add(menu, etiquetaBuscar, buscar, panelLista, panelDetalle, estado);
    }

    void Refrescar() {
        string texto = (buscar.Text?.ToString() ?? "").Trim();

        filtrados = contactos
            .Where(c => !soloFavoritos || c.Favorito)
            .Where(c => texto == ""
                || c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || c.Telefonos.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || c.Email.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var lineas = filtrados.Select(c => (c.Favorito ? "* " : "  ") + c.Nombre).ToList();
        lista.SetSource(new ObservableCollection<string>(lineas));
        MostrarDetalle();
    }

    void MostrarDetalle() {
        Contacto? c = ContactoSeleccionado();
        if (c is null) {
            detalle.Text = "";
            return;
        }

        detalle.Text =
            "Nombre:    " + c.Nombre + "\n" +
            "Telefonos: " + c.Telefonos + "\n" +
            "Email:     " + c.Email + "\n" +
            "Favorito:  " + (c.Favorito ? "Si" : "No") + "\n\n" +
            "Notas:\n" + c.Notas;
    }

    Contacto? ContactoSeleccionado() {
        int i = lista.SelectedItem ?? -1;
        if (i < 0 || i >= filtrados.Count) return null;
        return filtrados[i];
    }

    void Nuevo() {
        ContactDialog dialog = new("Nuevo contacto", new Contacto());
        App!.Run(dialog);
        if (!dialog.Aceptado) return;

        store.Insertar(dialog.Contacto);
        contactos = store.Listar();
        estado.Text = "Contacto agregado.";
        Refrescar();
    }

    void EditarSeleccionado() {
        Contacto? c = ContactoSeleccionado();
        if (c is null) { Aviso("Selecciona un contacto."); return; }

        ContactDialog dialog = new("Editar contacto", c.Clone());
        App!.Run(dialog);
        if (!dialog.Aceptado) return;

        store.Actualizar(dialog.Contacto);
        contactos = store.Listar();
        estado.Text = "Contacto actualizado.";
        Refrescar();
    }

    void EliminarSeleccionado() {
        Contacto? c = ContactoSeleccionado();
        if (c is null) { Aviso("Selecciona un contacto."); return; }

        int r = MessageBox.Query(App!, "Eliminar", $"Eliminar a {c.Nombre}?", "No", "Si") ?? 0;
        if (r != 1) return;

        store.Eliminar(c);
        contactos = store.Listar();
        estado.Text = "Contacto eliminado.";
        Refrescar();
    }

    void ToggleFavoritos() {
        soloFavoritos = !soloFavoritos;
        estado.Text = soloFavoritos ? "Mostrando solo favoritos." : "Mostrando todos.";
        Refrescar();
    }

    void ExportarJson() {
        string? ruta = PedirRuta("Exportar JSON", "agenda.json");
        if (ruta is null) return;

        try {
            JsonAgendaIO.Escribir(ruta, contactos);
            estado.Text = "Exportado a " + ruta + ".";
        } catch (Exception ex) {
            Aviso(ex.Message);
        }
    }

    void ImportarJson() {
        string? ruta = PedirRuta("Importar JSON", "agenda.json");
        if (ruta is null) return;

        List<Contacto> nuevos;
        try {
            nuevos = JsonAgendaIO.Leer(ruta);
        } catch (Exception ex) {
            Aviso(ex.Message);
            return;
        }

        int r = MessageBox.Query(App!, "Importar", $"Se agregaran {nuevos.Count} contactos. Continuar?", "No", "Si") ?? 0;
        if (r != 1) return;

        foreach (Contacto c in nuevos) {
            c.Id = 0;
            store.Insertar(c);
        }
        contactos = store.Listar();
        estado.Text = $"Importados {nuevos.Count} contactos.";
        Refrescar();
    }

    string? PedirRuta(string titulo, string rutaInicial) {
        string? resultado = null;

        Dialog dialog = new() { Title = titulo, Width = 70, Height = 8 };
        TextField campo = new() { Text = rutaInicial, X = 1, Y = 1, Width = Dim.Fill(2) };

        Button aceptar = new() { Text = "_Aceptar" };
        aceptar.Accepting += (_, e) => {
            string r = (campo.Text?.ToString() ?? "").Trim();
            if (r != "") {
                resultado = r;
                App!.RequestStop();
            }
            e.Handled = true;
        };

        Button cancelar = new() { Text = "_Cancelar" };
        cancelar.Accepting += (_, e) => {
            App!.RequestStop();
            e.Handled = true;
        };

        dialog.Add(campo);
        dialog.AddButton(aceptar);
        dialog.AddButton(cancelar);
        campo.SetFocus();

        App!.Run(dialog);
        return resultado;
    }

    void Aviso(string mensaje) => MessageBox.Query(App!, "Agenda", mensaje, "OK");

    void AcercaDe() =>
        MessageBox.Query(App!, "Acerca de", "Agenda TUI\nTrabajo Practico 3\nTerminal.Gui + SQLite + JSON", "OK");

    void Salir() => App!.RequestStop();

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) { Salir(); return true; }
        if (key == Key.F2) { Nuevo(); return true; }
        if (key == Key.F4) { buscar.SetFocus(); return true; }
        return base.OnKeyDown(key);
    }
}


public sealed class ContactDialog : Dialog {
    readonly TextField nombre;
    readonly TextField[] telefonos = new TextField[5];
    readonly TextField email;
    readonly TextView notas;
    readonly Button favorito;
    bool esFavorito;
    readonly Contacto original;

    public bool Aceptado { get; private set; }
    public Contacto Contacto { get; private set; }

    public ContactDialog(string titulo, Contacto contacto) {
        original = contacto;
        Contacto = contacto;

        Title  = titulo;
        Width  = 64;
        Height = 20;

        nombre = new TextField { Text = contacto.Nombre, X = 12, Y = 1, Width = Dim.Fill(2) };
        Add(new Label { Text = "Nombre:", X = 1, Y = 1 }, nombre);

        string[] partes = contacto.Telefonos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++) {
            telefonos[i] = new TextField {
                Text = i < partes.Length ? partes[i] : "",
                X = 12, Y = 3 + i, Width = Dim.Fill(2)
            };
            Add(new Label { Text = $"Telefono {i + 1}:", X = 1, Y = 3 + i }, telefonos[i]);
        }

        email = new TextField { Text = contacto.Email, X = 12, Y = 9, Width = Dim.Fill(2) };
        Add(new Label { Text = "Email:", X = 1, Y = 9 }, email);

        esFavorito = contacto.Favorito;
        favorito = new Button { Text = EtiquetaFavorito(), X = 1, Y = 11 };
        favorito.Accepting += (_, e) => {
            e.Handled = true;
            esFavorito = !esFavorito;
            favorito.Text = EtiquetaFavorito();
        };
        Add(favorito);

        Add(new Label { Text = "Notas:", X = 1, Y = 12 });
        notas = new TextView { X = 1, Y = 13, Width = Dim.Fill(2), Height = 3, Text = contacto.Notas };
        Add(notas);

        Button aceptar = new() { Text = "_Aceptar" };
        aceptar.Accepting += (_, e) => {
            e.Handled = true;
            Guardar();
        };

        Button cancelar = new() { Text = "_Cancelar" };
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
            MessageBox.Query(App!, "Validacion", "El nombre no puede estar vacio.", "OK");
            return;
        }

        string mail = (email.Text?.ToString() ?? "").Trim();
        if (mail != "" && !mail.Contains('@')) {
            MessageBox.Query(App!, "Validacion", "El email debe contener @.", "OK");
            return;
        }

        string tel = string.Join(", ", telefonos
            .Select(t => (t.Text?.ToString() ?? "").Trim())
            .Where(t => t != ""));

        Contacto = new Contacto {
            Id        = original.Id,
            Nombre    = nom,
            Telefonos = tel,
            Email     = mail,
            Notas     = notas.Text?.ToString() ?? "",
            Favorito  = esFavorito
        };
        Aceptado = true;
        App!.RequestStop();
    }

    string EtiquetaFavorito() => esFavorito ? "[X] Favorito" : "[ ] Favorito";
}


public sealed class SqliteAgendaStore {
    readonly string connectionString;

    public SqliteAgendaStore(string connectionString) {
        this.connectionString = connectionString;
    }

    SqliteConnection Abrir() {
        SqliteConnection conexion = new(connectionString);
        conexion.Open();
        return conexion;
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
        return db.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
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
    static readonly JsonSerializerOptions Opciones = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Escribir(string ruta, List<Contacto> contactos) {
        File.WriteAllText(ruta, JsonSerializer.Serialize(contactos, Opciones));
    }

    public static List<Contacto> Leer(string ruta) {
        if (!File.Exists(ruta)) {
            throw new Exception($"No existe el archivo: {ruta}");
        }
        return JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(ruta)) ?? [];
    }
}


[Table("Contactos")]
public sealed class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

    public Contacto Clone() => new() {
        Id        = Id,
        Nombre    = Nombre,
        Telefonos = Telefonos,
        Email     = Email,
        Notas     = Notas,
        Favorito  = Favorito
    };
}