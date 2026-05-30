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

            if (i + 1 >= args.Length)
            throw new Exception("Falta valor para -b");

            var field = args[++i];

            var parts = field.Split(':');

            string campo = parts[0];
            string tipo = parts.Length > 1 ? parts[1] : "alpha";
            string orden = parts.Length > 2 ? parts[2] : "asc";

            sortOptions.Add(new SortOption(campo, tipo, orden));
            break;

            case "-d":
            case "--delimiter":
            if (i + 1 >= args.Length)
            throw new Exception("Falta valor para -d");
                delimiter = args[++i];
            break;

            case "-nh":
            case "--no-header":
                noHeader = true;
            break;

            case "-i":
            case "--input":  
            if (i + 1 >= args.Length)
            throw new Exception("Falta valor para -i");
                inputFile = args[++i];
            break;

            case "-o":
            case "--output":
            if (i + 1 >= args.Length)
            throw new Exception("Falta valor para -o");
                outputFile = args[++i];
            break;
            
            case "-h":
            case "--help":

            Console.WriteLine("""
            sortx [input [output]]
                [-b|--by campo[:tipo[:orden]]]
                [-i|--input archivo]
                [-o|--output archivo]
                [-d|--delimiter delimitador]
                [-nh|--no-header]
                [-h|--help]
            """);

            Environment.Exit(0);

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
            row[headers[j]] = j < values.Length? values[j]: "";
        }
        rows.Add(row);
    }
    return rows;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows,AppConfig config)
{
    if (rows.Count == 0)
    return rows;
    
    if (config.SortOptions.Count == 0)
        return rows;

    foreach(var sort in config.SortOptions)
    {
        if (!rows[0].ContainsKey(sort.Campo))
            throw new Exception($"Campo inexistente: {sort.Campo}");
    }

    IOrderedEnumerable<Dictionary<string,string>>? ordered = null;

    foreach(var sort in config.SortOptions)
    {
        bool numeric = sort.Tipo == "num";
        bool desc = sort.Orden == "desc";

        if (ordered == null)
        {
            if (numeric)
            {
                ordered = desc
                    ? rows.OrderByDescending(r => double.Parse(r[sort.Campo]))
                    : rows.OrderBy(r => double.Parse(r[sort.Campo]));
            }
            else
            {
                ordered = desc
                    ? rows.OrderByDescending(r => r[sort.Campo])
                    : rows.OrderBy(r => r[sort.Campo]);
            }
        }
        else
        {
            if (numeric)
            {
                ordered = desc
                    ? ordered.ThenByDescending(r => double.Parse(r[sort.Campo]))
                    : ordered.ThenBy(r => double.Parse(r[sort.Campo]));
            }
            else
            {
                ordered = desc
                    ? ordered.ThenByDescending(r => r[sort.Campo])
                    : ordered.ThenBy(r => r[sort.Campo]);
            }
        }
    }

    return ordered!.ToList();
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
    {
        return "";
    }
    var lines = new List<string>();
    var headers = rows[0].Keys.ToArray();
    if (!config.NoHeader)
{
    lines.Add(string.Join(config.Delimiter, headers));
}

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
