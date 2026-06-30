using System;

public class Pruebas
{
    public static void EjecutarTodo()
    {
        Console.WriteLine("Ejecutando pruebas automáticas...");
        bool todasPasaron = true;

        todasPasaron &= ProbarCaso("1 + 2 * 3", 0, 7);
        todasPasaron &= ProbarCaso("1 + 2 * x", 10, 21);
        todasPasaron &= ProbarCaso("(x - 1) * (x - 8 / 4) + 3", 10, 75);
        todasPasaron &= ProbarCaso("-(3 + 2)", 0, -5);
        todasPasaron &= ProbarCaso("10 / 2", 0, 5);

        if (todasPasaron)
        {
            Console.WriteLine("Todas las pruebas pasaron correctamente.");
        }
        else
        {
            Console.WriteLine("Algunas pruebas fallaron.");
        }
    }

    private static bool ProbarCaso(string expresion, int x, int esperado)
    {
        try
        {
            Compilador comp = new Compilador(expresion);
            Nodo ast = comp.Parsear();
            int resultado = ast.Evaluar(x);

            if (resultado == esperado)
            {
                return true;
            }
            else
            {
                Console.WriteLine("Fallo: " + expresion + " con x=" + x + ". Esperado: " + esperado + ", Obtenido: " + resultado);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inesperado en prueba '" + expresion + "': " + ex.Message);
            return false;
        }
    }
}