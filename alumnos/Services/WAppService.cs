using System.Runtime.InteropServices.Marshalling;

namespace Tup26.AlumnosApp;

/*
# WAppService

Servicio para interactuar con WhatsApp mediante `wacli`.

## Funciones públicas

- `Sincronizar()`: actualiza la base local desde `wacli` y recarga los grupos disponibles.

- `Enviar(destinatario, mensaje)`: envía un mensaje a un contacto o grupo resolviendo nombre, teléfono o JID.
    - `destinatario`: nombre, teléfono o JID del contacto o grupo.
    - `mensaje`: contenido del mensaje.

- `Invitar(grupo, usuarios)`: invita uno o más usuarios a un grupo.
    - `grupo`: grupo destino.
    - `usuarios`: usuarios a invitar.

- `Grupos()`: devuelve la lista de grupos disponibles.

- `Participantes(grupo)`: obtiene los participantes de un grupo.
    - `grupo`: grupo a consultar.

- `Mensajes(referencia, desde, hasta)`: devuelve los mensajes de una conversación filtrando opcionalmente por rango de fechas.
    - `referencia`: contacto, grupo, teléfono o JID.
    - `desde`: fecha/hora inicial opcional.
    - `hasta`: fecha/hora final opcional.

*/

class WAppService {
    readonly string? store;
    readonly TimeSpan timeout;

    public WAppService(string? store = null, TimeSpan? timeout = null) {
        this.store   = store;
        this.timeout = timeout ?? TimeSpan.FromMinutes(5);

        Sincronizar();
    }

    static void ValidarNoVacio(string valor, string parametro) {
        if (string.IsNullOrWhiteSpace(valor)) {
            throw new ArgumentException($"El valor de '{parametro}' no puede estar vacío.", parametro);
        }
    }

    static void ValidarNoVacio(string[] valores, string parametros) {
        if (valores == null || valores.Length == 0 || valores.All(string.IsNullOrWhiteSpace)) {
            throw new ArgumentException($"El valor de '{parametros}' no puede estar vacío.", parametros);
        }
    }

    static void ValidarTrue(bool condicion, string mensaje) {
        if (!condicion) {
            throw new ArgumentException(mensaje);
        }
    }

    public void Enviar(string destinatario, string mensaje) {
        ValidarNoVacio(destinatario, nameof(destinatario));
        ValidarNoVacio(mensaje, nameof(mensaje));

        string destino = ResolverDestinoMensaje(destinatario);
        List<string> argumentos = ["send", "text", "--to", destino, "--message", mensaje];
        Ejecutar(argumentos);
    }


    public void Invitar(string grupo, params string[] contactos) {
        ValidarNoVacio(grupo, nameof(grupo));
        ValidarNoVacio(contactos, nameof(contactos));

        string grupoJid = ResolverJidGrupo(grupo);
        List<string> argumentos = ["groups", "participants", "add", "--jid", grupoJid ];

        foreach (string usuario in contactos) {
            if (string.IsNullOrWhiteSpace(usuario)) { continue; }

            argumentos.Add("--user");
            argumentos.Add(usuario);
        }

        if (!argumentos.Any(argumento => argumento == "--user")) {
            throw new ArgumentException("Debes indicar al menos un participante válido.", nameof(contactos));
        }

        Ejecutar(argumentos);
    }

    void Sincronizar() {
        try {
            Ejecutar([ "groups", "refresh" ]);
        }
        catch (InvalidOperationException ex) when (EsErrorAutenticacionWacli(ex)) {
            Log.Warning("Aviso: wacli no está autenticado; se usa la base local para grupos y contactos.");
        }
    }

    public List<ContactoWhatsApp> Participantes(string grupo) {
        ValidarNoVacio(grupo, nameof(grupo));

        string? grupoJid = BuscarJidGrupoEnBaseLocal(grupo);
        if (string.IsNullOrWhiteSpace(grupoJid)) {
            throw new InvalidOperationException($"No se encontró ningún grupo para '{grupo}'.");
        }

        return ListarParticipantesDesdeBaseLocal(grupoJid);
    }

