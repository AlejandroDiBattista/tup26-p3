namespace Tup26.AlumnosApp;

class Alumnos : IEnumerable<Alumno> {

    public Alumno? BuscarPorLegajo(int legajo) =>
        Lista.FirstOrDefault(a => a.Legajo == legajo);
        
    public List<Alumno> Lista { get; set; } = new();

    public Alumno this[int index] => Lista[index];
    public int Count => Lista.Count;

    public Alumnos(IEnumerable<Alumno> alumnos) =>
        Lista = alumnos?.ToList() ?? new();

    public void Agregar(Alumno alumno) =>
        Lista.Add(alumno);

    public Alumnos ConGithub(bool tiene = true) =>
        new(Lista.Where(a => tiene == a.ConGithub));

    public Alumnos ConPractico(int numero, Estado estado) =>
        new(Lista.Where(a =>  (a.practicos[numero - 1] & estado) != 0));

    public Alumnos ConTelefono(bool tiene = true) =>
        new(Lista.Where(a => tiene == a.ConTelefono));

    public Alumnos ConFotos(bool tiene = true) =>
        new(Lista.Where(a => tiene == a.ConFoto));

    public Alumnos EnComision(string comision) =>
        new(Lista.Where(a => string.Equals(a.Comision, comision)));

    public Alumnos ParaAgregar() =>
        new(Lista.Where(a => a.GitHub == "(agregar)"));

    public IEnumerator<Alumno> GetEnumerator() => Lista.GetEnumerator(); 
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); 
}
