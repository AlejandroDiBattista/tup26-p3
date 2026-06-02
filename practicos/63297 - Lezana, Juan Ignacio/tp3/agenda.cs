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

string archBd = args.Length > 0 ? args[0] : ":memory:";
using SqliteAlmacenCtc almacen = new(archBd);
almacen.CrearTablas();
using IApplication apl = Application.Create().Init();
apl.Run(new VentanaCtc(almacen, archBd));

public sealed class VentanaCtc : Runnable {
    readonly SqliteAlmacenCtc almacen; readonly string bd;
    readonly List<Ctc> listaCtc; readonly List<Ctc> visibles = [];
    readonly System.Collections.ObjectModel.ObservableCollection<string> filas = [];
    readonly TextField cajaBusq; readonly ListView listaVis; readonly TextView panelDet; readonly Label barEstado;
    bool soloFav;

    public VentanaCtc(SqliteAlmacenCtc almacen, string bd) {
        this.almacen = almacen; this.bd = bd; listaCtc = almacen.ObtenerTodos().ToList();
        Title = "Agenda - Terminal.Gui"; Width = Dim.Fill(); Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;

        Label etiq = new() { Text = "Buscar:", X = 1, Y = 1 };
        cajaBusq = new() { X = Pos.Right(etiq) + 1, Y = 1, Width = Dim.Fill(1) };
        cajaBusq.TextChanged += (_, _) => ActVista();

        listaVis = new() { X = 0, Y = 3, Width = Dim.Percent(40), Height = Dim.Fill(1), Title = "Contactos", BorderStyle = LineStyle.Single };
        listaVis.SetSource(filas);
        listaVis.ValueChanged += (_, _) => MostrarDet();
        listaVis.Accepting += (_, e) => { Editar(); e.Handled = true; };

        panelDet = new() { X = Pos.Right(listaVis) + 1, Y = 3, Width = Dim.Fill(), Height = Dim.Fill(1), Title = "Detalle", BorderStyle = LineStyle.Single, CanFocus = false };
        barEstado = new() { X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Text = "Listo." };
        Add(ConstruirMenu(), etiq, cajaBusq, listaVis, panelDet, barEstado);
        ActVista();
    }

