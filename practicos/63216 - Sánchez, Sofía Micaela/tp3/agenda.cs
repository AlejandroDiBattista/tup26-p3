#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.ObjectModel;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

string archivoBase = args.Length > 0 ? args[0] : "agenda.db";

try
{
    using SqliteAgendaStore almacenAgenda = new(archivoBase);
    using IApplication aplicacion = Application.Create().Init();
    aplicacion.Run(new AgendaWindow(almacenAgenda));
}
catch (Exception error)
{
    Console.Error.WriteLine($"No se pudo abrir la agenda: {error.Message}");
    Environment.ExitCode = 1;
}

public sealed class AgendaWindow : Window
{
    private const string AppScheme = "AgendaTP3";

    private readonly SqliteAgendaStore repositorio;
    private readonly List<Contacto> agendaCompleta;
    private readonly List<Contacto> vistaActual = [];

    private TextField buscador = null!;
    private ListView grillaContactos = null!;
    private Label fichaContacto = null!;
    private StatusBar barraInferior = null!;

    private bool verSoloFavoritos;
    private int indiceRecordado;

    public AgendaWindow(SqliteAgendaStore store)
    {
        repositorio = store;
        agendaCompleta = store.GetAll().ToList();

        RegistrarPaleta();

        Title = $"Agenda TP3 - {store.DatabasePath}";
        Width = Dim.Fill();
        Height = Dim.Fill();
        SchemeName = AppScheme;

        Menu.DefaultBorderStyle = LineStyle.Single;

        ArmarPantalla();
        RefrescarListado();
        Informar($"Base abierta. {agendaCompleta.Count} contacto(s) disponible(s).");
    }

