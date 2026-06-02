using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui;
string databasePath = "agenda.db";
string[] argumentosLineaComandos = Environment.GetCommandLineArgs();

for (int indiceArgumento = 1; indiceArgumento < argumentosLineaComandos.Length; indiceArgumento++) {
    bool esIndicadorDeArchivo = argumentosLineaComandos[indiceArgumento] == "--" && indiceArgumento + 1 < argumentosLineaComandos.Length;
    if (esIndicadorDeArchivo) {
        databasePath = argumentosLineaComandos[indiceArgumento + 1];
        break;
    }
}

Application.Init();
try {
    using var almacenamientoContactos = new SqliteAgendaStore(databasePath);
    var ventanaPrincipal = new AgendaWindow(almacenamientoContactos);
    Application.Run(ventanaPrincipal);
}
catch (Exception excepcion) {
    MessageBox.ErrorQuery("Error crítico", $"No se pudo iniciar la aplicación: {excepcion.Message}", "Salir");
}
finally {
    Application.Shutdown();
}

namespace AgendaT;

public sealed class AgendaWindow : Window {
    private readonly SqliteAgendaStore _almacenamientoContactos;
    private List<Contacto> _todosLosContactos;
    private List<Contacto> _contactosFiltrados;
    private readonly ListView _listaDeContactos;
    private readonly TextField _campoDeBusqueda;
    private readonly TextView _panelDeDetalle;
    private readonly Label _barraDeEstado;
    private bool _filtrarSoloFavoritos;

    private const string TITULO_VENTANA = "Agenda de Contactos";
    private const string ETIQUETA_BUSQUEDA = "Búsqueda:";
    private const string ETIQUETA_LISTA = "Contactos:";
    private const string ETIQUETA_DETALLE = "Detalle:";
    private const string MENSAJE_ESTADO_INICIAL = "Listo. Use F2 para nuevo contacto, Ctrl+Q para salir";
    private const string TITULO_CONFIRMACION_ELIMINACION = "Confirmar eliminación";
    private const string MENSAJE_CONFIRMACION_ELIMINACION = "¿Está seguro que desea eliminar este contacto?";
    private const string TITULO_CONFIRMACION_IMPORTACION = "Confirmar importación";
    private const string MENSAJE_CONFIRMACION_IMPORTACION = "Se van a importar {0} contactos. ¿Desea continuar?";
    private const string TITULO_DIALOGO_EXPORTACION = "Exportar contactos";
    private const string MENSAJE_DIALOGO_EXPORTACION = "Ingrese la ruta del archivo JSON:";
    private const string TITULO_DIALOGO_IMPORTACION = "Importar contactos";
    private const string MENSAJE_DIALOGO_IMPORTACION = "Ingrese la ruta del archivo JSON:";
    private const string TITULO_ERROR = "Error";
    private const string TITULO_ACERCA_DE = "Acerca de AgendaT";
    private const string MENSAJE_ACERCA_DE = "AgendaT v1.0\nAplicación de agenda TUI con persistencia SQLite\nDesarrollada con Terminal.Gui y Dapper";
    private const string NOMBRE_ARCHIVO_EXPORTACION_POR_DEFECTO = "contactos_export.json";
    private const string TEXTO_BOTON_IMPORTAR = "Importar";
    private const string TEXTO_BOTON_EXPORTAR = "Exportar";
    private const string TEXTO_BOTON_CANCELAR = "Cancelar";
    private const string TEXTO_BOTON_SI = "Sí";
    private const string TEXTO_BOTON_NO = "No";
    private const string TEXTO_BOTON_OK = "OK";
    private const string FORMATO_FECHA_HORA = "HH:mm:ss";
    private const string MENSAJE_CONTACTO_CREADO = "Contacto '{0}' creado exitosamente";
    private const string MENSAJE_CONTACTO_ACTUALIZADO = "Contacto '{0}' actualizado exitosamente";
    private const string MENSAJE_CONTACTO_ELIMINADO = "Contacto '{0}' eliminado exitosamente";
    private const string MENSAJE_CONTACTOS_IMPORTADOS = "Importados {0} contactos exitosamente";
    private const string MENSAJE_CONTACTOS_EXPORTADOS = "Exportados {0} contactos a '{1}'";
    private const string MENSAJE_FILTRO_FAVORITOS_ACTIVO = "Filtro: Mostrando solo favoritos";
    private const string MENSAJE_FILTRO_FAVORITOS_INACTIVO = "Filtro: Mostrando todos los contactos";
    private const string MENSAJE_SIN_CONTACTO_SELECCIONADO = "No hay ningún contacto seleccionado";
    private const string MENSAJE_CONTACTOS_CARGADOS = "Contactos cargados correctamente";
    private const string FORMATO_DETALLE_CONTACTO = """
    Nombre: {0}
    Teléfonos: {1}
    Email: {2}
    Favorito: {3}
    
    Notas:
    {4}
    """;
    private const string TEXTO_FAVORITO_SI = "Sí";
    private const string TEXTO_FAVORITO_NO = "No";
    private const string TEXTO_SIN_TELEFONOS = "(ninguno)";
    private const string SEPARADOR_TELEFONOS = ", ";
    private const string INDICADOR_FAVORITO = "★ ";
    private const string ESPACIADOR_LISTA = "  ";

