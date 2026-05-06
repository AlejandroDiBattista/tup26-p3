using System;

public static class Comandos
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
            return;
        }

        if (arg1 == "--test" || arg1 == "-t" || arg1 == "-p" || arg1 == "--probar")
        {
            Pruebas.Ejecutar();
            return;
        }

        if (args.Length == 2)
        {
            ModoDirecto(args[0], args[1]);
            return;
        }

        Console.WriteLine("Error: Cantidad de argumentos inválida.");
        MostrarAyuda();
        Environment.Exit(1);
    }

    private static void MostrarAyuda()
    {
        Console.WriteLine("Uso: calculadora [expresion valor] [--help] [--probar]");
        Console.WriteLine("\nOpciones:");
        Console.WriteLine("  --help, -h       Muestra la ayuda y termina.");
        Console.WriteLine("  --test, -t, -p   Ejecuta pruebas automáticas.");
        Console.WriteLine("\nArgumentos posicionales:");
        Console.WriteLine("  expresion        Fórmula a evaluar (soporta variable x).");
        Console.WriteLine("  valor            Valor entero para reemplazar x.");
        Environment.Exit(0);
    }

    private static void ModoDirecto(string expresion, string valorX)
    {
        try
        {
            if (!int.TryParse(valorX, out int x))
                throw new Exception("Error: Valor de x inválido.");

            Nodo ast = Compilador.Parsear(expresion);
            Console.WriteLine(ast.Evaluar(x));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Environment.Exit(1);
        }
    }

    private static void ModoInteractivo()
    {
        Console.Write("Ingrese una expresión con la variable x: ");
        string expresion = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(expresion)) return;

        try
        {
            Nodo ast = Compilador.Parsear(expresion); // Compila una sola vez

            while (true)
            {
                Console.Write("Ingrese valor para x (o 'fin' para salir): ");
                string entrada = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(entrada) || entrada.ToLower() == "fin")
                    break;

                if (int.TryParse(entrada, out int x))
                {
                    try
                    {
                        Console.WriteLine($"Resultado: {ast.Evaluar(x)}");
                    }
                    catch (Exception evalEx)
                    {
                        Console.WriteLine(evalEx.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Error: Valor de x inválido.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}