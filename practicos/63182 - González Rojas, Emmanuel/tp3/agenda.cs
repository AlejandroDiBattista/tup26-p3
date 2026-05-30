#!/usr/bin/dotnet run

#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property PublishAot=false
#:property PublishTrimmed=false
#:property TrimMode=copyused
#:property EnableTrimAnalyzer=false

#:package Terminal.Gui@2.0.0-v2-develop.400
#:package Microsoft.Data.Sqlite@9.0.0
#:package Dapper@2.1.35
#:package Dapper.Contrib@2.0.78

// ==========================================================
// USING
// ==========================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

using System.Text.Json;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Terminal.Gui;

// ==========================================================
// PUNTO DE ENTRADA
// ==========================================================

SqlMapper.AddTypeHandler(new BooleanTypeHandler());

string archivoBaseDatos = args.Length > 0 ? args[0] : "agenda.db";

try
{
    SqliteAgendaStore store = new SqliteAgendaStore(archivoBaseDatos);

    Application.Init();

    AgendaWindow ventana = new AgendaWindow(store);
    Application.Run(ventana);
    Application.Shutdown();
}
catch (Exception ex)
{
    Console.WriteLine("Error al iniciar la aplicación:");
    Console.WriteLine(ex.Message);
}

// ==========================================================
// VENTANA PRINCIPAL
// ==========================================================

public sealed class AgendaWindow : Window
{
    private readonly SqliteAgendaStore store;
    private List<Contacto> contactos = new();
    private List<Contacto> contactosFiltrados = new();

    private readonly TextField campoBusqueda;
    private readonly ListView listaContactos;
    private readonly TextView detalleContacto;
    private readonly StatusBar barraEstado;
    private readonly Label mensajeEstado;
    private bool soloFavoritos = false;

    public AgendaWindow(SqliteAgendaStore store)
    {
        this.store = store;
        Title = "Agenda TUI (v2)";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        contactos = store.ObtenerTodos();

        // MENÚ 
        MenuBar menu = new MenuBar
        {
            Menus = new MenuBarItem[] {
                new MenuBarItem {
                    Title = "_Archivo",
                    Children = new MenuItem[] {
                        new MenuItem { Title = "_Importar JSON", Action = () => ImportarJson() },
                        new MenuItem { Title = "_Exportar JSON", Action = () => ExportarJson() },
                        new MenuItem { Title = "_Salir", Action = () => Salir() }
                    }
                },
                new MenuBarItem {
                    Title = "_Contactos",
                    Children = new MenuItem[] {
                        new MenuItem { Title = "_Nuevo", Action = () => NuevoContacto() },
                        new MenuItem { Title = "_Editar", Action = () => EditarContacto() },
                        new MenuItem { Title = "_Eliminar", Action = () => EliminarContacto() }
                    }
                },
                new MenuBarItem {
                    Title = "_Ver",
                    Children = new MenuItem[] {
                        new MenuItem { Title = "_Solo favoritos", Action = () => ToggleFavoritos() }
                    }
                },
                new MenuBarItem {
                    Title = "_Ayuda",
                    Children = new MenuItem[] {
                        new MenuItem { Title = "_Acerca de", Action = () => MostrarAcercaDe() }
                    }
                }
            }
        };
        Add(menu);

        // BÚSQUEDA (Solucionado warning de Dim nulo usando Dim.Percent)
        campoBusqueda = new TextField()
        {
            Text = "",
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            CanFocus = true
        };
        campoBusqueda.TextChanged += (_, _) => AplicarFiltros();
        Add(campoBusqueda);

        // LISTA
        listaContactos = new ListView()
        {
            X = 0,
            Y = 3,
            Width = 30,
            Height = Dim.Fill()
        };
        listaContactos.SelectedItemChanged += (_, _) => MostrarDetalle();
        listaContactos.OpenSelectedItem += (_, _) => EditarContacto();
        Add(listaContactos);

        // DETALLE
        detalleContacto = new TextView()
        {
            X = 31,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true
        };
        Add(detalleContacto);

        mensajeEstado = new Label()
        {
            Text = "Listo",
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill()
        };
        Add(mensajeEstado);

        barraEstado = new StatusBar()
        {
            Y = Pos.AnchorEnd(1)
        };
        barraEstado.Add(new Shortcut { Key = Key.F2, Title = "Nuevo", Action = NuevoContacto });
        barraEstado.Add(new Shortcut { Key = Key.F3, Title = "Editar", Action = EditarContacto });
        barraEstado.Add(new Shortcut { Key = Key.N.WithCtrl, Title = "Nuevo", Action = NuevoContacto });
        barraEstado.Add(new Shortcut { Key = Key.Delete, Title = "Eliminar", Action = EliminarContacto });
        barraEstado.Add(new Shortcut { Key = Key.D.WithCtrl, Title = "Eliminar", Action = EliminarContacto });
        barraEstado.Add(new Shortcut { Key = Key.I.WithCtrl, Title = "Importar", Action = ImportarJson });
        barraEstado.Add(new Shortcut { Key = Key.E.WithCtrl, Title = "Exportar", Action = ExportarJson });
        barraEstado.Add(new Shortcut { Key = Key.F4, Title = "Buscar", Action = () => campoBusqueda.SetFocus() });
        barraEstado.Add(new Shortcut { Key = Key.Q.WithCtrl, Title = "Salir", Action = Salir });
        Add(barraEstado);

        AplicarFiltros();
        SetEstado("Listo");

        Application.KeyDown += ManejarAtajosGlobales;
    }

