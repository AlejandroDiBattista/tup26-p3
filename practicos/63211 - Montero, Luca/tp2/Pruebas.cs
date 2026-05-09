using System;

public class Pruebas
{
    public static void Ejecutar()
    {
        Console.WriteLine("Ejecutando pruebas automáticas...\n");

        Probar("1 + 2 * 3", 0, 7);
        Probar("1 + 2 * x", 10, 21);
        Probar("(x - 1) * (x - 8 / 4) + 3", 10, 75);
        Probar("-(3 + 2)", 0, -5);
        Probar("10 / 2", 0, 5);

        ProbarError("(1 + 2", 0); // Paréntesis sin cerrar
        ProbarError("10 / x", 0);  // División por cero
        ProbarError("", 0);        // Entrada vacía

        Console.WriteLine("\nPruebas finalizadas.");
    }

    private static void Probar(string expresion, int x, int esperado)
    {
        try
        {
            Compilador compilador = new Compilador();
            Nodo ast = compilador.Parsear(expresion);
            int resultado = ast.Evaluar(x);
            if (resultado == esperado)
            {
                Console.WriteLine($"[OK]    '{expresion}' con x={x} == {resultado}");
            }
            else
            {
                Console.WriteLine($"[FALLO] '{expresion}' con x={x}. Esperado: {esperado}, Obtenido: {resultado}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FALLO] '{expresion}'. Excepción inesperada: {ex.Message}");
        }
    }

    private static void ProbarError(string expresion, int x)
    {
        try
        {
            Compilador compilador = new Compilador();
            Nodo ast = compilador.Parsear(expresion);
            ast.Evaluar(x); 
            Console.WriteLine($"[FALLO] '{expresion}'. Debería haber fallado pero no lo hizo.");
        }
        catch (Exception)
        {
            Console.WriteLine($"[OK]    '{expresion}' lanzó un error esperado.");
        }
    }
}