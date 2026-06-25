// ── PUNTO DE ENTRADA   ────────────────────────────────────

try
{
    var config = ParseArgs(args);
    var text = ReadInput(config.InputFile);
    var (headers, rows) = ParseDelimited(text, config);
    var sorted = SortRows(rows, headers, config);
    var output = Serialize(headers, sorted, config);
    WriteOutput(config.OutputFile, output);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var fields = new List<SortField>();
    var positional = new List<string>();

    int i = 0;
    while (i < args.Length)
    {
        var arg = args[i];

        switch (arg)
        {
            case "-i":
            case "--input":
                input = args[++i];
                i++;
                break;

            case "-o":
            case "--output":
                output = args[++i];
                i++;
                break;

            case "-d":
            case "--delimiter":
                string delVal = args[++i];
                delimiter = delVal == "\\t" ? "\t" : delVal;
                i++;
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                i++;
                break;

            case "-b":
            case "--by":
                fields.Add(ParseSortField(args[++i]));
                i++;
                break;

            case "-h":
            case "--help":
                PrintHelp();
                Environment.Exit(0);
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new Exception($"Opción inválida: {arg}");
                positional.Add(arg);
                i++;
                break;
        }
    }

    if (positional.Count > 0 && input == null) input = positional[0];
    if (positional.Count > 1 && output == null) output = positional[1];

    return new AppConfig(input, output, delimiter, noHeader, fields);
}

SortField ParseSortField(string text)
{
    var parts = text.Split(':');

    string name = parts[0];
    bool numeric = parts.Length > 1 && parts[1] == "num";
    bool desc = parts.Length > 2 && parts[2] == "desc";

    return new SortField(name, numeric, desc);
}

string ReadInput(string? file)
{
    if (file == null)
        return Console.In.ReadToEnd();

    if (!File.Exists(file))
        throw new Exception($"El archivo '{file}' no existe.");

    return File.ReadAllText(file);
}

(List<string> headers, List<string[]> rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split('\n')
                    .Where(l => l.Trim() != "")
                    .ToList();

    List<string> headers;
    int start = 0;

    if (!config.NoHeader)
    {
        headers = lines[0].TrimEnd('\r').Split(config.Delimiter).Select(x => x.Trim()).ToList();
        start = 1;
    }
    else
    {
        int cols = lines[0].TrimEnd('\r').Split(config.Delimiter).Length;
        headers = Enumerable.Range(0, cols).Select(x => x.ToString()).ToList();
    }

    var rows = new List<string[]>();

    for (int i = start; i < lines.Count; i++)
        rows.Add(lines[i].TrimEnd('\r').Split(config.Delimiter));

    return (headers, rows);
}

List<string[]> SortRows(List<string[]> rows, List<string> headers, AppConfig config)
{
    if (config.SortFields.Count == 0)
        return rows;

    int GetIndex(string name)
    {
        int idx = headers.IndexOf(name);
        if (idx == -1)
            throw new Exception($"La columna '{name}' no existe en el archivo.");
        return idx;
    }

    SortField first = config.SortFields[0];
    int firstIndex = GetIndex(first.Name);

    IOrderedEnumerable<string[]> sorted = first.Numeric
        ? (first.Descending
            ? rows.OrderByDescending(r => double.TryParse(r[firstIndex], out var n) ? n : 0)
            : rows.OrderBy(r => double.TryParse(r[firstIndex], out var n) ? n : 0))
        : (first.Descending
            ? rows.OrderByDescending(r => r[firstIndex])
            : rows.OrderBy(r => r[firstIndex]));

    for (int i = 1; i < config.SortFields.Count; i++)
    {
        SortField field = config.SortFields[i];
        int index = GetIndex(field.Name);

        sorted = field.Numeric
            ? (field.Descending
                ? sorted.ThenByDescending(r => double.TryParse(r[index], out var n) ? n : 0)
                : sorted.ThenBy(r => double.TryParse(r[index], out var n) ? n : 0))
            : (field.Descending
                ? sorted.ThenByDescending(r => r[index])
                : sorted.ThenBy(r => r[index]));
    }

    return sorted.ToList();
}

string Serialize(List<string> headers, List<string[]> rows, AppConfig config)
{
    var lines = new List<string>();

    if (!config.NoHeader)
        lines.Add(string.Join(config.Delimiter, headers));

    foreach (var r in rows)
        lines.Add(string.Join(config.Delimiter, r));

    return string.Join("\n", lines);
}

void WriteOutput(string? file, string content)
{
    if (file == null)
        Console.WriteLine(content);
    else
        File.WriteAllText(file, content);
}

void PrintHelp()
{
    Console.WriteLine("Uso: sortx [input] -b campo[:tipo[:orden]]");
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);