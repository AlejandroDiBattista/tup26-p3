using System;

namespace CalculadoraAST {
    public static class Pruebas {
        public static void Ejecutar() {
            Console.WriteLine("=== Iniciando Batería de Pruebas Automáticas ===");
            int exitosas = 0;
            int fallidas = 0;

            // Defino los casos de prueba: {Expresión, ValorX, ResultadoEsperado}
            var casos = new (string Exp, int X, int Esperado)[]
            {
                ("1 + 2 * 3", 0, 7),
                ("1 + 2 * x", 10, 21),
                ("(x - 1) * (x - 8 / 4) + 3", 10, 75),
                ("-(3 + 2)", 0, -5),
                ("10 / 2", 0, 5),
                ("x * x", 5, 25),
                ("(2 + 3) * 5", 0, 25)
            };

            foreach (var c in casos) {
                try {
                    Compilador comp = new Compilador(c.Exp);
                    Nodo ast = comp.Parsear();
                    int resultado = ast.Evaluar(c.X);

                    if (resultado == c.Esperado) {
                        Console.WriteLine($"[OK]  '{c.Exp}' con x={c.X} => {resultado}");
                        exitosas++;
                    } else {
                        Console.WriteLine($"[FAIL] '{c.Exp}' con x={c.X}. Esperado: {c.Esperado}, Obtuviste: {resultado}");
                        fallidas++;
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"[ERROR] En '{c.Exp}': {ex.Message}");
                    fallidas++;
                }
            }

            // Prueba de Error de Parsing (Caso especial)
            Console.WriteLine("\nVerificando detección de errores...");
            try {
                new Compilador("(1 + 2").Parsear();
                Console.WriteLine("[FAIL] '(1 + 2' no detectó el paréntesis sin cerrar.");
                fallidas++;
            } catch (Exception) {
                Console.WriteLine("[OK]  '(1 + 2' detectó correctamente el error de parsing.");
                exitosas++;
            }

            Console.WriteLine("\n------------------------------------------------");
            Console.WriteLine($"Resultado Final: {exitosas} Pasadas | {fallidas} Fallidas");
            Console.WriteLine("------------------------------------------------");
        }
    }
}
