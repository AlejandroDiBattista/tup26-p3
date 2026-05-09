using System;

public class Comandos
{
    public static void Procesar(string[] args)
    {
        if (args.Length == 0)
        {
            ModoInteractivo();
            return;
        }

        string arg1 = args[0].ToLower();

        if (arg1 == "--help" || arg1 == "-h")
        {
            MostrarAyuda();
            Environment.Exit(0);
        }

        if (arg1 == "--test" || arg1 == "-t" || arg1 == "-p")
        {
            Pruebas.Ejecutar();
            return;
        }

        if (args.Length == 2)
        {
            ModoDirecto(args[0], args[1]);
            return;
        }

        Console.WriteLine("Error: Argumentos inválidos.");
        MostrarAyuda();
    }

    private static void MostrarAyuda()
    {
        Console.WriteLine("calculadora [expresion valor] [--help] [--test]");
        Console.WriteLine("Opciones:");
        Console.WriteLine("  --help, -h   Muestra la ayuda y termina.");
        Console.WriteLine("  --test, -t   Ejecuta pruebas automáticas.");
        Console.WriteLine("Argumentos posicionales:");
        Console.WriteLine("  expresion    Fórmula a evaluar.");
        Console.WriteLine("  valor        Valor entero con el que se reemplaza la variable x.");
    }

    private static void ModoDirecto(string expresion, string valorStr)
    {
        try
        {
            if (!int.TryParse(valorStr, out int x))
                throw new Exception("Error: Valor de x inválido.");

            Compilador compilador = new Compilador();
            Nodo ast = compilador.Parsear(expresion);
            int resultado = ast.Evaluar(x);
            Console.WriteLine(resultado);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static void ModoInteractivo()
    {
        Console.Write("Ingrese la expresión a evaluar: ");
        string expresion = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(expresion)) return;

        Compilador compilador = new Compilador();
        Nodo ast;
        try
        {
            ast = compilador.Parsear(expresion);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        while (true)
        {
            Console.Write("Ingrese valor para x (o 'fin' para salir): ");
            string entrada = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(entrada) || entrada.ToLower() == "fin")
                break;

            if (!int.TryParse(entrada, out int x))
            {
                Console.WriteLine("Error: Valor de x inválido.");
                continue;
            }

            try
            {
                int resultado = ast.Evaluar(x);
                Console.WriteLine($"Resultado: {resultado}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}