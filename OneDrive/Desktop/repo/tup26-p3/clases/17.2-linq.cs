using System.Collections;
using System.Collections.Generic;
using System.Linq; // LInQ : Language Integrated Query

using static System.Console;

Curso curso = new Curso();
curso.Agregar(new Alumno("Alice",   20, true,  61008));
curso.Agregar(new Alumno("Bob",     22, false, 61007));
curso.Agregar(new Alumno("Charlie", 19, true,  61009));
curso.Agregar(new Alumno("Daniel",  21, false, 61010));
curso.Agregar(new Alumno("Eve",     20, true,  61011));
curso.Agregar(new Alumno("Frank",   23, false, 61012));


IEnumerable<Alumno> lista = curso.OrderBy(e => e.Edad).Take(3);


var pares = curso
              .OrderBy(a => a.Nombre)
              .Where(a => a.Aprobado && a.Edad >= 21)
              .Select(a => new {a.Legajo, a.Edad });

// Sentencia SQL equivalente:
// SELECT Legajo, Edad FROM Alumnos WHERE Aprobado = 1 AND Edad >= 21 ORDER BY Nombre;

var min = curso.Min(a => a.Edad);
// SELECT MIN(Edad) FROM Alumnos;

var max = curso.Select(a => a.Edad).Max();
var alternados = curso.Where((a, i) => i % 2 == 0); // Solo los alumnos en posiciones pares (0, 2, 4, ...)
// SELECT * FROM Alumnos WHERE (ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1) % 2 = 0;


var todosAprobados = curso.Select(a => a.Aprobado).All();
    todosAprobados = curso.All(a => a.Aprobado); // Equivalente a la línea anterior, pero más eficiente porque no necesita proyectar toda la secuencia de aprobados.

var algunoAprobado = curso.Select(a => a.Aprobado).Any();
    algunoAprobado = curso.Any(a => a.Aprobado); // Equivalente a la línea anterior, pero más eficiente porque no necesita proyectar toda la secuencia de aprobados.

var cantidadAprobados = curso.Count(a => a.Aprobado);
var legajosAprobados  = curso.Where(a => a.Aprobado).Select(a => a.Legajo);
// SELECT Legajo FROM Alumnos WHERE Aprobado = 1;

var pagina = 2, tamaño = 5;
var pagina2 = curso.Skip((pagina-1) * tamaño).Take(tamaño); // Paginación: página p con 5 elementos por página
var pagina3 = curso.Pagina(pagina, tamaño); // Usando el método de extensión para paginar

var top3Mayores = curso.OrderByDescending(a => a.Edad).Take(3);
var top3Menores = curso.OrderBy(a => a.Edad).Take(3);

var agrupadosPorAprobacion = curso.GroupBy(a => a.Aprobado);
foreach (var grupo in agrupadosPorAprobacion) {
    WriteLine($"Aprobados: {grupo.Key} - Cantidad: {grupo.Count()}");
    foreach (var alumno in grupo) {
        WriteLine($"  - {alumno.Nombre} ({alumno.Edad} años)");
    }
}

// == Clases auxiliares ==

record Par(int Legajo, int Edad); // DTO Data Transfer Object

// == Clases auxiliares ==
record Alumno(string Nombre, int Edad, bool Aprobado, int Legajo) : IEquatable<Alumno> {
    public virtual bool Equals(Alumno? other) => Legajo == other?.Legajo;
    public override int GetHashCode() => Legajo.GetHashCode();
}

// == Clase que implementa IEnumerable<Alumno> para poder usar foreach y LINQ ==

class Curso : IEnumerable<Alumno> {
    private List<Alumno> alumnos = new();

    public void Agregar(Alumno alumno) => alumnos.Add(alumno);
    
    public IEnumerator<Alumno> GetEnumerator() {
        foreach(var alumno in alumnos) {
            yield return alumno;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// == Ejemplos de métodos de extensión (para IEnumerable<Alumno>) ==
static class ExtensionesAlumnos {
    extension (IEnumerable<Alumno> alumnos) {
        public int CantidadAprobados() => alumnos.Count(a => a.Aprobado);
        public double EdadPromedio() => alumnos.Where(a => a.Aprobado).Average(a => a.Edad);
    }
    extension (IEnumerable<T> secuencia) {
        public IEnumerable<T> Pagina(int numero, int tamaño = 10) => secuencia.Skip((numero-1) * tamaño).Take(tamaño);
    }
}