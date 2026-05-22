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


string destino = args.FirstOrDefault() ?? ":memory:";
using SqliteAgendaStore agendaStore = new(destino);
agendaStore.Inicializar();
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(agendaStore, destino));

public sealed class AgendaWindow : Runnable {
    readonly SqliteAgendaStore datos; readonly string origen;
    readonly List<Contacto> baseCompleta; readonly List<Contacto> baseFiltrada = [];
    readonly System.Collections.ObjectModel.ObservableCollection<string> lineas = [];
    readonly TextField txtFiltro; readonly ListView listado; readonly TextView panel; readonly Label barra;
    bool favoritosActivos;

    public AgendaWindow(SqliteAgendaStore datos, string origen) {
        this.datos = datos; this.origen = origen; baseCompleta = datos.Obtener().ToList();
        Title = "AgendaT"; Width = Dim.Fill(); Height = Dim.Fill();
        Menu.DefaultBorderStyle = LineStyle.Single;
        MenuBar menu = MenuPrincipal();
        Label etiqueta = new() { Text = "Filtro:", X = 1, Y = 1 };
        txtFiltro = new() { X = Pos.Right(etiqueta) + 1, Y = 1, Width = Dim.Fill(1) };
        txtFiltro.TextChanged += (_, _) => Redibujar();
        listado = new() { X = 0, Y = 3, Width = Dim.Percent(42), Height = Dim.Fill(1), Title = "Agenda", BorderStyle = LineStyle.Single };
        listado.SetSource(lineas);
        listado.ValueChanged += (_, _) => PintarFicha();
        listado.Accepting += (_, e) => { Modificar(); e.Handled = true; };
        panel = new() { X = Pos.Right(listado) + 1, Y = 3, Width = Dim.Fill(), Height = Dim.Fill(1), Title = "Contacto", BorderStyle = LineStyle.Single, CanFocus = false };
        barra = new() { X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Text = "Preparado." };
        Add(menu, etiqueta, txtFiltro, listado, panel, barra);
        Redibujar();
    }
    MenuBar MenuPrincipal() => new() { Menus = [
        new MenuBarItem("_Archivo", [
            new MenuItem("_Importar", "Ctrl+I", CargarJson),
            new MenuItem("_Exportar", "Ctrl+E", GuardarJson),
            null!,
            new MenuItem("_Salir", "Ctrl+Q", () => App!.RequestStop())]),
        new MenuBarItem("_Contactos", [
            new MenuItem("_Alta", "F2 / Ctrl+N", Alta),
            new MenuItem("_Edición", "F3 / Enter", Modificar),
            new MenuItem("_Baja", "Del / Ctrl+D", Baja)]),
        new MenuBarItem("_Ver", [
            new MenuItem("_Favoritos", null!, AlternarFavoritos)]),
        new MenuBarItem("A_yuda", [
            new MenuItem("_Acerca de", null!, () => MessageBox.Query(App!, "AgendaT", $"Base actual: {(origen == ":memory:" ? "memoria" : origen)}", "Aceptar"))])
    ] };

    protected override bool OnKeyDown(Key key) {
        if (key == Key.F2 || key == Key.N.WithCtrl) { Alta(); return true; }
        if (key == Key.F3 || key == Key.Enter) { Modificar(); return true; }
        if (key == Key.Delete || key == Key.D.WithCtrl) { Baja(); return true; }
        if (key == Key.I.WithCtrl) { CargarJson(); return true; }
        if (key == Key.E.WithCtrl) { GuardarJson(); return true; }
        if (key == Key.F4) { txtFiltro.SetFocus(); Mensaje("Filtro listo."); return true; }
        if (key == Key.Q.WithCtrl) { App!.RequestStop(); return true; }
        return base.OnKeyDown(key);
    }
     void Alta() {
        ContactDialog d = new("Alta de contacto", new Contacto());
        App!.Run(d); if (d.Valor is null) return;
        Seguro("agregar", () => {
            Contacto creado = datos.Insertar(d.Valor);
            baseCompleta.Add(creado); Redibujar(creado.Id); Mensaje("Alta realizada.");
        });
    }
 void Modificar() {
        Contacto? actual = ContactoActual(); if (actual is null) { Mensaje("No hay selección."); return; }
        ContactDialog d = new("Edición de contacto", actual.Clone());
        App!.Run(d); if (d.Valor is null) return;
        Seguro("modificar", () => {
            datos.Actualizar(d.Valor);
            int i = baseCompleta.FindIndex(x => x.Id == actual.Id);
            if (i >= 0) baseCompleta[i] = d.Valor;
            Redibujar(d.Valor.Id); Mensaje("Registro actualizado.");
        });
    }

