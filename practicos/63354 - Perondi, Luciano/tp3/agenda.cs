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

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow());

// Ventana principal
public sealed class AgendaWindow : Runnable {

    public AgendaWindow() {
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
    }

    private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Nuevo contacto", null!, AbrirDialogo),
                    null!, // Separador
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ])
            ]
        };

        Button openButton = new() {
            Text = "_Abrir diálogo",
            X    = Pos.Center(),
            Y    = Pos.Center()
        };

        openButton.Accepting += (_, e) => {
            AbrirDialogo();
            e.Handled = true;
        };

        Add(menu, openButton);
    }

    private void AbrirDialogo() {
        ContactDialog dialog = new(new Contacto());
        App!.Run(dialog);
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }

    protected override bool OnKeyDown(Key key) {
        if (key == Key.Q.WithCtrl) {
            SolicitarSalir();
            return true;
        }

        return base.OnKeyDown(key);
    }
}

public sealed class ContactDialog : Dialog {
    public Contacto? resultado { get; private set; }
    public bool cancelado { get; private set; } = true;
    // campos de un contacto
    private readonly TextField campoNombre;
    private readonly TextField campoTelefonos;
    private readonly TextField campoEmail;
    private readonly TextField campoNotas;
    private readonly CheckBox  campoFavorito;
    public ContactDialog(Contacto contacto) {
        Title  = "Contacto";
        Width  = 60;
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
            X = 12, Y = 9};
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

            resultado = new Contacto {
                Id        = contacto.Id,
                Nombre    = nombre,
                Telefonos = (campoTelefonos.Text ?? "").Trim(),
                Email     = email,
                Notas     = (campoNotas.Text ?? "").Trim(),
                Favorito  = campoFavorito.Value == CheckState.Checked
            };
            cancelado = false;
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
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre    TEXT NOT NULL,
            Telefonos TEXT NOT NULL DEFAULT '',
            Email     TEXT NOT NULL DEFAULT '',
            Notas     TEXT NOT NULL DEFAULT '',
            Favorito  INTEGER NOT NULL DEFAULT 0
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
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

          public Contacto Clone() { //para no modificar el contacto original 
              return new Contacto {
                  Id        = this.Id,
                  Nombre    = this.Nombre,
                  Telefonos = this.Telefonos,
                  Email     = this.Email,
                  Notas     = this.Notas,
                  Favorito  = this.Favorito
              };
          }
}