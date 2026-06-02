namespace Tup26.AlumnosApp;

/*
# AlumnosManager

Servicio estático para leer, transformar y exportar la información de alumnos.

## Funciones públicas

- `Leer()`: carga el archivo principal de alumnos desde la ruta por defecto.

- `Leer(rutaArchivo)`: carga alumnos desde un archivo Markdown.
    - `rutaArchivo`: ruta del archivo a leer.

- `Escribir(alumnos, rutaArchivo)`: guarda el listado de alumnos en formato Markdown.
    - `alumnos`: colección a persistir.
    - `rutaArchivo`: ruta destino.

- `Listar(alumnos, titulo)`: muestra el listado en consola como tabla.
    - `alumnos`: colección a mostrar.
    - `titulo`: encabezado opcional del listado.

- `CrearCarpetas(alumnos)`: crea o normaliza las carpetas de prácticos de cada alumno.
    - `alumnos`: colección a procesar.

- `PublicarPractico(alumnos, practico, forzar)`: normaliza carpetas de alumnos y copia el enunciado de un práctico.
    - `alumnos`: colección a procesar.
    - `practico`: nombre del práctico.
    - `forzar`: sobrescribe el destino si ya existe.

- `PublicarRehacer(alumnos, practico)`: normaliza carpetas de alumnos, borra el práctico destino y copia el enunciado.
    - `alumnos`: colección a procesar.
    - `practico`: nombre del práctico.

- `CopiarFotoPerfil(alumnos, rutaFotos)`: copia fotos de perfil a las carpetas de alumnos cuando corresponde.
    - `alumnos`: colección a procesar.
    - `rutaFotos`: carpeta base de fotos origen.

- `CopiarEnunciadoPracticos(alumnos, practico, forzar)`: copia el enunciado de un práctico a cada carpeta de alumno.
    - `alumnos`: colección a procesar.
    - `practico`: nombre del práctico.
    - `forzar`: sobrescribe el destino si ya existe.

- `ActualizarDesdePerfiles(alumnos, rutaPerfiles)`: actualiza datos de alumnos usando perfiles Markdown externos.
    - `alumnos`: colección a actualizar.
    - `rutaPerfiles`: carpeta con perfiles.

- `EscribirJSON(alumnos, rutaArchivo)`: exporta el listado de alumnos en JSON.
    - `alumnos`: colección a exportar.
    - `rutaArchivo`: ruta destino.

- `EscribirVCard(alumnos, rutaArchivo)`: exporta contactos de alumnos en formato vCard.
    - `alumnos`: colección a exportar.
    - `rutaArchivo`: ruta destino.

*/

static class AlumnosManager {

    public static Alumnos Leer() =>
        Leer(AppPaths.ArchivoAlumnos);

    public static Alumnos Leer(string rutaArchivo) {
        Alumnos alumnos = new(Array.Empty<Alumno>());
        string comisionActual = string.Empty;

        try {
            string[] lineas = AppPaths.LeerLineas(rutaArchivo);

            foreach (string linea in lineas) {
                if (string.IsNullOrWhiteSpace(linea)) {
                    continue;
                }

                if (linea.StartsWith("## ")) {
                    comisionActual = linea.Substring(3).Trim();
                    continue;
                }

                if (linea.StartsWith("#") || linea.StartsWith("```") || linea.StartsWith("Legajo") || linea.StartsWith("------")) {
                    continue;
                }

                Alumno? alumno = ExtraerAlumnoFormatoMarkdown(linea, comisionActual);

                if (alumno != null) {
                    alumnos.Agregar(alumno);
                }
            }
        } catch (Exception ex) {
            Log.Error($"Error al leer el archivo: {ex.Message}");
        }

        return alumnos;
    }

