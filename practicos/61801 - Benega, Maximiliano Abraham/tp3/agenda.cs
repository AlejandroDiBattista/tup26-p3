using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text.Json;
using Terminal.Gui;
using Microsoft.Data.Sqlite;
using Dapper;

string rutaBaseDatos = "agenda.db";
try {
    Application.Init();
    var persistencia = new SqliteAgendaStore(rutaBaseDatos);
    var ventanaMain = new AgendaWindow(persistencia);
    Application.Run(ventanaMain);
    Application.Shutdown();
}
catch (Exception fallo) {
    Console.WriteLine($"Error crítico de ejecución: {fallo.Message}");
}

public class SqliteAgendaStore {
    private readonly string _cadenaConexion;

    public SqliteAgendaStore(string rutaArchivo) {
        _cadenaConexion = $"Data Source={rutaArchivo}";
        using var conexion = new SqliteConnection(_cadenaConexion);
        conexion.Execute(@"CREATE TABLE IF NOT EXISTS Contactos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
            Nombre TEXT NOT NULL,
            Telefonos TEXT, 
            Email TEXT, 
            Notas TEXT, 
            Favorito INTEGER)");
    }

    public List<Contacto> ObtenerTodos() {
        using var conexion = new SqliteConnection(_cadenaConexion);
        return conexion.Query<Contacto>("SELECT Id, Nombre, Telefonos, Email, Notas, Favorito FROM Contactos").ToList();
    }

    public void Insertar(Contacto persona) {
        using var conexion = new SqliteConnection(_cadenaConexion);
        string query = @"INSERT INTO Contactos (Nombre, Telefonos, Email, Notas, Favorito) 
                         VALUES (@Nombre, @Telefonos, @Email, @Notas, @Favorito);
                         SELECT last_insert_rowid();";
        persona.Id = conexion.ExecuteScalar<int>(query, persona);
    }

    public void Actualizar(Contacto persona) {
        using var conexion = new SqliteConnection(_cadenaConexion);
        string query = @"UPDATE Contactos SET Nombre = @Nombre, Telefonos = @Telefonos, 
                         Email = @Email, Notas = @Notas, Favorito = @Favorito WHERE Id = @Id";
        conexion.Execute(query, persona);
    }

    public void Eliminar(Contacto persona) {
        using var conexion = new SqliteConnection(_cadenaConexion);
        conexion.Execute("DELETE FROM Contactos WHERE Id = @Id", new { Id = persona.Id });
    }
}

public static class AdministradorJson {
    private static readonly JsonSerializerOptions opciones = new() {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void SerializarYGuardar(string path, IEnumerable<Contacto> lista) => 
        File.WriteAllText(path, JsonSerializer.Serialize(lista, opciones));

    public static List<Contacto> LeerDeDisco(string path) => 
        File.Exists(path) ? JsonSerializer.Deserialize<List<Contacto>>(File.ReadAllText(path), opciones) ?? new() : throw new FileNotFoundException();
}

public class ContactDialog : Window {
    public Contacto DatosResultantes { get; private set; } = new Contacto();
    public bool TransaccionExitosa { get; private set; }

    private readonly Contacto _modeloClonado;
    private readonly TextField _campoNombre, _campoEmail, _campoNotas;
    private readonly List<TextField> _listaCamposTelefonos = new();
    private readonly CheckBox _casillaFavorito;

    public event Action<Contacto>? OnFormularioCompletado;

    public ContactDialog(string encabezado, Contacto original) {
        Title = encabezado; 
        X = Pos.Center(); Y = Pos.Center();
        Width = 65; Height = 18;
        Modal = true;

        DatosResultantes = original;
        _modeloClonado = original;

        var lblNom = new Label("Nombre (*):") { X = 2, Y = 1 };
        _campoNombre = new TextField { X = 16, Y = 1, Width = Dim.Fill(2), Text = original.Nombre ?? "" };

        var lblMail = new Label("Email:") { X = 2, Y = 3 };
        _campoEmail = new TextField { X = 16, Y = 3, Width = Dim.Fill(2), Text = original.Email ?? "" };

        var lblTels = new Label("Telefonos:") { X = 2, Y = 5 };
        var fragmentosTels = (original.Telefonos ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        
        Add(lblNom, _campoNombre, lblMail, _campoEmail, lblTels);

        for (int k = 0; k < 5; k++) {
            var campoIndividual = new TextField { 
                X = 16 + (k * 9), 
                Y = 5, 
                Width = 8, 
                Text = k < fragmentosTels.Length ? fragmentosTels[k] : "" 
            };
            _listaCamposTelefonos.Add(campoIndividual);
            Add(campoIndividual);
        }

        var lblNot = new Label("Notas:") { X = 2, Y = 7 };
        _campoNotas = new TextField { X = 16, Y = 7, Width = Dim.Fill(2), Text = original.Notas ?? "" };

        _casillaFavorito = new CheckBox { Text = "Marcar como Favorito", X = 16, Y = 9, Checked = original.Favorito };

        var botonGuardar = new Button { Text = "Aceptar", X = 15, Y = 12, IsDefault = true };
        var botonCancelar = new Button { Text = "Cancelar", X = 35, Y = 12 };

botonGuardar.Clicked += () => {            // Evaluamos el texto de forma segura usando el operador condicional null
            string txtNombre = _campoNombre.Text?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(txtNombre)) { 
                MessageBox.ErrorQuery("Validación", "El nombre es mandatorio.", "Entendido"); 
                return; 
            }

            string txtEmail = _campoEmail.Text?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(txtEmail) && !txtEmail.Contains("@")) { 
                MessageBox.ErrorQuery("Validación", "Email inválido.", "Entendido"); 
                return; 
            }

            _modeloClonado.Nombre = txtNombre;
            _modeloClonado.Email = txtEmail;
            _modeloClonado.Notas = _campoNotas.Text?.ToString()?.Trim() ?? "";
            _modeloClonado.Favorito = _casillaFavorito.Checked;
            
            _modeloClonado.Telefonos = string.Join(", ", _listaCamposTelefonos
                .Select(c => c.Text?.ToString()?.Trim() ?? "")
                .Where(t => !string.IsNullOrEmpty(t)));

            DatosResultantes = _modeloClonado;
            TransaccionExitosa = true;
            OnFormularioCompletado?.Invoke(DatosResultantes);
            Application.RequestStop();
        };

