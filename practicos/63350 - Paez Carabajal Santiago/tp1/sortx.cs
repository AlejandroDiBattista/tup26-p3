// PUNTO DE ENTRADA
try
{
    Console.WriteLine("Paso 1: ParseArgs");
    var config = ParseArgs(args);

    Console.WriteLine("Paso 2: ReadInput");
    var text = ReadInput(config.InputFile);

    Console.WriteLine("Paso 3: ParseDelimited");
    var (headers, rows) = ParseDelimited(text, config);

    Console.WriteLine("Paso 4: SortRows");
    var sorted = SortRows(rows, headers, config);

    Console.WriteLine("Paso 5: Serialize");
    var output = Serialize(headers, sorted, config);

    Console.WriteLine("Paso 6: WriteOutput");
    WriteOutput(config.OutputFile, output);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
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
                var value = args[++i];
                delimiter = value == "\\t" ? "\t" : value;
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

    if (positional.Count > 0 && input == null)
        input = positional[0];

    if (positional.Count > 1 && output == null)
        output = positional[1];

    if (fields.Count == 0)
        throw new Exception("Debes usar -b");

    return new AppConfig(input, output, delimiter, noHeader, fields);
}

SortField ParseSortField(string text)
{
    var parts = text.Split(':');

    string name = parts[0];
    bool numeric = false;
    bool desc = false;

    if (parts.Length > 1)
    {
        if (parts[1] == "num") numeric = true;
        else if (parts[1] == "alpha") numeric = false;
    }

    if (parts.Length > 2)
    {
        if (parts[2] == "desc") desc = true;
        else if (parts[2] == "asc") desc = false;
    }

    return new SortField(name, numeric, desc);
}

string ReadInput(string? file)
{
    if (file == null) return Console.In.ReadToEnd();

    if (!File.Exists(file))
        throw new Exception("Archivo no encontrado");

    return File.ReadAllText(file);
}

(List<string> headers, List<string[]> rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
        .Where(l => l.Trim() != "")
        .ToList();

    List<string> headers;
    int start = 0;

    if (!config.NoHeader)
    {
        headers = lines[0]
            .Split(config.Delimiter)
            .Select(x => x.Trim())
            .ToList();

        start = 1;
    }
    else
    {
        int cols = lines[0].Split(config.Delimiter).Length;
        headers = Enumerable.Range(0, cols)
            .Select(x => x.ToString())
            .ToList();
    }

    var rows = new List<string[]>();

    for (int i = start; i < lines.Count; i++)
    {
        rows.Add(lines[i]
            .Split(config.Delimiter)
            .Select(x => x.Trim())
            .ToArray());
    }

    return (headers, rows);
}

List<string[]> SortRows(List<string[]> rows, List<string> headers, AppConfig config)
{
    int GetIndex(string name)
    {
        int idx = headers.FindIndex(h =>
            h.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (idx == -1)
            throw new Exception($"Columna no encontrada: {name}");

        return idx;
    }

    var result = rows;

    foreach (var field in config.SortFields.AsEnumerable().Reverse())
    {
        int index = GetIndex(field.Name);

        if (field.Numeric)
        {
            result = field.Descending
                ? result.OrderByDescending(r =>
                    double.TryParse(r[index],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var n) ? n : 0)
                    .ToList()
                : result.OrderBy(r =>
                    double.TryParse(r[index],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var n) ? n : 0)
                    .ToList();
        }
        else
        {
            result = field.Descending
                ? result.OrderByDescending(r => r[index]).ToList()
                : result.OrderBy(r => r[index]).ToList();
        }
    }

    return result;
}

string Serialize(List<string> headers, List<string[]> rows, AppConfig config)
{
    var result = "";

    if (!config.NoHeader)
        result += string.Join(config.Delimiter, headers) + "\n";

    foreach (var r in rows)
        result += string.Join(config.Delimiter, r) + "\n";

    return result;
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
    Console.WriteLine("Uso: sortx [input] --by campo[:tipo[:orden]] [--output archivo]");
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
