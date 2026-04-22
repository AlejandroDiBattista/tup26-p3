// -- Conjunto con lista interna --
// En esta clase implementamos la clase Conjunto usando una lista interna de enteros.

static class Program {
    public static void Main() {
        var a = new Conjunto();
        a.Agregar(5);
        a.Agregar(10);
        a.Agregar(5); // No se agrega porque ya existe

        Console.Clear();
        Console.WriteLine("== Conjunto ==");
        Console.WriteLine($" Hay {a.Count} elementos."); // 2
        Console.WriteLine($" - Contiene  5?: {a.Contiene(5)}");  // True
        Console.WriteLine($" - Contiene 10?: {a.Contiene(10)}"); // True
        Console.WriteLine($" - Contiene  3?: {a.Contiene(3)}");  // False
        Console.WriteLine($" Conjunto: {a}"); // { 5, 10 }

        var b = new Conjunto();
        b.Agregar(10);
        b.Agregar(20);

        var c = Conjunto.Union(a, b);
        var d = b & c;

        Console.WriteLine(" == Operaciones con conjuntos ==");
        Console.WriteLine($" - A: {a}");      // { 5, 10 }
        Console.WriteLine($" - B: {b}");      // { 10, 20 }
        Console.WriteLine($" - Unión: {c}");  // { 5, 10, 20 }
        Console.WriteLine($" - Intersección: {d}"); // { 10 }
    }
}


// Implementamos la clase Conjunto usando una lista interna de enteros.
// Composición: Usarmos una clase para implmentar la funcionalidad de otra.
class Conjunto {
    List<int> elementos;

    public Conjunto() {
        elementos = new List<int>();
    }

    public void Agregar(int valor) {
        if (Contiene(valor)) { return; }
        elementos.Add(valor);
    }

    public void Eliminar(int valor) {
        if (!Contiene(valor)) { return;  }
        elementos.Remove(valor);
    }

    public bool Contiene(int valor) {
        foreach (var e in elementos) {
            if (e == valor) {
                return true;
            }
        }
        return false;
    }

    public int[] Elementos => elementos.ToArray(); // Devuelve una copia de la lista de elementos para evitar que se modifique desde afuera.

    // Funciones para operar con conjuntos 
    // Aprovechamos los metodos de la clase Conjunto para implementar la unión e intersección de conjuntos.
    public static Conjunto Union(Conjunto a, Conjunto b) {
        Conjunto resultado = new Conjunto();
        foreach (var e in a.Elementos) {
            resultado.Agregar(e);
        }
        foreach (var e in b.Elementos) {
            resultado.Agregar(e);
        }
        return resultado;
    }

    public static Conjunto Interseccion(Conjunto a, Conjunto b) {
        Conjunto resultado = new Conjunto();
        foreach (var e in a.Elementos) {
            if (b.Contiene(e)) {
                resultado.Agregar(e);
            }
        }
        return resultado;
    }

    public static Conjunto operator |(Conjunto a, Conjunto b) => Union(a, b);
    public static Conjunto operator &(Conjunto a, Conjunto b) => Interseccion(a, b);

    public override string ToString() => $"{{{string.Join(", ", elementos)}}}";
    public int Count => elementos.Count;
}

