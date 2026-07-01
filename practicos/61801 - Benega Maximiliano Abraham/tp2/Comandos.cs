using System;
using System.Collections.Generic;

namespace Calculadora
{
    public static class ControladorDeOpciones
    {
        public static bool Manejar(string[] argumentos)
        {
            if (argumentos == null || argumentos.Length == 0)
                return false;  

            string[] ayuda = { "--help", "-h", "--ayuda" };
            if (argumentos.Length == 1 && Array.Exists(ayuda, a => a == argumentos[0]))
            {
                MostrarAyuda();
                return true;
            }

            string[] pruebas = { "--test", "-t", "--probar", "-p" };
            if (argumentos.Length == 1 && Array.Exists(pruebas, p => p == argumentos[0]))
            {
                EjecutarValidaciones();
                return true;
            }

            if (argumentos.Length == 2)
            {
                string textoExpresion = argumentos[0];
                string textoValor = argumentos[1];

                if (!int.TryParse(textoValor, out int valorX))
                {
                    Console.WriteLine("ERROR: El segundo argumento debe ser un número entero.");
                    return true;
                }

                try
                {
                    var arbol = ConstructorDeExpresiones.AnalizarCadena(textoExpresion);
                    int resultado = arbol.Calcular(valorX);
                    Console.WriteLine(resultado);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al evaluar: {ex.Message}");
                }
                return true;
            }

            Console.WriteLine("Argumentos no reconocidos. Use --help para ver las opciones.");
            return false;
        }

        private static void MostrarAyuda()
        {
            Console.WriteLine("CALCULADORA SIMBÓLICA");
            Console.WriteLine("Uso:");
            Console.WriteLine("  calculadora <expresión> <valorX>");
            Console.WriteLine("  calculadora --help");
            Console.WriteLine("  calculadora --test");
            Console.WriteLine("\nOpciones:");
            Console.WriteLine("  --help, -h, --ayuda    Muestra esta ayuda");
            Console.WriteLine("  --test, -t, --probar, -p  Ejecuta pruebas internas");
            Console.WriteLine("\nEjemplo:");
            Console.WriteLine("  calculadora \"2 + x * 3\" 4");
        }

        private static void EjecutarValidaciones()
        {
            // Corregido: Ahora llama a VerificadorDeCasos.EjecutarComprobaciones()
            VerificadorDeCasos.EjecutarComprobaciones();
        }
    }
}