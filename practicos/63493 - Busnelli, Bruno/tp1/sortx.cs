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
    throw new NotImplementedException();
}

void WriteOutput(string output, AppConfig config)
{
    throw new NotImplementedException();
}

string GetHelpText()
{
    return "sortx - ayuda";
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

    string name = parts[0];
    string type = parts.Length >= 2 ? parts[1] : "alpha";
    string order = parts.Length >= 3 ? parts[2] : "asc";

    bool numeric = type == "num";
    bool descending = order == "desc";

    return new SortField(name, numeric, descending);
}

string NormalizeDelimiter(string value)
{
    return value == "\\t" ? "\t" : value;
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