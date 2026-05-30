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

    public static int ListarSinGithub() {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.Listar(alumnos.ConGithub(false), "Alumnos sin GitHub");
        return 0;
    }

    public static int ListarSinTelefono() {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.Listar(alumnos.ConTelefono(false), "Alumnos sin telefono");
        return 0;
    }

    public static int ListarSinFoto() {
        Alumnos alumnos = CargarAlumnos();
        int actualizados = AlumnosManager.SincronizarEstadoFotosDesdeCarpetas(alumnos);

        if (actualizados > 0) {
            AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
            Log.Info($"Estado de foto actualizado en alumnos.md: {actualizados} alumno(s).");
        } else {
            Log.Info("No hubo cambios en el estado de fotos al revisar carpetas.");
        }

        AlumnosManager.Listar(alumnos.ConFotos(false), "Alumnos sin foto");
        return 0;
    }

    public static int ListarTp1NoPresentado() {
        return ListarTpNoPresentado("1");
    }

    public static int ListarTp2NoPresentado() {
        return ListarTpNoPresentado("2");
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

        AlumnosManager.Listar(noPresentaron, $"Alumnos que no presentaron TP{numeroTp}");
        return 0;
    }

    public static int ListarSinPracticosPresentados() {
        Alumnos alumnos = CargarAlumnos();
        IEnumerable<Alumno> sinPracticos = alumnos.Where(alumno => !TieneAlgunPracticoPresentado(alumno));

        AlumnosManager.Listar(sinPracticos, "Alumnos sin prácticos presentados");
        return 0;
    }

    public static int LimpiarProyectosPracticos() {
        LimpiezaCompilacionPracticosResultado resultado = AppPaths.LimpiarDirectoriosCompilacionPracticos();
        IReadOnlyList<string> elementosEliminados = resultado.ElementosEliminados;
        IReadOnlyList<string> elementosRestantes = resultado.ElementosRestantes;

        if (elementosEliminados.Count == 0 && elementosRestantes.Count == 0) {
            Log.Info("No se encontraron carpetas ni cachés de compilación dentro de prácticos.");
            return 0;
        }

        foreach (string ruta in elementosEliminados) {
            Log.Info($"Eliminado: {AppPaths.RutaRelativaDesdePracticos(ruta)}");
        }

        Log.Info($"Total de elementos eliminados: {elementosEliminados.Count}");

        if (elementosRestantes.Count > 0) {
            Log.Warning($"Quedaron o se regeneraron {elementosRestantes.Count} elemento(s) de compilación.");
            foreach (string ruta in elementosRestantes.Take(10)) {
                Log.Warning($"Pendiente: {AppPaths.RutaRelativaDesdePracticos(ruta)}");
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

    public static int CrearCarpetas() {
        Alumnos alumnos = CargarAlumnos();
        AlumnosManager.CrearCarpetas(alumnos);
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

        Alumnos alumnos = CargarAlumnos();
        Log.Info($"Publicando {carpetaTp.ToUpperInvariant()} desde {AppPaths.EnunciadoPracticoDirectory(carpetaTp)}");
        AlumnosManager.PublicarPractico(alumnos, carpetaTp, forzar);
        return 0;
    }

    public static int PublicarRehacer(string trabajoPractico) {
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
        List<Alumno> alumnosParaRehacer = alumnos
            .Where(alumno => alumno.EstadoPractico(numeroTp) == Estado.Revision)
            .ToList();

        Log.Info($"Republicando {carpetaTp.ToUpperInvariant()} para alumnos en estado Revisar desde {AppPaths.EnunciadoPracticoDirectory(carpetaTp)}");
        Log.Info($"Alumnos en estado Revisar para {carpetaTp.ToUpperInvariant()}: {alumnosParaRehacer.Count}");
        AlumnosManager.PublicarRehacer(alumnosParaRehacer, carpetaTp);
        return 0;
    }

    public static int RevisarPullRequests() {
        Alumnos alumnos = CargarAlumnos();
        GitHub gh = new();

        foreach (var pr in gh.PullRequests()) {
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
            int tp = GitHub.ExtraerTP(pr.Titulo);
            string carpetaTp = tp > 0 ? $"tp{tp}" : string.Empty;
            List<string> archivosTp = string.IsNullOrWhiteSpace(carpetaTp)
                ? new()
                : gh.ListarArchivosDirectorio(pr.Numero, alumno.CarpetaNombre, carpetaTp);

            Console.ForegroundColor = detallePr.EsMergeable ? ConsoleColor.Green : ConsoleColor.Red;
            Console.BackgroundColor = cantidadArchivos < 10 ? ConsoleColor.Black : ConsoleColor.DarkRed;
            Log.WriteLine($"PR #{pr.Numero:000} | {legajo} | {alumno.NombreCompleto,-40} | A:{cantidadArchivos,4} | L:{cantidadLineas,4} | C:{cantidadCommits,2} | {estado} | {mergeable,-15} | TP{tp}");
            foreach (string archivo in archivosTp) {
                Log.WriteLine($"  - {archivo}");
            }
            Console.ResetColor();
        }

        return 0;
    }

    public static int NormalizarPullRequests(bool simular) {
        Alumnos alumnos = CargarAlumnos();
        new GitHub().NormalizarTitulos(alumnos, simular);
        return 0;
    }

    public static int BajarPullRequests(string trabajoPractico, bool forzar) {
        int numeroTp = ObtenerNumeroTP(trabajoPractico);
        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        GitHub gh = new();
        IEnumerable<(int Numero, string Titulo)> prs = gh.PullRequests(tp: numeroTp);

        foreach (var pr in prs) {
            gh.BajarArchivosAlumno(pr.Numero, forzar);
        }

        return 0;
    }

    public static int CerrarPullRequests(string? trabajoPractico) {
        GitHub gh = new();

        if (string.IsNullOrWhiteSpace(trabajoPractico)) {
            gh.CerrarPRsAbiertos();
            return 0;
        }

        int numeroTp = ObtenerNumeroTP(trabajoPractico);
        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        gh.CerrarPRsAbiertos(numeroTp);
        return 0;
    }

    public static int RevisarPresentados(string trabajoPractico) {
        Alumnos alumnos = CargarAlumnos();
        int numeroTp = ObtenerNumeroTP(trabajoPractico);

        if (numeroTp <= 0) {
            Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
            return 1;
        }

        string carpetaTp = CarpetaTrabajoPractico(numeroTp);
        string rutaEnunciado = AppPaths.EnunciadoPracticoDirectory(carpetaTp);
        int lineasEnunciado = ObtenerLineasBaseEnunciado(numeroTp, carpetaTp, rutaEnunciado, alumnos);

        Log.Info($"{carpetaTp.ToUpperInvariant()} | líneas base del enunciado: {lineasEnunciado}");
        List<TrabajoPresentadoLocal> trabajosPresentados = new();
        bool habiaCodigos = alumnos.Any(alumno => !string.IsNullOrWhiteSpace(alumno.Codigo));

        foreach (Alumno alumno in alumnos.OrderBy(alumno => alumno.Legajo)) {
            string rutaPractico = AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp);
            int lineasTotales   = ContarLineasPracticoLocal(rutaPractico);
            int lineasAgregadas = Math.Max(0, lineasTotales - lineasEnunciado);

            // alumno.Codigo = string.Empty;

            Estado estado = Estado.Desaprobado;
            if (PracticoParecePresentado(numeroTp, lineasTotales, lineasAgregadas)) {
                estado = Estado.Aprobado;
                trabajosPresentados.Add(new(alumno, rutaPractico, ObtenerLineasCodigoNormalizadas(rutaPractico)));
            }

            alumno.Practico(numeroTp, estado);
        }

        int copias = RevisarCopiasTrabajosPresentados(numeroTp, trabajosPresentados);
        int marcados = alumnos.Count(alumno => alumno.EstadoPractico(numeroTp) == Estado.Aprobado);

        if (trabajosPresentados.Count > 0 || habiaCodigos) {
            AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        }

        foreach (Alumno alumno in alumnos.OrderBy(alumno => alumno.Legajo)) {
            string rutaPractico = AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp);
            int lineasTotales   = ContarLineasPracticoLocal(rutaPractico);
            int lineasAgregadas = Math.Max(0, lineasTotales - lineasEnunciado);

            alumno.Codigo = string.Empty;

            var estado = alumno.EstadoPractico(numeroTp);
            Log.Info($"{alumno.Legajo} | {alumno.NombreCompleto,-40} | L:{lineasTotales,4} | L+:{lineasAgregadas,4} | marcado    {estado.ToEmoji()}");
            }

        Log.Info($"Resumen TP{numeroTp}: marcados={marcados}, copias={copias}, total={alumnos.Count()}, porcentaje={marcados * 100.0 / alumnos.Count():F2}%");
        return 0;
    }

    public static int RegistrarAsistencias() {
        Alumnos alumnos = CargarAlumnos();
        Alumno[] presentesHoy = alumnos.Where(alumno => alumno.Presente).OrderBy(alumno => alumno.Legajo).ToArray();
        int contar = presentesHoy.Length;

        if (contar > 0) {
            AlumnosManager.Listar(presentesHoy, "Alumnos presentes hoy");
        }

        foreach (Alumno alumno in alumnos) {
            if (alumno.Presente) {
                alumno.Asistencias++;
            }

            alumno.Presente = false;
            alumno.Examen(1, alumno.Asistencias switch { >= 8 => Estado.Aprobado, >= 4 => Estado.Pendiente, _ => Estado.Desaprobado });
        }

        AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        if (contar == 0) {
            Log.WriteLine("No hay alumnos presentes para registrar.");
        }

        Log.WriteLine($"Asistencias registradas: {contar}");
        return 0;
    }

    public static int RelevarAsistencias() {
        Alumnos alumnos = CargarAlumnos();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .Start("Preparando relevamiento de asistencias...", contexto =>
                CargarAsistenciasHastaHoy(alumnos, estado => contexto.Status(estado)));

        Alumno[] presentesHoy = alumnos.Where(alumno => alumno.Presente).OrderBy(alumno => alumno.Legajo).ToArray();
        AlumnosManager.Listar(presentesHoy, "Alumnos presentes hoy");
        Log.WriteLine($"Presentes detectados hoy: {presentesHoy.Length}");
        Log.WriteLine($"Asistencias acumuladas hasta ayer: {alumnos.Sum(alumno => alumno.Asistencias)}");
        AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        return 0;
    }

    public static int RegistrarRespuestas() {
        Alumnos alumnos = CargarAlumnos();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .Start("Registrando respuestas de WhatsApp...", contexto =>
                CargarCodigosDesdeWhatsApp(alumnos, estado => contexto.Status(estado)));

        IEnumerable<Alumno> conCodigo = alumnos.Where(alumno => !string.IsNullOrWhiteSpace(alumno.Codigo));
        AlumnosManager.Listar(conCodigo, "Alumnos con código registrado");
        Log.WriteLine($"Códigos detectados: {conCodigo.Count()}");
        AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        return 0;
    }

    public static int WappGrupos() {
        WAppService wapp = new();

        foreach (var grupo in wapp.Grupos()) {
            Log.WriteLine($"Grupo: {grupo.Group}");
            foreach (var contacto in wapp.Participantes(grupo.Group)) {
                Log.WriteLine($"  - {contacto.Name,-30} {contacto.PhoneNumber} {contacto.Jid}");
            }
        }

        return 0;
    }

    public static int WappRecuperarPracticos(string? trabajoPractico, bool simular) {
        int? numeroTp = null;
        if (!string.IsNullOrWhiteSpace(trabajoPractico)) {
            int n = ObtenerNumeroTP(trabajoPractico);
            if (n <= 0) {
                Log.Error(MensajeTrabajoPracticoInvalido(trabajoPractico));
                return 1;
            }
            numeroTp = n;
        }

        Alumnos alumnos = CargarAlumnos();
        List<Alumno> destinatarios = alumnos
            .Where(alumno => numeroTp switch {
                1 => alumno.EstadoPractico(1) != Estado.Aprobado,
                2 => alumno.EstadoPractico(2) != Estado.Aprobado,
                _ => alumno.EstadoPractico(1) != Estado.Aprobado && alumno.EstadoPractico(2) != Estado.Aprobado
            })
            .OrderBy(alumno => alumno.Comision)
            .ThenBy(alumno => alumno.NombreCompleto)
            .ThenBy(alumno => alumno.Legajo)
            .ToList();

        string etiqueta = numeroTp is int tp ? $"TP{tp}" : "TP1 y TP2";
        if (destinatarios.Count == 0) {
            Log.Info($"No hay alumnos con {etiqueta} pendiente de presentación.");
            return 0;
        }

        Log.Info($"{(simular ? "Simulación" : "Envío")} de WhatsApp a alumnos con {etiqueta} no presentado.");

        int enviados = 0;
        int omitidos = 0;

        if (simular) {
            foreach (Alumno alumno in destinatarios) {
                string mensaje = MensajeRecuperacion(alumno, numeroTp);
                if (string.IsNullOrWhiteSpace(alumno.TelefonoId)) {
                    omitidos++;
                    Log.Warning($"Omitido sin teléfono: {alumno.Legajo} | {alumno.NombreCompleto}");
                    continue;
                }
                Log.Info($"\n-- SIMULAR {alumno.Legajo} | {alumno.NombreCompleto} | {alumno.TelefonoId} ".PadRight(90, '-'));
                Log.WriteLine(mensaje);
            }
        } else {
            WAppService wapp = new();
            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
                .Start(ctx => {
                    var tarea = ctx.AddTask($"Enviando {etiqueta}", maxValue: destinatarios.Count);
                    foreach (Alumno alumno in destinatarios) {
                        string mensaje = MensajeRecuperacion(alumno, numeroTp);
                        if (string.IsNullOrWhiteSpace(alumno.TelefonoId)) {
                            omitidos++;
                            Log.Warning($"Omitido sin teléfono: {alumno.Legajo} | {alumno.NombreCompleto}");
                        } else {
                            try {
                                wapp.Enviar(alumno.TelefonoId, mensaje, null);
                                enviados++;
                                Log.Info($"Enviado: {alumno.Legajo} | {alumno.NombreCompleto} | {alumno.TelefonoId}");
                            } catch (Exception ex) {
                                omitidos++;
                                Log.Error($"No se pudo enviar a {alumno.Legajo} | {alumno.NombreCompleto}: {ex.Message}");
                            }
                        }
                        tarea.Increment(1);
                    }
                });
        }

        Log.Info($"Resumen WhatsApp {etiqueta}: destinatarios={destinatarios.Count}, enviados={enviados}, omitidos={omitidos}, simular={simular}");
        return omitidos > 0 && !simular ? 1 : 0;
    }

    public static int WappFotoParcial(bool simular) {
        Alumnos alumnos = CargarAlumnos();
        List<Alumno> destinatarios = alumnos
            .Where(alumno => !alumno.ConFoto)
            .OrderBy(alumno => alumno.Comision)
            .ThenBy(alumno => alumno.NombreCompleto)
            .ThenBy(alumno => alumno.Legajo)
            .ToList();

        if (destinatarios.Count == 0) {
            Log.Info("No hay alumnos sin foto de perfil.");
            return 0;
        }

        Log.Info($"{(simular ? "Simulación" : "Envío")} de WhatsApp a alumnos sin foto de perfil.");

        int enviados = 0;
        int omitidos = 0;

        if (simular) {
            foreach (Alumno alumno in destinatarios) {
                string mensaje = MensajeFotoParcial(alumno);
                if (string.IsNullOrWhiteSpace(alumno.TelefonoId)) {
                    omitidos++;
                    Log.Warning($"Omitido sin teléfono: {alumno.Legajo} | {alumno.NombreCompleto}");
                    continue;
                }
                Log.Info($"\n-- SIMULAR {alumno.Legajo} | {alumno.NombreCompleto} | {alumno.TelefonoId} ".PadRight(90, '-'));
                Log.WriteLine(mensaje);
            }
        } else {
            WAppService wapp = new();
            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
                .Start(ctx => {
                    var tarea = ctx.AddTask("Enviando foto parcial", maxValue: destinatarios.Count);
                    foreach (Alumno alumno in destinatarios) {
                        string mensaje = MensajeFotoParcial(alumno);
                        if (string.IsNullOrWhiteSpace(alumno.TelefonoId)) {
                            omitidos++;
                            Log.Warning($"Omitido sin teléfono: {alumno.Legajo} | {alumno.NombreCompleto}");
                        } else {
                            try {
                                wapp.Enviar(alumno.TelefonoId, mensaje, null);
                                enviados++;
                                Log.Info($"Enviado: {alumno.Legajo} | {alumno.NombreCompleto} | {alumno.TelefonoId}");
                            } catch (Exception ex) {
                                omitidos++;
                                Log.Error($"No se pudo enviar a {alumno.Legajo} | {alumno.NombreCompleto}: {ex.Message}");
                            }
                        }
                        tarea.Increment(1);
                    }
                });
        }

        Log.Info($"Resumen WhatsApp foto parcial: destinatarios={destinatarios.Count}, enviados={enviados}, omitidos={omitidos}, simular={simular}");
        return omitidos > 0 && !simular ? 1 : 0;
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

    static string ResolverRuta(string? ruta, string rutaPorDefecto) =>
        string.IsNullOrWhiteSpace(ruta) ? rutaPorDefecto : ruta;

    static string CarpetaTrabajoPractico(int numeroTp) => $"tp{numeroTp}";

    static void CargarAsistenciasHastaHoy(Alumnos alumnos, Action<string>? actualizarEstado = null) {
        actualizarEstado?.Invoke("Sincronizando WhatsApp...");
        WAppService wapp = new();
        DateTime hoy = DateTime.Today;

        DateTime desde = new(hoy.Year, 4, 1);
        DateTime hasta = hoy.AddHours(12).AddMinutes(30);
        Dictionary<int, HashSet<DateTime>> asistenciasPorAlumno = alumnos
            .ToDictionary(alumno => alumno.Legajo, _ => new HashSet<DateTime>());

        foreach (var grupo in new[] { "C7", "C9" }) {
            actualizarEstado?.Invoke($"Leyendo mensajes de {grupo}...");
            foreach (var mensaje in wapp.Mensajes(grupo, desde, hasta)) {
                if (!EsMensajeDeAsistencia(mensaje, desde, hasta)) { continue; }

                string telefono = wapp.ObtenerTelefonoAutorMensaje(mensaje);
                Alumno? alumno = alumnos.BuscarPorTelefono(telefono);

                if (alumno is null) { continue; }

                asistenciasPorAlumno[alumno.Legajo].Add(mensaje.Fecha.Date);
            }
        }

        actualizarEstado?.Invoke("Consolidando asistencias...");
        foreach (Alumno alumno in alumnos) {
            HashSet<DateTime> fechas = asistenciasPorAlumno[alumno.Legajo];
            alumno.Presente = fechas.Contains(hoy.Date);
            alumno.Asistencias = fechas.Count(fecha => fecha < hoy.Date);
        }
    }

    static void CargarCodigosDesdeWhatsApp(Alumnos alumnos, Action<string>? actualizarEstado = null) {
        actualizarEstado?.Invoke("Sincronizando WhatsApp...");
        WAppService wapp = new();
        DateTime desde = DateTime.Today.AddDays(-30);
        DateTime hasta = DateTime.Today.AddDays(1);
        Dictionary<int, (DateTime Fecha, string Codigo, string Origen)> codigosDetectados = new();

        foreach (string grupo in new[] { "C7", "C9" }) {
            actualizarEstado?.Invoke($"Leyendo mensajes del grupo {grupo}...");
            try {
                foreach (MensajeWhatsApp mensaje in wapp.Mensajes(grupo, desde, hasta)) {
                    if (mensaje.FromMe) { continue; }

                    string? codigo = ExtraerCodigoDesdeTexto(mensaje.Content);
                    if (codigo is null) { continue; }

                    Alumno? alumno = ExtraerLegajoDesdeCodigo(codigo) is int legajo
                        ? alumnos.BuscarPorLegajo(legajo)
                        : null;

                    if (alumno is null) {
                        string telefono = wapp.ObtenerTelefonoAutorMensaje(mensaje);
                        alumno = alumnos.BuscarPorTelefono(telefono);
                    }

                    if (alumno is null) { continue; }

                    RegistrarCodigoDetectado(alumno, mensaje, codigo, grupo);
                }
            } catch (Exception ex) {
                Log.Warning($"No se pudieron leer mensajes del grupo {grupo}: {ex.Message}");
            }
        }

        foreach (Alumno alumno in alumnos) {
            if (string.IsNullOrWhiteSpace(alumno.TelefonoId)) { continue; }

            actualizarEstado?.Invoke($"Leyendo chat privado de {alumno.NombreCompleto}...");
            try {
                foreach (MensajeWhatsApp mensaje in wapp.Mensajes(alumno.TelefonoId, desde, hasta)) {
                    if (mensaje.FromMe) { continue; }

                    string? codigo = ExtraerCodigoDesdeTexto(mensaje.Content);
                    if (codigo is not null) {
                        RegistrarCodigoDetectado(alumno, mensaje, codigo, "privado");
                    }
                }
            } catch (Exception ex) {
                Log.Warning($"No se pudieron leer mensajes de {alumno.NombreCompleto}: {ex.Message}");
            }
        }

        actualizarEstado?.Invoke("Consolidando códigos...");

        foreach (Alumno alumno in alumnos) {
            if (!codigosDetectados.TryGetValue(alumno.Legajo, out var detectado)) { continue; }

            alumno.Codigo = detectado.Codigo;
            alumno.Presente = true;
            Log.Info($"Código detectado ({detectado.Origen}) [{detectado.Fecha:HH:mm}]: {alumno.NombreCompleto} → {detectado.Codigo}");
        }

        void RegistrarCodigoDetectado(Alumno alumno, MensajeWhatsApp mensaje, string codigo, string origen) {
            if (codigosDetectados.TryGetValue(alumno.Legajo, out var anterior) && anterior.Fecha > mensaje.Fecha) {
                return;
            }

            codigosDetectados[alumno.Legajo] = (mensaje.Fecha, codigo, origen);
        }
    }

    static string? ExtraerCodigoDesdeTexto(string texto) {
        Match m = Regex.Match(texto, @"\b\d+\.\d+\.[a-zA-Z0-9]+(?:\.\d{2})?\b");
        return m.Success ? m.Value : null;
    }

    static int? ExtraerLegajoDesdeCodigo(string codigo) {
        string primerParte = codigo.Split('.')[0];
        return int.TryParse(primerParte, out int legajo) ? legajo : null;
    }

    static bool EsMensajeDeAsistencia(MensajeWhatsApp mensaje, DateTime desde, DateTime hasta) {
        if (mensaje.FromMe || mensaje.Fecha < desde || mensaje.Fecha > hasta) { return false; }

        DayOfWeek dia = mensaje.Fecha.DayOfWeek;
        TimeSpan hora = mensaje.Fecha.TimeOfDay;
        return
            dia >= DayOfWeek.Monday && dia <= DayOfWeek.Thursday &&
            hora >= new TimeSpan(8, 0, 0) && hora <= new TimeSpan(12, 30, 0);
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
                    string anterior = numeroTp == 1 ? "" : alumno.Codigo;
                    alumno.Codigo = $"{anterior} TP{numeroTp}:{codigo}".Trim();
                    Console.WriteLine($"{numeroTp} -> {anterior} | {alumno.Codigo}");
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

    static bool PracticoParecePresentado(int numeroTp, int lineasTotales, int lineasAgregadas) =>
        numeroTp switch {
            1 => lineasTotales   >= 100,
            2 => lineasAgregadas >= 20,
            3 => lineasAgregadas >= 50,
            _ => lineasTotales   >= 100
        };

    static bool TieneAlgunPracticoPresentado(Alumno alumno) =>
        alumno.practicos.Any(estado => estado == Estado.Aprobado);

    sealed record TrabajoPresentadoLocal(Alumno Alumno, string RutaPractico, HashSet<string> LineasCodigo);

    readonly record struct CopiaDetectada(int LineasComunes, int MaximoLineas, double Porcentaje);

    static string MensajeFotoParcial(Alumno alumno) =>
        $"""
        Hola *{alumno.Nombre}*.

        No tengo registrada una foto tuya en el sistema y mañana tenemos el parcial.

        Respondé este mensaje con una *selfie simple* para que pueda identificarte durante el examen. 📸
        """;

    static string MensajeRecuperacion(Alumno alumno, int? numeroTp) {
        string tpsTexto = numeroTp switch {
            1 => "el trabajo práctico 1",
            2 => "el trabajo práctico 2",
            3 => "el trabajo práctico 3",
            4 => "el trabajo práctico 4",
            5 => "el trabajo práctico 5",
            _ => "el trabajo práctico 1 ni el trabajo práctico 2"
        };

        return $"""
            Hola *{alumno.Nombre}*.

            Según mi registro, no has presentado {tpsTexto}.

            Podes recuperar el trabajo si lo presentas hasta el próximo jueves {ProximoJueves():dd/MM}.
            """;
    }

    static DateTime ProximoJueves() {
        int dias = ((int)DayOfWeek.Thursday - (int)DateTime.Today.DayOfWeek + 7) % 7;
        if (dias == 0) { dias = 7; }

        return DateTime.Today.AddDays(dias);
    }
}