    private const int ESPACIADO_VERTICAL = 3;
    private const int PORCENTAJE_ANCHO_LISTA = 40;
    private const int MARGEN_IZQUIERDO = 1;
    private const int POSICION_Y_INICIAL = 1;
    private const int POSICION_Y_BARRA_ESTADO = 1;
    private const int ANCHO_DIALOGO = 60;
    private const int ALTO_DIALOGO = 10;
    private const int RESPUESTA_CONFIRMACION_SI = 0;

    public AgendaWindow(SqliteAgendaStore almacenamientoContactos) {
        _almacenamientoContactos = almacenamientoContactos;
        _todosLosContactos = new List<Contacto>();
        _contactosFiltrados = new List<Contacto>();
        _filtrarSoloFavoritos = false;

        Title = TITULO_VENTANA;
        Width = Dim.Fill();
        Height = Dim.Fill();

        InicializarBarraDeMenu();
        InicializarComponentesVisuales();
        CargarContactosDesdeBaseDeDatos();
        ConfigurarEventosDeInterfaz();
    }

    private void InicializarBarraDeMenu() {
        var menuArchivo = new MenuBarItem("Archivo", new MenuItem[]
        {
            new MenuItem("Importar JSON", "", ImportarContactosDesdeArchivo, () => true),
            new MenuItem("Exportar JSON", "", ExportarContactosAArchivo, () => true),
            new MenuItem("Salir", "", SalirDeLaAplicacion, () => true)
        });

        var menuContactos = new MenuBarItem("Contactos", new MenuItem[]
        {
            new MenuItem("Nuevo", "", IniciarCreacionDeContacto, () => true),
            new MenuItem("Editar", "", IniciarEdicionDeContacto, () => true),
            new MenuItem("Eliminar", "", IniciarEliminacionDeContacto, () => true)
        });

        var menuVer = new MenuBarItem("Ver", new MenuItem[]
        {
            new MenuItem("Solo favoritos", "", AlternarFiltroDeFavoritos, () => true)
        });

        var menuAyuda = new MenuBarItem("Ayuda", new MenuItem[]
        {
            new MenuItem("Acerca de", "", MostrarInformacionAcercaDe, () => true)
        });

        var barraDeMenu = new MenuBar(new MenuBarItem[] { menuArchivo, menuContactos, menuVer, menuAyuda });
        Add(barraDeMenu);
    }

