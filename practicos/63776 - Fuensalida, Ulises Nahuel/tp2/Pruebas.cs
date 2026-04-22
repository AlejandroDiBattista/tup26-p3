using System;

namespace CalculadoraUTN
{
    class Pruebas
    {
        public static void EjecutarPruebas()
        {
            var casos = new (string exp, int x, int esperado)[] {
                ("1 + 2 * 3", 0, 7),
                ("1 + 2 * x", 10, 21),
                ("(x - 1) * (x - 8 / 4) + 3", 10, 75),
                ("-(3 + 2)", 0, -5),
                ("10 / 2", 0, 5)
            };

            var comp = new Compilador();
            int exitos = 0;

            Console.WriteLine("Ejecutando pruebas automáticas...");
            foreach (var p in casos)
            {
                try
                {
                    int res = comp.Parsear(p.exp).Evaluar(p.x);
                    if (res == p.esperado)
                    {
                        Console.WriteLine($"[OK] {p.exp} (x={p.x}) = {res}");
                        exitos++;
                    }
                    else
                    {
                        Console.WriteLine($"[FALLO] {p.exp} -> Esperado {p.esperado}, obtenido {res}");
                    }
                }
                catch (Exception e) { Console.WriteLine($"[ERROR] {p.exp}: {e.Message}"); }
            }
            Console.WriteLine($"\nResultado: {exitos}/{casos.Length} pruebas pasadas.");
        }
    }
}