    MenuBar ConstruirMenu() => new() { Menus = [
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
            new MenuItem("_Solo favoritos", null!, () => { soloFav = !soloFav; ActVista(); Avisar(soloFav ? "Solo favoritos." : "Todos los contactos."); })]),
        new MenuBarItem("A_yuda", [
            new MenuItem("_Acerca de", null!, () => MessageBox.Query(App!, "Acerca de", $"AgendaT\nBase: {(bd == ":memory:" ? "memoria" : bd)}", "Aceptar"))])
    ] };

    protected override bool OnKeyDown(Key key) {
        if (key == Key.F2 || key == Key.N.WithCtrl) { Nuevo(); return true; }
        if (key == Key.F3 || key == Key.Enter) { Editar(); return true; }
        if (key == Key.Delete || key == Key.D.WithCtrl) { Eliminar(); return true; }
        if (key == Key.I.WithCtrl) { Importar(); return true; }
        if (key == Key.E.WithCtrl) { Exportar(); return true; }
        if (key == Key.F4) { cajaBusq.SetFocus(); Avisar("Búsqueda activa."); return true; }
        if (key == Key.Q.WithCtrl) { App!.RequestStop(); return true; }
        return base.OnKeyDown(key);
    }

    void Nuevo() {
        DialogCtc dlg = new("Nuevo contacto", new Ctc());
        App!.Run(dlg); if (dlg.Resultado is null) return;
        Intentar("crear", () => { var ctc = almacen.Agregar(dlg.Resultado); listaCtc.Add(ctc); ActVista(ctc.Id); Avisar("Contacto creado."); });
    }

    void Editar() {
        Ctc? ctc = Seleccionado(); if (ctc is null) { Avisar("No hay contacto seleccionado."); return; }
        DialogCtc dlg = new("Editar contacto", ctc.Clonar());
        App!.Run(dlg); if (dlg.Resultado is null) return;
        Intentar("editar", () => {
            almacen.Modificar(dlg.Resultado);
            listaCtc[listaCtc.FindIndex(x => x.Id == ctc.Id)] = dlg.Resultado;
            ActVista(dlg.Resultado.Id); Avisar("Contacto actualizado.");
        });
    }

    void Eliminar() {
        Ctc? ctc = Seleccionado(); if (ctc is null) { Avisar("Seleccioná un contacto."); return; }
        if (MessageBox.Query(App!, "Confirmar", $"¿Eliminar a {ctc.Nombre}?", "Eliminar", "Cancelar") != 0) return;
        Intentar("eliminar", () => { almacen.Borrar(ctc); listaCtc.RemoveAll(x => x.Id == ctc.Id); ActVista(); Avisar("Contacto eliminado."); });
    }

    void Importar() {
        string? ruta = PedirRuta("Importar JSON", "Archivo:", "Importar"); if (ruta is null) return;
        Intentar("importar", () => {
            var nuevos = JsonCtcIO.Leer(ruta).ToList();
            if (MessageBox.Query(App!, "Importar", $"Se agregarán {nuevos.Count} contacto(s).", "Aceptar", "Cancelar") != 0) return;
            foreach (Ctc ctc in nuevos) listaCtc.Add(almacen.Agregar(ctc));
            ActVista(); Avisar("Importación terminada.");
        });
    }

    void Exportar() {
        string? ruta = PedirRuta("Exportar JSON", "Destino:", "Exportar"); if (ruta is null) return;
        Intentar("exportar", () => { JsonCtcIO.Escribir(ruta, listaCtc); Avisar("Exportación terminada."); });
    }

    string? PedirRuta(string titulo, string etiqueta, string boton) {
        DialogRuta dlg = new(titulo, etiqueta, boton);
        App!.Run(dlg);
        return string.IsNullOrWhiteSpace(dlg.Ruta) ? null : dlg.Ruta;
    }

    void ActVista(int? id = null) {
        string texto = cajaBusq.Text?.ToString() ?? "";
        visibles.Clear();
        visibles.AddRange(listaCtc.Where(ctc => Coincide(ctc, texto) && (!soloFav || ctc.Fav)).OrderBy(ctc => ctc.Nombre));
        filas.Clear();
        foreach (Ctc ctc in visibles) filas.Add($"{(ctc.Fav ? "*" : " ")} {ctc.Nombre} - {ctc.Tels}");
        listaVis.SetSource(filas);
        int pos = id is null ? 0 : visibles.FindIndex(ctc => ctc.Id == id);
        listaVis.SelectedItem = visibles.Count == 0 ? null : Math.Max(0, pos);
        MostrarDet();
    }

    static bool Coincide(Ctc ctc, string q) => string.IsNullOrWhiteSpace(q)
        || ctc.Nombre.Contains(q, StringComparison.CurrentCultureIgnoreCase)
        || ctc.Tels.Contains(q, StringComparison.CurrentCultureIgnoreCase)
        || ctc.Email.Contains(q, StringComparison.CurrentCultureIgnoreCase);

    Ctc? Seleccionado() {
        int? i = listaVis.SelectedItem;
        return i is null || i < 0 || i >= visibles.Count ? null : visibles[i.Value];
    }

    void MostrarDet() {
        Ctc? ctc = Seleccionado();
        panelDet.Text = ctc is null ? "Sin contactos." : $"Id: {ctc.Id}\nNombre: {ctc.Nombre}\nTeléfonos: {ctc.Tels}\nEmail: {ctc.Email}\nFavorito: {(ctc.Fav ? "Sí" : "No")}\n\nNotas:\n{ctc.Notas}";
    }

    void Intentar(string accion, Action op) {
        try { op(); } catch (Exception ex) { MessageBox.ErrorQuery(App!, $"Error al {accion}", ex.Message, "Aceptar"); Avisar(ex.Message); }
    }
    void Avisar(string msg) { barEstado.Text = msg; barEstado.SetNeedsDraw(); }
}

public sealed class DialogCtc : Dialog {
    readonly TextField campNombre, campEmail; readonly TextField[] campTels; readonly TextView campNotas; readonly CheckBox chkFav;
    public Ctc? Resultado { get; private set; }

    public DialogCtc(string titulo, Ctc ctc) {
        Title = titulo; Width = Dim.Percent(70); Height = Dim.Percent(80);
        campNombre = Campo("Nombre:", 1, ctc.Nombre);
        campTels = Enumerable.Range(0, 5).Select(i => Campo($"Teléfono {i + 1}:", 3 + i * 2, ObtTel(ctc, i))).ToArray();
        campEmail = Campo("Email:", 13, ctc.Email);
        Add(new Label { Text = "Notas:", X = 2, Y = 15 });
        campNotas = new() { X = 14, Y = 15, Width = Dim.Fill(2), Height = Dim.Fill(4), Text = ctc.Notas, BorderStyle = LineStyle.Single };
        chkFav = new() { Text = "Favorito", X = 14, Y = Pos.Bottom(campNotas) + 1, Value = ctc.Fav ? CheckState.Checked : CheckState.UnChecked };
        Add(campNotas, chkFav);
        Button btnGuardar = new() { Text = "Guardar", IsDefault = true };
        btnGuardar.Accepting += (_, e) => { if (Construir(ctc.Id, out Ctc listo)) { Resultado = listo; App!.RequestStop(); } e.Handled = true; };
        Button btnCancelar = new() { Text = "Cancelar" };
        btnCancelar.Accepting += (_, e) => { Resultado = null; App!.RequestStop(); e.Handled = true; };
        AddButton(btnCancelar); AddButton(btnGuardar);
    }

