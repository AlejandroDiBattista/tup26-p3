static class Program {
    static void Main(string[] args) {
        if (Comandos.Procesar(args)) {
            return;
        }

        Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
        Console.Write("Ingrese una expresión matemática con la variable 'x' (ej: (x - 1) * (x - 8/4) + 3): \n>  ");


        var expresion = Console.ReadLine() ?? "";
        if (string.IsNullOrWhiteSpace(expresion)) {
            Console.WriteLine("No se ingresó ninguna expresión. Saliendo...");
            return;
        }
        Nodo funcion;
        try {
            funcion = Compilador.Parse(expresion);
        } catch (FormatException ex) {
            Console.WriteLine(ex.Message);
            return;
        }

        while (true) {
            Console.Write("x = ");
            var entrada = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(entrada) ||
                entrada.Equals("fin", StringComparison.OrdinalIgnoreCase)) {
                break;
            }

            if (!int.TryParse(entrada, out var x)) {
                Console.WriteLine("Valor de x inválido.");
                continue;
            }

            try {
                Console.WriteLine(funcion.Evaluar(x));
            } catch (DivideByZeroException ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