    void Baja() {
        Contacto? actual = ContactoActual(); if (actual is null) { Mensaje("No hay selección."); return; }
        if (MessageBox.Query(App!, "Baja", $"¿Borrar \"{actual.Nombre}\"?", "Borrar", "Cancelar") != 0) return;
        Seguro("borrar", () => { datos.Borrar(actual); baseCompleta.RemoveAll(x => x.Id == actual.Id); Redibujar(); Mensaje("Registro eliminado."); });
    }

void CargarJson() {
        string? archivo = DialogoRuta("Importar JSON", "Ruta:", "Importar"); if (archivo is null) return;
        Seguro("importar", () => {
            var lista = JsonAgendaIO.Importar(archivo).ToList();
            if (MessageBox.Query(App!, "Confirmar importación", $"Contactos a agregar: {lista.Count}", "Continuar", "Cancelar") != 0) return;
            foreach (Contacto c in lista) baseCompleta.Add(datos.Insertar(c));
            Redibujar(); Mensaje("JSON importado.");
        });
    }

    void GuardarJson() {
        string? archivo = DialogoRuta("Exportar JSON", "Guardar en:", "Exportar"); if (archivo is null) return;
        Seguro("exportar", () => { JsonAgendaIO.Exportar(archivo, baseCompleta); Mensaje("JSON exportado."); });
    }

    void AlternarFavoritos() {
        favoritosActivos = !favoritosActivos;
        Redibujar();
        Mensaje(favoritosActivos ? "Mostrando favoritos." : "Mostrando todos.");
    }

    string? DialogoRuta(string titulo, string texto, string ok) {
        PathDialog d = new(titulo, texto, ok);
        App!.Run(d);
        return string.IsNullOrWhiteSpace(d.Texto) ? null : d.Texto;
    }

    void Redibujar(int? id = null) {
        string f = txtFiltro.Text?.ToString() ?? "";
        baseFiltrada.Clear();
        baseFiltrada.AddRange(baseCompleta.Where(c => Acepta(c, f) && (!favoritosActivos || c.Favorito)).OrderBy(c => c.Nombre));
        lineas.Clear();
        foreach (Contacto c in baseFiltrada) lineas.Add($"{(c.Favorito ? "[*]" : "[ ]")} {c.Nombre} <{c.Email}>");
        listado.SetSource(lineas);
        int p = id.HasValue ? baseFiltrada.FindIndex(c => c.Id == id) : 0;
        listado.SelectedItem = baseFiltrada.Count == 0 ? null : Math.Max(0, p);
        PintarFicha();
    }
    static bool Acepta(Contacto c, string f) => string.IsNullOrWhiteSpace(f)
        || c.Nombre.Contains(f, StringComparison.CurrentCultureIgnoreCase)
        || c.Telefonos.Contains(f, StringComparison.CurrentCultureIgnoreCase)
        || c.Email.Contains(f, StringComparison.CurrentCultureIgnoreCase);

    Contacto? ContactoActual() {
        int? pos = listado.SelectedItem;
        return pos is null || pos < 0 || pos >= baseFiltrada.Count ? null : baseFiltrada[pos.Value];
    }

    void PintarFicha() {
        Contacto? c = ContactoActual();
        panel.Text = c is null ? "La agenda está vacía." :
            $"Nombre: {c.Nombre}\nTeléfonos: {c.Telefonos}\nEmail: {c.Email}\nFavorito: {(c.Favorito ? "Sí" : "No")}\n\nNotas:\n{c.Notas}";
    }

    void Seguro(string accion, Action tarea) {
        try { tarea(); } catch (Exception e) { MessageBox.ErrorQuery(App!, $"No se pudo {accion}", e.Message, "Aceptar"); Mensaje(e.Message); }
    }
    void Mensaje(string texto) { barra.Text = texto; barra.SetNeedsDraw(); }
}
