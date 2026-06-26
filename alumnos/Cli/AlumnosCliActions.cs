using Spectre.Console;

namespace Tup26.AlumnosApp;

static class AlumnosCliActions {

    const double UmbralCopia = 0.90;
    const int MinimoLineasCopia = 20;
    //  60% -> 91
    //  70% -> 64
    //  75% -> 56
    //  80% -> 45
    //  90% -> 41
    //  95% -> 35
    //  99% -> 31
    // 100% -> 24
    public static int Listar() {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.Listar(alumnos);
        return 0;
    }

    public static int ListarTpNoPresentado(string trabajoPractico) {
        int numeroTp = ObtenerNumeroTP(trabajoPractico);
        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        Alumnos alumnos = CargarAlumnos();
        IEnumerable<Alumno> noPresentaron = alumnos
            .Where(TieneAlgunPracticoPresentado)
            .Where(alumno => alumno.EstadoPractico(numeroTp) != Estado.Aprobado);

        AlumnosManager.Listar(noPresentaron, $"Alumnos con TP{numeroTp} faltante");
        return 0;
    }

    public static int NormalizarCarpetas() {
        PrepararCarpetasAlumnos();
        return 0;
    }

    public static int LimpiarProyectosPracticos() {
        PrepararCarpetasAlumnos();
        LimpiezaCompilacionPracticosResultado resultado = AppPaths.LimpiarDirectoriosCompilacionPracticos();
        IReadOnlyList<string> elementosEliminados = resultado.ElementosEliminados;
        IReadOnlyList<string> elementosRestantes  = resultado.ElementosRestantes;

        if (elementosEliminados.Count == 0 && elementosRestantes.Count == 0) {
            Log.Info("No se encontraron carpetas ni cachés de compilación dentro de prácticos, enunciados ni clases.");
            return 0;
        }

        foreach (string ruta in elementosEliminados) {
            Log.Info($"Eliminado: {AppPaths.RutaRelativaDesdeRepo(ruta)}");
        }

        Log.Info($"Total de elementos eliminados: {elementosEliminados.Count}");

        if (elementosRestantes.Count > 0) {
            Log.Warning($"Quedaron o se regeneraron {elementosRestantes.Count} elemento(s) de compilación.");
            foreach (string ruta in elementosRestantes.Take(10)) {
                Log.Warning($"Pendiente: {AppPaths.RutaRelativaDesdeRepo(ruta)}");
            }

            if (elementosRestantes.Count > 10) {
                Log.Warning($"... y {elementosRestantes.Count - 10} más.");
            }

            Log.Warning("Si el número cambia al ejecutar de nuevo, probablemente VS Code/C# Dev Kit está recreando cachés de proyectos.");
        }

        return 0;
    }

    public static int GuardarMarkdown(string? rutaDestino) {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.Escribir(alumnos, ResolverRuta(rutaDestino, AppPaths.ArchivoAlumnos));
        return 0;
    }

    public static int GuardarJson(string? rutaDestino) {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.EscribirJSON(alumnos, ResolverRuta(rutaDestino, AppPaths.ArchivoJsonAlumnos));
        return 0;
    }

    public static int GuardarVcf(string? rutaDestino) {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.EscribirVCard(alumnos, ResolverRuta(rutaDestino, AppPaths.ArchivoVcf));
        return 0;
    }

    public static int PublicarEstadoInformer() {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.EscribirEstadoInformer(alumnos, AppPaths.ArchivoEstadoRepo);
        return 0;
    }

    public static int PublicarPractico(string trabajoPractico, bool forzar) {
        int numeroTp = ObtenerNumeroTP(trabajoPractico);
        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        string carpetaTp = CarpetaTrabajoPractico(numeroTp);
        if (!AppPaths.ExisteEnunciadoPractico(carpetaTp)) {
            Log.Error($"No existe la carpeta del enunciado: {AppPaths.EnunciadoPracticoDirectory(carpetaTp)}");
            return 1;
        }

        Alumnos alumnos = CargarAlumnosConCarpetasPreparadas();
        Log.Info($"Publicando {carpetaTp.ToUpperInvariant()} desde {AppPaths.EnunciadoPracticoDirectory(carpetaTp)}");
        AlumnosManager.PublicarPractico(alumnos, carpetaTp, forzar);
        return 0;
    }

