using System;

namespace Calculadora
{
    class PuntoDeEntrada
    {
        static int Main(string[] argumentos)
        {
            try
            {
                if (ControladorDeOpciones.Manejar(argumentos))
                    return 0;

                EjecutarSesionInteractiva();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fatal: {ex.Message}");
                return 1;
            }
        }

        private static void EjecutarSesionInteractiva()
        {
            Console.WriteLine("=== Calculadora interactiva ===");
            Console.WriteLine("Instrucciones: escriba una expresión con números, x, + - * / y paréntesis.");
            Console.WriteLine("Para terminar, ingrese 'fin' como expresión o como valor de x.\n");

            while (true)
            {
                Console.Write("Expresión: ");
                string? expresion = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(expresion) || expresion == "fin")
                {
                    Console.WriteLine("Saliendo...");
                    break;
                }

                ExpresionMatematica arbol;
                try
                {
                    arbol = ConstructorDeExpresiones.AnalizarCadena(expresion);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en expresión: {ex.Message}");
                    continue;
                }

                while (true)
                {
                    Console.Write("x = ");
                    string? entradaX = Console.ReadLine()?.Trim();
                    if (entradaX == "fin" || string.IsNullOrEmpty(entradaX))
                    {
                        Console.WriteLine("Volviendo a pedir expresión...\n");
                        break;
                    }

                    if (!int.TryParse(entradaX, out int valorX))
                    {
                        Console.WriteLine("Debe ingresar un número entero.");
                        continue;
                    }

                    try
                    {
                        int resultado = arbol.Calcular(valorX);
                        Console.WriteLine($"Resultado: {resultado}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al evaluar: {ex.Message}");
                    }
                }
            }
        }
    }
}