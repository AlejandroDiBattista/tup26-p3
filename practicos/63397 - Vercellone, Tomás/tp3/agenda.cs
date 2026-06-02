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
using System.Text;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;


string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store;
try
{
    store = new SqliteAgendaStore(dbPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine("No se pudo abrir la base de datos: " + ex.Message);
    return 1;
}

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));
return 0;


public sealed class AgendaWindow : Runnable
{
    private readonly SqliteAgendaStore store;

    private List<Contacto> contactos = new List<Contacto>();
    private List<Contacto> contactosFiltrados = new List<Contacto>();
    private bool mostrandoSoloFavoritos = false;

   
    private ListView  listaView    = null!;
    private TextField campoBusqueda = null!;
    private Label lblNombre    = null!;
    private Label lblTelefonos = null!;
    private Label lblEmail     = null!;
    private Label lblNotas     = null!;
    private Label lblFavorito  = null!;
    private Label barraEstado  = null!;

    public AgendaWindow(SqliteAgendaStore storeRecibido)
    {
        store = storeRecibido;
        Title  = "AgendaT - TP3";
        Width  = Dim.Fill();
        Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;

        ConstruirLayout();
        CargarContactos();
    }

    private void ConstruirLayout()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarDesdeJSON),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarAJSON),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", PedirSalida)
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo",    "F2",  AbrirNuevoContacto),
                    new MenuItem("_Editar",   "F3",  AbrirEdicionContacto),
                    new MenuItem("_Eliminar", "Del", EliminarContactoSeleccionado)
                ]),
                new MenuBarItem("_Ver",
                [
                    new MenuItem("_Solo favoritos", "", ToggleFavoritos)
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", null!, MostrarAcercaDe)
                ])
            ]
        };

        var etiquetaBuscar = new Label { Text = "Buscar: ", X = 1, Y = 1 };

        campoBusqueda = new TextField
        {
            X = Pos.Right(etiquetaBuscar), Y = 1, Width = Dim.Percent(40)
        };
        campoBusqueda.TextChanging += (sender, args) => AplicarFiltro();

        var panelLista = new FrameView
        {
            Title = "Contactos", X = 1, Y = 3,
            Width = Dim.Percent(40), Height = Dim.Fill(2)
        };

        listaView = new ListView
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
        };

       
        listaView.Accepting += (s, e) => { MostrarDetalleContacto(); };

        panelLista.Add(listaView);

        var panelDetalle = new FrameView
        {
            Title = "Detalle",
            X = Pos.Right(panelLista) + 1, Y = 3,
            Width = Dim.Fill(1), Height = Dim.Fill(2)
        };

        lblNombre    = CrearLabel(0);
        lblTelefonos = CrearLabel(2);
        lblEmail     = CrearLabel(4);
        lblFavorito  = CrearLabel(6);
        lblNotas     = CrearLabel(8);

        panelDetalle.Add(lblNombre, lblTelefonos, lblEmail, lblFavorito, lblNotas);

        barraEstado = new Label
        {
            Text = "Bienvenido a AgendaT.",
            X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = 1
        };

        Add(menu, etiquetaBuscar, campoBusqueda, panelLista, panelDetalle, barraEstado);
    }

    private Label CrearLabel(int posY) => new Label
    {
        X = 1, Y = posY, Width = Dim.Fill(), Height = 1, Text = ""
    };

    private void CargarContactos()
    {
        contactos = store.ObtenerTodos().ToList();
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string textoBusqueda = "";
        if (campoBusqueda?.Text != null)
            textoBusqueda = campoBusqueda.Text.ToLowerInvariant();

        var resultado = contactos.AsEnumerable();

        if (mostrandoSoloFavoritos)
            resultado = resultado.Where(c => c.Favorito);

        if (!string.IsNullOrEmpty(textoBusqueda))
        {
            resultado = resultado.Where(c =>
                c.Nombre.ToLowerInvariant().Contains(textoBusqueda)    ||
                c.Telefonos.ToLowerInvariant().Contains(textoBusqueda) ||
                c.Email.ToLowerInvariant().Contains(textoBusqueda)
            );
        }

        contactosFiltrados = resultado.ToList();

        var items = new ObservableCollection<string>(
            contactosFiltrados.Select(c => (c.Favorito ? "★ " : "  ") + c.Nombre)
        );
        listaView.SetSource(items);

        MostrarDetalleContacto();
    }

    private void MostrarDetalleContacto()
    {
        var c = ObtenerContactoSeleccionado();
        if (c == null)
        {
            lblNombre.Text = lblTelefonos.Text = lblEmail.Text =
            lblFavorito.Text = lblNotas.Text = "";
            return;
        }
        lblNombre.Text    = "Nombre:    " + c.Nombre;
        lblTelefonos.Text = "Teléfonos: " + c.Telefonos;
        lblEmail.Text     = "Email:     " + c.Email;
        lblFavorito.Text  = "Favorito:  " + (c.Favorito ? "★ Sí" : "No");
        lblNotas.Text     = "Notas:     " + c.Notas;
    }

   
    private Contacto? ObtenerContactoSeleccionado()
    {
        int indice = listaView.SelectedItem ?? -1;
        if (indice < 0 || indice >= contactosFiltrados.Count)
            return null;
        return contactosFiltrados[indice];
    }

    private void ActualizarEstado(string msg) => barraEstado.Text = msg;

    

    private void AbrirNuevoContacto()
    {
        var dlg = new ContactDialog(new Contacto(), "Nuevo contacto");
        App!.Run(dlg);
        if (dlg.Resultado == null) return;

        try
        {
            store.Insertar(dlg.Resultado);
            contactos.Insert(0, dlg.Resultado);
            AplicarFiltro();
            ActualizarEstado("Contacto '" + dlg.Resultado.Nombre + "' guardado.");
        }
        catch (Exception ex) { MostrarError("No se pudo guardar: " + ex.Message); }
    }

    private void AbrirEdicionContacto()
    {
        var c = ObtenerContactoSeleccionado();
        if (c == null) { ActualizarEstado("Seleccioná un contacto para editar."); return; }

        var dlg = new ContactDialog(c.Clone(), "Editar contacto");
        App!.Run(dlg);
        if (dlg.Resultado == null) return;

        try
        {
            store.Actualizar(dlg.Resultado);
            int pos = contactos.FindIndex(x => x.Id == dlg.Resultado.Id);
            if (pos >= 0) contactos[pos] = dlg.Resultado;
            AplicarFiltro();
            ActualizarEstado("Contacto '" + dlg.Resultado.Nombre + "' actualizado.");
        }
        catch (Exception ex) { MostrarError("No se pudo actualizar: " + ex.Message); }
    }

    private void EliminarContactoSeleccionado()
    {
        var c = ObtenerContactoSeleccionado();
        if (c == null) { ActualizarEstado("Seleccioná un contacto para eliminar."); return; }

     
        bool confirmado = false;
        var dlgConfirm = new Dialog
        {
            Title = "Confirmar eliminacion",
            Width = 55, Height = 7
        };
        dlgConfirm.Add(new Label
        {
            Text = "¿Seguro que querés eliminar a '" + c.Nombre + "'?",
            X = 1, Y = 1, Width = Dim.Fill()
        });
        var btnSi = new Button { Text = "_Sí", IsDefault = true };
        btnSi.Accepting += (s, e) => { confirmado = true; App!.RequestStop(); e.Handled = true; };
        var btnNo = new Button { Text = "_No" };
        btnNo.Accepting += (s, e) => { App!.RequestStop(); e.Handled = true; };
        dlgConfirm.AddButton(btnSi);
        dlgConfirm.AddButton(btnNo);
        App!.Run(dlgConfirm);

        if (!confirmado) return;

        try
        {
            store.Eliminar(c.Id);
            contactos.RemoveAll(x => x.Id == c.Id);
            AplicarFiltro();
            ActualizarEstado("Contacto '" + c.Nombre + "' eliminado.");
        }
        catch (Exception ex) { MostrarError("No se pudo eliminar: " + ex.Message); }
    }

    private void ToggleFavoritos()
    {
        mostrandoSoloFavoritos = !mostrandoSoloFavoritos;
        ActualizarEstado(mostrandoSoloFavoritos
            ? "Mostrando solo favoritos."
            : "Mostrando todos los contactos.");
        AplicarFiltro();
    }



    private void ImportarDesdeJSON()
    {
        string? ruta = PedirRutaAlUsuario("Importar JSON", "Ingresá la ruta del archivo JSON a importar:");
        if (ruta == null) return;

        List<Contacto> importados;
        try
        {
            importados = JsonAgendaIO.Importar(ruta);
        }
        catch (FileNotFoundException) { MostrarError("No encontré el archivo: " + ruta); return; }
        catch (Exception ex)          { MostrarError("Error al leer el JSON: " + ex.Message); return; }

     
        bool confirmado = false;
        var dlgConf = new Dialog { Title = "Confirmar importacion", Width = 55, Height = 7 };
        dlgConf.Add(new Label
        {
            Text = "Se van a agregar " + importados.Count + " contacto(s). ¿Continuar?",
            X = 1, Y = 1, Width = Dim.Fill()
        });
        var btnSi = new Button { Text = "_Sí", IsDefault = true };
        btnSi.Accepting += (s, e) => { confirmado = true; App!.RequestStop(); e.Handled = true; };
        var btnNo = new Button { Text = "_No" };
        btnNo.Accepting += (s, e) => { App!.RequestStop(); e.Handled = true; };
        dlgConf.AddButton(btnSi);
        dlgConf.AddButton(btnNo);
        App!.Run(dlgConf);

        if (!confirmado) return;

        try
        {
            foreach (var c in importados)
            {
                c.Id = 0; 
                store.Insertar(c);
                contactos.Insert(0, c);
            }
            AplicarFiltro();
            ActualizarEstado("Se importaron " + importados.Count + " contacto(s) desde '" + ruta + "'.");
        }
        catch (Exception ex) { MostrarError("Error durante la importacion: " + ex.Message); }
    }

    private void ExportarAJSON()
    {
        string? ruta = PedirRutaAlUsuario("Exportar JSON", "Ingresá la ruta donde guardar el archivo:");
        if (ruta == null) return;

        try
        {
            JsonAgendaIO.Exportar(contactos, ruta);
            ActualizarEstado("Se exportaron " + contactos.Count + " contacto(s) a '" + ruta + "'.");
        }
        catch (Exception ex) { MostrarError("No se pudo exportar: " + ex.Message); }
    }

    private string? PedirRutaAlUsuario(string titulo, string mensaje)
    {
        string? rutaIngresada = null;
        var dlg = new Dialog { Title = titulo, Width = 60, Height = 8 };

        dlg.Add(new Label { Text = mensaje, X = 1, Y = 1, Width = Dim.Fill() });
        var campoRuta = new TextField { X = 1, Y = 3, Width = Dim.Fill(2) };
        dlg.Add(campoRuta);

        var btnOk = new Button { Text = "_Aceptar", IsDefault = true };
        btnOk.Accepting += (s, e) =>
        {
            rutaIngresada = (campoRuta.Text ?? "").Trim();
            App!.RequestStop();
            e.Handled = true;
        };
        var btnCancelar = new Button { Text = "_Cancelar" };
        btnCancelar.Accepting += (s, e) => { App!.RequestStop(); e.Handled = true; };

        dlg.AddButton(btnOk);
        dlg.AddButton(btnCancelar);
        App!.Run(dlg);

        return string.IsNullOrWhiteSpace(rutaIngresada) ? null : rutaIngresada;
    }


    private void MostrarError(string mensaje)
    {
        var dlg = new Dialog { Title = "Error", Width = 60, Height = 7 };
        dlg.Add(new Label { Text = mensaje, X = 1, Y = 1, Width = Dim.Fill() });
        var btnOk = new Button { Text = "_OK", IsDefault = true };
        btnOk.Accepting += (s, e) => { App!.RequestStop(); e.Handled = true; };
        dlg.AddButton(btnOk);
        App!.Run(dlg);
    }

    private void MostrarAcercaDe()
    {
        var dlg = new Dialog { Title = "Acerca de AgendaT", Width = 55, Height = 9 };
        dlg.Add(new Label
        {
            Text = "AgendaT - Trabajo Practico 3\n\nGestion de contactos con TUI.\nUsa Terminal.Gui v2, SQLite y Dapper.",
            X = 1, Y = 1, Width = Dim.Fill()
        });
        var btnOk = new Button { Text = "_OK", IsDefault = true };
        btnOk.Accepting += (s, e) => { App!.RequestStop(); e.Handled = true; };
        dlg.AddButton(btnOk);
        App!.Run(dlg);
    }

    private void PedirSalida() => App!.RequestStop();

    protected override bool OnKeyDown(Key key)
    {
       
        if (listaView.HasFocus &&
            (key == Key.CursorUp || key == Key.CursorDown ||
             key == Key.PageUp   || key == Key.PageDown   ||
             key == Key.Home     || key == Key.End))
        {
            
            bool handled = base.OnKeyDown(key);
            MostrarDetalleContacto();
            return handled;
        }

        if (key == Key.Q.WithCtrl)  { PedirSalida();               return true; }
        if (key == Key.N.WithCtrl)  { AbrirNuevoContacto();         return true; }
        if (key == Key.F2)          { AbrirNuevoContacto();         return true; }
        if (key == Key.F3)          { AbrirEdicionContacto();       return true; }
        if (key == Key.F4)          { campoBusqueda?.SetFocus();    return true; }
        if (key == Key.I.WithCtrl)  { ImportarDesdeJSON();          return true; }
        if (key == Key.E.WithCtrl)  { ExportarAJSON();              return true; }
        if (key == Key.D.WithCtrl)  { EliminarContactoSeleccionado(); return true; }
        if (key == Key.DeleteChar)  { EliminarContactoSeleccionado(); return true; }
        if (key == Key.Enter && listaView.HasFocus) { AbrirEdicionContacto(); return true; }

        return base.OnKeyDown(key);
    }
}


