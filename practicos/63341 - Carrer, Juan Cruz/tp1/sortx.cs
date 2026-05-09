using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


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

    var positionals = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        switch (arg)
        {
            case "-i":
            case "--input":
                input = args[++i];
                break;

            case "-o":
            case "--output":
                output = args[++i];
                break;

            case "-d":
            case "--delimiter":
                delimiter = args[++i] == "\\t" ? "\t" : args[i];
                break;

            case "-nh":
            case "--no-header":
                noHeader = true;
                break;

            case "-h":
            case "--help":
                ShowHelp();
                Environment.Exit(0);
                break;

            case "-b":
            case "--by":
                var spec = args[++i];
                sortFields.Add(ParseSortField(spec));
                break;

            default:
                if (arg.StartsWith("-"))
                    throw new Exception($"Opción desconocida: {arg}");

                positionals.Add(arg);
                break;
        }
    }
    

    // Posicionales
    if (positionals.Count > 0 && input == null)
        input = positionals[0];

    if (positionals.Count > 1 && output == null)
        output = positionals[1];

    if (sortFields.Count == 0)
        throw new Exception("Debe especificar al menos un campo de ordenamiento con -b");

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

void ShowHelp()
{
    Console.WriteLine(@"
Uso:
  sortx [input [output]] -b campo[:tipo[:orden]]...

Opciones:
  -b, --by           Campo de ordenamiento
  -i, --input        Archivo de entrada
  -o, --output       Archivo de salida
  -d, --delimiter    Delimitador (default ,)
  -nh, --no-header   Sin encabezado
  -h, --help         Mostrar ayuda

Ejemplos:
  sortx empleados.csv -b apellido
  sortx empleados.csv -b salario:num:desc
");
}

string ReadInput(AppConfig config)
{
    // Si hay archivo → leer archivo
    if (!string.IsNullOrEmpty(config.InputFile))
    {
        if (!File.Exists(config.InputFile))
            throw new Exception($"Archivo no encontrado: {config.InputFile}");

        return File.ReadAllText(config.InputFile);
    }

    // Si no hay archivo → leer stdin
    if (!Console.IsInputRedirected)
        throw new Exception("No se especificó archivo de entrada ni hay datos en stdin");

    using var reader = Console.In;
    return reader.ReadToEnd();
}

List<Dictionary<string, string>> ParseDelimited(string text, AppConfig config)
{
    var rows = new List<Dictionary<string, string>>();

    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    if (lines.Length == 0)
        return rows;

    string[] headers;

    if (!config.NoHeader)
    {
        headers = lines[0].Split(config.Delimiter);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(config.Delimiter);

            var dict = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length; j++)
                dict[headers[j]] = values[j];

            rows.Add(dict);
        }
    }
    else
    {
        var first = lines[0].Split(config.Delimiter);
        headers = Enumerable.Range(0, first.Length).Select(i => i.ToString()).ToArray();

        foreach (var line in lines)
        {
            var values = line.Split(config.Delimiter);
            var dict = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length; j++)
                dict[headers[j]] = values[j];

            rows.Add(dict);
        }
    }

    return rows;
}

List<Dictionary<string, string>> SortRows(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
        return rows;

    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    foreach (var field in config.SortFields)
    {
        Func<Dictionary<string, string>, object> key = row =>
        {
            if (!row.ContainsKey(field.Name))
                throw new Exception($"Columna inexistente: {field.Name}");

            var value = row[field.Name];

            if (field.Numeric)
            {
                if (!double.TryParse(value, out var num))
                    throw new Exception($"Valor no numérico: {value}");

                return num;
            }

            return value;
        };

        if (ordered == null)
        {
            ordered = field.Descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
        }
        else
        {
            ordered = field.Descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
        }
    }

    return (ordered ?? Enumerable.Empty<Dictionary<string, string>>()).ToList();
}

string Serialize(List<Dictionary<string, string>> rows, AppConfig config)
{
    if (rows.Count == 0)
        return "";

    var sb = new StringBuilder();
    var headers = rows[0].Keys.ToList();

    if (!config.NoHeader)
        sb.AppendLine(string.Join(config.Delimiter, headers));

    foreach (var row in rows)
    {
        var values = headers.Select(h => row[h]);
        sb.AppendLine(string.Join(config.Delimiter, values));
    }

    return sb.ToString();
}

void WriteOutput(string output, AppConfig config)
{
    if (!string.IsNullOrEmpty(config.OutputFile))
    {
        File.WriteAllText(config.OutputFile, output);
    }
    else
    {
        Console.WriteLine(output);
    }
}

SortField ParseSortField(string spec)
{
    var parts = spec.Split(':');

    var name = parts[0];

    bool numeric = false;
    bool desc = false;

    if (parts.Length > 1)
    {
        numeric = parts[1] switch
        {
            "num" => true,
            "alpha" => false,
            _ => throw new Exception($"Tipo inválido: {parts[1]}")
        };
    }

    if (parts.Length > 2)
    {
        desc = parts[2] switch
        {
            "desc" => true,
            "asc" => false,
            _ => throw new Exception($"Orden inválido: {parts[2]}")
        };
    }

    return new SortField(name, numeric, desc);
}
record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string?         InputFile,
    string?         OutputFile,
    string          Delimiter,
    bool            NoHeader,
    List<SortField> SortFields
);