    private static void RegistrarPaleta()
    {
        SchemeManager.AddScheme(AppScheme, new Scheme
        {
            Normal = new Terminal.Gui.Drawing.Attribute(Color.BrightCyan, Color.Black),
            Focus = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.BrightCyan),
            Active = new Terminal.Gui.Drawing.Attribute(Color.White, Color.Blue),
            HotNormal = new Terminal.Gui.Drawing.Attribute(Color.BrightYellow, Color.Black),
            HotFocus = new Terminal.Gui.Drawing.Attribute(Color.Black, Color.BrightYellow),
            Editable = new Terminal.Gui.Drawing.Attribute(Color.White, Color.DarkGray),
            Highlight = new Terminal.Gui.Drawing.Attribute(Color.BrightYellow, Color.Blue)
        });
    }

    private void ArmarPantalla()
    {
        MenuBar menuPrincipal = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", CerrarAgenda)
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo", "F2 / Ctrl+N", CrearContacto),
                    new MenuItem("_Editar", "F3 / Enter", ModificarContacto),
                    new MenuItem("_Eliminar", "Del / Ctrl+D", BorrarContacto)
                ]),
                new MenuBarItem("_Ver",
                [
                    new MenuItem("_Solo favoritos", null!, AlternarFavoritos)
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", null!, ShowAbout)
                ])
            ]
        };

        Label etiquetaBusqueda = new()
        {
            Text = "Buscar:",
            X = 1,
            Y = 1,
            Width = 8
        };

        buscador = new TextField
        {
            X = Pos.Right(etiquetaBusqueda) + 1,
            Y = 1,
            Width = Dim.Fill(1)
        };
        buscador.TextChanged += (_, _) => RefrescarListado();

        FrameView panelAgenda = new()
        {
            Title = "Agenda",
            X = 1,
            Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1)
        };
        panelAgenda.SchemeName = AppScheme;

        grillaContactos = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        grillaContactos.ValueChanged += (_, _) =>
        {
            indiceRecordado = grillaContactos.SelectedItem ?? 0;
            DibujarFicha();
        };
        panelAgenda.Add(grillaContactos);

        FrameView panelFicha = new()
        {
            Title = "Ficha",
            X = Pos.Right(panelAgenda) + 1,
            Y = 3,
            Width = Dim.Fill(1),
            Height = Dim.Fill(1)
        };
        panelFicha.SchemeName = AppScheme;

        fichaContacto = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = Dim.Fill()
        };
        panelFicha.Add(fichaContacto);

        barraInferior = new StatusBar(
        [
            new Shortcut(Key.F2, "Nuevo", CrearContacto),
            new Shortcut(Key.F3, "Editar", ModificarContacto),
            new Shortcut(Key.Delete, "Borrar", BorrarContacto),
            new Shortcut(Key.F4, "Buscar", ActivarBusqueda),
            new Shortcut(Key.Q.WithCtrl, "Salir", CerrarAgenda)
        ]);

        Add(menuPrincipal, etiquetaBusqueda, buscador, panelAgenda, panelFicha, barraInferior);
    }
         private void RefrescarListado()
    {
        int idPrevio = ContactoEnFoco()?.Id ?? 0;
        string filtro = buscador?.Text?.ToString() ?? "";

        vistaActual.Clear();
        vistaActual.AddRange(agendaCompleta
            .Where(contacto => (!verSoloFavoritos || contacto.Favorito) && PasaFiltro(contacto, filtro))
            .OrderByDescending(contacto => contacto.Favorito)
            .ThenBy(contacto => contacto.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(contacto => contacto.Id));

        grillaContactos?.SetSource(new ObservableCollection<string>(
            vistaActual.Select(LineaAgenda).ToList()));

        indiceRecordado = 0;
        if (idPrevio != 0)
        {
            int posicionConservada = vistaActual.FindIndex(contacto => contacto.Id == idPrevio);
            indiceRecordado = posicionConservada >= 0 ? posicionConservada : 0;
        }

        if (grillaContactos is not null && vistaActual.Count > 0)
        {
            grillaContactos.SelectedItem = Math.Min(indiceRecordado, vistaActual.Count - 1);
        }

        DibujarFicha();
    }

    private static bool PasaFiltro(Contacto contacto, string filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro))
        {
            return true;
        }

        return contacto.Nombre.Contains(filtro, StringComparison.CurrentCultureIgnoreCase)
            || contacto.Telefonos.Contains(filtro, StringComparison.CurrentCultureIgnoreCase)
            || contacto.Email.Contains(filtro, StringComparison.CurrentCultureIgnoreCase);
    }

    private static string LineaAgenda(Contacto contacto)
    {
        string marca = contacto.Favorito ? "* " : "  ";
        string correo = string.IsNullOrWhiteSpace(contacto.Email) ? "" : $" | {contacto.Email}";
        return $"{marca}{contacto.Nombre}{correo}";
    }

    private Contacto? ContactoEnFoco()
    {
        if (vistaActual.Count == 0)
        {
            return null;
        }

        int posicion = grillaContactos?.SelectedItem ?? indiceRecordado;
        if (posicion < 0 || posicion >= vistaActual.Count)
        {
            posicion = 0;
        }

        return vistaActual[posicion];
    }

    private void DibujarFicha()
    {
        if (fichaContacto is null)
        {
            return;
        }

        Contacto? seleccionado = ContactoEnFoco();
        fichaContacto.Text = seleccionado is null
            ? "No hay contactos para mostrar."
            : TextoFicha(seleccionado);
    } 

      private static string TextoFicha(Contacto contacto)
    {
        string destacado = contacto.Favorito ? "Si" : "No";

        return $"Codigo: {contacto.Id}\n"
            + $"Nombre: {contacto.Nombre}\n"
            + $"Telefonos: {contacto.Telefonos}\n"
            + $"Email: {contacto.Email}\n"
            + $"Favorito: {destacado}\n\n"
            + "Notas:\n"
            + contacto.Notas;
    }

    private void CrearContacto()
    {
        ContactDialog dialog = new();
        App!.Run(dialog);

        if (!dialog.Accepted || dialog.Contact is null)
        {
            Informar("Alta cancelada.");
            return;
        }

        try
        {
            Contacto alta = dialog.Contact;
            alta.Id = repositorio.Insert(alta);

            agendaCompleta.Add(alta);
            RefrescarListado();
            EnfocarPorId(alta.Id);

            Informar($"Contacto agregado: {alta.Nombre}.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error al guardar", ex.Message, "Aceptar");
        }
    }

    private void ModificarContacto()
    {
        Contacto? seleccionado = ContactoEnFoco();
        if (seleccionado is null)
        {
            Informar("No hay contacto seleccionado para editar.");
            return;
        }

        ContactDialog dialog = new(seleccionado);
        App!.Run(dialog);

        if (!dialog.Accepted || dialog.Contact is null)
        {
            Informar("Edicion cancelada.");
            return;
        }

        try
        {
            Contacto editado = dialog.Contact;
            repositorio.Update(editado);

            int posicion = agendaCompleta.FindIndex(contacto => contacto.Id == editado.Id);
            if (posicion >= 0)
            {
                agendaCompleta[posicion] = editado;
            }

            RefrescarListado();
            EnfocarPorId(editado.Id);
            Informar($"Contacto actualizado: {editado.Nombre}.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error al actualizar", ex.Message, "Aceptar");
        }
    }

    private void BorrarContacto()
    {
        Contacto? seleccionado = ContactoEnFoco();
        if (seleccionado is null)
        {
            Informar("No hay contacto seleccionado para eliminar.");
            return;
        }

        int answer = MessageBox.Query(
            App!,
            "Confirmar eliminacion",
            $"Eliminar el contacto \"{seleccionado.Nombre}\"?",
            "Eliminar",
            "Cancelar") ?? 1;

        if (answer != 0)
        {
            Informar("Eliminacion cancelada.");
            return;
        }

        try
        {
            repositorio.Delete(seleccionado);
            agendaCompleta.RemoveAll(contacto => contacto.Id == seleccionado.Id);

            RefrescarListado();
            Informar($"Contacto eliminado: {seleccionado.Nombre}.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error al eliminar", ex.Message, "Aceptar");
        }
    }
       private void ImportarJson()
    {
        string? ruta = PedirRuta(App!, "Importar JSON", "Archivo JSON:", "Importar");
        if (string.IsNullOrWhiteSpace(ruta))
        {
            Informar("Importacion cancelada.");
            return;
        }

        try
        {
            List<Contacto> importados = JsonAgendaIO.Read(ruta).ToList();
            int answer = MessageBox.Query(
                App!,
                "Confirmar importacion",
                $"Se agregaran {importados.Count} contacto(s). Continuar?",
                "Importar",
                "Cancelar") ?? 1;

            if (answer != 0)
            {
                Informar("Importacion cancelada.");
                return;
            }

            foreach (Contacto contacto in importados)
            {
                contacto.Id = 0;
                contacto.Id = repositorio.Insert(contacto);
                agendaCompleta.Add(contacto);
            }

            RefrescarListado();
            Informar($"Importados {importados.Count} contacto(s) desde {ruta}.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error al importar", ex.Message, "Aceptar");
        }
    }

    private void ExportarJson()
    {
        string? ruta = PedirRuta(App!, "Exportar JSON", "Ruta de salida:", "Exportar");
        if (string.IsNullOrWhiteSpace(ruta))
        {
            Informar("Exportacion cancelada.");
            return;
        }

        try
        {
            JsonAgendaIO.Write(ruta, agendaCompleta);
            Informar($"Exportados {agendaCompleta.Count} contacto(s) a {ruta}.");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(App!, "Error al exportar", ex.Message, "Aceptar");
        }
    }

    private static string? PedirRuta(IApplication app, string titulo, string consigna, string textoAccion)
    {
        Dialog dialog = new()
        {
            Title = titulo,
            Width = 74,
            Height = 8
        };

        Label label = new()
        {
            Text = consigna,
            X = 1,
            Y = 1,
            Width = 16
        };

        TextField campoRuta = new()
        {
            X = Pos.Right(label) + 1,
            Y = 1,
            Width = Dim.Fill(1)
        };

        string? rutaElegida = null;

        Button accept = new()
        {
            Text = $"_{textoAccion}",
            IsDefault = true
        };
        accept.Accepting += (_, e) =>
        {
            rutaElegida = campoRuta.Text?.ToString()?.Trim();
            app.RequestStop();
            e.Handled = true;
        };

        Button cancel = new()
        {
            Text = "_Cancelar"
        };
        cancel.Accepting += (_, e) =>
        {
            rutaElegida = null;
            app.RequestStop();
            e.Handled = true;
        };

        dialog.Add(label, campoRuta);
        dialog.AddButton(accept);
        dialog.AddButton(cancel);

        app.Run(dialog);
        return string.IsNullOrWhiteSpace(rutaElegida) ? null : rutaElegida;
    }
       private void AlternarFavoritos()
    {
        verSoloFavoritos = !verSoloFavoritos;
        RefrescarListado();

        Informar(verSoloFavoritos
            ? "Filtro activo: solo favoritos."
            : "Filtro de favoritos desactivado.");
    }

    private void ShowAbout()
    {
        MessageBox.Query(
            App!,
            "Acerca de",
            "AgendaT\nTUI con Terminal.Gui v2, SQLite y JSON.",
            "Aceptar");
    }

    private void ActivarBusqueda()
    {
        buscador.SetFocus();
        Informar("Busqueda activa.");
    }

    private void CerrarAgenda()
    {
        App!.RequestStop();
    }

    private void EnfocarPorId(int id)
    {
        int posicion = vistaActual.FindIndex(contacto => contacto.Id == id);
        if (posicion >= 0)
        {
            grillaContactos.SelectedItem = posicion;
            indiceRecordado = posicion;
            DibujarFicha();
        }
    }

    private void Informar(string mensaje)
    {
        if (barraInferior is not null)
        {
            barraInferior.Text = mensaje;
        }
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.N.WithCtrl || key == Key.F2)
        {
            CrearContacto();
            return true;
        }

        if (key == Key.F3 || key == Key.Enter)
        {
            ModificarContacto();
            return true;
        }

        if (key == Key.D.WithCtrl || key == Key.Delete)
        {
            BorrarContacto();
            return true;
        }

        if (key == Key.I.WithCtrl)
        {
            ImportarJson();
            return true;
        }

        if (key == Key.E.WithCtrl)
        {
            ExportarJson();
            return true;
        }

        if (key == Key.F4)
        {
            ActivarBusqueda();
            return true;
        }

        if (key == Key.Q.WithCtrl)
        {
            CerrarAgenda();
            return true;
        }

        bool handled = base.OnKeyDown(key);
        indiceRecordado = grillaContactos?.SelectedItem ?? indiceRecordado;
        DibujarFicha();
        return handled;
    }
}
public sealed class ContactDialog : Dialog
{
    private readonly TextField entradaNombre;
    private readonly TextField[] entradasTelefonicas;
    private readonly TextField entradaCorreo;
    private readonly TextView entradaNotas;
    private readonly CheckBox marcaFavorito;

