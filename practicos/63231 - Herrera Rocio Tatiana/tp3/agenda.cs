#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
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

#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
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

string archivoDb = args.Length > 0 ? args[0] : ":memory:";
using SqliteAgendaStore store = new(archivoDb);
store.CrearTablas();
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store, archivoDb));

[Table("Contactos")]
public sealed class Contacto {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }
    public Contacto Clone() => new() { Id = Id, Nombre = Nombre, Telefonos = Telefonos, Email = Email, Notas = Notas, Favorito = Favorito };
}

public sealed class SqliteAgendaStore : IDisposable {
    readonly SqliteConnection cn;
    public SqliteAgendaStore(string archivo) { cn = new(new SqliteConnectionStringBuilder { DataSource = archivo }.ConnectionString); cn.Open(); }
    public void CrearTablas() => cn.Execute("""
        CREATE TABLE IF NOT EXISTS Contactos(
            Id INTEGER PRIMARY KEY AUTOINCREMENT, Nombre TEXT NOT NULL,
            Telefonos TEXT NOT NULL DEFAULT '', Email TEXT NOT NULL DEFAULT '',
            Notas TEXT NOT NULL DEFAULT '', Favorito INTEGER NOT NULL DEFAULT 0);
        """);
    public IEnumerable<Contacto> Listar() => cn.GetAll<Contacto>();
    public Contacto Agregar(Contacto c) { Validar(c); c.Id = 0; c.Id = Convert.ToInt32(cn.Insert(c)); return c; }
    public void Modificar(Contacto c) { Validar(c); cn.Update(c); }
    public void Eliminar(Contacto c) => cn.Delete(c);
    public void Dispose() => cn.Dispose();
    static void Validar(Contacto c) {
        if (string.IsNullOrWhiteSpace(c.Nombre)) throw new InvalidOperationException("El nombre no puede estar vacío.");
        if (!string.IsNullOrWhiteSpace(c.Email) && !c.Email.Contains('@')) throw new InvalidOperationException("El email debe contener @.");
    }
}
public sealed class AgendaWindow : Runnable {
    readonly SqliteAgendaStore store; readonly string db;
    readonly List<Contacto> contactos; readonly List<Contacto> visibles = [];
    readonly System.Collections.ObjectModel.ObservableCollection<string> filas = [];
    readonly TextField buscar; readonly ListView lista; readonly TextView detalle; readonly Label estado;
    bool soloFav;

    public AgendaWindow(SqliteAgendaStore store, string db) {
        this.store = store; this.db = db; contactos = store.Listar().ToList();
        Title = "Agenda - Terminal.Gui"; Width = Dim.Fill(); Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;

        Label lbl = new() { Text = "Buscar:", X = 1, Y = 1 };
        buscar = new() { X = Pos.Right(lbl) + 1, Y = 1, Width = Dim.Fill(1) };
        buscar.TextChanged += (_, _) => ActualizarVista();

        lista = new() { X = 0, Y = 3, Width = Dim.Percent(40), Height = Dim.Fill(1), Title = "Contactos", BorderStyle = LineStyle.Single };
        lista.SetSource(filas);
        lista.ValueChanged += (_, _) => MostrarDetalle();
        lista.Accepting += (_, e) => { Editar(); e.Handled = true; };

        detalle = new() { X = Pos.Right(lista) + 1, Y = 3, Width = Dim.Fill(), Height = Dim.Fill(1), Title = "Detalle", BorderStyle = LineStyle.Single, CanFocus = false };
        estado = new() { X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Text = "Listo." };
        Add(ArmarMenu(), lbl, buscar, lista, detalle, estado);
        ActualizarVista();
    }

    MenuBar ArmarMenu() => new() { Menus = [
        new MenuBarItem("_Archivo", [
            new MenuItem("_Importar JSON", "Ctrl+I", Importar),
            new MenuItem("_Exportar JSON", "Ctrl+E", Exportar),
            null!,
            new MenuItem("_Salir", "Ctrl+Q", () => App!.RequestStop())]),
        new MenuBarItem("_Contactos", [
            new MenuItem("_Nuevo", "F2 / Ctrl+N", Nuevo),
            new MenuItem("_Editar", "F3 / Enter", Editar),
            new MenuItem("_Eliminar", "Del / Ctrl+D", Eliminar)]),
        new MenuBarItem("_Ver", [
            new MenuItem("_Solo favoritos", null!, () => { soloFav = !soloFav; ActualizarVista(); Avisar(soloFav ? "Solo favoritos." : "Todos los contactos."); })]),
        new MenuBarItem("A_yuda", [
            new MenuItem("_Acerca de", null!, () => MessageBox.Query(App!, "Acerca de", $"AgendaT\nBase: {(db == ":memory:" ? "memoria" : db)}", "Aceptar"))])
    ] };

