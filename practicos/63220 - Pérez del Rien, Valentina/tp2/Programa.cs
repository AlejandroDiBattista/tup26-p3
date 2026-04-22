using System;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var cmd = new Comandos(args);

            if (cmd.EsHelp)
            {
                MostrarHelp();
                return;
            }

            if (cmd.EsTest)
            {
                Pruebas.Ejecutar();
                return;
            }

            if (cmd.Expresion != null && cmd.ValorX.HasValue)
            {
                EjecutarDirecto(cmd.Expresion, cmd.ValorX.Value);
                return;
            }

            ModoInteractivo();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    static void EjecutarDirecto(string expr, int x)
    {
        var comp = new Compilador();
        var ast = comp.Parsear(expr);

        Console.WriteLine(ast.Evaluar(x));
    }

    static void ModoInteractivo()
    {
        Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
        Console.Write("Ingrese una expresión matemática con la variable 'x': \n> ");

        string expr = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(expr))
            throw new Exception("Entrada vacía");

        var comp = new Compilador();
        var ast = comp.Parsear(expr);

        while (true)
        {
            Console.Write("x = ");
            string input = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "fin")
                break;

            if (!int.TryParse(input, out int x))
                throw new Exception("Valor de x inválido");

            Console.WriteLine(ast.Evaluar(x));
        }
    }

    static void MostrarHelp()
    {
        Console.WriteLine("Uso:");
        Console.WriteLine("calculadora \"expresion\" valor");
        Console.WriteLine("--help | -h");
        Console.WriteLine("--test | -t");
    }
}