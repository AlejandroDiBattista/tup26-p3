using System;
using System.IO;

public static class Herramientas
{
    public static string ListarArchivos(string ruta)
    {
        try
        {
            string dir = string.IsNullOrWhiteSpace(ruta) ? Directory.GetCurrentDirectory() : ruta;
            if (!Directory.Exists(dir)) return "Error: El directorio especificado no existe.";
            
            string[] elementos = Directory.GetFileSystemEntries(dir);
            return elementos.Length == 0 ? "El directorio está vacío." : string.Join("\n", elementos);
        }
        catch (Exception ex)
        {
            return $"Error al listar archivos: {ex.Message}";
        }
    }

    public static string LeerArchivo(string ruta)
    {
        try
        {
            if (!File.Exists(ruta)) return "Error: El archivo no existe.";
            return File.ReadAllText(ruta);
        }
        catch (Exception ex)
        {
            return $"Error al leer el archivo: {ex.Message}";
        }
    }

    public static string EscribirArchivo(string ruta, string contenido)
    {
        try
        {
            File.WriteAllText(ruta, contenido);
            return $"Archivo escrito con éxito en: {ruta}";
        }
        catch (Exception ex)
        {
            return $"Error al escribir el archivo: {ex.Message}";
        }
    }
}