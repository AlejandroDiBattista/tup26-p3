#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@*
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*


using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data.Common;
using Dapper.Contrib.Extensions;
using System.Text.Json; //para el JsonSerializer y JsonSerializerOptions
using System.Text.Encodings.Web; // para el encoder
using System.Collections.ObjectModel; //se actualizo la terminalGUIv2 y ahora el ListView no acepta List<string>, solo ObservableCollection<string>, 
                                    //la IA me dijo que solucione importando esta libreria

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada - modificado
string ruta = args.Length > 0 ? args[0] : "agenda.db";
SqliteAgendaStore almacen = new(ruta);

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(almacen));

// Ventana principal - modificado
public sealed class AgendaWindow : Runnable {

    private readonly SqliteAgendaStore almacen;
    private List<Contacto> contactosTodos = new();
    private List<Contacto> contactosFiltrados = new();
    private ListView<string> listaContactos = null!;
    private TextField campoBusqueda = null!;
    private Label panelDetalle = null!;
    private bool soloFavoritos = false;

    public AgendaWindow(SqliteAgendaStore almacen) {
        this.almacen = almacen;

        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;

        contactosTodos = almacen.ObtenerTodos();
        contactosFiltrados = contactosTodos;

        BuildLayout();
    }

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Nuevo contacto", null!, AbrirDialogo),
                    new MenuItem("_Editar", null!, EditarSeleccionado),
                    new MenuItem("E_liminar", null!, EliminarSeleccionado),
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Ver", [
                    new MenuItem("Solo _favoritos", null!, AlternarFavoritos)
                ])
            ]
        };

        Label etiquetaBuscar = new() { Text = "Buscar:", X = 0, Y = 1 };
        campoBusqueda = new() {
            X = Pos.Right(etiquetaBuscar) + 1,
            Y = 1,
            Width = Dim.Fill()
        };

        listaContactos = new() {
            X = 0,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 7
        };

        Label tituloDetalle = new() {
            Text = "── Detalle ──",
            X = 0,
            Y = Pos.Bottom(listaContactos)
        };
        panelDetalle = new() {
            X = 0,
            Y = Pos.Bottom(tituloDetalle),
            Width = Dim.Fill(),
            Height = 6
        };

        Add(menu, etiquetaBuscar, campoBusqueda, listaContactos, tituloDetalle, panelDetalle);
        campoBusqueda.TextChanged += (_, _) => RefrescarLista();
        listaContactos.ValueChanged += (_, _) => RefrescarDetalle();
        listaContactos.Activated += (_, _) => EditarSeleccionado();
        RefrescarLista(); 
    }

    private void RefrescarLista() {
        string filtro = (campoBusqueda.Text ?? "").Trim();

        contactosFiltrados = contactosTodos
            .Where(c => filtro == ""
                     || c.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                     || c.Telefonos.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                     || c.Email.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            .Where(c => !soloFavoritos || c.Favorito)
            .ToList();

        var lineas = contactosFiltrados
            .Select(c => c.Nombre)
            .ToList();
        listaContactos.SetSource(new ObservableCollection<string>(lineas));
    }
    private void RefrescarDetalle() {
        Contacto? c = ContactoSeleccionado();
        if (c == null) {
            panelDetalle.Text = "";
            return;
        }
        string fav = c.Favorito ? "Sí" : "No";
        panelDetalle.Text =
            $"Nombre: {c.Nombre}\n" +
            $"Teléfonos: {c.Telefonos}\n" +
            $"Email: {c.Email}\n" +
            $"Notas: {c.Notas}\n" +
            $"Favorito: {fav}";
    }
    private Contacto? ContactoSeleccionado() {
        string? nombre = listaContactos.SelectedItem;
        if (nombre == null) return null;
        return contactosFiltrados.FirstOrDefault(c => c.Nombre == nombre);
    }

    private void AlternarFavoritos() {
        soloFavoritos = !soloFavoritos;
        RefrescarLista();
    }

    private void AbrirDialogo() {
        ContactDialog dialog = new(new Contacto());
        App!.Run(dialog);

        if (dialog.Cancelado || dialog.Resultado == null) return;

        almacen.Agregar(dialog.Resultado);
        contactosTodos = almacen.ObtenerTodos();
        RefrescarLista();
    }

    private void EditarSeleccionado() {
        Contacto? c = ContactoSeleccionado();
        if (c == null) return;

        ContactDialog dialog = new(c.Clone());
        App!.Run(dialog);

        if (dialog.Cancelado || dialog.Resultado == null) return;

        almacen.Actualizar(dialog.Resultado);
        contactosTodos = almacen.ObtenerTodos();
        RefrescarLista();
    }

    private void EliminarSeleccionado() {
        Contacto? c = ContactoSeleccionado();
        if (c == null) return;

        int? r = MessageBox.Query(App!, "Confirmar", $"¿Eliminar a {c.Nombre}?", "Sí", "No");
        if (r != 0) return;

        almacen.Eliminar(c);
        contactosTodos = almacen.ObtenerTodos();
        RefrescarLista();
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) { SolicitarSalir();        return true; }
        if (key == Key.N.WithCtrl) { AbrirDialogo();          return true; }
        if (key == Key.F2)         { AbrirDialogo();          return true; }
        if (key == Key.F3)         { EditarSeleccionado();    return true; }
        if (key == Key.D.WithCtrl) { EliminarSeleccionado();  return true; }
        if (key == Key.DeleteChar) { EliminarSeleccionado();  return true; }
        if (key == Key.F4)         { campoBusqueda.SetFocus(); return true; }
        if (key == Key.F5)         { AlternarFavoritos();     return true; }
        if (key == Key.I.WithCtrl) { ImportarJson(); return true; }
        if (key == Key.E.WithCtrl) { ExportarJson(); return true; }
        return base.OnKeyDown(key);
    }

    private void ExportarJson() {
        JsonAgendaIO io = new();
        try {
            io.exportar("contactos.json", contactosTodos);
            MessageBox.Query(App!, "Exportar", "Contactos exportados a contactos.json", "Aceptar");
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al exportar", ex.Message, "Aceptar");
        }
    }
    private void ImportarJson() {
        JsonAgendaIO io = new();
        try {
            List<Contacto> importados = io.importar("contactos.json");

            int r = MessageBox.Query(App!, "Importar",
                $"Se encontraron {importados.Count} contactos. ¿Importar?", "Sí", "No") ?? 1;
            if (r != 0) return;

            foreach (Contacto c in importados) {
                c.Id = 0;                 // que la base asigne un Id nuevo
                almacen.Agregar(c);       // base
            }
            contactosTodos = almacen.ObtenerTodos();   // memoria
            RefrescarLista();                          // vista
        } catch (Exception ex) {
            MessageBox.ErrorQuery(App!, "Error al importar", ex.Message, "Aceptar");
        }
    }

}