    public new bool Accepted { get; private set; }
    public Contacto? Contact { get; private set; }

    public ContactDialog(Contacto? contact = null)
    {
        Contacto borrador = contact?.Clone() ?? new Contacto();

        Title = contact is null ? "Nuevo contacto" : "Editar contacto";
        Width = 76;
        Height = 22;

        Label rotuloNombre = CrearRotulo("Nombre:", 1, 1);
        entradaNombre = CrearEntrada(Pos.Right(rotuloNombre) + 1, 1, borrador.Nombre);

        entradasTelefonicas = new TextField[5];
        List<Label> rotulosTelefono = [];
        string[] telefonosPrevios = PhoneTextTools.Separar(borrador.Telefonos).Take(5).ToArray();

        for (int vuelta = 0; vuelta < entradasTelefonicas.Length; vuelta++)
        {
            Label rotuloTelefono = CrearRotulo($"Telefono {vuelta + 1}:", 1, 3 + vuelta);
            rotulosTelefono.Add(rotuloTelefono);
            entradasTelefonicas[vuelta] = CrearEntrada(
                Pos.Right(rotuloTelefono) + 1,
                3 + vuelta,
                vuelta < telefonosPrevios.Length ? telefonosPrevios[vuelta] : "");
        }

        Label rotuloCorreo = CrearRotulo("Email:", 1, 9);
        entradaCorreo = CrearEntrada(Pos.Right(rotuloCorreo) + 1, 9, borrador.Email);

        marcaFavorito = new CheckBox
        {
            Text = "Favorito",
            X = 13,
            Y = 11,
            Value = borrador.Favorito ? CheckState.Checked : CheckState.UnChecked
        };

        Label rotuloNotas = CrearRotulo("Notas:", 1, 13);
        entradaNotas = new TextView
        {
            X = 13,
            Y = 13,
            Width = Dim.Fill(1),
            Height = 4,
            Text = borrador.Notas
        };

        Button botonGuardar = new()
        {
            Text = "_Guardar",
            IsDefault = true
        };
        botonGuardar.Accepting += (_, evento) =>
        {
            if (IntentarArmarContacto(borrador.Id, out Contacto? resultado))
            {
                Contact = resultado;
                Accepted = true;
                App!.RequestStop();
            }

            evento.Handled = true;
        };

        Button botonCancelar = new()
        {
            Text = "_Cancelar"
        };
        botonCancelar.Accepting += (_, evento) =>
        {
            Accepted = false;
            App!.RequestStop();
            evento.Handled = true;
        };

        Add(rotuloNombre, entradaNombre, rotuloCorreo, entradaCorreo, marcaFavorito, rotuloNotas, entradaNotas);
        for (int vuelta = 0; vuelta < entradasTelefonicas.Length; vuelta++)
        {
            Add(rotulosTelefono[vuelta], entradasTelefonicas[vuelta]);
        }

        AddButton(botonGuardar);
        AddButton(botonCancelar);
    }

