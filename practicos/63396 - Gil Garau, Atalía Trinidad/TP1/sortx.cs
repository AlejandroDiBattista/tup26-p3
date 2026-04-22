using System.Globalization;
try
{
    AppConfig config = ParseArgs(args);

    string inputText = ReadInput(config.InputFile);
    var (headers, rows) = ParseDelimited(inputText, config);
    var sortedRows = SortRows(rows, headers, config.SortFields);
    string output = Serialize(headers, sortedRows, config);
    WriteOutput(config.OutputFile, output);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine("Use --help para ver las opciones.");
    Environment.Exit(1);
}
AppConfig ParseArgs(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;

    var sortFields = new List<SortField>();
    var positional = new List<string>();

    int i = 0;
    while (i < args.Length)
    {
        string arg = args[i];

        switch (arg)
        {
            case "--help":
            case "-h":
                PrintHelp();
                Environment.Exit(0);
                break;

            case "--input":
            case "-i":
                inputFile = Next(args, ref i, arg);
                break;

            case "--output":
            case "-o":
                outputFile = Next(args, ref i, arg);
                break;

            case "--delimiter":
            case "-d":
                delimiter = Next(args, ref i, arg);
                break;

            case "--no-header":
            case "-nh":
                noHeader = true;
                break;

            case "--by":
            case "-b":
                sortFields.Add(ParseSortField(Next(args, ref i, arg)));
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new Exception($"Opción desconocida: '{arg}'.");

                positional.Add(arg);
                break;
        }

        i++;
    }

    if (positional.Count >= 1 && inputFile == null)
        inputFile = positional[0];
    if (positional.Count >= 2 && outputFile == null)
        outputFile = positional[1];
    if (positional.Count > 2)
        throw new Exception("Demasiados argumentos. Máximo: [input] [output]");
    if (sortFields.Count == 0)
        throw new Exception("Debe indicar al menos un campo con -b|--by.");
    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

string Next(string[] args, ref int i, string option)
{
    i++;
    if (i >= args.Length)
        throw new Exception($"Falta valor para la opción '{option}'.");

    return args[i];
}
string ReadInput(string? filePath)
{
    if (filePath == null)
        return Console.In.ReadToEnd();

    if (!File.Exists(filePath))
        throw new FileNotFoundException($"Archivo no encontrado: '{filePath}'");

    return File.ReadAllText(filePath);
}
void WriteOutput(string? filePath, string content)
{
    if (filePath == null)
        Console.Write(content);
    else
        File.WriteAllText(filePath, content);
}
(string[] headers, List<Dictionary<string, string>> rows) ParseDelimited(string content, AppConfig config)
{
    string delimiter = config.Delimiter == "\\t" ? "\t" : config.Delimiter;
    string[] lines = content.Split('\n');

    int last = lines.Length - 1;
    while (last >= 0 && lines[last].Trim() == "")
        last--;

    if (last < 0)
        return (Array.Empty<string>(), new List<Dictionary<string, string>>());

    var rows = new List<Dictionary<string, string>>();
    string[] headers;
    int startRow;

    if (config.NoHeader)
    {
        string firstLine = lines[0].TrimEnd('\r');
        string[] firstValues = firstLine.Split(delimiter);

        headers = new string[firstValues.Length];
        for (int i = 0; i < firstValues.Length; i++)
            headers[i] = i.ToString();

        startRow = 0;
    }
    else
    {
        headers = lines[0].TrimEnd('\r').Split(delimiter);
        startRow = 1;
    }

    for (int i = startRow; i <= last; i++)
    {
        string line = lines[i];
        if (line.Trim() == "")
            continue;

        string[] values = line.TrimEnd('\r').Split(delimiter);
        var row = new Dictionary<string, string>();

        for (int col = 0; col < headers.Length; col++)
            row[headers[col]] = col < values.Length ? values[col] : "";

        rows.Add(row);
    }

    return (headers, rows);
}
List<Dictionary<string, string>> SortRows(
    List<Dictionary<string, string>> rows,
    string[] headers,
    List<SortField> sortFields)
{
    foreach (SortField field in sortFields)
    {
        bool found = false;

        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i] == field.Name)
            {
                found = true;
                break;
            }
        }

        if (!found)
            throw new Exception($"Campo no encontrado: '{field.Name}'");
    }
    rows.Sort((a, b) =>
    {
        foreach (SortField field in sortFields)
        {
            string valueA = a.ContainsKey(field.Name) ? a[field.Name] : "";
            string valueB = b.ContainsKey(field.Name) ? b[field.Name] : "";

            int result;

            if (field.Numeric)
            {
                double numA = double.TryParse(valueA, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedA) ? parsedA : 0;
                double numB = double.TryParse(valueB, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedB) ? parsedB : 0;
                result = numA.CompareTo(numB);
            }
            else
            {
                result = string.Compare(valueA, valueB, StringComparison.Ordinal);
            }

            if (field.Descending)
                result = -result;
            if (result != 0)
                return result;
        }
        return 0;
    });
    return rows;
}
string Serialize(string[] headers, List<Dictionary<string, string>> rows, AppConfig config)
{
    string delimiter = config.Delimiter == "\\t" ? "\t" : config.Delimiter;
    string result = "";

    if (!config.NoHeader)
        result += string.Join(delimiter, headers) + "\n";

    foreach (var row in rows)
    {
        string[] values = new string[headers.Length];

        for (int i = 0; i < headers.Length; i++)
            values[i] = row.ContainsKey(headers[i]) ? row[headers[i]] : "";

        result += string.Join(delimiter, values) + "\n";
    }
    return result;
}

SortField ParseSortField(string text)
{
    string[] parts = text.Split(':');

    if (parts.Length < 1 || parts[0] == "")
        throw new Exception("Campo de orden inválido.");

    string name = parts[0];
    bool numeric = false;
    bool descending = false;

    if (parts.Length >= 2)
    {
        if (parts[1] == "num")
            numeric = true;
        else if (parts[1] == "text"  || parts[1] == "alpha")
            numeric = false;
        else    
            throw new Exception($"Tipo inválido en '{text}'. Use 'num', 'text' o 'alpha'.");
    }
    if (parts.Length >= 3)
    {
        if (parts[2] == "desc")
            descending = true;
        else if (parts[2] == "asc")
            descending = false;
        else
            throw new Exception($"Orden inválido en '{text}'. Use 'asc' o 'desc'.");
    }
    if (parts.Length > 3)
        throw new Exception($"Formato inválido en '{text}'. Use campo[:tipo[:orden]].");

    return new SortField(name, numeric, descending);
    
}
void PrintHelp()
{
    Console.WriteLine("Uso:");
    Console.WriteLine("sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
    Console.WriteLine("      [-i|--input input] [-o|--output output]");
    Console.WriteLine("      [-d|--delimiter delimitador]");
    Console.WriteLine("      [-nh|--no-header] [-h|--help]");
}
record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
