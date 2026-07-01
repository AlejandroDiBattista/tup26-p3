namespace Tup26.AlumnosApp;

static class ObjetivosPracticos {
    static readonly IReadOnlyDictionary<int, string[]> archivosPorPractico =
        new Dictionary<int, string[]> {
            [1] = ["sortx.cs"],
            [3] = ["agenda.cs"],
            [4] = ["servidor.cs", "catalogo.cs"],
            [6] = ["asistente.cs"]
        };

    public static IReadOnlyList<string> Obtener(string rutaPractico, int numeroTp) {
        if (!Directory.Exists(rutaPractico)) {
            return [];
        }

        List<string> proyectos = Directory
            .EnumerateFiles(rutaPractico, "*.csproj", SearchOption.AllDirectories)
            .Where(EsArchivoFuente)
            .OrderBy(ruta => ruta, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (proyectos.Count > 0) {
            return proyectos;
        }

        if (!archivosPorPractico.TryGetValue(numeroTp, out string[]? nombresEsperados)) {
            return [];
        }

        List<string> archivos = Directory
            .EnumerateFiles(rutaPractico, "*.cs", SearchOption.AllDirectories)
            .Where(EsArchivoFuente)
            .ToList();

        return nombresEsperados
            .Select(nombre => archivos.FirstOrDefault(
                ruta => string.Equals(Path.GetFileName(ruta), nombre, StringComparison.OrdinalIgnoreCase)))
            .Where(ruta => ruta is not null)
            .Cast<string>()
            .ToList();
    }

    public static bool EsArchivoFuente(string rutaArchivo) {
        string[] partes = rutaArchivo.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !partes.Any(parte => parte is "bin" or "obj" or ".vs");
    }
}
