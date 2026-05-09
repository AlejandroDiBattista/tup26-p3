using System;
using System.Collections.Generic;

try
{
    // Si el usuario pone --help o -h
    if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
    {
        MostrarAyuda();
    }
    // Si quiere correr las pruebas
    else if (args.Length > 0 && (args[0] == "--test" || args[0] == "-t" || args[0] == "--probar"))
    {
        Pruebas.Ejecutar();
    }
    // Si pone la formula y el valor de x directo
    else if (args.Length == 2)
    {
        string formula = args[0];
        if (int.TryParse(args[1], out int valorX))
        {
            EvaluarDirecto(formula, valorX);
        }
        else
        {
            Console.WriteLine("Error: El valor de x tiene que ser un numero entero.");
        }
    }
    // Si no pone nada, vamos al modo interactivo
    else if (args.Length == 0)
    {
        ModoInteractivo();
    }
    else
    {
        Console.WriteLine("Argumentos invalidos. Usa --help para ver como se usa.");
    }
}
catch (Exception ex)
{
    Console.WriteLine("HUBO UN ERROR: " + ex.Message);
}

void EvaluarDirecto(string formula, int x)
{
    // Uso Compilador.Parse que es el nombre que le gusta a las pruebas
    Nodo ast = Compilador.Parse(formula);
    int resultado = ast.Evaluar(x);
    Console.WriteLine(resultado);
}

void ModoInteractivo()
{
    Console.WriteLine("--- MODO INTERACTIVO ---");
    Console.Write("Escribi la formula con x: ");
    string formula = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(formula))
    {
        return;
    }

    // Compilamos la formula una sola vez
    Nodo ast = Compilador.Parse(formula);

    while (true)
    {
        Console.Write("Dame el valor de x (o escribe 'fin' para salir): ");
        string entrada = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(entrada) || entrada.ToLower() == "fin")
        {
            break;
        }

        try
        {
            if (int.TryParse(entrada, out int x))
            {
                int resultado = ast.Evaluar(x);
                Console.WriteLine("Resultado: " + resultado);
            }
            else
            {
                Console.WriteLine("Eso no parece un numero, intenta de nuevo.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al evaluar: " + ex.Message);
        }
    }
}

void MostrarAyuda()
{
    Console.WriteLine("Calculadora de Octavio");
    Console.WriteLine("Uso:");
    Console.WriteLine("  dotnet run -- \"expresion\" valor_x   (Modo directo)");
    Console.WriteLine("  dotnet run --                      (Modo interactivo)");
    Console.WriteLine("  dotnet run -- --test               (Ejecutar pruebas)");
    Console.WriteLine("Opciones:");
    Console.WriteLine("  --help, -h    Muestra esta ayuda.");
    Environment.Exit(0);
}