    private void InicializarComponentesVisuales() {
        var contenedorPrincipal = new FrameView {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            X = 0,
            Y = 1
        };

        var etiquetaDeBusqueda = new Label(ETIQUETA_BUSQUEDA) {
            Y = POSICION_Y_INICIAL,
            X = MARGEN_IZQUIERDO
        };

        _campoDeBusqueda = new TextField("") {
            Width = Dim.Fill(),
            Y = POSICION_Y_INICIAL,
            X = Pos.Right(etiquetaDeBusqueda) + MARGEN_IZQUIERDO
        };

        var etiquetaDeLista = new Label(ETIQUETA_LISTA) {
            Y = POSICION_Y_INICIAL + ESPACIADO_VERTICAL,
            X = MARGEN_IZQUIERDO
        };

        _listaDeContactos = new ListView {
            Width = Dim.Percent(PORCENTAJE_ANCHO_LISTA),
            Height = Dim.Fill() - ESPACIADO_VERTICAL - POSICION_Y_INICIAL,
            Y = POSICION_Y_INICIAL + ESPACIADO_VERTICAL + MARGEN_IZQUIERDO,
            X = MARGEN_IZQUIERDO,
            AllowsMarking = false
        };

        var etiquetaDeDetalle = new Label(ETIQUETA_DETALLE) {
            Y = POSICION_Y_INICIAL + ESPACIADO_VERTICAL,
            X = Pos.Percent(PORCENTAJE_ANCHO_LISTA) + MARGEN_IZQUIERDO
        };

        _panelDeDetalle = new TextView {
            Width = Dim.Fill(),
            Height = Dim.Fill() - ESPACIADO_VERTICAL - POSICION_Y_INICIAL,
            Y = POSICION_Y_INICIAL + ESPACIADO_VERTICAL + MARGEN_IZQUIERDO,
            X = Pos.Percent(PORCENTAJE_ANCHO_LISTA) + MARGEN_IZQUIERDO,
            ReadOnly = true
        };

        _barraDeEstado = new Label(MENSAJE_ESTADO_INICIAL) {
            Y = Pos.AnchorEnd(POSICION_Y_BARRA_ESTADO),
            Width = Dim.Fill(),
            ColorScheme = Colors.Menu
        };

        contenedorPrincipal.Add(etiquetaDeBusqueda, _campoDeBusqueda, etiquetaDeLista, _listaDeContactos, etiquetaDeDetalle, _panelDeDetalle);
        Add(contenedorPrincipal, _barraDeEstado);
    }

    private void ConfigurarEventosDeInterfaz() {
        _listaDeContactos.OpenSelectedItem += ManejarSeleccionDeContacto;
        _listaDeContactos.SelectedItemChanged += ManejarSeleccionDeContacto;
        _campoDeBusqueda.TextChanged += ManejarCambioEnTextoDeBusqueda;

        ConfigurarAtajosDeTeclado();
    }

    private void ConfigurarAtajosDeTeclado() {
        this.KeyPress += ManejarPresionDeTecla;
    }

