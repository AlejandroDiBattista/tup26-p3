#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

/* USING */
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using System;
using System.Data.Common;
using System.Text.Json;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dapper.Contrib.Extensions;

/// ==== 
/// Estes es un archivo de referencia con el esqueleto del proyecto.
/// No es un código de ejemplo, sino el punto de partida para el desarrollo del trabajo práctico. 
/// ====

// Punto de entrada
/* 1- PUNTO DE ENTRADA (proceso de argumentos)*/
string rutadb = args.Length > 0 ? args[0] : "agenda.db";
SqliteAgendaStore bd;
try {
    bd = new SqliteAgendaStore(rutadb);
}
catch (Exception ex) {
    Console.Error.WriteLine($"Error al iniciar la base de datos: {ex.Message}");
    return;
}
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(bd));


// Ventana principal clase AgendaWindow
public sealed class AgendaWindow : Runnable {

        private readonly SqliteAgendaStore _bd = null!;
        private List<Contacto> todos = new();
        private List<Contacto> filtrados = new();

        private ListView lista = null!;
        private TextField txtBuscar = null!;
        private Button btnFav = null!;
        private bool soloFavoritos = false;
        private Label estado = null!;

        private Label lblNom = null!, lblTel = null!, lblMail = null!;
        private TextView txtNotas = null!;

    public AgendaWindow(SqliteAgendaStore bd) {
        _bd = bd;
        Title  = "Agenda - Terminal.Gui";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;
        BuildLayout();
        Cargar();
    }

     private void BuildLayout() {
        MenuBar menu = new() {
            Menus = [
                new MenuBarItem("_Archivo", [
                    new MenuItem("_Importar JSON", "Ctrl+I", Importar),
                    new MenuItem("_Exportar JSON", "Ctrl+E", Exportar),
                    null!, // Separador
                    new MenuItem("_Salir", "Ctrl+Q", SolicitarSalir)
                ]),
                new MenuBarItem("_Contactos",[
                    new MenuItem("_Nuevo","F2 / Ctrl+N", AbrirDialogo),
                    new MenuItem("_Editar","F3 / Enter", Editar),
                    new MenuItem("_Eliminar","Del / Ctrl+D",Borrar)
            ]),
                new MenuBarItem("_Ayuda", [
                    new MenuItem("_Acerca de", "", () => MostrarMsg("Acerca de", "Agenda Terminal - TP3"))
                ])
            ]
        };

        Label etiBuscar = new() { Text = "Buscar:", X = 0, Y = 1 };
        txtBuscar = new TextField() { X = Pos.Right(etiBuscar) + 1, Y = 1, Width = Dim.Percent(30) };
        txtBuscar.TextChanged += (_, _) => Filtrar();

       
        btnFav = new Button() { Text = "[ ] Solo favoritos", X = Pos.Right(txtBuscar) + 2, Y = 1 };
        btnFav.Accepting += (_, e) => {
            soloFavoritos = !soloFavoritos;
            btnFav.Text = soloFavoritos ? "[X] Solo favoritos" : "[ ] Solo favoritos";
            Filtrar();
            e.Handled = true;
        };

        FrameView panelIzq = new() { Title = "Contactos", X = 0, Y = 2, Width = Dim.Percent(40), Height = Dim.Fill(1) };
        lista = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        panelIzq.Add(lista);

        FrameView panelDer = new() { Title = "Detalles del Contacto", X = Pos.Right(panelIzq), Y = 2, Width = Dim.Fill(), Height = Dim.Fill(1) };
        lblNom  = new Label() { X = 1, Y = 1, Width = Dim.Fill() };
        lblTel  = new Label() { X = 1, Y = 3, Width = Dim.Fill() };
        lblMail = new Label() { X = 1, Y = 5, Width = Dim.Fill() };
        txtNotas = new TextView() { X = 1, Y = 8, Width = Dim.Fill() - 1, Height = Dim.Fill(), ReadOnly = true };

        panelDer.Add(lblNom, lblTel, lblMail, new Label() { Text = "Notas:", X = 1, Y = 7 }, txtNotas);

        
        estado = new Label() { Text = " Listo", X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = 1 };

        Add(menu, etiBuscar, txtBuscar, btnFav, panelIzq, panelDer, estado);
    }

