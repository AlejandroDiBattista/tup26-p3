using System;

static class Program {
    static void Main(string[] args) {
        if (Comandos.Procesar(args)) {
            return;
        }

        Console.WriteLine("\n== Evaluación de Expresiones Matemáticas ==\n");
        Console.Write("Ingrese una expresión matemática con la variable 'x':\n> ");

        var expresion = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(expresion)) {
            Console.WriteLine("No se ingresó ninguna expresión. Saliendo...");
            return;
        }

        var funcion = Compilador.Parse(expresion);

        while (true) {
            Console.Write("x = ");
            var xTexto = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(xTexto) || xTexto.ToLower() == "fin") {
                break;
            }

            if (!int.TryParse(xTexto, out int x)) {
                Console.WriteLine("Valor de x inválido.");
                continue;
            }

            try {
                Console.WriteLine(funcion.Evaluar(x));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
