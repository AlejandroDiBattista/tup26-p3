using System;

namespace CalculadoraAST
{
    public static class Pruebas
    {
        public static void Ejecutar()
        {
            Console.WriteLine("Ejecutando pruebas automáticas...\n");

            var casos = new (string Exp, int X, int Esperado)[]
            {
                ("1 + 2 * 3", 0, 7),
                ("1 + 2 * x", 10, 21),
                ("(x - 1) * (x - 8 / 4) + 3", 10, 75),
                ("-(3 + 2)", 0, -5),
                ("10 / 2", 0, 5)
            };

            bool todasOk = true;

            foreach (var caso in casos)
            {
                try
                {
                    var comp = new Compilador(caso.Exp);
                    var ast = comp.Parsear();
                    int res = ast.Evaluar(caso.X);

                    if (res == caso.Esperado)
                    {
                        Console.WriteLine($"[OK] \"{caso.Exp}\" con x={caso.X} => {res}");
                    }
                    else
                    {
                        Console.WriteLine($"[FAIL] \"{caso.Exp}\" con x={caso.X} => Esperado: {caso.Esperado}, Obtenido: {res}");
                        todasOk = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FAIL] \"{caso.Exp}\" lanzó error: {ex.Message}");
                    todasOk = false;
                }
            }

            // Caso de error esperado (Paréntesis sin cerrar)
            try
            {
                new Compilador("(1 + 2").Parsear();
                Console.WriteLine("[FAIL] \"(1 + 2\" debería haber fallado.");
                todasOk = false;
            }
            catch
            {
                Console.WriteLine("[OK] \"(1 + 2\" falló correctamente (Error de parsing esperado).");
            }

            Console.WriteLine(todasOk ? "\n¡Todas las pruebas pasaron con éxito!" : "\nAlgunas pruebas fallaron.");
        }
    }
}