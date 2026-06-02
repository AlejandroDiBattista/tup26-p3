using System.IO.Enumeration;

namespace Tup26.AlumnosApp;

/*
# GitHub

Servicio para interactuar con la API de GitHub mediante `gh api`.

## Funciones públicas

- `AgregarColaborador(usuario)`: agrega un colaborador con permisos de escritura.
    - `usuario`: nombre de usuario de GitHub.

- `Colaboradores()`: devuelve la lista de colaboradores con permiso de escritura.

- `InvitacionesPendientes()`: devuelve los usuarios con invitaciones pendientes.

- `PullRequests(soloAbiertos)`: lista pull requests del repositorio.
    - `soloAbiertos`: cuando es `true`, devuelve solo PRs abiertos.

- `PRSinLegajo()`: informa PRs cuyo título no contiene un legajo válido.

- `PRConConflictos()`: informa PRs que no son mergeables.

- `NormalizarTitulos(alumnos, simular)`: ajusta títulos de PRs al formato esperado.
    - `alumnos`: colección usada para resolver nombre completo por legajo.
    - `simular`: muestra cambios sin aplicarlos.

- `ObtenerEstado(numeroPR)`: devuelve estado y mergeabilidad de un PR.
    - `numeroPR`: número del pull request.

- `ListarArchivos(numeroPR)`: devuelve los archivos modificados en un PR.
    - `numeroPR`: número del pull request.

- `ListarArchivosDirectorio(numeroPR, carpetaAlumnoRemota, directorioRemoto)`: devuelve los archivos modificados dentro de la carpeta de un alumno y un práctico del PR.
    - `numeroPR`: número del pull request.
    - `carpetaAlumnoRemota`: carpeta remota del alumno, por ejemplo `63341 - Carrer, Juan Cruz`.
    - `directorioRemoto`: carpeta del repositorio a filtrar, por ejemplo `tp1`.

- `ListarTPsPresentados(numeroPR, carpetaAlumnoRemota)`: devuelve los números de TP con archivos dentro de carpetas `tpN` de un alumno en un PR.
    - `numeroPR`: número del pull request.
    - `carpetaAlumnoRemota`: carpeta remota del alumno, por ejemplo `63341 - Carrer, Juan Cruz`.

- `CantidadLineasAgregadasDirectorio(numeroPR, carpetaAlumnoRemota, directorioRemoto)`: suma las líneas agregadas dentro de la carpeta de un alumno y un práctico del PR.
    - `numeroPR`: número del pull request.
    - `carpetaAlumnoRemota`: carpeta remota del alumno, por ejemplo `63341 - Carrer, Juan Cruz`.
    - `directorioRemoto`: carpeta del repositorio a filtrar, por ejemplo `tp1`.

- `CerrarPR(numeroPR)`: cierra un pull request.
    - `numeroPR`: número del pull request.

- `BajarArchivo(numeroPR, patron, rutaDestino, forzar)`: descarga archivos de un PR que coinciden con un patrón.
    - `numeroPR`: número del pull request.
    - `patron`: patrón simple de archivos a descargar.
    - `rutaDestino`: carpeta destino.
    - `forzar`: sobrescribe archivos existentes si corresponde.

- `BajarDirectorio(numeroPR, directorioRemoto, rutaDestino, forzar)`: descarga todos los archivos de un directorio del PR a una carpeta destino.
    - `numeroPR`: número del pull request.
    - `directorioRemoto`: carpeta del repositorio a descargar, por ejemplo `tp1`.
    - `rutaDestino`: carpeta destino local.
    - `forzar`: sobrescribe archivos existentes si corresponde.

- `BajarArchivosAlumno(numeroPR, forzar)`: descarga los archivos del práctico del alumno resuelto a partir del título del PR.
    - `numeroPR`: número del pull request.
    - `forzar`: sobrescribe archivos existentes si corresponde.

- `Merge(numeroPR)`: intenta mergear un PR abierto.
    - `numeroPR`: número del pull request.

- `MergeTP(numeroTP)`: mergea todos los PRs abiertos de un trabajo práctico.
    - `numeroTP`: número del trabajo práctico.

- `CambiarTitulo(numeroPR, titulo)`: actualiza el título de un PR.
    - `numeroPR`: número del pull request.
    - `titulo`: nuevo título.

- `Commits(numeroPR)`: lista commits de un PR con fecha y título.
    - `numeroPR`: número del pull request.

- `ExtraerTP(titulo)`: obtiene el número de TP a partir de un título.
    - `titulo`: texto a analizar.

- `ExtraerLegajo(titulo)`: obtiene el legajo a partir de un título.
    - `titulo`: texto a analizar.

*/

