static class Program {
    static void Main(string[] args) {
        try {
            if (Comandos.Procesar(args))
                return;

            Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
            Console.Write("Ingrese una expresión matemática con la variable 'x': \n>  ");

            var expresion = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(expresion)) {
                Console.WriteLine("No se ingresó ninguna expresión. Saliendo...");
                return;
            }

            var funcion = Compilador.Parse(expresion);

            while (true) {
                Console.Write("x = ");
                var input = Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(input) || input == "fin")
                    break;

                try {
                    int x = int.Parse(input);
                    Console.WriteLine(funcion.Evaluar(x));
                } catch (Exception ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
