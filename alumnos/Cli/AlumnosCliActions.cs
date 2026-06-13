using Spectre.Console;

namespace Tup26.AlumnosApp;

static class AlumnosCliActions {

    const double UmbralCopia = 0.90;
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

            var detallePr = gh.ObtenerEstado(pr.Numero);
            int cantidadArchivos = gh.ListarArchivos(pr.Numero).Count;
            int cantidadLineas = gh.CantidadLineas(pr.Numero);
            int cantidadCommits = gh.Commits(pr.Numero).Count;
            string estado = detallePr.Estado == "open" ? "abierto" : detallePr.Estado == "closed" ? "cerrado" : "sin dato";
            string mergeable = detallePr.EsMergeable ? "mergeable" : "con conflictos";
            List<int> tps = GitHub.ExtraerTPs(pr.Titulo);
            List<string> archivosTp = tps
                .SelectMany(tp => gh.ListarArchivosDirectorio(pr.Numero, alumno.CarpetaNombre, $"tp{tp}"))
                .OrderBy(ruta => ruta, StringComparer.OrdinalIgnoreCase)
                .ToList();
            string etiquetaTp = tps.Count == 0
                ? "TP?"
                : string.Join("", tps.Select(tp => $"TP{tp}"));

            Console.ForegroundColor = detallePr.EsMergeable ? ConsoleColor.Green : ConsoleColor.Red;
            Console.BackgroundColor = cantidadArchivos < 20 ? ConsoleColor.Black : ConsoleColor.DarkRed;
            Log.WriteLine($"PR #{pr.Numero:000} | {legajo} | {alumno.NombreCompleto,-40} | A:{cantidadArchivos,4} | L:{cantidadLineas,4} | C:{cantidadCommits,2} | {estado} | {mergeable,-15} | {etiquetaTp}");
            foreach (string archivo in archivosTp) {
                Log.WriteLine($"  - {archivo}");
            }
            Console.ResetColor();
        }

        return 0;
    }

    public static int BajarPullRequests() {
        (_, GitHub gh) = PrepararPullRequests(prepararCarpetas: true);
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
        foreach (var pr in prs) {
            indice++;
            Log.Info($"\nRevisando PR {indice}/{prs.Count}: #{pr.Numero} | {pr.Titulo}");
            BajadaArchivosAlumnoResultado bajada = gh.BajarArchivosAlumno(pr.Numero, forzar: true);
            if (bajada.Archivos.Count > 0) {
                procesados++;
                trabajosProcesados += bajada.TrabajosPracticos.Count;
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
                    Estado estado = Estado.Desaprobado;
                    if (configuracion.ParecePresentado(lineasTotales, lineasAgregadas)) {
                        estado = Estado.Aprobado;
                        HashSet<string> lineasCodigo = ObtenerLineasCodigoNormalizadas(rutaPractico);
                        lineasCodigo.ExceptWith(lineasCodigoEnunciado);
                        trabajosPresentados.Add(new(alumno, rutaPractico, lineasCodigo));
                    }

                    alumno.Practico(numeroTp, estado);
                    if (estadoAnterior != estado) {
                        presentacionesCambiadas.Add(new(alumno, estadoAnterior, estado, lineasTotales, lineasAgregadas));
                    }
                    tarea.Increment(1);
                }
            });

        int copias = RevisarCopiasTrabajosPresentados(numeroTp, trabajosPresentados);
        int marcados = alumnos.Count(alumno => alumno.EstadoPractico(numeroTp) == Estado.Aprobado);

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

        Log.Success($"Resumen TP{numeroTp}: marcados={marcados}, copias={copias}, total={alumnos.Count()}, porcentaje={marcados * 100.0 / alumnos.Count():F2}%");
        return 0;
    }

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

    static int ContarLineasPracticoLocal(string rutaPractico) =>
        AppPaths.ContarLineasArchivos(rutaPractico, "*.cs", SearchOption.TopDirectoryOnly);

    static int ObtenerLineasBaseEnunciado(int numeroTp, string carpetaTp, string rutaEnunciado, Alumnos alumnos) {
        int lineasEnunciado = AppPaths.ContarLineasArchivos(rutaEnunciado, "*.cs");
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
        if (maximoLineas == 0) {
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

        foreach (string rutaArchivo in Directory.EnumerateFiles(rutaPractico, "*.cs", SearchOption.AllDirectories).Where(EsArchivoFuentePractico)) {
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

    readonly record struct CopiaDetectada(int LineasComunes, int MaximoLineas, double Porcentaje);

}
