using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

try
{
    var config = ParseArgs(args);
    string input = ReadInput(config.InputFile);
    var rows = ParseDelimited(input, config.Delimiter, config.NoHeader, out var headers);
    var sortedRows = SortRows(rows, config.SortFields);
    string output = Serialize(sortedRows, headers, config.Delimiter, config.NoHeader);
    WriteOutput(config.OutputFile, output);
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
    var sortFields = new List<SortField>();
    var positionalArgs = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h":
            case "--help":
                PrintHelp();
                Environment.Exit(0);
                break;
            case "-nh":
            case "--no-header":
                noHeader = true;
                break;
            case "-d":
            case "--delimiter":
                if (i + 1 >= args.Length) throw new Exception("Falta valor para el delimitador.");
                delimiter = args[++i];
                if (delimiter == "\\t") delimiter = "\t";
                break;
            case "-i":
            case "--input":
                if (i + 1 >= args.Length) throw new Exception("Falta valor para input.");
                input = args[++i];
                break;
            case "-o":
            case "--output":
                if (i + 1 >= args.Length) throw new Exception("Falta valor para output.");
                output = args[++i];
                break;
            case "-b":
            case "--by":
                if (i + 1 >= args.Length) throw new Exception("Falta valor para --by.");
                string[] parts = args[++i].Split(':');
                string name = parts[0];
                bool numeric = parts.Length > 1 && parts[1].ToLower() == "num";
                bool descending = parts.Length > 2 && parts[2].ToLower() == "desc";
                sortFields.Add(new SortField(name, numeric, descending));
                break;
            default:
                positionalArgs.Add(args[i]);
                break;
        }
    }

    if (input == null && positionalArgs.Count > 0) input = positionalArgs[0];
    if (output == null && positionalArgs.Count > 1) output = positionalArgs[1];

    return new AppConfig(input, output, delimiter, noHeader, sortFields);

    void PrintHelp()
    {
        Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
        Console.WriteLine("Opciones:");
        Console.WriteLine("  -b, --by          Campo por el que ordenar (ej. salario:num:desc).");
        Console.WriteLine("  -i, --input       Archivo de entrada (default: stdin).");
        Console.WriteLine("  -o, --output      Archivo de salida (default: stdout).");
        Console.WriteLine("  -d, --delimiter   Carácter delimitador (default: ','). Usar '\\t' para tabulación.");
        Console.WriteLine("  -nh, --no-header  Indica que el archivo no tiene fila de encabezado.");
        Console.WriteLine("  -h, --help        Muestra esta ayuda.");
    }
}
string ReadInput(string? inputFile)
{
    if (!string.IsNullOrWhiteSpace(inputFile))
    {
        if (!File.Exists(inputFile)) throw new Exception($"El archivo de entrada no existe: {inputFile}");
        return File.ReadAllText(inputFile);
    }

    if (Console.IsInputRedirected)
    {
        return Console.In.ReadToEnd();
    }

    throw new Exception("No se especificó un archivo de entrada y no hay datos en stdin.");
}
List<Dictionary<string, string>> ParseDelimited(string text, string delimiter, bool noHeader, out List<string> headers)
{
    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    if (lines.Length == 0) throw new Exception("El archivo de entrada está vacío.");

    headers = new List<string>();
    var rows = new List<Dictionary<string, string>>();
    int startIndex = 0;

    var firstLineParts = lines[0].Split(delimiter);

    if (!noHeader)
    {
        headers.AddRange(firstLineParts);
        startIndex = 1;
    }
    else
    {
        for (int i = 0; i < firstLineParts.Length; i++)
        {
            headers.Add(i.ToString());
        }
    }

    for (int i = startIndex; i < lines.Length; i++)
    {
        var parts = lines[i].Split(delimiter);
        var row = new Dictionary<string, string>();
        for (int j = 0; j < headers.Count; j++)
        {
            row[headers[j]] = j < parts.Length ? parts[j] : string.Empty;
        }
        rows.Add(row);
    }

    return rows;
}
List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, List<SortField> fields)
{
    if (fields == null || fields.Count == 0) return rows;

    rows.Sort((a, b) =>
    {
        foreach (var field in fields)
        {
            if (!a.TryGetValue(field.Name, out string? valA)) throw new Exception($"Columna inexistente: {field.Name}");
            if (!b.TryGetValue(field.Name, out string? valB)) throw new Exception($"Columna inexistente: {field.Name}");

            int cmp;
            if (field.Numeric)
            {
                double numA = double.TryParse(valA, out double da) ? da : 0;
                double numB = double.TryParse(valB, out double db) ? db : 0;
                cmp = numA.CompareTo(numB);
            }
            else
            {
                cmp = string.Compare(valA, valB, StringComparison.OrdinalIgnoreCase);
            }

            if (cmp != 0)
            {
                return field.Descending ? -cmp : cmp;
            }
        }
        return 0;
    });

    return rows;
}
string Serialize(List<Dictionary<string, string>> rows, List<string> headers, string delimiter, bool noHeader)
{
    using var writer = new StringWriter();

    if (!noHeader)
    {
        writer.WriteLine(string.Join(delimiter, headers));
    }

    foreach (var row in rows)
    {
        var lineValues = headers.Select(h => row.ContainsKey(h) ? row[h] : string.Empty);
        writer.WriteLine(string.Join(delimiter, lineValues));
    }

    return writer.ToString();
}
void WriteOutput(string? outputFile, string content)
{
    if (!string.IsNullOrWhiteSpace(outputFile))
    {
        File.WriteAllText(outputFile, content);
    }
    else
    {
        Console.Out.Write(content);
    }
}
record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields);