#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// 1. Procesar argumentos y arrancar la app
try {
    Console.OutputEncoding = Encoding.UTF8;
    Menu.DefaultBorderStyle = LineStyle.Single;

    string rutaDb = args.Length > 0 ? args[0] : "agenda.db";
    AlmacenAgendaSqlite almacen = new(rutaDb);
    JsonAgendaIO ioJson = new();

    using IApplication app = Application.Create().Init();
    app.Run(new VentanaAgenda(almacen, ioJson));
} catch (Exception ex) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error crítico al iniciar la aplicación: {ex.Message}");
    Console.ResetColor();
}

// 2. Ventana principal
public sealed class VentanaAgenda : Runnable {
    private readonly AlmacenAgendaSqlite almacen;
    private readonly JsonAgendaIO ioJson;

    private List<Contacto> contactos = new();
    private List<Contacto> contactosFiltrados = new();

    private readonly ListView listaContactos = new();
    private readonly TextField campoBusqueda = new();
    private readonly TextView vistaDetalle = new();
    private readonly Label etiquetaEstado = new();

    private bool soloFavoritos = false;

    public VentanaAgenda(AlmacenAgendaSqlite almacen, JsonAgendaIO ioJson) {
        this.almacen = almacen;
        this.ioJson = ioJson;

        Title = "AgendaT - TUI";
        Width = Dim.Fill();
        Height = Dim.Fill();

        ConstruirLayout();
        CargarContactos();
        campoBusqueda.SetFocus();
    }