    private void Cargar() {
        try {
            todos = _bd.LeerTodos().ToList();
            Filtrar();
        } catch (Exception ex) { 
            Console.Error.WriteLine($"Error al cargar: {ex.Message}");
            throw;
        }
    }
    
    private void Filtrar() {
        string q = txtBuscar.Text?.ToLower() ?? "";

        filtrados = todos.Where(c => {
            if (soloFavoritos && !c.Favorito) return false;
            if (string.IsNullOrEmpty(q)) return true;
            return (c.Nombre?.ToLower().Contains(q) == true) ||
                   (c.Telefonos?.ToLower().Contains(q) == true) ||
                   (c.Email?.ToLower().Contains(q) == true);
        }).ToList();

        
        lista.SetSource<Contacto>(new System.Collections.ObjectModel.ObservableCollection<Contacto>(filtrados));
        VerDetalle();
    }

     private void VerDetalle() {
        
        if(lista.SelectedItem.HasValue && lista.SelectedItem.Value >= 0 && lista.SelectedItem.Value < filtrados.Count) {
            var c = filtrados[lista.SelectedItem.Value];
            lblNom.Text = $"Nombre: {c.Nombre} {(c.Favorito ? "★" : "")}";
            lblTel.Text = $"Teléfonos: {c.Telefonos}";
            lblMail.Text = $"Email: {c.Email}";
            txtNotas.Text = c.Notas ?? "";
        } else {
            lblNom.Text = lblTel.Text = lblMail.Text = txtNotas.Text = "";
        }
    }

     private void MostrarMsg(string titulo, string msj) {
        Dialog diag = new Dialog() { Title = titulo, Width = 50, Height = 8 };
        Button btnOk = new Button() { Text = "Ok", IsDefault = true };
        btnOk.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        diag.Add(new Label() { Text = msj, X = 1, Y = 1 });
        diag.AddButton(btnOk);
        App!.Run(diag);
    }
    private int Confirmar(string titulo, string msj) {
        int result = -1;
        Dialog diag = new Dialog() { Title = titulo, Width = 50, Height = 8 };
        Button btnSi = new Button() { Text = "Sí", IsDefault = true };
        Button btnNo = new Button() { Text = "No" };
        btnSi.Accepting += (_, e) => { result = 0; App!.RequestStop(); e.Handled = true; };
        btnNo.Accepting += (_, e) => { result = 1; App!.RequestStop(); e.Handled = true; };
        diag.Add(new Label() { Text = msj, X = 1, Y = 1 });
        diag.AddButton(btnNo);
        diag.AddButton(btnSi);
        App!.Run(diag);
        return result;
    }


    private void SetEstado(string msj) => estado.Text = $" {msj}";

    private void AbrirDialogo() {
        ContactDialog dialog = new();
        App!.Run(dialog);
        if (dialog.Salida != null) {
            try {
                _bd.Crear(dialog.Salida);
                Cargar();
                SetEstado("Contacto creado.");
            } catch (Exception ex) { MostrarMsg("Error", ex.Message); }
        }
    }
    private void Editar() {
        if (!lista.SelectedItem.HasValue || lista.SelectedItem.Value < 0 || lista.SelectedItem.Value >= filtrados.Count) return;
        var actual = filtrados[lista.SelectedItem.Value];
        var dialog = new ContactDialog(actual);
          App!.Run(dialog);
        
        if (dialog.Salida != null) {
            dialog.Salida.Id = actual.Id;
            try {
                _bd.Modificar(dialog.Salida);
                Cargar();
                SetEstado("Contacto actualizado.");
            } catch (Exception ex) { MostrarMsg("Error", ex.Message); }
        }
    }

    private void Borrar() {
        if (!lista.SelectedItem.HasValue || lista.SelectedItem.Value < 0 || lista.SelectedItem.Value >= filtrados.Count) return;
        var c = filtrados[lista.SelectedItem.Value];
        if (Confirmar("Confirmar", $"¿Eliminar a '{c.Nombre}'?") == 0) {
            try {
                _bd.Borrar(c);
                Cargar();
                SetEstado("Contacto eliminado.");
            } catch (Exception ex) { MostrarMsg("Error", ex.Message); }
        }
    }

