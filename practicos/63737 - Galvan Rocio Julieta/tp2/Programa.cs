using Calculadora;

static class Program
{
    static void Main(string[] args)
    {
        try
        {
            if (Comandos.Procesar(args))
            {
                return;
            }

            Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
            Console.Write("Ingrese una expresión matemática con la variable 'x' (ej: (x - 1) * (x - 8/4) + 3): \n> ");

            var expresion = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(expresion))
            {
                Console.WriteLine("Error: entrada vacía.");
                return;
            }

            var funcion = Compilador.Parse(expresion);

            while (true)
            {
                Console.Write("x = ");
                var textoX = Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(textoX) || textoX == "fin")
                {
                    break;
                }

                if (!int.TryParse(textoX, out int x))
                {
                    Console.WriteLine("Error: valor de x inválido.");
                    continue;
                }

                Console.WriteLine(funcion.Evaluar(x));
            }
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error de parsing: {ex.Message}");
        }
        catch (DivideByZeroException ex)
        {
            Console.WriteLine($"Error de evaluación: {ex.Message}");
        }
    }
}