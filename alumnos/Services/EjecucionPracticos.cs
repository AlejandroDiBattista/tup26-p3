namespace Tup26.AlumnosApp;

enum EstadoEjecucionObjetivo {
    Correcto,
    Error,
    Omitido
}

readonly record struct EjecucionObjetivoResultado(
    string Objetivo,
    EstadoEjecucionObjetivo Estado,
    IReadOnlyList<string> Mensajes) {

    public bool Exito => Estado is EstadoEjecucionObjetivo.Correcto or EstadoEjecucionObjetivo.Omitido;
    public bool Omitido => Estado == EstadoEjecucionObjetivo.Omitido;
}

readonly record struct EjecucionPracticoResultado(
    bool Exito,
    IReadOnlyList<EjecucionObjetivoResultado> Objetivos) {

    public int Omitidos => Objetivos.Count(objetivo => objetivo.Omitido);
}

static class EjecucionPracticos {
    const int TiempoArranqueMs = 8_000;
    const int TiempoMaximoEjecucionMs = 20_000;

    static readonly string[] patronesError = [
        "Unhandled exception",
        "Excepción no controlada",
        "exception has been thrown",
        "System.",
        ": error ",
        "fail:",
        "crit:",
        "fatal",
        "segmentation fault",
        "stack overflow"
    ];

    static readonly string[] patronesTerminalInteractiva = [
        "terminal",
        "tty",
        "console input",
        "console output",
        "input is not redirected",
        "operation is not supported on this platform",
        "inappropriate ioctl"
    ];

    static readonly string[] patronesArranqueCorrecto = [
        "Now listening on:",
        "Application started",
        "Content root path:",
        "Hosting environment:",
        "listening on"
    ];

    public static EjecucionPracticoResultado Verificar(string rutaPractico, int numeroTp) {
        IReadOnlyList<string> objetivos = ObjetivosPracticos.Obtener(rutaPractico, numeroTp);
        if (objetivos.Count == 0) {
            return new(false, [
                new("sin objetivo", EstadoEjecucionObjetivo.Error, ["No se encontró un proyecto o archivo de entrada para ejecutar."])
            ]);
        }

        List<EjecucionObjetivoResultado> resultados = new();
        foreach (string objetivo in objetivos) {
            resultados.Add(Ejecutar(rutaPractico, objetivo, numeroTp));
        }

        return new(resultados.All(resultado => resultado.Exito), resultados);
    }

    static EjecucionObjetivoResultado Ejecutar(string rutaPractico, string objetivo, int numeroTp) {
        try {
            ProcessStartInfo startInfo = CrearStartInfo(rutaPractico, objetivo, numeroTp);

            using Process proceso = Process.Start(startInfo)
                ?? throw new InvalidOperationException("No se pudo iniciar dotnet run.");
            StringBuilder salida = new();
            object salidaLock = new();
            proceso.OutputDataReceived += (_, e) => AgregarLinea(salida, salidaLock, e.Data);
            proceso.ErrorDataReceived += (_, e) => AgregarLinea(salida, salidaLock, e.Data);
            proceso.BeginOutputReadLine();
            proceso.BeginErrorReadLine();

            try {
                proceso.StandardInput.Close();
            } catch (InvalidOperationException) {
                // La entrada solo se cierra si fue redirigida correctamente.
            }

            DateTime limiteArranque = DateTime.UtcNow.AddMilliseconds(TiempoArranqueMs);
            while (!proceso.HasExited && DateTime.UtcNow < limiteArranque) {
                string salidaActual = ObtenerSalida(salida, salidaLock);
                if (ContieneErrorClaro(salidaActual)) {
                    MatarProceso(proceso);
                    proceso.WaitForExit();
                    return new(
                        Path.GetFileName(objetivo),
                        EstadoEjecucionObjetivo.Error,
                        ResumirErrores(
                            LimpiarSalida(rutaPractico, ObtenerSalida(salida, salidaLock)),
                            "La aplicación informó un error durante el arranque."));
                }

                if (PareceArranqueCorrecto(salidaActual)) {
                    MatarProceso(proceso);
                    return new(Path.GetFileName(objetivo), EstadoEjecucionObjetivo.Correcto, []);
                }

                Thread.Sleep(100);
            }

            if (proceso.HasExited) {
                proceso.WaitForExit();
                string salidaProceso = LimpiarSalida(rutaPractico, ObtenerSalida(salida, salidaLock));

                if (proceso.ExitCode == 0) {
                    return new(Path.GetFileName(objetivo), EstadoEjecucionObjetivo.Correcto, []);
                }

                if (PareceProblemaTerminalInteractiva(salidaProceso)) {
                    return new(
                        Path.GetFileName(objetivo),
                        EstadoEjecucionObjetivo.Omitido,
                        ["No se pudo verificar porque requiere una terminal interactiva."]);
                }

                return new(
                    Path.GetFileName(objetivo),
                    EstadoEjecucionObjetivo.Error,
                    ResumirErrores(salidaProceso, "dotnet run terminó con error sin informar detalles."));
            }

            string salidaArranque = ObtenerSalida(salida, salidaLock);
            if (ContieneErrorClaro(salidaArranque)) {
                MatarProceso(proceso);
                proceso.WaitForExit();
                return new(
                    Path.GetFileName(objetivo),
                    EstadoEjecucionObjetivo.Error,
                    ResumirErrores(
                        LimpiarSalida(rutaPractico, ObtenerSalida(salida, salidaLock)),
                        "La aplicación informó un error durante el arranque."));
            }

            if (DebeTerminarSolo(numeroTp)) {
                if (!proceso.WaitForExit(Math.Max(0, TiempoMaximoEjecucionMs - TiempoArranqueMs))) {
                    MatarProceso(proceso);
                    return new(
                        Path.GetFileName(objetivo),
                        EstadoEjecucionObjetivo.Error,
                        [$"La ejecución no terminó luego de {TiempoMaximoEjecucionMs / 1000} segundos."]);
                }

                proceso.WaitForExit();
                string salidaFinal = LimpiarSalida(rutaPractico, ObtenerSalida(salida, salidaLock));
                if (proceso.ExitCode == 0) {
                    return new(Path.GetFileName(objetivo), EstadoEjecucionObjetivo.Correcto, []);
                }

                return new(
                    Path.GetFileName(objetivo),
                    EstadoEjecucionObjetivo.Error,
                    ResumirErrores(salidaFinal, "dotnet run terminó con error sin informar detalles."));
            } else {
                MatarProceso(proceso);
                return new(Path.GetFileName(objetivo), EstadoEjecucionObjetivo.Correcto, []);
            }
        } catch (Exception ex) {
            return new(Path.GetFileName(objetivo), EstadoEjecucionObjetivo.Error, [ex.Message]);
        }
    }