    private void Importar() {
        string? ruta = PedirRuta("Importar JSON", "Ruta de origen:");
        if (string.IsNullOrWhiteSpace(ruta)) return;

        try {
            var listaImportados = JsonAgendaIO.Importar(ruta).ToList();
            if (Confirmar("Confirmar", $"¿Importar {listaImportados.Count} contactos?") == 0) {
                foreach (var c in listaImportados) { c.Id = 0; _bd.Crear(c); }
                Cargar();
                SetEstado($"Importados {listaImportados.Count} contactos.");
            }
        } catch (Exception ex) { MostrarMsg("Error", ex.Message); }
    }

    private void Exportar() {
        string? ruta = PedirRuta("Exportar JSON", "Ruta de destino:");
        if (string.IsNullOrWhiteSpace(ruta)) return;
        try {
            JsonAgendaIO.Exportar(todos, ruta);
            SetEstado($"Exportados a {ruta}.");
        } catch (Exception ex) { MostrarMsg("Error", ex.Message); }
    }
    private string? PedirRuta(string titulo, string msj) {
        string? res = null;
        Dialog diag = new() { Title = titulo, Width = 50, Height = 8 };
        TextField txt = new() { X = 1, Y = 3, Width = Dim.Fill(1) };
        Button btnOk = new() { Text = "Ok", IsDefault = true };
        Button btnCan = new() { Text = "Cancelar" };
        
        btnOk.Accepting += (_, e) => { res = txt.Text; App!.RequestStop(); e.Handled = true; };
        btnCan.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        
        diag.Add(new Label() { Text = msj, X = 1, Y = 1 }, txt);
        diag.AddButton(btnCan); diag.AddButton(btnOk);
        App!.Run(diag);
        return res;
    }

    private void SolicitarSalir() {
        App!.RequestStop();
    }
    protected override bool OnKeyDown(Key key) {
        bool handled = false;
        if (key == Key.F2 || key == Key.N.WithCtrl) { AbrirDialogo(); handled = true; }
        else if (key == Key.F3 || (key == Key.Enter && lista.HasFocus)) { Editar(); handled = true; }
        else if (key == Key.DeleteChar || key == Key.D.WithCtrl) { Borrar(); handled = true; }
        else if (key == Key.I.WithCtrl) { Importar(); handled = true; }
        else if (key == Key.E.WithCtrl) { Exportar(); handled = true; }
        else if (key == Key.F4) { txtBuscar.SetFocus(); handled = true; }
        else if (key == Key.Q.WithCtrl) { SolicitarSalir(); handled = true; }

        if (!handled) {
            handled = base.OnKeyDown(key);
        }
        
        
        VerDetalle();
        return handled;
    }
   
}
/* 3- clase contactDialog */
public sealed class ContactDialog : Dialog {
   private TextField txtNom = null!, txtMail = null!;
    private TextField[] txtTels = new TextField[5];
    private TextView txtNotas = null!;
    
    private Button btnFav = null!;
    private bool esFavorito = false;

    public Contacto? Salida { get; private set; }

    public ContactDialog(Contacto? c = null) {
        Title = c == null ? "Nuevo Contacto" : "Editar Contacto";
        Width = 50; 
        Height = 22;

        CrearUI();
        if (c != null) Llenar(c);
    }
    private void CrearUI() {
        int y = 0;
        Add(new Label() { Text = "Nombre:", X = 1, Y = y }); txtNom = new TextField() { X = 12, Y = y++, Width = Dim.Fill(1) };
        Add(new Label() { Text = "Email:", X = 1, Y = y });  txtMail = new TextField() { X = 12, Y = y++, Width = Dim.Fill(1) };

        Add(new Label() { Text = "Teléfonos:", X = 1, Y = y++ });
        for (int i = 0; i < 5; i++) { 
            txtTels[i] = new TextField() { X = 12, Y = y++, Width = Dim.Fill(1) }; 
        }

        Add(new Label() { Text = "Notas:", X = 1, Y = y++ });
        txtNotas = new TextView() { X = 12, Y = y, Width = Dim.Fill(1), Height = 4 }; y += 4;
        
        btnFav = new Button() { Text = "[ ] Es Favorito", X = 12, Y = y };
        btnFav.Accepting += (_, e) => {
            esFavorito = !esFavorito;
            btnFav.Text = esFavorito ? "[X] Es Favorito" : "[ ] Es Favorito";
            e.Handled = true;
        };

        Add(txtNom, txtMail);
        foreach (var t in txtTels) Add(t);
        Add(txtNotas, btnFav);

        Button btnGuardar = new() { Text = "_Guardar", IsDefault = true };
        Button btnCancelar = new() { Text = "_Cancelar" };

        btnGuardar.Accepting += (_, e) => { if (Guardar()) App!.RequestStop(); e.Handled = true; };
        btnCancelar.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };

