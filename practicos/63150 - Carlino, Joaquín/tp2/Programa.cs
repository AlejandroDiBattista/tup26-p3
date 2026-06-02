using System;

namespace CalculadoraAST
{
    static class Program 
    {
        static void Main(string[] args) 
        {
            if (Comandos.Procesar(args)) 
            {
                return;
            }

            Console.WriteLine("\n Evaluación de expresiones matemáticas \n");
            Console.Write("Ingrese una expresión matemática con la variable 'x': \n> ");

            var expresion = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(expresion)) 
            {
                Console.WriteLine("Error: Entrada vacía. Terminando programa.");
                return;
            }

            try
            {
                var funcion = Compilador.Parse(expresion);

                while (true) 
                {
                    Console.Write("x = ");
                    var entradaX = Console.ReadLine() ?? "";

                    if (string.IsNullOrWhiteSpace(entradaX) || entradaX.Trim().ToLower() == "fin") 
                    {
                        break;
                    }

                    if (int.TryParse(entradaX, out int x))
                    {
                        try
                        {
                            Console.WriteLine($"Resultado: {funcion.Evaluar(x)}");
                        }
                        catch (DivideByZeroException ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: Valor de x inválido. Ingrese un número entero");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}