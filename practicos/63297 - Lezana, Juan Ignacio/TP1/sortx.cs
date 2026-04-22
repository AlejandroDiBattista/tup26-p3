using System;
using System.Collections.Generic;
using System.IO;

try
{
    var config = ParseArgs(args);
    var text = ReadInput(config);
    var rows = ParseDelimited(text, config);
    var sorted = SortRows(rows, config);
    var output = Serialize(sorted, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}

AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
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
                delimiter = args[++i] == "\\t" ? "\t" : args[i];
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-b":
            case "--by":
                var parts = args[++i].Split(':');
                string name = parts[0];
                bool numeric = parts.Length > 1 && parts[1] == "num";
                bool desc = parts.Length > 2 && parts[2] == "desc";

                sortFields.Add(new SortField(name, numeric, desc));
                break;
        }
    }

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

string ReadInput(AppConfig config)
{
    if (config.InputFile != null)
        return File.ReadAllText(config.InputFile);

    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config)
{
    var rows = new List<Dictionary<string, string>>();
    var lines = text.Split('\n');

    string[] headers;

    if (!config.NoHeader)
    {
        headers = lines[0].Trim().Split(config.Delimiter);
    }
    else
    {
        var first = lines[0].Split(config.Delimiter);
        headers = new string[first.Length];

        for (int i = 0; i < first.Length; i++)
            headers[i] = i.ToString();
    }

    int start = config.NoHeader ? 0 : 1;

    for (int i = start; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i])) continue;

        var values = lines[i].Trim().Split(config.Delimiter);
        var dict = new Dictionary<string, string>();

        for (int j = 0; j < headers.Length; j++)
            dict[headers[j]] = values[j];

        rows.Add(dict);
    }

    return rows;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    rows.Sort((a, b) =>
    {
        foreach (var field in config.SortFields)
        {
            int result;

            if (field.Numeric)
            {
                double va = double.Parse(a[field.Name]);
                double vb = double.Parse(b[field.Name]);
                result = va.CompareTo(vb);
            }
            else
            {
                result = string.Compare(a[field.Name], b[field.Name]);
            }

            if (result != 0)
                return field.Descending ? -result : result;
        }

        return 0;
    });

    return rows;
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0) return "";

    var headers = new List<string>();

    foreach (var key in rows[0].Keys)
        headers.Add(key);

    string result = "";

    if (!config.NoHeader)
        result += string.Join(config.Delimiter, headers) + "\n";

    foreach (var row in rows)
    {
        string line = "";

        for (int i = 0; i < headers.Count; i++)
        {
            line += row[headers[i]];
            if (i < headers.Count - 1)
                line += config.Delimiter;
        }

        result += line + "\n";
    }

    return result;
}

void WriteOutput(string output, AppConfig config)
{
    if (config.OutputFile != null)
        File.WriteAllText(config.OutputFile, output);
    else
        Console.WriteLine(output);
}
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields
);