using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// --- 1. PUNTO DE ENTRADA (PIPELINE) ---
try
{
    var config = ParseArgs(args);
    if (config.ShowHelp)
    {
        ShowHelp();
        return 0;
    }

    string input = ReadInput(config);
    var (header, rows) = ParseDelimited(input, config);
    var sortedRows = SortRows(rows, config);
    string output = Serialize(header, sortedRows, config);
    WriteOutput(output, config);
    
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

// --- 3. FUNCIONES LOCALES ---
AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    bool showHelp = false;
    var sortFields = new List<SortField>();

    int positionalCount = 0;

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];
        if (arg == "-h" || arg == "--help") showHelp = true;
        else if (arg == "-nh" || arg == "--no-header") noHeader = true;
        else if (arg == "-d" || arg == "--delimiter") delimiter = args[++i].Replace("\\t", "\t");
        else if (arg == "-i" || arg == "--input") input = args[++i];
        else if (arg == "-o" || arg == "--output") output = args[++i];
        else if (arg == "-b" || arg == "--by")
        {
            var parts = args[++i].Split(':');
            string name = parts[0];
            bool isNum = parts.Length > 1 && parts[1] == "num";
            bool isDesc = parts.Length > 2 && parts[2] == "desc";
            sortFields.Add(new SortField(name, isNum, isDesc));
        }
        else
        {
            if (positionalCount == 0) input = arg;
            else if (positionalCount == 1) output = arg;
            positionalCount++;
        }
    }

    return new AppConfig(input, output, delimiter, noHeader, sortFields, showHelp);
}

void ShowHelp()
{
    Console.WriteLine("Uso: sortx [input [output]] [-b|--by campo[:tipo[:orden]]]... [-d delimitador] [-nh] [-h]");
}

string ReadInput(AppConfig config)
{
    if (string.IsNullOrEmpty(config.InputFile))
    {
        if (Console.IsInputRedirected) return Console.In.ReadToEnd();
        throw new Exception("Falta archivo de entrada.");
    }
    if (!File.Exists(config.InputFile)) throw new Exception($"Archivo no encontrado: {config.InputFile}");
    return File.ReadAllText(config.InputFile);
}

(string? header, List<Dictionary<string, string>> rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    if (lines.Length == 0) return (null, new List<Dictionary<string, string>>());

    string? headerLine = null;
    int startIndex = 0;
    string[] keys;

    if (config.NoHeader)
    {
        int colCount = lines[0].Split(config.Delimiter).Length;
        keys = Enumerable.Range(0, colCount).Select(i => i.ToString()).ToArray();
    }
    else
    {
        headerLine = lines[0];
        keys = headerLine.Split(config.Delimiter);
        startIndex = 1;
    }

    var rowsList = new List<Dictionary<string, string>>();
    for (int i = startIndex; i < lines.Length; i++)
    {
        var values = lines[i].Split(config.Delimiter);
        var dict = new Dictionary<string, string>();
        for (int j = 0; j < keys.Length; j++)
        {
            dict[keys[j]] = j < values.Length ? values[j] : "";
        }
        rowsList.Add(dict);
    }
    return (headerLine, rowsList);
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (config.SortFields.Count == 0 || rows.Count == 0) return rows;

    IOrderedEnumerable<Dictionary<string, string>>? query = null;
    bool isFirst = true;

    foreach (var field in config.SortFields)
    {
        object KeySelector(Dictionary<string, string> row)
        {
            if (!row.TryGetValue(field.Name, out string? val))
                throw new Exception($"Columna inexistente: {field.Name}");

            if (field.Numeric)
            {
                return double.TryParse(val, out double num) ? num : double.MinValue;
            }
            return val;
        }

        if (isFirst)
        {
            query = field.Descending ? rows.OrderByDescending(KeySelector) : rows.OrderBy(KeySelector);
            isFirst = false;
        }
        else
        {
            query = field.Descending ? query!.ThenByDescending(KeySelector) : query!.ThenBy(KeySelector);
        }
    }

    return query!.ToList();
}

string Serialize(string? header, List<Dictionary<string, string>> rows, AppConfig config)
{
    var sb = new StringBuilder();
    if (header != null) sb.AppendLine(header);

    if (rows.Count > 0)
    {
        var keys = rows[0].Keys.ToList();
        foreach (var row in rows)
        {
            var values = keys.Select(k => row[k]);
            sb.AppendLine(string.Join(config.Delimiter, values));
        }
    }
    
    return sb.ToString();
}

void WriteOutput(string text, AppConfig config)
{
    if (string.IsNullOrEmpty(config.OutputFile))
    {
        Console.Write(text);
    }
    else
    {
        File.WriteAllText(config.OutputFile, text);
    }
}

// --- 2. MODELO DE CONFIGURACIÓN ---
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields,
    bool ShowHelp
);
