using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

try
{
    var config = ParseArgs(args);
    if (config == null) return 0;

    string inputText = ReadInput(config);
    var (header, rows) = ParseDelimited(inputText, config);
    var sortedRows = SortRows(rows, config);
    string outputText = Serialize(header, sortedRows, config);
    WriteOutput(outputText, config);

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

AppConfig? ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();
    int positionalIndex = 0;

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];

        if (arg == "-h" || arg == "--help")
        {
            Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]]... [-i|--input] [-o|--output] [-d|--delimiter] [-nh|--no-header] [-h|--help]");
            return null;
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            string d = args[++i];
            delimiter = d == "\\t" ? "\t" : d;
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            noHeader = true;
        }
        else if (arg == "-i" || arg == "--input")
        {
            input = args[++i];
        }
        else if (arg == "-o" || arg == "--output")
        {
            output = args[++i];
        }
        else if (arg == "-b" || arg == "--by")
        {
            string[] parts = args[++i].Split(':');
            string name = parts[0];
            bool isNum = parts.Length > 1 && parts[1].ToLower() == "num";
            bool isDesc = parts.Length > 2 && parts[2].ToLower() == "desc";
            sortFields.Add(new SortField(name, isNum, isDesc));
        }
        else if (!arg.StartsWith("-"))
        {
            // Resolución de argumentos posicionales
            if (positionalIndex == 0 && input == null) input = arg;
            else if (positionalIndex == 1 && output == null) output = arg;
            positionalIndex++;
        }
    }
    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}
    string ReadInput(AppConfig config)
{
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        if (!File.Exists(config.InputFile))
            throw new Exception($"El archivo de entrada no existe: {config.InputFile}");
        return File.ReadAllText(config.InputFile);
    }

    // Si no hay archivo definido por argumentos, intentamos leer desde la consola (stdin)
    if (Console.IsInputRedirected)
        return Console.In.ReadToEnd();

    throw new Exception("No se especificó archivo de entrada ni se detectó un flujo de texto en stdin.");
}

(string[]? Header, List<Dictionary<string, string>> Rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    if (lines.Length == 0) return (null, new List<Dictionary<string, string>>());

    string[]? header = null;
    int startRow = 0;

    if (!config.NoHeader)
    {
        header = lines[0].Split(config.Delimiter);
        startRow = 1; 
    }

    var rows = new List<Dictionary<string, string>>();
    for (int i = startRow; i < lines.Length; i++)
    {
        var values = lines[i].Split(config.Delimiter);
        var rowDict = new Dictionary<string, string>();

        for (int j = 0; j < values.Length; j++)
        {
            string key = config.NoHeader ? j.ToString() : header![j];
            rowDict[key] = values[j];
        }
        rows.Add(rowDict);
    }

    return (header, rows);
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (config.SortFields.Count == 0 || rows.Count == 0) return rows;

    IOrderedEnumerable<Dictionary<string, string>>? orderedRows = null;
    bool isFirstCriteria = true;

    foreach (var field in config.SortFields)
    {
        if (field.Numeric)
        {
            double GetNumVal(Dictionary<string, string> r)
            {
                if (!r.TryGetValue(field.Name, out string? val))
                    throw new Exception($"Columna inexistente: {field.Name}");
                return double.TryParse(val, out double num) ? num : 0;
            }

            if (isFirstCriteria)
                orderedRows = field.Descending ? rows.OrderByDescending(GetNumVal) : rows.OrderBy(GetNumVal);
            else
                orderedRows = field.Descending ? orderedRows!.ThenByDescending(GetNumVal) : orderedRows!.ThenBy(GetNumVal);
        }
        else
        {
            string GetAlphaVal(Dictionary<string, string> r)
            {
                if (!r.TryGetValue(field.Name, out string? val))
                    throw new Exception($"Columna inexistente: {field.Name}");
                return val;
            }

            if (isFirstCriteria)
                orderedRows = field.Descending ? rows.OrderByDescending(GetAlphaVal) : rows.OrderBy(GetAlphaVal);
            else
                orderedRows = field.Descending ? orderedRows!.ThenByDescending(GetAlphaVal) : orderedRows!.ThenBy(GetAlphaVal);
        }
        isFirstCriteria = false;
    }

    return orderedRows?.ToList() ?? rows;
}
string Serialize(string[]? header, List<Dictionary<string, string>> rows, AppConfig config)
{
    var sw = new StringWriter();

    if (header != null)
        sw.WriteLine(string.Join(config.Delimiter, header));

    foreach (var row in rows)
    {
        var values = new List<string>();
        int colCount = header != null ? header.Length : row.Count;

        for (int i = 0; i < colCount; i++)
        {
            string key = header != null ? header[i] : i.ToString();
            values.Add(row.TryGetValue(key, out var val) ? val : "");
        }
        sw.WriteLine(string.Join(config.Delimiter, values));
    }

    return sw.ToString();
}
void WriteOutput(string outputText, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile))
    {
        File.WriteAllText(config.OutputFile, outputText);
    }
    else
    {
        // Si no hay archivo de salida, se imprime por consola
        Console.Write(outputText);
    }
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields);
    