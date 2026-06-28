static class Program {
    static void Main(string[] args) {
        try {
            if (Comandos.Procesar(args))
                return;

            // modo interactivo
            Console.WriteLine("\n== Evaluador de Expresiones Matemáticas ==\n");
            Console.Write("Ingrese una expresión con la variable 'x'\n(ej: (x - 1) * (x - 8/4) + 3)\n>  ");

            var input = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(input)) {
                Console.WriteLine("No se ingresó ninguna expresión. Saliendo...");
                return;
            }

            var funcion = Compilador.Parse(input);

            Console.WriteLine("\nExpresión compilada. Ingrese valores para 'x' (o 'fin' para salir).");
            while (true) {
                Console.Write("x = ");
                var linea = Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(linea) || linea.Trim() == "fin")
                    break;

                if (!int.TryParse(linea.Trim(), out int x)) {
                    Console.WriteLine($"  '{linea}' no es un entero válido, intente de nuevo.");
                    continue;
                }

                Console.WriteLine($"  = {funcion.Evaluar(x)}");
            }
        } catch (Exception e) {
            Console.Error.WriteLine($"Error: {e.Message}");
        }
    }
}
