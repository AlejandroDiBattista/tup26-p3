using System;
using System.Collections.Generic;
using System.IO;

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);

class Program
{
    static void Main(string[] args)
    {
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
            Console.Error.WriteLine(ex.Message);
        }
    }

 static AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        if (arg == "-i" || arg == "--input")
        {
            if (i + 1 < args.Length) input = args[++i];
        }
        else if (arg == "-o" || arg == "--output")
        {
            if (i + 1 < args.Length) output = args[++i];
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            if (i + 1 < args.Length) delimiter = args[++i];
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            noHeader = true;
        }
        else if (arg == "-b" || arg == "--by")
        {
            if (i + 1 < args.Length)
            {
                var spec = args[++i];
                var parts = spec.Split(':');
                var name = parts[0];
                bool numeric = parts.Length > 1 && parts[1] == "num";
                bool descending = parts.Length > 2 && parts[2] == "desc";
                sortFields.Add(new SortField(name, numeric, descending));
            }
        }
        else if (arg == "-h" || arg == "--help")
        {
            Console.WriteLine("Uso: sortx [input [output]] [-b campo[:tipo[:orden]]] [-d delimitador] [-nh] [-h]");
            Environment.Exit(0);
        }
        else
        {
            // Argumentos posicionales
            if (input == null) input = arg;
            else if (output == null) output = arg;
        }
    }

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

   static string ReadInput(AppConfig config)
{
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        return File.ReadAllText(config.InputFile);
    }
    else
    {
        return Console.In.ReadToEnd();
    }
}

   static List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config)
{
    var rows = new List<Dictionary<string, string>>();
    var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    if (lines.Length == 0)
        return rows;

    var delimiter = config.Delimiter;
    string[] headers;

    int startIndex = 0;

    if (!config.NoHeader)
    {
        headers = lines[0].Trim().Split(delimiter);
        startIndex = 1;
    }
    else
    {
        var firstLine = lines[0].Trim().Split(delimiter);
        headers = new string[firstLine.Length];

        for (int i = 0; i < headers.Length; i++)
            headers[i] = i.ToString();
    }

    for (int i = startIndex; i < lines.Length; i++)
    {
        var values = lines[i].Trim().Split(delimiter);
        var dict = new Dictionary<string, string>();

        for (int j = 0; j < headers.Length && j < values.Length; j++)
        {
            dict[headers[j]] = values[j];
        }

        rows.Add(dict);
    }

    return rows;
}

    static List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (config.SortFields.Count == 0)
        return rows;

    rows.Sort((a, b) =>
    {
        foreach (var field in config.SortFields)
        {
            var valA = a.ContainsKey(field.Name) ? a[field.Name] : "";
            var valB = b.ContainsKey(field.Name) ? b[field.Name] : "";

            int result;
            if (field.Numeric)
            {
                double numA = double.TryParse(valA, out var na) ? na : 0;
                double numB = double.TryParse(valB, out var nb) ? nb : 0;
                result = numA.CompareTo(numB);
            }
            else
            {
                result = string.Compare(valA, valB, StringComparison.OrdinalIgnoreCase);
            }

            if (field.Descending) result = -result;

            if (result != 0) return result; // si hay diferencia, devuelve
        }

        return 0; // todos iguales
    });

    return rows;
}

    static string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
        return "";

    var delimiter = config.Delimiter;
    var headers = new List<string>(rows[0].Keys);

    var lines = new List<string>();

    if (!config.NoHeader)
    {
        lines.Add(string.Join(delimiter, headers));
    }

    foreach (var row in rows)
    {
        var values = new List<string>();

        foreach (var h in headers)
        {
            values.Add(row.ContainsKey(h) ? row[h] : "");
        }

        lines.Add(string.Join(delimiter, values));
    }

    return string.Join("\n", lines);
}

   static void WriteOutput(string text, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile))
    {
        File.WriteAllText(config.OutputFile, text);
    }
    else
    {
        Console.WriteLine(text);
    }
}
}