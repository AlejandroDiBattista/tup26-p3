using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

try
{
    var config = ParseArgs(args);
    var InputText = ReadInput(config);
    var rows = ParseDelimited(InputText, config);
    var sortedRows = SortRows(rows, config);
    var outputText = Serialize(sortedRows, config);
    WriteOutput(outputText, config);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Environment.Exit(1);
}
AppConfig ParseArgs(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;

    var sortOptions = new List<SortOption>();

    int positionalCount = 0;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-b":
            case "--by":
                var field = args[++i];

                sortOptions.Add(
                    new SortOption(field, "alpha", "asc")
                );
            break;

            case "-d":
            case "--delimiter":
         
                delimiter = args[++i];
            break;

            case "-nh":
            case "--no-header":
                noHeader = true;
            break;

            case "-i":
            case "--input":
                
                inputFile = args[++i];
            break;

            case "-o":
            case "--output":

                outputFile = args[++i];
            break;

            default:
                if (!args[i].StartsWith("-"))
                {
                    if(positionalCount == 0)
                    {
                        inputFile = args [i];
                    }
                    else if(positionalCount == 1)
                    {
                        outputFile = args [i];
                    }
                    positionalCount++;
                }

            break;
        }
    }
    return new AppConfig(
        inputFile,
        outputFile,
        delimiter,
        noHeader,
        sortOptions
    );    
}

string ReadInput(AppConfig config)
{
    if (config.Input != null)
    {
        return File.ReadAllText(config.Input);
    }
    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string inputText, AppConfig config)
{
    var rows = new List<Dictionary<string, string>>();

    var lines = inputText.Split(
        Environment.NewLine, StringSplitOptions.RemoveEmptyEntries
    );

    if (lines.Length == 0)
    {
        return rows;
    }
    string[] headers;
    int startRow;

    if (config.NoHeader)
    {
        var firstRow = lines[0].Split(config.Delimiter);
        headers = new string[firstRow.Length];
        for (int i=0; i < firstRow.Length; i++)
        {
            headers[i] = i.ToString();
        }
        startRow = 0; 
    }
    else
    {
        headers = lines[0].Split(config.Delimiter);
        startRow = 1;
    }
    for (int i = startRow; i < lines.Length; i++)
    {
        var values = lines[i].Split(config.Delimiter);
        var row = new Dictionary<string, string>();
        for (int j=0; j < headers.Length; j++)
        {
            row[headers[j]] = values[j];
        }
        rows.Add(row);
    }
    return rows;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (config.SortOptions.Count == 0)
    {
        return rows;
    }
    var sortField = config.SortOptions[0];

    return rows.OrderBy(row => row[sortField.Campo]).ToList();
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
    {
        return "";
    }
    var lines = new List<string>();
    var headers = rows[0].Keys.ToArray();
    lines.Add(string.Join(config.Delimiter, headers));

    foreach (var row in rows)
    {
        var values = headers.Select(h => row[h]);
        lines.Add(string.Join(config.Delimiter, values));
    }
    return string.Join(Environment.NewLine, lines);
}

void WriteOutput(string outputText, AppConfig config)
{
    if (!string.IsNullOrWhiteSpace(config.Output))
    {
        File.WriteAllText(
            config.Output,
            outputText
        );
    }
    else
    {
        Console.Write(outputText);
    }
}
record SortOption(string Campo, string Tipo, string Orden);

record AppConfig(
    string? Input,
    string? Output,
    string Delimiter,
    bool NoHeader,
    List<SortOption> SortOptions
);