    // Solucionado warning de nulabilidad cambiando object por object?
    private void ManejarAtajosGlobales(object? sender, Key keyEvent)
    {
        if (keyEvent == Key.F2) { NuevoContacto(); keyEvent.Handled = true; }
        else if (keyEvent == Key.N.WithCtrl) { NuevoContacto(); keyEvent.Handled = true; }
        else if (keyEvent == Key.F3) { EditarContacto(); keyEvent.Handled = true; }
        else if (keyEvent == Key.Enter && listaContactos.HasFocus) { EditarContacto(); keyEvent.Handled = true; }
        else if (keyEvent == Key.Delete) { EliminarContacto(); keyEvent.Handled = true; }
        else if (keyEvent == Key.D.WithCtrl) { EliminarContacto(); keyEvent.Handled = true; }
        else if (keyEvent == Key.I.WithCtrl) { ImportarJson(); keyEvent.Handled = true; }
        else if (keyEvent == Key.E.WithCtrl) { ExportarJson(); keyEvent.Handled = true; }
        else if (keyEvent == Key.F4) { campoBusqueda.SetFocus(); keyEvent.Handled = true; }
        else if (keyEvent == Key.Q.WithCtrl) { Salir(); keyEvent.Handled = true; }
    }

    private void AplicarFiltros()
    {
        string textoBusqueda = campoBusqueda.Text?.ToString()?.ToLower() ?? "";

        contactosFiltrados = contactos
            .Where(c => (string.IsNullOrEmpty(textoBusqueda) || 
                         c.Nombre.ToLower().Contains(textoBusqueda) ||
                         c.Telefonos.ToLower().Contains(textoBusqueda) ||
                         c.Email.ToLower().Contains(textoBusqueda)) &&
                        (!soloFavoritos || c.Favorito))
            .ToList();

        var nombres = new ObservableCollection<string>(
            contactosFiltrados.Select(c => c.Favorito ? "★ " + c.Nombre : c.Nombre)
        );

        listaContactos.SetSource<string>(nombres);
        
        if (contactosFiltrados.Count > 0 && listaContactos.SelectedItem < 0)
        {
            listaContactos.SelectedItem = 0;
        }

        MostrarDetalle();
    }

