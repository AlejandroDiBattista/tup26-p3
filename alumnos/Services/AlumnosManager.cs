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
        string[] etiquetas = ["Legajo", "Nombre y Apellido", "Teléfono", "GitHub", "Prácticos", "Exm", "Prs", "Ast", "Nta", "Observaciones"];
        string[] guiones = ["------", "------------------------------", "-------------", "-------------------------", "----------", "---", "---", "---", "---", "------------------------"];
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
        string[] etiquetas = ["Legajo", "Nombre y Apellido", "Prácticos", "Exm", "Ast", "Nta"];
        string[] guiones = ["------", "------------------------------", "----------", "---", "---", "---"];

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
        string[] campos = ["Legajo", "Nombre y Apellido", "Teléfono", "GitHub", "Prácticos", "Exm", "Prs", "Ast", "Nta"];
        string[] guiones = ["------", "------------------------------", "-------------", "-------------------------", "----------", "---", "---", "---", "---"];

        string comision = "";
        if (!alumnos.Any()) {
            Log.Warning("No hay alumnos para mostrar.");
            return;
        }

        List<Alumno> alumnosOrdenados = alumnos.OrderBy(a => a.Comision).ThenBy(a => a.NombreCompleto).ThenBy(a => a.Legajo).ToList();

        Log.WriteLine($"[blue]=== {titulo.ToUpper()} ===");

        foreach (var a in alumnosOrdenados) {
            if (a.Comision != comision) {
                comision = a.Comision;
                Log.WriteLine($"\n[yellow]== {comision} ({alumnosOrdenados.Count(x => x.Comision == comision)}) ==");
                Log.WriteLine($"{FormatearFilaTablaListado(campos)}\n[blue]{FormatearFilaTablaListado(guiones)}");
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
                Log.Info($" ➕ {nombreCarpeta}");
                return true;
            }

            if (carpetasConLegajo.Count == 1) {
                string rutaCarpetaExistente = carpetasConLegajo[0];
                string rutaRelativa = AppPaths.RutaRelativaDesdePracticos(rutaCarpetaExistente);
                if (!string.Equals(rutaCarpetaExistente, rutaCarpeta, StringComparison.OrdinalIgnoreCase)) {
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
                GitHub = alumno.GitHub,
                alumno.Nota,
                alumno.Observaciones,
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

    static Alumno? ExtraerAlumnoFormatoMarkdown(string linea, string comisionActual) {
        List<string> columnas = Regex.Split(linea.TrimEnd(), @"\s{2,}").ToList();
        bool tieneColumnaFoto = columnas.Count >= 11;
        int indiceGitHub = tieneColumnaFoto ? 4 : 3;
        int indicePracticos = tieneColumnaFoto ? 5 : 4;
        int indiceExamenes = tieneColumnaFoto ? 6 : 5;
        int indicePresente = tieneColumnaFoto ? 7 : 6;
        int indiceAsistencias = tieneColumnaFoto ? 8 : 7;
        int indiceNota = tieneColumnaFoto ? 9 : 8;
        int indiceObservaciones = tieneColumnaFoto ? 10 : 9;

        while (columnas.Count <= indiceObservaciones) {
            columnas.Add(string.Empty);
        }

        var legajo = ExtraerInt(columnas[0]);
        if (legajo == 0) { return null; }

        (string apellido, string nombre) = ExtraerApellidoNombre(columnas[1]);

        int nota = ExtraerInt(columnas[indiceNota]);
        string observaciones = LimpiarCampo(columnas[indiceObservaciones]);

        Alumno alumno = new(legajo, comisionActual, nombre, apellido, ExtraerTelefono(columnas[2]), ExtraerGitHub(columnas[indiceGitHub]), tieneColumnaFoto && ExtraerBool(columnas[3]), ExtraerBool(columnas[indicePresente]), ExtraerInt(columnas[indiceAsistencias]), nota, observaciones);
        CargarEstados(alumno.practicos, columnas[indicePracticos]);
        CargarEstados(alumno.examenes,  columnas[indiceExamenes]);

        return alumno;
    }

    static string FormatearFilaTabla(params string?[] columnas) {
        int[] anchos = [6, 30, 13, 25, 10, 3, 3, -2, -3, 0];
        string[] separadores = ["  ", "  ", "   ", "  ", "   ", "   ", "   ", "  ", "  "];
        return FormatearFilaConAnchos(anchos, separadores, columnas).TrimEnd();
    }

    static string FormatearFilaTablaEstadoInformer(params string?[] columnas) {
        int[] anchos = [6, 30, 10, 3, -3, -3];
        string[] separadores = ["  ", "  ", "   ", "   ", "  "];
        return FormatearFilaConAnchos(anchos, separadores, columnas).TrimEnd();
    }

    static string FormatearFilaTablaListado(params string?[] columnas) {
        int[] anchos = [6, 30, 13, 25, 10, 3, 3, -2, -3];
        string[] separadores = ["  ", "  ", "   ", "  ", "   ", "   ", "   ", "  "];
        return FormatearFilaConAnchos(anchos, separadores, columnas).TrimEnd();
    }

    static string FormatearFilaConAnchos(int[] anchos, params string?[] columnas) {
        string[] separadores = Enumerable.Repeat("  ", Math.Max(0, columnas.Length - 1)).ToArray();
        return FormatearFilaConAnchos(anchos, separadores, columnas);
    }

    static string FormatearFilaConAnchos(int[] anchos, string[] separadores, params string?[] columnas) {
        StringBuilder sb = new();
        int cantidad = Math.Min(anchos.Length, columnas.Length);

        for (int i = 0; i < cantidad; i++) {
            if (i > 0) {
                sb.Append(separadores[Math.Min(i - 1, separadores.Length - 1)]);
            }

            sb.Append(AjustarColumna(columnas[i] ?? string.Empty, anchos[i]));
        }

        return sb.ToString();
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
        return FormatearFilaTabla(a.Legajo.ToString(), a.NombreCompleto, a.Telefono, a.GitHub, a.practicos.ToString(12), a.examenes.ToString(4), a.Presente.ToSiNo(), a.Asistencias.ToString(), a.Nota.ToString(), a.Observaciones);
    }

    static string FormatearFilaListado(Alumno a) {
        return FormatearFilaTablaListado(a.Legajo.ToString(), a.NombreCompleto, a.Telefono, a.GitHub, a.practicos.ToString(10), a.examenes.ToString(4), a.Presente.ToSiNo(), a.Asistencias.ToString(), a.Nota.ToString());
    }

    static string FormatearFilaEstadoInformer(Alumno alumno) {
        return FormatearFilaTablaEstadoInformer(alumno.Legajo.ToString(), alumno.NombreCompleto, alumno.practicos.ToString(10), alumno.examenes.ToString(4), alumno.Asistencias.ToString(), alumno.Nota.ToString());
    }


    static string AjustarColumna(string texto, int ancho = 20) {
        if (ancho == 0) { return texto.Trim(); }

        string valor = FormatearTexto(texto);

        bool derecha = ancho < 0;
        ancho = Math.Abs(ancho);
        if (valor.Length > ancho) { return valor; }
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
