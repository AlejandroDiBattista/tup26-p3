using Terminal.Gui;

namespace AgendaTrabajoPracticoTres;

public sealed class DialogoEdicionContacto : Dialog
{
    private readonly TextField _campoNombreCompleto;
    private readonly TextField _campoListaDeTelefonos;
    private readonly TextField _campoCorreoElectronico;
    private readonly TextView _areaNotasAdicionales;
    private readonly CheckBox _indicadorEsFavorito;
    private readonly Button _botonConfirmar;
    private readonly Button _botonCancelar;
    
    private const string TITULO_DEL_DIALOGO = "Editar Contacto";
    private const string ETIQUETA_NOMBRE = "Nombre (*):";
    private const string ETIQUETA_TELEFONOS = "Teléfonos (separados por coma, máximo 5):";
    private const string ETIQUETA_EMAIL = "Correo Electrónico:";
    private const string ETIQUETA_NOTAS = "Notas Adicionales:";
    private const string ETIQUETA_FAVORITO = "Marcar como favorito";
    private const string TEXTO_BOTON_CONFIRMAR = "Aceptar";
    private const string TEXTO_BOTON_CANCELAR = "Cancelar";
    private const string TITULO_ERROR_VALIDACION = "Error de validación";
    private const string MENSAJE_ERROR_NOMBRE_VACIO = "El nombre completo es obligatorio y no puede estar vacío.";
    private const string MENSAJE_ERROR_EMAIL_INVALIDO = "El correo electrónico debe contener el símbolo '@'.";
    private const string MENSAJE_ERROR_DEMASIADOS_TELEFONOS = "No pueden ingresarse más de 5 números de teléfono.";
    
    private const int CANTIDAD_MAXIMA_DE_TELEFONOS = 5;
    private const char SEPARADOR_DE_NUMEROS_TELEFONICOS = ',';
    private const int ANCHO_DE_CAMPOS = 40;
    private const int ANCHO_DE_ETIQUETAS = 20;
    private const int DESPLAZAMIENTO_HORIZONTAL_ETIQUETA = 20;
    private const int ESPACIADO_VERTICAL_ENTRE_CAMPOS = 3;
    private const int ALTURA_DEL_AREA_DE_NOTAS = 5;
    private const int ESPACIADO_DESPUES_DE_NOTAS = 7;
    private const int ESPACIADO_DESPUES_DE_FAVORITO = 2;
    private const int DESPLAZAMIENTO_BOTON_IZQUIERDO = 10;
    private const int DESPLAZAMIENTO_BOTON_DERECHO = 2;
    private const int POSICION_Y_INICIAL = 1;

    public Contacto ContactoResultante { get; private set; }

    public DialogoEdicionContacto() : this(new Contacto()) { }

    public DialogoEdicionContacto(Contacto contactoParaEditar)
    {
        Title = TITULO_DEL_DIALOGO;
        Width = Dim.Percent(60);
        Height = Dim.Percent(65);

        _campoNombreCompleto = CrearCampoTexto(contactoParaEditar.NombreCompleto);
        _campoListaDeTelefonos = CrearCampoTexto(contactoParaEditar.ListaDeTelefonos);
        _campoCorreoElectronico = CrearCampoTexto(contactoParaEditar.CorreoElectronico);
        _areaNotasAdicionales = CrearAreaTextoMultilinea(contactoParaEditar.NotasAdicionales);
        _indicadorEsFavorito = new CheckBox(ETIQUETA_FAVORITO) 
        { 
            Checked = contactoParaEditar.EsFavorito 
        };
        
        _botonConfirmar = new Button(TEXTO_BOTON_CONFIRMAR);
        _botonCancelar = new Button(TEXTO_BOTON_CANCELAR);
        
        _botonConfirmar.Clicked += ManejarConfirmacionDelUsuario;
        _botonCancelar.Clicked += ManejarCancelacionDelUsuario;

        ConstruirInterfazVisual();
    }

    private static TextField CrearCampoTexto(string valorInicial)
    {
        return new TextField(valorInicial) 
        { 
            Width = Dim.Fill(ANCHO_DE_CAMPOS), 
            X = Pos.Center() 
        };
    }

    private static TextView CrearAreaTextoMultilinea(string textoInicial)
    {
        return new TextView 
        { 
            Width = Dim.Fill(ANCHO_DE_CAMPOS), 
            Height = ALTURA_DEL_AREA_DE_NOTAS, 
            Text = textoInicial, 
            X = Pos.Center() 
        };
    }

