#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1
#:package Dapper@*
#:package Microsoft.Data.Sqlite@*
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using TuiAttribute = Terminal.Gui.Drawing.Attribute;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Menu.DefaultBorderStyle = LineStyle.Single;

AgendaDb db = new("Data Source=agenda-demo.db");
db.Inicializar();
db.SemillarSiEstaVacia(DatosDemo.Contactos());

using IApplication app = Application.Create().Init();
app.Run(new AgendaApp(db));

record Contacto( long Id, string Nombre, string Apellido, string Email, string Telefonos, string Direccion, string Notas ) {
    public string NombreCompleto => $"{Apellido}, {Nombre}";
}

class AgendaApp : Runnable {
    readonly AgendaDb db;
    readonly ListView lista = new();
    readonly TextField buscar = new();
    readonly Markdown detalle = new();
    readonly Label estado = new();

    List<Contacto> filtrados = [];
    Contacto? seleccionado;

    public AgendaApp(AgendaDb db) {
        this.db = db;

        Title  = "Agenda";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        ConstruirInterfaz();
        Refrescar();
        buscar.SetFocus();
    }

    void ConstruirInterfaz() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("Archivo", [
                    new MenuItem("Importar JSON", "Reemplazar agenda desde JSON", ImportarJson),
                    new MenuItem("Exportar JSON", "Guardar agenda como JSON", ExportarJson),
                    null!,
                    new MenuItem("Salir", "Cerrar la aplicación", () => App!.RequestStop())
                ]),
                new MenuBarItem("Contactos", [
                    new MenuItem("Nuevo", "Crear contacto", Nuevo, Key.N.WithCtrl),
                    new MenuItem("Editar", "Editar contacto seleccionado", EditarSeleccionado, Key.E.WithCtrl),
                    new MenuItem("Eliminar", "Borrar contacto seleccionado", EliminarSeleccionado, Key.D.WithCtrl)
                ])
            ]
        };

        Label buscarLabel = new() { Text = "Buscar:", X = 1, Y = 2 };
        buscar.X = 10;
        buscar.Y = 2;
        buscar.Width = Dim.Fill(2);
        buscar.CanFocus = true;
        buscar.TextChanged += (_, _) => Refrescar();
        buscar.KeyDown += (_, key) => {
            if (key != Key.Enter && key != Key.Tab) {
                return;
            }

            key.Handled = true;
            lista.SetFocus();
        };

        FrameView maestro = new() {
            Title = "Contactos",
            X = 0,
            Y = 4,
            Width = Dim.Percent(36),
            Height = Dim.Fill(2)
        };
        maestro.BorderStyle = LineStyle.Single;

        lista.X = 0;
        lista.Y = 0;
        lista.Width = Dim.Fill();
        lista.Height = Dim.Fill();
        lista.CanFocus = true;
        lista.ValueChanged += (_, _) => Seleccionar(lista.SelectedItem ?? -1);
        lista.Accepted     += (_, _) => EditarSeleccionado();
        lista.Activated    += (_, _) => EditarSeleccionado();
        lista.KeyDown += (_, key) => {
            if (key == Key.Enter) {
                key.Handled = true;
                EditarSeleccionado();
            } else if (key == Key.Delete) {
                key.Handled = true;
                EliminarSeleccionado();
            }
        };
        maestro.Add(lista);

        FrameView panelDetalle = new() {
            Title = "Detalle",
            X = Pos.Right(maestro),
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        panelDetalle.BorderStyle = LineStyle.Single;

        detalle.X = 1;
        detalle.Y = 1;
        detalle.Width = Dim.Fill(2);
        detalle.Height = Dim.Fill(2);
        panelDetalle.Add(detalle);

        estado.X = 1;
        estado.Y = Pos.AnchorEnd(1);
        estado.Width = Dim.Fill();
        estado.Text = "Buscar: Enter/Tab va a contactos | Lista: Enter edita, Delete borra | Menú Contactos: operaciones";

        Add(menu, buscarLabel, buscar, maestro, panelDetalle, estado);
    }

    void Refrescar() {
        string texto = buscar.Text?.ToString()?.Trim() ?? "";

        filtrados = db.Buscar(texto).ToList();

        lista.SetSource(new ObservableCollection<string>(filtrados.Select(c => c.NombreCompleto)));

        if (filtrados.Count == 0) {
            seleccionado = null;
            detalle.Text = "No hay contactos para mostrar.";
            return;
        }

        Seleccionar(Math.Clamp(lista.SelectedItem ?? 0, 0, filtrados.Count - 1));
    }

    void Seleccionar(int indice) {
        seleccionado = indice >= 0 && indice < filtrados.Count ? filtrados[indice] : null;
        detalle.Text = seleccionado is null ? "Sin contacto seleccionado." : FormatearDetalle(seleccionado);
    }

    void Nuevo() {
        ContactoDialog dialog = new("Nuevo contacto", ContactoVacio());
        App!.Run(dialog);

        if (!dialog.Aceptado) {
            return;
        }

        db.Insertar(dialog.Contacto);
        estado.Text = "Contacto creado.";
        Refrescar();
    }

    void EditarSeleccionado() {
        if (!HaySeleccionado(out Contacto contacto)) {
            return;
        }

        ContactoDialog dialog = new("Editar contacto", contacto);
        App!.Run(dialog);

        if (!dialog.Aceptado) {
            return;
        }

        db.Actualizar(dialog.Contacto);
        estado.Text = "Contacto actualizado.";
        Refrescar();
    }

    void EliminarSeleccionado() {
        if (!HaySeleccionado(out Contacto contacto)) {
            return;
        }

        int respuesta = MessageBox.Query(App!, "Eliminar contacto", $"¿Eliminar a {contacto.NombreCompleto}?", "No", "Sí") ?? 0;
        if (respuesta != 1) {
            return;
        }

        db.Eliminar(contacto.Id);
        estado.Text = "Contacto eliminado.";
        Refrescar();
    }

    void ExportarJson() {
        RutaDialog dialog = new("Exportar JSON", "agenda.json");
        App!.Run(dialog);

        if (!dialog.Aceptado) {
            return;
        }

        try {
            JsonSerializerOptions opciones = new() { WriteIndented = true };
            File.WriteAllText(dialog.Ruta, JsonSerializer.Serialize(db.Listar(), opciones));
            estado.Text = $"Agenda exportada a {dialog.Ruta}.";
        } catch (Exception ex) {
            MessageBox.Query(App!, "Exportar JSON", ex.Message, "OK");
        }
    }

    void ImportarJson() {
        RutaDialog dialog = new("Importar JSON", "agenda.json");
        App!.Run(dialog);

        if (!dialog.Aceptado) {
            return;
        }

        if (!File.Exists(dialog.Ruta)) {
            MessageBox.Query(App!, "Importar JSON", $"No existe el archivo:\n{dialog.Ruta}", "OK");
            return;
        }

        List<Contacto> contactos;
        try {
            contactos = JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(dialog.Ruta)) ?? [];
        } catch (Exception ex) {
            MessageBox.Query(App!, "Importar JSON", ex.Message, "OK");
            return;
        }

        int respuesta = MessageBox.Query(App!, "Importar JSON", "La importación reemplazará la agenda actual.", "Cancelar", "Importar") ?? 0;
        if (respuesta != 1) {
            return;
        }

        try {
            db.Reemplazar(contactos);
            estado.Text = $"Agenda importada desde {dialog.Ruta}.";
            Refrescar();
        } catch (Exception ex) {
            MessageBox.Query(App!, "Importar JSON", ex.Message, "OK");
        }
    }

    bool HaySeleccionado(out Contacto contacto) {
        if (seleccionado is not null) {
            contacto = seleccionado;
            return true;
        }

        contacto = ContactoVacio();
        MessageBox.Query(App!, "Agenda", "Seleccioná un contacto.", "OK");
        return false;
    }

    static Contacto ContactoVacio() => new(0, "", "", "", "", "", "");

    static string FormatearDetalle(Contacto c) => $"""
        Apellido:   **{c.Apellido}**

        Nombre:     **{c.Nombre}**

        Email:      **{c.Email}**

        Teléfonos:  **{c.Telefonos}**
        
        Dirección:  **{c.Direccion}**

        Notas:
        {c.Notas}
        """;
}

