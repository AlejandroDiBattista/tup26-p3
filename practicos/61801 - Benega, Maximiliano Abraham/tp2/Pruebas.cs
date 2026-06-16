using System;

namespace Calculadora
{
    internal static class VerificadorDeCasos
    {
        public static void EjecutarComprobaciones()
        {
            // Casos de prueba: (expresión, x, esperado)
            var pruebas = new[]
            {
                ("1 + 2 * 3", 0, 7),
                ("1 + 2 * x", 10, 21),
                ("(x - 1) * (x - 8 / 4) + 3", 10, 75),
                ("-(3 + 2)", 0, -5),
                ("10 / 2", 0, 5),
                ("(1 + 2", 0, 0)   // caso inválido, espera excepción
            };

            int contadorExitos = 0;
            int total = pruebas.Length;

            foreach (var (expr, x, esperado) in pruebas)
            {
                try
                {
                    if (expr == "(1 + 2")  // caso especial que debe lanzar error
                    {
                        ConstructorDeExpresiones.AnalizarCadena(expr).Calcular(x);
                        Console.WriteLine($"[FALLO] La expresión '{expr}' debió fallar pero no lo hizo.");
                    }
                    else
                    {
                        int resultado = ConstructorDeExpresiones.AnalizarCadena(expr).Calcular(x);
                        if (resultado == esperado)
                        {
                            Console.WriteLine($"[PASÓ] {expr} (x={x}) = {resultado}");
                            contadorExitos++;
                        }
                        else
                        {
                            Console.WriteLine($"[FALLO] {expr} (x={x}) -> se esperaba {esperado}, se obtuvo {resultado}");
                        }
                    }
                }
                catch (Exception)
                {
                    if (expr == "(1 + 2")
                    {
                        Console.WriteLine($"[PASÓ] {expr} → correctamente rechazada");
                        contadorExitos++;
                    }
                    else
                    {
                        Console.WriteLine($"[EXCEPCIÓN] {expr} lanzó una excepción inesperada.");
                    }
                }
            }

            Console.WriteLine($"\n--- Resumen: {contadorExitos}/{total} pruebas exitosas ---");
            if (contadorExitos == total)
                Console.WriteLine("✓ Todas las pruebas pasaron correctamente.");
            else
                Console.WriteLine("✗ Hay pruebas fallidas, revise su implementación.");
        }
    }
}