public sealed class ContactDialog : Dialog
{
    public Contacto? Resultado { get; private set; }

    private readonly TextField campoNombre;
    private readonly TextField campoTel1, campoTel2, campoTel3, campoTel4, campoTel5;
    private readonly TextField campoEmail;
    private readonly TextView  campoNotas;


    private bool esFavorito;
    private readonly CheckBox checkFavorito;

    public ContactDialog(Contacto contacto, string titulo)
    {
        Title = titulo; Width = 65; Height = 28;
        esFavorito = contacto.Favorito;

        int fila = 1;

        Add(new Label { Text = "Nombre:", X = 1, Y = fila });
        campoNombre = new TextField { X = 16, Y = fila, Width = 40, Text = contacto.Nombre };
        Add(campoNombre); fila++;

        fila++;
        Add(new Label { Text = "Teléfonos:", X = 1, Y = fila }); fila++;

        string[] tels = contacto.Telefonos
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim()).ToArray();
        string Tel(int i) => i < tels.Length ? tels[i] : "";

        Add(new Label { Text = "  Número 1:", X = 1, Y = fila });
        campoTel1 = new TextField { X = 16, Y = fila, Width = 22, Text = Tel(0) }; Add(campoTel1); fila++;
        Add(new Label { Text = "  Número 2:", X = 1, Y = fila });
        campoTel2 = new TextField { X = 16, Y = fila, Width = 22, Text = Tel(1) }; Add(campoTel2); fila++;
        Add(new Label { Text = "  Número 3:", X = 1, Y = fila });
        campoTel3 = new TextField { X = 16, Y = fila, Width = 22, Text = Tel(2) }; Add(campoTel3); fila++;
        Add(new Label { Text = "  Número 4:", X = 1, Y = fila });
        campoTel4 = new TextField { X = 16, Y = fila, Width = 22, Text = Tel(3) }; Add(campoTel4); fila++;
        Add(new Label { Text = "  Número 5:", X = 1, Y = fila });
        campoTel5 = new TextField { X = 16, Y = fila, Width = 22, Text = Tel(4) }; Add(campoTel5); fila++;

