var p = new  {
    Nombre = "María",
    Apellido = "González",
    Edad = 25
};

List<int> numeros = new() {1, 2, 3, 5};

var num = new List<int>();
num.Add(1);
num.Add(2);
num.Add(3);
num.Add(5);

List<Alumno> curso = new List<Alumno>();
curso.Add(new Alumno { Nombre = "María", Apellido = "González", Legajo = 62000, Nota = 10 });
curso.Add(new Alumno { Nombre = "Juan", Apellido = "Pérez", Legajo = 62001, Nota = 5 });

curso
    // .Where(a => a.Nota >= 8)
    .Select(a => new { Nombre = a.Nombre, Apellido = a.Apellido, Aprobado = a.Nota >= 8 })
    .Where(a => a.Aprobado)
    .ToList()
    .ForEach(a => Console.WriteLine($"- {a.Nombre} {a.Apellido}"));
class Persona {
    public string Nombre {get;set;}
    public string Apellido {get;set;}
    public int Edad {get;set;}
}

// class AlumnoEstado
// {
//     public string Nombre {get;set;}
//     public string Apellido {get;set;}
//     public bool Aprobado {get;set;}
// }

class Alumno  {
    public string Nombre {get;set;}
    public string Apellido {get;set;}
    public int Legajo {get;set;}
    public int Nota {get;set;}
}