        botonCancelar.Clicked += () => {
            DatosResultantes = original; 
            TransaccionExitosa = false;
            Application.RequestStop();
        };
        Add(lblNot, _campoNotas, _casillaFavorito, botonGuardar, botonCancelar);
    }
}

public class AgendaWindow : Window {
    private readonly SqliteAgendaStore _db;
    private List<Contacto> _memoriaContactos, _memoriaFiltrada = new();
    private readonly ListView _visorListaNombres;
    private readonly TextView _bloqueFichaInformativa;
    private readonly View _statusMarcador;
    private readonly TextField _barraBusqueda;
    private bool _filtroFavoritoActivado = false;

    public AgendaWindow(SqliteAgendaStore canalPersistencia) {
        _db = canalPersistencia;
        Title = "Mi Agenda de Contactos - Estilo TUI";
        Width = Dim.Fill(); Height = Dim.Fill();

        _memoriaContactos = _db.ObtenerTodos();

        var etiquetaBuscar = new Label("Filtro rápido (F4): ") { X = 2, Y = 1 };
        _barraBusqueda = new TextField { X = Pos.Right(etiquetaBuscar) + 1, Y = 1, Width = Dim.Fill(2) };
        _barraBusqueda.TextChanged += (_) => ReestructurarVistaFiltrada();

        var contenedorIzquierdo = new FrameView { Title = "Contactos registrados", X = 2, Y = 3, Width = Dim.Percent(40), Height = Dim.Fill(2) };
        _visorListaNombres = new ListView { Width = Dim.Fill(), Height = Dim.Fill() };
        _visorListaNombres.SelectedItemChanged += (_) => SincronizarDetalleEnPantalla();
        contenedorIzquierdo.Add(_visorListaNombres);

        var contenedorDerecho = new FrameView { Title = "Datos del contacto seleccionado", X = Pos.Right(contenedorIzquierdo) + 2, Y = 3, Width = Dim.Fill(2), Height = Dim.Fill(2) };
        _bloqueFichaInformativa = new TextView { Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        contenedorDerecho.Add(_bloqueFichaInformativa);

        _statusMarcador = new Label(" [F2] Nuevo | [F3] Editar | [F5] Eliminar | [F6] Favoritos | [F7] Exportar | [F8] Importar ") { 
            X = 0, 
            Y = Pos.AnchorEnd(1), 
            Width = Dim.Fill() 
        };

        Add(etiquetaBuscar, _barraBusqueda, contenedorIzquierdo, contenedorDerecho, _statusMarcador);

        KeyDown += ev => {
            switch (ev.KeyEvent.Key)
            {
                case Key.F2:
                    DispararAltaContacto();
                    ev.Handled = true;
                    break;
                case Key.F3:
                    DispararModificacionContacto();
                    ev.Handled = true;
                    break;
                case Key.F5:
                    DispararBajaContacto();
                    ev.Handled = true;
                    break;
                case Key.F6:
                    _filtroFavoritoActivado = !_filtroFavoritoActivado; 
                    ReestructurarVistaFiltrada();
                    ev.Handled = true;
                    break;
                case Key.F7:
                    DispararExportacion();
                    ev.Handled = true;
                    break;
                case Key.F8:
                    DispararImportacion();
                    ev.Handled = true;
                    break;
            }
        };
        ReestructurarVistaFiltrada();
    }

    private void ReestructurarVistaFiltrada() {
        string criterio = _barraBusqueda.Text?.ToString()?.ToLower() ?? "";
        _memoriaFiltrada = _memoriaContactos.Where(p => 
            (!_filtroFavoritoActivado || p.Favorito) && 
            ((p.Nombre ?? "").ToLower().Contains(criterio) || 
             (p.Telefonos ?? "").ToLower().Contains(criterio) || 
             (p.Email ?? "").ToLower().Contains(criterio))
        ).OrderBy(p => p.Nombre).ToList();

        _visorListaNombres.SetSource(new ObservableCollection<Contacto>(_memoriaFiltrada));
        SincronizarDetalleEnPantalla();
    }

    private void SincronizarDetalleEnPantalla() {
        if (_visorListaNombres.SelectedItem >= 0 && _visorListaNombres.SelectedItem < _memoriaFiltrada.Count) {
            var item = _memoriaFiltrada[_visorListaNombres.SelectedItem];
            _bloqueFichaInformativa.Text = $"NOMBRE COMPLETO: {item.Nombre}\n\nCORREO ELECTRONICO: {item.Email}\n\nTELEFONOS: {item.Telefonos}\n\nES FAVORITO: {(item.Favorito ? "SI [★]" : "NO")}\n\nNOTAS ADICIONALES:\n{item.Notas}";
        } else {
            _bloqueFichaInformativa.Text = "Ningún elemento seleccionado.";
        }
    }

    private void DispararAltaContacto() {
        var subVentana = new ContactDialog("Agregar Nuevo Registro", new Contacto());
        subVentana.OnFormularioCompletado += (nuevoContacto) => {
            _db.Insertar(nuevoContacto);
            _memoriaContactos.Add(nuevoContacto);
            ReestructurarVistaFiltrada();
        };
        Application.Run(subVentana);
    }

    private void DispararModificacionContacto() {
        if (_visorListaNombres.SelectedItem < 0 || _visorListaNombres.SelectedItem >= _memoriaFiltrada.Count) return;
        var objetivo = _memoriaFiltrada[_visorListaNombres.SelectedItem];
        var subVentana = new ContactDialog("Actualizar Datos Existentes", objetivo.ClonarEstructura());
        subVentana.OnFormularioCompletado += (contactoModificado) => {
            _db.Actualizar(contactoModificado);
            int idx = _memoriaContactos.FindIndex(x => x.Id == contactoModificado.Id);
            if (idx >= 0) _memoriaContactos[idx] = contactoModificado;
            ReestructurarVistaFiltrada();
        };
        Application.Run(subVentana);
    }

    private void DispararBajaContacto() {
        if (_visorListaNombres.SelectedItem < 0 || _visorListaNombres.SelectedItem >= _memoriaFiltrada.Count) return;
        var seleccionado = _memoriaFiltrada[_visorListaNombres.SelectedItem];
        if (MessageBox.Query("Confirmar Acción", $"¿Está seguro de remover a {seleccionado.Nombre}?", "Confirmar", "Cancelar") == 0) {
            _db.Eliminar(seleccionado);
            _memoriaContactos.RemoveAll(x => x.Id == seleccionado.Id);
            ReestructurarVistaFiltrada();
        }
    }

    private void DispararExportacion() {
        var ruta = SolicitarRutaModal("Exportar Datos", "Escriba la ubicación del archivo destino (.json):");
        if (string.IsNullOrEmpty(ruta)) return;
        try {
            AdministradorJson.SerializarYGuardar(ruta, _memoriaContactos);
            MessageBox.Query("Operación Exitosa", "Los registros se exportaron sin problemas.", "Cerrar");
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Fallo de E/S", ex.Message, "Cerrar");
        }
    }

    private void DispararImportacion() {
        var ruta = SolicitarRutaModal("Importar Datos", "Escriba la ubicación del archivo origen (.json):");
        if (string.IsNullOrEmpty(ruta)) return;
        try {
            var importados = AdministradorJson.LeerDeDisco(ruta);
            if (MessageBox.Query("Validar Importación", $"¿Proceder con la carga de {importados.Count} contactos?", "Sí", "No") == 0) {
                foreach (var c in importados) {
                    c.Id = 0;
                    _db.Insertar(c);
                    _memoriaContactos.Add(c);
                }
                ReestructurarVistaFiltrada();
            }
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Fallo de E/S", ex.Message, "Cerrar");
        }
    }

    private string SolicitarRutaModal(string titulo, string mensajeInterno) {
        string bufferResultado = "";
        var cuadroModal = new Window { Title = titulo, X = Pos.Center(), Y = Pos.Center(), Width = 55, Height = 8, Modal = true };
        var msg = new Label(mensajeInterno) { X = 1, Y = 1 };
        var input = new TextField { X = 1, Y = 3, Width = Dim.Fill(1) };
        var bOk = new Button { Text = "Aceptar", X = 10, Y = 5, IsDefault = true };
        var bCancel = new Button { Text = "Cancelar", X = 30, Y = 5 };

        bOk.Clicked += () => { bufferResultado = input.Text?.ToString() ?? ""; Application.RequestStop(); };
        bCancel.Clicked += () => { bufferResultado = ""; Application.RequestStop(); };

        cuadroModal.Add(msg, input, bOk, bCancel);
        Application.Run(cuadroModal);
        return bufferResultado;
    }
}

public sealed class Contacto {
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto ClonarEstructura() => (Contacto)MemberwiseClone();
    public override string ToString() => $"{(Favorito ? "[★]" : "[ ]")} {Nombre}";
}