    private bool IntentarArmarContacto(int id, out Contacto? contacto)
    {
        contacto = null;

        string nombreIngresado = entradaNombre.Text?.ToString()?.Trim() ?? "";
        string correoIngresado = entradaCorreo.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombreIngresado))
        {
            MessageBox.ErrorQuery(App!, "Validacion", "El nombre no puede estar vacio.", "Aceptar");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(correoIngresado) && !correoIngresado.Contains('@'))
        {
            MessageBox.ErrorQuery(App!, "Validacion", "El email debe contener @.", "Aceptar");
            return false;
        }

        string telefonosIngresados = PhoneTextTools.Normalizar(entradasTelefonicas
            .SelectMany(campo => PhoneTextTools.Separar(campo.Text?.ToString())));

        contacto = new Contacto
        {
            Id = id,
            Nombre = nombreIngresado,
            Telefonos = telefonosIngresados,
            Email = correoIngresado,
            Notas = entradaNotas.Text?.ToString() ?? "",
            Favorito = marcaFavorito.Value == CheckState.Checked
        };

        return true;
    }

     private static Label CrearRotulo(string texto, int x, int y)
    {
        return new Label
        {
            Text = texto,
            X = x,
            Y = y,
            Width = 11
        };
    }

    private static TextField CrearEntrada(Pos x, int y, string texto)
    {
        return new TextField
        {
            Text = texto,
            X = x,
            Y = y,
            Width = Dim.Fill(1)
        };
    }
}

