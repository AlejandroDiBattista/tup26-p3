using System;

static class Comandos
{
    public static bool EsAyuda(string[] args)
        => args.Length == 1 && (args[0] == "--help" || args[0] == "-h");

    public static bool EsTest(string[] args)
        => args.Length == 1 && (args[0] == "--test" || args[0] == "--probar" || args[0] == "-t" || args[0] == "-p");

    public static void MostrarAyuda()
    {
        Console.WriteLine("calculadora [expresion valor]");
        Console.WriteLine("--help  -h");
        Console.WriteLine("--test  -t");
    }
}
