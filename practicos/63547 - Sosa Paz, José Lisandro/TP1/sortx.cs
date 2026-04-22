using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
 
// ─── Punto de entrada ────────────────────────────────────────────────────────
 
try
{
    AppConfig config                                                    = ParseArgs(args);
    string rawInput                                                     = ReadInput(config);
    (List<Dictionary<string, string>> rows, string[]? header)          = ParseDelimited(rawInput, config);
    List<Dictionary<string, string>> sorted                            = SortRows(rows, config);
    string output                                                       = Serialize(sorted, header, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
 
// ─── Funciones locales del pipeline ─────────────────────────────────────────
 
AppConfig ParseArgs(string[] args)
{
    string?         inputFile  = null;
    string?         outputFile = null;
    string          delimiter  = ",";
    bool            noHeader   = false;
    List<SortField> sortFields = new();
    List<string>    positional = new();
 
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--help":
            case "-h":
                Console.WriteLine("""
                    Uso: sortx [input [output]] [-b campo[:tipo[:orden]]]...
                               [-i input] [-o output] [-d delimitador]
                               [-nh] [-h]
 
                    Opciones:
                      -b, --by           Campo por el que ordenar (repetible)
                      -i, --input        Archivo de entrada
                      -o, --output       Archivo de salida
                      -d, --delimiter    Delimitador (default: ',')
                      -nh, --no-header   Sin fila de encabezado
                      -h, --help         Muestra esta ayuda
 
                    Formato de campo: campo[:tipo[:orden]]
                      tipo:  alpha (default) | num
                      orden: asc  (default)  | desc
 
                    Ejemplos:
                      sortx empleados.csv -b apellido
                      sortx empleados.csv resultado.csv -b salario:num:desc
                      sortx empleados.csv -b departamento -b salario:num:desc
                      cat empleados.csv | sortx -b apellido
                    """);
                Environment.Exit(0);
                break;
 
            case "--no-header":
            case "-nh":
                noHeader = true;
                break;
 
            case "--input":
            case "-i":
                inputFile = args[++i];
                break;
 
            case "--output":
            case "-o":
                outputFile = args[++i];
                break;
 
            case "--delimiter":
            case "-d":
                string raw = args[++i];
                delimiter = raw == @"\t" ? "\t" : raw;
                break;
 
            case "--by":
            case "-b":
                sortFields.Add(ParseSortField(args[++i]));
                break;
 
            default:
                if (!args[i].StartsWith("-"))
                    positional.Add(args[i]);
                else
                    throw new Exception($"Opción desconocida: {args[i]}");
                break;
        }
    }
 
    if (positional.Count >= 1 && inputFile  == null) inputFile  = positional[0];
    if (positional.Count >= 2 && outputFile == null) outputFile = positional[1];
 
    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields);
}
 
SortField ParseSortField(string spec)
{
    string[] parts      = spec.Split(':');
    string   name       = parts[0];
    bool     numeric    = parts.Length > 1 && parts[1].Equals("num",  StringComparison.OrdinalIgnoreCase);
    bool     descending = parts.Length > 2 && parts[2].Equals("desc", StringComparison.OrdinalIgnoreCase);
    return new SortField(name, numeric, descending);
}
 
string ReadInput(AppConfig config)
{
    if (config.InputFile is not null)
    {
        if (!File.Exists(config.InputFile))
            throw new Exception($"No se pudo localizar el archivo de datos: '{config.InputFile}'");
        return File.ReadAllText(config.InputFile);
    }
    return Console.In.ReadToEnd();
}
 
(List<Dictionary<string, string>> rows, string[]? header) ParseDelimited(string input, AppConfig config)
{
    string[] lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);
 
    if (lines.Length == 0)
        return (new List<Dictionary<string, string>>(), null);
 
    string[]? header     = null;
    int       startIndex = 0;
 
    if (!config.NoHeader)
    {
        header     = lines[0].TrimEnd('\r').Split(config.Delimiter);
        startIndex = 1;
    }
 
    List<Dictionary<string, string>> rows = new();
 
    for (int i = startIndex; i < lines.Length; i++)
    {
        string[]                   values = lines[i].TrimEnd('\r').Split(config.Delimiter);
        Dictionary<string, string> row    = new();
 
        if (header is not null)
        {
            for (int j = 0; j < header.Length; j++)
                row[header[j].Trim()] = j < values.Length ? values[j].Trim() : "";
        }
        else
        {
            for (int j = 0; j < values.Length; j++)
                row[j.ToString()] = values[j].Trim();
        }
 
        rows.Add(row);
    }
 
    return (rows, header);
}
 
List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (config.SortFields.Count == 0 || rows.Count == 0)
        return rows;
 
    foreach (SortField field in config.SortFields)
    {
        if (!rows[0].ContainsKey(field.Name))
            throw new Exception($"Campo no encontrado: '{field.Name}'");
    }
 
    IComparer<Dictionary<string, string>> comparer = BuildComparer(config.SortFields);
    return rows.OrderBy(r => r, comparer).ToList();
}
 
IComparer<Dictionary<string, string>> BuildComparer(List<SortField> fields)
{
    return Comparer<Dictionary<string, string>>.Create((a, b) =>
    {
        foreach (SortField field in fields)
        {
            string va = a.TryGetValue(field.Name, out string? x) ? x : "";
            string vb = b.TryGetValue(field.Name, out string? y) ? y : "";
 
            int cmp;
            if (field.Numeric
                && decimal.TryParse(va, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal da)
                && decimal.TryParse(vb, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal db))
            {
                cmp = da.CompareTo(db);
            }
            else
            {
                cmp = string.Compare(va, vb, StringComparison.CurrentCulture);
            }
 
            if (cmp != 0)
                return field.Descending ? -cmp : cmp;
        }
        return 0;
    });
}
 
string Serialize(List<Dictionary<string, string>> rows, string[]? header, AppConfig config)
{
    List<string> lines = new();
 
    if (header is not null)
        lines.Add(string.Join(config.Delimiter, header));
 
    foreach (Dictionary<string, string> row in rows)
    {
        if (header is not null)
            lines.Add(string.Join(config.Delimiter,
                header.Select(h => row.TryGetValue(h.Trim(), out string? val) ? val : "")));
        else
        {
            int count = row.Count;
            lines.Add(string.Join(config.Delimiter,
                Enumerable.Range(0, count).Select(i => row.TryGetValue(i.ToString(), out string? val) ? val : "")));
        }
    }
 
    return string.Join(Environment.NewLine, lines);
}
 
void WriteOutput(string output, AppConfig config)
{
    if (config.OutputFile is not null)
        File.WriteAllText(config.OutputFile, output);
    else
        Console.WriteLine(output);
}
 
// ─── Records de configuración (al final, después de top-level statements) ────
 
record SortField(string Name, bool Numeric, bool Descending);
 
record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields
);