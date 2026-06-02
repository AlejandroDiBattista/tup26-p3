
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

using System;
using System.Collections.Generic;
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

try
{
    var config = ParseArgs(args);

    var input = ReadInput(config);

    var data = ParseDelimited(input, config);

    var sortedRows = SortRows(data.Rows, data.Header, config);

    var output = Serialize(sortedRows, data.Header, config);

    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;

    string delimiter = ",";

    bool noHeader = false;

    List<SortField> sortFields = new();

    int positional = 0;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-i":
            case "--input":

                inputFile = args[++i];
                break;

            case "-o":
            case "--output":

                outputFile = args[++i];
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

                var parts = args[++i].Split(':');

                string name = parts[0];

                bool numeric = false;
                bool descending = false;

                if (parts.Length > 1)
                {
                    if (parts[1] == "num")
                        numeric = true;
                }

                if (parts.Length > 2)
                {
                    if (parts[2] == "desc")
                        descending = true;
                }

                sortFields.Add(
                    new SortField(name, numeric, descending)
                );

                break;

            case "-h":
            case "--help":

                Console.WriteLine("Uso: sortx [input [output]] [options]");
                Environment.Exit(0);
                break;

            default:

                if (!args[i].StartsWith("-"))
                {
                    if (positional == 0)
                    {
                        inputFile = args[i];
                        positional++;
                    }
                    else if (positional == 1)
                    {
                        outputFile = args[i];
                        positional++;
                    }
                }

                break;
        }
    }

    return new AppConfig(
        inputFile,
        outputFile,
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

(string[]? Header, List<string[]> Rows) ParseDelimited(
    string input,
    AppConfig config
)
{
    var lines = input
        .Split('\n')
        .Select(x => x.Replace("\r", ""))
        .Where(x => x.Length > 0)
        .ToList();

    string[]? header = null;

    List<string[]> rows = new();

    if (!config.NoHeader)
    {
        header = lines[0]
            .Split(config.Delimiter);

        for (int i = 1; i < lines.Count; i++)
        {
            rows.Add(
                lines[i].Split(config.Delimiter)
            );
        }
    }
    else
    {
        for (int i = 0; i < lines.Count; i++)
        {
            rows.Add(
                lines[i].Split(config.Delimiter)
            );
        }
    }

    return (header, rows);
}

List<string[]> SortRows(
    List<string[]> rows,
    string[]? header,
    AppConfig config
)
{
    if (config.SortFields.Count == 0)
        return rows;

    var sorted = rows;

    for (int i = config.SortFields.Count - 1; i >= 0; i--)
    {
        var field = config.SortFields[i];

        int index = GetColumnIndex(
            field.Name,
            header,
            config.NoHeader
        );

        if (field.Numeric)
        {
            if (field.Descending)
            {
                sorted = sorted
                    .OrderByDescending(x =>
                    {
                        if (index < x.Length)
                        {
                            double.TryParse(
                                x[index],
                                out double n
                            );

                            return n;
                        }

                        return 0;
                    })
                    .ToList();
            }
            else
            {
                sorted = sorted
                    .OrderBy(x =>
                    {
                        if (index < x.Length)
                        {
                            double.TryParse(
                                x[index],
                                out double n
                            );

                            return n;
                        }

                        return 0;
                    })
                    .ToList();
            }
        }
        else
        {
            if (field.Descending)
            {
                sorted = sorted
                    .OrderByDescending(x =>
                    {
                        if (index < x.Length)
                            return x[index];

                        return "";
                    })
                    .ToList();
            }
            else
            {
                sorted = sorted
                    .OrderBy(x =>
                    {
                        if (index < x.Length)
                            return x[index];

                        return "";
                    })
                    .ToList();
            }
        }
    }

    return sorted;
}

int GetColumnIndex(
    string name,
    string[]? header,
    bool noHeader
)
{
    int number;

    if (int.TryParse(name, out number))
    {
        return number;
    }

    if (noHeader || header == null)
    {
        throw new Exception(
            $"No existe la columna '{name}'."
        );
    }

    for (int i = 0; i < header.Length; i++)
    {
        if (header[i] == name)
        {
            return i;
        }
    }

    throw new Exception(
        $"No existe la columna '{name}'."
    );
}

string Serialize(
    List<string[]> rows,
    string[]? header,
    AppConfig config
)
{
    List<string> lines = new();

    if (!config.NoHeader && header != null)
    {
        lines.Add(
            string.Join(config.Delimiter, header)
        );
    }

    foreach (var row in rows)
    {
        lines.Add(
            string.Join(config.Delimiter, row)
        );
    }

    return string.Join(
        Environment.NewLine,
        lines
    );
}

void WriteOutput(
    string text,
    AppConfig config
)
{
    if (config.OutputFile != null)
    {
        File.WriteAllText(
            config.OutputFile,
            text
        );
    }
    else
    {
        Console.WriteLine(text);
    }
}
