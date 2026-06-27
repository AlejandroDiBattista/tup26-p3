using Microsoft.Extensions.AI;

internal sealed class FileTools
{
    private readonly string _workspaceRoot;

    public FileTools(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    public IReadOnlyList<AITool> CreateTools()
    {
        return
        [
            AIFunctionFactory.Create(
                (Func<string, Task<string>>)LeerArchivoAsync,
                "leer-archivo",
                "Lee el contenido de un archivo de texto. Parámetro: ruta."),
            AIFunctionFactory.Create(
                (Func<string, string, Task<string>>)EscribirArchivoAsync,
                "escribir-archivo",
                "Crea o sobrescribe un archivo de texto. Parámetros: ruta y contenido."),
            AIFunctionFactory.Create(
                (Func<string, Task<string>>)ListarArchivosAsync,
                "listar-archivos",
                "Lista archivos y carpetas de un directorio. Parámetro: ruta.")
        ];
    }

    private async Task<string> LeerArchivoAsync(string ruta)
    {
        try
        {
            var path = ResolvePath(ruta);
            if (!File.Exists(path))
            {
                return $"No existe el archivo: {ruta}";
            }

            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return $"No se pudo leer el archivo '{ruta}': {ex.Message}";
        }
    }

    private async Task<string> EscribirArchivoAsync(string ruta, string contenido)
    {
        try
        {
            var path = ResolvePath(ruta);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, contenido);
            return $"Archivo escrito correctamente: {path}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return $"No se pudo escribir el archivo '{ruta}': {ex.Message}";
        }
    }

    private Task<string> ListarArchivosAsync(string ruta)
    {
        try
        {
            var path = ResolvePath(ruta);
            if (!Directory.Exists(path))
            {
                return Task.FromResult($"No existe el directorio: {ruta}");
            }

            var entries = Directory.EnumerateFileSystemEntries(path)
                .OrderBy(entry => entry)
                .Select(entry => Directory.Exists(entry)
                    ? $"[dir]  {Path.GetFileName(entry)}"
                    : $"[file] {Path.GetFileName(entry)}");

            return Task.FromResult(string.Join(Environment.NewLine, entries));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Task.FromResult($"No se pudo listar el directorio '{ruta}': {ex.Message}");
        }
    }

    private string ResolvePath(string ruta)
    {
        var candidate = string.IsNullOrWhiteSpace(ruta) ? "." : ruta.Trim();
        return Path.GetFullPath(Path.IsPathRooted(candidate)
            ? candidate
            : Path.Combine(_workspaceRoot, candidate));
    }
}