    public List<GrupoWhatsApp> Grupos() {
        return ListarGruposDesdeBaseLocal();
    }

    public List<MensajeWhatsApp> Mensajes(string referencia, DateTime? desde = null, DateTime? hasta = null) {
        ValidarNoVacio(referencia, nameof(referencia));
        ValidarTrue(!desde.HasValue || !hasta.HasValue || desde.Value <= hasta.Value, "La fecha inicial no puede ser mayor que la fecha final.");

        string chatJid = ResolverDestinoMensaje(referencia);
        return ListarMensajesDesdeBaseLocal(chatJid, desde, hasta);
    }

    string ResolverJidGrupo(string grupo) {
        ValidarNoVacio(grupo, nameof(grupo));

        string referencia = grupo.Trim();

        if (EsJidGrupo(referencia)) {
            return referencia;
        }

        string? grupoJid = BuscarJidGrupoEnBaseLocal(referencia);
        if (!string.IsNullOrWhiteSpace(grupoJid)) {
            return grupoJid;
        }

        throw new InvalidOperationException($"No se encontró ningún grupo para '{referencia}'.");
    }

    string ResolverDestinoMensaje(string destinatario) {
        ValidarNoVacio(destinatario, nameof(destinatario));

        string referencia = destinatario.Trim();

        if (EsJidWhatsapp(referencia) || EsJidGrupo(referencia)) {
            return referencia;
        }

        if (EsReferenciaTelefonica(referencia)) {
            return FormatearTelefonoJid(referencia);
        }

        string? grupoJid = BuscarJidGrupoEnBaseLocal(referencia);
        if (!string.IsNullOrWhiteSpace(grupoJid)) {
            return grupoJid;
        }

        ContactoWhatsApp? coincidencia = BuscarContactoPorReferenciaEnBaseLocal(referencia);
        if (coincidencia != null) {
            return coincidencia.Jid;
        }

        throw new InvalidOperationException($"No se encontró ningún destino para '{referencia}'.");
    }

    string? BuscarJidGrupoEnBaseLocal(string referencia) {
        string valor = EscaparSqlite(referencia);

        List<string> coincidenciasExactas = EjecutarSqlite(
            $"SELECT jid FROM groups WHERE lower(name) = lower('{valor}') OR lower(jid) = lower('{valor}') ORDER BY name, jid;");

        if (coincidenciasExactas.Count == 1) {
            return coincidenciasExactas[0];
        }

        if (coincidenciasExactas.Count > 1) {
            throw new InvalidOperationException($"La referencia exacta '{referencia}' coincide con varios grupos locales.");
        }

        List<string> coincidenciasParciales = EjecutarSqlite(
            $"SELECT jid FROM groups WHERE lower(name) LIKE lower('%{valor}%') OR lower(jid) LIKE lower('%{valor}%') ORDER BY name, jid;");

        if (coincidenciasParciales.Count == 1) {
            return coincidenciasParciales[0];
        }

        if (coincidenciasParciales.Count > 1) {
            throw new InvalidOperationException($"La búsqueda '{referencia}' coincide con varios grupos locales.");
        }

        return null;
    }

