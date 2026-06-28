// '''''
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

try
{
    var config = ParseArgs(args);
    var input = ReadInput(config);
    var parsed = ParseDelimited(input, config);
    var sorted = SortRows(parsed.Rows, config);
    var output = Serialize(parsed.Header, sorted, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] arguments)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();
    var positionalArgs = new List<string>();

    for (int i = 0; i < arguments.Length; i++)
    {
        string arg = arguments[i];
        if (arg == "-h" || arg == "--help")
        {
            Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...");
            Console.WriteLine("Opciones:");
            Console.WriteLine("  -b, --by <campo>      Campo por el que ordenar (ej: salario:num:desc)");
            Console.WriteLine("  -i, --input <archivo> Archivo de entrada");
            Console.WriteLine("  -o, --output <arch.>  Archivo de salida");
            Console.WriteLine("  -d, --delimiter <del> Carácter delimitador (por defecto: ',')");
            Console.WriteLine("  -nh, --no-header      Indica que no hay encabezado");
            Console.WriteLine("  -h, --help            Muestra esta ayuda y termina");
            Environment.Exit(0);
        }
        else if (arg == "-i" || arg == "--input") inputFile = arguments[++i];
        else if (arg == "-o" || arg == "--output") outputFile = arguments[++i];
        else if (arg == "-d" || arg == "--delimiter")
        {
            delimiter = arguments[++i];
            if (delimiter == "\\t") delimiter = "\t";
        }
        else if (arg == "-nh" || arg == "--no-header") noHeader = true;
        else if (arg == "-b" || arg == "--by")
        {
            string byArg = arguments[++i];
            var parts = byArg.Split(':');
            string name = parts[0];
            bool numeric = parts.Length > 1 && parts[1] == "num";
            bool descending = parts.Length > 2 && parts[2] == "desc";
            sortFields.Add(new SortField(name, numeric, descending));
        }
        else if (arg.StartsWith("-"))
        {
            throw new Exception($"Opción desconocida: {arg}");
        }
        else
        {
            positionalArgs.Add(arg);
        }
    }

    if (inputFile == null && positionalArgs.Count > 0) inputFile = positionalArgs[0];
    if (outputFile == null && positionalArgs.Count > 1) outputFile = positionalArgs[1];

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}

string ReadInput(AppConfig config)
{
    if (string.IsNullOrEmpty(config.InputFile))
    {
        // Si no se especificó un archivo, leemos de la entrada estándar (stdin)
        return Console.In.ReadToEnd();
    }
    return File.ReadAllText(config.InputFile);
}

(string[] Header, List<Dictionary<string, string>> Rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToArray();

    if (lines.Length == 0) return (Array.Empty<string>(), new List<Dictionary<string, string>>());

    string[] header;
    int startIndex = 0;

    if (config.NoHeader)
    {
        int colCount = lines[0].Split(config.Delimiter).Length;
        // Los encabezados simulados son los índices (0, 1, 2...)
        header = Enumerable.Range(0, colCount).Select(i => i.ToString()).ToArray();
    }
    else
    {
        header = lines[0].Split(config.Delimiter);
        startIndex = 1;
    }

    var rows = new List<Dictionary<string, string>>();
    for (int i = startIndex; i < lines.Length; i++)
    {
        var values = lines[i].Split(config.Delimiter);
        var row = new Dictionary<string, string>();
        for (int j = 0; j < header.Length; j++)
        {
            row[header[j]] = j < values.Length ? values[j] : "";
        }
        rows.Add(row);
    }

    return (header, rows);
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (config.SortFields.Count == 0) return rows;

    Comparison<Dictionary<string, string>> comparison = (a, b) =>
    {
        foreach (var field in config.SortFields)
        {
            if (!a.TryGetValue(field.Name, out var valA) || !b.TryGetValue(field.Name, out var valB))
                throw new Exception($"Columna '{field.Name}' no encontrada para ordenar.");

            int cmp = 0;
            if (field.Numeric)
            {
                // Se parsea en cultura invariante para evitar problemas con símbolos locales
                double numA = double.TryParse(valA, NumberStyles.Any, CultureInfo.InvariantCulture, out var na) ? na : double.MinValue;
                double numB = double.TryParse(valB, NumberStyles.Any, CultureInfo.InvariantCulture, out var nb) ? nb : double.MinValue;
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
        return 0; // Son idénticos en todos los criterios evaluados
    };

    var rowsArray = rows.ToArray();
    Array.Sort(rowsArray, comparison);
    return rowsArray.ToList();
}

string Serialize(string[] header, List<Dictionary<string, string>> rows, AppConfig config)
{
    var lines = new List<string>();

    if (!config.NoHeader && header.Length > 0)
    {
        lines.Add(string.Join(config.Delimiter, header));
    }

    foreach (var row in rows)
    {
        var values = header.Select(h => row.ContainsKey(h) ? row[h] : "").ToArray();
        lines.Add(string.Join(config.Delimiter, values));
    }

    return string.Join(Environment.NewLine, lines);
}

void WriteOutput(string text, AppConfig config)
{
    if (string.IsNullOrEmpty(config.OutputFile))
    {
        Console.Write(text + Environment.NewLine);
    }
    else
    {
        File.WriteAllText(config.OutputFile, text + Environment.NewLine);
    }
}

// Configuración inmutable mediante Records
record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(string? InputFile, string? OutputFile, string Delimiter, bool NoHeader, List<SortField> SortFields);