class GitHub {
    readonly string owner;
    readonly string repo;

    public GitHub(string owner = "AlejandroDiBattista", string repo = "tup26-p3") {
        this.owner = owner;
        this.repo = repo;
    }


    public bool AgregarColaborador(string usuario) {
        string? salida = Ejecutar($"Error al agregar colaborador '{usuario}'",
            $"/collaborators/{usuario}", "--method", "PUT", "-f", "permission=push");

        return salida is not null;
    }


    public List<string> Colaboradores() {
        string? salida = Ejecutar("Error al listar colaboradores",
            "/collaborators", "--jq", ".[] | select(.permissions.push == true) | .login");

        if (salida is null) { return new(); }

        return Lineas(salida);
    }


    public List<string> InvitacionesPendientes() {
        string? salida = Ejecutar("Error al listar invitaciones pendientes",
            "/invitations", "--paginate", "--jq", ".[].invitee.login");

        if (salida is null) { return new(); }

        return Lineas(salida);
    }


    public List<(int Numero, string Titulo)> PullRequests(bool soloAbiertos = true, int tp = 0) {
        string estado = soloAbiertos ? "open" : "all";

        string? salida = Ejecutar("Error al listar PRs",
            $"/pulls?state={estado}", "--paginate", "--jq", @".[] | ""\(.number)\t\(.title)""");

        if (salida is null) { return new(); }

        List<(int Numero, string Titulo)> prs = new();
        foreach (string linea in Lineas(salida, pasarAMinusculas: false)) {
            string[] partes = linea.Split('\t', 2);
            if (partes.Length != 2) { continue; }
            string practico = partes[0];
            string titulo = partes[1];
            if (tp != 0 && GitHub.ExtraerTP(titulo) != tp) { continue; }
            if (!int.TryParse(practico, out int numero)) { continue; }

            prs.Add((numero, titulo));
        }
        prs.Sort((a, b) => a.Numero.CompareTo(b.Numero));
        return prs;
    }


    public int PRSinLegajo() {
        List<(int Numero, string Titulo)> prs = PullRequests();
        int count = 0;

        foreach ((int Numero, string Titulo) pr in prs) {
            if (ExtraerLegajo(pr.Titulo) == 0) {
                if (count++ == 0) {
                    Log.Warning("= PR Sin legajo válido =");
                }

                Log.Warning($"- #{pr.Numero}: {pr.Titulo}");
            }
        }

        if (count == 0) {
            Log.Info("Todos los PRs tienen un legajo válido en el título.");
        } else {
            Log.Warning($"Total de PRs sin legajo válido: {count}");
        }

        return count;
    }


    public int PRConConflictos() {
        List<(int Numero, string Titulo)> prs = PullRequests();
        int count = 0;

        foreach ((int Numero, string Titulo) pr in prs) {
            (string Estado, bool EsMergeable) detallePr = ObtenerEstado(pr.Numero);

            if (detallePr.EsMergeable == false) {
                if (count++ == 0) {
                    Log.Warning("= PR con conflictos =");
                }

                Log.Warning($"- #{pr.Numero}: {pr.Titulo}");
            }
        }

        if (count == 0) {
            Log.Info("No se encontraron PRs con conflictos.");
        } else {
            Log.Warning($"Total de PRs con conflictos: {count}");
        }

        return count;
    }


