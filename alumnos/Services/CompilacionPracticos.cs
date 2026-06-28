namespace Tup26.AlumnosApp;

readonly record struct CompilacionObjetivoResultado(string Objetivo, bool Exito, IReadOnlyList<string> Errores);

readonly record struct CompilacionPracticoResultado(
    bool Exito,
    IReadOnlyList<CompilacionObjetivoResultado> Objetivos);

static class CompilacionPracticos {
    const int TiempoMaximoCompilacionMs = 180_000;

    public static CompilacionPracticoResultado Verificar(string rutaPractico, int numeroTp) {
        IReadOnlyList<string> objetivos = ObjetivosPracticos.Obtener(rutaPractico, numeroTp);
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

    static CompilacionObjetivoResultado Compilar(string rutaPractico, string objetivo) {
        try {
            string objetivoRelativo = Path.GetRelativePath(rutaPractico, objetivo);

            ProcessStartInfo startInfo = new() {
                FileName = "dotnet",
                WorkingDirectory = rutaPractico,
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
            string prefijoPractico = Path.GetFullPath(rutaPractico).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (prefijoPractico.StartsWith("/var/", StringComparison.Ordinal)) {
                salida = salida.Replace(
                    "/private" + prefijoPractico,
                    string.Empty,
                    StringComparison.Ordinal);
            }
            salida = salida.Replace(prefijoPractico, string.Empty, StringComparison.Ordinal);
            bool exito = proceso.ExitCode == 0;
            return new(
                Path.GetFileName(objetivo),
                exito,
                exito ? [] : ResumirErrores(salida));
        } catch (Exception ex) {
            return new(Path.GetFileName(objetivo), false, [ex.Message]);
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

}
