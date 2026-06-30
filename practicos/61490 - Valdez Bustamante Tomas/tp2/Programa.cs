namespace calculadora;

// ─── Punto de entrada ─────────────────────────────────────────────────────────

class Programa
{
    static void Main(string[] args)
    {
        try
        {
            var comando = Comandos.Parsear(args);
            var compilador = new Compilador();

            switch (comando.Modo)
            {
                // ── Ayuda ──────────────────────────────────────────────────────────────
                case ModoEjecucion.Ayuda:
                    Comandos.MostrarAyuda();
                    Environment.Exit(0);
                    break;

                // ── Pruebas ────────────────────────────────────────────────────────────
                case ModoEjecucion.Pruebas:
                    Pruebas.Ejecutar();
                    break;

                // ── Modo directo ───────────────────────────────────────────────────────
                case ModoEjecucion.Directo:
                {
                    Nodo ast = compilador.Compilar(comando.Expresion!);
                    int resultado = ast.Evaluar(comando.Valor!.Value);
                    Console.WriteLine(resultado);
                    break;
                }

                // ── Modo interactivo ───────────────────────────────────────────────────
                case ModoEjecucion.Interactivo:
                {
                    // 1. Pedir y compilar la expresión una sola vez
                    Console.Write("Expresión: ");
                    string? linea = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(linea))
                    {
                        Console.Error.WriteLine("Error: la expresión está vacía.");
                        Environment.Exit(1);
                    }

                    Nodo ast = compilador.Compilar(linea);
                    Console.WriteLine($"Expresión compilada: {ast}");
                    Console.WriteLine("Ingrese valores para x (o 'fin' / vacío para terminar):");

                    // 2. Evaluar repetidamente
                    while (true)
                    {
                        Console.Write("x = ");
                        string? entrada = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(entrada) ||
                            entrada.Trim().Equals("fin", StringComparison.OrdinalIgnoreCase))
                            break;

                        if (!int.TryParse(entrada.Trim(), out int valorX))
                        {
                            Console.Error.WriteLine($"Error: '{entrada}' no es un entero válido.");
                            continue;
                        }

                        try
                        {
                            Console.WriteLine(ast.Evaluar(valorX));
                        }
                        catch (DivisionPorCeroException ex)
                        {
                            Console.Error.WriteLine(ex.Message);
                        }
                    }

                    break;
                }
            }
        }
        catch (ErrorDeParsing ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }
        catch (DivisionPorCeroException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }
    }
}