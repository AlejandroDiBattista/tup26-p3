using static System.Console;

Clear();


var d = Dias.Miercoles;
WriteLine(d);
WriteLine((int)d);
WriteLine("Permisos:");
var p = Permisos.Leer | Permisos.Escribir;
WriteLine(p);
WriteLine((int)p);
WriteLine("¿Tiene permiso de ejecutar? " + p.HasFlag(Permisos.Ejecutar));
WriteLine("¿Tiene permiso de escribir? " + p.HasFlag(Permisos.Escribir));

enum Dias {
    Lunes=0, 
    Martes, 
    Miercoles, 
    Jueves, 
    Viernes, 
    Sabado, 
    Domingo
};

[Flags]
enum Permisos {
    Ninguno  = 0b0000,
    Leer     = 0b0001,
    Escribir = 0b0010,
    Ejecutar = 0b0100,
    Eliminar = 0b1000,
    Todos = Leer | Escribir | Ejecutar | Eliminar
}