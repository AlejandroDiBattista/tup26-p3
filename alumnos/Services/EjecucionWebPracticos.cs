using System.Xml.Linq;

namespace Tup26.AlumnosApp;

readonly record struct EjecucionWebResultado(
    bool Iniciada,
    string? Proyecto,
    string? Url,
    Process? Proceso,
    IReadOnlyList<string> Mensajes);

static class EjecucionWebPracticos {
    const int TiempoMaximoArranqueMs = 60_000;

    static readonly Regex regexUrlEscucha = new(@"Now listening on:\s+(https?://\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly string[] patronesErrorArranque = [
        "Unhandled exception",
        "Excepción no controlada",
        "exception has been thrown",
        ": error ",
        "fail:",
        "crit:",
        "fatal"
    ];

    static readonly string[] sufijosTemporalesSqlite = ["-wal", "-shm", "-journal"];

    public static EjecucionWebResultado Iniciar(string rutaPractico, int numeroTp) {
        string? proyecto = SeleccionarProyectoWeb(rutaPractico, numeroTp);
        if (proyecto is null) {
            return new(false, null, null, null, ["No se encontró un proyecto web para ejecutar."]);
        }

        Process? proceso = null;
        try {
            proceso = IniciarAplicacion(rutaPractico, proyecto);
            (bool inicio, string? urlBase, string salida) = EsperarUrlAplicacion(proceso, rutaPractico);
            if (!inicio || string.IsNullOrWhiteSpace(urlBase)) {
                MatarProceso(proceso);
                proceso.Dispose();
                LimpiarTemporalesSqlite(rutaPractico);
                return new(
                    false,
                    Path.GetFileName(proyecto),
                    null,
                    null,
                    ResumirSalida(salida, "La aplicación no informó una URL de escucha."));
            }

            return new(true, Path.GetFileName(proyecto), urlBase, proceso, []);
        } catch (Exception ex) {
            if (proceso is not null) {
                MatarProceso(proceso);
                proceso.Dispose();
            }

            LimpiarTemporalesSqlite(rutaPractico);
            return new(false, Path.GetFileName(proyecto), null, null, [ex.Message]);
        }
    }

    public static bool AbrirNavegador(string url) {
        try {
            if (OperatingSystem.IsWindows()) {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            } else if (OperatingSystem.IsMacOS()) {
                Process.Start("open", url);
            } else {
                Process.Start("xdg-open", url);
            }

            return true;
        } catch {
            return false;
        }
    }

    public static void Detener(Process proceso, string rutaPractico) {
        MatarProceso(proceso);
        proceso.Dispose();
        LimpiarTemporalesSqlite(rutaPractico);
    }

