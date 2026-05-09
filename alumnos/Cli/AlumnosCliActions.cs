namespace Tup26.AlumnosApp;

static class AlumnosCliActions {
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
        Alumnos alumnos = CargarAlumnos();
        IEnumerable<Alumno> noPresentaron = alumnos.Where(alumno => alumno.EstadoPractico(1) != Estado.Aprobado);

        AlumnosManager.Listar(noPresentaron, "Alumnos que no presentaron TP1");
        return 0;
    }

    public static int ListarTp2NoPresentado() {
        Alumnos alumnos = CargarAlumnos();
        IEnumerable<Alumno> noPresentaron = alumnos.Where(alumno => alumno.EstadoPractico(2) != Estado.Aprobado);

        AlumnosManager.Listar(noPresentaron, "Alumnos que no presentaron TP2");
        return 0;
    }

    public static int LimpiarProyectosPracticos() {
        List<string> directoriosEliminados = AppPaths.LimpiarDirectoriosCompilacionPracticos();

        if (directoriosEliminados.Count == 0) {
            Log.Info("No se encontraron carpetas bin u obj dentro de prácticos.");
            return 0;
        }

        foreach (string directorio in directoriosEliminados) {
            Log.Info($"Eliminado: {AppPaths.RutaRelativaDesdePracticos(directorio)}");
        }

        Log.Info($"Total de carpetas eliminadas: {directoriosEliminados.Count}");
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

        const int minimoLineasAgregadas = 100;
        string carpetaTp = $"tp{numeroTp}";
        string rutaEnunciado = AppPaths.EnunciadoPracticoDirectory(carpetaTp);
        int lineasEnunciado = AppPaths.ContarLineasArchivos(rutaEnunciado, "*.cs");

        Log.Info($"{carpetaTp.ToUpperInvariant()} | líneas base del enunciado: {lineasEnunciado}");
        int marcados = 0;

        foreach (Alumno alumno in alumnos.OrderBy(alumno => alumno.Legajo)) {
            string rutaPractico = AppPaths.PracticoAlumnoSubdirectory(alumno, carpetaTp);
            int lineasTotales = ContarLineasPracticoLocal(rutaPractico);
            int lineasAgregadas = Math.Max(0, lineasTotales - lineasEnunciado);

            Estado estado = Estado.Desaprobado;
            if (lineasAgregadas >= minimoLineasAgregadas) {
                estado = Estado.Aprobado;
                marcados++;
            }

            alumno.Practico(numeroTp, estado);
            Log.Info($"{alumno.Legajo} | {alumno.NombreCompleto,-40} | L:{lineasTotales,4} | L+:{lineasAgregadas,4} | marcado    {estado.ToEmoji()}");
        }

        if (marcados > 0) {
            AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        }

        Log.Info($"Resumen TP{numeroTp}: marcados={marcados}, total={alumnos.Count()}, porcentaje={marcados * 100.0 / alumnos.Count():F2}%");
        return 0;
    }

    public static int RegistrarAsistencias() {
        Alumnos alumnos = CargarAlumnos();
        int contar = 0;

        foreach (Alumno alumno in alumnos) {
            if (alumno.Presente) {
                alumno.Asistencias++;
                contar++;
            }

            alumno.Presente = false;
            alumno.Examen(1, alumno.Asistencias switch{ >= 8 => Estado.Aprobado, >= 4 => Estado.Pendiente, _ => Estado.Desaprobado });
        }

        AlumnosManager.Escribir(alumnos, AppPaths.ArchivoAlumnos);
        AlumnosManager.Listar(alumnos.Where(alumno => alumno.Presente), "Alumnos presentes hoy");
        Log.WriteLine($"Asistencias registradas: {contar}");
        return 0;
    }

    public static int RelevarAsistencias() {
        Alumnos alumnos = CargarAlumnos();
        CargarAsistenciasHoy(alumnos);
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

    public static int WappRecuperarTp1Tp2(bool simular) {
        Alumnos alumnos = CargarAlumnos();
        List<Alumno> destinatarios = alumnos
            .Where(alumno => alumno.EstadoPractico(1) != Estado.Aprobado &&
                             alumno.EstadoPractico(2) != Estado.Aprobado)
            .OrderBy(alumno => alumno.Comision)
            .ThenBy(alumno => alumno.NombreCompleto)
            .ThenBy(alumno => alumno.Legajo)
            .ToList();

        if (destinatarios.Count == 0) {
            Log.Info("No hay alumnos con TP1 y TP2 pendientes de presentación.");
            return 0;
        }

        Log.Info($"{(simular ? "Simulación" : "Envío")} de WhatsApp a alumnos con TP1 y TP2 no presentados.");

        WAppService? wapp = simular ? null : new WAppService();
        int enviados = 0;
        int omitidos = 0;

        foreach (Alumno alumno in destinatarios) {
            string mensaje = MensajeRecuperacionTp1Tp2(alumno);

            if (string.IsNullOrWhiteSpace(alumno.TelefonoId)) {
                omitidos++;
                Log.Warning($"Omitido sin teléfono: {alumno.Legajo} | {alumno.NombreCompleto}");
                continue;
            }

            if (simular) {
                Log.Info($"SIMULAR {alumno.Legajo} | {alumno.NombreCompleto} | {alumno.TelefonoId}");
                Log.WriteLine(mensaje);
            } else {
                try {
                    wapp!.Enviar(alumno.TelefonoId, mensaje, null);
                    enviados++;
                    Log.Info($"Enviado: {alumno.Legajo} | {alumno.NombreCompleto} | {alumno.TelefonoId}");
                } catch (Exception ex) {
                    omitidos++;
                    Log.Error($"No se pudo enviar a {alumno.Legajo} | {alumno.NombreCompleto}: {ex.Message}");
                }
            }
        }

        Log.Info($"Resumen WhatsApp TP1/TP2: destinatarios={destinatarios.Count}, enviados={enviados}, omitidos={omitidos}, simular={simular}");
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

    static void CargarAsistenciasHoy(Alumnos alumnos) {
        WAppService wapp = new();
        DateTime hoy = DateTime.Today;

        DateTime desde = new(hoy.Year, 4, 1);
        DateTime hasta = hoy.AddHours(12).AddMinutes(30);
        Dictionary<int, HashSet<DateTime>> asistenciasPorAlumno = alumnos
            .ToDictionary(alumno => alumno.Legajo, _ => new HashSet<DateTime>());

        foreach (var grupo in new[] { "C7", "C9" }) {
            foreach (var mensaje in wapp.Mensajes(grupo, desde, hasta)) {
                if (!EsMensajeDeAsistencia(mensaje, desde, hasta)) {
                    continue;
                }

                string telefono = wapp.ObtenerTelefonoAutorMensaje(mensaje);
                Alumno? alumno = alumnos.BuscarPorTelefono(telefono);

                if (alumno is null) {
                    continue;
                }

                asistenciasPorAlumno[alumno.Legajo].Add(mensaje.Fecha.Date);
            }
        }

        foreach (Alumno alumno in alumnos) {
            alumno.Asistencias = asistenciasPorAlumno[alumno.Legajo].Count;
        }

        AlumnosManager.Listar(alumnos.Where(alumno => alumno.Asistencias > 0), "Alumnos con asistencias desde el 01/04");
        Log.WriteLine($"Asistencias detectadas: {alumnos.Sum(alumno => alumno.Asistencias)}");
    }

    static bool EsMensajeDeAsistencia(MensajeWhatsApp mensaje, DateTime desde, DateTime hasta) {
        if (mensaje.FromMe || mensaje.Fecha < desde || mensaje.Fecha > hasta) {
            return false;
        }

        DayOfWeek dia = mensaje.Fecha.DayOfWeek;
        TimeSpan hora = mensaje.Fecha.TimeOfDay;
        return dia >= DayOfWeek.Monday &&
            dia <= DayOfWeek.Thursday &&
            hora >= new TimeSpan(8, 0, 0) &&
            hora <= new TimeSpan(12, 30, 0);
    }

    static int ContarLineasPracticoLocal(string rutaPractico) =>
        AppPaths.ContarLineasArchivos(rutaPractico, "*.cs", SearchOption.TopDirectoryOnly);

    static string MensajeRecuperacionTp1Tp2(Alumno alumno) =>
        $"""
        Hola {alumno.Nombre}. Según mi registro, no has presentado el trabajo práctico 1 ni el trabajo práctico 2.

        Tenés una oportunidad más para recuperar: presentalos ahora.
        Tenés tiempo hasta el próximo miércoles {ProximoMiercoles():dd/MM}.
        """;

    static DateTime ProximoMiercoles() {
        int dias = ((int)DayOfWeek.Wednesday - (int)DateTime.Today.DayOfWeek + 7) % 7;
        if (dias == 0) {
            dias = 7;
        }

        return DateTime.Today.AddDays(dias);
    }
}