    public int NormalizarTitulos(Alumnos alumnos, bool simular = false) {
        List<(int Numero, string Titulo)> prs = PullRequests(soloAbiertos: true);
        int count = 0;
        int omitidos = 0;

        foreach ((int Numero, string Titulo) pr in prs) {
            int legajo = ExtraerLegajo(pr.Titulo);

            if (legajo <= 0) {
                if (omitidos++ == 0) {
                    Log.Error("= PRs sin información suficiente para normalizar =");
                }

                Log.Error($"No se puede normalizar PR #{pr.Numero}: falta legajo en el título.\n > {pr.Titulo}");
                continue;
            }

            Alumno? alumno = alumnos.BuscarPorLegajo(legajo);
            if (alumno is null) {
                if (omitidos++ == 0) {
                    Log.Error("= PRs sin información suficiente para normalizar =");
                }

                Log.Error($"No se puede normalizar PR #{pr.Numero}: el legajo {legajo} no está en alumnos.md.\n > {pr.Titulo}");
                continue;
            }

            List<int> tpsPresentados = ListarTPsPresentados(pr.Numero, alumno.CarpetaNombre);
            if (tpsPresentados.Count == 0) {
                if (omitidos++ == 0) {
                    Log.Error("= PRs sin información suficiente para normalizar =");
                }

                Log.Error($"No se puede normalizar PR #{pr.Numero}: no se encontraron archivos en carpetas tpN de {alumno.CarpetaNombre}.\n > {pr.Titulo}");
                continue;
            }

            string trabajosPracticos = string.Join("", tpsPresentados);
            string nuevoTitulo = $"{legajo} - TP{trabajosPracticos} - {alumno.NombreCompleto}";

            if (nuevoTitulo != pr.Titulo) {
                if (count++ == 0) {
                    Log.Info("= PRs a actualizar =");
                }

                Log.Info($"{(simular ? "Cambiaría" : "Actualizando")} PR #{pr.Numero}:");
                Log.Info($" > {pr.Titulo}");
                Log.Info($" < {nuevoTitulo}");

                if (!simular) {
                    CambiarTitulo(pr.Numero, nuevoTitulo);
                }
            }
        }

        if (count == 0) {
            Log.Info("No se encontraron PRs para actualizar.");
        } else {
            Log.Info($"Total de PRs a actualizar: {count}");
        }

        if (omitidos > 0) {
            Log.Error($"Total de PRs sin información suficiente: {omitidos}");
        }

        return count;
    }


    public (string Estado, bool EsMergeable) ObtenerEstado(int numeroPR) {
        string? salida = Ejecutar($"Error al consultar el estado del PR #{numeroPR}",
            $"/pulls/{numeroPR}", "--jq", @"""\(.state)\t\(.mergeable)""");

        if (salida is null) { return (string.Empty, false); }

        string[] partes = salida.Trim().Split('\t', 2);
        if (partes.Length != 2) { return (string.Empty, false); }

        return (partes[0].ToLower(), partes[1].ToLower() == "true");
    }

    public int CantidadLineas(int numeroPR) {
        string? salida = Ejecutar($"Error al contar líneas del PR #{numeroPR}",
            $"/pulls/{numeroPR}/files", "--paginate", "--jq", @".[] | .changes");

        if (salida is null) { return 0; }

        int total = 0;

        foreach (string linea in Lineas(salida, pasarAMinusculas: false)) {
            if (int.TryParse(linea, out int cambios)) {
                total += cambios;
            }
        }

        return total;
    }

    public List<string> ListarArchivos(int numeroPR) {
        string? salida = Ejecutar($"Error al listar archivos del PR #{numeroPR}",
            $"/pulls/{numeroPR}/files", "--paginate", "--jq", @".[] | .filename");

        if (salida is null) { return new(); }

        return Lineas(salida);
    }

    public List<string> ListarArchivosDirectorio(int numeroPR, string carpetaAlumnoRemota, string directorioRemoto) {
        string carpetaAlumno = NormalizarRutaRemota(carpetaAlumnoRemota);
        string carpetaRemota = NormalizarRutaRemota(directorioRemoto);
        if (string.IsNullOrWhiteSpace(carpetaAlumno) || string.IsNullOrWhiteSpace(carpetaRemota)) {
            return new();
        }

        return ListarArchivos(numeroPR)
            .Select(NormalizarRutaRemota)
            .Where(nombreRemoto => TryObtenerRutaRelativaDirectorio(nombreRemoto, carpetaAlumno, carpetaRemota, out _))
            .ToList();
    }

    public List<int> ListarTPsPresentados(int numeroPR, string carpetaAlumnoRemota) {
        return TPsPresentadosDesdeArchivos(ListarArchivos(numeroPR), carpetaAlumnoRemota);
    }