    static string? SeleccionarProyectoWeb(string rutaPractico, int numeroTp) {
        List<string> proyectosWeb = ObjetivosPracticos.Obtener(rutaPractico, numeroTp)
            .Where(ruta => string.Equals(Path.GetExtension(ruta), ".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(EsProyectoWeb)
            .OrderBy(ruta => EstaEnRaizPractico(rutaPractico, ruta) ? 0 : 1)
            .ThenBy(ruta => ruta, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return proyectosWeb.FirstOrDefault();
    }

    static bool EsProyectoWeb(string rutaProyecto) {
        try {
            XDocument documento = XDocument.Load(rutaProyecto);
            string? sdk = documento.Root?.Attribute("Sdk")?.Value;
            return sdk?.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) == true;
        } catch {
            return File.ReadLines(rutaProyecto)
                .Take(5)
                .Any(linea => linea.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase));
        }
    }

    static bool EstaEnRaizPractico(string rutaPractico, string rutaProyecto) {
        string? directorioProyecto = Path.GetDirectoryName(Path.GetFullPath(rutaProyecto));
        return string.Equals(
            directorioProyecto,
            Path.GetFullPath(rutaPractico).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            AppPaths.ComparacionRutas);
    }

    static Process IniciarAplicacion(string rutaPractico, string proyecto) {
        string proyectoRelativo = Path.GetRelativePath(rutaPractico, proyecto);
        ProcessStartInfo startInfo = new() {
            FileName = "dotnet",
            WorkingDirectory = rutaPractico,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(proyectoRelativo);
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("-p:EnableSourceControlManagerQueries=false");

        Process proceso = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar dotnet run.");
        try {
            proceso.StandardInput.Close();
        } catch (InvalidOperationException) {
        }

        return proceso;
    }

    static (bool Inicio, string? UrlBase, string Salida) EsperarUrlAplicacion(Process proceso, string rutaPractico) {
        StringBuilder salida = new();
        object salidaLock = new();
        proceso.OutputDataReceived += (_, e) => AgregarLinea(salida, salidaLock, e.Data);
        proceso.ErrorDataReceived += (_, e) => AgregarLinea(salida, salidaLock, e.Data);
        proceso.BeginOutputReadLine();
        proceso.BeginErrorReadLine();

        DateTime limite = DateTime.UtcNow.AddMilliseconds(TiempoMaximoArranqueMs);
        while (!proceso.HasExited && DateTime.UtcNow < limite) {
            string salidaActual = LimpiarSalida(rutaPractico, ObtenerSalida(salida, salidaLock));
            Match match = regexUrlEscucha.Match(salidaActual);
            if (match.Success) {
                return (true, match.Groups[1].Value.TrimEnd('/'), salidaActual);
            }

            if (ContieneErrorArranque(salidaActual)) {
                return (false, null, salidaActual);
            }

            Thread.Sleep(100);
        }

        return (false, null, LimpiarSalida(rutaPractico, ObtenerSalida(salida, salidaLock)));
    }

    static void AgregarLinea(StringBuilder salida, object salidaLock, string? linea) {
        if (linea is null) {
            return;
        }

        lock (salidaLock) {
            salida.AppendLine(linea);
        }
    }

    static string ObtenerSalida(StringBuilder salida, object salidaLock) {
        lock (salidaLock) {
            return salida.ToString();
        }
    }

    static bool ContieneErrorArranque(string salida) =>
        patronesErrorArranque.Any(patron => salida.Contains(patron, StringComparison.OrdinalIgnoreCase));

    static string LimpiarSalida(string rutaPractico, string salida) {
        string prefijoPractico = Path.GetFullPath(rutaPractico).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (prefijoPractico.StartsWith("/var/", StringComparison.Ordinal)) {
            salida = salida.Replace(
                "/private" + prefijoPractico,
                string.Empty,
                StringComparison.Ordinal);
        }

        return salida.Replace(prefijoPractico, string.Empty, StringComparison.Ordinal);
    }

    static IReadOnlyList<string> ResumirSalida(string salida, string mensajePorDefecto) {
        string[] lineas = salida
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> errores = lineas
            .Where(linea =>
                linea.Contains(": error ", StringComparison.OrdinalIgnoreCase) ||
                linea.StartsWith("fail:", StringComparison.OrdinalIgnoreCase) ||
                linea.StartsWith("crit:", StringComparison.OrdinalIgnoreCase) ||
                linea.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        if (errores.Count > 0) {
            return errores;
        }

        return lineas
            .Where(linea => !string.IsNullOrWhiteSpace(linea))
            .TakeLast(3)
            .DefaultIfEmpty(mensajePorDefecto)
            .ToList();
    }

    static void MatarProceso(Process proceso) {
        try {
            if (!proceso.HasExited) {
                proceso.Kill(entireProcessTree: true);
                proceso.WaitForExit();
            }
        } catch (InvalidOperationException) {
        }
    }

    static void LimpiarTemporalesSqlite(string rutaPractico) {
        if (!Directory.Exists(rutaPractico)) {
            return;
        }

        foreach (string archivo in Directory.EnumerateFiles(rutaPractico, "*", SearchOption.AllDirectories)) {
            if (!sufijosTemporalesSqlite.Any(sufijo => archivo.EndsWith(sufijo, StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            try {
                File.Delete(archivo);
            } catch {
            }
        }
    }
}
