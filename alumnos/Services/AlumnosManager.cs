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
        string[] etiquetas = ["Legajo", "Nombre y Apellido", "Teléfono", "Foto", "GitHub", "Prácticos", "Ex", "Pr", "Nr"];
        var guiones = etiquetas.Select(e => new string('-', 40)).ToArray();
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
        string[] etiquetas = ["Legajo", "Nombre y Apellido", "Prácticos", "Asistencias"];
        var guiones = etiquetas.Select(_ => new string('-', 40)).ToArray();

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
        string[] campos = ["Legajo", "Nombre y Apellido", "Teléfono", "Foto", "GitHub", "Prácticos", "Ex", "Pr", "Nr"];
        string[] guiones = campos.Select(c => new string('-', 40)).ToArray();

        string comision = "";
        if (!alumnos.Any()) {
            Log.Warning("No hay alumnos para mostrar.");
            return;
        }

        List<Alumno> alumnosOrdenados = alumnos.OrderBy(a => a.Comision).ThenBy(a => a.NombreCompleto).ThenBy(a => a.Legajo).ToList();

        string encabezado = FormatearFilaTabla(campos);

        Log.WriteLine($"[blue]=== {titulo.ToUpper()} ===");
        Log.WriteLine();

        foreach (var a in alumnosOrdenados) {
            if (a.Comision != comision) {
                comision = a.Comision;
                Log.WriteLine($"\n{FormatearFilaTabla(campos)}\n[blue]{FormatearFilaTabla(guiones)}");
            }
            Log.WriteLine(FormatearFila(a));
        }

        Log.WriteLine($"\n[green]Total de alumnos: {alumnos.Count()}\n");
    }

    public static void CrearCarpetas(IEnumerable<Alumno> alumnos) {
        AppPaths.AsegurarDirectorioPracticos();

        foreach (Alumno alumno in alumnos) {
            string nombreCarpeta = alumno.CarpetaNombre;
            string rutaCarpeta = AppPaths.RutaCarpetaAlumnoEsperada(alumno);
            try {
                List<string> carpetasConLegajo = AppPaths.BuscarCarpetasMismoLegajo(alumno.Legajo);

                if (!carpetasConLegajo.Any()) {
                    AppPaths.AsegurarCarpetaAlumno(alumno);
                    Log.Debug($" ➕ {rutaCarpeta,-40}");
                } else if (carpetasConLegajo.Count == 1) {
                    string rutaCarpetaExistente = carpetasConLegajo[0];
                    string rutaRelativa = AppPaths.RutaRelativaDesdePracticos(rutaCarpetaExistente);
                    if (string.Equals(rutaCarpetaExistente, rutaCarpeta, StringComparison.OrdinalIgnoreCase)) {
                        Log.Info($" ✅ {rutaRelativa,-40}");
                    } else {
                        AppPaths.RenombrarCarpetaAlumno(rutaCarpetaExistente, alumno);
                        Log.Warning($" 🔄 {rutaRelativa,-40} → {nombreCarpeta}");
                    }
                } else {
                    Log.Warning($" ⚠️  {alumno.Legajo}. Revisar manualmente las duplicadas.");
                }
            } catch (Exception ex) {
                Log.Error($"Error al crear la carpeta para {nombreCarpeta}: {ex.Message}");
            }
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

        if (string.IsNullOrWhiteSpace(nombrePractico)) {
            Log.Error("Debe indicar el nombre del práctico a copiar.");
            return;
        }

        if (!AppPaths.ExisteEnunciadoPractico(nombrePractico)) {
            Log.Error($"No existe la carpeta del enunciado: {AppPaths.EnunciadoPracticoDirectory(nombrePractico)}");
            return;
        }

        if (!AppPaths.ExisteDirectorioPracticos()) {
            Log.Error($"No existe la carpeta base de prácticos: {AppPaths.PracticosDirectory}");
            return;
        }

        foreach (Alumno alumno in alumnos) {
            string rutaAlumno = AppPaths.RutaCarpetaAlumnoEsperada(alumno);

            if (!AppPaths.ExisteDirectorio(rutaAlumno)) {
                try {
                    AppPaths.AsegurarCarpetaAlumno(alumno);
                    Log.Debug($" ➕ {rutaAlumno,-40}");
                } catch (Exception ex) {
                    Log.Error($"No se pudo crear la carpeta del alumno {rutaAlumno}: {ex.Message}");
                    continue;
                }
            }

            try {
                CopiaRuta copia = AppPaths.CopiarEnunciadoPractico(alumno, nombrePractico, forzar);
                Log.Info($"Enunciado copiado: {copia.Origen} -> {copia.Destino}");
            } catch (Exception ex) {
                Log.Error($"Error al copiar el enunciado para {alumno.CarpetaNombre}: {ex.Message}");
            }
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
                Practicos = alumno.practicos.Select(e => e.ToEmoji()).ToList(),
                Examenes = alumno.examenes.Select(e => e.ToEmoji()).ToList()
            });

            JsonSerializerOptions opciones = new() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            AppPaths.EscribirAlumnosJson(JsonSerializer.Serialize(datos, opciones) + Environment.NewLine, rutaArchivo);

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

        while (columnas.Count < 9) {
            columnas.Add(string.Empty);
        }

        var legajo = ExtraerInt(columnas[0]);
        if (legajo == 0) { return null; }

        (string apellido, string nombre) = ExtraerApellidoNombre(columnas[1]);

        Alumno alumno = new(legajo, comisionActual, nombre, apellido, ExtraerTelefono(columnas[2]), ExtraerGitHub(columnas[4]), ExtraerBool(columnas[3]), ExtraerBool(columnas[7]), ExtraerInt(columnas[8]));
        CargarEstados(alumno.practicos, columnas[5]);
        CargarEstados(alumno.examenes, columnas[6]);

        return alumno;
    }


    // static string FormatearFilaTabla(string legajo, string nombreApellido, string telefono, string foto, string gitHub, string comision, string pruebas, string examenes, string presente, string asistencias) {
    static string FormatearFilaTabla(params string?[] columnas) {
        int[] anchos = [6, 30, 14, 4, 25, 20, 4, 4, 4];
        return FormatearFilaConAnchos(anchos, columnas);
    }

    static string FormatearFilaTablaEstadoInformer(params string?[] columnas) {
        int[] anchos = [6, 30, 12, 3];
        return FormatearFilaConAnchos(anchos, columnas);
    }

    static string FormatearFilaConAnchos(int[] anchos, params string?[] columnas) {
        return string.Join("  ", anchos.Zip(columnas, (ancho, valor) => AjustarColumna(valor ?? string.Empty, ancho)));
    }

    static string ObtenerComision(Alumno alumno) {
        return FormatearTexto(alumno.Comision);
    }

    static string ToSiNo(this bool valor) => valor ? "Sí" : "No";

    static string FormatearFila(Alumno a) {
        return FormatearFilaTabla(a.Legajo.ToString(), a.NombreCompleto, a.Telefono, a.TieneFoto.ToSiNo(), a.GitHub, a.practicos.ToString(10), a.examenes.ToString(10), a.Presente.ToSiNo(), a.Asistencias.ToString());
    }

    static string FormatearFilaEstadoInformer(Alumno alumno) {
        return FormatearFilaTablaEstadoInformer(alumno.Legajo.ToString(), alumno.NombreCompleto, alumno.practicos.ToString(10), alumno.Asistencias.ToString());
    }


    static string AjustarColumna(string texto, int ancho = 20) {
        string valor = FormatearTexto(texto);
        if (valor.Length > ancho) { valor = valor[..ancho]; }
        return valor.PadRight(ancho);
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

    static string FormatearEstados(List<Estado> estados, int ancho = 20) {
        string valor = string.Empty;
        if (estados?.Count > 0) {
            valor = string.Join(string.Empty, estados.Select(e => e.ToEmoji()));
        }
        valor = valor.Replace(" ", "⚪️");
        while (StringInfo.ParseCombiningCharacters(valor).Length < ancho) {
            valor += "⚪️";
        }
        return valor;
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