    private void ConstruirInterfazVisual()
    {
        int posicionVerticalActual = POSICION_Y_INICIAL;
        
        AgregarCampoConEtiqueta(ETIQUETA_NOMBRE, _campoNombreCompleto, ref posicionVerticalActual);
        AgregarCampoConEtiqueta(ETIQUETA_TELEFONOS, _campoListaDeTelefonos, ref posicionVerticalActual);
        AgregarCampoConEtiqueta(ETIQUETA_EMAIL, _campoCorreoElectronico, ref posicionVerticalActual);
        AgregarCampoConEtiqueta(ETIQUETA_NOTAS, _areaNotasAdicionales, ref posicionVerticalActual);
        
        _indicadorEsFavorito.Y = posicionVerticalActual;
        _indicadorEsFavorito.X = Pos.Center();
        Add(_indicadorEsFavorito);
        
        posicionVerticalActual += ESPACIADO_DESPUES_DE_FAVORITO;
        
        _botonConfirmar.X = Pos.Center() - DESPLAZAMIENTO_BOTON_IZQUIERDO;
        _botonConfirmar.Y = posicionVerticalActual;
        _botonCancelar.X = Pos.Center() + DESPLAZAMIENTO_BOTON_DERECHO;
        _botonCancelar.Y = posicionVerticalActual;
        
        Add(_botonConfirmar, _botonCancelar);
    }

    private void AgregarCampoConEtiqueta(string textoDeEtiqueta, View campo, ref int posicionVertical)
    {
        Label etiqueta = new Label(textoDeEtiqueta)
        {
            X = Pos.Center() - DESPLAZAMIENTO_HORIZONTAL_ETIQUETA,
            Y = posicionVertical,
            Width = ANCHO_DE_ETIQUETAS,
            TextAlignment = Terminal.Gui.TextAlignment.Right
        };
        Add(etiqueta);
        
        campo.Y = posicionVertical;
        Add(campo);
        
        posicionVertical += ESPACIADO_VERTICAL_ENTRE_CAMPOS;
    }

    private void ManejarConfirmacionDelUsuario()
    {
        bool entradasDelUsuarioSonValidas = ValidarEntradasDelUsuario();
        
        if (!entradasDelUsuarioSonValidas)
        {
            return;
        }

        ContactoResultante = new Contacto
        {
            NombreCompleto = _campoNombreCompleto.Text.Trim(),
            ListaDeTelefonos = _campoListaDeTelefonos.Text.Trim(),
            CorreoElectronico = _campoCorreoElectronico.Text.Trim(),
            NotasAdicionales = _areaNotasAdicionales.Text.ToString(),
            EsFavorito = _indicadorEsFavorito.Checked
        };

        Application.RequestStop();
    }

    private void ManejarCancelacionDelUsuario()
    {
        ContactoResultante = null;
        Application.RequestStop();
    }

    private bool ValidarEntradasDelUsuario()
    {
        string nombreIngresado = _campoNombreCompleto.Text.Trim();
        bool nombreEstaVacio = string.IsNullOrWhiteSpace(nombreIngresado);
        
        if (nombreEstaVacio)
        {
            MostrarErrorValidacion(MENSAJE_ERROR_NOMBRE_VACIO);
            return false;
        }

        string emailIngresado = _campoCorreoElectronico.Text.Trim();
        bool emailNoEsVacio = !string.IsNullOrEmpty(emailIngresado);
        bool emailNoContieneArroba = !emailIngresado.Contains('@');
        bool emailEsInvalido = emailNoEsVacio && emailNoContieneArroba;
        
        if (emailEsInvalido)
        {
            MostrarErrorValidacion(MENSAJE_ERROR_EMAIL_INVALIDO);
            return false;
        }

        string telefonosIngresados = _campoListaDeTelefonos.Text.Trim();
        bool hayTelefonosIngresados = !string.IsNullOrEmpty(telefonosIngresados);
        
        if (hayTelefonosIngresados)
        {
            string[] numerosSeparados = telefonosIngresados.Split(SEPARADOR_DE_NUMEROS_TELEFONICOS);
            int cantidadDeNumerosValidos = 0;
            
            foreach (string numeroActual in numerosSeparados)
            {
                bool numeroNoEstaVacio = !string.IsNullOrWhiteSpace(numeroActual);
                
                if (numeroNoEstaVacio)
                {
                    cantidadDeNumerosValidos++;
                }
            }
            
            bool excedeLimiteMaximo = cantidadDeNumerosValidos > CANTIDAD_MAXIMA_DE_TELEFONOS;
            
            if (excedeLimiteMaximo)
            {
                MostrarErrorValidacion(MENSAJE_ERROR_DEMASIADOS_TELEFONOS);
                return false;
            }
        }

        return true;
    }

    private static void MostrarErrorValidacion(string mensajeDeError)
    {
        MessageBox.ErrorQuery(TITULO_ERROR_VALIDACION, mensajeDeError, "OK");
    }
}