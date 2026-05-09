
namespace calculadora;

// ─── Suite de pruebas automáticas ────────────────────────────────────────────

static class Pruebas
{
    record Caso(string Expresion, int X, int Esperado, string? ErrorEsperado = null);

    private static readonly Caso[] Casos =
    [
        // Casos del enunciado
        new("1 + 2 * 3",                 0,  7),
        new("1 + 2 * x",                10, 21),
        new("(x - 1) * (x - 8 / 4) + 3", 10, 75),
        new("-(3 + 2)",                  0, -5),
        new("10 / 2",                    0,  5),

        // Precedencia
        new("2 + 3 * 4",                 0, 14),
        new("(2 + 3) * 4",               0, 20),
        new("10 - 2 - 3",                0,  5),
        new("10 / 2 / 5",                0,  1),

        // Unarios
        new("+5",                         0,  5),
        new("--5",                        0,  5),
        new("-x",                         3, -3),
        new("-(x + 1)",                   4, -5),

        // Variable
        new("x",                         42, 42),
        new("X",                          7,  7),
        new("x * x",                      6, 36),

        // Paréntesis anidados
        new("((x + 1))",                  9, 10),
        new("(x - 1) * (x - 8 / 4) + 3", 5, 15),

        // Cero y negativos
        new("0",                          0,  0),
        new("-0",                         0,  0),
    ];

    // Casos que deben lanzar excepción
    record CasoError(string Expresion, int X, string Descripcion);

    private static readonly CasoError[] CasosError =
    [
        new("(1 + 2",  0, "paréntesis sin cerrar"),
        new("",        0, "expresión vacía"),
        new("1 +",     0, "token faltante al final"),
        new("1 + 2 3", 0, "token inesperado"),
        new("10 / 0",  0, "división por cero"),
        new("10 / (x - x)", 1, "división por cero (x=1, x-x=0)"),
    ];

    // ─── Ejecutar ─────────────────────────────────────────────────────────────

    public static void Ejecutar()
    {
        var compilador = new Compilador();
        int pasadas = 0, falladas = 0;

        Console.WriteLine("── Pruebas de evaluación ──────────────────────────────");

        foreach (var c in Casos)
        {
            try
            {
                Nodo ast = compilador.Compilar(c.Expresion);
                int resultado = ast.Evaluar(c.X);

                if (resultado == c.Esperado)
                {
                    Console.WriteLine($"  ✓  \"{c.Expresion}\" con x={c.X} → {resultado}");
                    pasadas++;
                }
                else
                {
                    Console.WriteLine($"  ✗  \"{c.Expresion}\" con x={c.X} → {resultado} (esperado {c.Esperado})");
                    falladas++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗  \"{c.Expresion}\" con x={c.X} → EXCEPCIÓN: {ex.Message} (esperado {c.Esperado})");
                falladas++;
            }
        }

        Console.WriteLine();
        Console.WriteLine("── Pruebas de errores ─────────────────────────────────");

        foreach (var c in CasosError)
        {
            try
            {
                Nodo ast = compilador.Compilar(c.Expresion);
                int resultado = ast.Evaluar(c.X);
                Console.WriteLine($"  ✗  [{c.Descripcion}] no generó error (resultado={resultado})");
                falladas++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✓  [{c.Descripcion}] → {ex.Message}");
                pasadas++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"── Resultado: {pasadas} pasadas, {falladas} falladas ──────────────────");

        if (falladas > 0)
            Environment.Exit(1);
    }
}