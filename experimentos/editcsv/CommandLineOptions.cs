namespace EditCsv;

internal sealed class CommandLineOptions
{
    public string? FilePath { get; private set; }
    public bool HasHeader { get; private set; } = true;
    public char? Delimiter { get; private set; }
    public bool ShowHelp { get; private set; }

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;

                case "--no-header":
                    options.HasHeader = false;
                    break;

                case "-d":
                case "--delimiter":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Falta el delimitador luego de -d/--delimiter.");
                    }

                    options.Delimiter = ParseDelimiter(args[++i]);
                    break;

                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new ArgumentException($"Opcion no reconocida: {arg}");
                    }

                    options.FilePath ??= arg;
                    break;
            }
        }

        return options;
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
            editcsv - editor TUI de archivos CSV
            
            Uso:
              dotnet run --project editcsv -- archivo.csv
              dotnet run --project editcsv -- archivo.csv --no-header
              dotnet run --project editcsv -- archivo.csv -d ';'
            
            Opciones:
              -h, --help         Muestra esta ayuda.
              --no-header        Trata la primera fila como datos.
              -d, --delimiter    Fuerza el delimitador: , ; | \t
            
            Controles dentro de la TUI:
              Flechas / Tab      Navegar
              Enter o E          Editar celda
              A                  Agregar fila
              D                  Borrar fila
              O                  Ordenar por columna actual
              R                  Renombrar columna
              L                  Cambiar delimitador
              .                  Alternar enteros / 2 decimales en la columna actual
              H                  Agregar columna
              X                  Borrar columna
              S                  Guardar
              Q                  Salir
            """);
    }

    internal static bool TryParseDelimiter(string value, out char delimiter)
    {
        try
        {
            delimiter = ParseDelimiter(value);
            return true;
        }
        catch (ArgumentException)
        {
            delimiter = default;
            return false;
        }
    }

    private static char ParseDelimiter(string value)
    {
        return value switch
        {
            "\\t" => '\t',
            "" => throw new ArgumentException("El delimitador no puede ser vacio."),
            _ when value.Length == 1 => value[0],
            _ => throw new ArgumentException($"Delimitador invalido: {value}")
        };
    }
}