    public int CantidadLineasAgregadasDirectorio(int numeroPR, string carpetaAlumnoRemota, string directorioRemoto) {
        string carpetaAlumno = NormalizarRutaRemota(carpetaAlumnoRemota);
        string carpetaRemota = NormalizarRutaRemota(directorioRemoto);
        if (string.IsNullOrWhiteSpace(carpetaAlumno) || string.IsNullOrWhiteSpace(carpetaRemota)) {
            return 0;
        }

        string? salida = Ejecutar($"Error al contar líneas agregadas del PR #{numeroPR}",
            $"/pulls/{numeroPR}/files", "--paginate", "--jq", ".[] | \"\\(.filename)\\t\\(.additions)\"");

        if (salida is null) { return 0; }

        int total = 0;

        foreach (string linea in Lineas(salida, pasarAMinusculas: false)) {
            string[] partes = linea.Split('\t', 2);
            if (partes.Length != 2) { continue; }

            string nombreRemoto = NormalizarRutaRemota(partes[0]);
            if (!TryObtenerRutaRelativaDirectorio(nombreRemoto, carpetaAlumno, carpetaRemota, out _)) {
                continue;
            }

            if (int.TryParse(partes[1], out int additions)) {
                total += additions;
            }
        }

        return total;
    }

    public void CerrarPRsAbiertos() {
        List<(int Numero, string Titulo)> prsAbiertos = PullRequests(soloAbiertos: true);

        if (prsAbiertos.Count == 0) {
            Log.Info("No hay PRs abiertos para cerrar.");
            return;
        }

        foreach ((int Numero, string Titulo) pr in prsAbiertos) {
            CerrarPR(pr.Numero);
        }
    }

    public void CerrarPRsAbiertos(int numeroTP) {
        if (numeroTP <= 0) {
            Log.Error("Debe indicar un número de TP mayor a cero.");
            return;
        }

        List<(int Numero, string Titulo)> prsAbiertos = PullRequests(soloAbiertos: true, tp: numeroTP);
        if (prsAbiertos.Count == 0) {
            Log.Info($"No hay PRs abiertos para cerrar en TP{numeroTP}.");
            return;
        }

        foreach ((int Numero, string Titulo) pr in prsAbiertos) {
            CerrarPR(pr.Numero);
        }
    }

    public void CerrarPR(int numeroPR) {
        string? salida = Ejecutar($"Error al cerrar el PR #{numeroPR}",
            $"/pulls/{numeroPR}", "--method", "PATCH", "-f", "state=closed");

        if (salida is not null) {
            Log.Info($"PR #{numeroPR} cerrado exitosamente.");
        }
    }

    public void BajarArchivo(int numeroPR, string patron, string rutaDestino, bool forzar = false) {
        string? salida = Ejecutar($"Error al bajar archivos del PR #{numeroPR}",
            $"/pulls/{numeroPR}/files", "--paginate", "--jq", ".[] | \"\\(.filename)\\t\\(.raw_url)\"");

        if (salida is null) { return; }

        AppPaths.AsegurarDirectorio(rutaDestino);

        List<string> urls = Lineas(salida, pasarAMinusculas: false);

        foreach (string linea in urls) {
            try {
                string[] partes = linea.Split('\t', 2);
                if (partes.Length != 2) { continue; }

                string nombreRemoto = partes[0];
                string url = partes[1];

                if (!FileSystemName.MatchesSimpleExpression(patron, nombreRemoto, ignoreCase: true)) { continue; }

                using HttpClient client = new();
                if (!forzar && AppPaths.ExisteArchivoDescargado(rutaDestino, nombreRemoto)) {
                    // Log.Info($"Archivo '{nombreRemoto}' ya existe. Se omite descarga: {rutaArchivo}");
                    continue;
                }

                byte[] contenido = client.GetByteArrayAsync(url).Result;
                string rutaArchivo = AppPaths.GuardarArchivoDescargado(rutaDestino, nombreRemoto, contenido, forzar);
                Log.Warning($"Archivo '{nombreRemoto}'\n      {rutaArchivo} ");
            } catch (Exception ex) {
                Log.Error($"Error al descargar el archivo desde '{linea}': {ex.Message}");
            }
        }
    }