        AddButton(btnCancelar); AddButton(btnGuardar);
    }

    private void Llenar(Contacto c) {
        txtNom.Text = c.Nombre; txtMail.Text = c.Email; txtNotas.Text = c.Notas; 
        
        esFavorito = c.Favorito;
        btnFav.Text = esFavorito ? "[X] Es Favorito" : "[ ] Es Favorito";

        if (!string.IsNullOrWhiteSpace(c.Telefonos)) {
            var tels = c.Telefonos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Math.Min(tels.Length, 5); i++) txtTels[i].Text = tels[i];
        }
    }

    private void MostrarMsgInterno(string titulo, string msj) {
        Dialog diag = new Dialog() { Title = titulo, Width = 50, Height = 8 };
        Button btnOk = new Button() { Text = "Ok", IsDefault = true };
        btnOk.Accepting += (_, e) => { App!.RequestStop(); e.Handled = true; };
        diag.Add(new Label() { Text = msj, X = 1, Y = 1 });
        diag.AddButton(btnOk);
         App!.Run(diag);
    }

    private bool Guardar() {
        string nom = txtNom.Text?.ToString() ?? "";
        string mail = txtMail.Text?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(nom)) { MostrarMsgInterno("Error", "El Nombre no puede estar vacío."); return false; }
        if (!string.IsNullOrWhiteSpace(mail) && !mail.Contains("@")) { MostrarMsgInterno("Error", "El Email debe contener '@'."); return false; }

        var validos = txtTels.Select(t => t.Text?.ToString() ?? "").Where(t => !string.IsNullOrWhiteSpace(t));

        Salida = new Contacto {
            Nombre = nom, Email = mail, Notas = txtNotas.Text ?? "",
            Favorito = esFavorito,
            Telefonos = string.Join(", ", validos)
        };
        return true;
    }
}

/* 4 - clase sqliteAgendaStore*/
public class SqliteAgendaStore {
    
        private readonly string conStr;

        public SqliteAgendaStore(string ruta) {
            conStr = $"Data Source={ruta}";
            using var db = new SqliteConnection(conStr);
            db.Open();
            db.Execute(@"CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, NOMBRE TEXT NOT NULL,
                Telefonos TEXT, Email TEXT, Notas TEXT, Favorito INTEGER)");
    }
    public IEnumerable<Contacto> LeerTodos() {

     using var db = new SqliteConnection(conStr); return db.GetAll<Contacto>(); }
    public long Crear(Contacto c) { using var db = new SqliteConnection(conStr); return db.Insert(c); }

    public bool Modificar(Contacto c) { using var db = new SqliteConnection(conStr); return db.Update(c); }

    public bool Borrar(Contacto c) { using var db = new SqliteConnection(conStr); return db.Delete(c); }

}


/* 5- clase JsonAgendaIO */
public class JsonAgendaIO {
    private static readonly JsonSerializerOptions opts = new() {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static void Exportar(IEnumerable<Contacto> lista, string ruta) => File.WriteAllText(ruta, JsonSerializer.Serialize(lista, opts));
    public static IEnumerable<Contacto> Importar(string ruta) => 
        File.Exists(ruta) ? JsonSerializer.Deserialize<IEnumerable<Contacto>>(File.ReadAllText(ruta), opts) ?? new List<Contacto>()
        : throw new FileNotFoundException("Archivo no encontrado.");
}       
/* 6 - clase Contacto */
[Table("Contactos")]
public class Contacto {
    [Key] public int    Id        { get; set; }
          public string Nombre    { get; set; } = "";
          public string Telefonos { get; set; } = "";
          public string Email     { get; set; } = "";
          public string Notas     { get; set; } = "";
          public bool   Favorito  { get; set; }

          public Contacto Clone() => (Contacto)this.MemberwiseClone();
          public override string ToString() => $"{(Favorito ?  "★ " : "")}{Nombre} ({Email})";


}