        fila++;
        Add(new Label { Text = "Email:", X = 1, Y = fila });
        campoEmail = new TextField { X = 16, Y = fila, Width = 40, Text = contacto.Email };
        Add(campoEmail); fila++;

        fila++;

        checkFavorito = new CheckBox
        {
            Text = "Marcar como favorito",
            X = 1, Y = fila
        };
       
        checkFavorito.Accepting += (s, e) =>
        {
            esFavorito = !esFavorito;
           
            e.Handled = true;
        };
        Add(checkFavorito); fila++;

        fila++;
        Add(new Label { Text = "Notas:", X = 1, Y = fila }); fila++;
        campoNotas = new TextView { X = 1, Y = fila, Width = Dim.Fill(2), Height = 4, Text = contacto.Notas };
        Add(campoNotas);

        var btnGuardar = new Button { Text = "_Guardar", IsDefault = true };
        btnGuardar.Accepting += (s, e) =>
        {
            if (!DatosValidos()) { e.Handled = true; return; }
            Resultado = ArmarContacto(contacto.Id);
            App!.RequestStop();
            e.Handled = true;
        };

        var btnCancelar = new Button { Text = "_Cancelar" };
        btnCancelar.Accepting += (s, e) => { App!.RequestStop(); e.Handled = true; };

        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }

    private bool DatosValidos()
    {
        if (string.IsNullOrWhiteSpace(campoNombre.Text))
        {
            var dlg = new Dialog { Title = "Validacion", Width = 45, Height = 6 };
            dlg.Add(new Label { Text = "El nombre no puede estar vacio.", X = 1, Y = 1 });
            var ok = new Button { Text = "_OK", IsDefault = true };
            ok.Accepting += (s, e) => { App!.RequestStop(); e.Handled = true; };
            dlg.AddButton(ok);
            App!.Run(dlg);
            campoNombre.SetFocus();
            return false;
        }
        string email = (campoEmail.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(email) && !email.Contains('@'))
        {
            var dlg = new Dialog { Title = "Validacion", Width = 45, Height = 6 };
            dlg.Add(new Label { Text = "El email tiene que tener '@'.", X = 1, Y = 1 });
            var ok = new Button { Text = "_OK", IsDefault = true };
            ok.Accepting += (s, e) => { App!.RequestStop(); e.Handled = true; };
            dlg.AddButton(ok);
            App!.Run(dlg);
            campoEmail.SetFocus();
            return false;
        }
        return true;
    }

    private Contacto ArmarContacto(int idExistente)
    {
        var numeros = new List<string>();
        foreach (var campo in new[] { campoTel1, campoTel2, campoTel3, campoTel4, campoTel5 })
        {
            string num = (campo.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(num)) numeros.Add(num);
        }

        return new Contacto
        {
            Id        = idExistente,
            Nombre    = (campoNombre.Text ?? "").Trim(),
            Telefonos = string.Join(", ", numeros),
            Email     = (campoEmail.Text ?? "").Trim(),
            Notas     = campoNotas.Text ?? "",
            Favorito  = esFavorito
        };
    }
}


