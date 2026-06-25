using System;

namespace CalculadoraUTN {
    class Comandos {
        public static void ModoDirecto(string exp, string valStr) {
            try {
                if (!int.TryParse(valStr, out int x))
                    throw new Exception("Error: El valor de x debe ser un número entero.");

                var compilador = new Compilador();
                var ast = compilador.Parsear(exp);
                Console.WriteLine(ast.Evaluar(x));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public static void ModoInteractivo() {
            Console.WriteLine("--- Calculadora Interactiva (Escriba 'fin' o deje vacío para salir) ---");
            Console.Write("Ingrese expresión (ej: 1 + 2 * x): ");
            string exp = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(exp) || exp.ToLower() == "fin") return;

            try {
                var compilador = new Compilador();
                var ast = compilador.Parsear(exp);
                Console.WriteLine("Expresión compilada con éxito.");

                while (true) {
                    Console.Write("Valor de x: ");
                    string input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "fin") break;

                    if (int.TryParse(input, out int x)) {
                        try { Console.WriteLine($"Resultado: {ast.Evaluar(x)}"); } catch (DivideByZeroException) { Console.WriteLine("Error: División por cero."); }
                    } else {
                        Console.WriteLine("Error: Ingrese un valor entero válido para x.");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error de sintaxis: {ex.Message}");
            }
        }

        public static void MostrarAyuda() {
            Console.WriteLine("Uso: calculadora [expresion valor] [--help] [--test]");
            Console.WriteLine("\nOpciones:");
            Console.WriteLine("  expresion valor   Evalúa la fórmula reemplazando x.");
            Console.WriteLine("  --help, -h        Muestra esta ayuda.");
            Console.WriteLine("  --test, -t        Ejecuta las pruebas automáticas.");
        }
    }
}

