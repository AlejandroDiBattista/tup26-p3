// Programa.cs
using System;

class Programa
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            ModoInteractivo();
        }
        else if (Comandos.EsOpcion(args[0], "-h", "--help"))
        {
            Comandos.MostrarAyuda();
        }
        else if (Comandos.EsOpcion(args[0], "-t", "--test") || args[0] == "-p")
        {
            Pruebas.Ejecutar();
        }
        else if (args.Length == 2)
        {
            ModoDirecto(args[0], args[1]);
        }
        else
        {
            Console.WriteLine("Uso incorrecto. Use --help para más información.");
        }
    }

    static void ModoDirecto(string exp, string valX)
    {
        try {
            var compilador = new Compilador();
            var ast = compilador.Parsear(exp);
            if (!int.TryParse(valX, out int x)) throw new Exception("Error: Valor de x inválido.");
            Console.WriteLine(ast.Evaluar(x));
        } catch (Exception e) {
            Console.WriteLine(e.Message);
        }
    }

    static void ModoInteractivo()
    {
        Console.Write("Ingrese expresión: ");
        string exp = Console.ReadLine();
        if (string.IsNullOrEmpty(exp) || exp.ToLower() == "fin") return;

        try {
            var compilador = new Compilador();
            var ast = compilador.Parsear(exp);

            while (true)
            {
                Console.Write("Ingrese valor de x: ");
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) || input.ToLower() == "fin") break;
                
                if (int.TryParse(input, out int x)) Console.WriteLine($"Resultado: {ast.Evaluar(x)}");
                else Console.WriteLine("Error: Valor de x inválido.");
            }
        } catch (Exception e) {
            Console.WriteLine(e.Message);
        }
    }
}