    private void ManejarPresionDeTecla(KeyEventEventArgs argumentosEventoTecla) {
        bool esAtajoNuevoContacto = argumentosEventoTecla.KeyEvent.Key == Key.F2 ||
                                     (argumentosEventoTecla.KeyEvent.Key == Key.N && argumentosEventoTecla.KeyEvent.Ctrl);

        bool esAtajoEditarContacto = argumentosEventoTecla.KeyEvent.Key == Key.F3 ||
                                      argumentosEventoTecla.KeyEvent.Key == Key.Enter;

        bool esAtajoEliminarContacto = argumentosEventoTecla.KeyEvent.Key == Key.Delete ||
                                        (argumentosEventoTecla.KeyEvent.Key == Key.D && argumentosEventoTecla.KeyEvent.Ctrl);

        bool esAtajoImportarContactos = argumentosEventoTecla.KeyEvent.Key == Key.I && argumentosEventoTecla.KeyEvent.Ctrl;

        bool esAtajoExportarContactos = argumentosEventoTecla.KeyEvent.Key == Key.E && argumentosEventoTecla.KeyEvent.Ctrl;

        bool esAtajoFocoEnBusqueda = argumentosEventoTecla.KeyEvent.Key == Key.F4;

        bool esAtajoSalirAplicacion = argumentosEventoTecla.KeyEvent.Key == Key.Q && argumentosEventoTecla.KeyEvent.Ctrl;

        if (esAtajoNuevoContacto) {
            IniciarCreacionDeContacto();
            argumentosEventoTecla.Handled = true;
            return;
        }

        if (esAtajoEditarContacto) {
            IniciarEdicionDeContacto();
            argumentosEventoTecla.Handled = true;
            return;
        }

        if (esAtajoEliminarContacto) {
            IniciarEliminacionDeContacto();
            argumentosEventoTecla.Handled = true;
            return;
        }

        if (esAtajoImportarContactos) {
            ImportarContactosDesdeArchivo();
            argumentosEventoTecla.Handled = true;
            return;
        }

        if (esAtajoExportarContactos) {
            ExportarContactosAArchivo();
            argumentosEventoTecla.Handled = true;
            return;
        }

        if (esAtajoFocoEnBusqueda) {
            _campoDeBusqueda.SetFocus();
            argumentosEventoTecla.Handled = true;
            return;
        }

        if (esAtajoSalirAplicacion) {
            SalirDeLaAplicacion();
            argumentosEventoTecla.Handled = true;
            return;
        }
    }

    private async void CargarContactosDesdeBaseDeDatos() {
        try {
            _todosLosContactos = await _almacenamientoContactos.GetAllContactsAsync();
            AplicarFiltrosDeVisualizacion();
            ActualizarBarraDeEstado(MENSAJE_CONTACTOS_CARGADOS);
        }
        catch (Exception excepcion) {
            MostrarMensajeDeError($"Error al cargar contactos: {excepcion.Message}");
        }
    }

    private void AplicarFiltrosDeVisualizacion() {
        string textoDeBusqueda = _campoDeBusqueda.Text.Trim().ToLower();
        _contactosFiltrados = new List<Contacto>();

        foreach (Contacto contacto in _todosLosContactos) {
            bool pasaFiltroDeFavoritos = _filtrarSoloFavoritos ? contacto.Favorito : true;

            bool pasaFiltroDeBusqueda = string.IsNullOrEmpty(textoDeBusqueda);

            if (!pasaFiltroDeBusqueda) {
                bool nombreCoincide = contacto.Nombre.ToLower().Contains(textoDeBusqueda);
                bool telefonosCoinciden = contacto.Telefonos.ToLower().Contains(textoDeBusqueda);
                bool emailCoincide = contacto.Email.ToLower().Contains(textoDeBusqueda);

                pasaFiltroDeBusqueda = nombreCoincide || telefonosCoinciden || emailCoincide;
            }

            bool contactoDebeSerMostrado = pasaFiltroDeFavoritos && pasaFiltroDeBusqueda;

            if (contactoDebeSerMostrado) {
                _contactosFiltrados.Add(contacto);
            }
        }

        RefrescarListaDeContactos();
    }

    private void RefrescarListaDeContactos() {
        List<string> listaVisual = new List<string>();

        foreach (Contacto contacto in _contactosFiltrados) {
            string indicadorVisual = contacto.Favorito ? INDICADOR_FAVORITO : ESPACIADOR_LISTA;
            string textoDeLinea = $"{indicadorVisual}{contacto.Nombre}";
            listaVisual.Add(textoDeLinea);
        }

        _listaDeContactos.SetSource(listaVisual);

        bool hayContactosEnLaLista = _contactosFiltrados.Count > 0;

        if (hayContactosEnLaLista) {
            _listaDeContactos.SelectedItem = 0;
            MostrarDetalleDelContactoEnIndice(0);
        }
        else {
            _panelDeDetalle.Text = "";
        }
    }

    private void ManejarSeleccionDeContacto(ListViewItemEventArgs argumentosSeleccion) {
        bool indiceEsValido = argumentosSeleccion.Item >= 0 && argumentosSeleccion.Item < _contactosFiltrados.Count;

        if (indiceEsValido) {
            MostrarDetalleDelContactoEnIndice(argumentosSeleccion.Item);
        }
    }

