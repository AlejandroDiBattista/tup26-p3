using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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
    static int Main(string[] args)
    {
        try
        {
            var config = ParseArgs(args);
            var text = ReadInput(config);
            var parsed = ParseDelimited(text, config);
            var sorted = SortRows(parsed.rows, parsed.headers, config);
            var output = Serialize(parsed.headers, sorted, config);
            WriteOutput(output, config);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static AppConfig ParseArgs(string[] args)
    {
        string? input = null;
        string? output = null;
        string delimiter = ",";
        bool noHeader = false;
        var sortFields = new List<SortField>();
        var positional = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    Console.WriteLine(
@"sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
      [-i|--input input] [-o|--output output]
      [-d|--delimiter delimitador]
      [-nh|--no-header] [-h|--help]");
                    Environment.Exit(0);
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
                    delimiter = args[++i];
                    if (delimiter == "\\t")
                        delimiter = "\t";
                    break;

                case "-nh":
                case "--no-header":
                    noHeader = true;
                    break;

                case "-b":
                case "--by":
                    {
                        var parts = args[++i].Split(':');
                        string name = parts[0];
                        bool numeric = parts.Length > 1 &&
                                       parts[1].Equals("num", StringComparison.OrdinalIgnoreCase);
                        bool desc = parts.Length > 2 &&
                                    parts[2].Equals("desc", StringComparison.OrdinalIgnoreCase);

                        sortFields.Add(new SortField(name, numeric, desc));
                    }
                    break;

                default:
                    if (args[i].StartsWith("-"))
                        throw new Exception($"Opción desconocida: {args[i]}");

                    positional.Add(args[i]);
                    break;
            }
        }

        if (input == null && positional.Count > 0)
            input = positional[0];

        if (output == null && positional.Count > 1)
            output = positional[1];

        return new AppConfig(input, output, delimiter, noHeader, sortFields);
    }

    static string ReadInput(AppConfig config)
    {
        return config.InputFile == null
            ? Console.In.ReadToEnd()
            : File.ReadAllText(config.InputFile);
    }

    static (List<string> headers, List<Dictionary<string, string>> rows)
        ParseDelimited(string text, AppConfig config)
    {
        var lines = text.Replace("\r\n", "\n")
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
            return (new List<string>(), new List<Dictionary<string, string>>());

        List<string> headers;
        int startIndex;

        if (config.NoHeader)
        {
            int count = lines[0].Split(config.Delimiter).Length;
            headers = Enumerable.Range(0, count)
                                .Select(x => x.ToString())
                                .ToList();
            startIndex = 0;
        }
        else
        {
            headers = lines[0].Split(config.Delimiter).ToList();
            startIndex = 1;
        }

        var rows = new List<Dictionary<string, string>>();

        for (int i = startIndex; i < lines.Length; i++)
        {
            var values = lines[i].Split(config.Delimiter);
            var row = new Dictionary<string, string>();

            for (int c = 0; c < headers.Count; c++)
            {
                row[headers[c]] = c < values.Length ? values[c] : "";
            }

            rows.Add(row);
        }

        return (headers, rows);
    }

    static List<Dictionary<string, string>> SortRows(
        List<Dictionary<string, string>> rows,
        List<string> headers,
        AppConfig config)
    {
        foreach (var field in config.SortFields)
        {
            if (!headers.Contains(field.Name))
                throw new Exception($"Campo inexistente: {field.Name}");
        }

        IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

        foreach (var field in config.SortFields)
        {
            if (field.Numeric)
            {
                Func<Dictionary<string, string>, double> selector = r =>
                {
                    double.TryParse(
                        r[field.Name],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double value);

                    return value;
                };

                ordered = ordered == null
                    ? (field.Descending
                        ? rows.OrderByDescending(selector)
                        : rows.OrderBy(selector))
                    : (field.Descending
                        ? ordered.ThenByDescending(selector)
                        : ordered.ThenBy(selector));
            }
            else
            {
                Func<Dictionary<string, string>, string> selector =
                    r => r[field.Name];

                ordered = ordered == null
                    ? (field.Descending
                        ? rows.OrderByDescending(selector)
                        : rows.OrderBy(selector))
                    : (field.Descending
                        ? ordered.ThenByDescending(selector)
                        : ordered.ThenBy(selector));
            }
        }

        return (ordered ?? rows.OrderBy(x => 0)).ToList();
    }

    static string Serialize(
        List<string> headers,
        List<Dictionary<string, string>> rows,
        AppConfig config)
    {
        var output = new List<string>();

        if (!config.NoHeader)
            output.Add(string.Join(config.Delimiter, headers));

        foreach (var row in rows)
        {
            output.Add(string.Join(
                config.Delimiter,
                headers.Select(h => row[h])));
        }

        return string.Join(Environment.NewLine, output);
    }

    static void WriteOutput(string text, AppConfig config)
    {
        if (config.OutputFile == null)
            Console.Write(text);
        else
            File.WriteAllText(config.OutputFile, text);
    }
}