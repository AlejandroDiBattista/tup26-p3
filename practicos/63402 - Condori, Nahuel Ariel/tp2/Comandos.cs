// Comandos.cs
using System;

public static class Comandos {
    public static void Procesar(string[] args) {
        if (args.Length == 0) {
            Programa.ModoInteractivo();
            return;
        }

        string arg1 = args[0].ToLower();

        if (arg1 == "--help" || arg1 == "-h") {
            MostrarAyuda();
            Environment.Exit(0);
        }

        if (arg1 == "--test" || arg1 == "-t" || arg1 == "--probar" || arg1 == "-p") {
            Pruebas.Ejecutar();
            Environment.Exit(0);
        }

        if (args.Length >= 2) {
            string expresion = args[0];
            if (int.TryParse(args[1], out int x)) {
                Programa.ModoDirecto(expresion, x);
            } else {
                Console.WriteLine("Error: El valor de 'x' provisto es inválido. Debe ser un entero.");
                Environment.Exit(1);
            }
            return;
        }

        Console.WriteLine("Error: Argumentos inválidos.");
        MostrarAyuda();
        Environment.Exit(1);
    }

    private static void MostrarAyuda() {
        Console.WriteLine("Uso: calculadora [expresion valor] [--help] [--test]");
        Console.WriteLine();
        Console.WriteLine("Opciones:");
        Console.WriteLine("  --help, -h      Muestra esta ayuda y termina.");
        Console.WriteLine("  --test, -t      Ejecuta pruebas automáticas (--probar y -p también son válidos).");
        Console.WriteLine();
        Console.WriteLine("Ejemplos:");
        Console.WriteLine("  Modo directo:      dotnet run -- \"(x - 1) * 2\" 10");
        Console.WriteLine("  Modo interactivo:  dotnet run");
    }
}