    public void BajarDirectorio(int numeroPR, string carpetaAlumnoRemota, string directorioRemoto, string rutaDestino, bool forzar = false) {
        string carpetaAlumno = NormalizarRutaRemota(carpetaAlumnoRemota);
        string carpetaRemota = NormalizarRutaRemota(directorioRemoto);
        if (string.IsNullOrWhiteSpace(carpetaAlumno) || string.IsNullOrWhiteSpace(carpetaRemota)) {
            Log.Error($"Error al bajar archivos del PR #{numeroPR}: debe indicar un directorio remoto válido.");
            return;
        }

        List<string> archivosDirectorio = ListarArchivosDirectorio(numeroPR, carpetaAlumno, carpetaRemota);
        if (archivosDirectorio.Count == 0) {
            Log.Error($"PR #{numeroPR}: no se encontraron archivos dentro de '{carpetaAlumno}/{carpetaRemota}/'.");
            return;
        }

        string? salida = Ejecutar($"Error al bajar archivos del PR #{numeroPR}", $"/pulls/{numeroPR}/files", "--paginate", "--jq", ".[] | \"\\(.filename)\\t\\(.raw_url)\"");

        if (salida is null) { return; }

        List<string> urls = Lineas(salida, pasarAMinusculas: false);
        int cantidadDescargas = 0;

        foreach (string linea in urls) {
            try {
                string[] partes = linea.Split('\t', 2);
                if (partes.Length != 2) { continue; }

                string nombreRemoto = NormalizarRutaRemota(partes[0]);
                string url = partes[1];

                if (!archivosDirectorio.Contains(nombreRemoto, StringComparer.OrdinalIgnoreCase)) {
                    continue;
                }

                if (!TryObtenerRutaRelativaDirectorio(nombreRemoto, carpetaAlumno, carpetaRemota, out string rutaRelativa)) {
                    continue;
                }

                using HttpClient client = new();
                byte[] contenido = client.GetByteArrayAsync(url).Result;
                int cantidadLineas = ContarLineas(contenido);
                string rutaArchivo = AppPaths.GuardarArchivoDescargadoRelativo(rutaDestino, rutaRelativa, contenido, forzar);
                string rutaLocalRelativa = $"{carpetaRemota}/{rutaRelativa}";
                Log.Info($"  - {rutaLocalRelativa,-30} | L:{cantidadLineas,4}");
                cantidadDescargas++;
            } catch (Exception ex) {
                Log.Error($"Error al descargar el archivo desde '{linea}': {ex.Message}");
            }
        }
    }

    public int BajarArchivosAlumno(int numeroPR, bool forzar = false, int? numeroTpSolicitado = null, bool informarOmitidos = true) {
        string? titulo = ObtenerTituloPR(numeroPR);
        if (string.IsNullOrWhiteSpace(titulo)) {
            return 0;
        }

        int legajo = ExtraerLegajo(titulo);
        if (legajo <= 0) {
            if (informarOmitidos) {
                Log.Warning($"Se omite PR #{numeroPR}: no se pudo resolver legajo desde el título '{titulo}'.");
            }
            return 0;
        }

        string? rutaCarpetaAlumno = AppPaths.ObtenerCarpetaUnicaMismoLegajo(legajo);
        if (!AppPaths.ExisteCarpetaAlumno(rutaCarpetaAlumno)) {
            if (informarOmitidos) {
                Log.Warning($"Se omite PR #{numeroPR}: no se encontró carpeta única para legajo {legajo}.");
            }
            return 0;
        }

        string carpetaAlumno = Path.GetFileName(rutaCarpetaAlumno!);
        List<int> tpsPresentados = ListarTPsPresentados(numeroPR, carpetaAlumno);
        if (numeroTpSolicitado is int tpSolicitado) {
            tpsPresentados = tpsPresentados.Where(tp => tp == tpSolicitado).ToList();
        }

        if (tpsPresentados.Count == 0) {
            string detalle = numeroTpSolicitado is int tp
                ? $"tp{tp}"
                : "carpetas tpN";
            if (informarOmitidos) {
                Log.Warning($"Se omite PR #{numeroPR}: no se encontraron archivos en {detalle} de '{carpetaAlumno}'.");
            }
            return 0;
        }

        foreach (int numeroTp in tpsPresentados) {
            string carpetaTp = $"tp{numeroTp}";
            string rutaDestino = Path.Combine(rutaCarpetaAlumno!, carpetaTp);
            BajarDirectorio(numeroPR, carpetaAlumno, carpetaTp, rutaDestino, forzar);
        }

        return tpsPresentados.Count;
    }

