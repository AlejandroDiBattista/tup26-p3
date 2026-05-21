// Consola es una clase estática que representa la consola del sistema, 
// proporcionando métodos y propiedades para interactuar con ella. 
// Permite leer entradas del usuario, escribir salidas, cambiar colores, 
// y controlar el cursor, entre otras funcionalidades.

using static System.Console;

Clear();
WriteLine("¿Cuál es tu nombre? ");

string? nombre;
while((nombre = ReadLine()) is not null) {
    WriteLine($"Hola {nombre.ToUpper()}");
}
