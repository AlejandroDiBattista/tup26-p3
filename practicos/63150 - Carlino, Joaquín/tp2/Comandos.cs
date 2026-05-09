namespace calculadora;

// ─── Resultado del análisis de argumentos ────────────────────────────────────

enum ModoEjecucion
{
    Directo,
    Interactivo,
    Ayuda,
    Pruebas
}

record ComandoParsed(
    ModoEjecucion Modo,
    string? Expresion = null,
    int? Valor = null
);

// ─── Procesador de argumentos ────────────────────────────────────────────────

static class Comandos
{
    public static ComandoParsed Parsear(string[] args)
    {
        if (args.Length == 0)
            return new ComandoParsed(ModoEjecucion.Interactivo);

        // Flags de una sola opción
        if (args.Length == 1)
        {
            string flag = args[0].ToLower();
            if (flag is "--help" or "-h")
                return new ComandoParsed(ModoEjecucion.Ayuda);

            if (flag is "--test" or "--probar" or "-t" or "-p")
                return new ComandoParsed(ModoEjecucion.Pruebas);
        }

        // Modo directo: expresion + valor
        if (args.Length == 2)
        {
            string expresion = args[0];

            if (!int.TryParse(args[1], out int valor))
                throw new ArgumentException($"Error: el valor '{args[1]}' no es un entero válido.");

            return new ComandoParsed(ModoEjecucion.Directo, expresion, valor);
        }

        throw new ArgumentException(
            "Error: argumentos inválidos. Use --help para ver las opciones disponibles.");
    }

    public static void MostrarAyuda()
    {
        Console.WriteLine("""
            calculadora — Evalúa expresiones aritméticas con la variable x

            Uso:
              calculadora                     Modo interactivo
              calculadora "expresion" valor   Evalúa la expresión con el valor dado
              calculadora --help              Muestra esta ayuda
              calculadora --test              Ejecuta pruebas automáticas

            Expresiones soportadas:
              Números enteros, variable x, operadores + - * /
              Paréntesis y operadores unarios + y -

            Ejemplos:
              calculadora "1 + 2 * 3" 0
              calculadora "(x - 1) * 2" 10
              calculadora --test
            """);
    }
}