    public static void Escribir(IEnumerable<Alumno> alumnos, string rutaArchivo) {
        string[] etiquetas = ["Legajo", "Nombre y Apellido", "Teléfono", "Foto", "GitHub", "Prácticos", "Ex", "Pr", "Nr", "Nota", "Código"];
        var guiones = etiquetas.Select(e => new string('-', 80)).ToArray();
        try {
            List<Alumno> alumnosOrdenados = new(alumnos);
            alumnosOrdenados.Sort(Alumno.Comparar);

            StringBuilder sb = new();
            sb.AppendLine("# TUP 2026 - Programación III");
            sb.AppendLine();

            string? comisionActual = null;

            foreach (Alumno alumno in alumnosOrdenados) {
                string comisionAlumno = ObtenerComision(alumno);

                if (comisionActual != comisionAlumno) {
                    if (comisionActual != null) {
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }

                    comisionActual = comisionAlumno;
                    sb.AppendLine($"## {comisionActual}");
                    sb.AppendLine("```text");
                    sb.AppendLine(FormatearFilaTabla(etiquetas));
                    sb.AppendLine(FormatearFilaTabla(guiones));
                }

                sb.AppendLine(FormatearFila(alumno));
            }

            if (comisionActual != null) {
                sb.AppendLine("```");
            }

            AppPaths.EscribirAlumnosMarkdown(sb.ToString().TrimEnd() + Environment.NewLine, rutaArchivo);
            Log.Info($"Alumnos guardados en: {rutaArchivo}");
        } catch (Exception ex) {
            Log.Error($"Error al guardar el archivo: {ex.Message}");
        }
    }

    public static void EscribirEstadoInformer(IEnumerable<Alumno> alumnos, string rutaArchivo) {
        string[] etiquetas = ["Legajo", "Nombre y Apellido", "Prácticos", "Asistencias", "Nota"];
        var guiones = etiquetas.Select(_ => new string('-', 80)).ToArray();

        try {
            List<Alumno> alumnosOrdenados = new(alumnos);
            alumnosOrdenados.Sort(Alumno.Comparar);

            StringBuilder sb = new();
            sb.AppendLine("# TUP 2026 - Programación III");
            sb.AppendLine();

            string? comisionActual = null;

            foreach (Alumno alumno in alumnosOrdenados) {
                string comisionAlumno = ObtenerComision(alumno);

                if (comisionActual != comisionAlumno) {
                    if (comisionActual != null) {
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }

                    comisionActual = comisionAlumno;
                    sb.AppendLine($"## {comisionActual}");
                    sb.AppendLine("```text");
                    sb.AppendLine(FormatearFilaTablaEstadoInformer(etiquetas));
                    sb.AppendLine(FormatearFilaTablaEstadoInformer(guiones));
                }

                sb.AppendLine(FormatearFilaEstadoInformer(alumno));
            }

            if (comisionActual != null) {
                sb.AppendLine("```");
            }

            AppPaths.EscribirTexto(rutaArchivo, sb.ToString().TrimEnd() + Environment.NewLine, Encoding.UTF8);
            Log.Info($"Estado Informer publicado en: {rutaArchivo}");
        } catch (Exception ex) {
            Log.Error($"Error al publicar el estado Informer: {ex.Message}");
        }
    }

    public static void Listar(IEnumerable<Alumno> alumnos, string titulo = "Listado de Alumnos") {
        string[] campos = ["Legajo", "Nombre y Apellido", "Teléfono", "Foto", "GitHub", "Prácticos", "Ex", "Pr", "Nr", "Nota"];
        string[] guiones = campos.Select(c => new string('-', 40)).ToArray();

        string comision = "";
        if (!alumnos.Any()) {
            Log.Warning("No hay alumnos para mostrar.");
            return;
        }

        List<Alumno> alumnosOrdenados = alumnos.OrderBy(a => a.Comision).ThenBy(a => a.NombreCompleto).ThenBy(a => a.Legajo).ToList();

        string encabezado = FormatearFilaTabla(campos);

        Log.WriteLine($"[blue]=== {titulo.ToUpper()} ===");

        foreach (var a in alumnosOrdenados) {
            if (a.Comision != comision) {
                comision = a.Comision;
                Log.WriteLine($"\n[yellow]== {comision} ({alumnosOrdenados.Count(x => x.Comision == comision)}) ==");
                Log.WriteLine($"{FormatearFilaTabla(campos)}\n[blue]{FormatearFilaTabla(guiones)}");
            }
            Log.WriteLine(FormatearFilaListado(a));
        }

        Log.WriteLine($"\n[green]Total de alumnos: {alumnos.Count()}\n");
    }

    public static void CrearCarpetas(IEnumerable<Alumno> alumnos) {
        AppPaths.AsegurarDirectorioPracticos();

        foreach (Alumno alumno in alumnos) {
            AsegurarCarpetaAlumnoNormalizada(alumno);
        }
    }