public sealed class ContactDialog : Dialog {
    public Contacto? Resultado { get; private set; }
    public bool Cancelado { get; private set; } = true;
    // campos de un contacto
    private readonly TextField campoNombre;
    private readonly TextField campoTelefonos;
    private readonly TextField campoEmail;
    private readonly TextField campoNotas;
    private readonly CheckBox  campoFavorito;
    public ContactDialog(Contacto contacto) {
        Title = "Contacto";
        Width = 60;
        Height = 15;

        Label etiquetaNombre = new() { Text = "Nombre:", X = 1, Y = 1 };
        Label etiquetaTelefonos = new() { Text = "Teléfonos:", X = 1, Y = 3 };
        Label etiquetaEmail = new() { Text = "Email:", X = 1, Y = 5 };
        Label etiquetaNotas = new() { Text = "Notas:", X = 1, Y = 7 };

        campoNombre = new() {
            Text = contacto.Nombre,
            X = 12, Y = 1,
            Width = Dim.Fill() - 2};

        campoTelefonos = new() {
            Text = contacto.Telefonos,
            X = 12, Y = 3,
            Width = Dim.Fill() - 2};

        campoEmail = new() {
            Text = contacto.Email,
            X = 12, Y = 5,
            Width = Dim.Fill() - 2};

        campoNotas = new() {
            Text = contacto.Notas,
            X = 12, Y = 7,
            Width = Dim.Fill() - 2};

        campoFavorito = new() {
            Text = "Favorito",
            X = 12, Y = 8};
        campoFavorito.Value = contacto.Favorito ? CheckState.Checked : CheckState.UnChecked;
        
        Add(etiquetaNombre, etiquetaTelefonos, etiquetaEmail, etiquetaNotas, campoNombre, campoTelefonos, campoEmail, campoNotas, campoFavorito);

        Button botonAceptar = new() {Text = "_Aceptar", IsDefault = true};
        Button botonCancelar = new() {Text = "_Cancelar"};
        botonAceptar.Accepting += (_, e) => {
            e.Handled = true;

            string nombre = (campoNombre.Text ?? "").Trim();
            string email  = (campoEmail.Text  ?? "").Trim();

            if (nombre == "") {
                MessageBox.Query(App!, "Falta el nombre", "El nombre no puede quedar vacío.", "Aceptar");
                return;
            }

            if (email != "" && !email.Contains("@")) {
                MessageBox.Query(App!, "Email inválido", "El email debe contener una @.", "Aceptar");
                return;
            }

            Resultado = new Contacto {
                Id = contacto.Id,
                Nombre = nombre,
                Telefonos = (campoTelefonos.Text ?? "").Trim(),
                Email = email,
                Notas = (campoNotas.Text ?? "").Trim(),
                Favorito = campoFavorito.Value == CheckState.Checked
            };
            Cancelado = false; 
            App!.RequestStop();
        };

        botonCancelar.Accepting += (_, e) => {
            e.Handled = true;
            App!.RequestStop();
        };

        AddButton(botonAceptar);
        AddButton(botonCancelar);
    }
}

