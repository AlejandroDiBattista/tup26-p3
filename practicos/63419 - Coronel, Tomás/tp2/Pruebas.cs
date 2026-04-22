using System;
 
class Pruebas
{
    static void Probar(string expr, int x, int esperado)
    {
        var nodo = Compilador.Compilar(expr);
        int resultado = nodo.Evaluar(x);
       
        if (resultado != esperado)
        {
            Console.WriteLine($"Error: {expr} con x={x} dio {resultado}, se esperaba {esperado}");
        }
    }

    public static void Ejecutar()
    {
        try
        {
            Probar("1 + 2 * 3", 0, 7);
            Probar("1 + 2 * x", 10, 21);
            Probar("(x - 1) * (x - 8 / 4) + 3", 10, 75);
            Probar("-(3 + 2)", 0, -5);
            Probar("10 / 2", 0, 5);

            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fallo: " + ex.Message);
        }
    }
}