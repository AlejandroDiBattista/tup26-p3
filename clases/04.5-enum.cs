using static System.Console;

var x = Permisos.Lectura | Permisos.Escritura;
WriteLine(x);
[Flags]
enum Permisos
{
    Lectura   = 0b001,  // 1
    Escritura = 0b010,  // 2
    Ejecucion = 0b100   // 4
}
