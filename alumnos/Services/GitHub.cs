using System.Data.SqlTypes;
using System.IO.Enumeration;

namespace Tup26.AlumnosApp;

class GitHub {
    readonly string owner;
    readonly string repo;

    public GitHub(string owner = "AlejandroDiBattista", string repo = "tup26-p3") {
        this.owner = owner;
        this.repo = repo;
    }


    public bool AgregarColaborador(string usuario) {
        string? salida = Ejecutar( $"Error al agregar colaborador '{usuario}'",
            $"/collaborators/{usuario}", "--method", "PUT", "-f", "permission=push" );

        return salida is not null;
    }


    public List<string> Colaboradores() {
        string? salida = Ejecutar( "Error al listar colaboradores",
            "/collaborators", "--jq", ".[] | select(.permissions.push == true) | .login" );

        if (salida is null) { return new(); }

        return Lineas(salida);
    }


    public List<string> InvitacionesPendientes() {
        string? salida = Ejecutar( "Error al listar invitaciones pendientes",
            "/invitations", "--paginate", "--jq", ".[].invitee.login" );

        if (salida is null) { return new(); }

        return Lineas(salida);
    }


    public List<(int Numero, string Titulo)> PullRequests(bool soloAbiertos = true) {
        string estado = soloAbiertos ? "open" : "all";

        string? salida = Ejecutar( "Error al listar PRs",
            $"/pulls?state={estado}", "--paginate", "--jq", @".[] | ""\(.number)\t\(.title)""" );

        if (salida is null) { return new(); }

        List<(int Numero, string Titulo)> prs = new();
        foreach (string linea in Lineas(salida, pasarAMinusculas: false)) {
            string[] partes = linea.Split('\t', 2);
            if (partes.Length != 2) { continue; }

            if (!int.TryParse(partes[0], out int numero)) { continue; }

            prs.Add((numero, partes[1]));
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
        List<(int Numero, string Titulo)> prs = PullRequests();
        int count = 0;

        foreach ((int Numero, string Titulo) pr in prs) {
            int legajo = ExtraerLegajo(pr.Titulo);
            Alumno? alumno = alumnos.BuscarPorLegajo(legajo);

            if (alumno != null) {
                string nuevoTitulo = $"{legajo} - TP{ExtraerTP(pr.Titulo)} - {alumno.NombreCompleto}";

                if (nuevoTitulo != pr.Titulo) {
                    if (count++ == 0) {
                        Log.Info("= PRs a actualizar =");
                    }

                    Log.Info($"Actualizando PR #{pr.Numero}:\n > {pr.Titulo}\n < {nuevoTitulo}");

                    if (!simular) {
                        CambiarTitulo(pr.Numero, nuevoTitulo);
                    }
                }
            }
        }

        if (count == 0) {
            Log.Info("No se encontraron PRs para actualizar.");
        } else {
            Log.Info($"Total de PRs a actualizar: {count}");
        }

        return count;
    }


    public (string Estado, bool EsMergeable) ObtenerEstado(int numeroPR) {
        string? salida = Ejecutar( $"Error al consultar el estado del PR #{numeroPR}",
            $"/pulls/{numeroPR}", "--jq", @"""\(.state)\t\(.mergeable)""" );

        if (salida is null) { return (string.Empty, false); }

        string[] partes = salida.Trim().Split('\t', 2);
        if (partes.Length != 2) { return (string.Empty, false); }

        return (partes[0].ToLower(), partes[1].ToLower() == "true");
    }


    public List<string> ListarArchivos(int numeroPR) {
        string? salida = Ejecutar( $"Error al listar archivos del PR #{numeroPR}",
            $"/pulls/{numeroPR}/files", "--paginate", "--jq", @".[] | .filename" );

        if (salida is null) { return new(); }

        return Lineas(salida);
    }

    public void CerrarPR(int numeroPR) {
        string? salida = Ejecutar( $"Error al cerrar el PR #{numeroPR}",
            $"/pulls/{numeroPR}", "--method", "PATCH", "-f", "state=closed" );

        if (salida is not null) {
            Log.Info($"PR #{numeroPR} cerrado exitosamente.");
        }
    }

    public void BajarArchivo(int numeroPR, string patron, string rutaDestino) {
        string? salida = Ejecutar( $"Error al bajar archivos del PR #{numeroPR}",
            $"/pulls/{numeroPR}/files", "--paginate", "--jq", ".[] | \"\\(.filename)\\t\\(.raw_url)\"" );

        if (salida is null) { return; }

        List<string> urls = Lineas(salida, pasarAMinusculas: false);

        foreach (string linea in urls) {
            try {
                string[] partes = linea.Split('\t', 2);
                if (partes.Length != 2) { continue; }

                string nombreRemoto = partes[0];
                string url = partes[1];

                if (!FileSystemName.MatchesSimpleExpression(patron, nombreRemoto, ignoreCase: true)) { continue; }

                using HttpClient client = new();
                string nombreArchivo = Path.GetFileName(nombreRemoto);
                string rutaArchivo   = Path.Combine(rutaDestino, nombreArchivo);

                byte[] contenido = client.GetByteArrayAsync(url).Result;
                File.WriteAllBytes(rutaArchivo, contenido);
                Log.Warning($"Archivo '{nombreRemoto}'\n      {rutaArchivo} ");
            } catch (Exception ex) {
                Log.Error($"Error al descargar el archivo desde '{linea}': {ex.Message}");
            }
        }
    }

    public bool Merge(int numeroPR) {
        var detalle = ObtenerEstado(numeroPR);

        if (!string.Equals(detalle.Estado, "open")) {
            Log.Error($"Error al mergear el PR #{numeroPR}: el PR no está abierto.");
            return false;
        }

        string? salida = Ejecutar( $"Error al mergear el PR #{numeroPR}",
            $"/pulls/{numeroPR}/merge", "--method", "PUT", "-f", "merge_method=merge" );

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

        string? salida = Ejecutar( $"Error al cambiar el título del PR #{numeroPR}",
            $"/pulls/{numeroPR}", "--method", "PATCH", "-f", $"title={titulo}" );

        return salida is not null;
    }


    public List<(string Titulo, DateTimeOffset FechaHora)> Commits(int numeroPR) {
        string? salida = Ejecutar( $"Error al listar commits del PR #{numeroPR}",
            $"/pulls/{numeroPR}/commits", "--paginate", "--jq", @".[] | ""\(.commit.message | split(""\n"")[0])\t\(.commit.author.date)""" );

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
            RedirectStandardError  = true
        };

        startInfo.ArgumentList.Add("api");
        string rutaRelativa = endpoint.TrimStart('/');
        startInfo.ArgumentList.Add($"repos/{owner}/{repo}/{rutaRelativa}");
        foreach (string argumento in argumentos) {
            startInfo.ArgumentList.Add(argumento);
        }

        using Process proceso = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo iniciar gh.");

        string salida = proceso.StandardOutput.ReadToEnd().Trim();
        string error  = proceso.StandardError.ReadToEnd().Trim();

        proceso.WaitForExit();

        if (proceso.ExitCode != 0) {
            string detalle = string.IsNullOrWhiteSpace(error) ? salida : error;
            Log.Error($"{mensajeError}: {detalle}");
            return null;
        }

        return salida;
    }


    static List<string> Lineas(string texto, bool pasarAMinusculas = true) {
        return texto.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
                    .Select(linea => linea.Trim())
                    .Select(linea => pasarAMinusculas ? linea.ToLower() : linea)
                    .Where(linea => !string.IsNullOrWhiteSpace(linea))
                    .ToList();
    }

    public static int ExtraerTP(string titulo) {
        Match match = Regex.Match(titulo, @"\bTP\s*\d+\b", RegexOptions.IgnoreCase);
        return match.Success ? int.Parse(match.Value[2..].Trim()) : 0;
    }

    public static int ExtraerLegajo(string titulo) {
        Match match = Regex.Match(titulo, @"\b\d{5}\b");
        return match.Success ? int.Parse(match.Value) : 0;
    }
   
}
