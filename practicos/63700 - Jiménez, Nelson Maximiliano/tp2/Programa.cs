class Programa
{
    static void Main(string[] args)
    {
        try
        {
            var cmd = Comandos.Parse(args);

            if (cmd.Help)
            {
                MostrarAyuda();
                return;
            }

            if (cmd.Test)
            {
                Pruebas.Ejecutar();
                return;
            }

            var compilador = new Compilador();

            // MODO DIRECTO
            if (cmd.Expresion != null && cmd.Valor != null)
            {
                var nodo = compilador.Parsear(cmd.Expresion);

                if (!int.TryParse(cmd.Valor, out int x))
                    throw new Exception("Valor de x inválido");

                Console.WriteLine(nodo.Evaluar(x));
                return;
            }

            // MODO INTERACTIVO
            Console.Write("Expresión: ");
            var expr = Console.ReadLine();

            var ast = compilador.Parsear(expr);

            while (true)
            {
                Console.Write("x = ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input)) break;

                if (!int.TryParse(input, out int x))
                {
                    Console.WriteLine("Valor inválido");
                    continue;
                }

                Console.WriteLine($"Resultado: {ast.Evaluar(x)}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void MostrarAyuda()
    {
        Console.WriteLine(@"
Uso:
  calculadora ""expresion"" valor

Opciones:
  -h, --help     Mostrar ayuda
  -t, --test     Ejecutar pruebas

Ejemplo:
  calculadora ""1 + 2 * x"" 10
");
    }
}