#!/usr/bin/env dotnet-script
using System.Globalization;

try
{
    var config = ParseArgs(args);

    if (config.ShowHelp)
    {
        ShowHelp();
        return;
    }

    var inputText = ReadInput(config);
    var rows = ParseDelimited(inputText, config);
    var sorted = SortRows(rows, config);
    var outputText = Serialize(sorted, config);
    WriteOutput(outputText, config);
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
    bool showHelp = false;
    var sortFields = new List<SortField>();

    var positional = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        switch (arg)
        {
            case "-h":
            case "--help":
                showHelp = true;
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-i":
            case "--input":
                input = args[++i];
                break;

            case "-o":
            case "--output":
                output = args[++i];
                break;

            case "-d":
            case "--delimiter":
                var d = args[++i];
                delimiter = d == "\\t" ? "\t" : d;
                break;

            case "-b":
            case "--by":
                sortFields.Add(ParseSortField(args[++i]));
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new Exception($"Opción desconocida: {arg}");
                positional.Add(arg);
                break;
        }
    }

    if (positional.Count > 0) input ??= positional[0];
    if (positional.Count > 1) output ??= positional[1];

    return new AppConfig(input, output, delimiter, noHeader, showHelp, sortFields);
}

SortField ParseSortField(string expr)
{
    var parts = expr.Split(':');

    var name = parts[0];
    var type = parts.Length > 1 ? parts[1] : "alpha";
    var order = parts.Length > 2 ? parts[2] : "asc";

    bool numeric = type == "num";
    bool desc = order == "desc";

    return new SortField(name, numeric, desc);
}

string ReadInput(AppConfig config)
{
    if (config.InputFile != null)
        return File.ReadAllText(config.InputFile);

    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.TrimEnd('\r'))
                    .ToList();

    if (lines.Count == 0) return new();

    List<string> headers;

    int start = 0;

    if (config.NoHeader)
    {
        var count = lines[0].Split(config.Delimiter).Length;
        headers = Enumerable.Range(0, count).Select(i => i.ToString()).ToList();
    }
    else
    {
        headers = lines[0].Split(config.Delimiter).ToList();
        start = 1;
    }

    var result = new List<Dictionary<string, string>>();

    for (int i = start; i < lines.Count; i++)
    {
        var values = lines[i].Split(config.Delimiter);
        var dict = new Dictionary<string, string>();

        for (int j = 0; j < headers.Count; j++)
        {
            dict[headers[j]] = j < values.Length ? values[j] : "";
        }

        result.Add(dict);
    }

    // validar columnas
    foreach (var sf in config.SortFields)
    {
        if (!headers.Contains(sf.Name))
            throw new Exception($"Columna inexistente: {sf.Name}");
    }

    return result;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    for (int i = 0; i < config.SortFields.Count; i++)
    {
        var sf = config.SortFields[i];

        Func<Dictionary<string, string>, object> keySelector = row =>
        {
            var val = row[sf.Name];

            if (sf.Numeric)
                return double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0;

            return val;
        };

        if (i == 0)
        {
            ordered = sf.Descending
                ? rows.OrderByDescending(keySelector)
                : rows.OrderBy(keySelector);
        }
        else
        {
            ordered = sf.Descending
                ? ordered!.ThenByDescending(keySelector)
                : ordered!.ThenBy(keySelector);
        }
    }

    return ordered?.ToList() ?? rows;
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0) return "";

    var headers = rows[0].Keys.ToList();
    var lines = new List<string>();

    if (!config.NoHeader)
        lines.Add(string.Join(config.Delimiter, headers));

    foreach (var row in rows)
    {
        var line = string.Join(config.Delimiter, headers.Select(h => row[h]));
        lines.Add(line);
    }

    return string.Join("\n", lines);
}

void WriteOutput(string text, AppConfig config)
{
    if (config.OutputFile != null)
    {
        File.WriteAllText(config.OutputFile, text);
    }
    else
    {
        Console.WriteLine(text);
    }
}

void ShowHelp()
{
    Console.WriteLine(@"

");
}

record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    bool ShowHelp,
    List<SortField> SortFields
);