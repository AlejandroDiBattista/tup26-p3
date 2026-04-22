namespace Tup26.AlumnosApp;

static class MensajesService {
    public static void MensajeGithubErroneo() {
        Alumnos alumnos = AlumnosManager.Cargar(AppPaths.ArchivoAlumnos);

        foreach (string comision in new[] { "C7", "C9" }) {
            Alumnos lista = alumnos.ConGithub(true).EnComision(comision).ConPractico(1, Estado.Revision);
            if (lista.Count == 0) { continue; }

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

    public static void MensajeSinGithub() {
        Alumnos alumnos = AlumnosManager.Cargar(AppPaths.ArchivoAlumnos);

        foreach (string comision in new[] { "C7", "C9" }) {
            Alumnos lista = alumnos.ConGithub(false).EnComision(comision);
            if (lista.Count == 0) { continue; }

            Console.WriteLine($"""
            *{comision} - Sin Usuario GitHub*
            
            Los siguientes alumnos no me informaron cual es su usuario en GitHub.
            Es necesario que tengan un usuario de GitHub para poder publicar el trabajo práctico y que yo pueda corregirlo.

            ```
            """);

            foreach (Alumno a in lista) {
                Console.WriteLine($"{a.Legajo}: {a.NombreCompleto}");
            }

            Console.WriteLine("""
            Envien el usuario junto con su legajo por este grupo 

            p.e "63241 josias57455"

            """);
        }
    }
}
