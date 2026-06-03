var a = new Alumno {
    Nombre   = "Alejandro",
    Apellido = "Di Battista",
    Legajo   = 62000,
};

List<Contactos> agenda = ...;

var lista = agenda
    .Where(a => a.Nombre != "Alejandro")
    .Select(a => new { 
                Apellido = a.Apellido, 
                Legajo   = a.Legajo, 
                Aprobado = a.Nota > 10 && a.Asistencia > 5
            });

legajo.Sum(a => a.Legajo);


class AlumnoReducido {
    public string Apellido { get; set; } = "";
    public int Legajo {get;set;}
    public bool Aprobado {get; set;} = false;
}

class Alumno {
    public int Legajo {get;set;}
    public string Nombre {get; set;} = "";
    public string Apellido {get;set;} = "";

    public Alumno(string nombre, string apellido, int legajo)
    {
        Nombre = nombre;
        Apellido = apellido;
        Legajo = legajo;
    }
}