    public static void PublicarPractico(IEnumerable<Alumno> alumnos, string practico, bool forzar = false) {
        string nombrePractico = practico.Trim();

        if (!PuedeCopiarEnunciadoPractico(nombrePractico, crearBasePracticos: true)) {
            return;
        }

        List<Alumno> alumnosPublicables = new();
        foreach (Alumno alumno in alumnos) {
            if (AsegurarCarpetaAlumnoNormalizada(alumno)) {
                alumnosPublicables.Add(alumno);
            }
        }

        CopiarEnunciadoPracticosEnCarpetasNormalizadas(alumnosPublicables, nombrePractico, forzar);
    }

    public static void PublicarRehacer(IEnumerable<Alumno> alumnos, string practico) {
        string nombrePractico = practico.Trim();

        if (!PuedeCopiarEnunciadoPractico(nombrePractico, crearBasePracticos: true)) {
            return;
        }

        List<Alumno> alumnosPublicables = new();
        foreach (Alumno alumno in alumnos) {
            if (AsegurarCarpetaAlumnoNormalizada(alumno)) {
                alumnosPublicables.Add(alumno);
            }
        }

        if (alumnosPublicables.Count == 0) {
            Log.Info("No hay alumnos para republicar.");
            return;
        }

        foreach (Alumno alumno in alumnosPublicables) {
            RepublicarEnunciadoPractico(alumno, nombrePractico);
        }
    }

    public static void CopiarFotoPerfil(IEnumerable<Alumno> alumnos, string rutaFotos) {
        if (!AppPaths.ExisteDirectorioPracticos()) {
            Log.Error($"No existe la carpeta base de prácticos: {AppPaths.PracticosDirectory}");
            return;
        }

        foreach (Alumno alumno in alumnos) {
            if (string.IsNullOrWhiteSpace(alumno.TelefonoId)) {
                continue;
            }

            if (!AppPaths.ExisteFotoPerfil(rutaFotos, alumno)) {
                continue;
            }

            string? rutaCarpetaAlumno = AppPaths.ObtenerCarpetaUnicaMismoLegajo(alumno.Legajo);
            if (!AppPaths.ExisteCarpetaAlumno(rutaCarpetaAlumno)) {
                continue;
            }

            if (AppPaths.ExisteFotoAlumno(rutaCarpetaAlumno!)) {
                continue;
            }

            try {
                CopiaRuta copia = AppPaths.CopiarFotoPerfil(rutaFotos, alumno, rutaCarpetaAlumno!);
                Log.Info($"Foto copiada: {copia.Origen} -> {copia.Destino}");
            } catch (Exception ex) {
                Log.Error($"Error al copiar la foto para {alumno.CarpetaNombre}: {ex.Message}");
            }
        }
    }

    public static void CopiarEnunciadoPracticos(IEnumerable<Alumno> alumnos, string practico, bool forzar = false) {
        string nombrePractico = practico.Trim();

        if (!PuedeCopiarEnunciadoPractico(nombrePractico, crearBasePracticos: false)) {
            return;
        }

        foreach (Alumno alumno in alumnos) {
            if (!AsegurarCarpetaAlumnoNormalizada(alumno)) {
                continue;
            }

            CopiarEnunciadoPractico(alumno, nombrePractico, forzar);
        }
    }

    static bool PuedeCopiarEnunciadoPractico(string nombrePractico, bool crearBasePracticos) {
        if (string.IsNullOrWhiteSpace(nombrePractico)) {
            Log.Error("Debe indicar el nombre del práctico a copiar.");
            return false;
        }

        if (!AppPaths.ExisteEnunciadoPractico(nombrePractico)) {
            Log.Error($"No existe la carpeta del enunciado: {AppPaths.EnunciadoPracticoDirectory(nombrePractico)}");
            return false;
        }

        if (crearBasePracticos) {
            try {
                AppPaths.AsegurarDirectorioPracticos();
            } catch (Exception ex) {
                Log.Error($"No se pudo crear la carpeta base de prácticos {AppPaths.PracticosDirectory}: {ex.Message}");
                return false;
            }

            return true;
        }

        if (!AppPaths.ExisteDirectorioPracticos()) {
            Log.Error($"No existe la carpeta base de prácticos: {AppPaths.PracticosDirectory}");
            return false;
        }

        return true;
    }

