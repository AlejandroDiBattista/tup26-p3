namespace Tup26.AlumnosApp;

class Program {
    static int ExtraerLegajo(string titulo) {
        var match = System.Text.RegularExpressions.Regex.Match(titulo, @"\b\d{5}\b");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    static int ListarPrSinLegajo(GitHub gh) {
        var prs = gh.ListarPRs();
        int count = 0;
        foreach(var pr in prs) {
            if (ExtraerLegajo(pr.Titulo) == 0) {
                count++;
                if(count == 1) {
                    Console.WriteLine("= PR Sin legajo válido =");
                }
                Console.WriteLine($"- #{pr.Numero}: {pr.Titulo}");
            }
        }
        if (count == 0) {
            Console.WriteLine("Todos los PRs tienen un legajo válido en el título.");
        } else {
            Console.WriteLine($"Total de PRs sin legajo válido: {count}");
        }
        return count;
    }

    static int NormalizarTitulosPR(GitHub gh, Alumnos alumnos, bool simular = false) {
        var prs = gh.ListarPRs();
        int count = 0;
        foreach(var pr in prs) {
            var legajo = ExtraerLegajo(pr.Titulo);
            var alumno = alumnos.BuscarPorLegajo(legajo);
            if (alumno != null) {
                string nuevoTitulo = $"{legajo} - TP1 - {alumno.NombreCompleto}";
                if (nuevoTitulo != pr.Titulo) {
                    count++;
                    if(count == 1) {
                        Console.WriteLine("= PRs a actualizar =");
                    }
                    Console.WriteLine($"Actualizando PR #{pr.Numero}:\n > {pr.Titulo}\n < {nuevoTitulo}");
                    if (!simular) { gh.CambiarTituloPR(pr.Numero, nuevoTitulo); }
                }
            }
        }
        if (count == 0) {
            Console.WriteLine("No se encontraron PRs para actualizar.");
        } else {
            Console.WriteLine($"Total de PRs a actualizar: {count}");
        }
        return count;
    }

    static void Main(string[] args) {
        Alumnos alumnos = AlumnosManager.Cargar(AppPaths.ArchivoAlumnos);
        
        // AlumnosManager.Listar(alumnos.ConFotos(false), "Alumnos sin foto");
        // AlumnosManager.Listar(alumnos.ConTelefono(false), "Alumnos sin telefono");
        // AlumnosManager.Listar(alumnos.ConGithub(false).ConTelefono(true), "Alumnos sin GitHub");
        // AlumnosManager.ActualizarDesdePerfiles(alumnos, "../material-docente/perfil/perfiles");
        // AlumnosManager.Guardar(alumnos, AppPaths.ArchivoAlumnos);
        // AlumnosManager.GuardarJSON(alumnos, "alumnos.json");
        // AlumnosManager.GuardarVCard(alumnos, AppPaths.ArchivoVcf);
        // AlumnosManager.CrearCarpetas(alumnos);
        // AlumnosManager.CopiarFotoPerfil(alumnos, rutaPerfiles);

        // var sinFoto = alumnos.SinFotos();
        // AlumnosManager.Guardar(sinFoto, "alumnos-sin-foto.md");

        // var sinTelefono = alumnos.SinTelefono();
        // AlumnosManager.Guardar(sinTelefono, "alumnos-sin-telefono.md");

        // var sinGitHub = alumnos.FiltrarSinGithub();
        // AlumnosManager.Guardar(sinGitHub, "alumnos-sin-github.md");

        // Console.WriteLine("Enviando mensaje al grupo de WhatsApp...");

        // Alumnos alumnosConTelefonoComision7 = alumnos.EnComision("C7").SinTelefono();
        // AlumnosManager.Listar(alumnosConTelefonoComision7);

        // Alumnos alumnosConTelefonoComision9 = alumnos.EnComision("C9").SinTelefono();

        // AlumnosManager.Listar(alumnos.ParaAgregar());

        // AlumnosManager.GuardarVCard(alumnos.ParaAgregar().EnComision("C7"), "alumnos-agregar-c7.vcf");
        // AlumnosManager.GuardarVCard(alumnos.ParaAgregar().EnComision("C9"), "alumnos-agregar-c9.vcf");
        // AlumnosManager.CopiarEnunciadoPracticos(alumnos, "tp1");

        GitHub gh = new GitHub();
        if (ListarPrSinLegajo(gh) == 0) {
            NormalizarTitulosPR(gh, alumnos, simular: false);
        } 
        var prs = gh.ListarPRs();
        
        foreach(var pr in prs.OrderBy(pr => pr.Numero)) {
            var commits = gh.ListarCommitsPR(pr.Numero);
            string estado = pr.EstaAbierto ? "abierto" : "cerrado";
            string mergeable = pr.EsMergeable switch {
                true => "mergeable",
                false => "con conflictos",
                null => "sin dato"
            };

            Console.WriteLine($"- #{pr.Numero:D3} | {estado,-7} | {mergeable,-13} | {(commits.Count > 3 ? "🟢" : "🔴")} {commits.Count,2} | {pr.Titulo}");
            foreach(var commit in commits) {
                // Console.WriteLine($" > {commit.FechaHora:dd-MM HH:mm} - {commit.Titulo} ()");
            }
        }
        // List<string> colaboradores = gh.ListarColaboradores();
        // List<string> invitaciones  = gh.ListarInvitacionesPendientes();

        // Console.WriteLine($"Colaboradores: {string.Join(" ", colaboradores)}");
        // Console.WriteLine($"Pendientes: {string.Join(" ", invitaciones)}");
        // Console.WriteLine();

        // AlumnosManager.Guardar(alumnos, AppPaths.ArchivoAlumnos);

        // MensajesService.MensajeSinGithub();
        // MensajesService.MensajeGithubErroneo();

        // var wapp = new WAppService();
        // // wapp.Enviar("3815343456", "Hola desde la aplicación de alumnos!");
        // Console.WriteLine("\n= Participantes del grupo C7 =");
        // foreach(var c in wapp.Participantes("C7")) {
        //     Console.WriteLine($"- {c.Name,-30} | {c.PhoneNumber} | {c.Jid}");
        // }
        // Console.WriteLine("\n= Participantes del grupo C9 =");
        // foreach(var c in wapp.Participantes("C9")) {
        //     Console.WriteLine($"- {c.Name,-30} | {c.PhoneNumber} | {c.Jid}");
        // }
        // Console.WriteLine("\n= Grupos ==");
        // foreach(var g in wapp.Grupos()) {
        //     Console.WriteLine($"- {g.Group,-30} | {g.Jid}");
        // }

    }
}