    private void MostrarDetalle()
    {
        if (contactosFiltrados.Count == 0)
        {
            detalleContacto.Text = "";
            return;
        }

        int indice = listaContactos.SelectedItem;
        if (indice < 0 || indice >= contactosFiltrados.Count)
        {
            detalleContacto.Text = "";
            return;
        }

        Contacto c = contactosFiltrados[indice];
        detalleContacto.Text = $"""
        Nombre:
        {c.Nombre}

        Teléfonos:
        {c.Telefonos}

        Email:
        {c.Email}

        Favorito:
        {(c.Favorito ? "Sí" : "No")}

        Notas:
        {c.Notas}
        """;
    }

    private void NuevoContacto()
    {
        ContactDialog dialogo = new ContactDialog(new Contacto(), true);
        Application.Run(dialogo);

        if (!dialogo.Guardado) return;

        dialogo.Contacto.Id = 0;
        store.Insertar(dialogo.Contacto);
        contactos = store.ObtenerTodos();
        AplicarFiltros();

        MessageBox.Query("Éxito", "Contacto agregado", "OK");
        SetEstado("Contacto agregado");
    }

    private void EditarContacto()
    {
        if (contactosFiltrados.Count == 0) return;

        int indice = listaContactos.SelectedItem;
        if (indice < 0 || indice >= contactosFiltrados.Count) return;

        Contacto original = contactosFiltrados[indice];
        Contacto copia = original.Clone();

        ContactDialog dialogo = new ContactDialog(copia);
        Application.Run(dialogo);

        if (!dialogo.Guardado) return;

        store.Actualizar(dialogo.Contacto);
        contactos = store.ObtenerTodos();
        AplicarFiltros();
        MessageBox.Query("Actualizado", "Contacto modificado", "OK");
        SetEstado("Contacto modificado");
    }

    private void EliminarContacto()
    {
        if (contactosFiltrados.Count == 0) return;

        int indice = listaContactos.SelectedItem;
        if (indice < 0 || indice >= contactosFiltrados.Count) return;

        Contacto contacto = contactosFiltrados[indice];
        int respuesta = MessageBox.Query("Confirmar", $"¿Eliminar a {contacto.Nombre}?", "Sí", "No");

        if (respuesta != 0) return;

        store.Eliminar(contacto);
        contactos = store.ObtenerTodos();
        AplicarFiltros();
        SetEstado($"Contacto eliminado: {contacto.Nombre}");
    }

