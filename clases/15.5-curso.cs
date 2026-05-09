var c = new Curso("Programación 1");
c.Add(new Alumno("Alice", 20, 1008));
c.Add(new Alumno("Bob", 22, 1007));
c.Add(new Alumno("Charlie", 19, 1009));

var dobleMayor = c.Select(a => a.Edad + 2).Where(edad => edad < 25);

foreach (var alumno in c) {
    Console.WriteLine(alumno);
}

// Mapear (Map) y Select 
// Filtrar (Filter) y Where


class Alumno(string nombre, int edad, int legajo) {
    public string Nombre { get; } = nombre;
    public int Edad { get; } = edad;
    public int Legajo { get; } = legajo;

    public override string ToString() => $"✅ {Nombre,-20} ({Edad} años, legajo {Legajo})";
}

class Curso(string Nombre) : IEnumerable<Alumno> {
    public List<Alumno> Alumnos { get; } = new();

    public void Add(Alumno alumno) {
        Alumnos.Add(alumno);
    }

    public IEnumerable<Alumno> GetEnumerator() {
        foreach (Alumno alumno in Alumnos) {
            yield return alumno;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() =>  GetEnumerator();
}