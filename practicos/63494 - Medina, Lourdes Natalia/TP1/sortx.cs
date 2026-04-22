using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

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
    Environment.Exit(1);
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
        var arg = args[i];

        if (arg == "-i" || arg == "--input")
            input = args[++i];

        else if (arg == "-o" || arg == "--output")
            output = args[++i];

        else if (arg == "-d" || arg == "--delimiter")
        {
            var val = args[++i];
            delimiter = val == "\\t" ? "\t" : val;
        }

        else if (arg == "-nh" || arg == "--no-header")
            noHeader = true;

        else if (arg == "-b" || arg == "--by")
            sortFields.Add(ParseSortField(args[++i]));

        else if (arg == "-h" || arg == "--help")
        {
            PrintHelp();
            Environment.Exit(0);
        }

        else if (arg.StartsWith("-"))
            throw new Exception($"Argumento inválido: {arg}");

        else
        {
            if (input == null) input = arg;
            else if (output == null) output = arg;
        }
    }

    if (!sortFields.Any())
        throw new Exception("Debe especificar al menos un criterio de orden (-b)");

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

string ReadInput(AppConfig config)
{
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        if (!File.Exists(config.InputFile))
            throw new Exception("El archivo de entrada no existe");

        return File.ReadAllText(config.InputFile);
    }

    if (!Console.IsInputRedirected)
        throw new Exception("No hay entrada por stdin");

    return Console.In.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    if (lines.Length == 0)
        return new();

    var table = new List<Dictionary<string, string>>();
    string[] headers;

    if (!config.NoHeader)
    {
        headers = lines[0].Split(config.Delimiter);

        foreach (var line in lines.Skip(1))
            table.Add(CreateRow(headers, line.Split(config.Delimiter)));
    }
    else
    {
        var first = lines[0].Split(config.Delimiter);
        headers = Enumerable.Range(0, first.Length)
                            .Select(i => i.ToString())
                            .ToArray();

        foreach (var line in lines)
            table.Add(CreateRow(headers, line.Split(config.Delimiter)));
    }

    return table;
}

Dictionary<string, string> CreateRow(string[] headers, string[] values)
{
    var row = new Dictionary<string, string>();

    for (int i = 0; i < headers.Length; i++)
        row[headers[i]] = i < values.Length ? values[i] : "";

    return row;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    foreach (var field in config.SortFields)
    {
        if (field.Numeric)
        {
            if (ordered == null)
            {
                ordered = field.Descending
                    ? rows.OrderByDescending(r =>
                    {
                        if (!double.TryParse(r[field.Name], NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                            throw new Exception($"Valor no numérico en campo {field.Name}");
                        return number;
                    })
                    : rows.OrderBy(r =>
                    {
                        if (!double.TryParse(r[field.Name], NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                            throw new Exception($"Valor no numérico en campo {field.Name}");
                        return number;
                    });
            }
            else
            {
                ordered = field.Descending
                    ? ordered.ThenByDescending(r =>
                    {
                        if (!double.TryParse(r[field.Name], NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                            throw new Exception($"Valor no numérico en campo {field.Name}");
                        return number;
                    })
                    : ordered.ThenBy(r =>
                    {
                        if (!double.TryParse(r[field.Name], NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                            throw new Exception($"Valor no numérico en campo {field.Name}");
                        return number;
                    });
            }
        }
        else
        {
            if (ordered == null)
            {
                ordered = field.Descending
                    ? rows.OrderByDescending(r => r[field.Name])
                    : rows.OrderBy(r => r[field.Name]);
            }
            else
            {
                ordered = field.Descending
                    ? ordered.ThenByDescending(r => r[field.Name])
                    : ordered.ThenBy(r => r[field.Name]);
            }
        }
    }

    return ordered?.ToList() ?? rows;
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (!rows.Any())
        return "";

    var sb = new StringBuilder();
    var headers = rows.First().Keys.ToList();

    if (!config.NoHeader)
        sb.AppendLine(string.Join(config.Delimiter, headers));

    foreach (var row in rows)
        sb.AppendLine(string.Join(config.Delimiter, headers.Select(h => row[h])));

    return sb.ToString();
}

void WriteOutput(string content, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile))
        File.WriteAllText(config.OutputFile, content);
    else
        Console.Write(content);
}

SortField ParseSortField(string input)
{
    var parts = input.Split(':');

    var name = parts[0];
    bool numeric = parts.Length > 1 && parts[1] == "num";
    bool descending = parts.Length > 2 && parts[2] == "desc";

    return new SortField(name, numeric, descending);
}

void PrintHelp()
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  sortx [input [output]] -b campo[:tipo[:orden]]");
    Console.WriteLine("Opciones:");
    Console.WriteLine("  -b, --by           Campo de ordenamiento");
    Console.WriteLine("  -i, --input        Archivo de entrada");
    Console.WriteLine("  -o, --output       Archivo de salida");
    Console.WriteLine("  -d, --delimiter    Delimitador (default ,)");
    Console.WriteLine("  -nh, --no-header   Sin encabezado");
    Console.WriteLine("  -h, --help         Mostrar ayuda");
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);