    static bool AsegurarCarpetaAlumnoNormalizada(Alumno alumno) {
        string nombreCarpeta = alumno.CarpetaNombre;
        string rutaCarpeta = AppPaths.RutaCarpetaAlumnoEsperada(alumno);

        try {
            List<string> carpetasConLegajo = AppPaths.BuscarCarpetasMismoLegajo(alumno.Legajo);

            if (!carpetasConLegajo.Any()) {
                AppPaths.AsegurarCarpetaAlumno(alumno);
                Log.Debug($" ➕ {rutaCarpeta,-40}");
                return true;
            }

            if (carpetasConLegajo.Count == 1) {
                string rutaCarpetaExistente = carpetasConLegajo[0];
                string rutaRelativa = AppPaths.RutaRelativaDesdePracticos(rutaCarpetaExistente);
                if (string.Equals(rutaCarpetaExistente, rutaCarpeta, StringComparison.OrdinalIgnoreCase)) {
                    Log.Info($" ✅ {rutaRelativa,-40}");
                } else {
                    AppPaths.RenombrarCarpetaAlumno(rutaCarpetaExistente, alumno);
                    Log.Warning($" 🔄 {rutaRelativa,-40} → {nombreCarpeta}");
                }

                return true;
            }

            Log.Warning($" ⚠️  {alumno.Legajo}. Revisar manualmente las duplicadas.");
            return false;
        } catch (Exception ex) {
            Log.Error($"Error al crear la carpeta para {nombreCarpeta}: {ex.Message}");
            return false;
        }
    }

    static void CopiarEnunciadoPracticosEnCarpetasNormalizadas(IEnumerable<Alumno> alumnos, string nombrePractico, bool forzar) {
        foreach (Alumno alumno in alumnos) {
            CopiarEnunciadoPractico(alumno, nombrePractico, forzar);
        }
    }

    static void CopiarEnunciadoPractico(Alumno alumno, string nombrePractico, bool forzar) {
        try {
            CopiaRuta copia = AppPaths.CopiarEnunciadoPractico(alumno, nombrePractico, forzar);
            Log.Info($"Enunciado copiado: {copia.Origen} -> {copia.Destino}");
        } catch (Exception ex) {
            Log.Error($"Error al copiar el enunciado para {alumno.CarpetaNombre}: {ex.Message}");
        }
    }

    static void RepublicarEnunciadoPractico(Alumno alumno, string nombrePractico) {
        try {
            string rutaDestino = AppPaths.BorrarPracticoAlumno(alumno, nombrePractico);
            Log.Warning($"Carpeta borrada: {rutaDestino}");

            CopiaRuta copia = AppPaths.CopiarEnunciadoPractico(alumno, nombrePractico, forzar: true);
            Log.Info($"Enunciado republicado: {copia.Origen} -> {copia.Destino}");
        } catch (Exception ex) {
            Log.Error($"Error al republicar el enunciado para {alumno.CarpetaNombre}: {ex.Message}");
        }
    }

    public static void ActualizarDesdePerfiles(IEnumerable<Alumno> alumnos, string rutaPerfiles) {
        Dictionary<int, Alumno> porLegajo = new();

        foreach (Alumno alumno in alumnos) {
            porLegajo[alumno.Legajo] = alumno;
        }

        foreach (PerfilMarkdown perfil in AppPaths.LeerPerfilesMarkdown(rutaPerfiles)) {
            try {
                int legajo = 0;
                string gitHub = string.Empty;

                foreach (string linea in perfil.Lineas) {
                    string l = linea.Trim();

                    if (l.StartsWith("- Legajo:")) {
                        string valor = l.Substring("- Legajo:".Length).Trim();
                        int.TryParse(valor, out legajo);
                    } else if (l.StartsWith("- Github:")) {
                        string valor = l.Substring("- Github:".Length).Trim();

                        if (!string.IsNullOrWhiteSpace(valor) &&
                            !valor.StartsWith("No", StringComparison.OrdinalIgnoreCase)) {
                            gitHub = valor;
                        }
                    }
                }

                if (legajo == 0 || !porLegajo.ContainsKey(legajo)) {
                    continue;
                }

                Alumno alumno = porLegajo[legajo];
                bool actualizado = false;

                if (!string.IsNullOrWhiteSpace(gitHub) && alumno.GitHub != gitHub) {
                    Log.Info($"  GitHub actualizado {alumno.CarpetaNombre}: '{alumno.GitHub}' -> '{gitHub}'");
                    alumno.GitHub = gitHub;
                    actualizado = true;
                }

                if (!actualizado) {
                    Log.Warning($"  Sin cambios: {alumno.CarpetaNombre}");
                }
            } catch (Exception ex) {
                Log.Error($"Error al leer perfil {perfil.Ruta}: {ex.Message}");
            }
        }
    }

