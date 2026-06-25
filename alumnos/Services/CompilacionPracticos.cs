namespace Tup26.AlumnosApp;

readonly record struct CompilacionObjetivoResultado(string Objetivo, bool Exito, IReadOnlyList<string> Errores);

readonly record struct CompilacionPracticoResultado(
    bool Exito,
    IReadOnlyList<CompilacionObjetivoResultado> Objetivos);

static class CompilacionPracticos {
    const int TiempoMaximoCompilacionMs = 180_000;

    static readonly IReadOnlyDictionary<int, string[]> archivosPorPractico =
        new Dictionary<int, string[]> {
            [1] = ["sortx.cs"],
            [3] = ["agenda.cs"],
            [4] = ["servidor.cs", "catalogo.cs"],
            [6] = ["asistente.cs"]
        };

    public static CompilacionPracticoResultado Verificar(string rutaPractico, int numeroTp) {
        IReadOnlyList<string> objetivos = ObtenerObjetivos(rutaPractico, numeroTp);
        if (objetivos.Count == 0) {
            return new(false, [
                new("sin objetivo", false, ["No se encontró un proyecto o archivo de entrada para compilar."])
            ]);
        }

        List<CompilacionObjetivoResultado> resultados = new();
        foreach (string objetivo in objetivos) {
            resultados.Add(Compilar(rutaPractico, objetivo));
        }

        return new(resultados.All(resultado => resultado.Exito), resultados);
    }

    static IReadOnlyList<string> ObtenerObjetivos(string rutaPractico, int numeroTp) {
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

    static CompilacionObjetivoResultado Compilar(string rutaPractico, string objetivo) {
        string directorioTemporal = Path.Combine(
            Path.GetTempPath(),
            "tup26-compilacion",
            Guid.NewGuid().ToString("N"));
        string directorioFuente = DirectorioFuenteTemporal(directorioTemporal, rutaPractico);

        try {
            CopiarDirectorioFuente(rutaPractico, directorioFuente);
            string objetivoRelativo = Path.GetRelativePath(rutaPractico, objetivo);

            ProcessStartInfo startInfo = new() {
                FileName = "dotnet",
                WorkingDirectory = directorioFuente,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("build");
            startInfo.ArgumentList.Add(objetivoRelativo);
            startInfo.ArgumentList.Add("--nologo");
            startInfo.ArgumentList.Add("--verbosity");
            startInfo.ArgumentList.Add("quiet");
            startInfo.ArgumentList.Add("-p:EnableSourceControlManagerQueries=false");

            using Process proceso = Process.Start(startInfo)
                ?? throw new InvalidOperationException("No se pudo iniciar dotnet build.");
            Task<string> salidaTask = proceso.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = proceso.StandardError.ReadToEndAsync();

            if (!proceso.WaitForExit(TiempoMaximoCompilacionMs)) {
                proceso.Kill(entireProcessTree: true);
                proceso.WaitForExit();
                return new(Path.GetFileName(objetivo), false, ["La compilación superó los 3 minutos."]);
            }

            Task.WaitAll(salidaTask, errorTask);
            string salida = $"{salidaTask.Result}{Environment.NewLine}{errorTask.Result}";
            string prefijoFuente = directorioFuente + Path.DirectorySeparatorChar;
            if (prefijoFuente.StartsWith("/var/", StringComparison.Ordinal)) {
                salida = salida.Replace(
                    "/private" + prefijoFuente,
                    string.Empty,
                    StringComparison.Ordinal);
            }
            salida = salida.Replace(prefijoFuente, string.Empty, StringComparison.Ordinal);
            bool exito = proceso.ExitCode == 0;
            return new(
                Path.GetFileName(objetivo),
                exito,
                exito ? [] : ResumirErrores(salida));
        } catch (Exception ex) {
            return new(Path.GetFileName(objetivo), false, [ex.Message]);
        } finally {
            try {
                if (Directory.Exists(directorioTemporal)) {
                    Directory.Delete(directorioTemporal, recursive: true);
                }
            } catch {
                // Los artefactos temporales no deben ocultar el resultado de compilación.
            }
        }
    }

    static string DirectorioFuenteTemporal(string directorioTemporal, string rutaPractico) {
        string rutaPracticoNormalizada = rutaPractico.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        string nombrePractico = Path.GetFileName(rutaPracticoNormalizada);
        string? rutaAlumno = Path.GetDirectoryName(rutaPracticoNormalizada);
        string nombreAlumno = string.IsNullOrWhiteSpace(rutaAlumno)
            ? "alumno"
            : Path.GetFileName(rutaAlumno);

        if (string.IsNullOrWhiteSpace(nombreAlumno)) {
            nombreAlumno = "alumno";
        }
        if (string.IsNullOrWhiteSpace(nombrePractico)) {
            nombrePractico = "tp";
        }

        return Path.Combine(directorioTemporal, nombreAlumno, nombrePractico);
    }

    static void CopiarDirectorioFuente(string origen, string destino) {
        Directory.CreateDirectory(destino);

        foreach (string archivo in Directory.EnumerateFiles(origen)) {
            File.Copy(archivo, Path.Combine(destino, Path.GetFileName(archivo)));
        }

        foreach (string subdirectorio in Directory.EnumerateDirectories(origen)) {
            string nombre = Path.GetFileName(subdirectorio);
            if (nombre is "bin" or "obj" or ".vs") {
                continue;
            }

            if (File.GetAttributes(subdirectorio).HasFlag(FileAttributes.ReparsePoint)) {
                continue;
            }

            CopiarDirectorioFuente(subdirectorio, Path.Combine(destino, nombre));
        }
    }

    static IReadOnlyList<string> ResumirErrores(string salida) {
        string[] lineas = salida
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> errores = lineas
            .Where(linea =>
                linea.Contains(": error ", StringComparison.OrdinalIgnoreCase) ||
                linea.StartsWith("error ", StringComparison.OrdinalIgnoreCase))
            .Where(linea => !string.Equals(linea, "ERROR al compilar.", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        if (errores.Count > 0) {
            return errores;
        }

        return lineas
            .Where(linea => !string.IsNullOrWhiteSpace(linea))
            .TakeLast(3)
            .DefaultIfEmpty("dotnet build terminó con error sin informar detalles.")
            .ToList();
    }

    static bool EsArchivoFuente(string rutaArchivo) {
        string[] partes = rutaArchivo.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !partes.Any(parte => parte is "bin" or "obj" or ".vs");
    }
}
