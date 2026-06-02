using System;

namespace CalculadoraAST {
    class Program {
        static void Main(string[] args) {
            // Caso 1: Ayuda
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h")) {
                Comandos.MostrarAyuda();
                return;
            }

            // Caso 2: Pruebas Automáticas
            if (args.Length > 0 && (args[0] == "--test" || args[0] == "-t" || args[0] == "-p")) {
                // Nota: Pruebas.Ejecutar() lo creo en el Paso 5
                Console.WriteLine("Ejecutando pruebas automáticas...");
                Pruebas.Ejecutar();
                return;
            }

            // Caso 3: Modo Directo (Expresión + Valor de X)
            if (args.Length == 2) {
                Comandos.EjecutarDirecto(args[0], args[1]);
            }
            // Caso 4: Modo Interactivo (Sin argumentos)
            else if (args.Length == 0) {
                EjecutarModoInteractivo();
            } else {
                Console.WriteLine("Argumentos no válidos. Use --help para ver las opciones.");
            }
        }

        static void EjecutarModoInteractivo() {
            Console.WriteLine("--- Modo Interactivo ---");
            Console.Write("Ingrese la expresión matemática (con x): ");
            string entrada = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(entrada) || entrada.ToLower() == "fin") return;

            try {
                Compilador compilador = new Compilador(entrada);
                Nodo ast = compilador.Parsear(); // Compila una sola vez

                while (true) {
                    Console.Write("Ingrese valor de x (o 'fin' para salir): ");
                    string inputX = Console.ReadLine() ?? "";

                    if (string.IsNullOrWhiteSpace(inputX) || inputX.ToLower() == "fin") break;

                    if (int.TryParse(inputX, out int x)) {
                        Console.WriteLine($"Resultado: {ast.Evaluar(x)}");
                    } else {
                        Console.WriteLine("Error: El valor de x debe ser un número entero.");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error de compilación: {ex.Message}");
            }
        }
    }
}
