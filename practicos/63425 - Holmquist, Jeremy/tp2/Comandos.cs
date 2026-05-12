using System;


public class Comandos
{
    public static void ProcesarArgumentos(string[] args)
    {
        if (args.Length == 0)
        {
            ModoInteractivo();
            return;
        }

        
        if (args[0] == "--help" || args[0] == "-h")
        {
            MostrarAyuda();
            Environment.Exit(0);
        }

        if (args[0] == "--test" || args[0] == "-t")
        {
            Pruebas.EjecutarPruebas();
            Environment.Exit(0);
        }

        if (args.Length >= 2)
        {
            string expresion = args[0];
            string valorStr = args[1];

            if (!int.TryParse(valorStr, out int valor))
            {
                Console.Error.WriteLine($"Error: valor '{valorStr}' no es un entero válido");
                Environment.Exit(1);
            }

            EvaluarDirecto(expresion, valor);
            return;
        }

        Console.Error.WriteLine("Error: argumentos inválidos");
        MostrarAyuda();
        Environment.Exit(1);
    }

    private static void EvaluarDirecto(string expresion, int valor)
    {
        try
        {
            var compilador = new Compilador(expresion);
            Nodo ast = compilador.Parsear();
            int resultado = ast.Evaluar(valor);
            Console.WriteLine(resultado);
        }
        catch (DivideByZeroException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error de parsing: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void ModoInteractivo()
    {
        Console.WriteLine("=== Calculadora Interactiva ===");
        Console.WriteLine("Ingrese una expresión con la variable x:");
        string expresionStr = Console.ReadLine();

        if (string.IsNullOrEmpty(expresionStr))
        {
            Console.WriteLine("Expresión vacía. Saliendo.");
            return;
        }

        Nodo ast;
        try
        {
            var compilador = new Compilador(expresionStr);
            ast = compilador.Parsear();
            Console.WriteLine($"✓ Expresión compilada correctamente");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error de parsing: {ex.Message}");
            return;
        }

        Console.WriteLine("\nIngrese valores para x (escriba 'fin' para terminar):");

        while (true)
        {
            Console.Write("> ");
            string entrada = Console.ReadLine();

            if (string.IsNullOrEmpty(entrada) || entrada.ToLower() == "fin")
            {
                Console.WriteLine("Hasta luego.");
                break;
            }

            if (!int.TryParse(entrada, out int valor))
            {
                Console.Error.WriteLine($"Error: '{entrada}' no es un entero válido");
                continue;
            }

            try
            {
                int resultado = ast.Evaluar(valor);
                Console.WriteLine($"Resultado: {resultado}");
            }
            catch (DivideByZeroException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error en evaluación: {ex.Message}");
            }
        }
    }

    private static void MostrarAyuda()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════╗");
        Console.WriteLine("║  Calculadora - Evaluador de Expresiones Aritméticas ║");
        Console.WriteLine("╚════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Uso:");
        Console.WriteLine("  calculadora [expresion valor]");
        Console.WriteLine("  calculadora --help");
        Console.WriteLine("  calculadora --test");
        Console.WriteLine();
        Console.WriteLine("Opciones:");
        Console.WriteLine("  --help, -h    Muestra esta ayuda");
        Console.WriteLine("  --test, -t    Ejecuta pruebas automáticas");
        Console.WriteLine();
        Console.WriteLine("Modo directo:");
        Console.WriteLine("  calculadora \"1 + 2 * 3\" 0");
        Console.WriteLine("  Salida: 7");
        Console.WriteLine();
        Console.WriteLine("Modo interactivo:");
        Console.WriteLine("  calculadora");
        Console.WriteLine("  (Pide expresión, luego valores de x repetidamente)");
        Console.WriteLine();
        Console.WriteLine("Expresiones soportadas:");
        Console.WriteLine("  - Números enteros: 0, 15, 123");
        Console.WriteLine("  - Variable: x, X");
        Console.WriteLine("  - Operadores: +, -, *, /");
        Console.WriteLine("  - Operadores unarios: +, -");
        Console.WriteLine("  - Paréntesis: ( )");
        Console.WriteLine();
        Console.WriteLine("Precedencia (de mayor a menor):");
        Console.WriteLine("  1. Paréntesis ( )");
        Console.WriteLine("  2. Operadores unarios (+, -)");
        Console.WriteLine("  3. Multiplicación y división (*, /)");
        Console.WriteLine("  4. Suma y resta (+, -)");
    }
}