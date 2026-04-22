// -- Conjunto con array interno --
// En esta clase implementamos la clase Conjunto usando un array interno de enteros.

static class Program {
    public static void Main() {

        var a = new Conjunto();
        a.Agregar(5);
        a.Agregar(10);
        a.Agregar(5); // No se agrega porque ya existe
        a.Agregar(20);

        a[7]  = true;  // Agrega 7
        a[20] = false; // Elimina 20

        Console.Clear();
        Console.WriteLine("== Conjunto ==");
        Console.WriteLine($" Hay {a.Count} elementos."); // 2
        Console.WriteLine($" - Contiene  5?: {a.Contiene(5)}");  // True
        Console.WriteLine($" - Contiene 10?: {a.Contiene(10)}"); // True
        Console.WriteLine($" - Contiene  3?: {a.Contiene(3)}");  // False
        Console.WriteLine($" Conjunto: {a}"); // { 5, 10 }
        Console.WriteLine($" - Contiene  5?: {a[5]}"); // True
        Console.WriteLine($" - Contiene  3?: {a[3]}"); // False

        var b = new Conjunto();
        b.Agregar(10);
        b.Agregar(20);

        var c = Conjunto.Union(a, b);
        Console.WriteLine($" A: {a}");      // { 5, 10 }
        Console.WriteLine($" B: {b}");      // { 10, 20 }
        Console.WriteLine($" Unión: {c}");  // { 5, 10, 20 }

        var x = new Conjunto();
        x.Agregar(5);
        x.Agregar(7);

        var d = b & c | x;
        Console.WriteLine($" Intersección: {d}"); // { 10 }
    }
}

// Implementamos la clase Conjunto usando un array interno de enteros.
// El array tiene una capacidad inicial y puede crecer dinámicamente si se llena.
class Conjunto {
    int[] elementos;
    int count;

    public Conjunto(int capacidad = 10) {
        elementos = new int[capacidad];
        count = 0;
    }

    // Agrega un valor al conjunto. Si el valor ya existe, no se agrega. 
    // Si el array interno se llena, se duplica su tamaño.
    public void Agregar(int valor) {
        if (Contiene(valor)) { return; } // Ignora si el valor ya existe

        elementos[count++] = valor;
    }

    // Elimina un valor del conjunto. Si el valor no existe, no hace nada.
    // Para eliminar un valor, se reemplaza por el último elemento del array y se decrementa el contador.
    public void Eliminar(int valor) {
        if (!Contiene(valor)) { return; } // Ignora si el valor no existe

        for (int i = 0; i < count; i++) {
            if (elementos[i] == valor) {
                elementos[i] = elementos[count - 1];
                count--;
                return;
            }
        }
    }

    // Verifica si un valor pertenece al conjunto. Devuelve true si el valor existe, false en caso contrario.
    public bool Contiene(int valor) {
        for (int i = 0; i < count; i++) {
            if (elementos[i] == valor) {
                return true;
            }
        }
        return false;
    }

    // Para poder iterar sobre los elementos del conjunto, podemos definir una propiedad que devuelva un array con los elementos actuales del conjunto.
    public int[] Elementos {
        get {
            int[] resultado = new int[count];
            Array.Copy(elementos, resultado, count);
            return resultado;
        }
    }

    // Indexador para acceder a los elementos del conjunto. 
    // Permite usar la sintaxis a[valor] para verificar si el valor pertenece al conjunto o para agregar/eliminar el valor.
    public bool this[int key] {
        get => Contiene(key);
        set {
            if (value) {
                Agregar(key);
            } else {
                Eliminar(key);
            }
        }
    }

    public override string ToString() => "{" + string.Join(", ", elementos.Take(count)) + " }";
    public int Count => count;

    // Implementamos la función de unión entre conjuntos.

    public static Conjunto Union(Conjunto a, Conjunto b) {
        Conjunto resultado = new Conjunto();
        foreach (var elemento in a.Elementos) {
            resultado.Agregar(elemento);
        }
        foreach (var elemento in b.Elementos) {
            resultado.Agregar(elemento);
        }
        return resultado;
    }

    public static Conjunto Interseccion(Conjunto a, Conjunto b) {
        Conjunto resultado = new Conjunto();
        foreach (var elemento in a.Elementos) {
            if (b.Contiene(elemento)) {
                resultado.Agregar(elemento);
            }
        }
        return resultado;
    }

    // Para mayor comodidad, podemos definir operadores para la unión e intersección de conjuntos.
    public static Conjunto operator |(Conjunto a, Conjunto b) => Union(a, b);
    public static Conjunto operator &(Conjunto a, Conjunto b) => Interseccion(a, b);
}

