
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]
try
{
    var config = ParseArgs(args);
    var text   = ReadInput(config);
    var rows   = ParseDelimited(text, config);
    var sorted = SortRows(rows, config);
    var output = Serialize(sorted, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string?         inputFile  = null;
    string?         outputFile = null;
    string          delimiter  = ",";
    bool            noHeader   = false;
    List<SortField> sortFields = new();

    int i = 0;
    while (i < args.Length)
    {
        string arg = args[i];

        if (arg == "-b" || arg == "--by")
        {
            i++;
            sortFields.Add(ParseSortField(args[i]));
        }
        else if (arg == "-i" || arg == "--input")
        {
            i++;
            inputFile = args[i];
        }
        else if (arg == "-o" || arg == "--output")
        {
            i++;
            outputFile = args[i];
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            i++;
            delimiter = args[i];
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            noHeader = true;
        }
        else if (arg == "-h" || arg == "--help")
        {
            Console.WriteLine("uso: sortx ...");
            Environment.Exit(0);
        }
        else
        {
            inputFile = arg;
        }

        i++;
    }

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

SortField ParseSortField(string spec)
{
    string[] parts = spec.Split(':');

    string name       = parts[0];
    bool   numeric    = parts.Length > 1 && parts[1] == "num";
    bool   descending = parts.Length > 2 && parts[2] == "desc";

    return new SortField(name, numeric, descending);
}

string ReadInput(AppConfig config)
{
    if (config.InputFile != null)
        return File.ReadAllText(config.InputFile);

    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string texto, AppConfig config)
{
    string[] lines = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    string[] headers;
    int      startLine;

    if (config.NoHeader)
    {
        int colCount = lines[0].TrimEnd('\r').Split(config.Delimiter).Length;
        headers   = Enumerable.Range(0, colCount).Select(n => n.ToString()).ToArray();
        startLine = 0;
    }
    else
    {
        headers   = lines[0].TrimEnd('\r').Split(config.Delimiter);
        startLine = 1;
    }

    var rows = new List<Dictionary<string, string>>();

    for (int i = startLine; i < lines.Length; i++)
    {
        string[] values = lines[i].TrimEnd('\r').Split(config.Delimiter);
        var      row    = new Dictionary<string, string>();

        for (int j = 0; j < headers.Length; j++)
            row[headers[j]] = j < values.Length ? values[j] : "";

        rows.Add(row);
    }

    return rows;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (config.SortFields.Count == 0)
        return rows;

    if (rows.Count > 0)
    {
        foreach (SortField field in config.SortFields)
        {
            if (!rows[0].ContainsKey(field.Name))
                throw new Exception($"La columna '{field.Name}' no existe en el archivo.");
        }
    }

    SortField first = config.SortFields[0];

    IOrderedEnumerable<Dictionary<string, string>> sorted = first.Descending
        ? rows.OrderByDescending(row => GetSortKey(row, first))
        : rows.OrderBy(row => GetSortKey(row, first));

    for (int i = 1; i < config.SortFields.Count; i++)
    {
        SortField field = config.SortFields[i];
        sorted = field.Descending
            ? sorted.ThenByDescending(row => GetSortKey(row, field))
            : sorted.ThenBy(row => GetSortKey(row, field));
    }

    return sorted.ToList();
}

object GetSortKey(Dictionary<string, string> row, SortField field)
{
    string value = row.ContainsKey(field.Name) ? row[field.Name] : "";

    if (field.Numeric && double.TryParse(value, out double number))
        return number;

    return value;
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
        return "";

    var lines = new List<string>();

    if (!config.NoHeader)
        lines.Add(string.Join(config.Delimiter, rows[0].Keys));

    foreach (var row in rows)
        lines.Add(string.Join(config.Delimiter, row.Values));

    return string.Join('\n', lines);
}

void WriteOutput(string text, AppConfig config)
{
    if (config.OutputFile != null)
        File.WriteAllText(config.OutputFile, text);
    else
        Console.WriteLine(text);
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?          InputFile,
    string?          OutputFile,
    string           Delimiter,
    bool             NoHeader,
    List<SortField>  SortFields
);

Console.WriteLine($"sortx {string.Join(" ", args)}");