    private void MostrarDetalleDelContactoEnIndice(int indiceDelContacto) {
        bool indiceInvalido = indiceDelContacto < 0 || indiceDelContacto >= _contactosFiltrados.Count;

        if (indiceInvalido) {
            return;
        }

        Contacto contactoSeleccionado = _contactosFiltrados[indiceDelContacto];

        string telefonosFormateados = FormatearTelefonosParaVisualizacion(contactoSeleccionado.Telefonos);

        string textoFavorito = contactoSeleccionado.Favorito ? TEXTO_FAVORITO_SI : TEXTO_FAVORITO_NO;

        _panelDeDetalle.Text = string.Format(FORMATO_DETALLE_CONTACTO,
            contactoSeleccionado.Nombre,
            telefonosFormateados,
            contactoSeleccionado.Email,
            textoFavorito,
            contactoSeleccionado.Notas);
    }

    private string FormatearTelefonosParaVisualizacion(string telefonosSinFormato) {
        bool noHayNumerosDeTelefono = string.IsNullOrWhiteSpace(telefonosSinFormato);

        if (noHayNumerosDeTelefono) {
            return TEXTO_SIN_TELEFONOS;
        }

        string[] numerosDeTelefono = telefonosSinFormato.Split(',', StringSplitOptions.RemoveEmptyEntries);

        List<string> numerosLimpios = new List<string>();

        foreach (string numero in numerosDeTelefono) {
            numerosLimpios.Add(numero.Trim());
        }

        return string.Join(SEPARADOR_TELEFONOS, numerosLimpios);
    }

    private void ManejarCambioEnTextoDeBusqueda(NStack.ustring textoActualizado) {
        AplicarFiltrosDeVisualizacion();
    }

    private void AlternarFiltroDeFavoritos() {
        _filtrarSoloFavoritos = !_filtrarSoloFavoritos;
        AplicarFiltrosDeVisualizacion();

        string mensajeEstado = _filtrarSoloFavoritos ? MENSAJE_FILTRO_FAVORITOS_ACTIVO : MENSAJE_FILTRO_FAVORITOS_INACTIVO;
        ActualizarBarraDeEstado(mensajeEstado);
    }

    private async void IniciarCreacionDeContacto() {
        ContactDialog dialogoDeContacto = new ContactDialog();
        Application.Run(dialogoDeContacto);

        bool noSeCreoContacto = dialogoDeContacto.ContactoResultante == null;

        if (noSeCreoContacto) {
            return;
        }

        try {
            int identificadorGenerado = await _almacenamientoContactos.InsertContactAsync(dialogoDeContacto.ContactoResultante);
            dialogoDeContacto.ContactoResultante.Id = identificadorGenerado;
            _todosLosContactos.Add(dialogoDeContacto.ContactoResultante);
            AplicarFiltrosDeVisualizacion();
            string mensajeExito = string.Format(MENSAJE_CONTACTO_CREADO, dialogoDeContacto.ContactoResultante.Nombre);
            ActualizarBarraDeEstado(mensajeExito);
        }
        catch (Exception excepcion) {
            MostrarMensajeDeError($"Error al crear contacto: {excepcion.Message}");
        }
    }

    private bool ExisteContactoSeleccionado() {
        return _listaDeContactos.SelectedItem >= 0 && _listaDeContactos.SelectedItem < _contactosFiltrados.Count;
    }

    private Contacto ObtenerContactoSeleccionado() {
        bool noHayContactoSeleccionado = !ExisteContactoSeleccionado();

        if (noHayContactoSeleccionado) {
            return null;
        }

        return _contactosFiltrados[_listaDeContactos.SelectedItem];
    }

