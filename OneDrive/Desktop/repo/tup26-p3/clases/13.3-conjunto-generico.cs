// -- Conjunto genérico con lista interna --
// En esta clase generalizamos el conjunto para que pueda trabajar con distintos tipos de datos usando una lista interna.

static class Program {
    public static void Main() {
        var a = new ConjuntoInt();
        a.Agregar(5);
        a.Agregar(10);

        Console.Clear();
        Console.WriteLine("== Conjunto<int> ==");
        Console.WriteLine($" Hay {a.Count} elementos."); // 2
        Console.WriteLine($" - Contiene  5?: {a.Contiene(5)}");  // True
        Console.WriteLine($" - Contiene 10?: {a.Contiene(10)}"); // True
        Console.WriteLine($" - Contiene  3?: {a.Contiene(3)}");  // False

        var b = new Conjunto<string>();
        b.Agregar("Hola");
        b.Agregar("Mundo");

        Console.WriteLine("\n== Conjunto<string> ==");
        Console.WriteLine($" Hay {b.Count} elementos."); // 2
        Console.WriteLine($" - Contiene 'Hola'?: {b.Contiene("Hola")}");  // False
        Console.WriteLine($" - Contiene 'Mundo'?: {b.Contiene("Mundo")}"); // True
        Console.WriteLine($" - Contiene 'C#'?: {b.Contiene("C#")}");  // True

        var c = new ConjuntoString();
        c.Agregar("Hola");
        c.Agregar("Mundo");

        Console.WriteLine("\n== Conjunto<string> ==");
        Console.WriteLine($" Hay {c.Count} elementos."); // 2
        Console.WriteLine($" - Contiene 'Hola'?: {c.Contiene("Hola")}");  // False
        Console.WriteLine($" - Contiene 'Mundo'?: {c.Contiene("Mundo")}"); // True
        Console.WriteLine($" - Contiene 'C#'?: {c.Contiene("C#")}");  // True

        // b is Conjunto<string> que implementa IConjunto<string>
        // c is ConjuntoString que implementa IConjunto<string>
        
        var d = Union(b, c); // Mezclamos los elementos de 2 clases distintas pero del mismo tipo genérico (string) para obtener un nuevo conjunto con los elementos de ambos conjuntos.
        Console.WriteLine("\n== Unión de Conjunto<string> ==");
        Console.WriteLine($" Resultado: {d}");
    }

    static IConjunto<T> Union<T>(IConjunto<T> a, IConjunto<T> b) where T : IEquatable<T> {
        var resultado = new Conjunto<T>();
        foreach (var e in a.Elementos) {
            resultado.Agregar(e);
        }
        foreach (var e in b.Elementos) {
            resultado.Agregar(e);
        }
        return resultado;
     }

     static IConjunto<T> Interseccion<T>(IConjunto<T> a, IConjunto<T> b) where T : IEquatable<T> {
        var resultado = new Conjunto<T>();
        foreach (var e in a.Elementos) {
            if (b.Contiene(e)) {
                resultado.Agregar(e);
            }
        }
        return resultado;
     }
}

// Definimos una interface generica para conjuntos, que especifica los métodos y propiedades que debe implementar cualquier clase que represente un conjunto de elementos de tipo T.
interface IConjunto<T> where T : IEquatable<T>  {
    void Agregar(T valor);
    void Eliminar(T valor);
    bool Contiene(T valor);
    List<T> Elementos { get; }
    int Count { get; }
}

// Clase genérica: Permite definir una clase con un tipo de dato genérico, que se especifica al crear una instancia de la clase.
// El tipo genérico se representa con una letra mayúscula (por convención) 
public class Conjunto<T> : IConjunto<T> where T : IEquatable<T> {
    List<T> elementos;

    public Conjunto() {
        elementos = new List<T>();
    }

    public Conjunto(List<T> elementos) {
        this.elementos = new List<T>();
        foreach (var e in elementos) {
            Agregar(e);
        }
    }

    public void Agregar(T valor) {
        if (!Contiene(valor)) {
            elementos.Add(valor);
        }
    }

    public void Eliminar(T valor) {
        elementos.Remove(valor);
    }

    public bool Contiene(T valor) {
        foreach (var e in elementos) {
            if (e.Equals(valor)) {
                return true;
            }
        }
        return false;
    }

    public List<T> Elementos => elementos.ToList(); // Devuelve una copia de la lista de elementos para evitar que se modifique desde afuera.

    public override string ToString() => $"{{{string.Join(", ", elementos)}}}";
    public int Count => elementos.Count;
}


// Implementamos la clase Conjunto de `int` 
class ConjuntoInt : IConjunto<int>  {
    List<int> elementos;

    public ConjuntoInt() {
        elementos = new List<int>();
    }

    public ConjuntoInt(List<int> elementos) {
        this.elementos = new List<int>();
        foreach (var e in elementos) {
            Agregar(e);
        }
    }

    public void Agregar(int valor) {
        if (!Contiene(valor)) {
            elementos.Add(valor);
        }
    }

    public void Eliminar(int valor) {
        elementos.Remove(valor);
    }

    public bool Contiene(int valor) {
        foreach (var e in elementos) {
            if (e.Equals(valor)) {
                return true;
            }
        }
        return false;
    }

    public List<int> Elementos => elementos.ToList(); // Devuelve una copia de la lista de elementos para evitar que se modifique desde afuera.
    public int Count => elementos.Count;

    public override string ToString() => $"{{{string.Join(", ", elementos)}}}";
}    

// Implementamos la clase Conjunto de `string` 
class ConjuntoString : IConjunto<string>  {
    List<string> elementos;    

    public ConjuntoString() {
        elementos = new List<string>();
    }

    public ConjuntoString(List<string> elementos) {
        this.elementos = new List<string>();
        foreach (var e in elementos) {
            Agregar(e);
        }
    }

    public void Agregar(string valor) {
        if (Contiene(valor)) { return; }
        elementos.Add(valor);
    }

    public void Eliminar(string valor) {
        if (!Contiene(valor)) { return; }
        elementos.Remove(valor);
    }

    public bool Contiene(string valor) {
        foreach (var e in elementos) {
            if (e.Equals(valor)) { return true; }
        }
        return false;
    }

    public List<string> Elementos => elementos.ToList(); 
    public int Count => elementos.Count;

    public override string ToString() => $"{{{string.Join(", ", elementos)}}}";
}
