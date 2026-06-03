using System;
using System.Collections.Generic;
using System.IO;

// Flujo de funciones

try
{
    AppConfig config = ParseArgs(args);

    string inputData = ReadInput(config.InputFile);

    CsvData parsedData = ParseDelimited(inputData, config.Delimiter, config.NoHeader);

    SortRows(parsedData, config.SortFields);

    string outputData = Serialize(parsedData, config.Delimiter, config.NoHeader);

    WriteOutput(config.OutputFile, outputData); 
}

catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

//lectura de argumentos y genera AppConfig
AppConfig ParseArgs(string[] arguments)
{
    string? input = null;
    string? output = null;
    string delim = ",";
    bool noHeader = false;
    List<SortField> sortFields = new List<SortField>();

    for (int i = 0; i < arguments.Length; i++)
    {
        string arg = arguments[i];

        if (arg == "-h" || arg == "--help")
        {
            Console.WriteLine("Uso: sortx [input] [output] [-b campo:tipo:orden] [-d delimitador] [-nh]");
            Environment.Exit(0);
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            delim = arguments[++i];
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            noHeader = true;
        }
        else if (arg == "-b" || arg == "--by")
        {
            string rawBy = arguments[++i];
            string[] parts = rawBy.Split(':');

            string name = parts[0];
            bool isNumeric = parts.Length > 1 && parts[1] == "num";
            bool isDesc = parts.Length > 2 && parts[2] == "desc";

            sortFields.Add(new SortField(name, isNumeric, isDesc));
        }
        else if (arg == "-i" || arg == "--input")
        {
            input = arguments[++i];
        }
        else if (arg == "-o" || arg == "--output")
        {
            output = arguments[++i];
        }
        else if (!arg.StartsWith("-"))
        {
            if (input == null) input = arg;
            else if (output == null) output = arg;
        }
    }

    return new AppConfig(input, output, delim, noHeader, sortFields);
}
// lectura del archivo 
string ReadInput(string? filePath)
{
    if (string.IsNullOrEmpty(filePath))
    {
        return Console.In.ReadToEnd(); 
    }
    return File.ReadAllText(filePath); 
}

// Creacion del archivo de datos
void WriteOutput(string? filePath, string content)
{
    if (string.IsNullOrEmpty(filePath))
    {
        Console.Write(content); 
    }
    else
    {
        File.WriteAllText(filePath, content); 
    }
}

// Transforma el texto crudo en una tabla estructurada (cabeceras y filas)
CsvData ParseDelimited(string rawText, string delimiter, bool noHeader)
{
    string[] lines = rawText.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);

    List<string[]> rows = new List<string[]>();
    string[] headers = Array.Empty<string>();

    if (lines.Length == 0) return new CsvData(headers, rows);

    int startIndex = 0;

    if (!noHeader)
    {
        headers = lines[0].Split(delimiter);
        startIndex = 1; 
    }
    else
    {
        int colCount = lines[0].Split(delimiter).Length;
        headers = new string[colCount];
        for (int i = 0; i < colCount; i++) headers[i] = $"Col{i}";
    }
    for (int i = startIndex; i < lines.Length; i++)
    {
        rows.Add(lines[i].Split(delimiter));
    }

    return new CsvData(headers, rows);
}

// ordenamiento de tablas
void SortRows(CsvData data, List<SortField> sortFields)
{
    if (sortFields.Count == 0 || data.Rows.Count <= 1) return;

    var sortColumns = new List<(int Index, SortField Field)>();
    foreach (var field in sortFields)
    {
        int index = Array.IndexOf(data.Headers, field.Name);
        if (index != -1) sortColumns.Add((index, field));
    }

    data.Rows.Sort((rowA, rowB) =>
    {
        foreach (var col in sortColumns)
        {
            
            string valA = rowA.Length > col.Index ? rowA[col.Index] : "";
            string valB = rowB.Length > col.Index ? rowB[col.Index] : "";

            int result = 0;

            if (col.Field.Numeric)
            {
                
                double numA = double.TryParse(valA, out double nA) ? nA : 0;
                double numB = double.TryParse(valB, out double nB) ? nB : 0;
                result = numA.CompareTo(numB);
            }
            else
            {
                result = string.Compare(valA, valB, StringComparison.OrdinalIgnoreCase);
            }
            if (result != 0)
            {
                return col.Field.Descending ? -result : result;
            }
        }
        return 0; 
    });
}

// Reconstruye la tabla en formato de texto usando el delimitador
string Serialize(CsvData data, string delimiter, bool noHeader)
{
    var sb = new System.Text.StringBuilder();

    if (!noHeader && data.Headers.Length > 0)
    {
        sb.AppendLine(string.Join(delimiter, data.Headers));
    }
    foreach (var row in data.Rows)
    {
        sb.AppendLine(string.Join(delimiter, row));
    }
    return sb.ToString();
}

// almanecer criterios de ordenamientos 
record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);
record CsvData(string[] Headers, List<string[]> Rows);