using System;

namespace CalculadoraAST
{
    public static class Comandos
    {
        public static void MostrarAyuda()
        {
            Console.WriteLine("Uso: calculadora [expresion valor] [--help] [--probar] [--test]");
            Console.WriteLine("\nOpciones:");
            Console.WriteLine("  -h, --help    Muestra esta ayuda y termina.");
            Console.WriteLine("  -t, -p, --test, --probar  Ejecuta las pruebas automáticas.");
            Console.WriteLine("\nArgumentos posicionales:");
            Console.WriteLine("  expresion     Fórmula matemática entre comillas (ej: \"(x+2)*3\").");
            Console.WriteLine("  valor         Número entero que reemplaza a la variable 'x'.");
        }
    }
}