    protected override bool OnKeyDown(Key key) {
        if (key == Key.F2 || key == Key.N.WithCtrl) { Nuevo(); return true; }
        if (key == Key.F3 || key == Key.Enter) { Editar(); return true; }
        if (key == Key.Delete || key == Key.D.WithCtrl) { Eliminar(); return true; }
        if (key == Key.I.WithCtrl) { Importar(); return true; }
        if (key == Key.E.WithCtrl) { Exportar(); return true; }
        if (key == Key.F4) { buscar.SetFocus(); Avisar("Búsqueda activa."); return true; }
        if (key == Key.Q.WithCtrl) { App!.RequestStop(); return true; }
        return base.OnKeyDown(key);
    }
}
void Nuevo() {
    ContactDialog dlg = new("Nuevo contacto", new Contacto());
    App!.Run(dlg); if (dlg.Resultado is null) return;
    Intentar("crear", () => { var c = store.Agregar(dlg.Resultado); contactos.Add(c); ActualizarVista(c.Id); Avisar("Contacto creado."); });
}

void Editar() {
    Contacto? c = Seleccionado(); if (c is null) { Avisar("No hay contacto seleccionado."); return; }
    ContactDialog dlg = new("Editar contacto", c.Clone());
    App!.Run(dlg); if (dlg.Resultado is null) return;
    Intentar("editar", () => {
        store.Modificar(dlg.Resultado);
        contactos[contactos.FindIndex(x => x.Id == c.Id)] = dlg.Resultado;
        ActualizarVista(dlg.Resultado.Id); Avisar("Contacto actualizado.");
    });
}

void Eliminar() {
    Contacto? c = Seleccionado(); if (c is null) { Avisar("Seleccioná un contacto."); return; }
    if (MessageBox.Query(App!, "Confirmar", $"¿Eliminar a {c.Nombre}?", "Eliminar", "Cancelar") != 0) return;
    Intentar("eliminar", () => { store.Eliminar(c); contactos.RemoveAll(x => x.Id == c.Id); ActualizarVista(); Avisar("Contacto eliminado."); });
}

void ActualizarVista(int? id = null) {
    string texto = buscar.Text?.ToString() ?? "";
    visibles.Clear();
    visibles.AddRange(contactos.Where(c => Coincide(c, texto) && (!soloFav || c.Favorito)).OrderBy(c => c.Nombre));
    filas.Clear();
    foreach (Contacto c in visibles) filas.Add($"{(c.Favorito ? "*" : " ")} {c.Nombre} - {c.Telefonos}");
    lista.SetSource(filas);
    int pos = id is null ? 0 : visibles.FindIndex(c => c.Id == id);
    lista.SelectedItem = visibles.Count == 0 ? null : Math.Max(0, pos);
    MostrarDetalle();
}

static bool Coincide(Contacto c, string q) => string.IsNullOrWhiteSpace(q)
    || c.Nombre.Contains(q, StringComparison.CurrentCultureIgnoreCase)
    || c.Telefonos.Contains(q, StringComparison.CurrentCultureIgnoreCase)
    || c.Email.Contains(q, StringComparison.CurrentCultureIgnoreCase);

Contacto? Seleccionado() {
    int? i = lista.SelectedItem;
    return i is null || i < 0 || i >= visibles.Count ? null : visibles[i.Value];
}

void MostrarDetalle() {
    Contacto? c = Seleccionado();
    detalle.Text = c is null ? "Sin contactos." : $"Id: {c.Id}\nNombre: {c.Nombre}\nTeléfonos: {c.Telefonos}\nEmail: {c.Email}\nFavorito: {(c.Favorito ? "Sí" : "No")}\n\nNotas:\n{c.Notas}";
}

void Intentar(string accion, Action op) {
    try { op(); } catch (Exception ex) { MessageBox.ErrorQuery(App!, $"Error al {accion}", ex.Message, "Aceptar"); Avisar(ex.Message); }
}
void Avisar(string msg) { estado.Text = msg; estado.SetNeedsDraw(); }

public sealed class ContactDialog : Dialog {
    readonly TextField nombre, email; readonly TextField[] tel; readonly TextView notas; readonly CheckBox fav;
    public Contacto? Resultado { get; private set; }

