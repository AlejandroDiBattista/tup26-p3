namespace TP2.Calculadora;

public static class Comandos
{
    public static void Ejecutar(string[] args)
    {
        if (args.Length == 0)
        {
            ModoInteractivo();
        }
        else if (args.Length == 2)
        {
            ModoDirecto(args[0], args[1]);
        }
        else
        {
            MostrarAyuda();
        }
    }

    private static void ModoDirecto(string expresion, string valorX)
    {
        try 
        {
            var nodo = Compilador.Parse(expresion);
            int x = int.Parse(valorX);
            Console.WriteLine(nodo.Evaluar(x));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void ModoInteractivo()
    {
        Console.Write("Ingrese la expresión matemática: ");
        string? expresion = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(expresion)) return;

        try
        {
            var nodoRaiz = Compilador.Parse(expresion);
            Console.WriteLine("Expresión aceptada. Ingrese valores para 'x' (o 'exit' para salir):");

            while (true)
            {
                Console.Write("x = ");
                string? entrada = Console.ReadLine();

                if (entrada?.ToLower() == "exit") break;

                if (int.TryParse(entrada, out int x))
                {
                    Console.WriteLine($"Resultado: {nodoRaiz.Evaluar(x)}");
                }
                else
                {
                    Console.WriteLine("Por favor, ingrese un número válido.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error de sintaxis: {ex.Message}");
        }
    }

    private static void MostrarAyuda()
    {
        Console.WriteLine("Uso de la Calculadora:");
        Console.WriteLine("  Modo Directo: dotnet run -- \"expresion\" valorX");
        Console.WriteLine("  Modo Interactivo: dotnet run");
    }
}