public sealed class SqliteAgendaStore : IDisposable
{
    private readonly SqliteConnection conexionActiva;

    public string DatabasePath { get; }

    public SqliteAgendaStore(string databasePath)
    {
        DatabasePath = databasePath;

        SqliteConnectionStringBuilder datosConexion = new()
        {
            DataSource = databasePath
        };

        conexionActiva = new SqliteConnection(datosConexion.ConnectionString);
        conexionActiva.Open();

        EnsureSchema();
    }

    public IEnumerable<Contacto> GetAll()
    {
        return conexionActiva.GetAll<Contacto>();
    }

    public int Insert(Contacto contact)
    {
        Validate(contact);
        long id = conexionActiva.Insert(contact);
        return checked((int)id);
    }

    public void Update(Contacto contact)
    {
        Validate(contact);
        conexionActiva.Update(contact);
    }

    public void Delete(Contacto contact)
    {
        conexionActiva.Delete(contact);
    }

    public void Dispose()
    {
        conexionActiva.Dispose();
    }

    private void EnsureSchema()
    {
        conexionActiva.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Notas TEXT NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            );
            """);
    }

    private static void Validate(Contacto contact)
    {
        contact.Nombre = contact.Nombre?.Trim() ?? "";
        contact.Telefonos = PhoneTextTools.Normalizar(contact.Telefonos);
        contact.Email = contact.Email?.Trim() ?? "";
        contact.Notas ??= "";

        if (string.IsNullOrWhiteSpace(contact.Nombre))
        {
            throw new InvalidOperationException("El nombre no puede estar vacio.");
        }

        if (!string.IsNullOrWhiteSpace(contact.Email) && !contact.Email.Contains('@'))
        {
            throw new InvalidOperationException("El email debe contener @.");
        }
    }

}

public static class JsonAgendaIO
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IReadOnlyList<Contacto> Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("El archivo JSON no existe.", path);
        }

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            List<Contacto>? contactosLeidos = JsonSerializer.Deserialize<List<Contacto>>(json, OpcionesJson);

            return contactosLeidos?.Select(LimpiarContactoImportado).ToList() ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON con formato invalido: {ex.Message}", ex);
        }
    }

    public static void Write(string path, IEnumerable<Contacto> contacts)
    {
        string json = JsonSerializer.Serialize(contacts, OpcionesJson);
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static Contacto LimpiarContactoImportado(Contacto contact)
    {
        contact.Id = 0;
        contact.Nombre = contact.Nombre?.Trim() ?? "";
        contact.Telefonos = PhoneTextTools.Normalizar(contact.Telefonos);
        contact.Email = contact.Email?.Trim() ?? "";
        contact.Notas ??= "";
        return contact;
    }
}
public static class PhoneTextTools
{
    public static IEnumerable<string> Separar(string? textoTelefonos)
    {
        return (textoTelefonos ?? "")
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(numero => !string.IsNullOrWhiteSpace(numero));
    }

    public static string Normalizar(string? textoTelefonos)
    {
        return Normalizar(Separar(textoTelefonos));
    }

    public static string Normalizar(IEnumerable<string> telefonos)
    {
        return string.Join(", ", telefonos
            .Select(numero => numero.Trim())
            .Where(numero => !string.IsNullOrWhiteSpace(numero))
            .Take(5));
    }
}

[Table("Contactos")]
public sealed class Contacto
{
    [Key]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone()
    {
        return new Contacto
        {
            Id = Id,
            Nombre = Nombre,
            Telefonos = Telefonos,
            Email = Email,
            Notas = Notas,
            Favorito = Favorito
        };
    }
}

