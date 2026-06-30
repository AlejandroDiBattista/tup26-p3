using System.Globalization;

try
{
    var config = ParseArgs(args);
    var input = ReadInput(config);
    var table = ParseDelimited(input, config);
    var sortedRows = SortRows(table.Rows, config);
    var output = Serialize(table.Headers, sortedRows, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    bool help = false;

    var sortFields = new List<SortField>();
    var positionals = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];

        switch (arg)
        {
            case "-h":
            case "--help":
                help = true;
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-i":
            case "--input":
                inputFile = RequireValue(args, ref i, arg);
                break;

            case "-o":
            case "--output":
                outputFile = RequireValue(args, ref i, arg);
                break;

            case "-d":
            case "--delimiter":
                delimiter = NormalizeDelimiter(RequireValue(args, ref i, arg));
                break;

            case "-b":
            case "--by":
                sortFields.Add(ParseSortField(RequireValue(args, ref i, arg)));
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new ArgumentException($"Opción desconocida: {arg}");

                positionals.Add(arg);
                break;
        }
    }

    if (help)
    {
        Console.WriteLine(GetHelpText());
        Environment.Exit(0);
    }

    if (positionals.Count > 2)
        throw new ArgumentException("Se esperaban como máximo dos argumentos posicionales.");

    inputFile ??= positionals.Count >= 1 ? positionals[0] : null;
    outputFile ??= positionals.Count >= 2 ? positionals[1] : null;

    if (sortFields.Count == 0)
        throw new ArgumentException("Debe indicar al menos un criterio de ordenamiento con -b o --by.");

    return new AppConfig(
        inputFile,
        outputFile,
        delimiter,
        noHeader,
        sortFields,
        help
    );
}

string ReadInput(AppConfig config)
{
    if (!string.IsNullOrWhiteSpace(config.InputFile))
    {
        if (!File.Exists(config.InputFile))
            throw new FileNotFoundException($"No existe el archivo de entrada: {config.InputFile}");

        return File.ReadAllText(config.InputFile);
    }

    return Console.In.ReadToEnd();
}

ParsedTable ParseDelimited(string text, AppConfig config)
{
    var lines = text
        .Replace("\r\n", "\n")
        .Replace("\r", "\n")
        .Split('\n')
        .Where(line => line.Length > 0)
        .ToList();

    if (lines.Count == 0)
        throw new ArgumentException("La entrada está vacía.");

    var firstRow = SplitLine(lines[0], config.Delimiter);

    List<string> headers;
    int startIndex;

    if (config.NoHeader)
    {
        headers = Enumerable
            .Range(0, firstRow.Count)
            .Select(i => i.ToString())
            .ToList();

        startIndex = 0;
    }
    else
    {
        headers = firstRow;
        startIndex = 1;
    }

    ValidateSortFields(headers, config);

    var rows = new List<Dictionary<string, string>>();

    for (int i = startIndex; i < lines.Count; i++)
    {
        var values = SplitLine(lines[i], config.Delimiter);

        if (values.Count != headers.Count)
            throw new ArgumentException(
                $"La fila {i + 1} tiene {values.Count} columnas, pero se esperaban {headers.Count}."
            );

        var row = new Dictionary<string, string>();

        for (int j = 0; j < headers.Count; j++)
            row[headers[j]] = values[j];

        rows.Add(row);
    }

    return new ParsedTable(headers, rows);
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    return rows
        .OrderBy(
            row => row,
            Comparer<Dictionary<string, string>>.Create(CompareRows)
        )
        .ToList();

    int CompareRows(
        Dictionary<string, string> a,
        Dictionary<string, string> b)
    {
        foreach (var field in config.SortFields)
        {
            string left = a[field.Name];
            string right = b[field.Name];

            int result = field.Numeric
                ? CompareNumeric(left, right, field.Name)
                : string.Compare(
                    left,
                    right,
                    StringComparison.CurrentCultureIgnoreCase
                );

            if (result != 0)
                return field.Descending ? -result : result;
        }

        return 0;
    }
}