public class SqliteAgendaStore
{
    private readonly string connectionString;

    public SqliteAgendaStore(string rutaArchivo)
    {
        connectionString = "Data Source=" + rutaArchivo;
        CrearTablasSiNoExisten();
    }

    private void CrearTablasSiNoExisten()
    {
        using var conn = AbrirConexion();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT    NOT NULL DEFAULT '',
                Telefonos TEXT    NOT NULL DEFAULT '',
                Email     TEXT    NOT NULL DEFAULT '',
                Notas     TEXT    NOT NULL DEFAULT '',
                Favorito  INTEGER NOT NULL DEFAULT 0
            );");
    }

    public IEnumerable<Contacto> ObtenerTodos()
    {
        using var conn = AbrirConexion();
        return conn.GetAll<Contacto>().OrderBy(c => c.Nombre).ToList();
    }

    public void Insertar(Contacto c)
    {
        using var conn = AbrirConexion();
        c.Id = (int)conn.Insert(c);
    }

    public void Actualizar(Contacto c)
    {
        using var conn = AbrirConexion();
        conn.Update(c);
    }

    public void Eliminar(int id)
    {
        using var conn = AbrirConexion();
        conn.Delete(new Contacto { Id = id });
    }

    private SqliteConnection AbrirConexion()
    {
        var conn = new SqliteConnection(connectionString);
        conn.Open();
        return conn;
    }
}