    public static int PublicarApuntes() {
        string script = Path.Combine(AppPaths.ApuntesDirectory, "publicar.py");
        if (!AppPaths.ExisteArchivo(script)) {
            Log.Error($"No existe el script de publicación: {script}");
            return 1;
        }

        try {
            Log.Info("Ejecutando apuntes/publicar.py...");
            ProcessStartInfo startInfo = new() {
                FileName = "python3",
                WorkingDirectory = AppPaths.ApuntesDirectory,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("publicar.py");

            using Process proceso = Process.Start(startInfo)
                ?? throw new InvalidOperationException("No se pudo iniciar publicar.py.");
            proceso.WaitForExit();

            if (proceso.ExitCode != 0) {
                Log.Error($"publicar.py terminó con código {proceso.ExitCode}.");
            }

            return proceso.ExitCode;
        } catch (Exception ex) {
            Log.Error($"No se pudo ejecutar publicar.py: {ex.Message}");
            return 1;
        }
    }

    public static int RevisarPullRequests() {
        (Alumnos alumnos, GitHub gh) = PrepararPullRequests();
        List<(int Numero, string Titulo)> prs = EjecutarConIndicador(
            "Revisar PRs",
            "Consultando pull requests abiertos...",
            actualizarEstado => {
                actualizarEstado("Leyendo pull requests abiertos desde GitHub...");
                return gh.PullRequests();
            });

        foreach (var pr in prs) {
            Log.Info($"Consultando PR #{pr.Numero}: {pr.Titulo}");
            int legajo = GitHub.ExtraerLegajo(pr.Titulo);
            Alumno? alumno = alumnos.BuscarPorLegajo(legajo);

            if (alumno is null) {
                Log.Error($"Alumno con legajo {legajo} no encontrado en la lista de alumnos.");
                continue;
            }

            int cantidadArchivos = gh.ListarArchivos(pr.Numero).Count;
            int cantidadLineas   = gh.CantidadLineas(pr.Numero);
            int cantidadCommits  = gh.Commits(pr.Numero).Count;
            List<int> tps = GitHub.ExtraerTPs(pr.Titulo);
            List<(int Tp, int CantidadArchivos)> archivosPorTp = tps
                .Select(tp => (
                    Tp: tp,
                    CantidadArchivos: gh.ListarArchivosDirectorio(pr.Numero, alumno.CarpetaNombre, $"tp{tp}").Count))
                .ToList();
            string etiquetaTp = tps.Count == 0 ? "?" : string.Join("", tps);

            Log.Print($"PR #{pr.Numero:000} | {legajo} | {alumno.NombreCompleto,-40} | A:{cantidadArchivos,4} | L:{cantidadLineas,4} | C:{cantidadCommits,2} | TP{etiquetaTp}");
            foreach ((int tp, int cantidad) in archivosPorTp) {
                Log.Print($" - TP{tp,2}: {cantidad,2} archivo{(cantidad == 1 ? "" : "s")}");
            }
        }

        return 0;
    }

    public static int BajarPullRequests() {
        (Alumnos alumnos, GitHub gh) = PrepararPullRequests(prepararCarpetas: true);
        List<(int Numero, string Titulo)> prs = EjecutarConIndicador(
            "Bajar PRs",
            "Consultando PRs abiertos...",
            actualizarEstado => {
                actualizarEstado("Leyendo PRs abiertos desde GitHub...");
                return gh.PullRequests();
            });

        if (prs.Count == 0) {
            Log.Error("No se encontraron PRs abiertos para bajar.");
            return 1;
        }

        int indice = 0;
        int procesados = 0;
        int trabajosProcesados = 0;
        int archivosDescargados = 0;
        HashSet<int> trabajosParaRevisar = new();
        Log.WriteLine("[red]Igual al existente  [green]Nuevo  [black]Modificado");
        foreach (var pr in prs) {
            indice++;
            Log.Info($"\nRevisando PR {indice}/{prs.Count}: #{pr.Numero} | {pr.Titulo}");
            int legajo = GitHub.ExtraerLegajo(pr.Titulo);
            if (legajo <= 0) {
                Log.Error($"Se omite PR #{pr.Numero}: no se encontró un legajo válido en el título.");
                continue;
            }

            Alumno? alumno = alumnos.BuscarPorLegajo(legajo);
            if (alumno is null) {
                Log.Error($"Se omite PR #{pr.Numero}: el legajo {legajo} no corresponde a un alumno de alumnos.md.");
                continue;
            }

            BajadaArchivosAlumnoResultado bajada = gh.BajarArchivosAlumno(pr.Numero, alumno, forzar: true);
            if (bajada.Archivos.Count > 0) {
                procesados++;
                trabajosProcesados  += bajada.TrabajosPracticos.Count;
                archivosDescargados += bajada.Archivos.Count;
                trabajosParaRevisar.UnionWith(bajada.Archivos.Select(archivo => archivo.TrabajoPractico));
            }
        }

        const string alcance = "todos los TP detectados";
        Log.Info($"\nResumen: {procesados}/{prs.Count} PR(s) procesados para {alcance}. TPs detectados: {trabajosProcesados}. Archivos bajados: {archivosDescargados}. Sobrescritura: sí");
        if (procesados == 0) {
            Log.Error($"No se encontraron PRs con archivos para {alcance}.");
            return 1;
        }

        Log.Info($"\nRevisando automáticamente los TP descargados: {string.Join(", ", trabajosParaRevisar.Order().Select(tp => $"TP{tp}"))}");
        foreach (int tp in trabajosParaRevisar.Order()) {
            if (RevisarPresentados(tp.ToString()) != 0) {
                return 1;
            }
        }

        return 0;
    }

    public static int CerrarPullRequests() {
        (_, GitHub gh) = PrepararPullRequests();
        List<(int Numero, string Titulo)> prs = EjecutarConIndicador(
            "Cerrar PRs",
            "Consultando todos los PRs abiertos...",
            actualizarEstado => {
                actualizarEstado("Leyendo PRs abiertos desde GitHub...");
                return gh.PullRequests(soloAbiertos: true);
            });

        if (prs.Count == 0) {
            Log.Info("No hay PRs abiertos para cerrar.");
            return 0;
        }

        int cerrados = 0;
        int errores = 0;
        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .Start(ctx => {
                var tarea = ctx.AddTask("Cerrando PRs", maxValue: prs.Count);
                foreach ((int Numero, string Titulo) pr in prs) {
                    tarea.Description = $"Cerrando PR #{pr.Numero}";
                    if (gh.CerrarPR(pr.Numero, informarExito: false)) {
                        cerrados++;
                    } else {
                        errores++;
                    }
                    tarea.Increment(1);
                }
                tarea.Description = "Cerrar PRs";
            });

        Log.Info($"Resumen: {cerrados}/{prs.Count} PRs abiertos cerrados.");
        if (errores > 0) {
            Log.Error($"No se pudieron cerrar {errores} PR(s).");
        }

        return 0;
    }

    public static int RevisarPresentados(string? trabajoPractico) {
        if (string.IsNullOrWhiteSpace(trabajoPractico)) {
            IReadOnlyList<EnunciadoPracticoDisponible> practicos =
                PracticosConfig.FiltrarConfigurados(AppPaths.ListarEnunciadosPracticos());
            if (practicos.Count == 0) {
                Log.Error($"No se encontraron trabajos prácticos con enunciado y criterio de revisión configurado en {AppPaths.EnunciadosDirectory}.");
                return 1;
            }

            Log.Info($"Revisando todos los trabajos prácticos: {string.Join(", ", practicos.Select(practico => $"TP{practico.Numero}"))}");
            foreach (EnunciadoPracticoDisponible practico in practicos) {
                if (RevisarPresentados(practico.Numero.ToString()) != 0) {
                    return 1;
                }
            }

            return 0;
        }

        int numeroTp = ObtenerNumeroTP(trabajoPractico);

        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        if (!PracticosConfig.TryObtener(numeroTp, out ConfiguracionPractico configuracion)) {
            Log.Error($"TP{numeroTp} no tiene un criterio de revisión configurado.");
            return 1;
        }

        Alumnos alumnos = CargarAlumnosConCarpetasPreparadas();
        string carpetaTp = CarpetaTrabajoPractico(numeroTp);
        string rutaEnunciado = AppPaths.EnunciadoPracticoDirectory(carpetaTp);
        int lineasEnunciado = ObtenerLineasBaseEnunciado(numeroTp, carpetaTp, rutaEnunciado, alumnos);
        HashSet<string> lineasCodigoEnunciado = ObtenerLineasCodigoNormalizadas(rutaEnunciado);

        Log.Info($"{carpetaTp.ToUpperInvariant()} | criterio: {configuracion.DescripcionCriterio} | líneas base del enunciado: {lineasEnunciado}");
        List<TrabajoPresentadoLocal> trabajosPresentados = new();
        List<PresentacionCambiada> presentacionesCambiadas = new();
        bool habiaObservaciones = alumnos.Any(alumno => !string.IsNullOrWhiteSpace(alumno.Observaciones));

        Alumno[] alumnosOrdenados = alumnos.OrderBy(alumno => alumno.Legajo).ToArray();
        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .Start(ctx => {
                var tarea = ctx.AddTask($"Revisando presentados TP{numeroTp}", maxValue: alumnosOrdenados.Length);
                foreach (Alumno alumno in alumnosOrdenados) {
                    tarea.Description = $"Revisando {alumno.Legajo}";
                    string rutaPractico = AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp);
                    int lineasTotales   = ContarLineasPracticoLocal(rutaPractico);
                    int lineasAgregadas = Math.Max(0, lineasTotales - lineasEnunciado);

                    // alumno.Observaciones = string.Empty;

                    Estado estadoAnterior = alumno.EstadoPractico(numeroTp);
                    Estado estado = estadoAnterior;
                    if (configuracion.ParecePresentado(lineasTotales, lineasAgregadas)) {
                        estado = Estado.Aprobado;
                        HashSet<string> lineasCodigo = ObtenerLineasCodigoNormalizadas(rutaPractico);
                        lineasCodigo.ExceptWith(lineasCodigoEnunciado);
                        trabajosPresentados.Add(new(alumno, rutaPractico, lineasCodigo));
                    }

                    if (estadoAnterior != estado) {
                        alumno.Practico(numeroTp, estado);
                        presentacionesCambiadas.Add(new(alumno, estadoAnterior, estado, lineasTotales, lineasAgregadas));
                    }
                    tarea.Increment(1);
                }
            });

        int copias = RevisarCopiasTrabajosPresentados(numeroTp, trabajosPresentados);
        Alumno[] alumnosPresentados = alumnos
            .Where(alumno => alumno.EstadoPractico(numeroTp) == Estado.Aprobado)
            .OrderBy(alumno => alumno.Legajo)
            .ToArray();
        int marcados = alumnosPresentados.Length;

        if (presentacionesCambiadas.Count > 0 || trabajosPresentados.Count > 0 || habiaObservaciones) {
            AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        }

        if (presentacionesCambiadas.Count == 0) {
            Log.Warning("No hubo cambios de estado.");
        } else {
            foreach (PresentacionCambiada cambio in presentacionesCambiadas.OrderBy(cambio => cambio.Alumno.Legajo)) {
                Log.Print($"{cambio.Alumno.Legajo} | {cambio.Alumno.NombreCompleto,-40} | L:{cambio.LineasTotales,4} | L+:{cambio.LineasAgregadas,4} | {cambio.EstadoAnterior.ToEmoji()} -> {cambio.EstadoNuevo.ToEmoji()}");
            }
        }

        AlumnosManager.Listar(alumnosPresentados, $"Alumnos con TP{numeroTp} presentado");
        Log.Success($"Resumen TP{numeroTp}: marcados={marcados}, copias={copias}, total={alumnos.Count()}, porcentaje={marcados * 100.0 / alumnos.Count():F2}%");
        return 0;
    }