    static ProcessStartInfo CrearStartInfo(string rutaPractico, string objetivo, int numeroTp) {
        string objetivoRelativo = Path.GetRelativePath(rutaPractico, objetivo);
        bool esProyecto = string.Equals(Path.GetExtension(objetivo), ".csproj", StringComparison.OrdinalIgnoreCase);

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
        startInfo.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";

        startInfo.ArgumentList.Add("run");
        if (esProyecto) {
            startInfo.ArgumentList.Add("--project");
        }

        startInfo.ArgumentList.Add(objetivoRelativo);
        if (esProyecto) {
            startInfo.ArgumentList.Add("--no-launch-profile");
            startInfo.ArgumentList.Add("-p:EnableSourceControlManagerQueries=false");
        }

        IReadOnlyList<string> argumentos = ArgumentosEjecucion(numeroTp);
        if (argumentos.Count > 0) {
            startInfo.ArgumentList.Add("--");
            foreach (string argumento in argumentos) {
                startInfo.ArgumentList.Add(argumento);
            }
        }

        return startInfo;
    }

    static IReadOnlyList<string> ArgumentosEjecucion(int numeroTp) =>
        numeroTp switch {
            1 or 2 => ["--help"],
            6 => ["openai"],
            _ => []
        };

    static bool DebeTerminarSolo(int numeroTp) => numeroTp is 1 or 2;

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

    static void MatarProceso(Process proceso) {
        try {
            if (!proceso.HasExited) {
                proceso.Kill(entireProcessTree: true);
                proceso.WaitForExit();
            }
        } catch (InvalidOperationException) {
        }
    }

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

    static bool ContieneErrorClaro(string salida) =>
        patronesError.Any(patron => salida.Contains(patron, StringComparison.OrdinalIgnoreCase));

    static bool PareceArranqueCorrecto(string salida) =>
        patronesArranqueCorrecto.Any(patron => salida.Contains(patron, StringComparison.OrdinalIgnoreCase));

    static bool PareceProblemaTerminalInteractiva(string salida) =>
        patronesTerminalInteractiva.Any(patron => salida.Contains(patron, StringComparison.OrdinalIgnoreCase));

    static IReadOnlyList<string> ResumirErrores(string salida, string mensajePorDefecto) {
        string[] lineas = salida
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> errores = lineas
            .Where(linea =>
                linea.Contains(": error ", StringComparison.OrdinalIgnoreCase) ||
                linea.StartsWith("error ", StringComparison.OrdinalIgnoreCase) ||
                linea.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase) ||
                linea.Contains("Excepción no controlada", StringComparison.OrdinalIgnoreCase) ||
                linea.StartsWith("fail:", StringComparison.OrdinalIgnoreCase) ||
                linea.StartsWith("crit:", StringComparison.OrdinalIgnoreCase))
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
}
