// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

try
{
    var config  = ParseArgs(args);
    var text    = ReadInput(config);
    var (headers, rows) = ParseDelimited(text, config);
    var sorted  = SortRows(rows, headers, config);
    var output  = Serialize(headers, sorted, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? inputFile = null, outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();
    int positional = 0;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h" or "--help":
                Console.WriteLine("""
                    Uso: sortx [input [output]] [-b campo[:tipo[:orden]]]...
                               [-i input] [-o output] [-d delimitador] [-nh] [-h]

                    Opciones:
                      -b, --by          Campo de ordenamiento (repetible): campo[:alpha|num[:asc|desc]]
                      -i, --input       Archivo de entrada (default: stdin)
                      -o, --output      Archivo de salida (default: stdout)
                      -d, --delimiter   Delimitador (default: ,). Usar \t para tabulación.
                      -nh, --no-header  Sin encabezado; referenciar columnas por índice
                      -h, --help        Muestra esta ayuda
                    """);
                Environment.Exit(0);
                break;
            case "-i" or "--input":    inputFile  = args[++i]; break;
            case "-o" or "--output":   outputFile = args[++i]; break;
            case "-nh" or "--no-header": noHeader = true; break;
            case "-d" or "--delimiter":
                var d = args[++i];
                delimiter = d == @"\t" ? "\t" : d;
                break;
            case "-b" or "--by":
                sortFields.Add(ParseField(args[++i]));
                break;
            default:
                if (args[i].StartsWith('-'))
                    throw new ArgumentException($"Opción desconocida: {args[i]}");
                if (positional == 0) inputFile  = args[i];
                else if (positional == 1) outputFile = args[i];
                positional++;
                break;
        }
    }

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

SortField ParseField(string spec)
{
    var parts = spec.Split(':');
    return new SortField(
        Name:       parts[0],
        Numeric:    parts.Length > 1 && parts[1] == "num",
        Descending: parts.Length > 2 && parts[2] == "desc"
    );
}

string ReadInput(AppConfig config) =>
    config.InputFile is { } file ? File.ReadAllText(file) : Console.In.ReadToEnd();

(string[]? headers, List<string[]> rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split('\n')
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => l.Length > 0)
                    .ToList();

    if (lines.Count == 0) return (null, []);

    if (config.NoHeader)
        return (null, lines.Select(l => l.Split(config.Delimiter)).ToList());

    var headers = lines[0].Split(config.Delimiter);
    var rows    = lines.Skip(1).Select(l => l.Split(config.Delimiter)).ToList();
    return (headers, rows);
}

List<string[]> SortRows(List<string[]> rows, string[]? headers, AppConfig config)
{
    if (config.SortFields.Count == 0) return rows;

    int GetIndex(SortField field)
    {
        if (int.TryParse(field.Name, out int idx)) return idx;
        if (headers is null)
            throw new ArgumentException($"Sin encabezado; usar índice en lugar de '{field.Name}'");
        int found = Array.IndexOf(headers, field.Name);
        if (found < 0)
            throw new ArgumentException($"Columna no encontrada: '{field.Name}'");
        return found;
    }

    var comparer = Comparer<string[]>.Create((a, b) =>
    {
        foreach (var field in config.SortFields)
        {
            var idx = GetIndex(field);
            var va  = idx < a.Length ? a[idx] : "";
            var vb  = idx < b.Length ? b[idx] : "";

            int cmp = field.Numeric
                ? double.Parse(va).CompareTo(double.Parse(vb))
                : string.Compare(va, vb, StringComparison.CurrentCulture);

            if (cmp != 0) return field.Descending ? -cmp : cmp;
        }
        return 0;
    });

    return rows.Order(comparer).ToList();
}

string Serialize(string[]? headers, List<string[]> rows, AppConfig config)
{
    var lines = rows.Select(r => string.Join(config.Delimiter, r));
    if (headers is not null)
        lines = lines.Prepend(string.Join(config.Delimiter, headers));
    return string.Join(Environment.NewLine, lines) + Environment.NewLine;
}

void WriteOutput(string text, AppConfig config)
{
    if (config.OutputFile is { } file)
        File.WriteAllText(file, text);
    else
        Console.Write(text);
}

record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(string? InputFile, string? OutputFile, string Delimiter, bool NoHeader, List<SortField> SortFields);