    private async void IniciarEdicionDeContacto() {
        Contacto contactoActual = ObtenerContactoSeleccionado();

        bool noHayContactoParaEditar = contactoActual == null;

        if (noHayContactoParaEditar) {
            MostrarMensajeDeError(MENSAJE_SIN_CONTACTO_SELECCIONADO);
            return;
        }

        ContactDialog dialogoDeEdicion = new ContactDialog(contactoActual.Clone());
        Application.Run(dialogoDeEdicion);

        bool noSeGuardoEdicion = dialogoDeEdicion.ContactoResultante == null;

        if (noSeGuardoEdicion) {
            return;
        }

        dialogoDeEdicion.ContactoResultante.Id = contactoActual.Id;

        try {
            await _almacenamientoContactos.UpdateContactAsync(dialogoDeEdicion.ContactoResultante);

            int indiceDelContacto = _todosLosContactos.FindIndex(contacto => contacto.Id == contactoActual.Id);

            bool contactoEncontradoEnLista = indiceDelContacto >= 0;

            if (contactoEncontradoEnLista) {
                _todosLosContactos[indiceDelContacto] = dialogoDeEdicion.ContactoResultante;
            }

            AplicarFiltrosDeVisualizacion();
            string mensajeExito = string.Format(MENSAJE_CONTACTO_ACTUALIZADO, dialogoDeEdicion.ContactoResultante.Nombre);
            ActualizarBarraDeEstado(mensajeExito);
        }
        catch (Exception excepcion) {
            MostrarMensajeDeError($"Error al actualizar contacto: {excepcion.Message}");
        }
    }

    private async void IniciarEliminacionDeContacto() {
        Contacto contactoAEliminar = ObtenerContactoSeleccionado();

        bool noHayContactoParaEliminar = contactoAEliminar == null;

        if (noHayContactoParaEliminar) {
            MostrarMensajeDeError(MENSAJE_SIN_CONTACTO_SELECCIONADO);
            return;
        }

        string mensajeConfirmacion = $"{MENSAJE_CONFIRMACION_ELIMINACION}\n\nContacto: {contactoAEliminar.Nombre}";
        int respuestaDelUsuario = MessageBox.Query(TITULO_CONFIRMACION_ELIMINACION, mensajeConfirmacion, TEXTO_BOTON_SI, TEXTO_BOTON_NO);

        bool usuarioCanceloEliminacion = respuestaDelUsuario != RESPUESTA_CONFIRMACION_SI;

        if (usuarioCanceloEliminacion) {
            return;
        }

        try {
            await _almacenamientoContactos.DeleteContactAsync(contactoAEliminar.Id);
            _todosLosContactos.RemoveAll(contacto => contacto.Id == contactoAEliminar.Id);
            AplicarFiltrosDeVisualizacion();
            string mensajeExito = string.Format(MENSAJE_CONTACTO_ELIMINADO, contactoAEliminar.Nombre);
            ActualizarBarraDeEstado(mensajeExito);
        }
        catch (Exception excepcion) {
            MostrarMensajeDeError($"Error al eliminar contacto: {excepcion.Message}");
        }
    }

    private async void ImportarContactosDesdeArchivo() {
        string rutaDelArchivo = MostrarDialogoParaSeleccionarArchivo(TITULO_DIALOGO_IMPORTACION, MENSAJE_DIALOGO_IMPORTACION, "");

        bool noSeSeleccionoArchivo = string.IsNullOrWhiteSpace(rutaDelArchivo);

        if (noSeSeleccionoArchivo) {
            return;
        }

        try {
            List<Contacto> contactosParaImportar = await JsonAgendaIO.ImportFromJsonAsync(rutaDelArchivo);

            string mensajeConfirmacion = string.Format(MENSAJE_CONFIRMACION_IMPORTACION, contactosParaImportar.Count);
            int respuestaDelUsuario = MessageBox.Query(TITULO_CONFIRMACION_IMPORTACION, mensajeConfirmacion, TEXTO_BOTON_SI, TEXTO_BOTON_NO);

            bool usuarioCancelóImportacion = respuestaDelUsuario != RESPUESTA_CONFIRMACION_SI;

            if (usuarioCancelóImportacion) {
                return;
            }

            foreach (Contacto contacto in contactosParaImportar) {
                int identificadorGenerado = await _almacenamientoContactos.InsertContactAsync(contacto);
                contacto.Id = identificadorGenerado;
                _todosLosContactos.Add(contacto);
            }

            AplicarFiltrosDeVisualizacion();
            string mensajeExito = string.Format(MENSAJE_CONTACTOS_IMPORTADOS, contactosParaImportar.Count);
            ActualizarBarraDeEstado(mensajeExito);
        }
        catch (Exception excepcion) {
            MostrarMensajeDeError($"Error al importar: {excepcion.Message}");
        }
    }