    private void ImportarJson()
    {
        OpenDialog dialogo = new OpenDialog { Title = "Importar", Text = "Seleccionar JSON" };
        Application.Run(dialogo);

        if (dialogo.Canceled) return;

        try
        {
            string ruta = dialogo.FilePaths.FirstOrDefault() ?? "";
            
            if (string.IsNullOrEmpty(ruta)) 
                return;

            List<Contacto> importados = JsonAgendaIO.Importar(ruta);

            int respuesta = MessageBox.Query("Importar", $"Se agregarán {importados.Count} contactos", "Aceptar", "Cancelar");
            if (respuesta != 0) return;

            foreach (Contacto c in importados)
            {
                c.Id = 0;
                store.Insertar(c);
            }

            contactos = store.ObtenerTodos();
            AplicarFiltros();
            SetEstado($"Importados {importados.Count} contactos");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void ExportarJson()
    {
        SaveDialog dialogo = new SaveDialog { Title = "Exportar", Text = "Guardar JSON" };
        Application.Run(dialogo);

        if (dialogo.Canceled) return;

        try
        {
            string ruta = dialogo.Path?.ToString() ?? "";
            if (string.IsNullOrEmpty(ruta)) return;

            JsonAgendaIO.Exportar(ruta, contactos);
            MessageBox.Query("Exportado", "Archivo JSON generado", "OK");
            SetEstado($"Exportado {Path.GetFileName(ruta)}");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", ex.Message, "OK");
        }
    }

    private void ToggleFavoritos()
    {
        soloFavoritos = !soloFavoritos;
        AplicarFiltros();
        SetEstado(soloFavoritos ? "Mostrando solo favoritos" : "Mostrando todos los contactos");
    }

    private void MostrarAcercaDe()
    {
        MessageBox.Query("Acerca de", "Agenda TUI - TP3\n.NET 10 + SQLite (v2)", "OK");
    }

    private void Salir() => Application.RequestStop();

    private void SetEstado(string mensaje)
    {
        mensajeEstado.Text = mensaje;
    }
}

// ==========================================================
// DIALOG CONTACTO
// ==========================================================

public sealed class ContactDialog : Dialog
{
    public Contacto Contacto { get; private set; }
    public bool Guardado { get; private set; }

    private readonly TextField txtNombre;
    private readonly TextField[] txtTelefonos = new TextField[5];
    private readonly TextField txtEmail;
    private readonly TextView txtNotas;
    private readonly CheckBox chkFavorito;
    private readonly bool esNuevo;

    public ContactDialog(Contacto contacto, bool esNuevo = false)
    {
        Contacto = contacto;
        this.esNuevo = esNuevo;
        Title = esNuevo ? "Nuevo contacto" : "Contacto";
        Width = 70;
        Height = 23;

        // Nombre
        Add(new Label() { Text = "Nombre:", X = 1, Y = 1 });
        txtNombre = new TextField() { Text = contacto.Nombre, X = 15, Y = 1, Width = 40 };
        Add(txtNombre);

        // Teléfonos
        Add(new Label() { Text = "Teléfonos:", X = 1, Y = 3 });
        string[] telefonos = contacto.Telefonos.Split(',');

        for (int i = 0; i < 5; i++)
        {
            txtTelefonos[i] = new TextField()
            {
                Text = i < telefonos.Length ? telefonos[i].Trim() : "",
                X = 15,
                Y = 3 + i,
                Width = 40
            };
            Add(new Label() { Text = $"Tel {i + 1}:", X = 1, Y = 3 + i });
            Add(txtTelefonos[i]);
        }

        // Email
        Add(new Label() { Text = "Email:", X = 1, Y = 9 });
        txtEmail = new TextField()
        {
            Text = contacto.Email,
            X = 15,
            Y = 9,
            Width = 40
        };
        Add(txtEmail);
        Add(new Label()
        {
            Text = "Presione F6 para insertar @",
            X = 15,
            Y = 10
        });
        txtEmail.KeyDown += (sender, e) =>
        {
            if (e == Key.F6)
            {
                txtEmail.Text = (txtEmail.Text?.ToString() ?? "") + "@";
            }
        };

        // Favorito
        chkFavorito = new CheckBox()
        {
            Text = "Favorito",
            X = 15,
            Y = 11,
            CheckedState = contacto.Favorito ? CheckState.Checked : CheckState.UnChecked
        };
        Add(chkFavorito);

        // Notas
        Add(new Label() { Text = "Notas:", X = 1, Y = 13 });
        txtNotas = new TextView() { X = 15, Y = 13, Width = 40, Height = 4, Text = contacto.Notas };
        Add(txtNotas);

        // Botones
        Button botonGuardar = new Button() { Text = "Guardar" };
        botonGuardar.Accept += (_, _) => Guardar();
        AddButton(botonGuardar);

        Button botonCancelar = new Button() { Text = "Cancelar" };
        botonCancelar.Accept += (_, _) => Application.RequestStop();
        AddButton(botonCancelar);

        txtNombre.SetFocus();

    //arroba
        txtEmail.KeyDown += (sender, keyEvent) =>
        {
            if (keyEvent.IsCtrl && keyEvent.KeyCode == KeyCode.A)
            {
                InsertarArrobaManualmente();
                keyEvent.Handled = true;
                return;
            }

            bool altGrQ = (keyEvent.IsAlt && keyEvent.IsCtrl && keyEvent.KeyCode == KeyCode.Q) || (keyEvent.IsAlt && keyEvent.KeyCode == KeyCode.Q);
            bool altGr2 = (keyEvent.IsAlt && keyEvent.IsCtrl && keyEvent.KeyCode == KeyCode.D2) || (keyEvent.IsAlt && keyEvent.KeyCode == KeyCode.D2);

            if (altGrQ || altGr2)
            {
                InsertarArrobaManualmente();
                keyEvent.Handled = true;
            }
        };
    }

    private void InsertarArrobaManualmente()
    {
        string textoActual = txtEmail.Text?.ToString() ?? "";
        int pos = txtEmail.CursorPosition;
        txtEmail.Text = textoActual.Insert(pos, "@");
        txtEmail.CursorPosition = pos + 1;
    }

    private void Guardar()
    {
        string nombre = txtNombre.Text?.ToString()?.Trim() ?? "";
        string email = txtEmail.Text?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.ErrorQuery("Error", "El nombre es obligatorio", "OK");
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
        {
            MessageBox.ErrorQuery("Error", "El email debe contener @", "OK");
            return;
        }

        Contacto.Nombre = nombre;
        List<string> telefonosGuardados = txtTelefonos
            .Select(t => t.Text?.ToString()?.Trim() ?? "")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        Contacto.Telefonos = string.Join(", ", telefonosGuardados);
        Contacto.Email = email;
        Contacto.Notas = txtNotas.Text?.ToString() ?? "";
        Contacto.Favorito = chkFavorito.CheckedState == CheckState.Checked;

        if (esNuevo)
        {
            Contacto.Id = 0;
        }

        Guardado = true;
        Application.RequestStop();
    }
}

// ==========================================================
// SQLITE AGENDA STORE
// ==========================================================

public sealed class SqliteAgendaStore
{
    private readonly string connectionString;

    public SqliteAgendaStore(string archivo)
    {
        connectionString = $"Data Source={archivo}";
        CrearTabla();
    }

    private void CrearTabla()
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        string sql = """
        CREATE TABLE IF NOT EXISTS Contactos(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Telefonos TEXT,
            Email TEXT,
            Notas TEXT,
            Favorito INTEGER NOT NULL
        );
        """;
        conexion.Execute(sql);
    }

    public List<Contacto> ObtenerTodos()
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        string sql = """
        SELECT Id, Nombre, Telefonos, Email, Notas, (Favorito = 1) AS Favorito 
        FROM Contactos 
        ORDER BY Nombre
        """;
        return conexion.Query<Contacto>(sql).ToList();
    }

