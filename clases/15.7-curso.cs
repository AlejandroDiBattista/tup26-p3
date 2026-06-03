
var c = new Curso("Programación 1");
c.Agregar(new Alumno("Alice", 20, 1008));
c.Agregar(new Alumno("Bob", 22, 1007));
c.Agregar(new Alumno("Charlie", 19, 1009));
foreach (var alumno in c) {
    Console.WriteLine(alumno);
}
class Alumno(string nombre, int edad, int legajo) {
    public string Nombre { get; } = nombre;
    public int Edad { get; } = edad;
    public int Legajo { get; } = legajo;
 
    public override string ToString() => $"✅ {Nombre,-20} ({Edad} años, legajo {Legajo})";
 }



    
 var c = Curso("...");
 c.Add(new Alumno(...))
 
 Mapear(c, x => x.Edad);
 c.Mapear(x => x.Edad);

 class Curso(string Nombre) : IEnumerable<Alumno> {
     private List<Alumno> Alumnos { get; } = new();
 
     public void Agregar(Alumno alumno) {
         Alumnos.Add(alumno);
     }
 
     public IEnumerable<Alumno> GetAlumnos() {
         return Alumnos;
     }
 
     public IEnumerator<Alumno> GetEnumerator() {
         return Alumnos.GetEnumerator();
     }
 
     IEnumerator IEnumerable.GetEnumerator() {
         return GetEnumerator();
     }
 }