    public ContactDialog(string titulo, Contacto c) {
        Title = titulo; Width = Dim.Percent(70); Height = Dim.Percent(80);
        nombre = Campo("Nombre:", 1, c.Nombre);
        tel = Enumerable.Range(0, 5).Select(i => Campo($"Teléfono {i + 1}:", 3 + i * 2, Telefono(c, i))).ToArray();
        email = Campo("Email:", 13, c.Email);
        Add(new Label { Text = "Notas:", X = 2, Y = 15 });
        notas = new() { X = 14, Y = 15, Width = Dim.Fill(2), Height = Dim.Fill(4), Text = c.Notas, BorderStyle = LineStyle.Single };
        fav = new() { Text = "Favorito", X = 14, Y = Pos.Bottom(notas) + 1, Value = c.Favorito ? CheckState.Checked : CheckState.UnChecked };
        Add(notas, fav);
        Button guardar = new() { Text = "Guardar", IsDefault = true };
        guardar.Accepting += (_, e) => { if (Crear(c.Id, out Contacto listo)) { Resultado = listo; App!.RequestStop(); } e.Handled = true; };
        Button cancelar = new() { Text = "Cancelar" };
        cancelar.Accepting += (_, e) => { Resultado = null; App!.RequestStop(); e.Handled = true; };
        AddButton(cancelar); AddButton(guardar);
    }

    TextField Campo(string texto, int y, string valor) {
        Add(new Label { Text = texto, X = 2, Y = y });
        TextField t = new() { X = 14, Y = y, Width = Dim.Fill(2), Text = valor };
        Add(t); return t;
    }

    bool Crear(int id, out Contacto c) {
        c = new Contacto();
        string n = nombre.Text?.ToString().Trim() ?? "", m = email.Text?.ToString().Trim() ?? "";
        if (n == "") { MessageBox.ErrorQuery(App!, "Validación", "El nombre no puede estar vacío.", "Aceptar"); return false; }
        if (m != "" && !m.Contains('@')) { MessageBox.ErrorQuery(App!, "Validación", "El email debe contener @.", "Aceptar"); return false; }
        c = new() { Id = id, Nombre = n, Email = m, Notas = notas.Text?.ToString() ?? "", Favorito = fav.Value == CheckState.Checked,
            Telefonos = string.Join(", ", tel.Select(x => x.Text?.ToString().Trim()).Where(x => !string.IsNullOrWhiteSpace(x))) };
        return true;
    }
    static string Telefono(Contacto c, int i) {
        string[] partes = c.Telefonos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return i < partes.Length ? partes[i] : "";
    }
}

public sealed class PathDialog : Dialog {
    readonly TextField campo; public string? Ruta { get; private set; }
    public PathDialog(string titulo, string etiqueta, string boton) {
        Title = titulo; Width = Dim.Percent(65); Height = 7;
        Add(new Label { Text = etiqueta, X = 1, Y = 1 });
        campo = new() { X = 1, Y = 2, Width = Dim.Fill(2) }; Add(campo);
        Button ok = new() { Text = boton, IsDefault = true };
        ok.Accepting += (_, e) => { Ruta = campo.Text?.ToString().Trim(); App!.RequestStop(); e.Handled = true; };
        Button cancelar = new() { Text = "Cancelar" };
        cancelar.Accepting += (_, e) => { Ruta = null; App!.RequestStop(); e.Handled = true; };
        AddButton(cancelar); AddButton(ok);
    }
}
void Importar() {
    string? ruta = PedirRuta("Importar JSON", "Archivo:", "Importar"); if (ruta is null) return;
    Intentar("importar", () => {
        var nuevos = JsonAgendaIO.Leer(ruta).ToList();
        if (MessageBox.Query(App!, "Importar", $"Se agregarán {nuevos.Count} contacto(s).", "Aceptar", "Cancelar") != 0) return;
        foreach (Contacto c in nuevos) contactos.Add(store.Agregar(c));
        ActualizarVista(); Avisar("Importación terminada.");
    });
}

void Exportar() {
    string? ruta = PedirRuta("Exportar JSON", "Destino:", "Exportar"); if (ruta is null) return;
    Intentar("exportar", () => { JsonAgendaIO.Escribir(ruta, contactos); Avisar("Exportación terminada."); });
}

string? PedirRuta(string titulo, string etiqueta, string boton) {
    PathDialog dlg = new(titulo, etiqueta, boton);
    App!.Run(dlg);
    return string.IsNullOrWhiteSpace(dlg.Ruta) ? null : dlg.Ruta;
}

public static class JsonAgendaIO {
    static readonly System.Text.Json.JsonSerializerOptions Opt = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static IEnumerable<Contacto> Leer(string ruta) {
        if (!File.Exists(ruta)) throw new FileNotFoundException("El archivo JSON no existe.", ruta);
        var datos = System.Text.Json.JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(ruta, System.Text.Encoding.UTF8), Opt) ?? throw new InvalidDataException("JSON inválido.");
        foreach (Contacto c in datos) { c.Id = 0; c.Nombre ??= ""; c.Telefonos ??= ""; c.Email ??= ""; c.Notas ??= ""; }
        return datos;
    }
    public static void Escribir(string ruta, IEnumerable<Contacto> contactos) =>
        File.WriteAllText(ruta, System.Text.Json.JsonSerializer.Serialize(contactos, Opt), new System.Text.UTF8Encoding(false));
}

