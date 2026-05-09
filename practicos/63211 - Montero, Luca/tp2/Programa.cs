using System;

static class Program
{
    static void Main(string[] args)
    {
        if (Comandos.Procesar(args))
        {
            return;
        }

        Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
        Console.Write("Ingrese una expresión matemática con la variable 'x' (ej: (x - 1) * (x - 8/4) + 3): \n>  ");

        var expresion = Console.ReadLine() ?? "";
        if (string.IsNullOrWhiteSpace(expresion))
        {
            Console.WriteLine("No se ingresó ninguna expresión. Saliendo...");
            return;
        }

        try
        {
            var funcion = Compilador.Parse(expresion);

            while (true)
            {
                Console.Write("x = ");
                var x = Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(x) || x.ToLower() == "fin")
                {
                    break;
                }

                if (int.TryParse(x, out int valorX))
                {
                    Console.WriteLine(funcion.Evaluar(valorX));
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