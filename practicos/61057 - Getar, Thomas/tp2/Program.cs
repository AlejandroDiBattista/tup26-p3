using System;

namespace Calculadora
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Comandos comandos = new Comandos(args);

                switch (comandos.Modo)
                {
                    case Modo.Ayuda:
                        MostrarAyuda();
                        return 0;

                    case Modo.Pruebas:
                        Pruebas.Ejecutar();
                        return 0;

                    case Modo.Directo:
                        Nodo ast = new Compilador(comandos.Expresion).Parsear();
                        Console.WriteLine(ast.Evaluar(comandos.ValorX));
                        return 0;

                    case Modo.Interactivo:
                        return ModoInteractivo();

                    default:
                        Console.Error.WriteLine("Argumentos inválidos.");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        static int ModoInteractivo()
        {
            Console.Write("Expresión: ");

            string? expresion = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(expresion))
            {
                Console.WriteLine("Expresión vacía.");
                return 1;
            }

            Nodo ast = new Compilador(expresion).Parsear();

            while (true)
            {
                Console.Write("x = ");

                string? entrada = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(entrada) ||
                    entrada.Equals("fin", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (!int.TryParse(entrada, out int x))
                {
                    Console.WriteLine("Valor inválido.");
                    continue;
                }

                try
                {
                    Console.WriteLine(ast.Evaluar(x));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return 0;
        }

        static void MostrarAyuda()
        {
            Console.WriteLine("calculadora [expresion valor]");
            Console.WriteLine();
            Console.WriteLine("Opciones:");
            Console.WriteLine("  -h, --help      Mostrar ayuda");
            Console.WriteLine("  -t, --test      Ejecutar pruebas");
            Console.WriteLine("  -p, --probar    Ejecutar pruebas");
            Console.WriteLine();
            Console.WriteLine("Ejemplos:");
            Console.WriteLine("  calculadora \"1 + 2 * x\" 10");
            Console.WriteLine("  calculadora");
        }
    }
}