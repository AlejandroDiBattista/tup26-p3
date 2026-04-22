// -- Conjunto genérico con List<T> y IEquatable<T> --
// En esta clase usamos `List<T>` y `IEquatable<T>` para comparar elementos del conjunto por su legajo.

static class Program {
    public static void Main() {
        var a1 = new Alumno("Ana", 20);
        var a2 = new Alumno("Ani", 20);
        var b1 = new Alumno("Bob", 22);

        var clase = new Conjunto<Alumno>();
        clase.Agregar(a1);
        clase.Agregar(a2); // No se agrega porque tiene el mismo legajo que Ana
        clase.Agregar(b1);

        Console.Clear();
        Console.WriteLine("== Conjunto<Alumno> ==");
        Console.WriteLine($" Hay {clase.Count} elementos."); // 2
        Console.WriteLine($" - Contiene  Ani?: {clase.Contiene(a2)}");  // True
        Console.WriteLine($" - Contiene  Bob?: {clase.Contiene(b1)}");  // True
        Console.WriteLine($" - Contiene  Car?: {clase.Contiene(new Alumno("Car", 25))}");  // False
        Console.WriteLine($" Conjunto<Alumno>: {clase}"); // { Ana (20 años), Bob (22 años) }
    }
}

class Alumno(string Nombre, int Legajo) : IEquatable<Alumno> {

    public bool Equals(Alumno? otro) {
        if (otro is null) { return false; }
        return this.Legajo == otro.Legajo;
    }

    public override int GetHashCode() =>Legajo.GetHashCode();
    public override string ToString() => $"{Nombre} (Legajo: {Legajo})";
}

class Conjunto<T> : IEquatable<T> {
    List<T> elementos;

    public Conjunto() {
        elementos = new List<T>();
    }

    public void Agregar(T valor) {
        if (Contiene(valor)) { return; }
        elementos.Add(valor);
    }

    public void Eliminar(T valor) {
        if(!Contiene(valor)) { return; }
        elementos.Remove(valor);
    }

    public bool Contiene(T valor) {
        // List<T> ya implementa el método Contains, que a su vez utiliza el método Equals de los
        //  elementos para determinar si el valor existe en la lista.
        return elementos.Contains(valor); 
    }

    public int Count => elementos.Count;
    public override string ToString() => $"{{{string.Join(", ", elementos)}}}";
}