    private async void ExportarContactosAArchivo() {
        string rutaDelArchivo = MostrarDialogoParaSeleccionarArchivo(TITULO_DIALOGO_EXPORTACION, MENSAJE_DIALOGO_EXPORTACION, NOMBRE_ARCHIVO_EXPORTACION_POR_DEFECTO);

        bool noSeSeleccionoArchivo = string.IsNullOrWhiteSpace(rutaDelArchivo);

        if (noSeSeleccionoArchivo) {
            return;
        }

        try {
            await JsonAgendaIO.ExportToJsonAsync(rutaDelArchivo, _todosLosContactos);
            string mensajeExito = string.Format(MENSAJE_CONTACTOS_EXPORTADOS, _todosLosContactos.Count, rutaDelArchivo);
            ActualizarBarraDeEstado(mensajeExito);
        }
        catch (Exception excepcion) {
            MostrarMensajeDeError($"Error al exportar: {excepcion.Message}");
        }
    }

    private string MostrarDialogoParaSeleccionarArchivo(string tituloDelDialogo, string mensajeDelDialogo, string valorPorDefecto) {
        Dialog dialogoSeleccionArchivo = new Dialog(tituloDelDialogo, ANCHO_DIALOGO, ALTO_DIALOGO);

        TextField campoRutaArchivo = new TextField(valorPorDefecto) {
            Width = Dim.Fill(),
            X = MARGEN_IZQUIERDO,
            Y = POSICION_Y_INICIAL
        };

        dialogoSeleccionArchivo.Add(new Label(mensajeDelDialogo) { Y = 0, X = MARGEN_IZQUIERDO });
        dialogoSeleccionArchivo.Add(campoRutaArchivo);

        string rutaSeleccionada = "";

        Button botonAceptar = new Button(TEXTO_BOTON_IMPORTAR);
        botonAceptar.Clicked += () => {
            rutaSeleccionada = campoRutaArchivo.Text.ToString();
            Application.RequestStop();
        };

        Button botonCancelar = new Button(TEXTO_BOTON_CANCELAR);
        botonCancelar.Clicked += () => {
            Application.RequestStop();
        };

        dialogoSeleccionArchivo.AddButton(botonAceptar);
        dialogoSeleccionArchivo.AddButton(botonCancelar);

        Application.Run(dialogoSeleccionArchivo);

        bool noSeIngresoRuta = string.IsNullOrWhiteSpace(rutaSeleccionada);

        if (noSeIngresoRuta) {
            return null;
        }

        return rutaSeleccionada;
    }

    private void MostrarInformacionAcercaDe() {
        MessageBox.Query(TITULO_ACERCA_DE, MENSAJE_ACERCA_DE, TEXTO_BOTON_OK);
    }

    private void SalirDeLaAplicacion() {
        Application.RequestStop();
    }

    private void ActualizarBarraDeEstado(string mensaje) {
        string marcaDeTiempo = DateTime.Now.ToString(FORMATO_FECHA_HORA);
        _barraDeEstado.Text = $"{mensaje} [{marcaDeTiempo}]";
    }

    private void MostrarMensajeDeError(string mensaje) {
        MessageBox.ErrorQuery(TITULO_ERROR, mensaje, TEXTO_BOTON_OK);
    }
}

