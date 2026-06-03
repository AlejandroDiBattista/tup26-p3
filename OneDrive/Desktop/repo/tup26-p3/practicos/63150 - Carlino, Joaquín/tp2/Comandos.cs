using System;

namespace CalculadoraAST
{
    static class Comandos 
    {
        public static bool Procesar(string[] args) 
        {
            switch (args) 
            {
                case ["--help"] or ["-h"] or ["--ayuda"]:
                    Console.WriteLine("""
Uso: calculadora [expresión valor] [--help] [--probar]

Este programa permite analizar y evaluar expresiones matemáticas
que pueden incluir la variable 'x'.

Si se proporciona una expresión junto con un valor, el programa
reemplaza 'x' por ese valor y muestra el resultado.

Si se ejecuta sin argumentos, inicia un modo interactivo para
ingresar una expresión y evaluarla con distintos valores de 'x'.

Expresiones válidas:
- Pueden contener números enteros, operadores binarios (+, -, *, /),
  operadores unarios (+, -), paréntesis y la variable 'x'.
- Ejemplo: (x - 1) * (x - 8/4) + 3

Opciones:
    --help, -h          Muestra esta ayuda y termina con código 0.
    --test, -t, --probar Ejecuta pruebas automáticas.
""");
                    return true;

                case ["--probar"] or ["-p"] or ["--test"] or ["-t"]:
                    Pruebas.Ejecutar();
                    return true;

                case [var expresion, var valor]:
                    try 
                    {
                        if (!int.TryParse(valor, out int x))
                        {
                            Console.WriteLine("Error: El valor de x provisto no es un entero válido.");
                            return true;
                        }

                        var funcion = Compilador.Parse(expresion);
                        Console.WriteLine(funcion.Evaluar(x));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