    public bool Merge(int numeroPR) {
        var detalle = ObtenerEstado(numeroPR);

        if (!string.Equals(detalle.Estado, "open")) {
            Log.Error($"Error al mergear el PR #{numeroPR}: el PR no está abierto.");
            return false;
        }

        string? salida = Ejecutar($"Error al mergear el PR #{numeroPR}",
            $"/pulls/{numeroPR}/merge", "--method", "PUT", "-f", "merge_method=merge");

        return salida is not null;
    }


    public int MergeTP(int numeroTP) {
        if (numeroTP <= 0) {
            Log.Error("Error al mergear PRs: el número de TP debe ser mayor a cero.");
            return 0;
        }

        List<(int Numero, string Titulo)> prs = PullRequests(soloAbiertos: true)
            .Where(pr => ExtraerTP(pr.Titulo) == numeroTP)
            .ToList();

        if (prs.Count == 0) {
            Log.Info($"No se encontraron PRs abiertos del TP {numeroTP}.");
            return 0;
        }

        int count = 0;

        foreach ((int Numero, string Titulo) pr in prs) {
            Log.Info($"Mergeando PR #{pr.Numero}: {pr.Titulo}");

            if (Merge(pr.Numero)) { count++; }
        }

        Log.Info($"PRs mergeados del TP {numeroTP}: {count}/{prs.Count}");
        return count;
    }


    public bool CambiarTitulo(int numeroPR, string titulo) {
        if (string.IsNullOrWhiteSpace(titulo)) {
            Log.Error("Error al cambiar el título del PR: el nuevo título no puede estar vacío.");
            return false;
        }

        string? salida = Ejecutar($"Error al cambiar el título del PR #{numeroPR}",
            $"/pulls/{numeroPR}", "--method", "PATCH", "-f", $"title={titulo}");

        return salida is not null;
    }


    public List<(string Titulo, DateTimeOffset FechaHora)> Commits(int numeroPR) {
        string? salida = Ejecutar($"Error al listar commits del PR #{numeroPR}",
            $"/pulls/{numeroPR}/commits", "--paginate", "--jq", @".[] | ""\(.commit.message | split(""\n"")[0])\t\(.commit.author.date)""");

        if (salida is null) { return new(); }

        List<(string Titulo, DateTimeOffset FechaHora)> commits = new();

        foreach (string linea in Lineas(salida, pasarAMinusculas: false)) {
            string[] partes = linea.Split('\t', 2);
            if (partes.Length != 2) { continue; }

            if (!DateTimeOffset.TryParse(partes[1], out DateTimeOffset fechaHora)) { continue; }

            commits.Add((partes[0], fechaHora));
        }

        commits.Sort((a, b) => a.FechaHora.CompareTo(b.FechaHora));
        return commits;
    }


    string? Ejecutar(string mensajeError, string endpoint, params string[] argumentos) {
        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = "gh",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("api");
        string rutaRelativa = endpoint.TrimStart('/');
        startInfo.ArgumentList.Add($"repos/{owner}/{repo}/{rutaRelativa}");
        foreach (string argumento in argumentos) {
            startInfo.ArgumentList.Add(argumento);
        }

        using Process proceso = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo iniciar gh.");

        string salida = proceso.StandardOutput.ReadToEnd().Trim();
        string error = proceso.StandardError.ReadToEnd().Trim();

        proceso.WaitForExit();

        if (proceso.ExitCode != 0) {
            string detalle = string.IsNullOrWhiteSpace(error) ? salida : error;
            Log.Error($"{mensajeError}: {detalle}");
            return null;
        }

        return salida;
    }


    string? ObtenerTituloPR(int numeroPR) {
        string? salida = Ejecutar($"Error al obtener el título del PR #{numeroPR}",
            $"/pulls/{numeroPR}", "--jq", ".title");

        return string.IsNullOrWhiteSpace(salida) ? null : salida.Trim();
    }


    static List<string> Lineas(string texto, bool pasarAMinusculas = true) {
        return texto.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
                    .Select(linea => linea.Trim())
                    .Select(linea => pasarAMinusculas ? linea.ToLower() : linea)
                    .Where(linea => !string.IsNullOrWhiteSpace(linea))
                    .ToList();
    }


    static string NormalizarRutaRemota(string ruta) {
        return ruta.Trim().Replace('\\', '/').Trim('/');
    }


