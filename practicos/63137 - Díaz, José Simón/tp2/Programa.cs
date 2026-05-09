static class Program {
    static void Main(string[] args) {
        if (Comandos.Procesar(args)) {
            return;
        }

        Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
        Console.WriteLine("Ingrese una expresión matemática con la variable 'x' (ej: (x - 1) * (x - 8/4) + 3):");
        Console.Write("> ");

        var expresion = Console.ReadLine() ?? "";
        if (expresion.IsWhiteSpace()) {
            Console.WriteLine("No se ingresó ninguna expresión. Saliendo...");
            return;
        }

        Nodo funcion;
        try {
            funcion = Compilador.Parse(expresion);
        } catch (FormatException ex) {
            Console.WriteLine($"Error en la expresión: {ex.Message}");
            return;
        }

        while (true) {
            Console.Write("x = ");
            var x = Console.ReadLine() ?? "";

            if (x.IsWhiteSpace() || x == "fin") {
                break;
            }

            if (!int.TryParse(x, out int valorX)) {
                Console.WriteLine("Valor inválido. Ingresá un número entero.");
                continue;
            }

            try {
                Console.WriteLine(funcion.Evaluar(valorX));
            } catch (DivideByZeroException ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