    public static void EscribirJSON(IEnumerable<Alumno> alumnos, string rutaArchivo) {
        try {
            var datos = alumnos.Select(alumno => new {
                alumno.Legajo,
                alumno.Comision,
                alumno.Nombre,
                alumno.Apellido,
                alumno.Telefono,
                TieneFoto = alumno.ConFoto,
                GitHub = alumno.GitHub,
                alumno.Nota,
                alumno.Codigo,
                Practicos = alumno.practicos.Select(e => e.ToEmoji()).ToList(),
                Examenes = alumno.examenes.Select(e => e.ToEmoji()).ToList()
            });

            JsonSerializerOptions opciones = new() {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(datos, opciones);
            json = DesescaparUnicodeJsonLegible(json);

            AppPaths.EscribirAlumnosJson(json + Environment.NewLine, rutaArchivo);

            Log.Info($"Alumnos guardados en JSON: {rutaArchivo}");
        } catch (Exception ex) {
            Log.Error($"Error al guardar el archivo JSON: {ex.Message}");
        }
    }

    public static void EscribirVCard(IEnumerable<Alumno> alumnos, string rutaArchivo) {
        try {
            var alumnosConTelefono = alumnos.Where(a => a.ConTelefono)
                .OrderBy(a => a.Comision).ThenBy(a => a.NombreCompleto).ThenBy(a => a.Legajo);

            StringBuilder sb = new();
            foreach (Alumno alumno in alumnosConTelefono) {
                AppendVCardContacto(sb, alumno);
            }
            AppPaths.EscribirAlumnosVCard(sb.ToString(), rutaArchivo);

            Log.Info($"Contactos vCard guardados en: {rutaArchivo}");
        } catch (Exception ex) {
            Log.Error($"Error al guardar el archivo vCard: {ex.Message}");
        }
    }

    public static int SincronizarEstadoFotosDesdeCarpetas(IEnumerable<Alumno> alumnos) {
        int actualizados = 0;

        foreach (Alumno alumno in alumnos) {
            string? rutaCarpetaAlumno = AppPaths.ObtenerCarpetaUnicaMismoLegajo(alumno.Legajo);
            bool tieneFotoEnCarpeta = AppPaths.ExisteCarpetaAlumno(rutaCarpetaAlumno) &&
                                      AppPaths.ExisteFotoAlumno(rutaCarpetaAlumno!);

            if (alumno.TieneFoto == tieneFotoEnCarpeta) {
                continue;
            }

            alumno.TieneFoto = tieneFotoEnCarpeta;
            actualizados++;
        }

        return actualizados;
    }

    static Alumno? ExtraerAlumnoFormatoMarkdown(string linea, string comisionActual) {
        List<string> columnas = Regex.Split(linea.TrimEnd(), @"\s{2,}").ToList();
        bool incluyeP1 = columnas.Count >= 11;

        while (columnas.Count < 11) {
            columnas.Add(string.Empty);
        }

        var legajo = ExtraerInt(columnas[0]);
        if (legajo == 0) { return null; }

        (string apellido, string nombre) = ExtraerApellidoNombre(columnas[1]);

        int nota = incluyeP1 ? ExtraerInt(columnas[9]) : 0;
        string codigo = LimpiarCampo(columnas[incluyeP1 ? 10 : 9]);

        Alumno alumno = new(legajo, comisionActual, nombre, apellido, ExtraerTelefono(columnas[2]), ExtraerGitHub(columnas[4]), ExtraerBool(columnas[3]), ExtraerBool(columnas[7]), ExtraerInt(columnas[8]), nota, codigo);
        CargarEstados(alumno.practicos, columnas[5]);
        CargarEstados(alumno.examenes,  columnas[6]);

        return alumno;
    }

    static string FormatearFilaTabla(params string?[] columnas) {
        int[] anchos = [6, 30, 13, 3, 25, 20, 8, 4, -4, -4, 0];
        return FormatearFilaConAnchos(anchos, columnas);
    }

    static string FormatearFilaTablaEstadoInformer(params string?[] columnas) {
        int[] anchos = [6, 30, 13, -3, -4];
        return FormatearFilaConAnchos(anchos, columnas);
    }

    static string FormatearFilaConAnchos(int[] anchos, params string?[] columnas) {
        return string.Join("  ", anchos.Zip(columnas, (ancho, valor) => AjustarColumna(valor ?? string.Empty, ancho)));
    }

    static string ObtenerComision(Alumno alumno) {
        return FormatearTexto(alumno.Comision);
    }

    static string DesescaparUnicodeJsonLegible(string json) {
        StringBuilder sb = new(json.Length);

        for (int i = 0; i < json.Length; i++) {
            if (!TryLeerCodigoUnicode(json, i, out int codePoint, out int consumed)) {
                sb.Append(json[i]);
                continue;
            }

            sb.Append(char.ConvertFromUtf32(codePoint));
            i += consumed - 1;
        }

        return sb.ToString();
    }

    static bool TryLeerCodigoUnicode(string json, int indice, out int codePoint, out int consumed) {
        codePoint = 0;
        consumed = 0;

        if (json[indice] != '\\' || indice + 5 >= json.Length || json[indice + 1] != 'u') {
            return false;
        }

        if (CantidadBarrasInvertidasConsecutivas(json, indice) % 2 != 0) {
            return false;
        }

        if (!TryParseHex(json.AsSpan(indice + 2, 4), out int primerValor)) {
            return false;
        }

        if (char.IsHighSurrogate((char)primerValor)) {
            if (indice + 11 >= json.Length || json[indice + 6] != '\\' || json[indice + 7] != 'u') {
                return false;
            }

            if (!TryParseHex(json.AsSpan(indice + 8, 4), out int segundoValor) || !char.IsLowSurrogate((char)segundoValor)) {
                return false;
            }

            codePoint = char.ConvertToUtf32((char)primerValor, (char)segundoValor);
            consumed = 12;
            return true;
        }

        if (primerValor < 0x20 || primerValor == '"' || primerValor == '\\' || char.IsSurrogate((char)primerValor)) {
            return false;
        }

        codePoint = primerValor;
        consumed = 6;
        return true;
    }

    static int CantidadBarrasInvertidasConsecutivas(string texto, int indice) {
        int cantidad = 0;

        for (int i = indice - 1; i >= 0 && texto[i] == '\\'; i--) {
            cantidad++;
        }

        return cantidad;
    }

    static bool TryParseHex(ReadOnlySpan<char> valor, out int resultado) =>
        int.TryParse(valor, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out resultado);

    static string ToSiNo(this bool valor) => valor ? "Sí" : "No";

    static string FormatearFila(Alumno a) {
        return FormatearFilaTabla(a.Legajo.ToString(), a.NombreCompleto, a.Telefono, a.TieneFoto.ToSiNo(), a.GitHub, a.practicos.ToString(12), a.examenes.ToString(4), a.Presente.ToSiNo(), a.Asistencias.ToString(), a.Nota.ToString(), a.Codigo);
    }

    static string FormatearFilaListado(Alumno a) {
        return FormatearFilaTabla(a.Legajo.ToString(), a.NombreCompleto, a.Telefono, a.TieneFoto.ToSiNo(), a.GitHub, a.practicos.ToString(12), a.examenes.ToString(4), a.Presente.ToSiNo(), a.Asistencias.ToString(), a.Nota.ToString());
    }

    static string FormatearFilaEstadoInformer(Alumno alumno) {
        return FormatearFilaTablaEstadoInformer(alumno.Legajo.ToString(), alumno.NombreCompleto, alumno.practicos.ToString(10), alumno.Asistencias.ToString(), alumno.Nota.ToString());
    }


    static string AjustarColumna(string texto, int ancho = 20) {
        string valor = FormatearTexto(texto);
        if (ancho == 0) { return valor; }

        bool derecha = ancho < 0;
        ancho = Math.Abs(ancho);
        if (valor.Length > ancho) { valor = valor[..ancho]; }
        return derecha ? valor.PadLeft(ancho) : valor.PadRight(ancho);
    }

    static string FormatearTexto(string texto) {
        if (string.IsNullOrWhiteSpace(texto)) { return "—"; }

        return texto.Trim();
    }

    static string LimpiarCampo(string texto) {
        string valor = texto.Trim();
        if (valor is "—" or "(-)") { return string.Empty; }

        return valor;
    }

    static string ExtraerTelefono(string texto) {
        return LimpiarCampo(texto);
    }

    static int ExtraerInt(string texto) {
        string valor = LimpiarCampo(texto);
        return int.TryParse(valor, out int resultado) ? resultado : 0;
    }

    static (string, string) ExtraerApellidoNombre(string nombreCompleto) {
        string apellido;
        string nombre;
        string[] partes = nombreCompleto.Split(',', 2);

        if (partes.Length == 2) {
            apellido = LimpiarCampo(partes[0]);
            nombre = LimpiarCampo(partes[1]);
        } else {
            apellido = LimpiarCampo(nombreCompleto);
            nombre = string.Empty;
        }

        return (apellido, nombre);
    }

    static string ExtraerGitHub(string texto) {
        string valor = LimpiarCampo(texto);
        return string.Equals(valor, "no", StringComparison.OrdinalIgnoreCase) ? string.Empty : valor;
    }

    static string FormatearGitHub(string gitHub) {
        gitHub = gitHub.Trim();
        return string.IsNullOrWhiteSpace(gitHub) ? "-" : gitHub;
    }

    static string ToString(this List<Estado> estados, int ancho = 10) {
        string valor = string.Join(string.Empty, estados.Select(e => e.ToEmoji()));
        valor = valor.Replace(" ", "⚪️");
        while (StringInfo.ParseCombiningCharacters(valor).Length < ancho) {
            valor += "⚪️";
        }
        return valor[..ancho];
    }

    static bool ExtraerBool(string texto) {
        texto = texto.Trim().ToLower();
        return texto is "si" or "sí" or "true" or "yes";
    }

    static void CargarEstados(List<Estado> destino, string texto) {
        destino.Clear();

        TextElementEnumerator enumerador = StringInfo.GetTextElementEnumerator(texto);
        while (enumerador.MoveNext()) {
            string elemento = enumerador.GetTextElement().Trim();
            if (string.IsNullOrWhiteSpace(elemento)) {
                continue;
            }

            Estado estado = EstadoExtensions.Parse(elemento);
            if (estado != Estado.Vacio || EsEstadoVacio(elemento)) {
                destino.Add(estado);
            }
        }

        while (destino.Count > 0 && destino[^1] == Estado.Vacio) {
            destino.RemoveAt(destino.Count - 1);
        }
    }

    static bool EsEstadoVacio(string texto) =>
        texto is "⚪" or " ";

    static void AppendVCardContacto(StringBuilder sb, Alumno alumno) {
        string apellido = FormatearTextoVcard(alumno.Apellido);
        string nombre = FormatearTextoVcard(alumno.Nombre);
        string nombreCompleto = FormatearTextoVcard($"{nombre} {apellido}");
        string comision = FormatearTextoVcard(alumno.Comision);
        string etiquetaBusqueda = FormatearTextoVcard(ObtenerEtiquetaBusqueda(alumno));
        string etiquetaVisible = FormatearTextoVcard($"{etiquetaBusqueda}-{alumno.Legajo}");
        string telefonoE164 = $"+{alumno.TelefonoId}";

        sb.AppendLine("BEGIN:VCARD");
        sb.AppendLine("VERSION:3.0");
        sb.AppendLine($"N:{apellido};{nombre};;;");
        sb.AppendLine($"FN:{nombreCompleto} | {etiquetaVisible}");
        sb.AppendLine($"NICKNAME:{etiquetaVisible}");
        sb.AppendLine("ORG:TUP 2026 - Programacion III");
        sb.AppendLine($"CATEGORIES:{etiquetaBusqueda}");
        sb.AppendLine($"NOTE:Legajo {alumno.Legajo} | Comision {comision} | Etiqueta {etiquetaBusqueda} | GitHub {ObtenerGitHubVisible(alumno)}");
        sb.AppendLine($"X-TUP-LEGAJO:{alumno.Legajo}");
        sb.AppendLine($"X-TUP-COMISION:{comision}");
        sb.AppendLine($"TEL;TYPE=CELL;TYPE=VOICE:{telefonoE164}");
        sb.AppendLine("END:VCARD");
    }

    static string FormatearTextoVcard(string texto) {
        string valor = FormatearTexto(texto);
        return valor
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }

    static string ObtenerGitHubVisible(Alumno alumno) {
        string gitHub = FormatearGitHub(alumno.GitHub);
        return gitHub == "-" ? "sin GitHub" : gitHub;
    }

    static string ObtenerEtiquetaBusqueda(Alumno alumno) {
        return $"TUP26-P3-{ObtenerComision(alumno)}";
    }

}