    public void Insertar(Contacto contacto)
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        conexion.Insert(contacto);
    }

    public void Actualizar(Contacto contacto)
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        conexion.Update(contacto);
    }

    public void Eliminar(Contacto contacto)
    {
        using SqliteConnection conexion = new SqliteConnection(connectionString);
        conexion.Open();
        conexion.Delete(contacto);
    }
}

// ==========================================================
// SOPORTE JSON
// ==========================================================

public static class JsonAgendaIO
{
    public static void Exportar(string ruta, List<Contacto> contactos)
    {
        JsonSerializerOptions opciones = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(contactos, opciones);
        File.WriteAllText(ruta, json);
    }

    public static List<Contacto> Importar(string ruta)
    {
        if (!File.Exists(ruta)) throw new Exception("El archivo JSON no existe");
        string json = File.ReadAllText(ruta);
        return JsonSerializer.Deserialize<List<Contacto>>(json) ?? new List<Contacto>();
    }
}

// ==========================================================
// MODELO
// ==========================================================

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

    public Contacto Clone() => new Contacto()
    {
        Id = this.Id,
        Nombre = this.Nombre,
        Telefonos = this.Telefonos,
        Email = this.Email,
        Notas = this.Notas,
        Favorito = this.Favorito
    };
}

// ==========================================================
// SOPORTE INTERNO: BooleanTypeHandler
// ==========================================================

internal class BooleanTypeHandler : SqlMapper.TypeHandler<bool>
{
    public override void SetValue(System.Data.IDbDataParameter parameter, bool value)
    {
        parameter.Value = value ? 1 : 0;
    }

    public override bool Parse(object value)
    {
        if (value is long l) return l == 1;
        if (value is int i) return i == 1;
        return Convert.ToBoolean(value);
    }
}