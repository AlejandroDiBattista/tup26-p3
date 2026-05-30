// -- Conjunto genérico con LINQ --
// En esta clase mostramos cómo consultar y combinar conjuntos usando LINQ.

using System;
using System.Collections;
using System.Collections.Generic;

static class Program {
    public static void Main() {

        var teorica = new Conjunto<Alumno>();
        teorica.Agregar(new Alumno("Car", 25, 8));
        teorica.Agregar(new Alumno("Ana", 20, 9));
        teorica.Agregar(new Alumno("Bob", 22, 4));
        teorica.Agregar(new Alumno("Car", 25, 8));
        teorica.Agregar(new Alumno("Dia", 18, 9));
        
        var practica = new Conjunto<Alumno>();
        practica.Agregar(new Alumno("Eve", 30, 8));
        practica.Agregar(new Alumno("Bob", 22, 7));
        practica.Agregar(new Alumno("Fra", 28, 8));
        practica.Agregar(new Alumno("Dia", 18, 9));

        Console.Clear();

        Console.WriteLine("== Alumnos (Clase Teórica) ==");
        foreach (var alumno in teorica) {
            Console.WriteLine($" - {alumno}");
        }
        Console.WriteLine($"Nota promedio : {teorica.Average(a => a.Nota),0:F2}");
        Console.WriteLine($"Nota mínima   : {teorica.Min(a => a.Nota),0:F2}");
        Console.WriteLine($"Nota máxima   : {teorica.Max(a => a.Nota),0:F2}");

        Console.WriteLine("\n== Alumnos (Filtrados y Ordenados) ==");
        foreach (var alumno in teorica.Where(a => a.Legajo > 20).OrderBy(a => a.Nombre)) {
            Console.WriteLine($" - {alumno}");
        }

        Console.WriteLine("\n== Alumnos (En alguna de las clases) ==");
        foreach (var alumno in teorica.Union(practica)) {
            Console.WriteLine($" - {alumno}");
        }

        Console.WriteLine("\n== Alumnos (En ambas) ==");
        foreach (var alumno in teorica.Intersect(practica).OrderBy(a => a.Nombre)) {
            Console.WriteLine($" - {alumno}");
        }
    }
}

record class Alumno(string Nombre, int Legajo, int Nota) : IEquatable<Alumno>, IComparable<Alumno> {
    
    public bool Equals(Alumno? otro) {
        if (otro is null) { return false; }
        return this.Legajo == otro.Legajo;
    }

    public int CompareTo(Alumno? otro) {
        if (otro is null) { return 1; }
        return this.Nombre.CompareTo(otro.Nombre);
    }
    
    public override string ToString() => $"{Nombre,20} (Legajo: {Legajo}, Nota: {Nota})";
}


interface IConjunto<T> : IEnumerable<T> where T : IEquatable<T> , IComparable<T> {
    void Agregar(T valor);
    void Eliminar(T valor);
    bool Contiene(T valor);
}

class Conjunto<T> : IConjunto<T> where T : IEquatable<T> , IComparable<T> {
    readonly List<T> elementos = new();

    public void Agregar(T valor) {
        if (Contiene(valor)) { return; }
        elementos.Add(valor);
    }

    public void Eliminar(T valor) {
        elementos.Remove(valor);
    }

    public bool Contiene(T valor) {
        return elementos.Contains(valor);
    }

    public IEnumerator<T> GetEnumerator() {
        foreach (var elemento in elementos) {
            yield return elemento;
        }
    }

    public override string ToString() => $"{{{string.Join(", ", elementos)}}}";
}