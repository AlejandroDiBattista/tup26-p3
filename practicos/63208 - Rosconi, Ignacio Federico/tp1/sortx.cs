
using System.Globalization;

try
{
    var config = ParseArgs(args);
    if (config == null) return; // Caso de --help

    var lines = ReadInput(config.InputFile);
    var (header, rows) = ParseDelimited(lines, config);
    var sortedRows = SortRows(rows, config, header);
    var outputText = Serialize(header, sortedRows, config);
    WriteOutput(outputText, config.OutputFile);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

// 1. ParseArgs: Leer la configuración desde los argumentos
AppConfig? ParseArgs(string[] args)
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
            case "-h": case "--help":
                ShowHelp();
                return null;
            case "-nh": case "--no-header":
                noHeader = true;
                break;
            case "-d": case "--delimiter":
                delimiter = args[++i].Replace("\\t", "\t");
                break;
            case "-i": case "--input":
                input = args[++i];
                break;
            case "-o": case "--output":
                output = args[++i];
                break;
            case "-b": case "--by":
                var parts = args[++i].Split(':');
                string name = parts[0];
                bool numeric = parts.Length > 1 && parts[1] == "num";
                bool desc = parts.Length > 2 && parts[2] == "desc";
                sortFields.Add(new SortField(name, numeric, desc));
                break;
            default:
                if (!args[i].StartsWith("-")) positionalArgs.Add(args[i]);
                break;
        }
    }

    if (input == null && positionalArgs.Count > 0) input = positionalArgs[0];
    if (output == null && positionalArgs.Count > 1) output = positionalArgs[1];

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

void ShowHelp()
{
    Console.WriteLine("Uso: sortx [input [output]] [opciones]");
    Console.WriteLine("Opciones:");
    Console.WriteLine("  -b, --by campo[:tipo[:orden]]  Campo para ordenar (alpha|num, asc|desc)");
    Console.WriteLine("  -d, --delimiter DELIM         Carácter delimitador (default: ,)");
    Console.WriteLine("  -nh, --no-header              El archivo no tiene encabezado");
    Console.WriteLine("  -i, --input ARCHIVO           Archivo de entrada");
    Console.WriteLine("  -o, --output ARCHIVO          Archivo de salida");
}

// 2. ReadInput: Leer texto desde archivo o stdin
List<string> ReadInput(string? filePath)
{
    if (string.IsNullOrEmpty(filePath))
    {
        var inputLines = new List<string>();
        string? line;
        while ((line = Console.ReadLine()) != null) inputLines.Add(line);
        return inputLines;
    }
    return File.ReadAllLines(filePath).ToList();
}

// 3. ParseDelimited: Convertir texto en lista de filas (diccionarios)
(string[]? Header, List<Dictionary<string, string>> Rows) ParseDelimited(List<string> lines, AppConfig config)
{
    if (lines.Count == 0) return (null, new List<Dictionary<string, string>>());

    string[]? header = null;
    int startIdx = 0;

    if (!config.NoHeader)
    {
        header = lines[0].Split(config.Delimiter);
        startIdx = 1;
    }

    var rows = new List<Dictionary<string, string>>();
    for (int i = startIdx; i < lines.Count; i++)
    {
        var values = lines[i].Split(config.Delimiter);
        var row = new Dictionary<string, string>();
        for (int j = 0; j < values.Length; j++)
        {
            string key = config.NoHeader ? j.ToString() : (header != null && j < header.Length ? header[j] : j.ToString());
            row[key] = values[j];
        }
        rows.Add(row);
    }
    return (header, rows);
}

// 4. SortRows: Ordenar filas según criterios
List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config, string[]? header)
{
    if (config.SortFields.Count == 0) return rows;

    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    foreach (var field in config.SortFields)
    {
        Func<Dictionary<string, string>, object> keySelector = r =>
        {
            if (!r.ContainsKey(field.Name)) 
                throw new Exception($"El campo '{field.Name}' no existe.");
            
            if (field.Numeric)
            {
                return double.TryParse(r[field.Name], NumberStyles.Any, CultureInfo.InvariantCulture, out double n) ? n : 0.0;
            }
            return r[field.Name];
        };

        if (ordered == null)
        {
            ordered = field.Descending ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);
        }
        else
        {
            ordered = field.Descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
        }
    }

    return ordered?.ToList() ?? rows;
}

// 5. Serialize: Convertir de vuelta a texto
string Serialize(string[]? header, List<Dictionary<string, string>> rows, AppConfig config)
{
    var result = new List<string>();
    if (header != null) result.Add(string.Join(config.Delimiter, header));

    foreach (var row in rows)
    {
        var values = row.Values;
        result.Add(string.Join(config.Delimiter, values));
    }
    return string.Join(Environment.NewLine, result);
}

// 6. WriteOutput: Escribir en archivo o stdout
void WriteOutput(string text, string? filePath)
{
    if (string.IsNullOrEmpty(filePath))
    {
        Console.WriteLine(text);
    }
    else
    {
        File.WriteAllText(filePath, text);
    }
}

// Modelos de datos
record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(string? InputFile, string? OutputFile, string Delimiter, bool NoHeader, List<SortField> SortFields);