class AgendaDb {
    readonly string connectionString;

    public AgendaDb(string connectionString) {
        this.connectionString = connectionString;
    }

    SqliteConnection AbrirConexion() {
        SqliteConnection conexion = new(connectionString);
        conexion.Open();
        return conexion;
    }

    public void Inicializar() {
        using SqliteConnection db = AbrirConexion();
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Apellido TEXT NOT NULL,
                Email TEXT NOT NULL,
                Telefonos TEXT NOT NULL,
                Direccion TEXT NOT NULL,
                Notas TEXT NOT NULL
            );
            """);
    }

    public void SemillarSiEstaVacia(IEnumerable<Contacto> contactos) {
        using SqliteConnection db = AbrirConexion();
        if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM Contactos") > 0) {
            return;
        }

        foreach (Contacto contacto in contactos) {
            Insertar(db, contacto);
        }
    }

    public IEnumerable<Contacto> Listar() {
        using SqliteConnection db = AbrirConexion();
        return db.Query<Contacto>("""
            SELECT Id, Nombre, Apellido, Email, Telefonos, Direccion, Notas
            FROM Contactos
            ORDER BY Apellido, Nombre
            """).ToList();
    }

    public IEnumerable<Contacto> Buscar(string texto) {
        using SqliteConnection db = AbrirConexion();
        string patron = $"%{texto}%";

        return db.Query<Contacto>("""
            SELECT Id, Nombre, Apellido, Email, Telefonos, Direccion, Notas
            FROM Contactos
            WHERE @Texto = ''
               OR Nombre LIKE @Patron
               OR Apellido LIKE @Patron
               OR Email LIKE @Patron
               OR Telefonos LIKE @Patron
               OR Direccion LIKE @Patron
               OR Notas LIKE @Patron
            ORDER BY Apellido, Nombre
            """, new { Texto = texto, Patron = patron }).ToList();
    }

    public Contacto Insertar(Contacto contacto) {
        using SqliteConnection db = AbrirConexion();
        long id = Insertar(db, contacto);
        return contacto with { Id = id };
    }

    public void Actualizar(Contacto contacto) {
        using SqliteConnection db = AbrirConexion();
        db.Execute("""
            UPDATE Contactos
            SET Nombre = @Nombre,
                Apellido = @Apellido,
                Email = @Email,
                Telefonos = @Telefonos,
                Direccion = @Direccion,
                Notas = @Notas
            WHERE Id = @Id
            """, contacto);
    }

    public void Eliminar(long id) {
        using SqliteConnection db = AbrirConexion();
        db.Execute("DELETE FROM Contactos WHERE Id = @id", new { id });
    }

    public void Reemplazar(IEnumerable<Contacto> contactos) {
        using SqliteConnection db = AbrirConexion();
        using var tx = db.BeginTransaction();

        db.Execute("DELETE FROM Contactos", transaction: tx);
        foreach (Contacto contacto in contactos) {
            if (contacto.Id <= 0) {
                Insertar(db, NormalizarImportado(contacto), tx);
                continue;
            }

            db.Execute("""
                INSERT INTO Contactos (Id, Nombre, Apellido, Email, Telefonos, Direccion, Notas)
                VALUES (@Id, @Nombre, @Apellido, @Email, @Telefonos, @Direccion, @Notas)
                """, NormalizarImportado(contacto), tx);
        }

        tx.Commit();
    }

    static long Insertar(SqliteConnection db, Contacto contacto, IDbTransaction? tx = null) {
        return db.ExecuteScalar<long>("""
            INSERT INTO Contactos (Nombre, Apellido, Email, Telefonos, Direccion, Notas)
            VALUES (@Nombre, @Apellido, @Email, @Telefonos, @Direccion, @Notas);
            SELECT last_insert_rowid();
            """, contacto, tx);
    }

    static Contacto NormalizarImportado(Contacto c) => c with {
        Id = c.Id <= 0 ? 0 : c.Id,
        Nombre = c.Nombre ?? "",
        Apellido = c.Apellido ?? "",
        Email = c.Email ?? "",
        Telefonos = c.Telefonos ?? "",
        Direccion = c.Direccion ?? "",
        Notas = c.Notas ?? ""
    };
}

static class DatosDemo {
    public static List<Contacto> Contactos() => [
        new(1, "Ada", "Lovelace", "ada@history.dev", "+54 381 111-1111", "San Miguel de Tucumán", "Pionera de la programación."),
        new(2, "Alan", "Turing", "alan@history.dev", "+54 381 222-2222", "Yerba Buena", "Interés en criptografía y computación."),
        new(3, "Grace", "Hopper", "grace@navy.mil", "+54 381 333-3333; +54 381 444-4444", "Tafí Viejo", "Trabaja con compiladores."),
        new(4, "Margaret", "Hamilton", "margaret@nasa.gov", "+54 381 555-5555", "Lules", "Software crítico y gestión de proyectos."),
        new(5, "Barbara", "Liskov", "barbara@mit.edu", "+54 381 666-6666", "Concepción", "Abstracción de datos."),
        new(6, "Donald", "Knuth", "donald@stanford.edu", "+54 381 777-7777", "Monteros", "Algoritmos y documentación técnica."),
        new(7, "Radia", "Perlman", "radia@networking.dev", "+54 381 888-8888", "Famaillá", "Redes y protocolos.")
    ];
}

class RutaDialog : Dialog {
    readonly TextField ruta;

    public bool Aceptado { get; private set; }
    public string Ruta => ruta.Text.ToString()?.Trim() ?? "";

    public RutaDialog(string titulo, string rutaInicial) {
        Title = titulo;
        Width = 72;
        Height = 9;

        Add(
            new Label { Title = "Archivo:", X = 2, Y = 1 },
            ruta = new TextField { Text = rutaInicial, X = 12, Y = 1, Width = Dim.Fill(3), CanFocus = true }
        );

        Button aceptar = new() { Text = "Aceptar" };
        aceptar.Accepted += (_, _) => {
            if (string.IsNullOrWhiteSpace(Ruta)) {
                return;
            }

            Aceptado = true;
            App!.RequestStop();
        };

        Button cancelar = new() { Text = "Cancelar" };
        cancelar.Accepted += (_, _) => App!.RequestStop();

        AddButton(aceptar);
        AddButton(cancelar);
        ruta.SetFocus();
    }
}

class ContactoDialog : Dialog {
    readonly List<CampoTexto> campos = [];

    public bool Aceptado { get; private set; }
    public Contacto Contacto { get; private set; }

    public ContactoDialog(string titulo, Contacto contacto) {
        Contacto = contacto;
        Title = titulo;
        Width = 72;

        int y = 1;
        CampoTexto apellido  = Campo("Apellido:", contacto.Apellido, ref y, Requerido("Ingrese el apellido"));
        CampoTexto nombre    = Campo("Nombre:", contacto.Nombre, ref y, Requerido("Ingrese el nombre"));
        CampoTexto email     = Campo("Email:", contacto.Email, ref y, Requerido("Ingrese el email"));
        CampoTexto telefonos = Campo("Teléfonos:", contacto.Telefonos, ref y, Requerido("Ingrese al menos un teléfono"));
        CampoTexto direccion = Campo("Dirección:", contacto.Direccion, ref y, _ => null);
        CampoTexto notas     = Campo("Notas:", contacto.Notas, ref y, _ => null);

        Button aceptar = new() { Text = "Aceptar" };
        aceptar.Accepted += (_, _) => {
            if (!campos.All(campo => campo.Validar())) {
                return;
            }

            Contacto = contacto with {
                Apellido = apellido.Texto,
                Nombre = nombre.Texto,
                Email = email.Texto,
                Telefonos = telefonos.Texto,
                Direccion = direccion.Texto,
                Notas = notas.Texto
            };
            Aceptado = true;
            App!.RequestStop();
        };

        Button cancelar = new() { Text = "Cancelar" };
        cancelar.Accepted += (_, _) => App!.RequestStop();

        AddButton(aceptar);
        AddButton(cancelar);

        Height = y + 8;
        apellido.Editor.SetFocus();
    }

    CampoTexto Campo(string etiqueta, string valor, ref int y, Func<string, string?> validar) {
        y += 2;
        CampoTexto campo = new(this, etiqueta, valor, y, validar);
        campos.Add(campo);
        return campo;
    }

    static Func<string, string?> Requerido(string mensaje) =>
        texto => string.IsNullOrWhiteSpace(texto) ? mensaje : null;
}

class CampoTexto {
    readonly Func<string, string?> validar;
    readonly Label error;
    readonly View contenedor;

    public TextField Editor { get; }
    public string Texto => Editor.Text.ToString()?.Trim() ?? "";

    public CampoTexto(View contenedor, string etiqueta, string valor, int y, Func<string, string?> validar) {
        this.contenedor = contenedor;
        this.validar = validar;

        Label titulo = new() { Title = etiqueta, X = 2, Y = y };
        Editor = new() { Text = valor, X = 14, Y = y, Width = Dim.Fill(3), CanFocus = true };
        error = new() { X = 14, Y = y + 1, Width = Dim.Fill(3) };
        error.SetScheme(new Scheme { Normal = new TuiAttribute(Color.Red, Color.None) });

        Editor.TextChanged += (_, _) => {
            if (Editor.IsDirty) {
                Validar();
            }
        };

        Editor.KeyDown += (_, key) => {
            if (key != Key.Enter) {
                return;
            }

            key.Handled = true;
            if (Validar()) {
                contenedor.AdvanceFocus(NavigationDirection.Forward, null);
            }
        };

        contenedor.Add(titulo, Editor, error);
    }

    public bool Validar() {
        string? mensaje = validar(Texto);
        error.Text = mensaje ?? "";
        return mensaje is null;
    }
}
