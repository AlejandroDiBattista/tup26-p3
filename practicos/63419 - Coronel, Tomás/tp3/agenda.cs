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
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Data.Common;

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;



string archivoDb = args.Length > 0 ? args[0] : "agenda.db";

var repositorio = new SqliteAgendaStore(archivoDb);
repositorio.InitDb();

using IApplication app = Application.Create().Init();
app.Run(new VentanaPrincipal(repositorio));


public sealed class VentanaPrincipal : Runnable
{
    private readonly SqliteAgendaStore _repositorio;
    private List<Contacto> _listaCompleta = [];
    private List<Contacto> _listaFiltrada = [];

    private ListView    _vistaLista    = null!;
    private TextField   _campoBusqueda = null!;
    private TextView    _vistaDetalle  = null!;
    private Label       _etiquetaEstado = null!;
    private MenuItem    _itemFavoritos  = null!;

    private bool _filtrarFavoritos = false;

    public VentanaPrincipal(SqliteAgendaStore repositorio)
    {
        _repositorio = repositorio;
        Title  = "AgendaT — TP3";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        ArmarVentana();
        CargarContactos();
    }

    private void ArmarVentana()
    {
        _itemFavoritos = new MenuItem("_Solo favoritos", null!, AlternarFavoritos);

        MenuBar barraMenu = new()
        {
            Menus =
            [
                new MenuBarItem("_Archivo",
                [
                    new MenuItem("_Importar JSON", "Ctrl+I", ImportarDesdeJson),
                    new MenuItem("_Exportar JSON", "Ctrl+E", ExportarAJson),
                    null!,
                    new MenuItem("_Salir", "Ctrl+Q", PedirSalida)
                ]),
                new MenuBarItem("_Contactos",
                [
                    new MenuItem("_Nuevo",    "F2",  AgregarContacto),
                    new MenuItem("_Editar",   "F3",  ModificarContacto),
                    new MenuItem("_Eliminar", "Del", BorrarContacto)
                ]),
                new MenuBarItem("_Ver",
                [
                    _itemFavoritos
                ]),
                new MenuBarItem("_Ayuda",
                [
                    new MenuItem("_Acerca de", null!, VerAcercaDe)
                ])
            ]
        };

        Label lblBuscar = new() { Text = "Buscar [F4]:", X = 0, Y = 1 };
        _campoBusqueda = new TextField()
        {
            Text = "",
            X = Pos.Right(lblBuscar) + 1,
            Y = 1,
            Width = Dim.Fill()
        };
        _campoBusqueda.TextChanged += (_, _) => AplicarFiltro();

        FrameView panelIzquierdo = new()
        {
            Title = "Contactos",
            X = 0, Y = 2,
            Width = Dim.Percent(50),
            Height = Dim.Fill(1)
        };
        _vistaLista = new ListView()
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _vistaLista.Accepting += (_, e) => { ModificarContacto(); e.Handled = true; };
        _vistaLista.KeyDown   += (_, _) => RefrescarDetalle();
        _vistaLista.KeyUp     += (_, _) => RefrescarDetalle();
        panelIzquierdo.Add(_vistaLista);

        FrameView panelDerecho = new()
        {
            Title = "Detalle",
            X = Pos.Right(panelIzquierdo), Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };
        _vistaDetalle = new TextView()
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        panelDerecho.Add(_vistaDetalle);

        _etiquetaEstado = new Label()
        {
            Text = "Listo.",
            X = 0,
            Y = Pos.Bottom(panelIzquierdo),
            Width = Dim.Fill()
        };

        Add(barraMenu, lblBuscar, _campoBusqueda, panelIzquierdo, panelDerecho, _etiquetaEstado);
    }

    private void ActualizarEstado(string msg) => _etiquetaEstado.Text = msg;

    private void CargarContactos()
    {
        try
        {
            _listaCompleta = _repositorio.GetAll().ToList();
            AplicarFiltro();
            ActualizarEstado($"{_listaCompleta.Count} contacto(s) cargado(s).");
        }
        catch (Exception ex)
        {
            DialogoError("Error al cargar", ex.Message);
        }
    }

    private void AplicarFiltro()
    {
        string termino = _campoBusqueda.Text?.ToLower() ?? "";
        _listaFiltrada = _listaCompleta.Where(c =>
        {
            if (_filtrarFavoritos && !c.Favorito) return false;
            if (string.IsNullOrWhiteSpace(termino)) return true;
            return (c.Nombre?.ToLower().Contains(termino) == true)    ||
                   (c.Telefonos?.ToLower().Contains(termino) == true) ||
                   (c.Email?.ToLower().Contains(termino) == true);
        }).ToList();

        _vistaLista.SetSource(new ObservableCollection<Contacto>(_listaFiltrada));
        RefrescarDetalle();
    }

