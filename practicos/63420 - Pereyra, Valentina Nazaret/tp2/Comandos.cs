using System;

static class Comandos {
    public static bool EsAyuda(string[] args)
        => args.Length == 1 && (args[0] == "--help" || args[0] == "-h");

    public static bool EsTest(string[] args)
        => args.Length == 1 && (args[0] == "--test" || args[0] == "-t" || args[0] == "-p");

    public static void MostrarAyuda() {
        Console.WriteLine("Uso:");
        Console.WriteLine("calculadora \"expresion\" valor");
        Console.WriteLine("calculadora");
        Console.WriteLine("Opciones:");
        Console.WriteLine("  -h, --help     Muestra ayuda");
        Console.WriteLine("  -t, --test     Ejecuta pruebas");
    }
}
