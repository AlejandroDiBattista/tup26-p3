using System;

namespace CalculadoraAST
{
    public static class Comandos
    {
        public static void MostrarAyuda()
        {
            Console.WriteLine("Uso: calculadora [expresion valor] [--help] [--test]");
            Console.WriteLine();
            Console.WriteLine("Argumentos:");
            Console.WriteLine("  expresion    Fórmula matemática a evaluar (ej: \"(x+2)*3\")");
            Console.WriteLine("  valor        Número entero que reemplaza a 'x'");
            Console.WriteLine();
            Console.WriteLine("Opciones:");
            Console.WriteLine("  -h, --help   Muestra esta ayuda y termina.");
            Console.WriteLine("  -t, --test   Ejecuta las pruebas automáticas.");
        }

        public static void EjecutarDirecto(string expresion, string valorX)
        {
            try
            {
                if (!int.TryParse(valorX, out int x))
                {
                    throw new Exception("Valor de x inválido.");
                }

                Compilador compilador = new Compilador(expresion);
                Nodo ast = compilador.Parsear();
                int resultado = ast.Evaluar(x);
                
                Console.WriteLine(resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}