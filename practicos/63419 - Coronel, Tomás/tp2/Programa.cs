static class Program {
    static void Main(string[] args) {
        var comandos = new Comandos(args);



        if (comandos.Ayuda) {
            Console.WriteLine("Uso:");
            Console.WriteLine("calculadora \"expresion\" valor");
            Console.WriteLine("calculadora --help");
            Console.WriteLine("calculadora --test");
            return;
        }

        if (comandos.Test) {
            Pruebas.Ejecutar();
            return;
        }

        if (comandos.ModoDirecto) {
            try {
                var nodo = Compilador.Compilar(comandos.Expresion);
                int resultado = nodo.Evaluar(comandos.Valor);
                Console.WriteLine(resultado);
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
            }
            return;
        }

        Console.WriteLine("ingrese una expresión:");
        string? expr = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(expr))
            return;

        Nodo arbol;

        try {
            arbol = Compilador.Compilar(expr);
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
            return;
        }

        while (true) {
            Console.Write("x = ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("fin", StringComparison.OrdinalIgnoreCase))
                break;

            if (!int.TryParse(input, out int valor)) {
                Console.WriteLine("Valor inválido");
                continue;
            }

            try {
                Console.WriteLine(arbol.Evaluar(valor));
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
            }

        }
    }
}
