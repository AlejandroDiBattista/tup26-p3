namespace Tup26.AlumnosApp;

static class MensajesService {
    public static void MensajeGithubErroneo() {
        Alumnos alumnos = AlumnosManager.Leer(AppPaths.ArchivoAlumnos);

        foreach (string comision in new[] { "C7", "C9" }) {
            Alumnos lista = alumnos.ConGithub(true).EnComision(comision).ConPractico(1, Estado.Revision);
            if (!lista.Any()) { continue; }

            Console.WriteLine("""
            *GitHub Erroreos ⁉️*
            
            Estos alumnos me informaron un usuario pero no los encuentro en de GitHub.

            Podria ser que haya un error de tipeo o de carga. 
            Si es así, por favor envien el usuario correcto junto con su legajo para que los autorice a publicar el trabajo práctico.

            ```
            """);

            foreach (Alumno a in lista) {
                Console.WriteLine($"{a.Legajo}: {a.NombreCompleto,20} {a.GitHub}");
            }

            Console.WriteLine("""
            Envien el usuario correcto junto con su legajo por este grupo

            """);
        }
    }

}