    private void RefrescarDetalle()
    {
        int? idx = _vistaLista.SelectedItem;
        if (idx.HasValue && idx.Value >= 0 && idx.Value < _listaFiltrada.Count)
        {
            var c = _listaFiltrada[idx.Value];
            _vistaDetalle.Text =
                $"Nombre:    {c.Nombre}\n"     +
                $"Email:     {c.Email}\n"       +
                $"Teléfonos: {c.Telefonos}\n"   +
                $"Favorito:  {(c.Favorito ? "Sí ★" : "No")}\n\n" +
                $"Notas:\n{c.Notas}";
        }
        else
        {
            _vistaDetalle.Text = "";
        }
    }

    private void AgregarContacto()
    {
        var dlg = new DialogoContacto();
        App!.Run(dlg);
        if (dlg.Cancelado || dlg.Resultado == null) return;
        try
        {
            _repositorio.Insert(dlg.Resultado);
            _listaCompleta.Add(dlg.Resultado);
            AplicarFiltro();
            ActualizarEstado($"Contacto '{dlg.Resultado.Nombre}' guardado.");
        }
        catch (Exception ex) { DialogoError("Error al guardar", ex.Message); }
    }

    private void ModificarContacto()
    {
        int? idx = _vistaLista.SelectedItem;
        if (!idx.HasValue || idx.Value < 0 || idx.Value >= _listaFiltrada.Count) return;

        var original = _listaFiltrada[idx.Value];
        var dlg = new DialogoContacto(original);
        App!.Run(dlg);
        if (dlg.Cancelado || dlg.Resultado == null) return;
        try
        {
            _repositorio.Update(dlg.Resultado);
            int pos = _listaCompleta.FindIndex(x => x.Id == dlg.Resultado.Id);
            if (pos >= 0) _listaCompleta[pos] = dlg.Resultado;
            AplicarFiltro();
            ActualizarEstado($"Contacto '{dlg.Resultado.Nombre}' actualizado.");
        }
        catch (Exception ex) { DialogoError("Error al actualizar", ex.Message); }
    }

    private void BorrarContacto()
    {
        int? idx = _vistaLista.SelectedItem;
        if (!idx.HasValue || idx.Value < 0 || idx.Value >= _listaFiltrada.Count) return;

        var c = _listaFiltrada[idx.Value];
        if (DialogoConfirmacion("Eliminar contacto", $"¿Eliminar a '{c.Nombre}'?") != 1) return;
        try
        {
            _repositorio.Delete(c);
            _listaCompleta.RemoveAll(x => x.Id == c.Id);
            AplicarFiltro();
            ActualizarEstado($"'{c.Nombre}' eliminado.");
        }
        catch (Exception ex) { DialogoError("Error al eliminar", ex.Message); }
    }

    private void AlternarFavoritos()
    {
        _filtrarFavoritos = !_filtrarFavoritos;
        _itemFavoritos.Title = _filtrarFavoritos ? "_Ver todos" : "_Solo favoritos";
        AplicarFiltro();
        ActualizarEstado(_filtrarFavoritos ? "Mostrando solo favoritos." : "Mostrando todos los contactos.");
    }

    private void ImportarDesdeJson()
    {
        string? ruta = PedirRuta("Importar JSON", "Ruta del archivo JSON:");
        if (ruta == null) return;
        try
        {
            var importados = JsonAgendaIO.Import(ruta).ToList();
            if (DialogoConfirmacion("Importar", $"Se encontraron {importados.Count} contacto(s).\n¿Agregar todos?") != 1) return;
            foreach (var c in importados)
            {
                c.Id = 0;
                _repositorio.Insert(c);
                _listaCompleta.Add(c);
            }
            AplicarFiltro();
            ActualizarEstado($"{importados.Count} contacto(s) importado(s).");
        }
        catch (FileNotFoundException) { DialogoError("Error", "No se encontró el archivo indicado."); }
        catch (JsonException)         { DialogoError("Error", "El archivo no tiene formato JSON válido."); }
        catch (Exception ex)          { DialogoError("Error al importar", ex.Message); }
    }

