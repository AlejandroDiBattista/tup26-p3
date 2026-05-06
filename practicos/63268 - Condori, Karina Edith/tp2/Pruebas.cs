using System;

public static class Pruebas
{
    public static void Ejecutar()
    {
        int pasadas = 0;
        int totales = 0;

        Probar("1 + 2 * 3", 0, 7, ref pasadas, ref totales);
        Probar("1 + 2 * x", 10, 21, ref pasadas, ref totales);
        Probar("(x - 1) * (x - 8 / 4) + 3", 10, 75, ref pasadas, ref totales);
        Probar("-(3 + 2)", 0, -5, ref pasadas, ref totales);
        Probar("10 / 2", 0, 5, ref pasadas, ref totales);

        // Prueba de error de parsing (Paréntesis sin cerrar)
        totales++;
        try
        {
            Compilador.Parsear("(1 + 2");
            Console.WriteLine("Fallo: Esperaba error en '(1 + 2' pero pasó.");
        }
        catch
        {
            pasadas++;
            Console.WriteLine("Paso: '(1 + 2' lanzó error correctamente.");
        }

        Console.WriteLine($"\nPruebas finalizadas: {pasadas}/{totales} exitosas.");
        Environment.Exit(pasadas == totales ? 0 : 1);
    }

    private static void Probar(string expresion, int x, int esperado, ref int pasadas, ref int totales)
    {
        totales++;
        try
        {
            Nodo ast = Compilador.Parsear(expresion);
            int resultado = ast.Evaluar(x);
            if (resultado == esperado)
            {
                pasadas++;
                Console.WriteLine($"Paso: '{expresion}' (x={x}) == {esperado}");
            }
            else
            {
                Console.WriteLine($"Fallo: '{expresion}' (x={x}) dio {resultado}, esperado {esperado}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fallo: '{expresion}' lanzó excepción: {ex.Message}");
        }
    }
}