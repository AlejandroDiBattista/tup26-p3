using static System.Console;

const int Aprobado = 10;
const int Desaprobado = 5;
const int Pendiente = 0;
Clear();
Permisos p = Permisos.Ejecucion | Permisos.Escritura;
WriteLine(p.HasFlag(Permisos.Ejecucion)); // True
WriteLine(p.HasFlag(Permisos.Escritura)); // True
WriteLine($"Permiso es {p}");   // False
enum Permisos {
    Lectura   = 0b001,
    Escritura = 0b010,
    Ejecucion = 0b100
}