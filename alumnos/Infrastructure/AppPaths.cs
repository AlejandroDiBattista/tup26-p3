namespace Tup26.AlumnosApp;

static class AppPaths {
    static readonly string dataDirectory = ResolverDirectorioDatos();

    public static string DataDirectory => dataDirectory;

    public static string RepoRoot => Directory.GetParent(DataDirectory)?.FullName ?? DataDirectory;

    public static string ArchivoAlumnos => Path.Combine(DataDirectory, "alumnos.md");

    public static string ArchivoVcf => Path.Combine(DataDirectory, "alumnos.vcf");

    public static string PracticosDirectory => Path.Combine(RepoRoot, "practicos");

    public static string EnunciadosDirectory => Path.Combine(RepoRoot, "enunciados");

    static string ResolverDirectorioDatos() {
        foreach (string candidato in ObtenerCandidatos()) {
            string? encontrado = BuscarDirectorioDatos(candidato);
            if (!string.IsNullOrWhiteSpace(encontrado)) {
                return encontrado;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    static IEnumerable<string> ObtenerCandidatos() {
        string actual = Directory.GetCurrentDirectory();
        yield return actual;

        string subdirectorioAlumnos = Path.Combine(actual, "alumnos");
        if (Directory.Exists(subdirectorioAlumnos)) {
            yield return subdirectorioAlumnos;
        }

        yield return AppContext.BaseDirectory;
    }

    static string? BuscarDirectorioDatos(string rutaInicial) {
        DirectoryInfo? actual = new DirectoryInfo(rutaInicial);

        while (actual != null) {
            if (EsDirectorioDatos(actual.FullName)) {
                return actual.FullName;
            }

            actual = actual.Parent;
        }

        return null;
    }

    static bool EsDirectorioDatos(string ruta) {
        return File.Exists(Path.Combine(ruta, "alumnos.md")) &&
               File.Exists(Path.Combine(ruta, "Alumnos.csproj"));
    }
}
