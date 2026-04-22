using System;

public class Pruebas
{
    public static void Ejecutar()
    {
        int pasadas = 0;
        int falladas = 0;

        // Casos de éxito
        Probar("1 + 2 * 3", 0, 7, ref pasadas, ref falladas);
        Probar("1 + 2 * x", 10, 21, ref pasadas, ref falladas);
        Probar("(x - 1) * (x - 8 / 4) + 3", 10, 75, ref pasadas, ref falladas);
        Probar("-(3 + 2)", 0, -5, ref pasadas, ref falladas);
        Probar("10 / 2", 0, 5, ref pasadas, ref falladas);

        // Caso de error esperado
        try
        {
            var comp = new Compilador();
            comp.Parsear("(1 + 2");
            Console.WriteLine("[FALLO] (1 + 2  -> Debería dar error de parsing.");
            falladas++;
        }
        catch (Exception)
        {
            Console.WriteLine("[OK] (1 + 2  -> Error de parsing (Esperado)");
            pasadas++;
        }

        Console.WriteLine($"\nPruebas: {pasadas} pasadas, {falladas} falladas.");
        
        if (falladas > 0)
        {
            Environment.Exit(1);
        }
    }

    private static void Probar(string expresion, int x, int esperado, ref int pasadas, ref int falladas)
    {
        try
        {
            var comp = new Compilador();
            var ast = comp.Parsear(expresion);
            int resultado = ast.Evaluar(x);

            if (resultado == esperado)
            {
                Console.WriteLine($"[OK] \"{expresion}\" con x={x} -> {resultado}");
                pasadas++;
            }
            else
            {
                Console.WriteLine($"[FALLO] \"{expresion}\" con x={x} -> {resultado} (Esperaba: {esperado})");
                falladas++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FALLO] \"{expresion}\" -> Excepción: {ex.Message}");
            falladas++;
        }
    }
}static class Program {
    static void Main(string[] args) {
        if (Comandos.Procesar(args)) {
            return;
        }

        Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
        Console.Write("Ingrese una expresión matemática con la variable 'x' (ej: (x - 1) * (x - 8/4) + 3): \n>  ");

        
        var expresion = Console.ReadLine() ?? "";
        if(expresion.IsWhiteSpace()) {
            Console.WriteLine("No se ingresó ninguna expresión. Saliendo...");
            return;
        }
        var funcion = Compilador.Parse(expresion);

        while (true) {
            Console.Write("x = ");
            var x = Console.ReadLine() ?? "";

            if (x.IsWhiteSpace() || x == "fin") {
                break;
            }

            Console.WriteLine(funcion.Evaluar(int.Parse(x)));
        }
    }
}