// Importacion y exportacion JSON
public class JsonAgendaIO
{
    private static readonly JsonSerializerOptions opcionesJson = new JsonSerializerOptions
    {
        WriteIndented        = true,
        Encoder              = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static List<Contacto> Importar(string ruta)
    {
        if (!File.Exists(ruta)) throw new FileNotFoundException("El archivo no existe.", ruta);
        string contenido = File.ReadAllText(ruta, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<Contacto>>(contenido, opcionesJson)
               ?? throw new InvalidDataException("El archivo no tiene el formato esperado.");
    }

    public static void Exportar(IEnumerable<Contacto> contactos, string ruta)
    {
        File.WriteAllText(ruta, JsonSerializer.Serialize(contactos.ToList(), opcionesJson), Encoding.UTF8);
    }
}

// Modelo de datos
[Table("Contactos")]
public sealed class Contacto
{
    [Key] public int    Id        { get; set; }
         public string Nombre    { get; set; } = "";
         public string Telefonos { get; set; } = "";
         public string Email     { get; set; } = "";
         public string Notas     { get; set; } = "";
         public bool   Favorito  { get; set; }

    public Contacto Clone() => new Contacto
    {
        Id = Id, Nombre = Nombre, Telefonos = Telefonos,
        Email = Email, Notas = Notas, Favorito = Favorito
    };
}