    private void ExportarAJson()
    {
        string? ruta = PedirRuta("Exportar JSON", "Ruta donde guardar:", "contactos.json");
        if (ruta == null) return;
        try
        {
            JsonAgendaIO.Export(ruta, _listaCompleta);
            ActualizarEstado($"Exportado correctamente en: {ruta}");
        }
        catch (Exception ex) { DialogoError("Error al exportar", ex.Message); }
    }

    private void VerAcercaDe()
    {
        Dialog d = new() { Title = "Acerca de", Width = 42, Height = 9 };
        d.Add(new Label() { Text = "AgendaT — Trabajo Práctico 3\nDesarrollado con Terminal.Gui v2\nC# / .NET 10", X = Pos.Center(), Y = 1 });
        Button btn = new() { Text = "Cerrar", IsDefault = true };
        btn.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        d.AddButton(btn);
        App!.Run(d);
    }

    private void PedirSalida() => App!.RequestStop();

    private void DialogoError(string titulo, string mensaje)
    {
        Dialog d = new() { Title = titulo, Width = 50, Height = 8 };
        d.Add(new Label() { Text = mensaje, X = Pos.Center(), Y = 1 });
        Button btn = new() { Text = "Aceptar", IsDefault = true };
        btn.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        d.AddButton(btn);
        App!.Run(d);
    }

    private int DialogoConfirmacion(string titulo, string mensaje)
    {
        Dialog d = new() { Title = titulo, Width = 50, Height = 8 };
        d.Add(new Label() { Text = mensaje, X = Pos.Center(), Y = 1 });
        Button btnSi = new() { Text = "Sí", IsDefault = true };
        Button btnNo = new() { Text = "No" };
        int resultado = 0;
        btnSi.Accepting += (_, e) => { resultado = 1; App!.RequestStop(); e.Handled = true; };
        btnNo.Accepting += (_, e) => { resultado = 0; App!.RequestStop(); e.Handled = true; };
        d.AddButton(btnNo);
        d.AddButton(btnSi);
        App!.Run(d);
        return resultado;
    }

    private string? PedirRuta(string titulo, string etiqueta, string valorInicial = "")
    {
        Dialog d = new() { Title = titulo, Width = 50, Height = 8 };
        Label lbl = new() { Text = etiqueta, X = 1, Y = 1 };
        TextField tf = new() { Text = valorInicial, X = 1, Y = 2, Width = Dim.Fill(1) };
        Button btnOk     = new() { Text = "Aceptar",  IsDefault = true };
        Button btnCancel = new() { Text = "Cancelar" };
        string? resultado = null;
        btnOk.Accepting     += (_, e) => { resultado = tf.Text; App!.RequestStop(); e.Handled = true; };
        btnCancel.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        d.Add(lbl, tf);
        d.AddButton(btnOk);
        d.AddButton(btnCancel);
        App!.Run(d);
        return string.IsNullOrWhiteSpace(resultado) ? null : resultado;
    }

    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Q.WithCtrl)                  { PedirSalida();         return true; }
        if (key == Key.I.WithCtrl)                  { ImportarDesdeJson();   return true; }
        if (key == Key.E.WithCtrl)                  { ExportarAJson();       return true; }
        if (key == Key.N.WithCtrl || key == Key.F2) { AgregarContacto();     return true; }
        if (key == Key.F3)                          { ModificarContacto();   return true; }
        if (key == Key.D.WithCtrl || key == Key.Delete) { BorrarContacto(); return true; }
        if (key == Key.F4)                          { _campoBusqueda.SetFocus(); return true; }
        return base.OnKeyDown(key);
    }
}


public sealed class DialogoContacto : Dialog
{
    private readonly TextField   _tfNombre;
    private readonly TextField[] _tfTelefonos = new TextField[5];
    private readonly TextField   _tfEmail;
    private readonly TextView    _tvNotas;
    private readonly Button      _btnFavorito;
    private bool _esFavorito;

    public Contacto? Resultado { get; private set; }
    public bool Cancelado { get; private set; } = true;

