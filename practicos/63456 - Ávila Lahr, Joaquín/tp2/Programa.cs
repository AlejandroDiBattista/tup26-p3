static class Program {
    static void Main(string[] args)
    {
     try
    {
        if (Comandos.Procesar(args)) {
            return;
        }

        Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
        Console.Write("Ingrese una expresión matemática con la variable 'x' (ej: (x - 1) * (x - 8/4) + 3): \n>  ");

        
       Console.Write("Ingrese una expresión: ");
var expresion = Console.ReadLine() ?? "";

if (string.IsNullOrWhiteSpace(expresion))
{
    Console.WriteLine("No se ingresó ninguna expresión.");
    return;
}

var funcion = Compilador.Parse(expresion);

        while (true)
        {
            Console.Write("x = ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input == "fin")
                break;

            if (!int.TryParse(input, out int x))
            {
                Console.WriteLine("Valor inválido");
                continue;
            }

            Console.WriteLine(funcion.Evaluar(x));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("Error: " + e.Message);
    }
}
}