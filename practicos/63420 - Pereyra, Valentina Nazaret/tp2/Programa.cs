using System;

class Programa {
    static void Main(string[] args) {
        try {
            if (Comandos.EsAyuda(args)) {
                Comandos.MostrarAyuda();
                return;
            }

            if (Comandos.EsTest(args)) {
                Pruebas.Ejecutar();
                return;
            }

            if (args.Length == 2) {
                string expr = args[0];
                int x = int.Parse(args[1]);

                var nodo = Compilador.Compilar(expr);
                int resultado = nodo.Evaluar(x);

                Console.WriteLine(resultado);
            } else {
                ModoInteractivo();
            }
        } catch (Exception ex) {
            Console.Error.WriteLine("Error: " + ex.Message);
        }
    }

    static void ModoInteractivo() {
        Console.Write("Expresión: ");
        string expr = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(expr))
            return;

        var nodo = Compilador.Compilar(expr);

        while (true) {
            Console.Write("x = ");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "fin")
                break;

            if (!int.TryParse(input, out int x)) {
                Console.WriteLine("Valor inválido");
                continue;
            }

            Console.WriteLine(nodo.Evaluar(x));
        }
    }
}