    public DialogoContacto(Contacto? c = null)
    {
        Title  = c == null ? "Nuevo contacto" : "Editar contacto";
        Width  = 55;
        Height = 22;

        Add(new Label() { Text = "Nombre:", X = 1, Y = 1 });
        _tfNombre = new TextField() { Text = c?.Nombre ?? "", X = 12, Y = 1, Width = Dim.Fill(1) };
        Add(_tfNombre);

        string[] numeros = (c?.Telefonos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < 5; i++)
        {
            Add(new Label() { Text = $"Teléfono {i + 1}:", X = 1, Y = 3 + i });
            _tfTelefonos[i] = new TextField()
            {
                Text = i < numeros.Length ? numeros[i].Trim() : "",
                X = 12, Y = 3 + i, Width = Dim.Fill(1)
            };
            Add(_tfTelefonos[i]);
        }

        Add(new Label() { Text = "Email:", X = 1, Y = 9 });
        _tfEmail = new TextField() { Text = c?.Email ?? "", X = 12, Y = 9, Width = Dim.Fill(1) };
        Add(_tfEmail);

        Add(new Label() { Text = "Notas:", X = 1, Y = 11 });
        _tvNotas = new TextView() { Text = c?.Notas ?? "", X = 12, Y = 11, Width = Dim.Fill(1), Height = 4 };
        Add(_tvNotas);

        _esFavorito = c?.Favorito ?? false;
        Add(new Label() { Text = "Favorito:", X = 1, Y = 16 });
        _btnFavorito = new Button()
        {
            Text = _esFavorito ? "[★] Sí" : "[ ] No",
            X = 12, Y = 16
        };
        _btnFavorito.Accepting += (_, e) =>
        {
            _esFavorito = !_esFavorito;
            _btnFavorito.Text = _esFavorito ? "[★] Sí" : "[ ] No";
            e.Handled = true;
        };
        Add(_btnFavorito);

        Button btnGuardar  = new() { Text = "Guardar",   IsDefault = true };
        Button btnCancelar = new() { Text = "Cancelar" };

        btnGuardar.Accepting += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(_tfNombre.Text))
            {
                MostrarValidacion("El nombre es obligatorio.");
                return;
            }
            string email = _tfEmail.Text ?? "";
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            {
                MostrarValidacion("El email debe contener '@'.");
                return;
            }
            var tels = _tfTelefonos
                .Select(t => t.Text?.Trim())
                .Where(t => !string.IsNullOrEmpty(t));

            Resultado = new Contacto
            {
                Id        = c?.Id ?? 0,
                Nombre    = _tfNombre.Text.Trim(),
                Telefonos = string.Join(",", tels),
                Email     = email.Trim(),
                Notas     = _tvNotas.Text ?? "",
                Favorito  = _esFavorito
            };
            Cancelado = false;
            App!.RequestStop();
            e.Handled = true;
        };

        btnCancelar.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };

        AddButton(btnGuardar);
        AddButton(btnCancelar);
    }

    private void MostrarValidacion(string mensaje)
    {
        Dialog d = new() { Title = "Atención", Width = 42, Height = 7 };
        d.Add(new Label() { Text = mensaje, X = Pos.Center(), Y = 1 });
        Button btn = new() { Text = "OK", IsDefault = true };
        btn.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        d.AddButton(btn);
        App!.Run(d);
    }
}


public class SqliteAgendaStore
{
    private readonly string _connStr;

    public SqliteAgendaStore(string rutaDb)
    {
        _connStr = new SqliteConnectionStringBuilder { DataSource = rutaDb }.ToString();
    }

    public void InitDb()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT NOT NULL,
                Telefonos TEXT,
                Email     TEXT,
                Notas     TEXT,
                Favorito  INTEGER
            )");
    }

    public IEnumerable<Contacto> GetAll()
    {
        using var conn = new SqliteConnection(_connStr);
        return conn.GetAll<Contacto>();
    }

    public void Insert(Contacto c)
    {
        using var conn = new SqliteConnection(_connStr);
        c.Id = (int)conn.Insert(c);
    }

    public void Update(Contacto c)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Update(c);
    }

    public void Delete(Contacto c)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Delete(c);
    }
}


public class JsonAgendaIO
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static void Export(string ruta, IEnumerable<Contacto> contactos)
    {
        var json = JsonSerializer.Serialize(contactos, Opciones);
        File.WriteAllText(ruta, json);
    }

    public static IEnumerable<Contacto> Import(string ruta)
    {
        if (!File.Exists(ruta)) throw new FileNotFoundException("No se encontró el archivo JSON.");
        var json = File.ReadAllText(ruta);
        return JsonSerializer.Deserialize<List<Contacto>>(json, Opciones) ?? [];
    }
}


[Table("Contactos")]
public class Contacto
{
    [Key] public int    Id        { get; set; }
         public string Nombre    { get; set; } = "";
         public string Telefonos { get; set; } = "";
         public string Email     { get; set; } = "";
         public string Notas     { get; set; } = "";
         public bool   Favorito  { get; set; }

    public Contacto Clone() => (Contacto)MemberwiseClone();
    public override string ToString() => $"{(Favorito ? "★" : " ")} {Nombre}";
}