public class SqliteAgendaStore {
    private const string CrearTablaSql = @"
        CREATE TABLE IF NOT EXISTS Contactos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Telefonos TEXT NOT NULL DEFAULT '',
            Email TEXT NOT NULL DEFAULT '',
            Notas TEXT NOT NULL DEFAULT '',
            Favorito INTEGER NOT NULL DEFAULT 0
        ); ";

    private readonly SqliteConnection conexion;
    public SqliteAgendaStore(string rutaArchivo) {
        conexion = new SqliteConnection($"Data Source={rutaArchivo}");
        conexion.Open();
        conexion.Execute(CrearTablaSql);
    }
    public List<Contacto> ObtenerTodos() {
        return conexion.GetAll<Contacto>().ToList();
    }
    public void Agregar(Contacto c) {
        conexion.Insert(c);
    }
    public void Actualizar(Contacto c) {
        conexion.Update(c);
    }
    public void Eliminar(Contacto c) {
        conexion.Delete(c);
    }
}
public class JsonAgendaIO {
    public List<Contacto> importar(string ruta) {
        string texto = File.ReadAllText(ruta);
        var lista = JsonSerializer.Deserialize<List<Contacto>>(texto);
        return lista ?? new List<Contacto>();
    }
    public void exportar(string ruta, List<Contacto> contactos) {
        var opciones = new JsonSerializerOptions {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        string texto = JsonSerializer.Serialize(contactos, opciones);
        File.WriteAllText(ruta, texto);
    }
}

[Table("Contactos")]
public sealed class Contacto { //clase sealed porque asi daba la clase contactos de ejemplo en el enunciado, intuyo que es para que no sea heredada 
    [Key] public int Id { get; set; }
          public string Nombre { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email { get; set; } = "";
          public string Notas { get; set; } = "";
          public bool Favorito { get; set; }

          public Contacto Clone() { //para no modificar el contacto original 
              return new Contacto {
                  Id = this.Id,
                  Nombre = this.Nombre,
                  Telefonos = this.Telefonos,
                  Email = this.Email,
                  Notas = this.Notas,
                  Favorito = this.Favorito
              };
          }
}