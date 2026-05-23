using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgendaTrabajoPracticoTres;

public static class ManejadorDeArchivosJson
{
    private static readonly JsonSerializerOptions ConfiguracionDeSerializacion = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string MENSAJE_ERROR_ARCHIVO_NO_ENCONTRADO = "El archivo '{0}' no existe.";
    private const string MENSAJE_ERROR_FORMATO_INVALIDO = "El archivo JSON no contiene una lista válida de contactos.";
    private const string MENSAJE_ERROR_JSON_INVALIDO = "Formato JSON inválido: {0}";

    public static async Task<List<Contacto>> ImportarContactosDesdeJsonAsync(string rutaDelArchivo)
    {
        bool archivoNoExisteEnDisco = !File.Exists(rutaDelArchivo);
        
        if (archivoNoExisteEnDisco)
        {
            string mensajeDeError = string.Format(MENSAJE_ERROR_ARCHIVO_NO_ENCONTRADO, rutaDelArchivo);
            throw new FileNotFoundException(mensajeDeError);
        }

        string contenidoJsonDelArchivo = await File.ReadAllTextAsync(rutaDelArchivo);
        
        try
        {
            List<Contacto> contactosDeserializados = JsonSerializer.Deserialize<List<Contacto>>(
                contenidoJsonDelArchivo, 
                ConfiguracionDeSerializacion);
            
            bool deserializacionFalloCompletamente = contactosDeserializados == null;
            
            if (deserializacionFalloCompletamente)
            {
                throw new InvalidOperationException(MENSAJE_ERROR_FORMATO_INVALIDO);
            }
            
            foreach (Contacto contactoActual in contactosDeserializados)
            {
                contactoActual.Identificador = 0;
            }
            
            return contactosDeserializados;
        }
        catch (JsonException excepcionJson)
        {
            string mensajeDeError = string.Format(MENSAJE_ERROR_JSON_INVALIDO, excepcionJson.Message);
            throw new InvalidOperationException(mensajeDeError);
        }
    }

    public static async Task ExportarContactosAJsonAsync(string rutaDelArchivo, List<Contacto> contactosAExportar)
    {
        string contenidoJson = JsonSerializer.Serialize(contactosAExportar, ConfiguracionDeSerializacion);
        await File.WriteAllTextAsync(rutaDelArchivo, contenidoJson);
    }

    public static async Task<int> ObtenerCantidadDeContactosDesdeJsonAsync(string rutaDelArchivo)
    {
        List<Contacto> contactosImportados = await ImportarContactosDesdeJsonAsync(rutaDelArchivo);
        int cantidadDeContactos = contactosImportados.Count;
        return cantidadDeContactos;
    }
}