    static bool TryObtenerRutaRelativaDirectorio(string nombreRemoto, string carpetaAlumnoRemota, string directorioRemoto, out string rutaRelativa) {
        rutaRelativa = string.Empty;

        string nombreNormalizado = NormalizarRutaRemota(nombreRemoto);
        string carpetaAlumnoNormalizada = NormalizarRutaRemota(carpetaAlumnoRemota);
        string directorioNormalizado = NormalizarRutaRemota(directorioRemoto);
        if (string.IsNullOrWhiteSpace(nombreNormalizado) || string.IsNullOrWhiteSpace(carpetaAlumnoNormalizada) || string.IsNullOrWhiteSpace(directorioNormalizado)) {
            return false;
        }

        string[] segmentos = nombreNormalizado.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmentos.Length < 3) {
            return false;
        }

        for (int i = 0; i <= segmentos.Length - 3; i++) {
            if (!EsCarpetaAlumnoEsperada(segmentos[i], carpetaAlumnoNormalizada)) {
                continue;
            }

            if (!string.Equals(segmentos[i + 1], directorioNormalizado, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            rutaRelativa = string.Join('/', segmentos[(i + 2)..]);
            return !string.IsNullOrWhiteSpace(rutaRelativa);
        }

        return false;
    }


    static List<int> TPsPresentadosDesdeArchivos(IEnumerable<string> nombresRemotos, string carpetaAlumnoRemota) {
        string carpetaAlumno = NormalizarRutaRemota(carpetaAlumnoRemota);
        if (string.IsNullOrWhiteSpace(carpetaAlumno)) {
            return new();
        }

        HashSet<int> trabajosPracticos = new();
        foreach (string nombreRemoto in nombresRemotos) {
            if (TryObtenerTpDesdeRutaAlumno(nombreRemoto, carpetaAlumno, out int numeroTp)) {
                trabajosPracticos.Add(numeroTp);
            }
        }

        return trabajosPracticos.Order().ToList();
    }


    static bool TryObtenerTpDesdeRutaAlumno(string nombreRemoto, string carpetaAlumnoRemota, out int numeroTp) {
        numeroTp = 0;

        string nombreNormalizado = NormalizarRutaRemota(nombreRemoto);
        string carpetaAlumnoNormalizada = NormalizarRutaRemota(carpetaAlumnoRemota);
        if (string.IsNullOrWhiteSpace(nombreNormalizado) || string.IsNullOrWhiteSpace(carpetaAlumnoNormalizada)) {
            return false;
        }

        string[] segmentos = nombreNormalizado.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmentos.Length < 3) {
            return false;
        }

        for (int i = 0; i <= segmentos.Length - 3; i++) {
            if (!EsCarpetaAlumnoEsperada(segmentos[i], carpetaAlumnoNormalizada)) {
                continue;
            }

            Match match = Regex.Match(segmentos[i + 1], @"^tp(\d+)$", RegexOptions.IgnoreCase);
            if (!match.Success) {
                continue;
            }

            return int.TryParse(match.Groups[1].Value, out numeroTp) && numeroTp > 0;
        }

        return false;
    }


    static bool EsCarpetaAlumnoEsperada(string segmentoRemoto, string carpetaAlumnoRemota) {
        int legajoEsperado = ExtraerLegajo(carpetaAlumnoRemota);
        if (legajoEsperado > 0) {
            return ExtraerLegajo(segmentoRemoto) == legajoEsperado;
        }

        return string.Equals(segmentoRemoto, carpetaAlumnoRemota, StringComparison.OrdinalIgnoreCase);
    }


    static int ContarLineas(byte[] contenido) {
        if (contenido.Length == 0) {
            return 0;
        }

        int lineas = contenido.Count(b => b == (byte)'\n');
        return contenido[^1] == (byte)'\n' ? lineas : lineas + 1;
    }


    public static int ExtraerTP(string titulo) {
        Match match = Regex.Match(titulo, @"\bTP\s*-?\s*\d+\b", RegexOptions.IgnoreCase);
        return match.Success ? int.Parse(match.Value[2..].Replace("-", "").Trim()) : 0;
    }


    public static int ExtraerLegajo(string titulo) {
        Match match = Regex.Match(titulo, @"\b\d{5}\b");
        return match.Success ? int.Parse(match.Value) : 0;
    }

}
