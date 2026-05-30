using static System.Console;
using System.Collections;
using System.Collections.Generic;

record Alumno(string Nombre, int Edad, bool Aprobado, int Legajo) : IEquatable<Alumno> {
    public virtual bool Equals(Alumno? other) => Legajo == other?.Legajo;
    public override int GetHashCode() => Legajo.GetHashCode();
}

// Para poder usar foreach en Curso, necesitamos implementar IEnumerable<Alumno>

static class Programa {
    static void Main() {
        var aliceBis = new Alumno("Alice Bis", 20, true, 1008); // Mismo legajo que Alice, pero no es el mismo objeto

        IEnumerable<Alumno> curso = new List<Alumno> {
            new Alumno("Alice",   20, true,  1008),
            new Alumno("Bob",     22, false, 1007),
            new Alumno("Charlie", 19, true,  1009),
            new Alumno("Daniel",  21, false, 1010),
            new Alumno("Eve",     20, true,  1011),
            new Alumno("Frank",   23, false, 1012)
        };



        // === Programación tradicional ===


        var aprobados = new List<Alumno>();
        foreach (Alumno alumno in curso) {
            if(alumno.Aprobado) {
                aprobados.Add(alumno);
            }
        }

        List<int> edades = new();
        foreach (Alumno alumno in aprobados) {
            edades.Add(alumno.Edad);
        }

        var suma = 0.0;
        foreach (int edad in edades) {
            suma += edad;
        }
        var promedio = suma / edades.Count;

        WriteLine($"La edad promedio de los aprobados es {promedio:0.0}");


        // === Programación funcional con LINQ (Language Integrated Query) ===

        var promedio = curso
                         .Where(a => a.Aprobado)
                         .Select(a => a.Edad)
                         .Average();

        // Agregamos métodos de extensión para poder usar la sintaxis de LINQ, que es más declarativa y fácil de leer.
        var n = curso.Mayores(20).CantidadAprobados();
        WriteLine($"Hay {n} alumnos mayores de 20 años que aprobaron");         
        WriteLine($"Hay {curso.Aprobados().Count()} alumnos aprobados");
    }

    
}

// LINQ es un conjunto de métodos de extensión que permiten realizar 
// operaciones de consulta sobre colecciones de datos de manera declarativa,
// utilizando expresiones lambda para definir los criterios de filtrado, proyección y agregación.

static class Extensiones {
    extension(IEnumerable<Alumno> valores) {
        public IEnumerable<Alumno> Aprobados() => 
            Where(a => a.Aprobado);
        
        public IEnumerable<Alumno> Mayores(int edad) => 
            Where(a => a.Edad > edad);
        
        public int CantidadAprobados() => 
            Aprobados().Count();
    }
}