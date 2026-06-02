// Pruebas.cs
using System;

public static class Pruebas {
    public static void Ejecutar() {
        var compilador = new Compilador();
        var casos = new (string exp, int x, int esperado)[] {
            ("1 + 2 * 3", 0, 7),
            ("1 + 2 * x", 10, 21),
            ("(x - 1) * (x - 8 / 4) + 3", 10, 75),
            ("-(3 + 2)", 0, -5),
            ("10 / 2", 0, 5)
        };

        Console.WriteLine("Ejecutando pruebas...");
        int pasadas = 0;

        foreach (var c in casos) {
            try {
                int result = compilador.Parsear(c.exp).Evaluar(c.x);
                if (result == c.esperado) pasadas++;
                else Console.WriteLine($"Falló: {c.exp} con x={c.x}. Esperado: {c.esperado}, Obtenido: {result}");
            } catch (Exception e) {
                Console.WriteLine($"Error en {c.exp}: {e.Message}");
            }
        }

        Console.WriteLine($"{pasadas}/{casos.Length} pruebas pasaron correctamente.");
    }
}
