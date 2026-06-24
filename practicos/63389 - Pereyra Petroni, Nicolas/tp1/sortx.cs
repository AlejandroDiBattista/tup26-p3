// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

using System.Globalization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

try
{
    AppConfig config = ParseArgs(args);

    if (config.Help)
    {
        ShowHelp();
        return;
    }

    string text = ReadInput(config);
    var data = ParseDelimited(text, config);
    var sortedRows = SortRows(data.Rows, data.Headers, config);
    string output = Serialize(data.Headers, sortedRows, config);
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error: " + ex.Message);
    Environment.Exit(1);
}

AppConfig ParseArgs(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;
    string delimiter = ",";
    bool noHeader = false;
    bool help = false;
    List<SortField> sortFields = new();
    List<string> positional = new();

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];

        if (arg == "-h" || arg == "--help")
        {
            help = true;
        }
        else if (arg == "-nh" || arg == "--no-header")
        {
            noHeader = true;
        }
        else if (arg == "-i" || arg == "--input")
        {
            inputFile = ReadNextArg(args, ref i, arg);
        }
        else if (arg == "-o" || arg == "--output")
        {
            outputFile = ReadNextArg(args, ref i, arg);
        }
        else if (arg == "-d" || arg == "--delimiter")
        {
            delimiter = ReadNextArg(args, ref i, arg);

            if (delimiter == "\\t")
            {
                delimiter = "\t";
            }
        }
        else if (arg == "-b" || arg == "--by")
        {
            sortFields.Add(ParseSortField(ReadNextArg(args, ref i, arg)));
        }
        else
        {
            positional.Add(arg);
        }
    }

    if (positional.Count > 0 && inputFile == null)
    {
        inputFile = positional[0];
    }

    if (positional.Count > 1 && outputFile == null)
    {
        outputFile = positional[1];
    }

    if (positional.Count > 2)
    {
        throw new Exception("Hay demasiados argumentos posicionales.");
    }

    return new AppConfig(inputFile, outputFile, delimiter, noHeader, sortFields, help);
}

string ReadNextArg(string[] args, ref int index, string option)
{
    if (index + 1 >= args.Length)
    {
        throw new Exception("Falta el valor para " + option + ".");
    }

    index++;
    return args[index];
}

SortField ParseSortField(string text)
{
    string[] parts = text.Split(':');

    if (parts.Length > 3 || parts[0] == "")
    {
        throw new Exception("Campo de ordenamiento invalido: " + text);
    }

    string name = parts[0];
    string type = parts.Length >= 2 ? parts[1].ToLower() : "alpha";
    string order = parts.Length >= 3 ? parts[2].ToLower() : "asc";

    if (type != "alpha" && type != "num")
    {
        throw new Exception("Tipo invalido en " + text + ". Use alpha o num.");
    }

    if (order != "asc" && order != "desc")
    {
        throw new Exception("Orden invalido en " + text + ". Use asc o desc.");
    }

    return new SortField(name, type == "num", order == "desc");
}

string ReadInput(AppConfig config)
{
    if (config.InputFile == null)
    {
        return Console.In.ReadToEnd();
    }

    return File.ReadAllText(config.InputFile);
}

(List<string> Headers, List<Dictionary<string, string>> Rows) ParseDelimited(string text, AppConfig config)
{
    List<string> lines = text
        .Replace("\r\n", "\n")
        .Replace("\r", "\n")
        .Split('\n')
        .Where(line => line.Length > 0)
        .ToList();

    if (lines.Count == 0)
    {
        throw new Exception("El archivo de entrada esta vacio.");
    }

    List<string> headers;
    int firstDataLine;

    if (config.NoHeader)
    {
        string[] firstRow = SplitLine(lines[0], config.Delimiter);
        headers = new List<string>();

        for (int i = 0; i < firstRow.Length; i++)
        {
            headers.Add(i.ToString());
        }

        firstDataLine = 0;
    }
    else
    {
        headers = SplitLine(lines[0], config.Delimiter).ToList();
        firstDataLine = 1;
    }

    List<Dictionary<string, string>> rows = new();

    for (int i = firstDataLine; i < lines.Count; i++)
    {
        string[] values = SplitLine(lines[i], config.Delimiter);

        if (values.Length != headers.Count)
        {
            throw new Exception("La fila " + (i + 1) + " no tiene la cantidad correcta de columnas.");
        }

        Dictionary<string, string> row = new();

        for (int j = 0; j < headers.Count; j++)
        {
            row[headers[j]] = values[j];
        }

        rows.Add(row);
    }

    return (headers, rows);
}

string[] SplitLine(string line, string delimiter)
{
    return line.Split(delimiter, StringSplitOptions.None);
}

List<Dictionary<string, string>> SortRows(
    List<Dictionary<string, string>> rows,
    List<string> headers,
    AppConfig config)
{
    foreach (SortField field in config.SortFields)
    {
        if (!headers.Contains(field.Name))
        {
            throw new Exception("No existe la columna: " + field.Name);
        }
    }

    List<Dictionary<string, string>> sortedRows = new(rows);

    for (int i = 0; i < sortedRows.Count - 1; i++)
    {
        for (int j = i + 1; j < sortedRows.Count; j++)
        {
            if (CompareRows(sortedRows[i], sortedRows[j]) > 0)
            {
                var aux = sortedRows[i];
                sortedRows[i] = sortedRows[j];
                sortedRows[j] = aux;
            }
        }
    }

    return sortedRows;

    int CompareRows(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        foreach (SortField field in config.SortFields)
        {
            int result;

            if (field.Numeric)
            {
                double numberA = ParseNumber(a[field.Name], field.Name);
                double numberB = ParseNumber(b[field.Name], field.Name);
                result = numberA.CompareTo(numberB);
            }
            else
            {
                result = string.Compare(a[field.Name], b[field.Name], StringComparison.CurrentCultureIgnoreCase);
            }

            if (result != 0)
            {
                return field.Descending ? -result : result;
            }
        }

        return 0;
    }
}

double ParseNumber(string value, string fieldName)
{
    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
    {
        return number;
    }

    if (double.TryParse(value, out number))
    {
        return number;
    }

    throw new Exception("El valor '" + value + "' de la columna " + fieldName + " no es numerico.");
}

string Serialize(
    List<string> headers,
    List<Dictionary<string, string>> rows,
    AppConfig config)
{
    List<string> lines = new();

    if (!config.NoHeader)
    {
        lines.Add(string.Join(config.Delimiter, headers));
    }

    foreach (Dictionary<string, string> row in rows)
    {
        List<string> values = new();

        foreach (string header in headers)
        {
            values.Add(row[header]);
        }

        lines.Add(string.Join(config.Delimiter, values));
    }

    return string.Join(Environment.NewLine, lines) + Environment.NewLine;
}

void WriteOutput(string output, AppConfig config)
{
    if (config.OutputFile == null)
    {
        Console.Write(output);
    }
    else
    {
        File.WriteAllText(config.OutputFile, output, Encoding.UTF8);
    }
}

void ShowHelp()
{
    Console.WriteLine("""
sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
      [-i|--input input] [-o|--output output]
      [-d|--delimiter delimitador]
      [-nh|--no-header] [-h|--help]

Opciones:
  -b,  --by           Campo para ordenar. Ejemplo: salario:num:desc
  -i,  --input        Archivo de entrada
  -o,  --output       Archivo de salida
  -d,  --delimiter    Delimitador. Default: ,  Use \t para tabulacion
  -nh, --no-header    El archivo no tiene encabezado
  -h,  --help         Muestra esta ayuda
""");
}

record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields,
    bool Help
);