    private void ConstruirLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON...", "Ctrl+I", ImportarJson),
                    new MenuItem("_Exportar JSON...", "Ctrl+E", ExportarJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contactos", [
                    new MenuItem("_Nuevo", "F2", NuevoContacto),
                    new MenuItem("_Editar", "F3", EditarContactoSeleccionado),
                    new MenuItem("E_liminar", "Del", EliminarContactoSeleccionado)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("Solo _favoritos", "", ToggleFavoritos)
                ]),
                new MenuBarItem("A_yuda", [
                    new MenuItem("Acerca _de", "", AcercaDe)
                ])
            ]
        };

        Label etiquetaBusqueda = new() { Text = "Buscar:", X = 1, Y = 1 };
        campoBusqueda.X = 10;
        campoBusqueda.Y = 1;
        campoBusqueda.Width = Dim.Fill(2);
        campoBusqueda.CanFocus = true;
        campoBusqueda.TextChanged += (_, _) => ActualizarContactos();
        campoBusqueda.KeyDown += (_, key) => {
            if (key == Key.Enter || key == Key.Tab) {
                key.Handled = true;
                listaContactos.SetFocus();
            }
        };

        FrameView maestro = new() {
            Title = "Contactos",
            X = 0,
            Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(2)
        };
        maestro.BorderStyle = LineStyle.Single;

        listaContactos.X = 0;
        listaContactos.Y = 0;
        listaContactos.Width = Dim.Fill();
        listaContactos.Height = Dim.Fill();
        listaContactos.CanFocus = true;
        listaContactos.ValueChanged += (_, _) => ActualizarDetalle();
        listaContactos.Activated += (_, _) => EditarContactoSeleccionado();

        listaContactos.KeyDown += (_, key) => {
            if (key == Key.Enter) {
                key.Handled = true;
                EditarContactoSeleccionado();
            } else if (key == Key.Delete) {
                key.Handled = true;
                EliminarContactoSeleccionado();
            }
        };
        maestro.Add(listaContactos);

        FrameView panelDetalle = new() {
            Title = "Detalle",
            X = Pos.Right(maestro),
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        panelDetalle.BorderStyle = LineStyle.Single;

        vistaDetalle.X = 0;
        vistaDetalle.Y = 0;
        vistaDetalle.Width = Dim.Fill();
        vistaDetalle.Height = Dim.Fill();
        vistaDetalle.ReadOnly = true;
        panelDetalle.Add(vistaDetalle);

        etiquetaEstado.X = 1;
        etiquetaEstado.Y = Pos.AnchorEnd(1);
        etiquetaEstado.Width = Dim.Fill();
        etiquetaEstado.Text = "Listo.";

        Add(menu, etiquetaBusqueda, campoBusqueda, maestro, panelDetalle, etiquetaEstado);
    }

    private void CargarContactos() {
        try {
            contactos = almacen.ObtenerTodos();
            ActualizarContactos();
        } catch (Exception ex) {
            MessageBox.Query(App!, "Error al cargar", $"No se pudieron cargar los contactos: {ex.Message}", "OK");
        }
    }

    private void ActualizarContactos(int? idSeleccionado = null) {
        string busqueda = campoBusqueda.Text?.ToString()?.Trim() ?? "";

        contactosFiltrados = contactos
            .Where(c => Coincide(c, busqueda, soloFavoritos))
            .OrderByDescending(c => c.Favorito)
            .ThenBy(c => c.Nombre)
            .ToList();

        var filas = contactosFiltrados.Select(c => $"{(c.Favorito ? "★" : " ")} {c.Nombre}").ToList();
        listaContactos.SetSource(new ObservableCollection<string>(filas));

        if (idSeleccionado.HasValue) {
            int idx = contactosFiltrados.FindIndex(c => c.Id == idSeleccionado.Value);
            if (idx >= 0) {
                listaContactos.SelectedItem = idx;
            }
        } else if (contactosFiltrados.Count > 0) {
            listaContactos.SelectedItem = 0;
        }

        ActualizarDetalle();
    }

    private bool Coincide(Contacto c, string consulta, bool soloFavoritos) {
        if (soloFavoritos && !c.Favorito) {
            return false;
        }
        if (string.IsNullOrEmpty(consulta)) {
            return true;
        }
        return (c.Nombre?.Contains(consulta, StringComparison.OrdinalIgnoreCase) ?? false)
            || (c.Telefonos?.Contains(consulta, StringComparison.OrdinalIgnoreCase) ?? false)
            || (c.Email?.Contains(consulta, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void ActualizarDetalle() {
        Contacto? seleccionado = ObtenerContactoSeleccionado();
        if (seleccionado is null) {
            vistaDetalle.Text = "Ningún contacto seleccionado.";
            return;
        }

        vistaDetalle.Text = $"""
            Nombre:    {seleccionado.Nombre}
            Email:     {seleccionado.Email}
            Favorito:  {(seleccionado.Favorito ? "Sí [★]" : "No")}
            Teléfonos: {seleccionado.Telefonos}

            Notas:
            {seleccionado.Notas}
            """;
    }

    private Contacto? ObtenerContactoSeleccionado() {
        int idx = listaContactos.SelectedItem ?? -1;
        if (idx >= 0 && idx < contactosFiltrados.Count) {
            return contactosFiltrados[idx];
        }
        return null;
    }

    private void NuevoContacto() {
        Contacto contacto = new();
        DialogoContacto dialogo = new("Nuevo Contacto", contacto);
        App!.Run(dialogo);

        if (!dialogo.Aceptado || dialogo.Contacto is null) {
            return;
        }

        try {
            int nuevoId = almacen.Insertar(dialogo.Contacto);
            dialogo.Contacto.Id = nuevoId;
            contactos.Add(dialogo.Contacto);

            EstablecerEstado($"Contacto '{dialogo.Contacto.Nombre}' creado.");
            ActualizarContactos(nuevoId);
        } catch (Exception ex) {
            MessageBox.Query(App!, "Error", $"No se pudo insertar: {ex.Message}", "OK");
        }
    }

    private void EditarContactoSeleccionado() {
        Contacto? seleccionado = ObtenerContactoSeleccionado();
        if (seleccionado is null) {
            MessageBox.Query(App!, "Editar", "Seleccioná un contacto de la lista.", "OK");
            return;
        }

        DialogoContacto dialogo = new("Editar Contacto", seleccionado.Clone());
        App!.Run(dialogo);

        if (!dialogo.Aceptado || dialogo.Contacto is null) {
            return;
        }

        try {
            almacen.Actualizar(dialogo.Contacto);
            int idx = contactos.FindIndex(c => c.Id == seleccionado.Id);
            if (idx >= 0) {
                contactos[idx] = dialogo.Contacto;
            }

            EstablecerEstado($"Contacto '{dialogo.Contacto.Nombre}' actualizado.");
            ActualizarContactos(dialogo.Contacto.Id);
        } catch (Exception ex) {
            MessageBox.Query(App!, "Error", $"No se pudo actualizar: {ex.Message}", "OK");
        }
    }

    private void EliminarContactoSeleccionado() {
        Contacto? seleccionado = ObtenerContactoSeleccionado();
        if (seleccionado is null) {
            return;
        }

        int respuesta = MessageBox.Query(App!, "Confirmar eliminación", $"¿Estás seguro de que querés eliminar a {seleccionado.Nombre}?", "No", "Sí") ?? 0;
        if (respuesta != 1) {
            return;
        }

        try {
            almacen.Eliminar(seleccionado.Id);
            contactos.RemoveAll(c => c.Id == seleccionado.Id);

            EstablecerEstado("Contacto eliminado.");
            ActualizarContactos();
        } catch (Exception ex) {
            MessageBox.Query(App!, "Error", $"No se pudo eliminar: {ex.Message}", "OK");
        }
    }

    private void ToggleFavoritos() {
        soloFavoritos = !soloFavoritos;
        EstablecerEstado($"Solo favoritos: {(soloFavoritos ? "Sí" : "No")}");
        ActualizarContactos();
    }

    private void AcercaDe() {
        MessageBox.Query(App!, "Acerca de", "AgendaT - Trabajo Práctico 3\nDesarrollado por Octavio González\n© 2026", "Aceptar");
    }

    private void ImportarJson() {
        DialogoRuta dialogo = new("Importar JSON", "agenda.json");
        App!.Run(dialogo);

        if (!dialogo.Aceptado) {
            return;
        }

        try {
            List<Contacto> importados = ioJson.Leer(dialogo.Ruta);
            int respuesta = MessageBox.Query(App!, "Confirmar importación", $"Se agregarán {importados.Count} contactos. ¿Continuar?", "No", "Sí") ?? 0;
            if (respuesta != 1) {
                return;
            }

            foreach (var c in importados) {
                c.Id = 0;
                int nuevoId = almacen.Insertar(c);
                c.Id = nuevoId;
            }

            CargarContactos();
            EstablecerEstado($"Se importaron {importados.Count} contactos.");
        } catch (Exception ex) {
            MessageBox.Query(App!, "Error de importación", $"Error: {ex.Message}", "OK");
        }
    }

    private void ExportarJson() {
        DialogoRuta dialogo = new("Exportar JSON", "agenda.json");
        App!.Run(dialogo);

        if (!dialogo.Aceptado) {
            return;
        }

        try {
            ioJson.Escribir(dialogo.Ruta, contactos);
            EstablecerEstado($"Se exportaron {contactos.Count} contactos a {dialogo.Ruta}.");
        } catch (Exception ex) {
            MessageBox.Query(App!, "Error de exportación", $"Error: {ex.Message}", "OK");
        }
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    private void EstablecerEstado(string mensaje) {
        etiquetaEstado.Text = mensaje;
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.F2 || key == Key.N.WithCtrl) {
            NuevoContacto();
            return true;
        }
        if (key == Key.F3) {
            EditarContactoSeleccionado();
            return true;
        }
        if (key == Key.Delete || key == Key.D.WithCtrl) {
            EliminarContactoSeleccionado();
            return true;
        }
        if (key == Key.I.WithCtrl) {
            ImportarJson();
            return true;
        }
        if (key == Key.E.WithCtrl) {
            ExportarJson();
            return true;
        }
        if (key == Key.F4) {
            campoBusqueda.SetFocus();
            return true;
        }
        if (key == Key.Q.WithCtrl) {
            SolicitarSalir();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

// 3. Diálogo de edición
public sealed class DialogoContacto : Dialog {
    private readonly TextField campoNombre;
    private readonly TextField campoEmail;
    private readonly TextView campoNotas;
    private readonly CheckBox campoFavorito;
    private readonly TextField[] camposTelefono = new TextField[5];

    public bool Aceptado { get; private set; }
    public Contacto Contacto { get; private set; }

    public DialogoContacto(string titulo, Contacto contacto) {
        Title = titulo;
        Width = 70;
        Height = 22;

        Contacto = contacto;

        Label etiquetaNombre = new() { Text = "Nombre *:", X = 2, Y = 1 };
        campoNombre = new() { Text = contacto.Nombre, X = 15, Y = 1, Width = Dim.Fill(4), CanFocus = true };

        Label etiquetaEmail = new() { Text = "Email:", X = 2, Y = 3 };
        campoEmail = new() { Text = contacto.Email, X = 15, Y = 3, Width = Dim.Fill(4), CanFocus = true };

        Label etiquetaFav = new() { Text = "Favorito:", X = 2, Y = 5 };
        campoFavorito = new() {
            Text = "",
            X = 15, Y = 5,
            Value = contacto.Favorito ? CheckState.Checked : CheckState.UnChecked,
            CanFocus = true
        };

        Label etiquetaNotas = new() { Text = "Notas:", X = 2, Y = 7 };
        campoNotas = new() {
            Text = contacto.Notas,
            X = 15, Y = 7,
            Width = Dim.Fill(4),
            Height = 3,
            CanFocus = true
        };

        Label etiquetaTelefono = new() { Text = "Teléfonos (hasta 5):", X = 2, Y = 11 };

        string[] partes = (contacto.Telefonos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < 5; i++) {
            camposTelefono[i] = new TextField() {
                Text = partes.Length > i ? partes[i] : "",
                X = 15 + (i * 10), Y = 11,
                Width = 9,
                CanFocus = true
            };
        }

        Add(etiquetaNombre, campoNombre);
        Add(etiquetaEmail, campoEmail);
        Add(etiquetaFav, campoFavorito);
        Add(etiquetaNotas, campoNotas);
        Add(etiquetaTelefono);
        foreach (var pf in camposTelefono) {
            Add(pf);
        }

        Button botonGuardar = new() { Text = "Guardar", IsDefault = true };
        botonGuardar.Accepted += (_, e) => {
            if (Guardar()) {
                e.Handled = true;
                App!.RequestStop();
            } else {
                e.Handled = true;
            }
        };

        Button botonCancelar = new() { Text = "Cancelar" };
        botonCancelar.Accepted += (_, e) => {
            Aceptado = false;
            e.Handled = true;
            App!.RequestStop();
        };

        AddButton(botonGuardar);
        AddButton(botonCancelar);
    }

    private bool Guardar() {
        string nombre = campoNombre.Text?.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre)) {
            MessageBox.Query(App!, "Error de validación", "El nombre es obligatorio.", "Aceptar");
            campoNombre.SetFocus();
            return false;
        }

        string email = campoEmail.Text?.ToString()?.Trim() ?? "";
        if (!string.IsNullOrEmpty(email) && !email.Contains('@')) {
            MessageBox.Query(App!, "Error de validación", "El email debe contener '@'.", "Aceptar");
            campoEmail.SetFocus();
            return false;
        }

        List<string> telefonos = new();
        for (int i = 0; i < 5; i++) {
            string p = camposTelefono[i].Text?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(p)) {
                telefonos.Add(p);
            }
        }

        Contacto.Nombre = nombre;
        Contacto.Email = email;
        Contacto.Favorito = campoFavorito.Value == CheckState.Checked;
        Contacto.Notas = campoNotas.Text?.ToString() ?? "";
        Contacto.Telefonos = string.Join(",", telefonos);

        Aceptado = true;
        return true;
    }
}

// Diálogo auxiliar para seleccionar ruta de archivo
public sealed class DialogoRuta : Dialog {
    private readonly TextField campoRuta;

    public bool Aceptado { get; private set; }
    public string Ruta => campoRuta.Text?.ToString()?.Trim() ?? "";

    public DialogoRuta(string titulo, string rutaInicial) {
        Title = titulo;
        Width = 60;
        Height = 8;

        Label etiqueta = new() { Text = "Ruta del archivo:", X = 2, Y = 1 };
        campoRuta = new() { Text = rutaInicial, X = 2, Y = 2, Width = Dim.Fill(2), CanFocus = true };

        Button botonAceptar = new() { Text = "Aceptar", IsDefault = true };
        botonAceptar.Accepted += (_, e) => {
            if (!string.IsNullOrWhiteSpace(Ruta)) {
                Aceptado = true;
                e.Handled = true;
                App!.RequestStop();
            }
        };

        Button botonCancelar = new() { Text = "Cancelar" };
        botonCancelar.Accepted += (_, e) => {
            Aceptado = false;
            e.Handled = true;
            App!.RequestStop();
        };

        Add(etiqueta, campoRuta);
        AddButton(botonAceptar);
        AddButton(botonCancelar);
        campoRuta.SetFocus();
    }
}

// 4. Persistencia
public sealed class AlmacenAgendaSqlite {
    private readonly string cadenaConexion;

    public AlmacenAgendaSqlite(string rutaDb) {
        this.cadenaConexion = $"Data Source={rutaDb}";
        AsegurarEsquema();
    }

    private SqliteConnection Abrir() {
        SqliteConnection conexion = new(this.cadenaConexion);
        conexion.Open();
        return conexion;
    }

    private void AsegurarEsquema() {
        using SqliteConnection db = Abrir();
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT    NOT NULL,
                Telefonos TEXT    NOT NULL DEFAULT '',
                Email     TEXT    NOT NULL DEFAULT '',
                Notas     TEXT    NOT NULL DEFAULT '',
                Favorito  INTEGER NOT NULL DEFAULT 0
            );
        """);
    }

    public List<Contacto> ObtenerTodos() {
        using SqliteConnection db = Abrir();
        return db.GetAll<Contacto>().ToList();
    }

    public int Insertar(Contacto contacto) {
        using SqliteConnection db = Abrir();
        return (int)db.Insert(contacto);
    }

    public bool Actualizar(Contacto contacto) {
        using SqliteConnection db = Abrir();
        return db.Update(contacto);
    }

    public bool Eliminar(int id) {
        using SqliteConnection db = Abrir();
        return db.Delete(new Contacto { Id = id });
    }
}

// 5. Interoperabilidad JSON
public sealed class JsonAgendaIO {
    private static readonly JsonSerializerOptions OpcionesJson = new() {
        WriteIndented = true,
        Encoder       = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public List<Contacto> Leer(string ruta) {
        if (!File.Exists(ruta)) {
            throw new FileNotFoundException($"Archivo no encontrado: '{ruta}'");
        }

        string json = File.ReadAllText(ruta, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<Contacto>>(json, OpcionesJson) ?? new();
    }

    public void Escribir(string ruta, IEnumerable<Contacto> contactos) {
        string json = JsonSerializer.Serialize(
            contactos.OrderBy(c => c.Id),
            OpcionesJson);

        File.WriteAllText(ruta, json, Encoding.UTF8);
    }
}

// 6. Modelo de datos
[Table("Contactos")]
public sealed class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

    public Contacto Clone() => new() {
        Id        = this.Id,
        Nombre    = this.Nombre,
        Telefonos = this.Telefonos,
        Email     = this.Email,
        Notas     = this.Notas,
        Favorito  = this.Favorito
    };
}