string Serialize(List<string> headers, List<Dictionary<string, string>> rows, AppConfig config)
{
    var outputLines = new List<string>();

    if (!config.NoHeader)
        outputLines.Add(string.Join(config.Delimiter, headers));

    foreach (var row in rows)
    {
        var values = headers.Select(header => row[header]);
        outputLines.Add(string.Join(config.Delimiter, values));
    }

    return string.Join(Environment.NewLine, outputLines) + Environment.NewLine;
}

void WriteOutput(string output, AppConfig config)
{
    if (!string.IsNullOrWhiteSpace(config.OutputFile))
        File.WriteAllText(config.OutputFile, output);
    else
        Console.Write(output);
}

string GetHelpText()
{
    return """
sortx - Herramienta CLI para ordenar archivos delimitados

Uso:
  sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
        [-i|--input input] [-o|--output output]
        [-d|--delimiter delimitador]
        [-nh|--no-header] [-h|--help]

Opciones:
  -b,  --by           Campo por el que ordenar. Se puede repetir.
  -i,  --input        Archivo de entrada.
  -o,  --output       Archivo de salida.
  -d,  --delimiter    Delimitador. Default: ",". Usar "\t" para tabulación.
  -nh, --no-header    Indica que el archivo no tiene encabezado.
  -h,  --help         Muestra esta ayuda y termina.

Formato de --by:
  campo[:tipo[:orden]]

Tipos:
  alpha    Comparación alfabética. Default.
  num      Comparación numérica.

Órdenes:
  asc      Ascendente. Default.
  desc     Descendente.

Ejemplos:
  sortx empleados.csv -b apellido
  sortx empleados.csv salida.csv -b salario:num:desc
  sortx empleados.csv -b departamento -b salario:num:desc
  sortx datos.tsv -d "\t" -nh -b 1:alpha:asc
  cat empleados.csv | sortx -b apellido > ordenado.csv
""";
}

string RequireValue(string[] args, ref int index, string option)
{
    if (index + 1 >= args.Length)
        throw new ArgumentException($"La opción {option} requiere un valor.");

    index++;
    return args[index];
}

SortField ParseSortField(string value)
{
    var parts = value.Split(':');

    if (parts.Length < 1 || parts.Length > 3 || string.IsNullOrWhiteSpace(parts[0]))
        throw new ArgumentException($"Especificación de campo inválida: {value}");

    string name = parts[0];
    string type = parts.Length >= 2 && parts[1] != "" ? parts[1] : "alpha";
    string order = parts.Length >= 3 && parts[2] != "" ? parts[2] : "asc";

    bool numeric = type switch
    {
        "alpha" => false,
        "num" => true,
        _ => throw new ArgumentException($"Tipo de comparación inválido: {type}. Use alpha o num.")
    };

    bool descending = order switch
    {
        "asc" => false,
        "desc" => true,
        _ => throw new ArgumentException($"Orden inválido: {order}. Use asc o desc.")
    };

    return new SortField(name, numeric, descending);
}

string NormalizeDelimiter(string value)
{
    return value switch
    {
        "\\t" => "\t",
        "tab" => "\t",
        _ when value.Length > 0 => value,
        _ => throw new ArgumentException("El delimitador no puede estar vacío.")
    };
}

List<string> SplitLine(string line, string delimiter)
{
    return line.Split(delimiter).ToList();
}

void ValidateSortFields(List<string> headers, AppConfig config)
{
    foreach (var field in config.SortFields)
    {
        if (!headers.Contains(field.Name))
        {
            string available = string.Join(", ", headers);

            throw new ArgumentException(
                $"Campo inexistente: {field.Name}. Campos disponibles: {available}"
            );
        }
    }
}

int CompareNumeric(string left, string right, string fieldName)
{
    if (!decimal.TryParse(
            left,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var n1))
    {
        throw new ArgumentException(
            $"El valor '{left}' del campo '{fieldName}' no es numérico."
        );
    }

    if (!decimal.TryParse(
            right,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var n2))
    {
        throw new ArgumentException(
            $"El valor '{right}' del campo '{fieldName}' no es numérico."
        );
    }

    return n1.CompareTo(n2);
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields,
    bool Help
);

record ParsedTable(
    List<string> Headers,
    List<Dictionary<string, string>> Rows
);