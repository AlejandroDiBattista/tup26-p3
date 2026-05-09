static class Program
{
    static void Main(string[] args)
    {
        // --test
        if (args.Length > 0 && (args[0] == "--test" || args[0] == "-t"))
        {
            Pruebas.Ejecutar();
            return;
        }

        // --help
        if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
        {
            Console.WriteLine("Uso:");
            Console.WriteLine("  calculadora \"expresion\" valor");
            Console.WriteLine("  calculadora --test");
            Console.WriteLine("  calculadora --help");
            return;
        }

        // modo directo
        if (args.Length == 2)
        {
            try
            {
                var nodo = Compilador.Parse(args[0]);

                if (!int.TryParse(args[1], out int x))
                {
                    Console.WriteLine("Valor de x inválido");
                    return;
                }

                Console.WriteLine(nodo.Evaluar(x));
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("Error: división por cero");
            }

            return;
        }

        // calculadora interactiva
        Console.WriteLine("Calculadiora interactiva. Escriba 'fin' para salir.");
        Console.Write("Ingrese expresion: ");
        var expresion = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(expresion))
            return;

        Nodo funcion;

        try
        {
            funcion = Compilador.Parse(expresion);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return;
        }

        while (true)
        {
            Console.Write("x = ");
            var entrada = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(entrada) || entrada == "fin") 
                break;

            if (!int.TryParse(entrada, out int x))
            {
                Console.WriteLine("Valor inválido");
                continue;
            }

            try
            {
                Console.WriteLine(funcion.Evaluar(x));
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("Error: división por cero");
            }
        }
    }
}