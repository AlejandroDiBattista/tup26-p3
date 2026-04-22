
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

try
{
    var config = ParseArgs(args);
    var input = ReadInput(config);
    var rows = ParseDelimited(input, config);
    var sorted = SortRows(rows, config);
    var output = Serialize(sorted, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}

//ParseArgs
AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();

    int i = 0;
    if (i < args.Length && !args[i].StartsWith("-"))
    {
        input = args[i++];
    }
    if (i < args.Length && !args[i].StartsWith("-"))
    {
        output = args[i++];
    }

    while (i < args.Length)
    {
        var arg = args[i];
        switch (arg)
        {
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

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;
            case "-b":
            case "--by":
                var value = args[++i];
                var parts = value.Split(':');

                string name = parts[0];
                bool numeric = parts.Length > 1 && parts[1] == "num";
                bool descending = parts.Length > 2 && parts[2] == "desc";

        sortFields.Add(new SortField(name, numeric, descending));
                break;
            case "-h":
            case "--help":
        ShowHelp();
            Environment.Exit(0);
                break;

            default:
                throw new Exception($"Argumento desconocido: {arg}");
        }
        i++;
    }

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

void ShowHelp()
{
    Console.WriteLine("Uso:");
    Console.WriteLine("sortx [input [output]] [opciones]");
    Console.WriteLine("");
    Console.WriteLine("Opciones:");
    Console.WriteLine("-b, --by campo[:tipo[:orden]]");
    Console.WriteLine("-i, --input archivo");
    Console.WriteLine("-o, --output archivo");
    Console.WriteLine("-d, --delimiter delimitador");
    Console.WriteLine("-nh, --no-header");
    Console.WriteLine("-h, --help");
}

string ReadInput(AppConfig config)
{
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        return File.ReadAllText(config.InputFile);
    }

    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string input, AppConfig config)
{
    var lines = input
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
        .ToList();

    var rows = new List<Dictionary<string, string>>();

    if (lines.Count == 0)
        return rows;

    string[] headers;
    int startIndex = 0;

    if (!config.NoHeader)
    {
        headers = lines[0].Split(config.Delimiter).Select(h => h.Trim()).ToArray();
        startIndex = 1;
    }
    else
    {
        var firstRow = lines[0].Split(config.Delimiter);
        headers = Enumerable.Range(0, firstRow.Length)
                            .Select(i => i.ToString())
                            .ToArray();
    }

    for (int i = startIndex; i < lines.Count; i++)
    {
        var values = lines[i].Split(config.Delimiter);
        var dict = new Dictionary<string, string>();

        for (int j = 0; j < headers.Length; j++)
        {
            var value = j < values.Length ? values[j] : "";
            dict[headers[j]] = value.Trim();
        }

        rows.Add(dict);
    }

    return rows;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (config.SortFields.Count == 0)
        return rows;

    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    for (int i = 0; i < config.SortFields.Count; i++)
    {
        var field = config.SortFields[i];

        Func<Dictionary<string, string>, object> keySelector;

        if (field.Numeric)
        {
            keySelector = r => double.Parse(r[field.Name]);
        }
        else
        {
            keySelector = r => r[field.Name];
        }

        if (i == 0)
        {
            ordered = field.Descending
                ? rows.OrderByDescending(keySelector)
                : rows.OrderBy(keySelector);
        }
        else
        {
            ordered = field.Descending
                ? ordered!.ThenByDescending(keySelector)
                : ordered!.ThenBy(keySelector);
        }
    }

    return ordered!.ToList();
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
        return "";

    var lines = new List<string>();
    var headers = rows[0].Keys.ToList();

    if (!config.NoHeader)
    {
        lines.Add(string.Join(config.Delimiter, headers));
    }

    foreach (var row in rows)
    {
        var values = headers.Select(h => row.ContainsKey(h) ? row[h] : "");
        lines.Add(string.Join(config.Delimiter, values));
    }

    return string.Join(Environment.NewLine, lines);
}

void WriteOutput(string output, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile))
    {
        File.WriteAllText(config.OutputFile, output);
    }
    else
    {
        Console.WriteLine(output);
    }
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