[Table("Contactos")]
public sealed class Contacto {
    [Key]
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Telefonos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;
    public bool Favorito { get; set; }

    public Contacto Clone() {
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

public sealed class SqliteAgendaStore : IDisposable {
    private readonly string _cadenaDeConexion;
    private SqliteConnection _conexion;
    private bool _recursosLiberados;

    public SqliteAgendaStore(string rutaBaseDatos) {
        _cadenaDeConexion = $"Data Source={rutaBaseDatos}";
        InicializarEstructuraBaseDatos();
    }

    private void InicializarEstructuraBaseDatos() {
        using SqliteConnection conexionTemporal = new SqliteConnection(_cadenaDeConexion);
        conexionTemporal.Open();

        const string instruccionCreacionTabla = @"
            CREATE TABLE IF NOT EXISTS Contactos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefonos TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Notas TEXT NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            );
            
            CREATE INDEX IF NOT EXISTS idx_contactos_nombre ON Contactos(Nombre);
            CREATE INDEX IF NOT EXISTS idx_contactos_email ON Contactos(Email);
        ";

        conexionTemporal.Execute(instruccionCreacionTabla);
    }

    private SqliteConnection ObtenerConexionActiva() {
        bool recursosYaLiberados = _recursosLiberados;

        if (recursosYaLiberados) {
            throw new ObjectDisposedException(nameof(SqliteAgendaStore));
        }

        SqliteConnection nuevaConexion = new SqliteConnection(_cadenaDeConexion);
        nuevaConexion.Open();
        return nuevaConexion;
    }

    public async Task<List<Contacto>> GetAllContactsAsync() {
        using SqliteConnection conexion = ObtenerConexionActiva();
        IEnumerable<Contacto> contactos = await conexion.GetAllAsync<Contacto>();
        return contactos.ToList();
    }

    public async Task<int> InsertContactAsync(Contacto contacto) {
        using SqliteConnection conexion = ObtenerConexionActiva();
        long identificadorGenerado = await conexion.InsertAsync(contacto);
        return (int)identificadorGenerado;
    }

    public async Task<bool> UpdateContactAsync(Contacto contacto) {
        using SqliteConnection conexion = ObtenerConexionActiva();
        return await conexion.UpdateAsync(contacto);
    }

    public async Task<bool> DeleteContactAsync(int identificador) {
        using SqliteConnection conexion = ObtenerConexionActiva();
        Contacto contactoAEliminar = new Contacto { Id = identificador };
        return await conexion.DeleteAsync(contactoAEliminar);
    }

    public void Dispose() {
        bool recursosNoLiberados = !_recursosLiberados;

        if (recursosNoLiberados) {
            _conexion?.Dispose();
            _recursosLiberados = true;
        }

        GC.SuppressFinalize(this);
    }
}

public static class JsonAgendaIO {
    private static readonly JsonSerializerOptions ConfiguracionSerializacion = new JsonSerializerOptions {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<List<Contacto>> ImportFromJsonAsync(string rutaArchivo) {
        bool archivoNoExiste = !File.Exists(rutaArchivo);

        if (archivoNoExiste) {
            throw new FileNotFoundException($"El archivo '{rutaArchivo}' no existe.");
        }

        string contenidoJson = await File.ReadAllTextAsync(rutaArchivo);

        try {
            List<Contacto> contactosDeserializados = JsonSerializer.Deserialize<List<Contacto>>(contenidoJson, ConfiguracionSerializacion);

            bool deserializacionFallida = contactosDeserializados == null;

            if (deserializacionFallida) {
                throw new InvalidOperationException("El archivo JSON no contiene una lista válida de contactos.");
            }

            foreach (Contacto contacto in contactosDeserializados) {
                contacto.Id = 0;
            }

            return contactosDeserializados;
        }
        catch (JsonException excepcionJson) {
            throw new InvalidOperationException($"Formato JSON inválido: {excepcionJson.Message}");
        }
    }

    public static async Task Export;
    }