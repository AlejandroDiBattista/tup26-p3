using System.Collections.Generic;
using System.Collections;

class Alumno(string nombre, int edad, int legajo) : IComparable<Alumno>, IEquatable<Alumno> {
    public string Nombre { get; } = nombre;
    public int Edad { get; } = edad;
    public int Legajo { get; } = legajo;

    public override string ToString() => $"✅ {Nombre,-20} ({Edad} años, legajo {Legajo})";
    // Para ordenar 
    public int CompareTo(Alumno? other) => -Nombre.CompareTo(other?.Nombre);
    // Para comparar 
    public bool Equals(Alumno? other) => Legajo == other?.Legajo;
}

record class Curso(string Nombre) : IEnumerable<Alumno> {
    public List<Alumno> Alumnos { get; } = new();

    public void Add(Alumno alumno) {
        Alumnos.Add(alumno);
    }

    public List<Alumno> Ordenados(){
        List<Alumno> resultado = new List<Alumno>(Alumnos);
        resultado.Sort();
        return resultado;
    }

    public bool Contains(Alumno alumno) {
        return Alumnos.Contains(alumno);
    }

    // Para poder usar foreach en Curso, necesitamos implementar IEnumerable<Alumno>
    public IEnumerator<Alumno> GetEnumerator() {
        return Alumnos.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() =>  GetEnumerator();
}

class Program {

    static void Main() {
        var alice   = new Alumno("Alice", 20, 1008);
        var bob     = new Alumno("Bob", 22, 1007);
        var charlie = new Alumno("Charlie", 19, 1009);
        var aliceBis = new Alumno("Alice Bis", 20, 1008);


        var curso = new Curso("Programación 1");
        curso.Add(alice);
        curso.Add(bob);
        curso.Add(charlie);
        
        var lab2 = new Curso("Laboratorio 2");
        lab2.Add(alice);
        lab2.Add(bob);
        lab2.Add(charlie);

         Console.WriteLine(curso.Nombre);
         foreach (var alumno in curso) {
             Console.WriteLine($" - {alumno}");
         }
         Console.WriteLine(curso.Contains(alice));
         Console.WriteLine(curso.Contains(aliceBis));

        foreach (var alumno in curso.OrderBy( a => a.Edad )) {
            Console.WriteLine($" - {alumno}");
        }
        
    }
}