    TextField Campo(string texto, int y, string valor) {
        Add(new Label { Text = texto, X = 2, Y = y });
        TextField t = new() { X = 14, Y = y, Width = Dim.Fill(2), Text = valor };
        Add(t); return t;
    }

    bool Construir(int id, out Ctc ctc) {
        ctc = new Ctc();
        string nom = campNombre.Text?.ToString().Trim() ?? "", mail = campEmail.Text?.ToString().Trim() ?? "";
        if (nom == "") { MessageBox.ErrorQuery(App!, "Validación", "El nombre no puede estar vacío.", "Aceptar"); return false; }
        if (mail != "" && !mail.Contains('@')) { MessageBox.ErrorQuery(App!, "Validación", "El email debe contener @.", "Aceptar"); return false; }
        ctc = new() { Id = id, Nombre = nom, Email = mail, Notas = campNotas.Text?.ToString() ?? "", Fav = chkFav.Value == CheckState.Checked,
            Tels = string.Join(", ", campTels.Select(x => x.Text?.ToString().Trim()).Where(x => !string.IsNullOrWhiteSpace(x))) };
        return true;
    }
    static string ObtTel(Ctc ctc, int i) {
        string[] partes = ctc.Tels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return i < partes.Length ? partes[i] : "";
    }
}

public sealed class SqliteAlmacenCtc : IDisposable {
    readonly SqliteConnection cn;
    public SqliteAlmacenCtc(string arch) { cn = new(new SqliteConnectionStringBuilder { DataSource = arch }.ConnectionString); cn.Open(); }
    public void CrearTablas() => cn.Execute("""
        CREATE TABLE IF NOT EXISTS Contactos(
            Id INTEGER PRIMARY KEY AUTOINCREMENT, Nombre TEXT NOT NULL,
            Tels TEXT NOT NULL DEFAULT '', Email TEXT NOT NULL DEFAULT '',
            Notas TEXT NOT NULL DEFAULT '', Fav INTEGER NOT NULL DEFAULT 0);
        """);
    public IEnumerable<Ctc> ObtenerTodos() => cn.GetAll<Ctc>();
    public Ctc Agregar(Ctc ctc) { Validar(ctc); ctc.Id = 0; ctc.Id = Convert.ToInt32(cn.Insert(ctc)); return ctc; }
    public void Modificar(Ctc ctc) { Validar(ctc); cn.Update(ctc); }
    public void Borrar(Ctc ctc) => cn.Delete(ctc);
    public void Dispose() => cn.Dispose();
    static void Validar(Ctc ctc) {
        if (string.IsNullOrWhiteSpace(ctc.Nombre)) throw new InvalidOperationException("El nombre no puede estar vacío.");
        if (!string.IsNullOrWhiteSpace(ctc.Email) && !ctc.Email.Contains('@')) throw new InvalidOperationException("El email debe contener @.");
    }
}

public static class JsonCtcIO {
    static readonly System.Text.Json.JsonSerializerOptions Opc = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static IEnumerable<Ctc> Leer(string ruta) {
        if (!File.Exists(ruta)) throw new FileNotFoundException("El archivo JSON no existe.", ruta);
        var datos = System.Text.Json.JsonSerializer.Deserialize<List<Ctc>>(File.ReadAllText(ruta, System.Text.Encoding.UTF8), Opc) ?? throw new InvalidDataException("JSON inválido.");
        foreach (Ctc ctc in datos) { ctc.Id = 0; ctc.Nombre ??= ""; ctc.Tels ??= ""; ctc.Email ??= ""; ctc.Notas ??= ""; }
        return datos;
    }
    public static void Escribir(string ruta, IEnumerable<Ctc> contactos) =>
        File.WriteAllText(ruta, System.Text.Json.JsonSerializer.Serialize(contactos, Opc), new System.Text.UTF8Encoding(false));
}

public sealed class DialogRuta : Dialog {
    readonly TextField campo; public string? Ruta { get; private set; }
    public DialogRuta(string titulo, string etiqueta, string boton) {
        Title = titulo; Width = Dim.Percent(65); Height = 7;
        Add(new Label { Text = etiqueta, X = 1, Y = 1 });
        campo = new() { X = 1, Y = 2, Width = Dim.Fill(2) }; Add(campo);
        Button btnOk = new() { Text = boton, IsDefault = true };
        btnOk.Accepting += (_, e) => { Ruta = campo.Text?.ToString().Trim(); App!.RequestStop(); e.Handled = true; };
        Button btnCancel = new() { Text = "Cancelar" };
        btnCancel.Accepting += (_, e) => { Ruta = null; App!.RequestStop(); e.Handled = true; };
        AddButton(btnCancel); AddButton(btnOk);
    }
}

[Table("Contactos")]
public sealed class Ctc {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tels { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Fav { get; set; }
    public Ctc Clonar() => new() { Id = Id, Nombre = Nombre, Tels = Tels, Email = Email, Notas = Notas, Fav = Fav };
}