    ContactoWhatsApp? BuscarContactoPorReferenciaEnBaseLocal(string referencia) {
        string valor = EscaparSqlite(referencia);

        List<ContactoWhatsApp> coincidenciasExactas = EjecutarSqlite(EjecutarSqliteConSessionDb(ConstruirConsultaContactos(
            $"lower(nombre) = lower('{valor}') OR lower(jid) = lower('{valor}') OR lower(telefono) = lower('{valor}')")))
            .Select(ParsearContactoWhatsApp)
            .GroupBy(contacto => contacto.Jid, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => grupo.First())
            .ToList();

        if (coincidenciasExactas.Count == 1) {
            return coincidenciasExactas[0];
        }

        if (coincidenciasExactas.Count > 1) {
            string nombres = string.Join(", ", coincidenciasExactas.Select(contacto => $"{contacto.Name} ({contacto.Jid})"));
            throw new InvalidOperationException($"La referencia exacta '{referencia}' coincide con varios contactos: {nombres}");
        }

        List<ContactoWhatsApp> coincidenciasParciales = EjecutarSqlite(EjecutarSqliteConSessionDb(ConstruirConsultaContactos(
            $"lower(nombre) LIKE lower('%{valor}%') OR lower(jid) LIKE lower('%{valor}%') OR lower(telefono) LIKE lower('%{valor}%')")))
            .Select(ParsearContactoWhatsApp)
            .GroupBy(contacto => contacto.Jid, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => grupo.First())
            .ToList();

        if (coincidenciasParciales.Count == 1) {
            return coincidenciasParciales[0];
        }

        if (coincidenciasParciales.Count > 1) {
            string nombres = string.Join(", ", coincidenciasParciales.Select(contacto => $"{contacto.Name} ({contacto.Jid})"));
            throw new InvalidOperationException($"La búsqueda '{referencia}' coincide con varios contactos: {nombres}");
        }

        return null;
    }

    List<GrupoWhatsApp> ListarGruposDesdeBaseLocal() {
        return EjecutarSqlite(
            "SELECT COALESCE(name, '') || char(9) || jid FROM groups ORDER BY name, jid;")
            .Select(ParsearGrupoWhatsApp)
            .ToList();
    }

    List<ContactoWhatsApp> ListarParticipantesDesdeBaseLocal(string grupoJid) {
        string jid = EscaparSqlite(grupoJid);
        string rutaSessionDb = EscaparSqlite(Path.Combine(ObtenerDirectorioStore(), "session.db"));

        List<string> filas = EjecutarSqlite(
            $@"ATTACH DATABASE '{rutaSessionDb}' AS session;
                SELECT
                    gp.user_jid || char(9) ||
                    COALESCE(
                        NULLIF(ca.alias, ''),
                        NULLIF(c.full_name, ''),
                        NULLIF(c.push_name, ''),
                        NULLIF(c.business_name, ''),
                        NULLIF(c.first_name, ''),
                        NULLIF(sc.full_name, ''),
                        NULLIF(sc.push_name, ''),
                        NULLIF(sc.business_name, ''),
                        NULLIF(sc.first_name, ''),
                        gp.user_jid
                    ) || char(9) ||
                    COALESCE(
                        NULLIF(c.phone, ''),
                        NULLIF(sc.redacted_phone, ''),
                        NULLIF(lm.pn, ''),
                        ''
                    )
                FROM group_participants gp
                LEFT JOIN session.whatsmeow_lid_map lm
                    ON replace(gp.user_jid, '@lid', '') = lm.lid
                LEFT JOIN contacts c
                    ON c.phone = lm.pn
                    OR c.jid = lm.pn || '@s.whatsapp.net'
                LEFT JOIN contact_aliases ca
                    ON ca.jid = gp.user_jid
                    OR ca.jid = lm.pn || '@s.whatsapp.net'
                LEFT JOIN session.whatsmeow_contacts sc
                    ON sc.their_jid = gp.user_jid
                    OR sc.their_jid = lm.pn || '@s.whatsapp.net'
                WHERE gp.group_jid = '{jid}'
                ORDER BY CASE gp.role WHEN 'superadmin' THEN 0 WHEN 'admin' THEN 1 ELSE 2 END, gp.user_jid;");

        return filas.Select(ParsearContactoWhatsApp).ToList();
    }

    List<MensajeWhatsApp> ListarMensajesDesdeBaseLocal(string chatJid, DateTime? desde, DateTime? hasta) {
        string jid = EscaparSqlite(chatJid);
        List<string> condiciones = new() { $"chat_jid = '{jid}'" };

        if (desde.HasValue) {
            condiciones.Add($"ts >= {ConvertirFechaAUnix(desde.Value)}");
        }

        if (hasta.HasValue) {
            condiciones.Add($"ts <= {ConvertirFechaAUnix(hasta.Value)}");
        }

        List<string> filas = EjecutarSqlite(
            $"SELECT ts || char(9) || COALESCE(sender_jid, '') || char(9) || COALESCE(sender_name, '') || char(9) || COALESCE(NULLIF(display_text, ''), NULLIF(text, ''), '') || char(9) || from_me FROM messages WHERE {string.Join(" AND ", condiciones)} ORDER BY ts;");

        return filas.Select(ParsearMensajeWhatsApp).ToList();
    }

    static ContactoWhatsApp ParsearContactoWhatsApp(string fila) {
        string[] partes = fila.Split('\t');

        string jid = partes.Length > 0 ? partes[0].Trim() : string.Empty;
        string nombre = partes.Length > 1 ? partes[1].Trim() : string.Empty;
        string telefono = partes.Length > 2 ? partes[2].Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(nombre)) {
            nombre = jid;
        }

        if (string.IsNullOrWhiteSpace(telefono)) {
            telefono = ExtraerTelefonoDesdeJid(jid);
        }

        return new(jid, nombre, telefono);
    }