    public static int VerificarCompilacion(string trabajoPractico) {
        int numeroTp = ObtenerNumeroTP(trabajoPractico);
        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        string carpetaTp = CarpetaTrabajoPractico(numeroTp);
        if (!AppPaths.ExisteEnunciadoPractico(carpetaTp)) {
            Log.Error($"No existe la carpeta del enunciado: {AppPaths.EnunciadoPracticoDirectory(carpetaTp)}");
            return 1;
        }

        Alumnos alumnos = CargarAlumnos();
        Alumno[] entregados = alumnos
            .Where(alumno => alumno.EstadoPractico(numeroTp) == Estado.Aprobado)
            .OrderBy(alumno => alumno.Legajo)
            .ToArray();

        if (entregados.Length == 0) {
            Log.Warning($"No hay trabajos entregados para verificar en TP{numeroTp}.");
            return 0;
        }

        List<CompilacionAlumnoResultado> resultados = new();
        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .Start(ctx => {
                var tarea = ctx.AddTask($"Compilando TP{numeroTp}", maxValue: entregados.Length);
                foreach (Alumno alumno in entregados) {
                    tarea.Description = $"Compilando {alumno.Legajo}";
                    string rutaPractico = AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp);
                    CompilacionPracticoResultado compilacion =
                        CompilacionPracticos.Verificar(rutaPractico, numeroTp);

                    if (!compilacion.Exito) {
                        alumno.Practico(numeroTp, Estado.Revision);
                    }

                    resultados.Add(new(alumno, compilacion));
                    tarea.Increment(1);
                }
            });

        int correctos = resultados.Count(resultado => resultado.Compilacion.Exito);
        int errores = resultados.Count - correctos;

        if (errores > 0) {
            AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        }

        foreach (CompilacionAlumnoResultado resultado in resultados) {
            if (resultado.Compilacion.Exito) {
                string objetivos = string.Join(", ", resultado.Compilacion.Objetivos.Select(objetivo => objetivo.Objetivo));
                Log.Success($"{resultado.Alumno.Legajo} | {resultado.Alumno.NombreCompleto,-40} | compila: {objetivos} | sin cambio");
                continue;
            }

            Log.Error($"{resultado.Alumno.Legajo} | {resultado.Alumno.NombreCompleto,-40} | no compila | {Estado.Aprobado.ToEmoji()} -> {Estado.Revision.ToEmoji()}");
            foreach (CompilacionObjetivoResultado objetivo in resultado.Compilacion.Objetivos.Where(objetivo => !objetivo.Exito)) {
                foreach (string error in objetivo.Errores) {
                    Log.Print($"  {objetivo.Objetivo}: {error}");
                }
            }
        }

