using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace AsistenteIA.Tools;

public static class FileSystemTools
{
    public static IList<AITool> Create()
    {
        return
        [
            AIFunctionFactory.Create(ReadTextFile, "leer-archivo", "Devuelve el contenido de un archivo de texto."),
            AIFunctionFactory.Create(WriteTextFile, "escribir-archivo", "Crea o sobrescribe un archivo de texto con el contenido indicado."),
            AIFunctionFactory.Create(ListDirectory, "listar-archivos", "Lista los archivos y carpetas de un directorio.")
        ];
    }

    private static string ReadTextFile([Description("Ruta del archivo a leer.")] string ruta)
    {
        try
        {
            var fullPath = Path.GetFullPath(ruta);
            if (!File.Exists(fullPath))
                return $"Error: no existe el archivo '{ruta}'.";

            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            return $"Error al leer '{ruta}': {ex.Message}";
        }
    }

    private static string WriteTextFile(
        [Description("Ruta del archivo a crear o sobrescribir.")] string ruta,
        [Description("Contenido completo que se escribira en el archivo.")] string contenido)
    {
        try
        {
            var fullPath = Path.GetFullPath(ruta);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, contenido);
            return $"Archivo '{ruta}' guardado correctamente.";
        }
        catch (Exception ex)
        {
            return $"Error al escribir '{ruta}': {ex.Message}";
        }
    }

    private static string ListDirectory([Description("Ruta del directorio a listar.")] string ruta)
    {
        try
        {
            var fullPath = Path.GetFullPath(string.IsNullOrWhiteSpace(ruta) ? "." : ruta);
            if (!Directory.Exists(fullPath))
                return $"Error: no existe el directorio '{ruta}'.";

            var directories = Directory.GetDirectories(fullPath)
                .OrderBy(Path.GetFileName)
                .Select(path => $"[dir]  {Path.GetFileName(path)}/");

            var files = Directory.GetFiles(fullPath)
                .OrderBy(Path.GetFileName)
                .Select(path => $"[file] {Path.GetFileName(path)}");

            var entries = directories.Concat(files).ToArray();
            return entries.Length == 0
                ? $"El directorio '{ruta}' esta vacio."
                : string.Join(Environment.NewLine, entries);
        }
        catch (Exception ex)
        {
            return $"Error al listar '{ruta}': {ex.Message}";
        }
    }
}
