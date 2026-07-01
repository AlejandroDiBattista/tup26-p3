
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

try
{
    var config = ParseArgs(args);
    var input  = ReadInput(config);
    var rows   = ParseDelimited(input, config);
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
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    List<SortField> sortFields = [];

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
                delimiter = args[++i];

                if (delimiter == "\\t")
                {
                    delimiter = "\t";
                }

                break;

            case "-h":
            case "--help":
                Console.WriteLine("""
sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
      [-i|--input input]
      [-o|--output output]
      [-d|--delimiter delimitador]
      [-nh|--no-header]
      [-h|--help]
""");

                Environment.Exit(0);
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-b":
            case "--by":
            {
                string[] partes = args[++i].Split(':');

                string campo = partes[0];
                bool numeric = partes.Contains("num");
                bool descending = partes.Contains("desc");

                sortFields.Add(
                    new SortField(
                        campo,
                        numeric,
                        descending
                    )
                );

                break;
            }

            default:
                if (!args[i].StartsWith("-"))
                {
                    if (input is null)
                        input = args[i];
                    else if (output is null)
                        output = args[i];
                }
                break;
        }
    }

    return new AppConfig(
        input,
        output,
        delimiter,
        noHeader,
        sortFields
    );
}




string ReadInput(AppConfig config)
{
    if (config.InputFile != null)
    {
        return File.ReadAllText(config.InputFile);
    }

    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(
    string input,
    AppConfig config)
{
    List<Dictionary<string, string>> rows = [];

    string[] lines = input.Split(
        '\n',
        StringSplitOptions.RemoveEmptyEntries);

    if (lines.Length == 0)
        return rows;

    string[] headers;
    int startRow;

    if (config.NoHeader)
    {
        string[] firstValues =
            lines[0].Trim().Split(config.Delimiter);

        headers = new string[firstValues.Length];

        for (int i = 0; i < firstValues.Length; i++)
        {
            headers[i] = i.ToString();
        }

        startRow = 0;
    }
    else
    {
        headers =
            lines[0].Trim().Split(config.Delimiter);

        startRow = 1;
    }

    for (int i = startRow; i < lines.Length; i++)
    {
        string[] values =
            lines[i].Trim().Split(config.Delimiter);

        Dictionary<string, string> row = [];

        for (int j = 0;
             j < headers.Length && j < values.Length;
             j++)
        {
            row[headers[j]] = values[j];
        }

        rows.Add(row);
    }

    return rows;
}

List<Dictionary<string, string>> SortRows(
    List<Dictionary<string, string>> rows,
    AppConfig config)
{
    if (rows.Count > 0)
    {
        foreach (var field in config.SortFields)
        {
            if (!rows[0].ContainsKey(field.Name))
            {
                throw new Exception(
                    $"La columna '{field.Name}' no existe.");
            }
        }
    }

    if (config.SortFields.Count == 0)
        return rows;

    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    foreach (var field in config.SortFields)
    {
        if (ordered == null)
        {
            if (field.Numeric)
            {
                ordered = field.Descending
                    ? rows.OrderByDescending(r => double.Parse(r[field.Name]))
                    : rows.OrderBy(r => double.Parse(r[field.Name]));
            }
            else
            {
                ordered = field.Descending
                    ? rows.OrderByDescending(r => r[field.Name])
                    : rows.OrderBy(r => r[field.Name]);
            }
        }
        else
        {
            if (field.Numeric)
            {
                ordered = field.Descending
                    ? ordered.ThenByDescending(r => double.Parse(r[field.Name]))
                    : ordered.ThenBy(r => double.Parse(r[field.Name]));
            }
            else
            {
                ordered = field.Descending
                    ? ordered.ThenByDescending(r => r[field.Name])
                    : ordered.ThenBy(r => r[field.Name]);
            }
        }
    }

    return ordered!.ToList();
}
string Serialize(
    List<Dictionary<string, string>> rows,
    AppConfig config)
{
    if (rows.Count == 0)
        return "";

    List<string> lines = [];

    if (!config.NoHeader)
    {
        lines.Add(
            string.Join(
                config.Delimiter,
                rows[0].Keys
            )
        );
    }

    foreach (var row in rows)
    {
        lines.Add(
            string.Join(
                config.Delimiter,
                row.Values
            )
        );
    }

    return string.Join("\n", lines);
}

void WriteOutput(string output, AppConfig config)
{
    if (config.OutputFile != null)
    {
        File.WriteAllText(config.OutputFile, output);
    }
    else
    {
        Console.Write(output);
    }
}



record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields
);