    static GrupoWhatsApp ParsearGrupoWhatsApp(string fila) {
        string[] partes = fila.Split('\t');

        string group = partes.Length > 0 ? partes[0].Trim() : string.Empty;
        string jid = partes.Length > 1 ? partes[1].Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(group)) {
            group = jid;
        }

        return new(jid, group, null);
    }

    static MensajeWhatsApp ParsearMensajeWhatsApp(string fila) {
        string[] partes = fila.Split('\t');

        long ts = partes.Length > 0 && long.TryParse(partes[0].Trim(), out long valorTs) ? valorTs : 0;
        string senderJid = partes.Length > 1 ? partes[1].Trim() : string.Empty;
        string senderName = partes.Length > 2 ? partes[2].Trim() : string.Empty;
        string content = partes.Length > 3 ? partes[3].Trim() : string.Empty;
        bool fromMe = partes.Length > 4 && (partes[4].Trim() == "1" || partes[4].Trim().Equals("true", StringComparison.OrdinalIgnoreCase));

        DateTime fecha = ConvertirUnixAFechaLocal(ts);

        if (string.IsNullOrWhiteSpace(senderName)) {
            senderName = senderJid;
        }

        return new(fecha, senderJid, senderName, content, fromMe);
    }

    static string ExtraerTelefonoDesdeJid(string jid) {
        if (string.IsNullOrWhiteSpace(jid)) {
            return string.Empty;
        }

        int separador = jid.IndexOf('@');
        string candidato = separador >= 0 ? jid.Substring(0, separador) : jid;

        return candidato.All(char.IsDigit) ? candidato : string.Empty;
    }

    static bool EsJidWhatsapp(string texto) {
        return texto.EndsWith("@g.us", StringComparison.OrdinalIgnoreCase) ||
               texto.EndsWith("@s.whatsapp.net", StringComparison.OrdinalIgnoreCase) ||
               texto.EndsWith("@lid", StringComparison.OrdinalIgnoreCase);
    }

    static bool EsReferenciaTelefonica(string texto) {
        string sinFormato = Regex.Replace(texto, @"[\d\s\-\+\(\)]", string.Empty);
        string digitos = Regex.Replace(texto, @"\D", string.Empty);

        return string.IsNullOrWhiteSpace(sinFormato) && !string.IsNullOrWhiteSpace(digitos);
    }

    static string FormatearTelefonoJid(string telefono) {
        string digitos = Regex.Replace(telefono.Trim(), @"\D", string.Empty);

        if (string.IsNullOrWhiteSpace(digitos)) {
            throw new InvalidOperationException($"No se pudo resolver el teléfono '{telefono}'.");
        }

        if (digitos.StartsWith("54")) {
            if (!digitos.StartsWith("549")) {
                digitos = $"549{digitos.Substring(2)}";
            }
        }
        else {
            if (digitos.StartsWith("0")) {
                digitos = digitos.Substring(1);
            }

            digitos = $"549{digitos}";
        }

        return $"{digitos}@s.whatsapp.net";
    }

    static DateTime ConvertirUnixAFechaLocal(long unixSeconds) {
        if (unixSeconds <= 0) {
            return DateTime.UnixEpoch;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
    }

    static long ConvertirFechaAUnix(DateTime fecha) {
        DateTimeOffset offset = fecha.Kind switch {
            DateTimeKind.Utc => new DateTimeOffset(fecha, TimeSpan.Zero),
            DateTimeKind.Local => new DateTimeOffset(fecha),
            _ => new DateTimeOffset(DateTime.SpecifyKind(fecha, DateTimeKind.Local))
        };

        return offset.ToUnixTimeSeconds();
    }

    static string ConstruirConsultaContactos(string whereClause) {
        return $@"ATTACH DATABASE '{{SESSION_DB}}' AS session;
                WITH candidatos AS (
                    SELECT DISTINCT
                        COALESCE(NULLIF(c.jid, ''), CASE WHEN NULLIF(c.phone, '') IS NOT NULL THEN c.phone || '@s.whatsapp.net' END) AS jid,
                        COALESCE(
                            NULLIF(ca.alias, ''),
                            NULLIF(c.full_name, ''),
                            NULLIF(c.push_name, ''),
                            NULLIF(c.business_name, ''),
                            NULLIF(c.first_name, ''),
                            NULLIF(c.phone, ''),
                            NULLIF(c.jid, '')
                        ) AS nombre,
                        COALESCE(NULLIF(c.phone, ''), '') AS telefono
                    FROM contacts c
                    LEFT JOIN contact_aliases ca
                        ON ca.jid = c.jid
                        OR ca.jid = c.phone || '@s.whatsapp.net'

                    UNION

                    SELECT DISTINCT
                        COALESCE(NULLIF(sc.their_jid, ''), CASE WHEN NULLIF(sc.redacted_phone, '') IS NOT NULL THEN sc.redacted_phone || '@s.whatsapp.net' END) AS jid,
                        COALESCE(
                            NULLIF(ca.alias, ''),
                            NULLIF(sc.full_name, ''),
                            NULLIF(sc.push_name, ''),
                            NULLIF(sc.business_name, ''),
                            NULLIF(sc.first_name, ''),
                            NULLIF(sc.redacted_phone, ''),
                            NULLIF(sc.their_jid, '')
                        ) AS nombre,
                        COALESCE(NULLIF(sc.redacted_phone, ''), '') AS telefono
                    FROM session.whatsmeow_contacts sc
                    LEFT JOIN contact_aliases ca
                        ON ca.jid = sc.their_jid
                        OR ca.jid = sc.redacted_phone || '@s.whatsapp.net'
                )
                SELECT DISTINCT
                    jid || char(9) || COALESCE(nombre, jid, '') || char(9) || COALESCE(telefono, '')
                FROM candidatos
                WHERE jid IS NOT NULL
                    AND jid <> ''
                    AND ({whereClause});";
    }

    List<string> EjecutarSqlite(string query) {
        string rutaDb = Path.Combine(ObtenerDirectorioStore(), "wacli.db");

        if (!File.Exists(rutaDb)) {
            Log.Warning($"Aviso: no existe la base local de wacli: {rutaDb}");
            return new();
        }

        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = "sqlite3",
            RedirectStandardInput = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(rutaDb);
        startInfo.ArgumentList.Add(query);

        using Process proceso = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar sqlite3.");

        string salida = proceso.StandardOutput.ReadToEnd().Trim();
        string error = proceso.StandardError.ReadToEnd().Trim();

        proceso.WaitForExit();

        if (proceso.ExitCode != 0) {
            string detalle = string.IsNullOrWhiteSpace(error) ? salida : error;
            throw new InvalidOperationException($"sqlite3 falló con código {proceso.ExitCode}: {detalle}");
        }

        return salida.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(linea => linea.Trim())
                     .Where(linea => !string.IsNullOrWhiteSpace(linea))
                     .ToList();
    }

    string ObtenerDirectorioStore() {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(store)) {
            return Path.Combine(home, ".wacli");
        }

        if (store == "~") {
            return home;
        }

        if (store.StartsWith("~/", StringComparison.Ordinal)) {
            return Path.Combine(home, store.Substring(2));
        }

        return Environment.ExpandEnvironmentVariables(store);
    }

    static string EscaparSqlite(string valor) {
        return valor.Trim().Replace("'", "''");
    }

    static bool EsJidGrupo(string texto) {
        return texto.EndsWith("@g.us", StringComparison.OrdinalIgnoreCase);
    }

    void Ejecutar(List<string> argumentos) {
        string salida = EjecutarYObtenerSalida(argumentos);

        if (!string.IsNullOrWhiteSpace(salida)) {
            Log.Info(salida);
        }
    }

    string EjecutarYObtenerSalida(List<string> argumentos) {
        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = "wacli",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(store)) {
            startInfo.ArgumentList.Add("--store");
            startInfo.ArgumentList.Add(store);
        }

        startInfo.ArgumentList.Add("--timeout");
        startInfo.ArgumentList.Add(FormatearDuracionWacli(timeout));

        foreach (string argumento in argumentos) {
            startInfo.ArgumentList.Add(argumento);
        }

        using Process proceso = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar wacli.");

        proceso.StandardInput.Close();

        if (!proceso.WaitForExit((int)timeout.TotalMilliseconds)) {
            try {
                proceso.Kill(entireProcessTree: true);
            }
            catch {
                // Ignoramos errores al intentar detener el proceso.
            }

            throw new TimeoutException($"wacli no terminó dentro de {timeout}.");
        }

        string salida = proceso.StandardOutput.ReadToEnd().Trim();
        string error = proceso.StandardError.ReadToEnd().Trim();

        if (proceso.ExitCode != 0) {
            string detalle = string.IsNullOrWhiteSpace(error) ? salida : error;
            throw new InvalidOperationException($"wacli falló con código {proceso.ExitCode}: {detalle}");
        }

        return salida;
    }

    string EjecutarSqliteConSessionDb(string query) {
        string rutaSessionDb = EscaparSqlite(Path.Combine(ObtenerDirectorioStore(), "session.db"));
        return query.Replace("{SESSION_DB}", rutaSessionDb, StringComparison.Ordinal);
    }

    static string FormatearDuracionWacli(TimeSpan duracion) {
        if (duracion <= TimeSpan.Zero) {
            return "1s";
        }

        if (duracion.TotalSeconds < 60 && duracion.TotalSeconds == Math.Floor(duracion.TotalSeconds)) {
            return $"{(int)duracion.TotalSeconds}s";
        }

        if (duracion.TotalMinutes < 60 && duracion.TotalSeconds % 60 == 0) {
            return $"{(int)duracion.TotalMinutes}m";
        }

        int horas = (int)duracion.TotalHours;
        int minutos = duracion.Minutes;
        int segundos = duracion.Seconds;

        StringBuilder sb = new();

        if (horas > 0) {
            sb.Append($"{horas}h");
        }

        if (minutos > 0) {
            sb.Append($"{minutos}m");
        }

        if (segundos > 0 || sb.Length == 0) {
            sb.Append($"{segundos}s");
        }

        return sb.ToString();
    }

    static bool EsErrorAutenticacionWacli(Exception ex) {
        return ex.Message.Contains("not authenticated", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase);
    }
}

record ContactoWhatsApp(string Jid, string Name, string PhoneNumber);
record GrupoWhatsApp(string Jid, string Group, DateTime? Creado);
record MensajeWhatsApp(DateTime Fecha, string SenderJid, string SenderName, string Content, bool FromMe);