        Log.Info($"Resumen TP{numeroTp}: entregados={entregados.Length}, compilan={correctos}, con errores={errores}, marcados para revisar={errores}");
        return errores == 0 ? 0 : 1;
    }

    public static int VerificarEjecucion(string trabajoPractico) {
        int numeroTp = ObtenerNumeroTP(trabajoPractico);
        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        string carpetaTp = CarpetaTrabajoPractico(numeroTp);
        if (!AppPaths.ExisteEnunciadoPractico(carpetaTp)) {
            Log.Error($"No existe la carpeta del enunciado: {AppPaths.EnunciadoPracticoDirectory(carpetaTp)}");
            return 1;
        }

        Alumnos alumnos = CargarAlumnos();
        Alumno[] entregados = alumnos
            .Where(alumno => alumno.EstadoPractico(numeroTp) == Estado.Aprobado)
            .OrderBy(alumno => alumno.Legajo)
            .ToArray();

        if (entregados.Length == 0) {
            Log.Warning($"No hay trabajos entregados para verificar en TP{numeroTp}.");
            return 0;
        }

        List<EjecucionAlumnoResultado> resultados = new();
        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .Start(ctx => {
                var tarea = ctx.AddTask($"Ejecutando TP{numeroTp}", maxValue: entregados.Length);
                foreach (Alumno alumno in entregados) {
                    tarea.Description = $"Ejecutando {alumno.Legajo}";
                    string rutaPractico = AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp);
                    EjecucionPracticoResultado ejecucion =
                        EjecucionPracticos.Verificar(rutaPractico, numeroTp);

                    if (!ejecucion.Exito) {
                        alumno.Practico(numeroTp, Estado.Revision);
                    }

                    resultados.Add(new(alumno, ejecucion));
                    tarea.Increment(1);
                }
            });

        int errores = resultados.Count(resultado => !resultado.Ejecucion.Exito);
        int omitidos = resultados.Sum(resultado => resultado.Ejecucion.Omitidos);
        int correctos = resultados.Count(resultado => resultado.Ejecucion.Exito && resultado.Ejecucion.Omitidos == 0);

        if (errores > 0) {
            AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        }

        foreach (EjecucionAlumnoResultado resultado in resultados) {
            if (resultado.Ejecucion.Exito) {
                string objetivosCorrectos = string.Join(", ",
                    resultado.Ejecucion.Objetivos
                        .Where(objetivo => !objetivo.Omitido)
                        .Select(objetivo => objetivo.Objetivo));
                string objetivosOmitidos = string.Join(", ",
                    resultado.Ejecucion.Objetivos
                        .Where(objetivo => objetivo.Omitido)
                        .Select(objetivo => objetivo.Objetivo));

                if (!string.IsNullOrWhiteSpace(objetivosCorrectos)) {
                    Log.Success($"{resultado.Alumno.Legajo} | {resultado.Alumno.NombreCompleto,-40} | ejecuta: {objetivosCorrectos} | sin cambio");
                }

                if (!string.IsNullOrWhiteSpace(objetivosOmitidos)) {
                    Log.Warning($"{resultado.Alumno.Legajo} | {resultado.Alumno.NombreCompleto,-40} | omitido: {objetivosOmitidos} | sin cambio");
                    foreach (EjecucionObjetivoResultado objetivo in resultado.Ejecucion.Objetivos.Where(objetivo => objetivo.Omitido)) {
                        foreach (string mensaje in objetivo.Mensajes) {
                            Log.Print($"  {objetivo.Objetivo}: {mensaje}");
                        }
                    }
                }

                continue;
            }

            Log.Error($"{resultado.Alumno.Legajo} | {resultado.Alumno.NombreCompleto,-40} | no ejecuta | {Estado.Aprobado.ToEmoji()} -> {Estado.Revision.ToEmoji()}");
            foreach (EjecucionObjetivoResultado objetivo in resultado.Ejecucion.Objetivos.Where(objetivo => objetivo.Estado == EstadoEjecucionObjetivo.Error)) {
                foreach (string error in objetivo.Mensajes) {
                    Log.Print($"  {objetivo.Objetivo}: {error}");
                }
            }
        }

        Log.Info($"Resumen TP{numeroTp}: entregados={entregados.Length}, ejecutan={correctos}, con errores={errores}, omitidos={omitidos}, marcados para revisar={errores}");
        return errores == 0 ? 0 : 1;
    }

    public static int CapturarPantallas(string trabajoPractico, string rutaInicial, bool headed, bool forzar) {
        int numeroTp = ObtenerNumeroTP(trabajoPractico);
        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        string carpetaTp = CarpetaTrabajoPractico(numeroTp);
        if (!AppPaths.ExisteEnunciadoPractico(carpetaTp)) {
            Log.Error($"No existe la carpeta del enunciado: {AppPaths.EnunciadoPracticoDirectory(carpetaTp)}");
            return 1;
        }

        Alumnos alumnos = CargarAlumnos();
        Alumno[] presentados = alumnos
            .Where(alumno => alumno.EstadoPractico(numeroTp) == Estado.Aprobado)
            .OrderBy(alumno => alumno.Legajo)
            .ToArray();

        if (presentados.Length == 0) {
            Log.Warning($"No hay trabajos presentados para capturar en TP{numeroTp}.");
            return 0;
        }

        List<CapturaPantallaAlumnoResultado> resultados = new();
        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .Start(ctx => {
                var tarea = ctx.AddTask($"Capturando pantallas TP{numeroTp}", maxValue: presentados.Length);
                foreach (Alumno alumno in presentados) {
                    tarea.Description = $"Capturando {alumno.Legajo}";
                    string rutaPractico = AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp);
                    CapturaPantallaResultado captura = CapturaPantallasPracticos
                        .Capturar(rutaPractico, numeroTp, rutaInicial, headed, forzar: true)
                        .GetAwaiter()
                        .GetResult();

                    resultados.Add(new(alumno, captura));
                    tarea.Increment(1);
                }
            });

        foreach (CapturaPantallaAlumnoResultado resultado in resultados) {
            if (resultado.Captura.Capturada) {
                string archivo = resultado.Captura.Archivo is null
                    ? "captura-tp5.png"
                    : Path.GetFileName(resultado.Captura.Archivo);
                string proyecto = string.IsNullOrWhiteSpace(resultado.Captura.Proyecto)
                    ? string.Empty
                    : $" | proyecto: {resultado.Captura.Proyecto}";
                Log.Success($"{resultado.Alumno.Legajo} | {resultado.Alumno.NombreCompleto,-40} | capturada: {archivo}{proyecto}");
                continue;
            }

            string etiqueta = resultado.Captura.Omitida ? "omitido" : "error";
            Action<string> log = resultado.Captura.Omitida ? Log.Warning : Log.Error;
            string proyectoDetalle = string.IsNullOrWhiteSpace(resultado.Captura.Proyecto)
                ? string.Empty
                : $" | proyecto: {resultado.Captura.Proyecto}";
            log($"{resultado.Alumno.Legajo} | {resultado.Alumno.NombreCompleto,-40} | {etiqueta}{proyectoDetalle}");
            foreach (string mensaje in resultado.Captura.Mensajes) {
                Log.Print($"  {mensaje}");
            }
        }

        int capturados = resultados.Count(resultado => resultado.Captura.Capturada);
        int errores = resultados.Count(resultado => resultado.Captura.Error);
        int omitidos = resultados.Count(resultado => resultado.Captura.Omitida);
        Log.Info($"Resumen TP{numeroTp}: presentados={presentados.Length}, capturados={capturados}, con errores={errores}, omitidos={omitidos}");
        return errores == 0 ? 0 : 1;
    }

    public static int EjecutarTp5(int? legajo) {
        const int numeroTp = 5;
        string carpetaTp = CarpetaTrabajoPractico(numeroTp);
        if (!AppPaths.ExisteEnunciadoPractico(carpetaTp)) {
            Log.Error($"No existe la carpeta del enunciado: {AppPaths.EnunciadoPracticoDirectory(carpetaTp)}");
            return 1;
        }

        Alumnos alumnos = CargarAlumnos();

        if (legajo is int valor) {
            Alumno? alumno = alumnos.BuscarPorLegajo(valor);
            if (alumno is null || alumno.EstadoPractico(numeroTp) != Estado.Aprobado) {
                Log.Error($"El alumno {valor} no existe o no tiene TP{numeroTp} presentado.");
                return 1;
            }

            EjecutarTp5Alumno(alumno, carpetaTp, numeroTp);
            RegistrarObservacionTp5(alumnos, alumno);
            return 0;
        }

        while (true) {
            Alumno[] candidatos = AlumnosTp5Ordenados(alumnos, numeroTp);
            if (candidatos.Length == 0) {
                Log.Warning($"No hay alumnos con TP{numeroTp} presentado para ejecutar.");
                return 0;
            }

            Alumno? alumno = SeleccionarAlumnoTp5(candidatos, numeroTp);
            if (alumno is null) {
                return 0;
            }

            EjecutarTp5Alumno(alumno, carpetaTp, numeroTp);
            RegistrarObservacionTp5(alumnos, alumno);
        }
    }

    static Alumno[] AlumnosTp5Ordenados(Alumnos alumnos, int numeroTp) =>
        alumnos
            .Where(alumno => alumno.EstadoPractico(numeroTp) == Estado.Aprobado)
            .OrderBy(alumno => string.IsNullOrWhiteSpace(alumno.Observaciones) ? 0 : 1)
            .ThenBy(alumno => alumno.Legajo)
            .ToArray();

    static void RegistrarObservacionTp5(Alumnos alumnos, Alumno alumno) {
        string observacion = AnsiConsole.Prompt(
            new TextPrompt<string>($"[bold cyan]Observación para {alumno.Legajo} | {Markup.Escape(alumno.NombreCompleto)}[/] [grey](vacío = sin observación)[/]:")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(observacion)) {
            Log.Warning("No se registró ninguna observación.");
            return;
        }

        alumno.Observaciones = observacion.Trim();
        AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        Log.Success($"Observación guardada para {alumno.Legajo} | {alumno.NombreCompleto}.");
    }

    static int EjecutarTp5Alumno(Alumno alumno, string carpetaTp, int numeroTp) {
        string rutaPractico = AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp);
        Log.Info($"Iniciando TP{numeroTp} de {alumno.Legajo} | {alumno.NombreCompleto}...");

        EjecucionWebResultado resultado = EjecutarConIndicador(
            $"Ejecutar TP{numeroTp}",
            "Compilando e iniciando el proyecto web...",
            actualizarEstado => {
                actualizarEstado("Esperando a que la aplicación informe su URL...");
                return EjecucionWebPracticos.Iniciar(rutaPractico, numeroTp);
            });

        if (!resultado.Iniciada || resultado.Url is null || resultado.Proceso is null) {
            Log.Error($"No se pudo iniciar el proyecto web de {alumno.Legajo} | {alumno.NombreCompleto}.");
            foreach (string mensaje in resultado.Mensajes) {
                Log.Print($"  {mensaje}");
            }
            return 1;
        }

        try {
            Log.Success($"Proyecto en ejecución: {resultado.Proyecto}");
            Log.Info($"Abriendo el navegador en {resultado.Url}");
            if (!EjecucionWebPracticos.AbrirNavegador(resultado.Url)) {
                Log.Warning($"No se pudo abrir el navegador automáticamente. Abrí manualmente {resultado.Url}");
            }

            AnsiConsole.MarkupLine("[grey]Presioná una tecla para detener la aplicación y volver al menú...[/]");
            Console.ReadKey(intercept: true);
        } finally {
            EjecucionWebPracticos.Detener(resultado.Proceso, rutaPractico);
            Log.Info("Aplicación detenida.");
        }

        return 0;
    }

    static Alumno? SeleccionarAlumnoTp5(IReadOnlyList<Alumno> presentados, int numeroTp) {
        List<OpcionAlumnoTp5> opciones = [
            .. presentados.Select(alumno => new OpcionAlumnoTp5(alumno)),
            new(null)
        ];

        OpcionAlumnoTp5 seleccion = AnsiConsole.Prompt(
            new SelectionPrompt<OpcionAlumnoTp5>()
                .Title($"[bold cyan]Ejecutar TP{numeroTp}[/] · Elegí el alumno\n[grey]{presentados.Count} alumno(s) con TP{numeroTp} presentado[/]")
                .PageSize(16)
                .UseConverter(opcion => opcion.Alumno is null
                    ? "[grey]Volver al menú principal[/]"
                    : $"[green]{opcion.Alumno.Legajo,-8}[/] [grey]{opcion.Alumno.NombreCompleto,-40}[/] {FormatearObservacion(opcion.Alumno.Observaciones)}")
                .AddChoices(opciones));

        return seleccion.Alumno;
    }

    static string FormatearObservacion(string observaciones) =>
        string.IsNullOrWhiteSpace(observaciones)
            ? string.Empty
            : $"[yellow]{Markup.Escape(observaciones)}[/]";

    public static int RelevarAsistencias() {
        Alumnos alumnos = CargarAlumnos();

        CargarAsistenciasHastaHoy(alumnos);
        Alumno[] presentesHoy = alumnos.Where(alumno => alumno.Presente).OrderBy(alumno => alumno.Legajo).ToArray();
        AlumnosManager.Listar(presentesHoy, "Alumnos presentes hoy");
        Log.WriteLine($"Presentes detectados hoy: {presentesHoy.Length}");
        Log.WriteLine($"Asistencias acumuladas hasta hoy: {alumnos.Sum(alumno => alumno.Asistencias)}");
        AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        return 0;
    }

    public static int WappGrupos() {
        Alumnos alumnos = CargarAlumnos();

        Log.Info("Listando grupos y participantes desde la base local de wacli.");
        Log.Info($"Base principal: {AppPaths.WacliDatabase(null)}");
        Log.Info($"Base de sesión: {AppPaths.WacliSessionDatabase(null)}");

        if (!AppPaths.ExisteArchivo(AppPaths.WacliDatabase(null))) {
            Log.Error("No existe la base principal de wacli. Ejecutá wacli o sincronizá WhatsApp antes de listar grupos.");
            return 1;
        }

        if (!AppPaths.ExisteArchivo(AppPaths.WacliSessionDatabase(null))) {
            Log.Error("No existe la base de sesión de wacli. Ejecutá wacli o sincronizá WhatsApp antes de listar participantes.");
            return 1;
        }

        WAppService wapp = new(sincronizar: false);
        List<GrupoWhatsApp> grupos;

        try {
            grupos = wapp.Grupos();
        } catch (Exception ex) {
            Log.Error($"No se pudieron listar grupos desde la base local de wacli: {ex.Message}");
            return 1;
        }

        if (grupos.Count == 0) {
            Log.Warning("No se encontraron grupos en la base local de wacli.");
            return 0;
        }

        Log.Info($"Grupos encontrados: {grupos.Count}");

        foreach (var grupo in grupos) {
            Log.WriteLine($"Grupo: {grupo.Group}");
            try {
                List<ContactoWhatsApp> participantes = wapp.Participantes(grupo.Jid);
                Log.WriteLine($"  Participantes: {participantes.Count}");
                foreach (var contacto in participantes) {
                    Alumno? alumno = alumnos.BuscarPorTelefono(contacto.PhoneNumber);
                    string datosAlumno = alumno is null
                        ? "SIN ALUMNO"
                        : $"{alumno.Comision} | {alumno.Legajo} | {alumno.NombreCompleto}";
                    string linea = $"  - {contacto.Name,-30} {contacto.PhoneNumber,-15} {contacto.Jid,-28} | {datosAlumno}";

                    if (alumno is null) {
                        Log.Error(linea);
                    } else {
                        Log.WriteLine(linea);
                    }
                }
            } catch (Exception ex) {
                Log.Error($"  No se pudieron listar participantes de '{grupo.Group}': {ex.Message}");
            }
        }

        return 0;
    }

    public static int ObtenerNumeroTP(string? valor) {
        if (string.IsNullOrWhiteSpace(valor)) {
            return 0;
        }

        int numeroTp = GitHub.ExtraerTP(valor);
        if (numeroTp == 0 && int.TryParse(valor, out int tpDesdeNumero)) {
            numeroTp = tpDesdeNumero;
        }

        return numeroTp;
    }

    public static bool EsTrabajoPracticoValido(string? valor) => ObtenerNumeroTP(valor) > 0;

    public static string MensajeTrabajoPracticoInvalido(string? valor) =>
        $"No se pudo interpretar el trabajo práctico '{valor}'. Use un valor como TP1 o 1.";

    static Alumnos CargarAlumnos() {
        Alumnos alumnos = AlumnosManager.Leer();
        Log.WriteLine($"Alumnos cargados: {alumnos.Count()}");
        return alumnos;
    }

    static void EjecutarConIndicador(string accion, string estadoInicial, Action<Action<string>> ejecutar) {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .Start($"{accion} · {estadoInicial}", contexto =>
                ejecutar(estado => contexto.Status($"{accion} · {estado}")));
    }

    static T EjecutarConIndicador<T>(string accion, string estadoInicial, Func<Action<string>, T> ejecutar) =>
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .Start($"{accion} · {estadoInicial}", contexto =>
                ejecutar(estado => contexto.Status($"{accion} · {estado}")));

    static string ResolverRuta(string? ruta, string rutaPorDefecto) =>
        string.IsNullOrWhiteSpace(ruta) ? rutaPorDefecto : ruta;

    static void PrepararCarpetasAlumnos() =>
        CargarAlumnosConCarpetasPreparadas();

    static Alumnos CargarAlumnosConCarpetasPreparadas() {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.CrearCarpetas(alumnos);
        return alumnos;
    }

    static (Alumnos Alumnos, GitHub GitHub) PrepararPullRequests(bool prepararCarpetas = false) {
        Alumnos alumnos = prepararCarpetas
            ? CargarAlumnosConCarpetasPreparadas()
            : CargarAlumnos();
        GitHub gh = new();

        Log.Info("Normalizando títulos de PRs antes de continuar...");
        gh.NormalizarTitulos(alumnos);
        return (alumnos, gh);
    }

    static string CarpetaTrabajoPractico(int numeroTp) => $"tp{numeroTp}";

    static void CargarAsistenciasHastaHoy(Alumnos alumnos) {
        WAppService wapp = EjecutarConIndicador(
            "Relevar asistencias",
            "Sincronizando WhatsApp...",
            actualizarEstado => {
                actualizarEstado("Sincronizando WhatsApp antes de leer mensajes...");
                return new WAppService();
            });
        DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

        DateTime desde = new(hoy.Year, 4, 1);
        DateTime hasta = hoy.ToDateTime(new TimeOnly(14, 0));
        Dictionary<int, HashSet<DateOnly>> asistenciasPorAlumno = alumnos
            .ToDictionary(alumno => alumno.Legajo, _ => new HashSet<DateOnly>());

        foreach (var grupo in new[] { "C7", "C9" }) {
            IEnumerable<MensajeWhatsApp> mensajes = wapp.Mensajes(grupo, desde, hasta).Where(mensaje => EsMensajeDeAsistencia(mensaje, desde, hasta));
            AnsiConsole.Progress()
                .AutoClear(true)
                .Start(ctx => {
                    var tarea = ctx.AddTask($"Procesando mensajes de {grupo}", maxValue: mensajes.Count());
                    foreach (var mensaje in mensajes) {
                        string telefono = wapp.ObtenerTelefonoAutorMensaje(mensaje);
                        Alumno? alumno = alumnos.BuscarPorTelefono(telefono);
                        if (alumno is not null) {
                            asistenciasPorAlumno[alumno.Legajo].Add(DateOnly.FromDateTime(mensaje.Fecha));
                            tarea.Increment(1);
                        }
                    }
                });
        }

        foreach (Alumno alumno in alumnos) {
            HashSet<DateOnly> fechas = asistenciasPorAlumno[alumno.Legajo];
            alumno.Presente    = fechas.Contains(hoy);
            alumno.Asistencias = fechas.Count;
            alumno.Examen(1, alumno.Asistencias switch { >= 8 => Estado.Aprobado, >= 4 => Estado.Pendiente, _ => Estado.Desaprobado });
        }
    }

    static bool EsMensajeDeAsistencia(MensajeWhatsApp mensaje, DateTime desde, DateTime hasta) {
        if (mensaje.FromMe || mensaje.Fecha < desde || mensaje.Fecha > hasta) { return false; }

        DayOfWeek dia = mensaje.Fecha.DayOfWeek;
        TimeSpan hora = mensaje.Fecha.TimeOfDay;
        return
            dia >= DayOfWeek.Monday && dia <= DayOfWeek.Thursday &&
                hora >= new TimeSpan(8, 0, 0) && hora <= new TimeSpan(13, 0, 0);
    }

    static readonly HashSet<string> extensionesFuentePractico = new(StringComparer.OrdinalIgnoreCase) {
        ".cs",
        ".cshtml",
        ".csproj",
        ".css",
        ".html",
        ".htm",
        ".js",
        ".json",
        ".razor",
        ".sln",
        ".slnx",
        ".ts"
    };

    static int ContarLineasPracticoLocal(string rutaPractico) {
        if (!Directory.Exists(rutaPractico)) {
            return 0;
        }

        return Directory
            .EnumerateFiles(rutaPractico, "*", SearchOption.AllDirectories)
            .Where(EsArchivoContabilizablePractico)
            .Sum(rutaArchivo => File.ReadLines(rutaArchivo).Count());
    }

    static bool EsArchivoContabilizablePractico(string rutaArchivo) =>
        EsArchivoFuentePractico(rutaArchivo) &&
        extensionesFuentePractico.Contains(Path.GetExtension(rutaArchivo)) &&
        ArchivoTexto.EsRutaTexto(rutaArchivo) &&
        ArchivoTexto.PareceArchivoTexto(rutaArchivo);

    static int ObtenerLineasBaseEnunciado(int numeroTp, string carpetaTp, string rutaEnunciado, Alumnos alumnos) {
        int lineasEnunciado = ContarLineasPracticoLocal(rutaEnunciado);
        if (numeroTp != 3) {
            return lineasEnunciado;
        }

        int lineasPlantilla = alumnos
            .Select(alumno => ContarLineasPracticoLocal(AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp)))
            .Where(lineas => lineas > 0)
            .DefaultIfEmpty(lineasEnunciado)
            .Min();

        return lineasPlantilla > 0 && lineasPlantilla < lineasEnunciado
            ? lineasPlantilla
            : lineasEnunciado;
    }

    static int RevisarCopiasTrabajosPresentados(int numeroTp, IReadOnlyList<TrabajoPresentadoLocal> trabajosPresentados) {
        HashSet<int> legajosConCopia = new();
        Dictionary<int, Alumno> alumnosPorLegajo = trabajosPresentados.ToDictionary(trabajo => trabajo.Alumno.Legajo, trabajo => trabajo.Alumno);
        Dictionary<int, HashSet<int>> copiasPorLegajo = new();

        for (int i = 0; i < trabajosPresentados.Count; i++) {
            TrabajoPresentadoLocal actual = trabajosPresentados[i];
            if (actual.LineasCodigo.Count == 0) {
                continue;
            }

            for (int j = i + 1; j < trabajosPresentados.Count; j++) {
                TrabajoPresentadoLocal otro = trabajosPresentados[j];
                if (CompararTrabajos(actual, otro) is not { } copia) { continue; }

                actual.Alumno.Practico(numeroTp, Estado.Revision);
                otro.Alumno.Practico(numeroTp, Estado.Revision);

                legajosConCopia.Add(actual.Alumno.Legajo);
                legajosConCopia.Add(otro.Alumno.Legajo);
                RegistrarRelacionCopia(copiasPorLegajo, actual.Alumno.Legajo, otro.Alumno.Legajo);

                Log.Warning( $"TP{numeroTp} | {actual.Alumno.Legajo} <-> {otro.Alumno.Legajo} | {copia.LineasComunes,3} de {copia.MaximoLineas,4} >>{copia.Porcentaje,6:P0}");
            }
        }

        AsignarCodigosDeCopia(alumnosPorLegajo, copiasPorLegajo, numeroTp);
        return legajosConCopia.Count;
    }

    static void RegistrarRelacionCopia(Dictionary<int, HashSet<int>> copiasPorLegajo, int legajoA, int legajoB) {
        if (!copiasPorLegajo.TryGetValue(legajoA, out HashSet<int>? copiasA)) {
            copiasA = new();
            copiasPorLegajo[legajoA] = copiasA;
        }

        if (!copiasPorLegajo.TryGetValue(legajoB, out HashSet<int>? copiasB)) {
            copiasB = new();
            copiasPorLegajo[legajoB] = copiasB;
        }

        copiasA.Add(legajoB);
        copiasB.Add(legajoA);
    }

    static void AsignarCodigosDeCopia(Dictionary<int, Alumno> alumnosPorLegajo, Dictionary<int, HashSet<int>> copiasPorLegajo, int numeroTp) {
        HashSet<int> visitados = new();

        foreach (int legajo in copiasPorLegajo.Keys.Order()) {
            if (!visitados.Add(legajo)) {
                continue;
            }

            List<int> grupo = new();
            Stack<int> pendientes = new();
            pendientes.Push(legajo);

            while (pendientes.Count > 0) {
                int actual = pendientes.Pop();
                grupo.Add(actual);

                if (!copiasPorLegajo.TryGetValue(actual, out HashSet<int>? relacionados)) {
                    continue;
                }

                foreach (int relacionado in relacionados) {
                    if (visitados.Add(relacionado)) {
                        pendientes.Push(relacionado);
                    }
                }
            }

            string codigo = string.Join(",", grupo.Order());
            foreach (int legajoGrupo in grupo) {
                if (alumnosPorLegajo.TryGetValue(legajoGrupo, out Alumno? alumno)) {
                    string anterior = numeroTp == 1 ? "" : alumno.Observaciones;
                    alumno.Observaciones = $"{anterior} TP{numeroTp}:{codigo}".Trim();
                    Console.WriteLine($"{numeroTp} -> {anterior} | {alumno.Observaciones}");
                }
            }
        }
    }

    static CopiaDetectada? CompararTrabajos(TrabajoPresentadoLocal actual, TrabajoPresentadoLocal otro) {
        int maximoLineas = Math.Min(actual.LineasCodigo.Count, otro.LineasCodigo.Count);
        if (maximoLineas < MinimoLineasCopia) {
            return null;
        }

        int lineasComunes = actual.LineasCodigo.Count <= otro.LineasCodigo.Count
            ? actual.LineasCodigo.Count(otro.LineasCodigo.Contains)
            : otro.LineasCodigo.Count(actual.LineasCodigo.Contains);

        double porcentaje = (double)lineasComunes / maximoLineas;
        return porcentaje >= UmbralCopia? new(lineasComunes, maximoLineas, porcentaje) : null;
    }

    static HashSet<string> ObtenerLineasCodigoNormalizadas(string rutaPractico) {
        HashSet<string> lineas = new(StringComparer.Ordinal);
        if (!Directory.Exists(rutaPractico)) {
            return lineas;
        }

        foreach (string rutaArchivo in Directory
            .EnumerateFiles(rutaPractico, "*.cs", SearchOption.AllDirectories)
            .Where(EsArchivoFuentePractico)
            .Where(ArchivoTexto.EsRutaTexto)
            .Where(ArchivoTexto.PareceArchivoTexto)) {
            foreach (string linea in NormalizarLineasCodigo(File.ReadLines(rutaArchivo))) {
                lineas.Add(linea);
            }
        }

        return lineas;
    }

    static bool EsArchivoFuentePractico(string rutaArchivo) {
        string[] partes = rutaArchivo.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !partes.Any(parte => parte is "bin" or "obj" or ".vs");
    }

    static IEnumerable<string> NormalizarLineasCodigo(IEnumerable<string> lineas) {
        bool dentroComentarioBloque = false;

        foreach (string lineaOriginal in lineas) {
            var aux = lineaOriginal.Replace("{", string.Empty).Replace("}", string.Empty);
            string sinComentarios = QuitarComentarios(aux, ref dentroComentarioBloque);
            string normalizada = QuitarEspacios(sinComentarios);
            if (normalizada.Length > 0) {
                yield return normalizada.ToLowerInvariant();
            }
        }
    }

    static string QuitarComentarios(string linea, ref bool dentroComentarioBloque) {
        StringBuilder sb = new();
        bool dentroString = false;
        bool dentroStringVerbatim = false;
        bool dentroChar = false;

        for (int i = 0; i < linea.Length; i++) {
            if (dentroComentarioBloque) {
                int finBloque = linea.IndexOf("*/", i, StringComparison.Ordinal);
                if (finBloque < 0) {
                    break;
                }

                dentroComentarioBloque = false;
                i = finBloque + 1;
                continue;
            }

            if (dentroStringVerbatim) {
                sb.Append(linea[i]);
                if (linea[i] == '"' && i + 1 < linea.Length && linea[i + 1] == '"') {
                    sb.Append(linea[++i]);
                    continue;
                }

                if (linea[i] == '"') {
                    dentroStringVerbatim = false;
                }

                continue;
            }

            if (dentroString) {
                sb.Append(linea[i]);
                if (linea[i] == '\\' && i + 1 < linea.Length) {
                    sb.Append(linea[++i]);
                    continue;
                }

                if (linea[i] == '"') {
                    dentroString = false;
                }

                continue;
            }

            if (dentroChar) {
                sb.Append(linea[i]);
                if (linea[i] == '\\' && i + 1 < linea.Length) {
                    sb.Append(linea[++i]);
                    continue;
                }

                if (linea[i] == '\'') {
                    dentroChar = false;
                }

                continue;
            }

            if (i + 1 < linea.Length && linea[i] == '/' && linea[i + 1] == '/') {
                break;
            }

            if (i + 1 < linea.Length && linea[i] == '/' && linea[i + 1] == '*') {
                dentroComentarioBloque = true;
                i++;
                continue;
            }

            if (linea[i] == '"') {
                dentroStringVerbatim = EsInicioStringVerbatim(linea, i);
                dentroString = !dentroStringVerbatim;
                sb.Append(linea[i]);
                continue;
            }

            if (linea[i] == '\'') {
                dentroChar = true;
                sb.Append(linea[i]);
                continue;
            }

            sb.Append(linea[i]);
        }

        return sb.ToString();
    }

    static bool EsInicioStringVerbatim(string linea, int indiceComilla) =>
        indiceComilla > 0 && linea[indiceComilla - 1] == '@' ||
        indiceComilla > 1 && linea[indiceComilla - 2] == '@' && linea[indiceComilla - 1] == '$';

    static string QuitarEspacios(string linea) {
        StringBuilder sb = new(linea.Length);
        foreach (char caracter in linea) {
            if (!char.IsWhiteSpace(caracter)) {
                sb.Append(caracter);
            }
        }

        return sb.ToString();
    }

    static bool TieneAlgunPracticoPresentado(Alumno alumno) =>
        alumno.practicos.Any(estado => estado == Estado.Aprobado);

    sealed record TrabajoPresentadoLocal(Alumno Alumno, string RutaPractico, HashSet<string> LineasCodigo);
    sealed record PresentacionCambiada(Alumno Alumno, Estado EstadoAnterior, Estado EstadoNuevo, int LineasTotales, int LineasAgregadas);
    sealed record CompilacionAlumnoResultado(Alumno Alumno, CompilacionPracticoResultado Compilacion);
    sealed record EjecucionAlumnoResultado(Alumno Alumno, EjecucionPracticoResultado Ejecucion);
    sealed record CapturaPantallaAlumnoResultado(Alumno Alumno, CapturaPantallaResultado Captura);
    sealed record OpcionAlumnoTp5(Alumno? Alumno);

    readonly record struct CopiaDetectada(int LineasComunes, int MaximoLineas, double Porcentaje);

}
