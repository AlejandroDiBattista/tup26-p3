using System;

public class Comandos
{
    public static void Ejecutar(string[] args)
    {
        if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
        {
            MostrarAyuda();
            return;
        }

        if (args.Length == 1 && (args[0] == "--test" || args[0] == "-t" || args[0] == "--probar" || args[0] == "-p"))
        {
            Pruebas.EjecutarTodo();
            return;
        }

        if (args.Length == 2)
        {
            EjecutarModoDirecto(args[0], args[1]);
            return;
        }

        if (args.Length == 0)
        {
            Programa.ModoInteractivo();
            return;
        }

        Console.WriteLine("Argumentos inválidos. Usá --help para ver las opciones.");
    }

    private static void MostrarAyuda()
    {
        Console.WriteLine("Uso: calculadora [expresion valor] [--help] [--probar]");
        Console.WriteLine();
        Console.WriteLine("Opciones:");
        Console.WriteLine("  -h, --help    Muestra esta ayuda.");
        Console.WriteLine("  -t, --test    Ejecuta las pruebas automáticas.");
    }

    private static void EjecutarModoDirecto(string expresion, string valorXStr)
    {
        int x;
        if (!int.TryParse(valorXStr, out x))
        {
            Console.WriteLine("Error: Valor de x inválido.");
            return;
        }

        try
        {
            Compilador comp = new Compilador(expresion);
            Nodo ast = comp.Parsear();
            int resultado = ast.Evaluar(x);
            Console.WriteLine(resultado);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}